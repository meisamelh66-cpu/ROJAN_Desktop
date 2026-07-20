using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>One organization's branches, grouped for the Branch Switcher flyout - the "Organization grouping" requirement.</summary>
public sealed record BranchSwitcherGroup(string OrganizationId, string OrganizationName, IReadOnlyList<BranchDto> Branches);
