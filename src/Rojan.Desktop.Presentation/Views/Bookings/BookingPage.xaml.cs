using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Bookings;

/// <summary>Booking list/detail layout. No code-behind logic - DataContext is the resolved BookingPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class BookingPage : UserControl
{
    public BookingPage()
    {
        InitializeComponent();
    }
}
