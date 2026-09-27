using Rojan.Desktop.Presentation.Modules;
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

    /// <summary>Whether <see cref="GoForward"/> has an entry to return to (only true right after a <see cref="GoBack"/>, cleared by any new navigation).</summary>
    public bool CanGoForward { get; }

    /// <summary>Activates a <typeparamref name="TViewModel"/>, resolved via DI, as the current content.</summary>
    public void NavigateTo<TViewModel>() where TViewModel : ViewModelBase;

    /// <summary>Activates the module described by <paramref name="descriptor"/>, using its own activation factory instead of DI type resolution - see <see cref="ModuleDescriptor"/>. A no-op when that module is already the one displayed, so its page (and any unsaved input on it) is kept rather than recreated.</summary>
    public void NavigateTo(ModuleDescriptor descriptor);

    /// <summary>
    /// Shell Navigation: replaces the displayed content with a fresh instance of
    /// <paramref name="descriptor"/>'s module even when it is the module already displayed - reserved
    /// for a genuine business-context change (branch/role/salon) that the current page's data no
    /// longer reflects. Replaces the current entry rather than pushing it onto the back-stack, so Back
    /// never returns to the stale, previous-context page.
    /// </summary>
    public void Reload(ModuleDescriptor descriptor);

    /// <summary>Returns to the previous entry in the navigation back-stack, if any.</summary>
    public void GoBack();

    /// <summary>Re-applies the entry <see cref="GoBack"/> just left, if any.</summary>
    public void GoForward();

    /// <summary>
    /// Shell Navigation: raised after the displayed content changes, whatever caused it (sidebar,
    /// Dashboard shortcut, Command Palette, Back/Forward). Lets the shell keep the sidebar selection
    /// in sync with what is actually on screen instead of only with its own clicks.
    /// </summary>
    public event EventHandler<NavigatedEventArgs>? Navigated;
}
