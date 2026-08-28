using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Organizations;

namespace Rojan.Desktop.Presentation.Tests.Organizations;

/// <summary>
/// <see cref="OrganizationPageViewModel"/> tests. Phase 8.27 added the first
/// <c>LoadAsync</c> logging coverage; Phase 8.78 (Missing-Guard Sweep Wave D)
/// adds command-failure guards (create org/branch, save branch settings,
/// switch role) plus the selection-triggered secondary-load guards. All test
/// doubles here are private nested - no shared stub is touched.
/// </summary>
public sealed class OrganizationPageViewModelTests
{
    private static OrganizationDto MakeOrg(string id, string name) =>
        new(id, name, name + " Legal LLC", string.Empty, "#000000", "TAX-" + id, SubscriptionPlan.Trial,
            OrganizationStatus.Active, DateTimeOffset.UnixEpoch, "ORG-" + id, "+1 555", id + "@rojan.example", "1 Main St", "UTC", "en", "USD");

    private static BranchDto MakeBranch(string id, string organizationId, string name) =>
        new(id, organizationId, name, "BR-" + id, "2 Side St", "+1 555", id + "@branch.example", "Manager", "UTC", "USD", BranchStatus.Active);

    private static OrganizationPageViewModel CreateSut(
        StubOrganizationQueryService queryService,
        StubOrganizationCommandService? commandService = null,
        StubCurrentSessionService? sessionService = null,
        RecordingLogger<OrganizationPageViewModel>? logger = null) =>
        new(queryService, commandService ?? new StubOrganizationCommandService(), new PermissionEngine(), sessionService ?? new StubCurrentSessionService(), logger);

    // -------------------------------------------------------------------------
    // Existing coverage (Phase 8.27) - LoadAsync boundary.
    // -------------------------------------------------------------------------

    [Fact]
    public void LoadAsync_QueryThrows_LogsError()
    {
        var queryService = new StubOrganizationQueryService { GetOrganizationsException = new InvalidOperationException("boom") };
        var logger = new RecordingLogger<OrganizationPageViewModel>();

        var sut = CreateSut(queryService, logger: logger);

        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new StubOrganizationQueryService { GetOrganizationsException = new InvalidOperationException("boom") };

        var exception = Record.Exception(() => CreateSut(queryService));

        Assert.Null(exception);
    }

    // -------------------------------------------------------------------------
    // Production Hardening - Missing-Guard Sweep Wave D (Organization commands).
    // -------------------------------------------------------------------------

    private const string OrgBackendSecret = "backend 500: org 'Rojan Holdings LLC' tax=IR-9982 VAT=9% role=PlatformOwner";

    [Fact]
    public void CreateOrganizationCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var commandService = new StubOrganizationCommandService { CreateOrganizationException = new InvalidOperationException(OrgBackendSecret) };
        var sut = CreateSut(queryService, commandService);
        sut.NewOrgName = "New Holdings";
        sut.NewOrgTaxInformation = "TAX-NEW";

        var exception = Record.Exception(() => sut.CreateOrganizationCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
        Assert.Equal("New Holdings", sut.NewOrgName);
        Assert.Equal("TAX-NEW", sut.NewOrgTaxInformation);
        Assert.Single(commandService.CreateOrganizationCalls);
    }

    [Fact]
    public void CreateBranchCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var commandService = new StubOrganizationCommandService { CreateBranchException = new InvalidOperationException(OrgBackendSecret) };
        var sut = CreateSut(queryService, commandService);
        sut.NewBranchName = "Downtown";
        sut.NewBranchAddress = "9 Center Ave";

        var exception = Record.Exception(() => sut.CreateBranchCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Equal("Downtown", sut.NewBranchName);
        Assert.Equal("9 Center Ave", sut.NewBranchAddress);
        Assert.Single(commandService.CreateBranchCalls);
    }

    [Fact]
    public void SaveBranchSettingsCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotShowSaved()
    {
        var queryService = new StubOrganizationQueryService
        {
            Organizations = { MakeOrg("org-1", "Rojan") },
            Branches = { MakeBranch("branch-1", "org-1", "Main") },
        };
        var commandService = new StubOrganizationCommandService { SetBranchSettingsException = new InvalidOperationException(OrgBackendSecret) };
        var sut = CreateSut(queryService, commandService);

        var exception = Record.Exception(() => sut.SaveBranchSettingsCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(Strings.Organizations_SettingsSaved, sut.StatusMessage);
        Assert.Null(sut.CurrentBranchSettings);
        Assert.Single(commandService.SetBranchSettingsCalls);
    }

    [Fact]
    public void SaveBranchSettingsCommand_MalformedTime_DoesNotThrow_SetsActionError()
    {
        var queryService = new StubOrganizationQueryService
        {
            Organizations = { MakeOrg("org-1", "Rojan") },
            Branches = { MakeBranch("branch-1", "org-1", "Main") },
        };
        var commandService = new StubOrganizationCommandService();
        var sut = CreateSut(queryService, commandService);
        sut.SettingsOpenTime = "not-a-time";

        var exception = Record.Exception(() => sut.SaveBranchSettingsCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(Strings.Organizations_SettingsSaved, sut.StatusMessage);
        Assert.Empty(commandService.SetBranchSettingsCalls);
    }

    [Fact]
    public void SwitchRoleCommand_Failure_DoesNotThrow_RevertsPickerAndLeavesSessionRoleUnchanged()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner, SwitchRoleException = new InvalidOperationException(OrgBackendSecret) };
        var sut = CreateSut(queryService, sessionService: session);
        sut.SelectedRoleToSwitchTo = WorkspaceRole.Reception;

        var exception = Record.Exception(() => sut.SwitchRoleCommand.Execute(null));

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.Equal(WorkspaceRole.PlatformOwner, session.CurrentRole);          // session unchanged
        Assert.Equal(WorkspaceRole.PlatformOwner, sut.SelectedRoleToSwitchTo);   // picker reverted
        Assert.Equal(WorkspaceRole.PlatformOwner, sut.CurrentRole);
        Assert.Equal(WorkspaceRole.Reception, Assert.Single(session.SwitchRoleAttempts));
    }

    [Fact]
    public void SwitchRoleCommand_Success_SwitchesRoleAndClearsError()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var session = new StubCurrentSessionService { CurrentRole = WorkspaceRole.PlatformOwner, SwitchRoleException = new InvalidOperationException("boom") };
        var sut = CreateSut(queryService, sessionService: session);
        sut.SelectedRoleToSwitchTo = WorkspaceRole.Reception;
        sut.SwitchRoleCommand.Execute(null);
        Assert.True(sut.HasActionError);

        session.SwitchRoleException = null;
        sut.SelectedRoleToSwitchTo = WorkspaceRole.Accounting;
        sut.SwitchRoleCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Equal(WorkspaceRole.Accounting, session.CurrentRole);
        Assert.Equal(WorkspaceRole.Accounting, sut.SelectedRoleToSwitchTo);
    }

    [Fact]
    public void CreateOrganizationCommand_SuccessAfterFailure_ClearsActionError()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var commandService = new StubOrganizationCommandService { CreateOrganizationException = new InvalidOperationException("boom") };
        var sut = CreateSut(queryService, commandService);
        sut.NewOrgName = "Attempt 1";
        sut.CreateOrganizationCommand.Execute(null);
        Assert.True(sut.HasActionError);

        commandService.CreateOrganizationException = null;
        sut.NewOrgName = "Attempt 2";
        sut.CreateOrganizationCommand.Execute(null);

        Assert.False(sut.HasActionError);
        Assert.Null(sut.ActionErrorMessage);
        Assert.Equal(string.Empty, sut.NewOrgName);
    }

    [Fact]
    public void SelectingAnotherOrganization_BranchLoadFails_SetsActionErrorAndDoesNotThrowOrBlankPage()
    {
        var queryService = new StubOrganizationQueryService
        {
            Organizations = { MakeOrg("org-1", "Rojan"), MakeOrg("org-2", "Second") },
        };
        var sut = CreateSut(queryService);
        Assert.Equal(DashboardState.Loaded, sut.State);

        queryService.GetBranchesException = new InvalidOperationException(OrgBackendSecret);
        var exception = Record.Exception(() => sut.SelectedOrganization = queryService.Organizations[1]);

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
    }

    [Fact]
    public void SelectingAnotherBranch_SettingsLoadFails_SetsActionError()
    {
        var queryService = new StubOrganizationQueryService
        {
            Organizations = { MakeOrg("org-1", "Rojan") },
            Branches = { MakeBranch("branch-1", "org-1", "Main"), MakeBranch("branch-2", "org-1", "Second") },
        };
        var sut = CreateSut(queryService);
        Assert.Equal(DashboardState.Loaded, sut.State);

        queryService.GetBranchSettingsException = new InvalidOperationException(OrgBackendSecret);
        var exception = Record.Exception(() => sut.SelectedBranch = queryService.Branches[1]);

        Assert.Null(exception);
        Assert.True(sut.HasActionError);
        Assert.Equal(Strings.Common_ActionFailedMessage, sut.ActionErrorMessage);
        Assert.NotEqual(DashboardState.Error, sut.State);
    }

    [Fact]
    public void CreateOrganizationCommand_Failure_LogsOperationNameOnly_NoOrgOrTaxLeak()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var commandService = new StubOrganizationCommandService { CreateOrganizationException = new InvalidOperationException(OrgBackendSecret) };
        var logger = new RecordingLogger<OrganizationPageViewModel>();
        var sut = CreateSut(queryService, commandService, logger: logger);
        sut.NewOrgName = "New Holdings";

        sut.CreateOrganizationCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=CreateOrganizationAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(OrgBackendSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(OrgBackendSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var session = new StubCurrentSessionService { SwitchRoleException = new InvalidOperationException(OrgBackendSecret) };
        var logger = new RecordingLogger<OrganizationPageViewModel>();
        var sut = CreateSut(queryService, sessionService: session, logger: logger);
        sut.SelectedRoleToSwitchTo = WorkspaceRole.Reception;

        sut.SwitchRoleCommand.Execute(null);

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("Operation=SwitchRoleAsync", StringComparison.Ordinal));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains(OrgBackendSecret, StringComparison.Ordinal));
        Assert.DoesNotContain(OrgBackendSecret, sut.ActionErrorMessage ?? string.Empty, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionMatrix_IsUnaffectedByCommandFailures()
    {
        var queryService = new StubOrganizationQueryService { Organizations = { MakeOrg("org-1", "Rojan") } };
        var commandService = new StubOrganizationCommandService { CreateOrganizationException = new InvalidOperationException("boom") };
        var sut = CreateSut(queryService, commandService);
        var matrixBefore = sut.PermissionMatrix.Count;
        sut.NewOrgName = "X";

        sut.CreateOrganizationCommand.Execute(null);

        Assert.Equal(matrixBefore, sut.PermissionMatrix.Count);
        Assert.NotEmpty(sut.PermissionMatrix);
    }

    // -------------------------------------------------------------------------
    // Private nested test doubles (no shared stub touched).
    // -------------------------------------------------------------------------

    private sealed class StubOrganizationQueryService : IOrganizationQueryService
    {
        public List<OrganizationDto> Organizations { get; } = [];

        public List<BranchDto> Branches { get; } = [];

        public BranchSettingsDto? BranchSettings { get; set; }

        public Exception? GetOrganizationsException { get; set; }

        public Exception? GetBranchesException { get; set; }

        public Exception? GetBranchSettingsException { get; set; }

        public Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken cancellationToken = default) =>
            GetOrganizationsException is not null
                ? Task.FromException<IReadOnlyList<OrganizationDto>>(GetOrganizationsException)
                : Task.FromResult<IReadOnlyList<OrganizationDto>>(Organizations);

        public Task<OrganizationDto?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Organizations.FirstOrDefault(o => o.Id == organizationId));

        public Task<IReadOnlyList<BranchDto>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default) =>
            GetBranchesException is not null
                ? Task.FromException<IReadOnlyList<BranchDto>>(GetBranchesException)
                : Task.FromResult<IReadOnlyList<BranchDto>>(Branches.Where(b => b.OrganizationId == organizationId).ToList());

        public Task<BranchDto?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Branches.FirstOrDefault(b => b.Id == branchId));

        public Task<BranchSettingsDto?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default) =>
            GetBranchSettingsException is not null
                ? Task.FromException<BranchSettingsDto?>(GetBranchSettingsException)
                : Task.FromResult(BranchSettings);
    }

    private sealed class StubOrganizationCommandService : IOrganizationCommandService
    {
        public List<string> CreateOrganizationCalls { get; } = [];

        public List<string> CreateBranchCalls { get; } = [];

        public List<BranchSettingsDto> SetBranchSettingsCalls { get; } = [];

        public Exception? CreateOrganizationException { get; set; }

        public Exception? CreateBranchException { get; set; }

        public Exception? SetBranchSettingsException { get; set; }

        public Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default)
        {
            CreateOrganizationCalls.Add(name);
            return CreateOrganizationException is not null
                ? Task.FromException<OrganizationDto>(CreateOrganizationException)
                : Task.FromResult(MakeOrg("org-new", name));
        }

        public Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default) =>
            Task.FromResult(organization);

        public Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default)
        {
            CreateBranchCalls.Add(name);
            return CreateBranchException is not null
                ? Task.FromException<BranchDto>(CreateBranchException)
                : Task.FromResult(MakeBranch("branch-new", organizationId, name));
        }

        public Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default) =>
            Task.FromResult(branch);

        public Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default)
        {
            SetBranchSettingsCalls.Add(settings);
            return SetBranchSettingsException is not null
                ? Task.FromException<BranchSettingsDto>(SetBranchSettingsException)
                : Task.FromResult(settings);
        }
    }

    private sealed class StubCurrentSessionService : ICurrentSessionService
    {
        public WorkspaceRole CurrentRole { get; set; } = WorkspaceRole.PlatformOwner;

        public Exception? SwitchRoleException { get; set; }

        public List<WorkspaceRole> SwitchRoleAttempts { get; } = [];

        public OrganizationDto? CurrentOrganization => null;

        public BranchDto? CurrentBranch => null;

        public DesktopContextState ContextState => DesktopContextState.NoBusinessContext;

        public IReadOnlyList<BranchDto> AvailableBranches => [];

        public IReadOnlyList<string> RecentBranchIds => [];

        public IReadOnlyList<string> FavoriteBranchIds => [];

        public event EventHandler? SessionChanged { add { } remove { } }

        public Task InitializeAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SwitchBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SwitchRoleAsync(WorkspaceRole role, CancellationToken cancellationToken = default)
        {
            SwitchRoleAttempts.Add(role);
            if (SwitchRoleException is not null)
            {
                return Task.FromException(SwitchRoleException);
            }

            CurrentRole = role;
            return Task.CompletedTask;
        }

        public Task ToggleFavoriteBranchAsync(string branchId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }
}
