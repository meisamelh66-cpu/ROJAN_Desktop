using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Infrastructure.DependencyInjection;
using Rojan.Server.Infrastructure.Persistence;

namespace Rojan.Server.Infrastructure.Tests;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. Foundation-only tests -
/// <see cref="ServiceCollectionExtensions.AddInfrastructure"/> only wires
/// <see cref="RojanServerDbContext"/> today (no repositories, since there
/// are no business entities yet). Building/resolving the DbContext here
/// never opens a real PostgreSQL connection - EF Core connects lazily
/// (see <see cref="RojanServerDbContext"/>'s own doc comment) - so these
/// tests need no real database.
/// </summary>
public sealed class ServiceCollectionExtensionsTests
{
    private static IConfiguration BuildConfiguration(string? connectionString)
    {
        var json = connectionString is null
            ? """{ "ConnectionStrings": {} }"""
            : $$"""{ "ConnectionStrings": { "DefaultConnection": "{{connectionString}}" } }""";

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    [Fact]
    public void AddInfrastructure_ValidConnectionString_RegistersDbContextResolvableWithoutConnecting()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("Host=localhost;Database=rojan_test;Username=postgres;Password=postgres");

        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();

        var dbContext = provider.GetRequiredService<RojanServerDbContext>();
        Assert.NotNull(dbContext);
    }

    [Fact]
    public void AddInfrastructure_MissingConnectionString_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration(connectionString: null);

        Assert.Throws<InvalidOperationException>(() => services.AddInfrastructure(configuration));
    }

    [Fact]
    public void AddInfrastructure_ReturnsTheSameServiceCollectionForChaining()
    {
        var services = new ServiceCollection();
        var configuration = BuildConfiguration("Host=localhost;Database=rojan_test;Username=postgres;Password=postgres");

        var result = services.AddInfrastructure(configuration);

        Assert.Same(services, result);
    }
}
