using System.Globalization;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Localization;

namespace Rojan.Desktop.Presentation.Notifications;

/// <summary>
/// Default <see cref="INotificationContentResolver"/>. Resolves
/// <see cref="NotificationDto.TitleKey"/>/<see cref="NotificationDto.MessageKey"/>
/// via <see cref="Strings.GetByKey"/> (the same dynamic-key mechanism
/// Phase 26 introduced), formatting <see cref="NotificationDto.MessageArgs"/>
/// into the message template with <see cref="string.Format(IFormatProvider, string, object[])"/>.
/// Category/Group labels resolve through a small, explicit map for the
/// categories this phase's own producers use
/// (<c>system</c>/<c>customers</c>/<c>bookings</c>/<c>inventory</c>/<c>sync</c>),
/// falling back to the raw category string for anything else - the same
/// "flagship subset now, honest fallback for the rest" shape Phase 22A/
/// 23/24/26 already established, since a category coined by some future
/// module cannot have a resx entry today.
/// </summary>
public sealed class NotificationContentResolver : INotificationContentResolver
{
    public ResolvedNotification Resolve(NotificationDto notification)
    {
        var title = Strings.GetByKey(notification.TitleKey);
        var messageTemplate = Strings.GetByKey(notification.MessageKey);
        var message = notification.MessageArgs.Count == 0
            ? messageTemplate
            : string.Format(CultureInfo.CurrentCulture, messageTemplate, [.. notification.MessageArgs]);

        return new ResolvedNotification(
            Id: notification.Id,
            Severity: notification.Severity,
            Priority: notification.Priority,
            Title: title,
            Message: message,
            CategoryLabel: ResolveCategoryLabel(notification.Category),
            GroupLabel: ResolveCategoryLabel(notification.GroupKey),
            CreatedAt: notification.CreatedAt,
            IsRead: notification.IsRead);
    }

    private static string ResolveCategoryLabel(string category) => category.ToLowerInvariant() switch
    {
        "system" => Strings.Notification_Category_System,
        "customers" => Strings.Notification_Category_Customers,
        "bookings" => Strings.Notification_Category_Bookings,
        "inventory" => Strings.Notification_Category_Inventory,
        "sync" => Strings.Notification_Category_Sync,
        _ => category,
    };
}
