using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>One severity filter chip in the Notification Center - <see cref="Value"/> is <see langword="null"/> for the "All" option.</summary>
public sealed record NotificationSeverityFilterOption(string Label, NotificationSeverity? Value);
