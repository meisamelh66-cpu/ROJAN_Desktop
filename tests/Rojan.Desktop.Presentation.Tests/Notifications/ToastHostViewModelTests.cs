using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Notifications;
using Rojan.Desktop.Presentation.ViewModels.Notifications;

namespace Rojan.Desktop.Presentation.Tests.Notifications;

/// <summary>Exercises <see cref="ToastHostViewModel"/>'s toast-stacking and scheduled auto-dismiss, using a controllable <see cref="IToastDismissScheduler"/> instead of a real timer.</summary>
public sealed class ToastHostViewModelTests
{
    private static NotificationDto Notification(NotificationSeverity severity = NotificationSeverity.Information) =>
        new("n1", severity, NotificationPriority.Normal, "Notification_Demo_WelcomeTitle", "Notification_Demo_WelcomeMessage", [], "system", "system", DateTimeOffset.UtcNow, false);

    [Fact]
    public void ToastRequested_AddsAToastToActiveToasts()
    {
        var service = new StubNotificationService();
        var scheduler = new StubToastDismissScheduler();
        var host = new ToastHostViewModel(service, new NotificationContentResolver(), scheduler);

        service.RaiseToastRequested(Notification());

        Assert.Single(host.ActiveToasts);
    }

    [Fact]
    public void ToastRequested_SchedulesAnAutoDismiss()
    {
        var service = new StubNotificationService();
        var scheduler = new StubToastDismissScheduler();
        var host = new ToastHostViewModel(service, new NotificationContentResolver(), scheduler);

        service.RaiseToastRequested(Notification());

        Assert.Single(scheduler.ScheduledCallbacks);
    }

    [Fact]
    public void ScheduledDismissCallback_RemovesTheToast()
    {
        var service = new StubNotificationService();
        var scheduler = new StubToastDismissScheduler();
        var host = new ToastHostViewModel(service, new NotificationContentResolver(), scheduler);
        service.RaiseToastRequested(Notification());

        scheduler.ScheduledCallbacks[0]();

        Assert.Empty(host.ActiveToasts);
    }

    [Fact]
    public void CloseCommand_RemovesTheToast()
    {
        var service = new StubNotificationService();
        var scheduler = new StubToastDismissScheduler();
        var host = new ToastHostViewModel(service, new NotificationContentResolver(), scheduler);
        service.RaiseToastRequested(Notification());
        var toast = host.ActiveToasts[0];

        toast.CloseCommand.Execute(null);

        Assert.Empty(host.ActiveToasts);
    }

    [Fact]
    public void MultipleToastRequests_StackInOrder()
    {
        var service = new StubNotificationService();
        var scheduler = new StubToastDismissScheduler();
        var host = new ToastHostViewModel(service, new NotificationContentResolver(), scheduler);

        service.RaiseToastRequested(Notification());
        service.RaiseToastRequested(Notification(NotificationSeverity.Error));

        Assert.Equal(2, host.ActiveToasts.Count);
    }
}
