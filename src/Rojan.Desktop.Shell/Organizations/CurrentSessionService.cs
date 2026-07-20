using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Organizations;
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
/// </summary>
public sealed class CurrentSessionService : ICurrentSessionService, IEnterpriseContext
{
    /// <summary>The Branch Switcher's "Recently used branches" cap - the five most recent, newest first.</summary>
    public const int MaxRecentBranches = 5;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly IOrganizationQueryService _organizationQueryService;
    private readonly string _settingsFilePath;
    private List<BranchDto> _availableBranches = [];
    private List<string> _recentBranchIds = [];
    private List<string> _favoriteBranchIds = [];

    public CurrentSessionService(IOrganizationQueryService organizationQueryService)
        : this(organizationQueryService, DefaultSettingsFilePath())
    {
    }

    internal CurrentSessionService(IOrganizationQueryService organizationQueryService, string settingsFilePath)
    {
        _organizationQueryService = organizationQueryService;
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

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var organizations = await _organizationQueryService.GetOrganizationsAsync(cancellationToken).ConfigureAwait(false);
        if (organizations.Count == 0)
        {
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
    }

    public async Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default)
    {
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
