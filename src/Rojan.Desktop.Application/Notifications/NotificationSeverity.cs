namespace Rojan.Desktop.Application.Notifications;

/// <summary>Application-layer mirror of <see cref="Domain.Notifications.NotificationSeverity"/> - kept as a separate type (not a reused Domain enum) so Presentation never needs a Domain reference for it, the same "Application owns its own enum copy" shape <c>Application.Customers.CustomerStatus</c> already establishes for <see cref="Domain.Customers.CustomerStatus"/>.</summary>
public enum NotificationSeverity
{
    Information,
    Success,
    Warning,
    Error,
}
