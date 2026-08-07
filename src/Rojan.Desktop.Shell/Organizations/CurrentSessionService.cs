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
/// Reception Production Integration: <see cref="InitializeAsync"/> now
/// tries a *real* resolution first, via <see cref="ISalonContextService.GetCurrentContextAsync"/>
/// (owner via <c>GET /salons/mine</c>, or an accepted Salon Invite) -
/// only when that yields nothing does it fall back to the
/// <see cref="IOrganizationQueryService"/>/<c>session.json</c> path
/// documented above, entirely unchanged. A session resolved for real is
/// marked (<see cref="_hasRealMembership"/>) so <see cref="SwitchRoleAsync"/>/
/// <see cref="SwitchBranchAsync"/> can refuse to silently overwrite it -
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
    private readonly string _settingsFilePath;
    private List<BranchDto> _availableBranches = [];
    private List<string> _recentBranchIds = [];
    private List<string> _favoriteBranchIds = [];
    private bool _hasRealMembership;

    public CurrentSessionService(IOrganizationQueryService organizationQueryService, ISalonContextService salonContextService)
        : this(organizationQueryService, salonContextService, DefaultSettingsFilePath())
    {
    }

    internal CurrentSessionService(IOrganizationQueryService organizationQueryService, ISalonContextService salonContextService, string settingsFilePath)
    {
        _organizationQueryService = organizationQueryService;
        _salonContextService = salonContextService;
        _settingsFilePath = settingsFilePath;
    }

    public OrganizationDto? CurrentOrganization { get; private set; }

    public BranchDto? CurrentBranch { get; private set; }

    public WorkspaceRole CurrentRole { get; private set; } = WorkspaceRole.PlatformOwner;

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
        var salonContext = await _salonContextService.GetCurrentContextAsync(cancellationToken).ConfigureAwait(false);
        if (salonContext is not null)
        {
            ApplyRealMembership(salonContext);
            SessionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        _hasRealMembership = false;

        var organizations = await _organizationQueryService.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);
        if (organizations.Count == 0)
        {
            SessionChanged?.Invoke(this, EventArgs.Empty);
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
        SessionChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default)
    {
        if (_hasRealMembership)
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
        if (_hasRealMembership)
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
    /// this phase doesn't extend to real salons yet). <see cref="OrganizationDto"/>'s
    /// many fields beyond id/name (LegalName/Logo/Subscription/Code/TimeZone/etc.)
    /// have no equivalent on ROJAN_Backend's <c>Salon</c> at all - populated
    /// with honest defaults, not fabricated values standing in for real data
    /// that simply does not exist in this backend's model.
    /// </summary>
    private void ApplyRealMembership(SalonContext salonContext)
    {
        CurrentOrganization = new OrganizationDto(
            Id: salonContext.SalonId,
            Name: salonContext.SalonName,
            LegalName: salonContext.SalonName,
            Logo: string.Empty,
            BrandColor: string.Empty,
            TaxInformation: string.Empty,
            Subscription: SubscriptionPlan.Trial,
            Status: OrganizationStatus.Active,
            CreatedDate: DateTimeOffset.UtcNow,
            Code: string.Empty,
            Phone: string.Empty,
            Email: string.Empty,
            Address: string.Empty,
            TimeZone: string.Empty,
            Language: string.Empty,
            Currency: string.Empty);
        _availableBranches = [];
        CurrentBranch = null;
        CurrentRole = MapRole(salonContext);
        _recentBranchIds = [];
        _favoriteBranchIds = [];
        _hasRealMembership = true;
    }

    /// <summary>An owner's role is never a <c>SalonRole</c> membership (see ROJAN_Backend's own doc comment on that enum) - only the non-owner branch maps a backend role string. Falls back to <see cref="WorkspaceRole.Reception"/> for any role string this Desktop app doesn't otherwise recognize (e.g. a manager invite accepted here, out of this phase's own scope) rather than throwing - a real, backend-confirmed membership must never fail to resolve into *some* usable session.</summary>
    private static WorkspaceRole MapRole(SalonContext salonContext)
    {
        if (salonContext.IsOwner)
        {
            return WorkspaceRole.OrganizationOwner;
        }

        return salonContext.MembershipRole switch
        {
            "MANAGER" => WorkspaceRole.OrganizationManager,
            _ => WorkspaceRole.Reception,
        };
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
