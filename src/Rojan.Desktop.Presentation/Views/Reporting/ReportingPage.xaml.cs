using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Rojan.Desktop.Presentation.ViewModels.Reporting;

namespace Rojan.Desktop.Presentation.Views.Reporting;

/// <summary>
/// Reporting page layout. The only code-behind logic is rebuilding
/// <see cref="ResultsDataGrid"/>'s columns whenever
/// <see cref="ReportingPageViewModel.CurrentResult"/> changes - report
/// columns are report-specific, so they can't be declared statically in
/// XAML (see the DataGrid's own doc comment).
/// </summary>
public partial class ReportingPage : UserControl
{
    public ReportingPage()
    {
        InitializeComponent();
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is ReportingPageViewModel oldViewModel)
            {
                oldViewModel.PropertyChanged -= OnViewModelPropertyChanged;
            }

            if (e.NewValue is ReportingPageViewModel newViewModel)
            {
                newViewModel.PropertyChanged += OnViewModelPropertyChanged;
            }
        };
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ReportingPageViewModel.CurrentResult) || sender is not ReportingPageViewModel viewModel)
        {
            return;
        }

        ResultsDataGrid.Columns.Clear();
        if (viewModel.CurrentResult is null)
        {
            return;
        }

        foreach (var column in viewModel.CurrentResult.Columns)
        {
            ResultsDataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = column.Header,
                Binding = new Binding($"Values[{column.Key}]"),
            });
        }
    }
}
