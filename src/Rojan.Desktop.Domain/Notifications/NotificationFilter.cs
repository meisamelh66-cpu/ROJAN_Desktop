namespace Rojan.Desktop.Domain.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center's Filtering requirement -
/// every field is optional (<see langword="null"/> means "don't filter
/// on this axis"). Deliberately excludes free-text search: matching
/// against a notification's resolved title/message requires localized
/// text, which only Presentation can produce - see
/// <c>Application.Notifications.NotificationSearchService</c> for that
/// half of Filtering, applied on top of this one.
/// </summary>
public sealed record NotificationFilter(
    NotificationSeverity? Severity = null,
    NotificationPriority? Priority = null,
    bool? IsRead = null,
    string? Category = null);
