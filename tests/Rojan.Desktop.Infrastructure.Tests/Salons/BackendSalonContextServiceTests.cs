using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Infrastructure.Salons;

namespace Rojan.Desktop.Infrastructure.Tests.Salons;

/// <summary>Exercises <see cref="BackendSalonContextService"/> - single-salon resolution, the documented multi-salon "first one wins" behavior, the zero-salon case, caching (one call resolves, every later call reuses it), and failure propagation.</summary>
public sealed class BackendSalonContextServiceTests
{
    [Fact]
    public async Task GetSalonIdAsync_OneSalon_ReturnsItsId()
    {
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1")]));

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_MultipleSalons_ReturnsTheFirstOne()
    {
        // Documented Phase 1 limitation - no salon-switcher UI yet, see this class's own doc comment.
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1"), Salon("salon-2")]));

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NoSalons_ReturnsNull()
    {
        using var service = new BackendSalonContextService(new StubApiClient([]));

        var salonId = await service.GetSalonIdAsync();

        Assert.Null(salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_CalledTwice_OnlyCallsTheBackendOnce()
    {
        var apiClient = new StubApiClient([Salon("salon-1")]);
        using var service = new BackendSalonContextService(apiClient);

        await service.GetSalonIdAsync();
        await service.GetSalonIdAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_ApiCallFails_ThrowsApiException()
    {
        using var service = new BackendSalonContextService(new StubApiClient(failureStatusCode: 500, failureMessage: "Internal error"));

        await Assert.ThrowsAsync<ApiException>(() => service.GetSalonIdAsync());
    }

    // Phase 1.2 Owner App Create Salon Flow: Invalidate() - see ISalonContextService's own doc comment for why this exists.

    [Fact]
    public async Task Invalidate_ThenGetSalonIdAsync_ReResolvesFromTheBackend()
    {
        var apiClient = new StubApiClient([]);
        using var service = new BackendSalonContextService(apiClient);
        Assert.Null(await service.GetSalonIdAsync());

        // Simulates the owner creating a salon between the first (cached, null) resolution and now.
        apiClient.Salons = [Salon("salon-1")];
        service.Invalidate();

        Assert.Equal("salon-1", await service.GetSalonIdAsync());
    }

    [Fact]
    public async Task Invalidate_ThenGetSalonIdAsync_CallsTheBackendAgain()
    {
        var apiClient = new StubApiClient([Salon("salon-1")]);
        using var service = new BackendSalonContextService(apiClient);
        await service.GetSalonIdAsync();
        await service.GetSalonIdAsync();
        Assert.Equal(1, apiClient.CallCount);

        service.Invalidate();
        await service.GetSalonIdAsync();

        Assert.Equal(2, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_WithoutInvalidate_NeverSeesASalonCreatedAfterTheFirstResolution()
    {
        // The known caching limitation Invalidate() exists to work around - confirms it's real.
        var apiClient = new StubApiClient([]);
        using var service = new BackendSalonContextService(apiClient);
        Assert.Null(await service.GetSalonIdAsync());

        apiClient.Salons = [Salon("salon-1")];

        Assert.Null(await service.GetSalonIdAsync());
    }

    private static SalonResponse Salon(string id) => new(id, "owner-1", "Test Salon", null, "0912", null, "Address", true);

    private sealed class StubApiClient : IApiClient
    {
        private readonly int? _failureStatusCode;
        private readonly string? _failureMessage;

        public int CallCount { get; private set; }

        /// <summary>Settable (not just constructor-supplied) so a test can simulate a salon appearing between two resolutions - see the Invalidate() tests above.</summary>
        public List<SalonResponse>? Salons { get; set; }

        public StubApiClient(List<SalonResponse> salons)
        {
            Salons = salons;
        }

        public StubApiClient(int failureStatusCode, string failureMessage)
        {
            _failureStatusCode = failureStatusCode;
            _failureMessage = failureMessage;
        }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (Salons is not null)
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)(object)Salons, 200));
            }

            return Task.FromResult(ApiResponseFactory.Failure<TResponse>(_failureStatusCode, _failureMessage!));
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never posts.");

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never patches.");
    }
}
