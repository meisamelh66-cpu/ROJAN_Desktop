using Microsoft.EntityFrameworkCore;
using Rojan.Desktop.Infrastructure.Persistence;

namespace Rojan.Desktop.Infrastructure.Tests.Persistence;

/// <summary>
/// Exercises <see cref="RojanDbContextFactory"/> - the design-time seam
/// <c>dotnet ef</c> tooling uses. Unlike every other test in this folder,
/// this necessarily touches <see cref="SqlitePersistenceOptions.Default"/>
/// (the real per-machine database path) rather than a temp path - the
/// factory has no path-injectable overload by design, since design-time
/// tooling should generate migrations against the same database shape a
/// real app run would use (see the factory's own doc comment). This never
/// opens a connection or creates a database file, only constructs the
/// context, so the only real side effect is the (harmless, idempotent)
/// parent-directory creation <see cref="SqlitePersistenceOptions"/>'s own
/// constructor already documents.
/// </summary>
public sealed class RojanDbContextFactoryTests
{
    [Fact]
    public void CreateDbContext_ReturnsAUsableContext()
    {
        var sut = new RojanDbContextFactory();

        using var context = sut.CreateDbContext([]);

        Assert.NotNull(context);
        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }

    [Fact]
    public void CreateDbContext_UsesTheDefaultDatabasePath()
    {
        var sut = new RojanDbContextFactory();

        using var context = sut.CreateDbContext([]);

        Assert.Contains(SqlitePersistenceOptions.Default.DatabasePath, context.Database.GetDbConnection().ConnectionString, StringComparison.Ordinal);
    }
}
