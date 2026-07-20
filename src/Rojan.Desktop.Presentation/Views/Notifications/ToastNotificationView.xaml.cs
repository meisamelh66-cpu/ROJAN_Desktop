using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Notifications;

/// <summary>Phase 27: one toast popup's View - pure XAML, no code-behind logic (matches its ViewModel, <c>ViewModels.Notifications.ToastNotificationViewModel</c>, which has none either).</summary>
public partial class ToastNotificationView : UserControl
{
    public ToastNotificationView()
    {
        InitializeComponent();
    }
}
