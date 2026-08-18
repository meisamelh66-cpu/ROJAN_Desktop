using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Shell.Organizations;

/// <summary>
/// Default <see cref="ICurrentSessionService"/> implementation. Persists
/// the selected organization/branch/role to its own
/// <c>%LocalAppData%\RojanDesktop\session.json</c> (separate from
/// Localization's <c>settings.json</c> and Theming's <c>theme.json</c> -
/// same "one concern, one file" shape) and restores it on
/// <see cref="InitializeAsync"/>. Defaults to the first seeded
/// organization's first branch as <see cref="WorkspaceRole.PlatformOwner"/>
/// when no settings file exists yet or the persisted ids no longer
/// resolve (e.g. seed data changed) - never leaves the session
/// unscoped.
///
/// Phase 22A: also implements <see cref="IEnterpriseContext"/> - the
/// Application layer's own (Presentation-independent) view of the same
/// scoping data, so every module's query/command service can depend on
/// current organization/branch/role without depending on
/// <see cref="ICurrentSessionService"/> (a Presentation-layer type
/// Application must never reference). One object, two interfaces, same
/// alias-registration pattern <c>App.xaml.cs</c> already uses for
/// <c>NavigationService</c>/<c>INavigationService</c>.
///
/// Reception Production Integration: <see cref="InitializeAsync"/> tries a
/// *real* resolution, via <see cref="ISalonContextService.GetCurrentContextAsync"/>
/// (owner or staff via <c>GET /me/salon-access</c>, or an accepted Salon Invite) -
/// only when that yields nothing does it fall back to the
/// <see cref="IOrganizationQueryService"/>/<c>session.json</c> path
/// documented above, entirely unchanged in its own internal behavior.
///
/// Phase 2B Context State Hardening: that fallback is no longer reached
/// silently. <see cref="_demoModeProvider"/> is checked first, before real
/// resolution is even attempted - only when it reports
/// <see langword="true"/> does <see cref="InitializeAsync"/> ever reach the
/// seeded-organization path, and the result is tagged
/// <see cref="DesktopContextState.DemoContext"/>, never conflated with a
/// real session. An empty real resolution with the flag off now resolves
/// to <see cref="DesktopContextState.NoBusinessContext"/> instead - no
/// organization, no fabricated privilege. <see cref="ContextState"/>
/// replaces the former <c>HasRealMembership</c> boolean this doc comment
/// used to describe here; <see cref="SwitchRoleAsync"/>/<see cref="SwitchBranchAsync"/>
/// now guard against it the same way, just against a richer signal -
/// closing the exact hole a real Reception session must not have (freely
/// switching to a role/org the backend never granted).
/// </summary>
public sealed class CurrentSessionService : ICurrentSessionService, IEnterpriseContext
{
    /// <summary>The Branch Switcher's "Recently used branches" cap - the five most recent, newest first.</summary>
    public const int MaxRecentBranches = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IOrganizationQueryService _organizationQueryService;
    private readonly ISalonContextService _salonContextService;
    private readonly ISalonSessionAdapter _salonSessionAdapter;
    private readonly IDemoModeProvider _demoModeProvider;
    private readonly string _settingsFilePath;
    private List<BranchDto> _availableBranches = [];
    private List<string> _recentBranchIds = [];
    private List<string> _favoriteBranchIds = [];
    private DesktopContextState _contextState = DesktopContextState.NoBusinessContext;

    public CurrentSessionService(IOrganizationQueryService organizationQueryService, ISalonContextService salonContextService, ISalonSessionAdapter salonSessionAdapter, IDemoModeProvider demoModeProvider)
        : this(organizationQueryService, salonContextService, salonSessionAdapter, demoModeProvider, DefaultSettingsFilePath())
    {
    }

    internal CurrentSessionService(IOrganizationQueryService organizationQueryService, ISalonContextService salonContextService, ISalonSessionAdapter salonSessionAdapter, IDemoModeProvider demoModeProvider, string settingsFilePath)
    {
        _organizationQueryService = organizationQueryService;
        _salonContextService = salonContextService;
        _salonSessionAdapter = salonSessionAdapter;
        _demoModeProvider = demoModeProvider;
        _settingsFilePath = settingsFilePath;
    }

    public OrganizationDto? CurrentOrganization { get; private set; }

    public BranchDto? CurrentBranch { get; private set; }

    /// <summary>
    /// Phase 2B Context State Hardening: the existing-enum least-privileged
    /// role (<see cref="WorkspaceRole.Support"/> - <c>DashboardView</c>,
    /// <c>CustomerRead</c>, <c>BookingRead</c> only, per <c>RolePermissions</c>,
    /// unchanged by this phase) is the default for
    /// <see cref="DesktopContextState.NoBusinessContext"/> - the safest
    /// value expressible without adding a new <see cref="WorkspaceRole"/>
    /// member, which this phase is explicitly barred from doing. No
    /// existing role expresses "zero permissions"; this is the closest
    /// available, documented here rather than left implicit.
    /// </summary>
    public WorkspaceRole CurrentRole { get; private set; } = WorkspaceRole.Support;

    public DesktopContextState ContextState => _contextState;

    string? IEnterpriseContext.CurrentOrganizationId => CurrentOrganization?.Id;

    string? IEnterpriseContext.CurrentBranchId => CurrentBranch?.Id;

    public IReadOnlyList<BranchDto> AvailableBranches => _availableBranches;

    public IReadOnlyList<string> RecentBranchIds => _recentBranchIds;

    public IReadOnlyList<string> FavoriteBranchIds => _favoriteBranchIds;

    public event EventHandler? SessionChanged;

    /// <summary>
    /// Reception Production Integration: raises <see cref="SessionChanged"/>
    /// at the end unconditionally (both the real and fallback paths) - a
    /// no-op the first time this runs (called from <c>App.xaml.cs</c> before
    /// <c>MainWindowViewModel</c> exists to subscribe), but exactly what lets
    /// <c>AcceptInviteViewModel</c> re-run this after a successful accept and
    /// have the shell's navigation/permissions refresh live, the same way
    /// <see cref="SwitchRoleAsync"/>/<see cref="SwitchBranchAsync"/> already
    /// do for their own changes.
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (_demoModeProvider.IsEnabled)
        {
            await ApplyDemoContextAsync(cancellationToken).ConfigureAwait(false);
            SessionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        var salonContext = await _salonContextService.GetCurrentContextAsync(cancellationToken).ConfigureAwait(false);
        if (salonContext is not null)
        {
            ApplyRealMembership(salonContext);
            SessionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        ApplyNoBusinessContext();
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Phase 2B Context State Hardening: the seeded-organization path -
    /// unchanged in its own internal behavior from before this phase, only
    /// now reachable exclusively via <see cref="_demoModeProvider"/>
    /// (see <see cref="InitializeAsync"/>), never as a side effect of empty
    /// real resolution.
    /// </summary>
    private async Task ApplyDemoContextAsync(CancellationToken cancellationToken)
    {
        var organizations = await _organizationQueryService.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);
        if (organizations.Count == 0)
        {
            _contextState = DesktopContextState.DemoContext;
            return;
        }

        var persisted = ReadPersistedSelection();

        var organization = organizations.FirstOrDefault(o => o.Id == persisted?.OrganizationId) ?? organizations[0];
        var branches = await _organizationQueryService.GetBranchesAsync(organization.Id, cancellationToken).ConfigureAwait(false);

        var branch = branches.FirstOrDefault(b => b.Id == persisted?.BranchId) ?? (branches.Count > 0 ? branches[0] : null);
        var role = persisted is not null && Enum.TryParse<WorkspaceRole>(persisted.Role, ignoreCase: true, out var parsedRole)
            ? parsedRole
            : WorkspaceRole.PlatformOwner;

        CurrentOrganization = organization;
        _availableBranches = branches.ToList();
        CurrentBranch = branch;
        CurrentRole = role;
        _recentBranchIds = persisted?.RecentBranchIds ?? [];
        _favoriteBranchIds = persisted?.FavoriteBranchIds ?? [];
        _contextState = DesktopContextState.DemoContext;
    }

    /// <summary>
    /// Phase 2B Context State Hardening: no organization, no branch, and
    /// the least-privileged existing role (see <see cref="CurrentRole"/>'s
    /// own doc comment) - the replacement for what used to silently become
    /// the seeded-organization fallback whenever real resolution came back
    /// empty. This is the P0 fix: an account with no real business context
    /// now genuinely has none, rather than one fabricated for it.
    /// </summary>
    private void ApplyNoBusinessContext()
    {
        CurrentOrganization = null;
        _availableBranches = [];
        CurrentBranch = null;
        CurrentRole = WorkspaceRole.Support;
        _recentBranchIds = [];
        _favoriteBranchIds = [];
        _contextState = DesktopContextState.NoBusinessContext;
    }

    public async Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default)
    {
        if (_contextState is DesktopContextState.OwnerContext or DesktopContextState.StaffContext)
        {
            throw new InvalidOperationException("Cannot switch branches for a session resolved from a real salon membership - there is no branch concept for a real salon in this phase.");
        }

        var branch = await _organizationQueryService.GetBranchByIdAsync(branchId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Branch '{branchId}' was not found.");

        if (CurrentOrganization is null || branch.OrganizationId != CurrentOrganization.Id)
        {
            var organization = await _organizationQueryService.GetOrganizationByIdAsync(branch.OrganizationId, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException($"Organization '{branch.OrganizationId}' was not found.");
            CurrentOrganization = organization;
            _availableBranches = (await _organizationQueryService.GetBranchesAsync(organization.Id, cancellationToken).ConfigureAwait(false)).ToList();
        }

        CurrentBranch = branch;

        _recentBranchIds.RemoveAll(id => id == branchId);
        _recentBranchIds.Insert(0, branchId);
        if (_recentBranchIds.Count > MaxRecentBranches)
        {
            _recentBranchIds.RemoveRange(MaxRecentBranches, _recentBranchIds.Count - MaxRecentBranches);
        }

        PersistSelection();
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default)
    {
        if (_contextState is DesktopContextState.OwnerContext or DesktopContextState.StaffContext)
        {
            throw new InvalidOperationException("Cannot switch roles for a session resolved from a real salon membership - accept a different invite, or sign in as a different user, instead.");
        }

        CurrentRole = role;
        PersistSelection();
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default)
    {
        if (!_favoriteBranchIds.Remove(branchId))
        {
            _favoriteBranchIds.Add(branchId);
        }

        PersistSelection();
        SessionChanged?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reception Production Integration: populates the session from a real
    /// <see cref="SalonContext"/> - no branch (backend Branch integration is
    /// out of this phase's scope, see the plan's own Context section),
    /// nothing recent/favorited (those are a local, per-org UI convenience
    /// this phase doesn't extend to real salons yet). Salon Adapter
    /// Migration Phase 1: the actual field-mapping (both the
    /// <see cref="OrganizationDto"/> shape and the <see cref="WorkspaceRole"/>)
    /// now lives in <see cref="_salonSessionAdapter"/> - this method only
    /// orchestrates this service's own state, unchanged in behavior from
    /// before the extraction.
    /// </summary>
    private void ApplyRealMembership(SalonContext salonContext)
    {
        CurrentOrganization = _salonSessionAdapter.ToOrganizationDto(salonContext);
        _availableBranches = [];
        CurrentBranch = null;
        CurrentRole = _salonSessionAdapter.ToWorkspaceRole(salonContext);
        _recentBranchIds = [];
        _favoriteBranchIds = [];
        _contextState = salonContext.IsOwner ? DesktopContextState.OwnerContext : DesktopContextState.StaffContext;
    }

    private SessionSettingsFile? ReadPersistedSelection()
    {
        if (!File.Exists(_settingsFilePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_settingsFilePath);
            return JsonSerializer.Deserialize<SessionSettingsFile>(json, SerializerOptions);
        }
#pragma warning disable CA1031 // A corrupt settings file must fall back to the default session, not crash startup.
        catch (Exception)
#pragma warning restore CA1031
        {
            return null;
        }
    }

    private void PersistSelection()
    {
        var directory = Path.GetDirectoryName(_settingsFilePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var settings = new SessionSettingsFile
        {
            OrganizationId = CurrentOrganization?.Id ?? string.Empty,
            BranchId = CurrentBranch?.Id ?? string.Empty,
            Role = CurrentRole.ToString(),
            RecentBranchIds = _recentBranchIds,
            FavoriteBranchIds = _favoriteBranchIds,
        };

        var json = JsonSerializer.Serialize(settings, SerializerOptions);
        File.WriteAllText(_settingsFilePath, json);
    }

    private static string DefaultSettingsFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "session.json");
}
