using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>
/// Shell Navigation: payload of <see cref="INavigationService.Navigated"/> - the content that is now
/// displayed, plus the id of the module it belongs to when the navigation service knows it (a
/// <see cref="INavigationService.NavigateTo(Modules.ModuleDescriptor)"/> navigation, or a
/// back/forward step onto one). <see langword="null"/> for a typed
/// <see cref="INavigationService.NavigateTo{TViewModel}"/> navigation - subscribers resolve those by
/// <see cref="Modules.ModuleDescriptor.ViewModelType"/> instead.
/// </summary>
public sealed class NavigatedEventArgs(ViewModelBase viewModel, string? moduleId) : EventArgs
{
    public ViewModelBase ViewModel { get; } = viewModel;

    public string? ModuleId { get; } = moduleId;
}
