using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Workspaces;

namespace Rojan.Desktop.Shell.Workspaces;

/// <summary>
/// Concrete <see cref="IFloatingWindowManager"/> - owns the real
/// <see cref="FloatingModuleWindow"/> instances a
/// <c>WorkspaceHostViewModel</c> never touches directly. Lives in Shell
/// (not Presentation) because it constructs real <see cref="Window"/>
/// objects, the same reasoning that keeps the concrete
/// <c>Shell.Navigation.NavigationService</c> out of Presentation.
/// </summary>
public sealed class FloatingWindowManager : IFloatingWindowManager
{
    private readonly IServiceProvider _serviceProvider;
    private readonly Dictionary<string, FloatingModuleWindow> _windows = [];

    public FloatingWindowManager(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public event EventHandler<string>? WindowClosed;

    public void Open(string floatingWindowId, ModuleDescriptor descriptor, double x, double y, double width, double height, bool isMaximized)
    {
        if (_windows.ContainsKey(floatingWindowId))
        {
            return;
        }

        var content = descriptor.CreateViewModel(_serviceProvider);
        var window = new FloatingModuleWindow(descriptor, content)
        {
            Left = x,
            Top = y,
            Width = width,
            Height = height,
            WindowState = isMaximized ? WindowState.Maximized : WindowState.Normal,
        };

        window.Closed += (_, _) => OnWindowClosed(floatingWindowId);
        _windows[floatingWindowId] = window;
        window.Show();
    }

    public void Focus(string floatingWindowId)
    {
        if (!_windows.TryGetValue(floatingWindowId, out var window))
        {
            return;
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    public void Close(string floatingWindowId)
    {
        if (_windows.TryGetValue(floatingWindowId, out var window))
        {
            window.Close();
        }
    }

    public void CloseAll()
    {
        foreach (var window in _windows.Values.ToList())
        {
            window.Close();
        }
    }

    public FloatingWindowGeometry? GetGeometry(string floatingWindowId)
    {
        if (!_windows.TryGetValue(floatingWindowId, out var window))
        {
            return null;
        }

        var isMaximized = window.WindowState == WindowState.Maximized;

        // RestoreBounds is the pre-maximize rectangle - saving that
        // (rather than the maximized-to-fill-the-monitor Left/Top/Width/
        // Height) means restoring a maximized floating window later still
        // remembers its "normal" size/position if the user un-maximizes it.
        var bounds = isMaximized ? window.RestoreBounds : new Rect(window.Left, window.Top, window.Width, window.Height);
        return new FloatingWindowGeometry(bounds.Left, bounds.Top, bounds.Width, bounds.Height, isMaximized);
    }

    private void OnWindowClosed(string floatingWindowId)
    {
        _windows.Remove(floatingWindowId);
        WindowClosed?.Invoke(this, floatingWindowId);
    }
}
