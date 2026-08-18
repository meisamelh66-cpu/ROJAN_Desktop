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
/// propagation, and the accepted-invite fallback for a caller who owns no
/// salon and has no backend membership either, plus
/// <see cref="ISalonContextService.GetCurrentContextAsync"/> sharing the
/// exact same cached resolution <see cref="ISalonContextService.GetSalonIdAsync"/>
/// uses.
///
/// Phase 1 Context Source Alignment: rewritten against
/// <c>GET /me/salon-access</c>'s <see cref="SalonAccessResponse"/> shape
/// (owned salons + backend memberships + specialist links) in place of the
/// old <c>GET /salons/mine</c> owned-salons-only shape. The resolution
/// priority under test is now owned salons, then the backend's own
/// membership list, then the local <see cref="IAcceptedMembershipStore"/>
/// fallback - see this phase's own plan document for why the local store
/// is kept rather than removed.
/// </summary>
public sealed class BackendSalonContextServiceTests
{
    [Fact]
    public async Task GetSalonIdAsync_OneOwnedSalon_ReturnsItsId()
    {
        using var service = new BackendSalonContextService(new StubApiClient(owned: [Owned("salon-1")]), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_MultipleOwnedSalons_ReturnsTheFirstOne()
    {
        // Documented Phase 1 limitation - no salon-switcher UI yet, see this class's own doc comment.
        using var service = new BackendSalonContextService(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")]), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NothingOwnedOrMemberOfAndNoAcceptedInvite_ReturnsNull()
    {
        using var service = new BackendSalonContextService(new StubApiClient(), new StubAcceptedMembershipStore());

        var salonId = await service.GetSalonIdAsync();

        Assert.Null(salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NoOwnedSalon_FallsBackToAcceptedInviteMembership()
    {
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient(), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-9", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_OwnsASalon_NeverConsultsAcceptedInviteMembership()
    {
        // Ownership wins over any locally-persisted membership - a real owner is never treated as a mere member of their own salon.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Some Other Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient(owned: [Owned("salon-1")]), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetCurrentContextAsync_Owner_ReturnsIsOwnerTrueAndNoMembershipRole()
    {
        using var service = new BackendSalonContextService(new StubApiClient(owned: [Owned("salon-1", name: "Glow Salon")]), new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-1", context!.SalonId);
        Assert.Equal("Glow Salon", context.SalonName);
        Assert.True(context.IsOwner);
        Assert.Null(context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_BackendMembership_ResolvesDirectlyFromTheBackendWithoutConsultingTheLocalStore()
    {
        // Phase 1 Context Source Alignment: /me/salon-access carries the caller's own active memberships now,
        // so this no longer needs the local accepted-invite store at all - the stub store below is left with
        // no membership set (default null) to prove it, and would fail the "no owned salon and no accepted
        // invite" null-result assertion if it were ever consulted instead of the backend list.
        using var service = new BackendSalonContextService(
            new StubApiClient(memberships: [Membership("salon-9", "Glow Salon", "MANAGER")]),
            new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-9", context!.SalonId);
        Assert.Equal("Glow Salon", context.SalonName);
        Assert.False(context.IsOwner);
        Assert.Equal("MANAGER", context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_Owner_CarriesPermissionsFromTheBackendResponse()
    {
        // Phase 3A Permission Consumer Adapter: SalonContext.Permissions must carry the backend's
        // response through unchanged - opaque strings, no interpretation, no filtering.
        var ownerPermissions = new[] { "MANAGE_SALON", "MANAGE_MEMBERSHIP", "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL", "MANAGE_SCHEDULE_OWN", "VIEW_CRM", "MANAGE_CRM", "MANAGE_BOOKINGS", "MANAGE_OWN_BOOKINGS" };
        using var service = new BackendSalonContextService(new StubApiClient(owned: [Owned("salon-1", permissions: ownerPermissions)]), new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal(ownerPermissions.ToHashSet(), context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_BackendMembership_CarriesPermissionsFromTheBackendResponse()
    {
        var managerPermissions = new[] { "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL", "VIEW_CRM", "MANAGE_CRM", "MANAGE_BOOKINGS" };
        using var service = new BackendSalonContextService(
            new StubApiClient(memberships: [Membership("salon-9", "Glow Salon", "MANAGER", permissions: managerPermissions)]),
            new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal(managerPermissions.ToHashSet(), context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_AcceptedInviteFallback_HasEmptyPermissions()
    {
        // The local accepted-invite fallback has never carried permissions - it predates
        // /me/salon-access entirely. Empty, not null: IEnterpriseContext.BackendPermissions
        // always has a set to query, never a nullable one every future consumer would need to guard.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient(), membershipStore);

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Empty(context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_OwnedAndMember_OwnershipWins()
    {
        using var service = new BackendSalonContextService(
            new StubApiClient(owned: [Owned("salon-1", name: "My Own Salon")], memberships: [Membership("salon-9", "Someone Else's Salon", "MANAGER")]),
            new StubAcceptedMembershipStore());

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-1", context!.SalonId);
        Assert.True(context.IsOwner);
    }

    [Fact]
    public async Task GetCurrentContextAsync_AcceptedReceptionInvite_ReturnsIsOwnerFalseAndTheBackendRole()
    {
        // No owned salon and no backend membership - only the local fallback has anything.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = new BackendSalonContextService(new StubApiClient(), membershipStore);

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-9", context!.SalonId);
        Assert.False(context.IsOwner);
        Assert.Equal("RECEPTIONIST", context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_NoOwnershipAndNoMembership_ReturnsNull()
    {
        using var service = new BackendSalonContextService(new StubApiClient(), new StubAcceptedMembershipStore());

        Assert.Null(await service.GetCurrentContextAsync());
    }

    [Fact]
    public async Task GetCurrentContextAsync_SpecialistLinkOnly_StillFallsThroughToLocalStore()
    {
        // Phase 1 Context Source Alignment: specialist links are present in the response but deliberately
        // not resolved into a SalonContext yet - a specialist-only response must behave exactly like an
        // empty one for this phase, falling through to whatever the local accepted-invite store has (here,
        // nothing), not silently picking up the specialist link.
        using var service = new BackendSalonContextService(
            new StubApiClient(specialistLinks: [Specialist("salon-5", "Specialist Salon")]),
            new StubAcceptedMembershipStore());

        Assert.Null(await service.GetCurrentContextAsync());
    }

    [Fact]
    public async Task GetSalonIdAsync_CalledTwice_OnlyCallsTheBackendOnce()
    {
        var apiClient = new StubApiClient(owned: [Owned("salon-1")]);
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());

        await service.GetSalonIdAsync();
        await service.GetSalonIdAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_AndGetCurrentContextAsync_ShareTheSameCache_OnlyOneBackendCallTotal()
    {
        var apiClient = new StubApiClient(owned: [Owned("salon-1")]);
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
        var apiClient = new StubApiClient();
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());
        Assert.Null(await service.GetSalonIdAsync());

        // Simulates the owner creating a salon between the first (cached, null) resolution and now.
        apiClient.Owned = [Owned("salon-1")];
        service.Invalidate();

        Assert.Equal("salon-1", await service.GetSalonIdAsync());
    }

    [Fact]
    public async Task Invalidate_ThenGetSalonIdAsync_CallsTheBackendAgain()
    {
        var apiClient = new StubApiClient(owned: [Owned("salon-1")]);
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
        var apiClient = new StubApiClient();
        using var service = new BackendSalonContextService(apiClient, new StubAcceptedMembershipStore());
        Assert.Null(await service.GetSalonIdAsync());

        apiClient.Owned = [Owned("salon-1")];

        Assert.Null(await service.GetSalonIdAsync());
    }

    private static OwnedSalonAccess Owned(string id, string name = "Test Salon", IReadOnlyList<string>? permissions = null) => new(id, name, Active: true, permissions ?? []);

    private static MembershipAccess Membership(string salonId, string name, string role, IReadOnlyList<string>? permissions = null) => new("membership-1", salonId, name, Active: true, role, permissions ?? []);

    private static SpecialistAccess Specialist(string salonId, string name) => new("specialist-1", salonId, name, Active: true, Permissions: []);

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
        private readonly bool _shouldFail;

        public int CallCount { get; private set; }

        /// <summary>Settable (not just constructor-supplied) so a test can simulate a salon appearing between two resolutions - see the Invalidate() tests above.</summary>
        public List<OwnedSalonAccess> Owned { get; set; }

        public List<MembershipAccess> Memberships { get; set; }

        public List<SpecialistAccess> SpecialistLinks { get; set; }

        public StubApiClient(List<OwnedSalonAccess>? owned = null, List<MembershipAccess>? memberships = null, List<SpecialistAccess>? specialistLinks = null)
        {
            Owned = owned ?? [];
            Memberships = memberships ?? [];
            SpecialistLinks = specialistLinks ?? [];
        }

        public StubApiClient(int failureStatusCode, string failureMessage)
        {
            _shouldFail = true;
            _failureStatusCode = failureStatusCode;
            _failureMessage = failureMessage;
            Owned = [];
            Memberships = [];
            SpecialistLinks = [];
        }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            CallCount++;
            if (_shouldFail)
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(_failureStatusCode, _failureMessage!));
            }

            var response = new SalonAccessResponse(Owned, Memberships, SpecialistLinks);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)(object)response, 200));
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
