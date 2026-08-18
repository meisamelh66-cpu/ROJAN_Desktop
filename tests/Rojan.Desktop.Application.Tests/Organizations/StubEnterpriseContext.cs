using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Tests.Organizations;

/// <summary>Minimal, controllable <see cref="IEnterpriseContext"/> test double - defaults to org-1/branch-1/PlatformOwner (the same ids <c>Infrastructure.Organizations.FakeOrganizationRepository</c> seeds), settable per test for isolation/permission scenarios.</summary>
public sealed class StubEnterpriseContext : IEnterpriseContext
{
    public string? CurrentOrganizationId { get; set; } = "org-1";

    public string? CurrentBranchId { get; set; } = "branch-1";

    public WorkspaceRole CurrentRole { get; set; } = WorkspaceRole.PlatformOwner;

    public IReadOnlySet<string> BackendPermissions { get; set; } = new HashSet<string>();
}
