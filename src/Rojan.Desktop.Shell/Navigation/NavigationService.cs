using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;

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
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<ViewModelBase> _backStack = new();
    private readonly Stack<ViewModelBase> _forwardStack = new();
    private ContentControl? _host;
    private ViewModelBase? _current;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

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

        SetContent(_backStack.Pop());
    }

    public void GoForward()
    {
        if (!CanGoForward)
        {
            return;
        }

        if (_current is not null)
        {
            _backStack.Push(_current);
        }

        SetContent(_forwardStack.Pop());
    }

    private void Navigate(ViewModelBase viewModel)
    {
        if (_current is not null)
        {
            _backStack.Push(_current);
        }

        _forwardStack.Clear();
        SetContent(viewModel);
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
