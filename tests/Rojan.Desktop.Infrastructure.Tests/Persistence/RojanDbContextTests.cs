using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence;

/// <summary>Exercises <see cref="RojanDbContext"/> against a temp-file SQLite database - construction, connection, and (empty, Commit 1 has no entities yet) database creation.</summary>
public sealed class RojanDbContextTests : IDisposable
{
    private readonly string _testRoot;

    public RojanDbContextTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite pools physical connections by connection
        // string for reuse, so a disposed RojanDbContext does not
        // necessarily release its file handle immediately - without
        // clearing the pool first, deleting _testRoot right after a test
        // can fail with "the process cannot access the file" even though
        // every context/connection in this test was already disposed.
        SqliteConnection.ClearAllPools();

        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private RojanDbContext CreateContext()
    {
        var options = new SqlitePersistenceOptions(Path.Combine(_testRoot, "rojan.db"));
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);

        return new RojanDbContext(optionsBuilder.Options);
    }

    [Fact]
    public void Constructor_GivenSqliteOptions_CreatesContextWithoutThrowing()
    {
        var exception = Record.Exception(() =>
        {
            using var context = CreateContext();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void Database_ProviderName_ReportsSqlite()
    {
        using var context = CreateContext();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public async Task Database_CanConnectAsync_AfterEnsureCreated_ReturnsTrue()
    {
        using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();

        var canConnect = await context.Database.CanConnectAsync();

        Assert.True(canConnect);
    }

    [Fact]
    public async Task Database_OpenConnectionAsync_CreatesTheDatabaseFile()
    {
        // SQLite creates the physical file as soon as a connection to a
        // not-yet-existing path is opened - independent of EnsureCreated's
        // schema-creation behavior (which this Commit 1 context has none
        // of yet, zero DbSet<TEntity>). Opening/closing the connection
        // directly is the most direct proof the configured SqliteOptions
        // connection string actually resolves to a writable location.
        var databasePath = Path.Combine(_testRoot, "rojan.db");
        var options = new SqlitePersistenceOptions(databasePath);
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>().UseSqlite(options.ConnectionString);
        using var context = new RojanDbContext(optionsBuilder.Options);

        await context.Database.OpenConnectionAsync();
        await context.Database.CloseConnectionAsync();

        Assert.True(File.Exists(databasePath));
    }
}
