using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Infrastructure.Organizations;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Modules;
using Rojan.Desktop.Presentation.ViewModels.Organizations;
using Rojan.Desktop.Shell.Navigation;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Reception Stabilization Sprint: exercises <see cref="NavigationService.NavigateTo{TViewModel}"/>'s
/// new permission check - the fix for the Dashboard chart/quick-action bypass (a direct
/// <c>NavigateTo&lt;ReportingPageViewModel&gt;()</c> call reaching a page the sidebar itself would
/// never show for the current role). <see cref="OrganizationPageViewModel"/> stands in for the
/// three gated destinations here since it is the lightest of the three to construct for real
/// (four simple dependencies, all faked/real-and-pure below) - the permission map itself is a
/// small static table in <see cref="NavigationService"/>, so exercising one gated entry and one
/// ungated entry is sufficient to cover the map-lookup logic.
/// </summary>
public sealed class NavigationServiceTests
{
    private static NavigationService CreateSut(IServiceProvider serviceProvider, WorkspaceRole role) =>
        new(serviceProvider, new PermissionEngine(), new StubCurrentSessionService { CurrentRole = role });

    private static ServiceProvider CreateServiceProviderWithOrganizationPageViewModel()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOrganizationQueryService>(new OrganizationQueryService(new FakeOrganizationRepository()));
        services.AddSingleton<IOrganizationCommandService, StubOrganizationCommandService>();
        services.AddSingleton<IPermissionEngine, PermissionEngine>();
        services.AddSingleton<ICurrentSessionService>(new StubCurrentSessionService());
        services.AddTransient<OrganizationPageViewModel>();
        return services.BuildServiceProvider();
    }

    [Fact]
    public void NavigateTo_TargetRequiresUngrantedPermission_DoesNotResolveOrNavigate()
    {
        // OrganizationPageViewModel is deliberately NOT registered here - if the permission
        // check failed to short-circuit, GetRequiredService would throw, failing this test.
        var sut = CreateSut(new ServiceCollection().BuildServiceProvider(), WorkspaceRole.Reception);

        var exception = Record.Exception(() => sut.NavigateTo<OrganizationPageViewModel>());

        Assert.Null(exception);
        Assert.False(sut.CanGoBack);
    }

    [Fact]
    public void NavigateTo_TargetRequiresGrantedPermission_NavigatesNormally()
    {
        var sut = CreateSut(CreateServiceProviderWithOrganizationPageViewModel(), WorkspaceRole.PlatformOwner);

        sut.NavigateTo<OrganizationPageViewModel>();
        sut.NavigateTo<OrganizationPageViewModel>();

        // A second successful navigation pushes the first onto the back-stack - the only
        // public signal available that navigation actually happened both times (CanGoBack
        // would stay false if the second call were silently blocked).
        Assert.True(sut.CanGoBack);
    }

    [Fact]
    public void NavigateTo_TargetHasNoRequiredPermission_NavigatesRegardlessOfRole()
    {
        var services = new ServiceCollection();
        services.AddTransient(_ => new PlaceholderModuleViewModel("placeholder"));
        var sut = CreateSut(services.BuildServiceProvider(), WorkspaceRole.Support);

        sut.NavigateTo<PlaceholderModuleViewModel>();
        sut.NavigateTo<PlaceholderModuleViewModel>();

        Assert.True(sut.CanGoBack);
    }

    private sealed class StubOrganizationCommandService : IOrganizationCommandService
    {
        public Task<OrganizationDto> CreateOrganizationAsync(string name, string legalName, string taxInformation, SubscriptionPlan subscription, string code, string phone, string email, string address, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by NavigationServiceTests - OrganizationPageViewModel is only constructed here, never driven to a command.");

        public Task<OrganizationDto> UpdateOrganizationAsync(OrganizationDto organization, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by NavigationServiceTests - OrganizationPageViewModel is only constructed here, never driven to a command.");

        public Task<BranchDto> CreateBranchAsync(string organizationId, string name, string code, string address, string phone, string email, string manager, string timeZone, string currency, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by NavigationServiceTests - OrganizationPageViewModel is only constructed here, never driven to a command.");

        public Task<BranchDto> UpdateBranchAsync(BranchDto branch, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by NavigationServiceTests - OrganizationPageViewModel is only constructed here, never driven to a command.");

        public Task<BranchSettingsDto> SetBranchSettingsAsync(BranchSettingsDto settings, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not exercised by NavigationServiceTests - OrganizationPageViewModel is only constructed here, never driven to a command.");
    }
}
