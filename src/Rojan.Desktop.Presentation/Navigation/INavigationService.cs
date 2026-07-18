using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.Navigation;

/// <summary>
/// ViewModel-first navigation. ViewModels depend on this abstraction only -
/// never on <c>Frame</c>, <c>Page</c>, or any other WPF navigation type -
/// so they stay testable and the dependency-inversion boundary with the
/// concrete (Shell-provided) implementation stays real, not just nominal.
/// </summary>
public interface INavigationService
{
    /// <summary>Whether <see cref="GoBack"/> has a prior entry to return to.</summary>
    public bool CanGoBack { get; }

    /// <summary>Activates a <typeparamref name="TViewModel"/>, resolved via DI, as the current content.</summary>
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>Returns to the previous entry in the navigation back-stack, if any.</summary>
    public void GoBack();
}
