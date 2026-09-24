using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Membership;
using Rojan.Desktop.Infrastructure.Salons;

namespace Rojan.Desktop.Infrastructure.Tests.Salons;

/// <summary>
/// Exercises <see cref="BackendSalonContextService"/> - single-salon
/// resolution, PASS D6's real multi-salon correctness fix (no more "first
/// one wins" - see the class's own doc comment), explicit selection and its
/// persistence across a simulated restart, a stale persisted selection
/// being safely discarded, caching (one call resolves, every later call
/// reuses it), failure propagation, and the accepted-invite fallback for a
/// caller who owns no salon and has no backend membership either, plus
/// <see cref="ISalonContextService.GetCurrentContextAsync"/> sharing the
/// exact same cached resolution <see cref="ISalonContextService.GetSalonIdAsync"/>
/// uses. Uses a temp settings file (never the real
/// %LocalAppData%\RojanDesktop\salons\active-salon.json) via the internal
/// path-overriding constructor - same shape
/// <c>Infrastructure.Tests.Api.ApiEnvironmentServiceTests</c> already
/// establishes.
/// </summary>
public sealed class BackendSalonContextServiceTests : IDisposable
{
    private readonly string _settingsFilePath;

    public BackendSalonContextServiceTests()
    {
        _settingsFilePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "active-salon.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private BackendSalonContextService CreateSut(IApiClient apiClient, IAcceptedMembershipStore? membershipStore = null) =>
        new(apiClient, membershipStore ?? new StubAcceptedMembershipStore(), _settingsFilePath);

    [Fact]
    public async Task GetSalonIdAsync_OneOwnedSalon_ReturnsItsId()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1")]));

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    /// <summary>
    /// PASS D6 (Active Salon Context Correctness): the real correctness fix - a multi-salon account
    /// with no explicit, validated selection yet must never silently operate on "the first one" (the
    /// pre-D6 behavior this exact test used to assert). Every salon-scoped repository already
    /// handles a null salonId safely (the same "no salon at all" path they already had), so this is
    /// a real, honest signal, not a new failure mode.
    /// </summary>
    [Fact]
    public async Task GetSalonIdAsync_MultipleOwnedSalonsAndNoValidatedSelection_NeverSilentlyPicksTheFirstOne()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")]));

        var salonId = await service.GetSalonIdAsync();

        Assert.Null(salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NothingOwnedOrMemberOfAndNoAcceptedInvite_ReturnsNull()
    {
        using var service = CreateSut(new StubApiClient());

        var salonId = await service.GetSalonIdAsync();

        Assert.Null(salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_NoOwnedSalon_FallsBackToAcceptedInviteMembership()
    {
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = CreateSut(new StubApiClient(), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-9", salonId);
    }

    [Fact]
    public async Task GetSalonIdAsync_OwnsASalon_NeverConsultsAcceptedInviteMembership()
    {
        // Ownership wins over any locally-persisted membership - a real owner is never treated as a mere member of their own salon.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Some Other Salon", "RECEPTIONIST") };
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1")]), membershipStore);

        var salonId = await service.GetSalonIdAsync();

        Assert.Equal("salon-1", salonId);
    }

    [Fact]
    public async Task GetCurrentContextAsync_Owner_ReturnsIsOwnerTrueAndNoMembershipRole()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1", name: "Glow Salon")]));

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
        using var service = CreateSut(new StubApiClient(memberships: [Membership("salon-9", "Glow Salon", "MANAGER")]));

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
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1", permissions: ownerPermissions)]));

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal(ownerPermissions.ToHashSet(), context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_BackendMembership_CarriesPermissionsFromTheBackendResponse()
    {
        var managerPermissions = new[] { "MANAGE_CATALOG", "MANAGE_STAFF", "MANAGE_SCHEDULE_ALL", "VIEW_CRM", "MANAGE_CRM", "MANAGE_BOOKINGS" };
        using var service = CreateSut(new StubApiClient(memberships: [Membership("salon-9", "Glow Salon", "MANAGER", permissions: managerPermissions)]));

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal(managerPermissions.ToHashSet(), context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_AcceptedInviteFallback_HasEmptyPermissions()
    {
        // The local accepted-invite fallback has never carried permissions - it predates
        // /me/salon-access entirely. Empty, not null: IEnterpriseContext.BackendPermissions
        // always has a set to query, never a nullable one every consumer would need to guard.
        var membershipStore = new StubAcceptedMembershipStore { Membership = new AcceptedMembership("salon-9", "Glow Salon", "RECEPTIONIST") };
        using var service = CreateSut(new StubApiClient(), membershipStore);

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Empty(context!.Permissions);
    }

    [Fact]
    public async Task GetCurrentContextAsync_OwnedAndMember_OwnershipWins()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1", name: "My Own Salon")], memberships: [Membership("salon-9", "Someone Else's Salon", "MANAGER")]));

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
        using var service = CreateSut(new StubApiClient(), membershipStore);

        var context = await service.GetCurrentContextAsync();

        Assert.NotNull(context);
        Assert.Equal("salon-9", context!.SalonId);
        Assert.False(context.IsOwner);
        Assert.Equal("RECEPTIONIST", context.MembershipRole);
    }

    [Fact]
    public async Task GetCurrentContextAsync_NoOwnershipAndNoMembership_ReturnsNull()
    {
        using var service = CreateSut(new StubApiClient());

        Assert.Null(await service.GetCurrentContextAsync());
    }

    [Fact]
    public async Task GetCurrentContextAsync_SpecialistLinkOnly_StillFallsThroughToLocalStore()
    {
        // Phase 1 Context Source Alignment: specialist links are present in the response but deliberately
        // not resolved into a SalonContext yet - a specialist-only response must behave exactly like an
        // empty one for this phase, falling through to whatever the local accepted-invite store has (here,
        // nothing), not silently picking up the specialist link.
        using var service = CreateSut(new StubApiClient(specialistLinks: [Specialist("salon-5", "Specialist Salon")]));

        Assert.Null(await service.GetCurrentContextAsync());
    }

    [Fact]
    public async Task GetSalonIdAsync_CalledTwice_OnlyCallsTheBackendOnce()
    {
        var apiClient = new StubApiClient(owned: [Owned("salon-1")]);
        using var service = CreateSut(apiClient);

        await service.GetSalonIdAsync();
        await service.GetSalonIdAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_AndGetCurrentContextAsync_ShareTheSameCache_OnlyOneBackendCallTotal()
    {
        var apiClient = new StubApiClient(owned: [Owned("salon-1")]);
        using var service = CreateSut(apiClient);

        await service.GetSalonIdAsync();
        await service.GetCurrentContextAsync();

        Assert.Equal(1, apiClient.CallCount);
    }

    [Fact]
    public async Task GetSalonIdAsync_ApiCallFails_ThrowsApiException()
    {
        using var service = CreateSut(new StubApiClient(failureStatusCode: 500, failureMessage: "Internal error"));

        await Assert.ThrowsAsync<ApiException>(() => service.GetSalonIdAsync());
    }

    // ---- PASS D6: GetAccessSummaryAsync ----

    [Fact]
    public async Task GetAccessSummaryAsync_OneOwnedSalon_CandidatesHasOneEntryAndActiveSalonIdIsSet()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1", name: "Glow Salon")]));

        var summary = await service.GetAccessSummaryAsync();

        Assert.Single(summary.Candidates);
        Assert.Equal("salon-1", summary.ActiveSalonId);
    }

    [Fact]
    public async Task GetAccessSummaryAsync_MultipleOwnedSalonsAndNoValidatedSelection_CandidatesHasBothButActiveSalonIdIsNull()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")]));

        var summary = await service.GetAccessSummaryAsync();

        Assert.Equal(2, summary.Candidates.Count);
        Assert.Null(summary.ActiveSalonId);
    }

    [Fact]
    public async Task GetAccessSummaryAsync_NoAccessAtAll_ReturnsEmptyCandidatesAndNullActiveSalonId()
    {
        using var service = CreateSut(new StubApiClient());

        var summary = await service.GetAccessSummaryAsync();

        Assert.Empty(summary.Candidates);
        Assert.Null(summary.ActiveSalonId);
    }

    // ---- PASS D6: SelectSalonAsync (explicit, validated selection) ----

    [Fact]
    public async Task SelectSalonAsync_RealCandidate_BecomesTheActiveContextForEveryConsumer()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2", name: "Second Salon")]));
        Assert.Null(await service.GetSalonIdAsync());

        var succeeded = await service.SelectSalonAsync("salon-2");

        Assert.True(succeeded);
        Assert.Equal("salon-2", await service.GetSalonIdAsync());
        Assert.Equal("Second Salon", (await service.GetCurrentContextAsync())!.SalonName);
        Assert.Equal("salon-2", (await service.GetAccessSummaryAsync()).ActiveSalonId);
    }

    [Fact]
    public async Task SelectSalonAsync_IdThatIsNotARealCandidate_ReturnsFalseAndLeavesActiveSalonUnchanged()
    {
        using var service = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")]));

        var succeeded = await service.SelectSalonAsync("salon-does-not-exist");

        Assert.False(succeeded);
        Assert.Null(await service.GetSalonIdAsync());
    }

    // ---- PASS D6: persistence across a simulated restart ----

    [Fact]
    public async Task SelectSalonAsync_ThenNewInstanceAgainstTheSameSettingsFile_RestoresTheSameActiveSalonWithoutAskingAgain()
    {
        var candidates = new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2", name: "Second Salon")]);
        using (var first = CreateSut(candidates))
        {
            await first.SelectSalonAsync("salon-2");
        }

        // A fresh instance against the same real backend data and the same persisted settings file -
        // simulates the app restarting, per PASS D6 Rule 4 ("restore it if valid").
        using var second = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2", name: "Second Salon")]));

        var summary = await second.GetAccessSummaryAsync();

        Assert.Equal("salon-2", summary.ActiveSalonId);
    }

    [Fact]
    public async Task PersistedSelection_SalonNoLongerInTheRealCandidateList_IsDiscardedNotTrustedBlindly()
    {
        using (var first = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")])))
        {
            await first.SelectSalonAsync("salon-2");
        }

        // Simulates the account losing access to salon-2 (or a different account's leftover local
        // data) between sessions - the real, freshly-resolved candidate list no longer contains it.
        using var second = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-3")]));

        var summary = await second.GetAccessSummaryAsync();

        Assert.Null(summary.ActiveSalonId);
        Assert.Equal(2, summary.Candidates.Count);
    }

    [Fact]
    public async Task PersistedSelection_SalonNoLongerAccessible_ClearsTheStaleFileSoANewSelectionPersistsCleanly()
    {
        using (var first = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-2")])))
        {
            await first.SelectSalonAsync("salon-2");
        }

        using (var second = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-3")])))
        {
            await second.GetAccessSummaryAsync(); // triggers the stale-selection cleanup
            await second.SelectSalonAsync("salon-3");
        }

        using var third = CreateSut(new StubApiClient(owned: [Owned("salon-1"), Owned("salon-3")]));
        var summary = await third.GetAccessSummaryAsync();

        Assert.Equal("salon-3", summary.ActiveSalonId);
    }

    [Fact]
    public async Task SelectSalonAsync_SingleCandidateAlreadyAutoSelected_PersistedSelectionStillRestoresCorrectlyNextRun()
    {
        // Rule 1 (auto-select) and Rule 4 (persistence) must compose correctly - an auto-selected
        // single salon is still a real, persisted selection, not merely an in-memory-only default.
        using (var first = CreateSut(new StubApiClient(owned: [Owned("salon-1")])))
        {
            Assert.Equal("salon-1", await first.GetSalonIdAsync());
        }

        using var second = CreateSut(new StubApiClient(owned: [Owned("salon-1")]));
        Assert.Equal("salon-1", await second.GetSalonIdAsync());
    }

    // ---- Phase 1.2 Owner App Create Salon Flow: Invalidate() - see ISalonContextService's own doc comment for why this exists. ----

    [Fact]
    public async Task Invalidate_ThenGetSalonIdAsync_ReResolvesFromTheBackend()
    {
        var apiClient = new StubApiClient();
        using var service = CreateSut(apiClient);
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
        using var service = CreateSut(apiClient);
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
        using var service = CreateSut(apiClient);
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

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonContextService never fetches raw bytes.");
    }
}
