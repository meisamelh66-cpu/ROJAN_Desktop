using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>Minimal, controllable <see cref="ICurrentSessionService"/> test double - lets a test set <see cref="CurrentRole"/> directly and fire <see cref="SessionChanged"/> on demand, without any file-system persistence.</summary>
internal sealed class StubCurrentSessionService : ICurrentSessionService
{
    public OrganizationDto? CurrentOrganization { get; set; }

    public BranchDto? CurrentBranch { get; set; }

    public WorkspaceRole CurrentRole { get; set; } = WorkspaceRole.PlatformOwner;

    public DesktopContextState ContextState { get; set; } = DesktopContextState.NoBusinessContext;

    public IReadOnlyList<BranchDto> AvailableBranches { get; set; } = [];

    public IReadOnlyList<string> RecentBranchIds { get; set; } = [];

    public IReadOnlyList<string> FavoriteBranchIds { get; set; } = [];

    public event EventHandler? SessionChanged;

    public void RaiseSessionChanged() => SessionChanged?.Invoke(this, EventArgs.Empty);

    public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default)
    {
        CurrentRole = role;
        RaiseSessionChanged();
        return Task.CompletedTask;
    }

    public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
