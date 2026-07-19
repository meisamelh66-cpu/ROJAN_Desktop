using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Calendar;

/// <summary>Daily availability layout. No code-behind logic - DataContext is the resolved CalendarPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class CalendarPage : UserControl
{
    public CalendarPage()
    {
        InitializeComponent();
    }
}
