using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Inventory;

/// <summary>Product catalog list/detail layout. No code-behind logic - DataContext is the resolved InventoryPageViewModel, set by WPF's implicit DataTemplate resolution.</summary>
public partial class InventoryPage : UserControl
{
    public InventoryPage()
    {
        InitializeComponent();
    }
}
