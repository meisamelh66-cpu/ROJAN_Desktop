using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Rojan.Desktop.Application.Workspaces;
using Rojan.Desktop.Presentation.ViewModels.Workspaces;

namespace Rojan.Desktop.Presentation.Views.Workspaces;

/// <summary>
/// Code-behind for <see cref="PaneSplitView"/>. Builds <see cref="RootGrid"/>'s
/// row/column structure and its two <see cref="ContentPresenter"/>s once,
/// from the bound <see cref="PaneSplitViewModel"/>'s Orientation/Ratio - see
/// the View's own doc comment for why this isn't done via XAML triggers.
/// The gutter's drag-to-resize reads the two definitions' actual pixel
/// sizes once dragging stops and turns that back into a 0..1 ratio via
/// <see cref="PaneSplitViewModel.ResizeCommand"/> - <see cref="GridSplitter"/>
/// itself only ever manipulates the raw <see cref="GridLength"/> values, it
/// has no concept of "ratio".
/// </summary>
public partial class PaneSplitView : UserControl
{
    private bool _isBuilt;

    public PaneSplitView()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Build();
    }

    private void Build()
    {
        if (_isBuilt || DataContext is not PaneSplitViewModel viewModel)
        {
            return;
        }

        _isBuilt = true;

        var firstPresenter = new ContentPresenter { Content = viewModel.First };
        var secondPresenter = new ContentPresenter { Content = viewModel.Second };
        var splitter = new GridSplitter
        {
            Background = (Brush)FindResource("Rojan.Brush.Border"),
            ResizeBehavior = GridResizeBehavior.PreviousAndNext,
            ShowsPreview = false,
        };
        splitter.DragCompleted += (_, _) => OnDragCompleted(viewModel);

        if (viewModel.Orientation == PaneOrientation.Vertical)
        {
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(viewModel.Ratio, GridUnitType.Star), MinHeight = 80 });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1 - viewModel.Ratio, GridUnitType.Star), MinHeight = 80 });

            splitter.Height = 6;
            splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
            splitter.VerticalAlignment = VerticalAlignment.Center;
            splitter.Cursor = Cursors.SizeNS;

            Grid.SetRow(firstPresenter, 0);
            Grid.SetRow(splitter, 1);
            Grid.SetRow(secondPresenter, 2);
        }
        else
        {
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(viewModel.Ratio, GridUnitType.Star), MinWidth = 120 });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1 - viewModel.Ratio, GridUnitType.Star), MinWidth = 120 });

            splitter.Width = 6;
            splitter.HorizontalAlignment = HorizontalAlignment.Center;
            splitter.VerticalAlignment = VerticalAlignment.Stretch;
            splitter.Cursor = Cursors.SizeWE;

            Grid.SetColumn(firstPresenter, 0);
            Grid.SetColumn(splitter, 1);
            Grid.SetColumn(secondPresenter, 2);
        }

        RootGrid.Children.Add(firstPresenter);
        RootGrid.Children.Add(splitter);
        RootGrid.Children.Add(secondPresenter);
    }

    private void OnDragCompleted(PaneSplitViewModel viewModel)
    {
        double ratio;
        if (viewModel.Orientation == PaneOrientation.Vertical)
        {
            var total = RootGrid.RowDefinitions[0].ActualHeight + RootGrid.RowDefinitions[2].ActualHeight;
            ratio = total > 0 ? RootGrid.RowDefinitions[0].ActualHeight / total : viewModel.Ratio;
        }
        else
        {
            var total = RootGrid.ColumnDefinitions[0].ActualWidth + RootGrid.ColumnDefinitions[2].ActualWidth;
            ratio = total > 0 ? RootGrid.ColumnDefinitions[0].ActualWidth / total : viewModel.Ratio;
        }

        if (viewModel.ResizeCommand.CanExecute(ratio))
        {
            viewModel.ResizeCommand.Execute(ratio);
        }
    }
}
