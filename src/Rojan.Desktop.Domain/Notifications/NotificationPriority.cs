namespace Rojan.Desktop.Domain.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. Independent of <see cref="NotificationSeverity"/> -
/// a <see cref="NotificationSeverity.Information"/> notice can still be
/// <see cref="Critical"/> (e.g. "sync completed"), and an
/// <see cref="NotificationSeverity.Error"/> can be <see cref="Low"/> (a
/// background retry that will resolve itself). Priority drives sort
/// order and Silent Mode's "still show Critical toasts" carve-out (see
/// <c>NotificationRules.ShouldShowToast</c>), not the visual severity
/// color.
/// </summary>
public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical,
}
