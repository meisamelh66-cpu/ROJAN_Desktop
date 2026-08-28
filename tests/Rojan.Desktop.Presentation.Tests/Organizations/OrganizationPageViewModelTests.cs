using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Tests.Automation;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;
using Rojan.Desktop.Presentation.ViewModels.Organizations;

namespace Rojan.Desktop.Presentation.Tests.Organizations;

/// <summary>
/// Phase 8.27 Organization Page Logging: the first dedicated
/// <see cref="OrganizationPageViewModel"/> test file - covers the one
/// broad-catch load boundary (<c>LoadAsync</c>) that now logs at Error
/// before surfacing the existing Error state. Reuses
/// <see cref="FakeCurrentSessionService"/> (from the Automation tests) and
/// the real <see cref="PermissionEngine"/>; the two Organization service
/// doubles are private nested stubs since no shared one exists in this
/// assembly.
/// </summary>
public sealed class OrganizationPageViewModelTests
{
    private static OrganizationPageViewModel CreateSut(
        IOrganizationQueryService queryService,
        RecordingLogger<OrganizationPageViewModel>? logger = null) =>
        new(queryService, new NotSupportedOrganizationCommandService(), new PermissionEngine(), new FakeCurrentSessionService(), logger);

    [Fact]
    public void LoadAsync_QueryThrows_LogsError()
    {
        var queryService = new ThrowingOrganizationQueryService(new InvalidOperationException("boom"));
        var logger = new RecordingLogger<OrganizationPageViewModel>();

        var sut = CreateSut(queryService, logger);

        // User-visible behaviour is unchanged - the log is additive.
        Assert.Equal(DashboardState.Error, sut.State);
        Assert.Equal("boom", sut.ErrorMessage);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Error && entry.Message.Contains("LoadAsync", StringComparison.Ordinal));
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows()
    {
        var queryService = new ThrowingOrganizationQueryService(new InvalidOperationException("boom"));

        var exception = Record.Exception(() => CreateSut(queryService));

        Assert.Null(exception);
    }

    /// <summary>Fails <see cref="GetOrganizationsAsync"/> so the ViewModel's LoadAsync catch is exercised; the other members are never reached on that path.</summary>
    private sealed class ThrowingOrganizationQueryService(Exception toThrow) : IOrganizationQueryService
    {
        public Task<IReadOnlyList<OrganizationDto>> GetOrganizationsAsync(CancellationToken cancellationToken = default) =>
            Task.FromException<IReadOnlyList<OrganizationDto>>(toThrow);

        public Task<OrganizationDto?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<OrganizationDto?>(null);

        public Task<IReadOnlyList<BranchDto>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchDto>>([]);

        public Task<BranchDto?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchDto?>(null);

        public Task<BranchSettingsDto?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchSettingsDto?>(null);
    }

    /// <summary>No command is invoked by any load-path test - every member throws to make that explicit.</summary>
    private sealed class NotSupportedOrganizationCommandService : IOrganizationCommandService
    {
        public Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by OrganizationPageViewModelTests.");

        public Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by OrganizationPageViewModelTests.");

        public Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by OrganizationPageViewModelTests.");

        public Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by OrganizationPageViewModelTests.");

        public Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by OrganizationPageViewModelTests.");
    }
}
