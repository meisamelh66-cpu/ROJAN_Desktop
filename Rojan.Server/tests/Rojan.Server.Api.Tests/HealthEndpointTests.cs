using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rojan.Server.Api.Tests;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. Foundation-only tests, per the
/// solution's own README - no business logic exists yet to test.
/// <see cref="WebApplicationFactory{TEntryPoint}"/> boots the real
/// <c>Program</c> (Controllers/Application/Infrastructure DI all wired
/// exactly as they would be at runtime) in-memory, over an in-memory
/// <see cref="HttpClient"/> - no real network socket, no real PostgreSQL
/// connection required (see <c>RojanServerDbContext</c>'s own doc
/// comment: EF Core only connects lazily, and nothing on the
/// <c>/health</c> path touches the database).
/// </summary>
public sealed class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ApplicationBoots_ServiceProviderIsAvailable()
    {
        using var scope = _factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider);
    }

    [Fact]
    public void Configuration_Loads_ConnectionStringsSectionIsPresent()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();

        // The key must exist (appsettings.json declares it, even if empty by
        // default) - proves configuration actually loaded appsettings.json,
        // not just that GetConnectionString tolerates a missing file.
        var section = configuration.GetSection("ConnectionStrings");
        Assert.True(section.Exists());
    }

    [Fact]
    public async Task GetHealth_ReturnsOkWithStatusOkBody()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        Assert.NotNull(body);
        Assert.Equal("ok", body!.Status);
    }

    [Fact]
    public async Task GetHealth_RequiresNoAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private sealed record HealthResponse(string Status);
}
