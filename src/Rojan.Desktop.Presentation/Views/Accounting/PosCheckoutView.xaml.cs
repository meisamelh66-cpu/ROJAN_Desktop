using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Accounting;

/// <summary>POS checkout wizard-dialog. No code-behind logic - DataContext is the PosCheckoutViewModel instance shown via IDialogService.</summary>
public partial class PosCheckoutView : UserControl
{
    public PosCheckoutView()
    {
        InitializeComponent();
    }
}
