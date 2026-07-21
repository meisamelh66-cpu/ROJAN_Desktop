using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Workspaces;

/// <summary>
/// One pane in the secondary-pane tree: a tab strip of open modules, at
/// most one active. Reused across rebuilds (see <c>WorkspaceHostViewModel</c>'s
/// leaf cache) so its <see cref="Tabs"/>' content ViewModels never get
/// needlessly recreated by an unrelated structural change elsewhere in the
/// tree. <see cref="SplitRightCommand"/>/<see cref="SplitDownCommand"/>/
/// <see cref="ClosePaneCommand"/>/<see cref="FocusCommand"/>/<see cref="AddTabCommand"/>
/// are supplied by <c>WorkspaceHostViewModel</c> at construction, each
/// closing over this pane's id - same "commands closed over identity"
/// shape <see cref="TabViewModel"/> establishes.
/// </summary>
public sealed class PaneLeafViewModel : ViewModelBase
{
    private TabViewModel? _activeTab;
    private bool _isFocused;

    public PaneLeafViewModel(string id, ICommand splitRightCommand, ICommand splitDownCommand, ICommand closePaneCommand, ICommand focusCommand, ICommand addTabCommand)
    {
        Id = id;
        Tabs = [];
        SplitRightCommand = splitRightCommand;
        SplitDownCommand = splitDownCommand;
        ClosePaneCommand = closePaneCommand;
        FocusCommand = focusCommand;
        AddTabCommand = addTabCommand;
    }

    public string Id { get; }

    public ObservableCollection<TabViewModel> Tabs { get; }

    public TabViewModel? ActiveTab
    {
        get => _activeTab;
        set
        {
            if (SetProperty(ref _activeTab, value))
            {
                foreach (var tab in Tabs)
                {
                    tab.IsActive = ReferenceEquals(tab, value);
                }
            }
        }
    }

    /// <summary>Whether this is the pane keyboard shortcuts (Ctrl+W, Ctrl+Tab, Ctrl+Shift+F, ...) currently act on - set by the pane's own click handler via <see cref="FocusCommand"/>.</summary>
    public bool IsFocused
    {
        get => _isFocused;
        set => SetProperty(ref _isFocused, value);
    }

    public ICommand SplitRightCommand { get; }

    public ICommand SplitDownCommand { get; }

    public ICommand ClosePaneCommand { get; }

    public ICommand FocusCommand { get; }

    /// <summary>Opens a module chosen from this pane's "+" module picker as a new tab. Parameter: the chosen <c>ModuleDescriptor</c>.</summary>
    public ICommand AddTabCommand { get; }
}
