using System.Runtime.CompilerServices;
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

    // ---------------------------------------------------------------------
    // Phase 8.6 - Navigation BackStack Hardening: bounded _backStack with
    // FIFO (oldest-first) eviction, capped at NavigationService.MaxBackStackDepth.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Registers a fresh, individually-identifiable <see cref="PlaceholderModuleViewModel"/>
    /// per resolution (Title = "page-0", "page-1", ...) so back-stack contents and
    /// eviction order can be asserted precisely. Placeholder pages have no required
    /// permission, so <see cref="NavigationService.NavigateTo{TViewModel}"/> always navigates.
    /// </summary>
    private static NavigationService CreateSutWithSequentialPlaceholderPages()
    {
        var next = 0;
        var services = new ServiceCollection();
        services.AddTransient(_ => new PlaceholderModuleViewModel($"page-{next++}"));
        return CreateSut(services.BuildServiceProvider(), WorkspaceRole.Support);
    }

    private static string CurrentTitle(NavigationService sut) =>
        ((PlaceholderModuleViewModel)sut.Current!).Title;

    [Fact]
    public void Navigate_ExceedsMaxDepth_BackStackDepthNeverExceedsCap()
    {
        var sut = CreateSutWithSequentialPlaceholderPages();

        for (var i = 0; i < NavigationService.MaxBackStackDepth + 10; i++)
        {
            sut.NavigateTo<PlaceholderModuleViewModel>();
            Assert.True(
                sut.BackStackDepth <= NavigationService.MaxBackStackDepth,
                $"BackStackDepth {sut.BackStackDepth} exceeded cap {NavigationService.MaxBackStackDepth} after {i + 1} navigations");
        }

        Assert.Equal(NavigationService.MaxBackStackDepth, sut.BackStackDepth);
    }

    [Fact]
    public void Navigate_ExceedsMaxDepth_EvictsOldestEntryFirst()
    {
        var sut = CreateSutWithSequentialPlaceholderPages();

        // Navigates page-0 .. page-21. At cap (20) after page-20 is pushed;
        // navigating page-21 pushes page-20 and evicts page-0 from the bottom.
        for (var i = 0; i < NavigationService.MaxBackStackDepth + 2; i++)
        {
            sut.NavigateTo<PlaceholderModuleViewModel>();
        }

        var reachedViaBack = new List<string>();
        while (sut.CanGoBack)
        {
            sut.GoBack();
            reachedViaBack.Add(CurrentTitle(sut));
        }

        Assert.Equal(NavigationService.MaxBackStackDepth, reachedViaBack.Count);
        Assert.Equal("page-20", reachedViaBack[0]);   // newest below current - popped first
        Assert.Equal("page-1", reachedViaBack[^1]);    // oldest still retained
        Assert.DoesNotContain("page-0", reachedViaBack); // oldest entry was evicted
    }

    [Fact]
    public void Navigate_ExceedsMaxDepth_EvictedViewModelIsReleasedForCollection()
    {
        var sut = CreateSutWithSequentialPlaceholderPages();

        var evicted = NavigateOnceAndWeaklyReferenceCurrent(sut);

        // Push well past the cap so the first-navigated page falls off the bottom.
        for (var i = 0; i < NavigationService.MaxBackStackDepth + 5; i++)
        {
            sut.NavigateTo<PlaceholderModuleViewModel>();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        Assert.False(evicted.IsAlive, "Evicted back-stack ViewModel is still reachable - FIFO eviction did not drop the reference");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference NavigateOnceAndWeaklyReferenceCurrent(NavigationService sut)
    {
        sut.NavigateTo<PlaceholderModuleViewModel>();
        return new WeakReference(sut.Current);
    }

    [Fact]
    public void GoBack_AfterEviction_WalksEveryRetainedEntryThenStopsWithoutThrowing()
    {
        var sut = CreateSutWithSequentialPlaceholderPages();

        for (var i = 0; i < NavigationService.MaxBackStackDepth + 3; i++)
        {
            sut.NavigateTo<PlaceholderModuleViewModel>();
        }

        var backSteps = 0;
        var exception = Record.Exception(() =>
        {
            while (sut.CanGoBack)
            {
                sut.GoBack();
                backSteps++;
            }
        });

        Assert.Null(exception);
        Assert.Equal(NavigationService.MaxBackStackDepth, backSteps);
        Assert.False(sut.CanGoBack);
    }

    [Fact]
    public void GoForward_AfterGoBack_RestoresThePageSteppedBackFrom()
    {
        var sut = CreateSutWithSequentialPlaceholderPages();

        sut.NavigateTo<PlaceholderModuleViewModel>(); // page-0
        sut.NavigateTo<PlaceholderModuleViewModel>(); // page-1
        sut.NavigateTo<PlaceholderModuleViewModel>(); // page-2 (current)

        sut.GoBack();
        Assert.Equal("page-1", CurrentTitle(sut));
        Assert.True(sut.CanGoForward);

        sut.GoForward();
        Assert.Equal("page-2", CurrentTitle(sut));
        Assert.False(sut.CanGoForward);
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
