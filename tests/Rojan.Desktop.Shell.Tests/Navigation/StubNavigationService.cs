using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>No-op <see cref="INavigationService"/> test double - MainWindowViewModel only ever calls into it, never reads state back that a navigation test needs to assert on.</summary>
internal sealed class StubNavigationService : INavigationService
{
    public bool CanGoBack => false;

    public bool CanGoForward => false;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
    }

    public void NavigateTo(ModuleDescriptor descriptor)
    {
    }

    public void GoBack()
    {
    }

    public void GoForward()
    {
    }
}
