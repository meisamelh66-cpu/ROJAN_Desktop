namespace Rojan.Desktop.Presentation.Workspaces;

/// <summary>An open floating window's current position/size/maximized state, as reported by <see cref="IFloatingWindowManager.GetGeometry"/> - lets a saved workspace remember where the user actually left a floating window, not just where it was originally opened.</summary>
public sealed record FloatingWindowGeometry(double X, double Y, double Width, double Height, bool IsMaximized);
