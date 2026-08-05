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

    private static BackendServiceRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

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

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendServiceRepository never patches.");
    }
}
