using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Rojan.Server.Api.Tests.Authentication;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Application.Customers;

namespace Rojan.Server.Api.Tests.Customers;

/// <summary>Exercises <c>CustomersController</c> end-to-end - the "API: authorized CRUD flow / unauthorized request rejected / Tenant A cannot access Tenant B customer" requirements this commit's own task list calls out explicitly.</summary>
public sealed class CustomersEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public CustomersEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterOrganizationOwnerRequest NewRegisterRequest() =>
        new("Rojan Salon", $"owner-{Guid.NewGuid():N}@rojan.example", "SuperSecret1", "Noah Bennett");

    private static CreateCustomerRequest NewCreateRequest() =>
        new("Noah Bennett", "555-0100", "noah@rojan.example", null);

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

        var createResponse = await client.PostAsJsonAsync("/api/v1/customers", NewCreateRequest());
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.NotNull(created);
        Assert.Equal("Noah Bennett", created!.Name);

        var getResponse = await client.GetAsync($"/api/v1/customers/{created.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetched = await getResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal(created.Id, fetched!.Id);

        var listResponse = await client.GetAsync("/api/v1/customers");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var list = await listResponse.Content.ReadFromJsonAsync<List<CustomerDto>>();
        Assert.Contains(list!, customer => customer.Id == created.Id);

        var updateResponse = await client.PutAsJsonAsync(
            $"/api/v1/customers/{created.Id}",
            new UpdateCustomerRequest("Noah Bennett-Chen", "555-0199", "noah.updated@rojan.example", null, "Active"));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updated = await updateResponse.Content.ReadFromJsonAsync<CustomerDto>();
        Assert.Equal("Noah Bennett-Chen", updated!.Name);
        Assert.Equal("555-0199", updated.Phone);
    }

    [Fact]
    public async Task GetById_UnknownCustomer_Returns404NotFound()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.GetAsync($"/api/v1/customers/{Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_MissingRequiredFields_Returns400BadRequest()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/api/v1/customers", new { name = "", phone = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/customers")]
    [InlineData("GET", "/api/v1/customers")]
    [InlineData("GET", "/api/v1/customers/some-id")]
    [InlineData("PUT", "/api/v1/customers/some-id")]
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
    public async Task TenantIsolation_TenantACannotReadTenantBsCustomer()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/customers", NewCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var response = await clientB.GetAsync($"/api/v1/customers/{created!.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_TenantACannotUpdateTenantBsCustomer()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponse = await clientA.PostAsJsonAsync("/api/v1/customers", NewCreateRequest());
        var created = await createResponse.Content.ReadFromJsonAsync<CustomerDto>();

        var response = await clientB.PutAsJsonAsync(
            $"/api/v1/customers/{created!.Id}",
            new UpdateCustomerRequest("Hijacked Name", "000-0000", null, null, "Active"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task TenantIsolation_TenantAsCustomerListNeverIncludesTenantBsCustomers()
    {
        var clientA = await CreateAuthenticatedClientAsync();
        var clientB = await CreateAuthenticatedClientAsync();
        var createResponseB = await clientB.PostAsJsonAsync("/api/v1/customers", NewCreateRequest());
        var createdByB = await createResponseB.Content.ReadFromJsonAsync<CustomerDto>();

        var listAsA = await clientA.GetAsync("/api/v1/customers");
        var customersVisibleToA = await listAsA.Content.ReadFromJsonAsync<List<CustomerDto>>();

        Assert.DoesNotContain(customersVisibleToA!, customer => customer.Id == createdByB!.Id);
    }
}
