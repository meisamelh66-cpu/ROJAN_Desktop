namespace Rojan.Desktop.Shell;

/// <summary>Notification-region entry. No producer exists yet (Phase 07 is architecture-first) - the region renders correctly empty until something starts adding to MainWindowViewModel.Notifications.</summary>
public sealed record NotificationItem(string Id, string Message, DateTimeOffset Timestamp);
