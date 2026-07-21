using System.Windows.Controls;
using System.Windows.Input;
using Rojan.Desktop.Presentation.ViewModels.Workspaces;

namespace Rojan.Desktop.Presentation.Views.Workspaces;

/// <summary>Code-behind for <see cref="PaneLeafView"/>: forwards a click anywhere in the pane into <see cref="PaneLeafViewModel.FocusCommand"/>, and opens the "+" module picker on a left click (its default trigger is right-click, wrong affordance for a visible "+" button) rather than needing a dedicated ContextMenu-opening ViewModel command.</summary>
public partial class PaneLeafView : UserControl
{
    public PaneLeafView()
    {
        InitializeComponent();
        RootBorder.PreviewMouseDown += OnRootBorderPreviewMouseDown;
        AddTabButton.Click += OnAddTabButtonClick;
    }

    private void OnRootBorderPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is PaneLeafViewModel viewModel && viewModel.FocusCommand.CanExecute(null))
        {
            viewModel.FocusCommand.Execute(null);
        }
    }

    private void OnAddTabButtonClick(object sender, System.Windows.RoutedEventArgs e)
    {
        if (AddTabButton.ContextMenu is not null)
        {
            AddTabButton.ContextMenu.PlacementTarget = AddTabButton;
            AddTabButton.ContextMenu.IsOpen = true;
        }
    }
}
