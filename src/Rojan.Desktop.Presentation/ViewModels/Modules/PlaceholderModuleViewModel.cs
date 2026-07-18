using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Modules;

/// <summary>ViewModel shared by every not-yet-implemented module (see <see cref="Rojan.Desktop.Presentation.Modules.PlaceholderModule"/>). No business logic - a real module replaces this entirely with its own ViewModel/View when implemented.</summary>
public sealed class PlaceholderModuleViewModel : ViewModelBase
{
    public PlaceholderModuleViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }
}
