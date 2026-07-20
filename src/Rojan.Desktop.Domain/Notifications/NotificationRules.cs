namespace Rojan.Desktop.Domain.Notifications;

/// <summary>Phase 27: Enterprise Notification Center. Pure filtering/grouping/ordering/Silent-Mode logic - no I/O, no localization awareness, matches the "value/data in, decision out" shape every other <c>*Rules</c> class in this codebase already uses (e.g. <see cref="Help.HelpContentRules"/>).</summary>
public static class NotificationRules
{
    /// <summary>True when <paramref name="notification"/> satisfies every non-null axis of <paramref name="filter"/> (an unset axis never excludes a notification).</summary>
    public static bool Matches(AppNotification notification, NotificationFilter filter)
    {
        if (filter.Severity is NotificationSeverity severity && notification.Severity != severity)
        {
            return false;
        }

        if (filter.Priority is NotificationPriority priority && notification.Priority != priority)
        {
            return false;
        }

        if (filter.IsRead is bool isRead && notification.IsRead != isRead)
        {
            return false;
        }

        if (filter.Category is string category && !string.Equals(notification.Category, category, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>The Grouping requirement's key - <see cref="AppNotification.GroupKey"/> when the notification explicitly named one, otherwise its <see cref="AppNotification.Category"/> (every notification belongs to a group, even without an explicit one).</summary>
    public static string GroupKeyFor(AppNotification notification) =>
        notification.GroupKey ?? notification.Category;

    /// <summary>Priority levels ranked highest-first - the sort key <c>NotificationService.GetAllAsync</c> orders by (priority, then most-recent-first) so a Critical notification from yesterday still outranks a Low one from five minutes ago.</summary>
    public static int PriorityRank(NotificationPriority priority) => priority switch
    {
        NotificationPriority.Critical => 3,
        NotificationPriority.High => 2,
        NotificationPriority.Normal => 1,
        NotificationPriority.Low => 0,
        _ => 0,
    };

    /// <summary>
    /// Silent Mode architecture's core rule: while Silent Mode is enabled,
    /// only <see cref="NotificationPriority.Critical"/> notifications still
    /// produce a toast (the common "Do Not Disturb still allows urgent"
    /// enterprise pattern) - everything else is suppressed from the toast
    /// surface but still recorded in the Notification Center list/history
    /// (Silent Mode never hides or drops a notification, only its toast
    /// popup). <see cref="AppNotification.IsSilent"/> is a stronger,
    /// per-notification override that suppresses the toast unconditionally,
    /// regardless of Silent Mode or priority.
    /// </summary>
    public static bool ShouldShowToast(AppNotification notification, bool isSilentModeEnabled)
    {
        if (notification.IsSilent)
        {
            return false;
        }

        return !isSilentModeEnabled || notification.Priority == NotificationPriority.Critical;
    }
}
