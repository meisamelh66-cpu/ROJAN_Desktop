using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Accounting;

/// <summary>Invoice list/detail layout. No code-behind logic - DataContext is the resolved AccountingPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class AccountingPage : UserControl
{
    public AccountingPage()
    {
        InitializeComponent();
    }
}
