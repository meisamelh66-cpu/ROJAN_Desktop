using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Infrastructure.Salons;

/// <summary>
/// Owner App Booking Integration: the real, backend-connected
/// <see cref="ISalonContextService"/>. Calls
/// <c>GET /api/v1/users/me/salon-access</c> once and caches the result for
/// this instance's lifetime (registered as a DI singleton, same lifetime as
/// <c>BackendBookingRepository</c>) - which salon an owner manages does not
/// change mid-session, so there is no reason to re-fetch on every booking
/// call. <see cref="_resolveLock"/> guards the lazy first resolution
/// against two callers racing on startup (e.g. the Bookings page and a
/// future Customers/Calendar page both loading at once) - only one of them
/// actually calls the backend.
///
/// PASS D6 (Active Salon Context Correctness): previously, "if the owner
/// manages more than one salon, the first one the backend returns is used" -
/// a real correctness risk for a real multi-salon account (every
/// salon-scoped module silently operated on whichever salon happened to
/// sort first), not merely a missing switcher UI. Replaced with a real,
/// explicit active-salon context: exactly one candidate auto-selects
/// (unchanged observable behavior for the common case); two or more
/// candidates resolve to <see langword="null"/> until <see cref="SelectSalonAsync"/>
/// is called with an explicit, validated choice - <c>Shell.App.OnStartup</c>
/// is the one caller, via a real selection window shown before
/// <c>MainWindow</c>, mirroring the existing Login gate's own shape. The
/// selection is persisted (a plain, non-sensitive id - same plaintext
/// settings-file convention <see cref="Infrastructure.Api.ApiEnvironmentService"/>
/// already uses, not <c>ISecureStorageService</c>, which this app reserves
/// for actual secrets) and re-validated against the real, freshly-resolved
/// candidate list on every app start - a persisted id that no longer
/// matches a real candidate (the account lost access, or is a different
/// account's leftover local data) is discarded, never trusted blindly.
///
/// Phase 1 Context Source Alignment: previously called
/// <c>GET /api/v1/salons/mine</c> (ownership only) and fell back to
/// <see cref="IAcceptedMembershipStore"/> for the staff case, since that
/// endpoint carried no membership data at all. <c>/me/salon-access</c>
/// carries the caller's owned salons *and* active staff memberships in one
/// response, so <see cref="ResolveAsync"/> now checks owned salons, then
/// the backend's own membership list, before falling back to
/// <see cref="IAcceptedMembershipStore"/> - which remains as the last
/// resort for a membership the backend response doesn't (yet) reflect.
/// Specialist links are present in the response but not resolved into a
/// <see cref="SalonContext"/> here - no existing code path resolved a
/// specialist before this change either, and adding one is a new
/// capability, not a source swap (deferred to a future phase - see
/// <c>ROJAN_Phase1_Context_Source_Alignment_Plan_v1.md</c>). Both
/// <see cref="GetSalonIdAsync"/> and <see cref="GetCurrentContextAsync"/>
/// share this one resolution method and its one cache, so they can never
/// disagree with each other or make separate backend calls.
/// </summary>
public sealed class BackendSalonContextService : ISalonContextService, IDisposable
{
    private const string SalonAccessPath = "/api/v1/users/me/salon-access";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IApiClient _apiClient;
    private readonly IAcceptedMembershipStore _acceptedMembershipStore;
    private readonly string _settingsFilePath;
    private readonly SemaphoreSlim _resolveLock = new(1, 1);
    private IReadOnlyList<SalonContext> _candidates = [];
    private SalonContext? _activeSalon;
    private bool _hasResolved;

    public BackendSalonContextService(IApiClient apiClient, IAcceptedMembershipStore acceptedMembershipStore)
        : this(apiClient, acceptedMembershipStore, DefaultSettingsFilePath())
    {
    }

    /// <summary>Test-only seam (see <c>Rojan.Desktop.Infrastructure.csproj</c>'s <c>InternalsVisibleTo</c>) - lets tests point persistence at a throwaway path instead of the real <c>%LocalAppData%</c>, same pattern <see cref="Api.ApiEnvironmentService"/> already establishes.</summary>
    internal BackendSalonContextService(IApiClient apiClient, IAcceptedMembershipStore acceptedMembershipStore, string settingsFilePath)
    {
        _apiClient = apiClient;
        _acceptedMembershipStore = acceptedMembershipStore;
        _settingsFilePath = settingsFilePath;
    }

    public async Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default)
    {
        await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return _activeSalon?.SalonId;
    }

    public async Task<SalonContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return _activeSalon;
    }

    /// <summary>PASS D6: the real candidate list (never silently collapsed to one) plus whichever one, if any, is already the active context - <c>Shell.App.OnStartup</c>'s one real caller, to decide whether a selection window is needed at all.</summary>
    public async Task<SalonAccessSummary> GetAccessSummaryAsync(CancellationToken cancellationToken = default)
    {
        await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return new SalonAccessSummary(_candidates, _activeSalon?.SalonId);
    }

    /// <summary>
    /// PASS D6: the one and only way the active salon changes after the automatic single-candidate
    /// case. Validated against the real, already-resolved candidate list - an id that isn't one of
    /// them (a stale/tampered value, never trusted) leaves the active context unchanged and returns
    /// <see langword="false"/>. A real match becomes the active context immediately (every other
    /// <see cref="ISalonContextService"/> consumer sees it on their very next call, no cache
    /// invalidation needed since this method owns the same cache <see cref="ResolveAsync"/> does)
    /// and is persisted for the next app start.
    /// </summary>
    public async Task<bool> SelectSalonAsync(string salonId, CancellationToken cancellationToken = default)
    {
        await ResolveAsync(cancellationToken).ConfigureAwait(false);

        var match = _candidates.FirstOrDefault(candidate => candidate.SalonId == salonId);
        if (match is null)
        {
            return false;
        }

        _activeSalon = match;
        PersistActiveSalonId(salonId);
        return true;
    }

    /// <summary>Phase 1.2: resets the cache so the next resolution re-runs from the backend/local store - see the interface's own doc comment for why this exists.</summary>
    public void Invalidate()
    {
        _hasResolved = false;
        _candidates = [];
        _activeSalon = null;
    }

    private async Task ResolveAsync(CancellationToken cancellationToken)
    {
        if (_hasResolved)
        {
            return;
        }

        await _resolveLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_hasResolved)
            {
                return;
            }

            var response = await _apiClient.GetAsync<SalonAccessResponse>(SalonAccessPath, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to resolve the signed-in user's salon (status {response.StatusCode}): {response.ErrorMessage}");
            }

            _candidates = await BuildCandidatesAsync(response.Data, cancellationToken).ConfigureAwait(false);

            // PASS D6: exactly one real candidate auto-selects (Rule 1, unchanged observable behavior
            // for the common single-salon case). Two or more never silently pick the first one
            // (Rule 8) - only a persisted, still-valid prior selection resolves automatically; anything
            // else leaves _activeSalon null until SelectSalonAsync is called with an explicit choice.
            _activeSalon = _candidates.Count switch
            {
                0 => null,
                1 => _candidates[0],
                _ => ResolvePersistedSelection(),
            };

            _hasResolved = true;
        }
        finally
        {
            _resolveLock.Release();
        }
    }

    private async Task<IReadOnlyList<SalonContext>> BuildCandidatesAsync(SalonAccessResponse data, CancellationToken cancellationToken)
    {
        if (data.OwnedSalons.Count > 0)
        {
            return data.OwnedSalons
                .Select(owned => new SalonContext(owned.SalonId, owned.SalonName, IsOwner: true, MembershipRole: null, owned.Permissions.ToHashSet()))
                .ToList();
        }

        if (data.Memberships.Count > 0)
        {
            return data.Memberships
                .Select(membership => new SalonContext(membership.SalonId, membership.SalonName, IsOwner: false, membership.Role, membership.Permissions.ToHashSet()))
                .ToList();
        }

        var accepted = await _acceptedMembershipStore.GetAsync(cancellationToken).ConfigureAwait(false);
        // The local accepted-invite fallback has never carried permissions (it predates
        // /me/salon-access entirely) - empty, not null, so IEnterpriseContext.BackendPermissions
        // always has a set to query rather than a nullable one every consumer would need to guard.
        return accepted is null
            ? []
            : [new SalonContext(accepted.SalonId, accepted.SalonName, IsOwner: false, accepted.Role, new HashSet<string>())];
    }

    /// <summary>PASS D6 (Rule 4): a persisted selection is trusted only if it still matches a real, freshly-resolved candidate - an account that lost access to that salon (or another account's leftover local data) gets the persisted id cleared and falls through to "no active salon yet," never a silently-wrong one.</summary>
    private SalonContext? ResolvePersistedSelection()
    {
        var persistedSalonId = ReadPersistedSalonId();
        if (persistedSalonId is null)
        {
            return null;
        }

        var match = _candidates.FirstOrDefault(candidate => candidate.SalonId == persistedSalonId);
        if (match is null)
        {
            ClearPersistedSalonId();
            return null;
        }

        return match;
    }

    private string? ReadPersistedSalonId()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<ActiveSalonSettingsFile>(json, SerializerOptions)?.SalonId;
        }
#pragma warning disable CA1031 // A corrupt settings file must fall back to "no persisted selection," not crash resolution.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void PersistActiveSalonId(string salonId)
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new ActiveSalonSettingsFile { SalonId = salonId }, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private void ClearPersistedSalonId()
    {
        if (File.Exists(_settingsFilePath))
        {
            File.Delete(_settingsFilePath);
        }
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "salons", "active-salon.json");

    public void Dispose() => _resolveLock.Dispose();

    private sealed class ActiveSalonSettingsFile
    {
        public string SalonId { get; set; } = string.Empty;
    }
}
