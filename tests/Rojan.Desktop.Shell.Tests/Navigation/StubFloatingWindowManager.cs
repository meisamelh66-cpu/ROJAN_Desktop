using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Workspaces;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>No-op <see cref="IFloatingWindowManager"/> test double - never opens a real <see cref="System.Windows.Window"/>, same "MainWindowViewModel only ever calls into it" reasoning as <see cref="StubNavigationService"/>.</summary>
internal sealed class StubFloatingWindowManager : IFloatingWindowManager
{
    public event EventHandler<string>? WindowClosed;

    public void Open(string floatingWindowId, ModuleDescriptor descriptor, double x, double y, double width, double height, bool isMaximized)
    {
    }

    public void Focus(string floatingWindowId)
    {
    }

    public void Close(string floatingWindowId)
    {
    }

    public void CloseAll()
    {
    }

    public FloatingWindowGeometry? GetGeometry(string floatingWindowId) => null;

    /// <summary>Lets a test simulate the user closing a floating window via its own title-bar close button.</summary>
    public void RaiseWindowClosed(string floatingWindowId) => WindowClosed?.Invoke(this, floatingWindowId);
}
