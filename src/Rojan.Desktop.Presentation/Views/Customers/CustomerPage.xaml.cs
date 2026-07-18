using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Customers;

/// <summary>Customer CRM layout. No code-behind logic - DataContext is the resolved CustomerPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class CustomerPage : UserControl
{
    public CustomerPage()
    {
        InitializeComponent();
    }
}
