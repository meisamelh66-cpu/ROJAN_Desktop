using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Membership;
using Rojan.Desktop.Infrastructure.Salons;

namespace Rojan.Desktop.Infrastructure.Tests.Salons;

/// <summary>
/// Exercises <see cref="BackendSalonContextService"/> - single-salon
/// resolution, the documented multi-salon "first one wins" behavior,
/// caching (one call resolves, every later call reuses it), failure
/// propagation, and (Reception Production Integration) the accepted-invite
/// fallback for a caller who owns no salon, plus <see cref="ISalonContextService.GetCurrentContextAsync"/>
/// sharing the exact same cached resolution <see cref="ISalonContextService.GetSalonIdAsync"/> uses.
/// </summary>
public sealed class BackendSalonContextServiceTests
{
    [Fact]
    public async Task GetSalonIdAsync_OneSalon_ReturnsItsId()
    {
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1")]), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_MultipleSalons_ReturnsTheFirstOne()
    {
        // Documented Phase 1 limitation - no salon-switcher UI yet, see this class's own doc comment.
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1"), Salon("salon-2")]), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NoOwnedSalonAndNoAcceptedInvite_ReturnsNull()
    {
        using var service = new BackendSalonContextService(new StubApiClient([]), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Null(salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NoOwnedSalon_FallsBackToAcceptedInviteMembership()
    {
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient([]), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-9", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_OwnsASalon_NeverConsultsAcceptedInviteMembership()
    {
        // Ownership wins over any locally-persisted membership - a real owner is never treated as a mere member of their own salon.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Some Other Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1")]), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetCurrentContextAsync_Owner_ReturnsIsOwnerTrueAndNoMembershipRole()
    {
        using var service = new BackendSalonContextService(new StubApiClient([Salon("salon-1", name: "Glow Salon")]), new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-1", context!.SalonId);
        Assert.Equal("Glow Salon", context.SalonName);
        Assert.True(context.IsOwner);
        Assert.Null(context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_AcceptedReceptionInvite_ReturnsIsOwnerFalseAndTheBackendRole()
    {
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient([]), membershipStore);

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-9", context!.SalonId);
        Assert.False(context.IsOwner);
        Assert.Equal("RECEPTIONIST", context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_NoOwnershipAndNoMembership_ReturnsNull()
    {
        using var service = new BackendSalonContextService(new StubApiClient([]), new StubAcceptedMembershipStore());

        Assert.Null(await service.GetCurrentContextAsync());
    }

    [Fact]
    public async Task GetSalonIdAsync_CalledTwice_OnlyCallsTheBackendOnce()
    {
        var apiClient = new StubApiClient([Salon("salon-1")]);
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());

        await service.GetSalonIdAsync();
        await service.GetSalonIdAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_AndGetCurrentContextAsync_ShareTheSameCache_OnlyOneBackendCallTotal()
    {
        var apiClient = new StubApiClient([Salon("salon-1")]);
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());

        await service.GetSalonIdAsync();
        await service.GetCurrentContextAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_ApiCallFails_ThrowsApiException()
    {
        using var service = new BackendSalonContextService(new StubApiClient(failureStatusCode: 500, failureMessage: "Internal error"), new StubAcceptedMembershipStore());

        await Assert.ThrowsAsync<ApiException>(() => service.GetSalonIdAsync());
    }

    // Phase 1.2 Owner App Create Salon Flow: Invalidate() - see ISalonContextService's own doc comment for why this exists.

    [Fact]
    public async Task Invalidate_ThenGetSalonIdAsync_ReResolvesFromTheBackend()
    {
        var apiClient = new StubApiClient([]);
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());
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
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());
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
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());
        Assert.Null(await service.GetSalonIdAsync());

        apiClient.Salons = [Salon("salon-1")];

        Assert.Null(await service.GetSalonIdAsync());
    }

    private static SalonResponse Salon(string id, string name = "Test Salon") => new(id, "owner-1", name, null, "0912", null, "Address", true);

    private sealed class StubAcceptedMembershipStore : IAcceptedMembershipStore
    {
        public AcceptedMembership? Membership { get; set; }

        public Task<AcceptedMembership?> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(Membership);

        public Task SaveAsync(AcceptedMembership membership, CancellationToken cancellationToken = default)
        {
            Membership = membership;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            Membership = null;
            return Task.CompletedTask;
        }
    }

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
