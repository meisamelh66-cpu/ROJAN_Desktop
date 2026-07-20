using System.Windows.Input;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>One transient toast popup - constructed by <see cref="ToastHostViewModel"/> for every notification <c>INotificationService.ToastRequested</c> fires (already Silent-Mode-filtered), auto-dismissed after a severity-scaled delay or via <see cref="CloseCommand"/>.</summary>
public sealed class ToastNotificationViewModel : ViewModelBase
{
    public ToastNotificationViewModel(ResolvedNotification notification, Action<ToastNotificationViewModel> onClose)
    {
        Id = notification.Id;
        Severity = notification.Severity;
        Title = notification.Title;
        Message = notification.Message;
        CloseCommand = new RelayCommand(_ => onClose(this));
    }

    public string Id { get; }

    public NotificationSeverity Severity { get; }

    public string Title { get; }

    public string Message { get; }

    public ICommand CloseCommand { get; }
}
