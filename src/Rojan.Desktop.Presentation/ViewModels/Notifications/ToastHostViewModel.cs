using System.Collections.ObjectModel;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>
/// Phase 27: Toast Notifications. Subscribes to
/// <see cref="INotificationService.ToastRequested"/> (already Silent-Mode-
/// filtered - see that event's own doc comment) and stacks a
/// <see cref="ToastNotificationViewModel"/> per raised toast, each
/// auto-dismissing itself after a severity-scaled delay via
/// <see cref="IToastDismissScheduler"/> (kept out of this class directly
/// depending on <c>System.Windows.Threading.DispatcherTimer</c>, which
/// <c>ArchitectureTests.ViewModelTestabilityTests</c> forbids for any
/// type under <c>ViewModels</c>). Lives entirely outside the modal dialog
/// region (<c>Shell.MainWindowViewModel.ActiveDialog</c>) - toasts are a
/// separate, non-modal overlay that can coexist with any number of open
/// dialogs/pages, documented in
/// <c>docs/phases/phase-27-enterprise-notification-center.md</c>.
/// </summary>
public sealed class ToastHostViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultDismissDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan ErrorDismissDelay = TimeSpan.FromSeconds(8);

    private readonly INotificationService _notificationService;
    private readonly INotificationContentResolver _contentResolver;
    private readonly IToastDismissScheduler _dismissScheduler;
    private readonly Dictionary<string, IDisposable> _dismissHandles = [];

    public ToastHostViewModel(INotificationService notificationService, INotificationContentResolver contentResolver, IToastDismissScheduler dismissScheduler)
    {
        _notificationService = notificationService;
        _contentResolver = contentResolver;
        _dismissScheduler = dismissScheduler;
        ActiveToasts = new ObservableCollection<ToastNotificationViewModel>();

        _notificationService.ToastRequested += OnToastRequested;
    }

    public ObservableCollection<ToastNotificationViewModel> ActiveToasts { get; }

    private void OnToastRequested(object? sender, NotificationDto notification)
    {
        var resolved = _contentResolver.Resolve(notification);
        var toast = new ToastNotificationViewModel(resolved, Dismiss);
        ActiveToasts.Add(toast);

        var delay = notification.Severity is NotificationSeverity.Error or NotificationSeverity.Warning
            ? ErrorDismissDelay
            : DefaultDismissDelay;

        _dismissHandles[toast.Id] = _dismissScheduler.Schedule(delay, () => Dismiss(toast));
    }

    private void Dismiss(ToastNotificationViewModel toast)
    {
        if (_dismissHandles.Remove(toast.Id, out var handle))
        {
            handle.Dispose();
        }

        ActiveToasts.Remove(toast);
    }
}
