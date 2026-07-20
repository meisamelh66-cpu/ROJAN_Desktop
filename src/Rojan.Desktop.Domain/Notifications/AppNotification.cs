namespace Rojan.Desktop.Domain.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. One raised notification -
/// metadata plus resx key references, never literal display text
/// (Domain/Infrastructure cannot depend on Presentation's <c>Strings</c>).
/// <see cref="TitleKey"/>/<see cref="MessageKey"/> are resolved to
/// localized text only at the Presentation boundary
/// (<c>Presentation.Notifications.NotificationContentResolver</c>),
/// mirroring Phase 26's <c>HelpTopic.KeyPrefix</c> convention.
/// <see cref="MessageArgs"/> lets a raised notification carry dynamic,
/// already-plain-text values (e.g. a customer's name) that get
/// substituted into the localized message template via
/// <see cref="string.Format(string, object?[])"/> - the template itself
/// stays fully localized, only the interpolated values are runtime data.
/// </summary>
/// <param name="Id">Stable identifier, minted once when raised.</param>
/// <param name="Severity">Success/Warning/Error/Information - drives the icon/color chosen at display time.</param>
/// <param name="Priority">Independent of <paramref name="Severity"/> - drives sort order and the Silent Mode toast carve-out.</param>
/// <param name="TitleKey">Resx key for the notification's title.</param>
/// <param name="MessageKey">Resx key for the notification's body text (a <see cref="string.Format(string, object?[])"/> template).</param>
/// <param name="MessageArgs">Already-plain-text values substituted into the localized <paramref name="MessageKey"/> template.</param>
/// <param name="Category">The module/source that raised this (e.g. <c>"customers"</c>, <c>"system"</c>) - both a grouping fallback and the Filtering requirement's category axis.</param>
/// <param name="GroupKey">Explicit grouping key (e.g. <c>"booking-status-change"</c>); falls back to <paramref name="Category"/> when unset - see <c>NotificationRules.GroupKeyFor</c>.</param>
/// <param name="CreatedAt">When this notification was raised.</param>
/// <param name="IsRead">Read/Unread state.</param>
/// <param name="IsSilent">Per-notification override - when <see langword="true"/>, this notification never produces a toast regardless of global Silent Mode (e.g. a low-value background sync tick), only ever appears in the Notification Center list.</param>
public sealed record AppNotification(
    string Id,
    NotificationSeverity Severity,
    NotificationPriority Priority,
    string TitleKey,
    string MessageKey,
    IReadOnlyList<string> MessageArgs,
    string Category,
    string? GroupKey,
    DateTimeOffset CreatedAt,
    bool IsRead,
    bool IsSilent);
