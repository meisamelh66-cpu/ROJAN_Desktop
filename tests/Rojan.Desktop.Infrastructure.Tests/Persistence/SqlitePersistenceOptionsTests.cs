using Rojan.Desktop.Infrastructure.Persistence;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence;

/// <summary>Exercises <see cref="SqlitePersistenceOptions"/> - connection string shape, default path, and the parent-directory-creation side effect its own doc comment documents.</summary>
public sealed class SqlitePersistenceOptionsTests : IDisposable
{
    private readonly string _testRoot;

    public SqlitePersistenceOptionsTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    [Fact]
    public void ConnectionString_IsSqliteDataSourceFormat()
    {
        var databasePath = Path.Combine(_testRoot, "rojan.db");

        var sut = new SqlitePersistenceOptions(databasePath);

        Assert.Equal($"Data Source={databasePath}", sut.ConnectionString);
    }

    [Fact]
    public void Constructor_ExposesTheGivenDatabasePath()
    {
        var databasePath = Path.Combine(_testRoot, "rojan.db");

        var sut = new SqlitePersistenceOptions(databasePath);

        Assert.Equal(databasePath, sut.DatabasePath);
    }

    [Fact]
    public void Constructor_ParentDirectoryDoesNotExist_CreatesIt()
    {
        var nestedDirectory = Path.Combine(_testRoot, "database");
        var databasePath = Path.Combine(nestedDirectory, "rojan.db");
        Assert.False(Directory.Exists(nestedDirectory));

        _ = new SqlitePersistenceOptions(databasePath);

        Assert.True(Directory.Exists(nestedDirectory));
    }

    [Fact]
    public void Constructor_ParentDirectoryAlreadyExists_DoesNotThrow()
    {
        Directory.CreateDirectory(_testRoot);
        var databasePath = Path.Combine(_testRoot, "rojan.db");

        var exception = Record.Exception(() => new SqlitePersistenceOptions(databasePath));

        Assert.Null(exception);
    }
}
