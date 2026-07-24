using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Rojan.Desktop.Infrastructure.Persistence;

/// <summary>
/// Lets the <c>dotnet ef migrations add</c>/<c>dotnet ef database update</c>
/// CLI tooling construct a <see cref="RojanDbContext"/> at design time -
/// this is a class library with no runnable <c>Program.cs</c>/host for the
/// tooling to discover options from (Shell owns the real composition
/// root), so EF Core's own design-time factory seam is the standard way
/// to give the CLI a usable context without running the whole app. Uses
/// <see cref="SqlitePersistenceOptions.Default"/> - the same connection
/// every real app run would use - so a migration generated this way
/// targets the real database shape, not a throwaway one.
/// </summary>
public sealed class RojanDbContextFactory : IDesignTimeDbContextFactory<RojanDbContext>
{
    public RojanDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<RojanDbContext>();
        optionsBuilder.UseSqlite(SqlitePersistenceOptions.Default.ConnectionString);

        return new RojanDbContext(optionsBuilder.Options);
    }
}
