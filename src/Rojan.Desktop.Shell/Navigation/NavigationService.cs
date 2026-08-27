using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Automation;
using Rojan.Desktop.Presentation.ViewModels.Organizations;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Shell.Navigation;

/// <summary>
/// Concrete <see cref="INavigationService"/> backed by a <see cref="ContentControl"/>
/// supplied by <c>MainWindow</c>, using WPF's implicit DataTemplate-per-
/// ViewModel resolution: setting <see cref="ContentControl.Content"/> to a
/// ViewModel instance renders whatever View is registered for that
/// ViewModel's type via a DataTemplate. Lives in Shell (not Presentation)
/// because it depends on the concrete <see cref="ContentControl"/> host -
/// ViewModels only ever see it through <see cref="INavigationService"/>.
///
/// History is standard browser-style: navigating to something new pushes
/// the current entry onto the back-stack and clears the forward-stack;
/// GoBack pushes the current entry onto the forward-stack; GoForward
/// pushes it back onto the back-stack. Only GoBack/GoForward move entries
/// between the two stacks - a fresh NavigateTo always clears "forward",
/// exactly like a web browser abandons forward history once you click a
/// new link after going back.
///
/// Phase 8.6 (Navigation BackStack Hardening): the back-stack is bounded to
/// <see cref="MaxBackStackDepth"/> entries with FIFO eviction - when a push
/// would exceed the cap, the oldest entry (the bottom of the stack) is
/// discarded first. This keeps a long-running session's retained
/// <see cref="ViewModelBase"/> count fixed regardless of how many total
/// navigations occur, at the disclosed cost of bounded Back-history depth
/// (the evicted oldest page is permanently unreachable via <see cref="GoBack"/>,
/// not merely deferred). <see cref="System.Collections.Generic.Stack{T}"/> has
/// no O(1) remove-from-bottom, so <see cref="_backStack"/> is a
/// <see cref="LinkedList{T}"/> used as a deque: <c>AddLast</c>/<c>RemoveLast</c>
/// are the push/pop of top-of-stack, <c>RemoveFirst</c> is the eviction of the
/// oldest. <see cref="_forwardStack"/> is left an ordinary
/// <see cref="System.Collections.Generic.Stack{T}"/> - it is cleared on every
/// fresh <see cref="Navigate"/> and so is already self-bounding by ordinary
/// usage.
///
/// Reception Stabilization Sprint: <see cref="NavigateTo{TViewModel}"/> is
/// now permission-checked - previously the sidebar's own
/// <c>MainWindowViewModel.BuildVisibleNavigationItems</c> filter was the
/// only permission gate in the app, and it only ever guards
/// <see cref="NavigateTo(ModuleDescriptor)"/> (sidebar clicks, always
/// called with an already-filtered <see cref="ModuleDescriptor"/>) - a
/// direct <see cref="NavigateTo{TViewModel}"/> call (e.g. a Dashboard
/// chart/quick-action click) bypassed it entirely.
/// <see cref="RequiredPermissionsByViewModelType"/> is a small, explicit
/// map rather than one derived by invoking every gated module's
/// <see cref="ModuleDescriptor.CreateViewModel"/> to learn its type - that
/// would construct real page ViewModels (several of which fire off a
/// background load from their constructor) purely to inspect their
/// <see cref="Type"/>, an avoidable side effect. Must be kept in sync with
/// each module's own <see cref="ModuleMetadata.RequiredPermission"/>
/// declaration (currently just <see cref="AutomationModule"/>/
/// <see cref="OrganizationModule"/>/<see cref="ReportingModule"/>) -
/// covered by <c>NavigationServiceTests</c>.
/// </summary>
public sealed class NavigationService : INavigationService
{
    private static readonly Dictionary<Type, Permission> RequiredPermissionsByViewModelType = new()
    {
        [typeof(AutomationPageViewModel)] = Permission.AutomationView,
        [typeof(OrganizationPageViewModel)] = Permission.OrganizationManage,
        [typeof(ReportingPageViewModel)] = Permission.ReportingView,
    };

    /// <summary>
    /// Maximum number of entries retained in <see cref="_backStack"/>. Deep
    /// enough that realistic step-back-a-handful-of-pages navigation is never
    /// truncated in practice; small enough to bound worst-case retained memory
    /// to a fixed small multiple of one page's footprint. Not derived from a
    /// measured memory budget - a reasonable default (Phase 8.6), tunable here.
    /// </summary>
    internal const int MaxBackStackDepth = 20;

    private readonly IServiceProvider _serviceProvider;
    private readonly IPermissionEngine _permissionEngine;
    private readonly ICurrentSessionService _currentSessionService;
    private readonly LinkedList<ViewModelBase> _backStack = new();
    private readonly Stack<ViewModelBase> _forwardStack = new();
    private ContentControl? _host;
    private ViewModelBase? _current;

    public NavigationService(
        IServiceProvider serviceProvider,
        IPermissionEngine permissionEngine,
        ICurrentSessionService currentSessionService)
    {
        _serviceProvider = serviceProvider;
        _permissionEngine = permissionEngine;
        _currentSessionService = currentSessionService;
    }

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    /// <summary>
    /// Test-only seam (mirrors <see cref="CanGoBack"/>'s role in
    /// <c>NavigationServiceTests</c>): the current <see cref="_backStack"/>
    /// entry count, so the cap and FIFO eviction (Phase 8.6) can be asserted
    /// directly without a public API for internal history depth.
    /// </summary>
    internal int BackStackDepth => _backStack.Count;

    /// <summary>
    /// Test-only seam: the currently-displayed ViewModel, so tests can assert
    /// exactly which entry <see cref="GoBack"/>/<see cref="GoForward"/> land on
    /// after FIFO eviction (Phase 8.6) without a public accessor for it.
    /// </summary>
    internal ViewModelBase? Current => _current;

    /// <summary>
    /// Wires this service to the window's navigation content host. Called
    /// once, from <c>MainWindow</c>'s constructor - by which point
    /// <see cref="NavigateTo{TViewModel}"/> may already have run once
    /// (<c>MainWindowViewModel</c>'s own constructor navigates on initial
    /// selection, and DI resolves it before <c>MainWindow</c>'s constructor
    /// body executes). Previously this meant the pending navigation was
    /// silently dropped - <see cref="_host"/> was still null when
    /// <see cref="NavigateTo{TViewModel}"/> ran, so <see cref="SetContent"/>
    /// had nothing to assign to, and nothing ever re-applied it once
    /// attached. Fixed here: any pending <see cref="_current"/> is applied
    /// (deferred - see <see cref="ApplyContent"/>) as soon as the host is
    /// available.
    /// </summary>
    internal void Attach(ContentControl host)
    {
        _host = host;
        if (_current is not null)
        {
            ApplyContent(host, _current);
        }
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        if (RequiredPermissionsByViewModelType.TryGetValue(typeof(TViewModel), out var requiredPermission)
            && !_permissionEngine.HasPermission(_currentSessionService.CurrentRole, requiredPermission))
        {
            return;
        }

        Navigate(_serviceProvider.GetRequiredService<TViewModel>());
    }

    public void NavigateTo(ModuleDescriptor descriptor)
    {
        Navigate(descriptor.CreateViewModel(_serviceProvider));
    }

    public void GoBack()
    {
        if (!CanGoBack)
        {
            return;
        }

        if (_current is not null)
        {
            _forwardStack.Push(_current);
        }

        SetContent(PopBackStack());
    }

    public void GoForward()
    {
        if (!CanGoForward)
        {
            return;
        }

        if (_current is not null)
        {
            PushBackStack(_current);
        }

        SetContent(_forwardStack.Pop());
    }

    private void Navigate(ViewModelBase viewModel)
    {
        if (_current is not null)
        {
            PushBackStack(_current);
        }

        _forwardStack.Clear();
        SetContent(viewModel);
    }

    /// <summary>
    /// Pushes <paramref name="viewModel"/> onto the top of the bounded
    /// back-stack. If the stack is already at <see cref="MaxBackStackDepth"/>,
    /// the oldest entry (bottom) is evicted first (FIFO) - so the count never
    /// exceeds the cap and the evicted <see cref="ViewModelBase"/> is no longer
    /// referenced by this service.
    /// </summary>
    private void PushBackStack(ViewModelBase viewModel)
    {
        if (_backStack.Count >= MaxBackStackDepth)
        {
            _backStack.RemoveFirst();
        }

        _backStack.AddLast(viewModel);
    }

    /// <summary>
    /// Pops the most-recent entry off the top of the bounded back-stack. Callers
    /// must check <see cref="CanGoBack"/> first (as <see cref="GoBack"/> does).
    /// </summary>
    private ViewModelBase PopBackStack()
    {
        var top = _backStack.Last!.Value;
        _backStack.RemoveLast();
        return top;
    }

    private void SetContent(ViewModelBase viewModel)
    {
        _current = viewModel;
        if (_host is not null)
        {
            ApplyContent(_host, viewModel);
        }
    }

    /// <summary>
    /// Assigns <see cref="ContentControl.Content"/> on the Dispatcher queue
    /// at <see cref="DispatcherPriority.Loaded"/> instead of synchronously,
    /// so it runs after the window's own initial layout/render rather than
    /// while <c>MainWindow</c> is still under construction. This is a
    /// defensive precaution, not a confirmed fix: the Phase 06A.1
    /// investigation into an intermittent, silent, several-seconds-delayed
    /// process exit could not establish a deterministic root cause -
    /// synchronous assignment was one candidate, tested and not reliably
    /// reproducible as the sole trigger, but avoiding it here is cheap and
    /// has no downside. See the Phase 06A.1 commit message for the full
    /// investigation record.
    /// </summary>
    private static void ApplyContent(ContentControl host, ViewModelBase viewModel)
    {
        host.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => host.Content = viewModel));
    }
}
