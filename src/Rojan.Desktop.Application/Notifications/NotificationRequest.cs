namespace Rojan.Desktop.Application.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. The input to
/// <see cref="INotificationService.RaiseAsync"/> - every module that
/// wants to notify the user goes through this one shape rather than
/// constructing a <see cref="Domain.Notifications.AppNotification"/>
/// directly (which would require it to know how ids/timestamps/read-state
/// are minted).
/// </summary>
/// <param name="Severity">Success/Warning/Error/Information - drives the icon/color chosen at display time.</param>
/// <param name="Priority">Independent of <paramref name="Severity"/> - drives sort order and the Silent Mode toast carve-out.</param>
/// <param name="TitleKey">Resx key for the notification's title.</param>
/// <param name="MessageKey">Resx key for the notification's body text (a <see cref="string.Format(string, object?[])"/> template).</param>
/// <param name="MessageArgs">Defaults to an empty list when omitted - most notifications are static text with no interpolated values.</param>
/// <param name="Category">Defaults to <c>"system"</c> when omitted.</param>
/// <param name="GroupKey">Defaults to <see langword="null"/> (falls back to <paramref name="Category"/> - see <c>Domain.Notifications.NotificationRules.GroupKeyFor</c>).</param>
/// <param name="IsSilent">Defaults to <see langword="false"/> - most notifications are eligible for a toast, subject to Silent Mode.</param>
public sealed record NotificationRequest(
    NotificationSeverity Severity,
    NotificationPriority Priority,
    string TitleKey,
    string MessageKey,
    IReadOnlyList<string>? MessageArgs = null,
    string Category = "system",
    string? GroupKey = null,
    bool IsSilent = false);
