using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Identity;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
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

        public DesktopContextState ContextState => DesktopContextState.NoBusinessContext;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged { add { } remove { } }

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

/// <summary>
/// Phase B: Windows Reception Device Registration. Exercises
/// <see cref="App.RegisterDeviceForActiveSalon"/> - the extracted, pure static method
/// <see cref="App.OnStartup"/> calls only after the real, resolved active salon is known (see that
/// method's own doc comment). Same "plain static method over injected seams, no WPF host/dispatcher
/// needed" shape as <see cref="AppTests"/>'s own <c>InitializeSessionWithRetry</c> coverage above.
/// </summary>
public sealed class RegisterDeviceForActiveSalonTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void SingleResolvedSalon_RegistersDevice_ForThatSalon()
    {
        var salonContext = new StubSalonContextService("salon-1");
        var deviceAuthorization = new StubDeviceAuthorizationService(
            DeviceRegistrationResult.Registered(new DeviceRegistrationResponse("record-1", "salon-1", "device-1", Now, Now, null)));
        var failureCalls = new List<(DeviceRegistrationOutcome Outcome, string? Message)>();

        App.RegisterDeviceForActiveSalon(salonContext, deviceAuthorization, (outcome, message) => failureCalls.Add((outcome, message)));

        Assert.Equal("salon-1", deviceAuthorization.LastSalonIdRegistered);
        Assert.Empty(failureCalls);
    }

    /// <summary>
    /// Multi-salon account, no explicit selection made yet (the real <c>ISalonContextService</c>
    /// implementation returns <see langword="null"/> from <c>GetSalonIdAsync</c> in exactly this
    /// state - see <c>BackendSalonContextService.ResolveAsync</c>'s own "never silently picks the
    /// first" doc comment). Must never guess a salon to register for.
    /// </summary>
    [Fact]
    public void MultiSalonNotYetResolved_NeverRegistersAnySalon()
    {
        var salonContext = new StubSalonContextService(activeSalonId: null);
        var deviceAuthorization = new StubDeviceAuthorizationService(
            DeviceRegistrationResult.Registered(new DeviceRegistrationResponse("record-1", "salon-1", "device-1", Now, Now, null)));

        App.RegisterDeviceForActiveSalon(salonContext, deviceAuthorization, (_, _) => throw new InvalidOperationException("must not be called - registration must never run at all"));

        Assert.False(deviceAuthorization.WasCalled);
    }

    /// <summary>
    /// Simulates the post-<c>SalonSelectionWindow</c> state: the user already made an explicit
    /// choice (<c>ISalonContextService.SelectSalonAsync</c> already ran), so <c>GetSalonIdAsync</c>
    /// now resolves the chosen salon, not <see langword="null"/> and not an arbitrary candidate.
    /// </summary>
    [Fact]
    public void MultiSalonExplicitlyResolved_RegistersForTheChosenSalon_NotAnArbitraryOne()
    {
        var salonContext = new StubSalonContextService("salon-2");
        var deviceAuthorization = new StubDeviceAuthorizationService(
            DeviceRegistrationResult.Registered(new DeviceRegistrationResponse("record-1", "salon-2", "device-1", Now, Now, null)));

        App.RegisterDeviceForActiveSalon(salonContext, deviceAuthorization, (_, _) => throw new InvalidOperationException("must not be called"));

        Assert.Equal("salon-2", deviceAuthorization.LastSalonIdRegistered);
    }

    [Fact]
    public void RegistrationOutcomeNotRegistered_InvokesCallback_NeverThrows_StartupCanContinue()
    {
        var salonContext = new StubSalonContextService("salon-1");
        var deviceAuthorization = new StubDeviceAuthorizationService(DeviceRegistrationResult.NetworkUnavailable("offline"));
        (DeviceRegistrationOutcome Outcome, string? Message)? captured = null;

        App.RegisterDeviceForActiveSalon(salonContext, deviceAuthorization, (outcome, message) => captured = (outcome, message));

        Assert.NotNull(captured);
        Assert.Equal(DeviceRegistrationOutcome.NetworkUnavailable, captured!.Value.Outcome);
    }

    private sealed class StubSalonContextService(string? activeSalonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(activeSalonId);
    }

    private sealed class StubDeviceAuthorizationService(DeviceRegistrationResult result) : IDeviceAuthorizationService
    {
        public bool WasCalled { get; private set; }

        public string? LastSalonIdRegistered { get; private set; }

        public Task<DeviceRegistrationResult> RegisterAsync(string salonId, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            LastSalonIdRegistered = salonId;
            return Task.FromResult(result);
        }
    }
}
