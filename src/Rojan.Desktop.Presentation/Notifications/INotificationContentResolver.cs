using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>Phase 27: Enterprise Notification Center. The one place a <see cref="NotificationDto"/>'s resx keys are expanded into actual localized text - see <see cref="ResolvedNotification"/>'s own doc comment for why this has to live in Presentation.</summary>
public interface INotificationContentResolver
{
    public ResolvedNotification Resolve(NotificationDto notification);
}
