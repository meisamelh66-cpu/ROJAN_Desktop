using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Rojan.Server.Api.Tests.Authentication;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Application.Tenancy;

namespace Rojan.Server.Api.Tests.Tenancy;

/// <summary>Exercises <c>TenantController</c>'s <c>GET api/v1/tenant/current</c> end-to-end - the "API: authenticated tenant endpoint / unauthorized request rejected" requirements this commit's own task list calls out. Reuses <see cref="AuthApiFactory"/> (register a real user to obtain a real bearer token, then call the tenant endpoint with it).</summary>
public sealed class TenantEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public TenantEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterOrganizationOwnerRequest NewRegisterRequest() =>
        new("Rojan Salon", $"owner-{Guid.NewGuid():N}@rojan.example", "SuperSecret1", "Noah Bennett");

    [Fact]
    public async Task GetCurrent_AuthenticatedRequest_ReturnsCurrentTenantInfo()
    {
        var client = _factory.CreateClient();
        var registerRequest = NewRegisterRequest();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);

        var response = await client.GetAsync("/api/v1/tenant/current");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tenant = await response.Content.ReadFromJsonAsync<CurrentTenantDto>();
        Assert.NotNull(tenant);
        Assert.Equal(registered.OrganizationId, tenant!.OrganizationId);
        Assert.Equal(registerRequest.OrganizationName, tenant.OrganizationName);
        Assert.Null(tenant.BranchId);
        Assert.Null(tenant.BranchName);
        Assert.Equal(registered.UserId, tenant.UserId);
    }

    [Fact]
    public async Task GetCurrent_NoAuthorizationHeader_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/tenant/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrent_InvalidBearerToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-real-token");

        var response = await client.GetAsync("/api/v1/tenant/current");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
