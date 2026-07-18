namespace Rojan.Desktop.Shell.Navigation;

/// <summary>Sidebar menu entry - a title and the section it identifies. No page content behind it yet.</summary>
public sealed record NavigationItem(ShellSection Section, string Title);
