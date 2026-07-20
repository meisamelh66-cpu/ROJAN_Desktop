namespace Rojan.Desktop.Shell.Organizations;

/// <summary>The persisted-selection JSON shape - see <see cref="CurrentSessionService"/>.</summary>
public sealed class SessionSettingsFile
{
    public string OrganizationId { get; set; } = string.Empty;

    public string BranchId { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
