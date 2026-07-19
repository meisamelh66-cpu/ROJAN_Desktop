using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Reporting;

/// <summary>Export Dialog layout. No code-behind logic - DataContext is the resolved ExportDialogViewModel, set by WPF's implicit DataTemplate resolution when Shell shows it via IDialogService.</summary>
public partial class ExportDialogView : UserControl
{
    public ExportDialogView()
    {
        InitializeComponent();
    }
}
