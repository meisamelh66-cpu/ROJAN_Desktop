using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.Tests.Specialists;
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

    // Phase 8.35 AcceptInvite Security Logging: the LookupAsync / AcceptAsync broad-catch
    // boundaries now log at Error, operation name only. The invite token, the user's
    // identity (resolved by ICurrentSessionService.InitializeAsync), and any backend
    // response detail must never enter the log line. User-visible error is unchanged,
    // verified by LookupCommand_Failure_* / AcceptCommand_Failure_* above.

    private const string SecretToken = "SECRET-INVITE-TOKEN-xyz789";

    [Fact]
    public void LookupCommand_Failure_LogsErrorWithoutLeakingToken()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsException = new InvalidOperationException($"invite {SecretToken} not found or no longer available"),
        };
        var logger = new RecordingLogger<AcceptInviteViewModel>();
        var sut = new AcceptInviteViewModel(inviteService, new StubSalonContextService(), new StubCurrentSessionService(), logger)
        {
            Token = SecretToken,
        };

        sut.LookupCommand.Execute(null);

        Assert.True(sut.HasLookupError);
        Assert.Contains(SecretToken, sut.LookupErrorMessage!, StringComparison.Ordinal); // the user still sees the raw backend message
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("LookupAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretToken, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptCommand_Failure_LogsErrorWithoutLeakingToken()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST"),
            AcceptException = new InvalidOperationException($"accept failed for {SecretToken}"),
        };
        var logger = new RecordingLogger<AcceptInviteViewModel>();
        var sut = new AcceptInviteViewModel(inviteService, new StubSalonContextService(), new StubCurrentSessionService(), logger)
        {
            Token = SecretToken,
        };
        sut.LookupCommand.Execute(null);

        sut.AcceptCommand.Execute(null);

        Assert.True(sut.HasAcceptError);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AcceptAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretToken, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsResult = new SalonInviteDetailsDto("Glow Salon", "RECEPTIONIST"),
            AcceptResult = new AcceptedMembershipDto("salon-1", "Glow Salon", "RECEPTIONIST"),
        };
        var session = new StubCurrentSessionService
        {
            InitializeException = new InvalidOperationException("session resolution failed for user owner@salon.example (id u-4821)"),
        };
        var logger = new RecordingLogger<AcceptInviteViewModel>();
        var sut = new AcceptInviteViewModel(inviteService, new StubSalonContextService(), session, logger)
        {
            Token = SecretToken,
        };
        sut.LookupCommand.Execute(null);

        sut.AcceptCommand.Execute(null);

        Assert.True(sut.HasAcceptError);
        Assert.False(sut.IsAccepted);
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("AcceptAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("owner@salon.example", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("u-4821", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(SecretToken, entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows()
    {
        var inviteService = new StubSalonInviteService
        {
            DetailsException = new InvalidOperationException("boom"),
        };
        var sut = new AcceptInviteViewModel(inviteService, new StubSalonContextService(), new StubCurrentSessionService())
        {
            Token = SecretToken,
        };

        var exception = Record.Exception(() => sut.LookupCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasLookupError);
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

        public Task<CreatedInviteDto> CreateReceptionInviteAsync(string salonId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AcceptInviteViewModel never creates invites - it only accepts them.");

        public Task<byte[]> GetInviteQrCodeAsync(string salonId, string inviteId, int sizePx, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("AcceptInviteViewModel never fetches invite QR codes.");
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

        /// <summary>Phase 8.35: when set, <see cref="InitializeAsync"/> throws this (after counting the call) so a test can exercise the post-accept session-re-establishment failure path.</summary>
        public Exception? InitializeException { get; set; }

        public OrganizationDto? CurrentOrganization => null;

        public BranchDto? CurrentBranch => null;

        public WorkspaceRole CurrentRole => WorkspaceRole.Reception;

        public DesktopContextState ContextState => DesktopContextState.NoBusinessContext;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeAsyncCallCount++;
            if (InitializeException is not null)
            {
                return Task.FromException(InitializeException);
            }

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
