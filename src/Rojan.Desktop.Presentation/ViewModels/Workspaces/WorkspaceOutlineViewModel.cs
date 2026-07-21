using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>
/// Phase 29's flagship dockable panel: a live list of every open tab across
/// every secondary pane plus every floating window, with click-to-focus and
/// click-to-close - real, useful content proving the docking system hosts
/// more than empty plumbing. Owned and refreshed by <c>WorkspaceHostViewModel</c>
/// (the only thing that actually tracks the live pane tree/floating window
/// set); <c>focus</c>/<c>close</c> are supplied as callbacks
/// at construction, the same "action map supplied by the opener" shape
/// <c>ViewModels.Search.CommandPaletteViewModel</c> already establishes.
/// </summary>
public sealed class WorkspaceOutlineViewModel : ViewModelBase
{
    public WorkspaceOutlineViewModel(Action<WorkspaceOutlineEntry> focus, Action<WorkspaceOutlineEntry> close)
    {
        FocusCommand = new RelayCommand(parameter => focus((WorkspaceOutlineEntry)parameter!));
        CloseCommand = new RelayCommand(parameter => close((WorkspaceOutlineEntry)parameter!));
    }

    public ObservableCollection<WorkspaceOutlineEntry> Entries { get; } = [];

    public ICommand FocusCommand { get; }

    public ICommand CloseCommand { get; }

    public void Refresh(IEnumerable<WorkspaceOutlineEntry> entries)
    {
        Entries.Clear();
        foreach (var entry in entries)
        {
            Entries.Add(entry);
        }
    }
}
