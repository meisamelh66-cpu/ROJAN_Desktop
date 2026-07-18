using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
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
/// </summary>
public sealed class NavigationService : INavigationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Stack<ViewModelBase> _backStack = new();
    private ContentControl? _host;
    private ViewModelBase? _current;

    public NavigationService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public bool CanGoBack => _backStack.Count > 0;

    /// <summary>Wires this service to the window's navigation content host. Called once, from <c>MainWindow</c>'s constructor.</summary>
    internal void Attach(ContentControl host)
    {
        _host = host;
    }

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
        var viewModel = _serviceProvider.GetRequiredService<TViewModel>();
        if (_current is not null)
        {
            _backStack.Push(_current);
        }

        SetContent(viewModel);
    }

    public void GoBack()
    {
        if (!CanGoBack)
        {
            return;
        }

        SetContent(_backStack.Pop());
    }

    private void SetContent(ViewModelBase viewModel)
    {
        _current = viewModel;
        if (_host is not null)
        {
            _host.Content = viewModel;
        }
    }
}
