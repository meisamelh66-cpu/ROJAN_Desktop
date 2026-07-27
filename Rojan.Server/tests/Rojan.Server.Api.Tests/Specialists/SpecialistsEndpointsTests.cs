using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Rojan.Server.Api.Tests.Authentication;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Application.Specialists;

namespace Rojan.Server.Api.Tests.Specialists;

/// <summary>Exercises <c>SpecialistsController</c> end-to-end - same "API: authorized CRUD flow / unauthorized request rejected / Tenant A cannot access Tenant B specialist" coverage as <c>Customers.CustomersEndpointsTests</c>.</summary>
public sealed class SpecialistsEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public SpecialistsEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterOrganizationOwnerRequest NewRegisterRequest() =>
        new("Rojan Salon", $"owner-{Guid.NewGuid():N}@rojan.example", "SuperSecret1", "Noah Bennett");

    private static CreateSpecialistRequest NewCreateRequest() =>
        new("Priya Anand", "555-0100", "priya@rojan.example", null);

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", NewRegisterRequest());
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResult>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered!.AccessToken);

        return client;
    }

    [Fact]
    public async Task FullCrudFlow_AuthorizedRequests_WorksEndToEnd()
    {
        var client = await CreateAuthenticatedClientAsync();

        var createResponse = await client.PostAsJsonAsync("/api/v1/specialists", NewCreateRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<SpecialistDto>();
        Assert.NotNull(created);
        Assert.Equal("Priya Anand", created!.FullName);

        var getResponse = await client.GetAsync($"/api/v1/specialists/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<SpecialistDto>();
        Assert.Equal(created.Id, fetched!.Id);

        var listResponse = await client.GetAsync("/api/v1/specialists");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<SpecialistDto>>();
        Assert.Contains(list!, specialist => specialist.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/specialists/{created.Id}",
            new UpdateSpecialistRequest("Priya Anand-Sharma", "555-0199", "priya.updated@rojan.example", null, "Active"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<SpecialistDto>();
        Assert.Equal("Priya Anand-Sharma", updated!.FullName);
        Assert.Equal("555-0199", updated.Phone);
    }

    [Fact]
    public async Task GetById_UnknownSpecialist_Returns404NotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/specialists/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400BadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/specialists", new { fullName = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/specialists")]
    [InlineData("GET", "/api/v1/specialists")]
    [InlineData("GET", "/api/v1/specialists/some-id")]
    [InlineData("PUT", "/api/v1/specialists/some-id")]
    public async Task Endpoints_NoAuthorizationHeader_Return401Unauthorized(string method, string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT")
        {
            request.Content = JsonContent.Create(new { });
        }

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_TenantACannotReadTenantBsSpecialist()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/specialists", NewCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<SpecialistDto>();

        var response = await clientB.GetAsync($"/api/v1/specialists/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_TenantACannotUpdateTenantBsSpecialist()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/specialists", NewCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<SpecialistDto>();

        var response = await clientB.PutAsJsonAsync(
            $"/api/v1/specialists/{created!.Id}",
            new UpdateSpecialistRequest("Hijacked Name", "000-0000", null, null, "Active"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_TenantAsSpecialistListNeverIncludesTenantBsSpecialists()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponseB = await clientB.PostAsJsonAsync("/api/v1/specialists", NewCreateRequest());
        var createdByB = await createResponseB.Content.ReadFromJsonAsync<SpecialistDto>();

        var listAsA = await clientA.GetAsync("/api/v1/specialists");
        var specialistsVisibleToA = await listAsA.Content.ReadFromJsonAsync<List<SpecialistDto>>();

        Assert.DoesNotContain(specialistsVisibleToA!, specialist => specialist.Id == createdByB!.Id);
    }
}
