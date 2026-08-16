using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Membership;

namespace Rojan.Desktop.Presentation.Tests.Membership;

public sealed class AcceptInviteViewModelTests
{
    [Fact]
    public void LookupCommand_CanExecute_FalseUntilATokenIsEntered()
    {
        var sut = CreateSut(out _, out _, out _);

        Assert.False(sut.LookupCommand.CanExecute(null));

        sut.Token = "tok-123";

        Assert.True(sut.LookupCommand.CanExecute(null));
    }

    [Fact]
    public void LookupCommand_Success_PopulatesDetailsAndJoinPrompt()
    {
        var inviteService = new StubSalonInviteService { DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST") };
        var sut = CreateSut(inviteService, out _, out _);
        sut.Token = "tok-123";

        sut.LookupCommand.Execute(null);

        Assert.True(sut.HasDetails);
        Assert.False(sut.HasLookupError);
        Assert.Equal("Glow Salon", sut.Details?.SalonName);
        Assert.Contains("Glow Salon", sut.JoinPrompt);
    }

    [Fact]
    public void LookupCommand_Failure_SetsLookupErrorAndLeavesTokenIntact()
    {
        var inviteService = new StubSalonInviteService { DetailsException = new InvalidOperationException("Salon invite not found or no longer available") };
        var sut = CreateSut(inviteService, out _, out _);
        sut.Token = "does-not-exist";

        sut.LookupCommand.Execute(null);

        Assert.False(sut.HasDetails);
        Assert.True(sut.HasLookupError);
        Assert.Equal("Salon invite not found or no longer available", sut.LookupErrorMessage);
        Assert.Equal("does-not-exist", sut.Token);
    }

    [Fact]
    public void SettingToken_AfterALookup_ClearsDetails()
    {
        var inviteService = new StubSalonInviteService { DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST") };
        var sut = CreateSut(inviteService, out _, out _);
        sut.Token = "tok-123";
        sut.LookupCommand.Execute(null);
        Assert.True(sut.HasDetails);

        sut.Token = "tok-456";

        Assert.False(sut.HasDetails);
    }

    [Fact]
    public void AcceptCommand_CanExecute_FalseUntilALookupHasSucceeded()
    {
        var sut = CreateSut(out _, out _, out _);
        sut.Token = "tok-123";

        Assert.False(sut.AcceptCommand.CanExecute(null));
    }

    [Fact]
    public void AcceptCommand_Success_InvalidatesSalonContextAndReinitializesSession()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST"),
            AcceptResult = new AcceptedMembershipDto("salon-1", "Glow Salon", "RECEPTIONIST"),
        };
        var sut = CreateSut(inviteService, out var salonContextService, out var currentSessionService);
        sut.Token = "tok-123";
        sut.LookupCommand.Execute(null);

        sut.AcceptCommand.Execute(null);

        Assert.True(sut.IsAccepted);
        Assert.False(sut.HasAcceptError);
        Assert.Equal(1, salonContextService.InvalidateCallCount);
        Assert.Equal(1, currentSessionService.InitializeAsyncCallCount);
        Assert.Equal("tok-123", inviteService.LastAcceptToken);
        Assert.Equal("Glow Salon", inviteService.LastAcceptSalonName);
    }

    [Fact]
    public void AcceptCommand_Failure_SetsAcceptErrorAndNeverTouchesTheSession()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST"),
            AcceptException = new InvalidOperationException("Salon invite not found or no longer available"),
        };
        var sut = CreateSut(inviteService, out var salonContextService, out var currentSessionService);
        sut.Token = "tok-123";
        sut.LookupCommand.Execute(null);

        sut.AcceptCommand.Execute(null);

        Assert.False(sut.IsAccepted);
        Assert.True(sut.HasAcceptError);
        Assert.Equal(0, salonContextService.InvalidateCallCount);
        Assert.Equal(0, currentSessionService.InitializeAsyncCallCount);
    }

    private static AcceptInviteViewModel CreateSut(
        StubSalonInviteService inviteService,
        out StubSalonContextService salonContextService,
        out StubCurrentSessionService currentSessionService)
    {
        salonContextService = new StubSalonContextService();
        currentSessionService = new StubCurrentSessionService();
        return new AcceptInviteViewModel(inviteService, salonContextService, currentSessionService);
    }

    private static AcceptInviteViewModel CreateSut(
        out StubSalonContextService salonContextService,
        out StubCurrentSessionService currentSessionService,
        out StubSalonInviteService inviteService)
    {
        inviteService = new StubSalonInviteService();
        return CreateSut(inviteService, out salonContextService, out currentSessionService);
    }

    private sealed class StubSalonInviteService : ISalonInviteService
    {
        public SalonInviteDetailsDto? DetailsResult { get; set; }

        public Exception? DetailsException { get; set; }

        public AcceptedMembershipDto? AcceptResult { get; set; }

        public Exception? AcceptException { get; set; }

        public string? LastAcceptToken { get; private set; }

        public string? LastAcceptSalonName { get; private set; }

        public Task<SalonInviteDetailsDto> GetDetailsAsync(string token, CancellationToken cancellationToken = default) =>
            DetailsException is not null ? Task.FromException<SalonInviteDetailsDto>(DetailsException) : Task.FromResult(DetailsResult!);

        public Task<AcceptedMembershipDto> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default)
        {
            LastAcceptToken = token;
            LastAcceptSalonName = salonName;
            return AcceptException is not null ? Task.FromException<AcceptedMembershipDto>(AcceptException) : Task.FromResult(AcceptResult!);
        }
    }

    private sealed class StubSalonContextService : ISalonContextService
    {
        public int InvalidateCallCount { get; private set; }

        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<string?>(null);

        public Task<SalonContext?> GetCurrentContextAsync(CancellationToken cancellationToken = default) => Task.FromResult<SalonContext?>(null);

        public void Invalidate() => InvalidateCallCount++;
    }

    private sealed class StubCurrentSessionService : ICurrentSessionService
    {
        public int InitializeAsyncCallCount { get; private set; }

        public OrganizationDto? CurrentOrganization => null;

        public BranchDto? CurrentBranch => null;

        public WorkspaceRole CurrentRole => WorkspaceRole.Reception;

        public bool HasRealMembership => false;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeAsyncCallCount++;
            SessionChanged?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        }

        public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by AcceptInviteViewModelTests.");

        public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by AcceptInviteViewModelTests.");

        public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by AcceptInviteViewModelTests.");
    }
}
