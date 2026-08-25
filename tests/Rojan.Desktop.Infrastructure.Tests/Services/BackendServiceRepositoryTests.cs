using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Services;
using Rojan.Desktop.Infrastructure.Services;

namespace Rojan.Desktop.Infrastructure.Tests.Services;

/// <summary>
/// Exercises <see cref="BackendServiceRepository"/> - the category/service
/// fetch-and-flatten shape, <see cref="ServiceCategory.Other"/> fallback
/// mapping (case-insensitive, alternate spelling), status mapping (no
/// <see cref="ServiceStatus.Seasonal"/> equivalent), the always-empty
/// specialist-assignment read, and why the two assignment writes always
/// throw. Only the HTTP transport (<see cref="IApiClient"/>) is faked -
/// same "exercise the real workflow" convention as
/// <c>BackendBookingRepositoryTests</c>.
/// </summary>
public sealed class BackendServiceRepositoryTests
{
    private const string SalonId = "salon-1";

    [Fact]
    public async Task GetServicesAsync_KnownCategoryName_MapsToTheMatchingEnumValueAndCarriesTheRealName()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Haircut", "Classic cut", 30, 1_200_000m, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var services = await repository.GetServicesAsync();

        var service = Assert.Single(services);
        Assert.Equal(ServiceCategory.Hair, service.Category);
        Assert.Equal("Hair", service.CategoryName);
        Assert.Equal("cat-1", service.CategoryId);
        Assert.Equal(ServiceStatus.Active, service.Status);
        Assert.Equal("1,200,000 تومان", service.Price);
        Assert.Equal(30, service.DurationMinutes);
        Assert.Equal("Classic cut", service.Description);
    }

    [Theory]
    [InlineData("HAIR", ServiceCategory.Hair)]
    [InlineData("color", ServiceCategory.Colour)]
    [InlineData("  Spa  ", ServiceCategory.Spa)]
    public async Task GetServicesAsync_CategoryNameMatching_IsCaseInsensitiveAndTrimmed(string categoryName, ServiceCategory expected)
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, categoryName, null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Service", null, 30, 100_000m, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var service = Assert.Single(await repository.GetServicesAsync());

        Assert.Equal(expected, service.Category);
    }

    [Fact]
    public async Task GetServicesAsync_UnrecognizedCategoryName_FallsBackToOtherWithoutLosingTheRealName()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] =
            new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Barbering", null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Beard Trim", null, 15, 80_000m, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var service = Assert.Single(await repository.GetServicesAsync());

        Assert.Equal(ServiceCategory.Other, service.Category);
        Assert.Equal("Barbering", service.CategoryName);
    }

    [Fact]
    public async Task GetServicesAsync_InactiveService_MapsToDiscontinued_NoSeasonalEquivalentExists()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Old Service", null, 30, 100_000m, false) };

        var repository = CreateRepository(apiClient, SalonId);

        var service = Assert.Single(await repository.GetServicesAsync());

        Assert.Equal(ServiceStatus.Discontinued, service.Status);
    }

    [Fact]
    public async Task GetServicesAsync_FlattensAcrossMultipleCategories()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>
        {
            new("cat-1", SalonId, "Hair", null, true),
            new("cat-2", SalonId, "Nails", null, true),
        };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Haircut", null, 30, 100_000m, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-2/services"] =
            new List<ServiceResponse> { new("service-2", SalonId, "cat-2", "Manicure", null, 45, 150_000m, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var services = await repository.GetServicesAsync();

        Assert.Equal(2, services.Count);
        Assert.Contains(services, s => s.Id == "service-1" && s.Category == ServiceCategory.Hair);
        Assert.Contains(services, s => s.Id == "service-2" && s.Category == ServiceCategory.Nails);
    }

    [Fact]
    public async Task GetServicesAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetServicesAsync());
    }

    [Fact]
    public async Task GetServicesAsync_CategoriesFetchFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/categories"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetServicesAsync());
    }

    [Fact]
    public async Task GetServicesAsync_ServicesForCategoryFetchFails_ThrowsApiException_UnlikeTheBestEffortBookingLookup()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetServicesAsync());
    }

    [Fact]
    public async Task GetServiceByIdAsync_ExistingService_ReturnsIt()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "Haircut", null, 30, 100_000m, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var service = await repository.GetServiceByIdAsync("service-1");

        Assert.NotNull(service);
        Assert.Equal("service-1", service!.Id);
    }

    [Fact]
    public async Task GetServiceByIdAsync_MissingService_ReturnsNull()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();

        var repository = CreateRepository(apiClient, SalonId);

        var service = await repository.GetServiceByIdAsync("missing");

        Assert.Null(service);
    }

    [Fact]
    public async Task GetAssignedSpecialistsAsync_AlwaysReturnsEmpty()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        var assignments = await repository.GetAssignedSpecialistsAsync("service-1");

        Assert.Empty(assignments);
    }

    [Fact]
    public async Task AssignSpecialistAsync_AlwaysThrowsNotSupportedException()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);
        var assignment = new SpecialistService("id", "service-1", "specialist-1", "Jamie");

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.AssignSpecialistAsync(assignment));
    }

    [Fact]
    public async Task UnassignSpecialistAsync_AlwaysThrowsNotSupportedException()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.UnassignSpecialistAsync("service-1", "assignment-1"));
    }

    [Fact]
    public async Task CreateServiceAsync_ValidService_PostsToCategoryRouteAndReturnsMappedResult()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.PostResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new ServiceResponse("service-new", SalonId, "cat-1", "Blow-dry", "A quick blow-dry.", 30, 350000m, true);

        var repository = CreateRepository(apiClient, SalonId);
        var toCreate = new Service("ignored-id", "Blow-dry", ServiceCategory.Other, ServiceStatus.Active, 30, "350000", "A quick blow-dry.", CategoryId: "cat-1");

        var created = await repository.CreateServiceAsync(toCreate);

        Assert.Equal("service-new", created.Id);
        Assert.Equal("cat-1", created.CategoryId);
        Assert.Equal("Hair", created.CategoryName);
        var request = Assert.IsType<Application.Api.Contracts.CreateServiceRequest>(
            apiClient.PostRequests[$"/api/v1/salons/{SalonId}/categories/cat-1/services"]);
        Assert.Equal("Blow-dry", request.Name);
        Assert.Equal(350000m, request.Price);
        Assert.Equal(30, request.DurationMinutes);
    }

    [Fact]
    public async Task CreateServiceAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.PostFailures[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] = (400, "Invalid price");

        var repository = CreateRepository(apiClient, SalonId);
        var toCreate = new Service("id", "Name", ServiceCategory.Other, ServiceStatus.Active, 30, "0", "", CategoryId: "cat-1");

        await Assert.ThrowsAsync<ApiException>(() => repository.CreateServiceAsync(toCreate));
    }

    [Fact]
    public async Task UpdateServiceAsync_ValidService_PutsToServiceRouteAndReturnsMappedResult()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.PutResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1"] =
            new ServiceResponse("service-1", SalonId, "cat-1", "Haircut (updated)", null, 45, 500000m, true);

        var repository = CreateRepository(apiClient, SalonId);
        var toUpdate = new Service("service-1", "Haircut (updated)", ServiceCategory.Other, ServiceStatus.Active, 45, "500000", "", CategoryId: "cat-1");

        var updated = await repository.UpdateServiceAsync(toUpdate);

        Assert.Equal("Haircut (updated)", updated.Name);
        Assert.Equal(45, updated.DurationMinutes);
        var request = Assert.IsType<Application.Api.Contracts.UpdateServiceRequest>(
            apiClient.PutRequests[$"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1"]);
        Assert.Equal(500000m, request.Price);
    }

    [Fact]
    public async Task UpdateServiceAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.PutFailures[$"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);
        var toUpdate = new Service("service-1", "Name", ServiceCategory.Other, ServiceStatus.Active, 30, "0", "", CategoryId: "cat-1");

        await Assert.ThrowsAsync<ApiException>(() => repository.UpdateServiceAsync(toUpdate));
    }

    [Fact]
    public async Task DeactivateServiceAsync_ValidIds_DeletesTheServiceRoute()
    {
        var apiClient = new StubApiClient();
        apiClient.DeleteSuccessPaths.Add($"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1");

        var repository = CreateRepository(apiClient, SalonId);

        await repository.DeactivateServiceAsync("cat-1", "service-1");

        Assert.Contains($"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1", apiClient.DeleteCalls);
    }

    [Fact]
    public async Task DeactivateServiceAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.DeleteFailures[$"/api/v1/salons/{SalonId}/categories/cat-1/services/service-1"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.DeactivateServiceAsync("cat-1", "service-1"));
    }

    [Fact]
    public async Task GetCategoriesAsync_ReturnsEveryRealCategory()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>
        {
            new("cat-1", SalonId, "Hair", null, true),
            new("cat-2", SalonId, "Nails", null, true),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var categories = await repository.GetCategoriesAsync();

        Assert.Equal(2, categories.Count);
        Assert.Contains(categories, category => category.Id == "cat-1" && category.Name == "Hair");
        Assert.Contains(categories, category => category.Id == "cat-2" && category.Name == "Nails");
    }

    private static BackendServiceRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    /// <summary>Service Catalog Management: Post/Put/Delete are now real (Create/Update/Deactivate), unlike every earlier pass of this stub - request bodies/paths are recorded for assertions, matching the same GetResponses/GetFailures configurable-by-path shape already established for GetAsync.</summary>
    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Dictionary<string, object> PostResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> PostFailures { get; } = [];

        public Dictionary<string, object> PostRequests { get; } = [];

        public Dictionary<string, object> PutResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> PutFailures { get; } = [];

        public Dictionary<string, object> PutRequests { get; } = [];

        public List<string> DeleteSuccessPaths { get; } = [];

        public Dictionary<string, (int? Status, string Message)> DeleteFailures { get; } = [];

        public List<string> DeleteCalls { get; } = [];

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            if (GetFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (GetResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected GET '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            PostRequests[path] = body!;

            if (PostFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (PostResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 201));
            }

            throw new InvalidOperationException($"Unexpected POST '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            PutRequests[path] = body!;

            if (PutFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (PutResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected PUT '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(path);

            if (DeleteFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (DeleteSuccessPaths.Contains(path))
            {
                return Task.FromResult(ApiResponseFactory.Success(default(TResponse)!, 204));
            }

            throw new InvalidOperationException($"Unexpected DELETE '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never fetches raw bytes.");
    }
}
