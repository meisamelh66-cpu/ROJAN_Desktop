using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Presentation.Organizations;

/// <summary>
/// The Enterprise Multi-Branch platform's session façade - which
/// <see cref="OrganizationDto"/>/<see cref="BranchDto"/>/
/// <see cref="WorkspaceRole"/> this running desktop session
/// is currently scoped to. Same "interface in Presentation, concrete
/// implementation in Shell" split as <c>Localization.ILocalizationService</c>/
/// <c>Theming.IThemeService</c>, since the real implementation needs
/// file-system access (persisted selection). Unlike Language/Theme,
/// switching branch or role takes effect immediately - <see cref="SessionChanged"/>
/// fires live so any subscribed ViewModel can refresh, per this phase's
/// explicit "Changing branch refreshes the active workspace" requirement
/// (no restart needed - a data-scope change, not a resource-dictionary
/// swap).
/// </summary>
public interface ICurrentSessionService
{
    public OrganizationDto? CurrentOrganization { get; }

    public BranchDto? CurrentBranch { get; }

    public WorkspaceRole CurrentRole { get; }

    /// <summary>
    /// Reception Stabilization Sprint: <see langword="true"/> once <see cref="InitializeAsync"/> has
    /// resolved a real backend salon membership (owner via <c>GET /salons/mine</c>, or an accepted
    /// Salon Invite) rather than falling back to the local/demo organization path - the same signal
    /// the concrete implementation already uses internally to lock <see cref="SwitchRoleAsync"/>/
    /// <see cref="SwitchBranchAsync"/>, now exposed so Presentation-layer consumers (e.g. the
    /// shell's initial navigation selection) can also branch on it without new plumbing.
    /// </summary>
    public bool HasRealMembership { get; }

    /// <summary>Every branch in <see cref="CurrentOrganization"/> - the Branch Switcher's data source.</summary>
    public IReadOnlyList<BranchDto> AvailableBranches { get; }

    /// <summary>Most-recently-switched-to branch ids across every organization, newest first - the Branch Switcher's "Recently used branches" section.</summary>
    public IReadOnlyList<string> RecentBranchIds { get; }

    /// <summary>Branch ids the user has starred, across every organization - the Branch Switcher's "Favorite branches" section.</summary>
    public IReadOnlyList<string> FavoriteBranchIds { get; }

    /// <summary>Raised after <see cref="SwitchBranchAsync"/>, <see cref="SwitchRoleAsync"/>, or <see cref="ToggleFavoriteBranchAsync"/> completes - live, not restart-required.</summary>
    public event EventHandler? SessionChanged;

    /// <summary>Loads the persisted organization/branch/role selection (defaulting to the first seeded organization's first branch, as <see cref="WorkspaceRole.PlatformOwner"/>, if none is saved yet) - called once at startup.</summary>
    public Task InitializeAsync(CancellationToken cancellationToken = default);

    /// <summary>Switches the active branch (and its owning organization, if different) live and persists the choice.</summary>
    public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default);

    /// <summary>Switches the active workspace role live and persists the choice.</summary>
    public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default);

    /// <summary>Stars or un-stars a branch (toggled) and persists the choice - the Branch Switcher's favorite-star action.</summary>
    public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default);
}
