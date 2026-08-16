using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Organizations;

namespace Rojan.Desktop.Shell.Tests;

/// <summary>
/// Reception Stabilization Sprint: exercises <see cref="App.InitializeSessionWithRetry"/> - the
/// fix for the unhandled startup crash when salon-scope resolution fails (a raw
/// <c>ApiException</c> from <c>BackendSalonContextService.ResolveAsync</c> previously propagated
/// straight off the top of <c>OnStartup</c>, before <c>MainWindow.Show()</c>, with nothing to
/// catch it). Calls the method directly (no <see cref="App"/> instance, no WPF host/dispatcher
/// needed - it's a pure static method over an injected <see cref="ICurrentSessionService"/> and a
/// <c>confirmRetry</c> delegate) so this is a plain, fast unit test despite living in the Shell
/// composition root.
/// </summary>
public sealed class AppTests
{
    [Fact]
    public void InitializeSessionWithRetry_SucceedsFirstTry_ReturnsTrueWithoutPromptingRetry()
    {
        var session = new ThrowingCurrentSessionService(failuresBeforeSuccess: 0);
        var confirmRetryCallCount = 0;

        var result = App.InitializeSessionWithRetry(session, () =>
        {
            confirmRetryCallCount++;
            return true;
        });

        Assert.True(result);
        Assert.Equal(0, confirmRetryCallCount);
        Assert.Equal(1, session.InitializeAsyncCallCount);
    }

    [Fact]
    public void InitializeSessionWithRetry_FailsThenSucceedsOnRetry_ReturnsTrue()
    {
        var session = new ThrowingCurrentSessionService(failuresBeforeSuccess: 2);

        var result = App.InitializeSessionWithRetry(session, () => true);

        Assert.True(result);
        Assert.Equal(3, session.InitializeAsyncCallCount);
    }

    [Fact]
    public void InitializeSessionWithRetry_FailsAndUserDeclinesRetry_ReturnsFalseWithoutCrashing()
    {
        var session = new ThrowingCurrentSessionService(failuresBeforeSuccess: int.MaxValue);

        var result = App.InitializeSessionWithRetry(session, () => false);

        Assert.False(result);
        Assert.Equal(1, session.InitializeAsyncCallCount);
    }

    private sealed class ThrowingCurrentSessionService(int failuresBeforeSuccess) : ICurrentSessionService
    {
        public int InitializeAsyncCallCount { get; private set; }

        public OrganizationDto? CurrentOrganization => null;

        public BranchDto? CurrentBranch => null;

        public WorkspaceRole CurrentRole => WorkspaceRole.PlatformOwner;

        public bool HasRealMembership => false;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeAsyncCallCount++;
            if (InitializeAsyncCallCount <= failuresBeforeSuccess)
            {
                throw new InvalidOperationException("Simulated salon-scope resolution failure.");
            }

            return Task.CompletedTask;
        }

        public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
