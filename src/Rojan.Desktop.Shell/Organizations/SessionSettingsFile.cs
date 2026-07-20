namespace Rojan.Desktop.Shell.Organizations;

/// <summary>The persisted-selection JSON shape - see <see cref="CurrentSessionService"/>.</summary>
public sealed class SessionSettingsFile
{
    public string OrganizationId { get; set; } = string.Empty;

    public string BranchId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    /// <summary>Most-recently-switched-to branch ids, newest first, capped at <see cref="Rojan.Desktop.Shell.Organizations.CurrentSessionService.MaxRecentBranches"/> - the Branch Switcher's "Recently used branches" section.</summary>
    public List<string> RecentBranchIds { get; set; } = [];

    /// <summary>Branch ids the user has starred - the Branch Switcher's "Favorite branches" section.</summary>
    public List<string> FavoriteBranchIds { get; set; } = [];
}
