using System.Windows.Controls;

namespace Rojan.Desktop.Presentation.Views.Notifications;

/// <summary>Phase 27: the Notification Center panel's View - pure XAML, no code-behind logic (all state/behavior lives in <c>ViewModels.Notifications.NotificationCenterViewModel</c>, bound via <c>DataContext</c> by whoever hosts this control).</summary>
public partial class NotificationCenterPanelView : UserControl
{
    public NotificationCenterPanelView()
    {
        InitializeComponent();
    }
}
