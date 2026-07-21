using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;

namespace Rojan.Desktop.Presentation.Tests.Search;

/// <summary>Records every navigation call so a test can assert on it, without a real navigation host.</summary>
internal sealed class StubNavigationService : INavigationService
{
    public List<ModuleDescriptor> NavigatedDescriptors { get; } = [];

    public bool CanGoBack => false;

    public bool CanGoForward => false;

    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase
    {
    }

    public void NavigateTo(ModuleDescriptor descriptor) => NavigatedDescriptors.Add(descriptor);

    public void GoBack()
    {
    }

    public void GoForward()
    {
    }
}
