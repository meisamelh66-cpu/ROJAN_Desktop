using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Presentation.Workspaces;

/// <summary>
/// Opens/closes real OS windows for detached ("floated out") modules.
/// ViewModels depend on this abstraction only - never on <c>Window</c> -
/// same "ViewModel-first, WPF type behind an interface" shape
/// <see cref="Navigation.INavigationService"/> already established for the
/// primary content region. The concrete implementation lives in Shell (it
/// has to construct real <c>Window</c> instances), registered as a DI
/// singleton - see <c>Shell.Workspaces.FloatingWindowManager</c>.
/// </summary>
public interface IFloatingWindowManager
{
    /// <summary>Fires when the user closes a floating window directly (its own title-bar close button), so <c>WorkspaceHostViewModel</c> can drop it from the saved layout without the caller having to poll.</summary>
    public event EventHandler<string>? WindowClosed;

    /// <summary>Opens a floating window for <paramref name="descriptor"/>, identified by <paramref name="floatingWindowId"/> - the same id persisted in <c>FloatingWindowDto.Id</c>, so a saved workspace's floating windows can be reopened at their saved position/size on restore. A no-op if a window with that id is already open (use <see cref="Focus"/> to bring an existing one to front instead).</summary>
    public void Open(string floatingWindowId, ModuleDescriptor descriptor, double x, double y, double width, double height, bool isMaximized);

    /// <summary>Brings an already-open floating window to the foreground - the Workspace Outline panel's "focus" action for a floating entry.</summary>
    public void Focus(string floatingWindowId);

    public void Close(string floatingWindowId);

    /// <summary>Closes every open floating window - used by "Reset Workspace" and when switching to a different workspace.</summary>
    public void CloseAll();

    /// <summary>The current position/size/maximized state of an open floating window - <see langword="null"/> if it isn't open. Read just before persisting a workspace, so a window the user moved/resized saves at its actual current geometry rather than wherever it was first opened.</summary>
    public FloatingWindowGeometry? GetGeometry(string floatingWindowId);
}
