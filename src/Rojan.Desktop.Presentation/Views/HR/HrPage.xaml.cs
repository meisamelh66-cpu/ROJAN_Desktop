using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.HR;

/// <summary>HR Dashboard/Employees/Attendance/Shifts/Leave/Commission/Payroll layout. No code-behind logic - DataContext is the resolved HrPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class HrPage : UserControl
{
    public HrPage()
    {
        InitializeComponent();
    }
}
