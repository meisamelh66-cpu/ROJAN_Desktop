using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Rojan.Desktop.Presentation.ViewModels.Search;

namespace Rojan.Desktop.Presentation.Views.Search;

/// <summary>
/// Phase 28: the Command Palette's View. Full keyboard navigation lives
/// here, self-contained (mirrors <c>Views.Help.ContextHelpDialogView</c>'s
/// own focus-trap code-behind): the search box keeps keyboard focus at
/// all times (typing must always work), so Up/Down/Enter are intercepted
/// on its <see cref="UIElement.PreviewKeyDown"/> and translated into
/// <see cref="CommandPaletteViewModel.SelectPreviousCommand"/>/
/// <see cref="CommandPaletteViewModel.SelectNextCommand"/>/
/// <see cref="CommandPaletteViewModel.ExecuteSelectedCommand"/> calls
/// rather than letting a results <see cref="ListBox"/> steal focus - the
/// same "search box has focus, arrow keys still drive the list"
/// interaction every command-palette-style UI (VS Code's Quick Open,
/// Windows' own Start menu search) already establishes. ESC closes -
/// scoped to this View only via a direct <see cref="ICommand"/> call,
/// not a global handler, so no other dialog's behavior changes.
/// </summary>
public partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        SearchBox.PreviewKeyDown += OnSearchBoxPreviewKeyDown;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        Keyboard.Focus(SearchBox);
    }

    private void OnSearchBoxPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not CommandPaletteViewModel viewModel)
        {
            return;
        }

        switch (e.Key)
        {
            case Key.Down:
                if (viewModel.SelectNextCommand.CanExecute(null))
                {
                    viewModel.SelectNextCommand.Execute(null);
                }

                e.Handled = true;
                break;
            case Key.Up:
                if (viewModel.SelectPreviousCommand.CanExecute(null))
                {
                    viewModel.SelectPreviousCommand.Execute(null);
                }

                e.Handled = true;
                break;
            case Key.Enter:
                if (viewModel.ExecuteSelectedCommand.CanExecute(null))
                {
                    viewModel.ExecuteSelectedCommand.Execute(null);
                }

                e.Handled = true;
                break;
            case Key.Escape:
                viewModel.CloseCommand.Execute(null);
                e.Handled = true;
                break;
        }
    }
}
