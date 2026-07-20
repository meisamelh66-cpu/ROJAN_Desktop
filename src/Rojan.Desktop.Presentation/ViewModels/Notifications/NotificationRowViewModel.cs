using System.Windows.Input;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>One row in the Notification Center list - wraps a <see cref="ResolvedNotification"/> with a computed relative timestamp, optional Search highlight spans (populated only while a search is active), and per-row Mark-as-Read/Dismiss commands that delegate back to <see cref="NotificationCenterViewModel"/>.</summary>
public sealed class NotificationRowViewModel : ViewModelBase
{
    public NotificationRowViewModel(
        ResolvedNotification notification,
        DateTimeOffset now,
        IReadOnlyList<HighlightSpan>? titleHighlights,
        IReadOnlyList<HighlightSpan>? messageHighlights,
        Func<string, Task> onMarkAsRead,
        Func<string, Task> onDismiss)
    {
        Notification = notification;
        TimeAgoText = RelativeTimeFormatter.Format(notification.CreatedAt, now);
        TitleHighlights = titleHighlights ?? [];
        MessageHighlights = messageHighlights ?? [];

        MarkAsReadCommand = new AsyncRelayCommand(_ => onMarkAsRead(notification.Id), _ => !notification.IsRead);
        DismissCommand = new AsyncRelayCommand(_ => onDismiss(notification.Id));
    }

    public ResolvedNotification Notification { get; }

    public string Id => Notification.Id;

    public NotificationSeverity Severity => Notification.Severity;

    public string Title => Notification.Title;

    public string Message => Notification.Message;

    public string CategoryLabel => Notification.CategoryLabel;

    public bool IsRead => Notification.IsRead;

    public string TimeAgoText { get; }

    public IReadOnlyList<HighlightSpan> TitleHighlights { get; }

    public IReadOnlyList<HighlightSpan> MessageHighlights { get; }

    public ICommand MarkAsReadCommand { get; }

    public ICommand DismissCommand { get; }
}
