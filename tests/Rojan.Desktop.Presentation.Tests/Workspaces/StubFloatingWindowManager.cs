using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Workspaces;

namespace Rojan.Desktop.Presentation.Tests.Workspaces;

/// <summary>No-op <see cref="IFloatingWindowManager"/> test double that records what was opened/closed, so a test can assert on it without a real <see cref="System.Windows.Window"/>.</summary>
internal sealed class StubFloatingWindowManager : IFloatingWindowManager
{
    public List<string> OpenedIds { get; } = [];

    public List<string> ClosedIds { get; } = [];

    public event EventHandler<string>? WindowClosed;

    public void Open(string floatingWindowId, ModuleDescriptor descriptor, double x, double y, double width, double height, bool isMaximized) =>
        OpenedIds.Add(floatingWindowId);

    public void Focus(string floatingWindowId)
    {
    }

    public void Close(string floatingWindowId) => ClosedIds.Add(floatingWindowId);

    public void CloseAll()
    {
    }

    public FloatingWindowGeometry? GetGeometry(string floatingWindowId) => null;

    public void RaiseWindowClosed(string floatingWindowId) => WindowClosed?.Invoke(this, floatingWindowId);
}
