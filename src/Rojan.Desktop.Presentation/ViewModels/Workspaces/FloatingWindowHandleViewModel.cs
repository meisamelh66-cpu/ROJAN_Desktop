using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>A lightweight handle to a real floating OS window - just enough to list/focus/close it from the Workspace Outline panel. The actual <c>Window</c> is owned by <c>Shell.Workspaces.FloatingWindowManager</c>, never by this ViewModel.</summary>
public sealed class FloatingWindowHandleViewModel : ViewModelBase
{
    public FloatingWindowHandleViewModel(string id, string moduleId, string title)
    {
        Id = id;
        ModuleId = moduleId;
        Title = title;
    }

    public string Id { get; }

    public string ModuleId { get; }

    public string Title { get; }
}
