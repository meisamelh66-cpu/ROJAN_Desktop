using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.BookingWorkflow;

/// <summary>Booking Wizard dialog layout. No code-behind logic - DataContext is the BookingWizardViewModel set by IDialogService.ShowDialog, resolved to this View by WPF's implicit DataTemplate mechanism (same as page navigation).</summary>
public partial class BookingWizardView : UserControl
{
    public BookingWizardView()
    {
        InitializeComponent();
    }
}
