using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// Recording <see cref="INavigationService"/> test double. Shell Navigation: records every
/// descriptor navigation/reload so tests can assert that a session change does (or does not)
/// recreate the current page, and lets a test simulate a navigation the shell did not start itself
/// (Dashboard shortcut, Command Palette, Back/Forward) via <see cref="RaiseNavigated"/>.
/// </summary>
internal sealed class StubNavigationService : INavigationService
{
    public List<string> NavigatedModuleIds { get; } = [];

    public List<string> ReloadedModuleIds { get; } = [];

    public bool CanGoBack => false;

    public bool CanGoForward => false;

    public event EventHandler<NavigatedEventArgs>? Navigated;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
    }

    public void NavigateTo(ModuleDescriptor descriptor) => NavigatedModuleIds.Add(descriptor.Metadata.Id);

    public void Reload(ModuleDescriptor descriptor) => ReloadedModuleIds.Add(descriptor.Metadata.Id);

    public void GoBack()
    {
    }

    public void GoForward()
    {
    }

    public void RaiseNavigated(ViewModelBase viewModel, string? moduleId) =>
        Navigated?.Invoke(this, new NavigatedEventArgs(viewModel, moduleId));
}
