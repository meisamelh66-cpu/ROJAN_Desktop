using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Application.Tests.Notifications;

/// <summary>Exercises <see cref="NotificationService"/>'s raise/query/mark-read/dismiss/clear/Silent-Mode behavior and its <see cref="INotificationService.NotificationRaised"/>/<see cref="INotificationService.ToastRequested"/>/<see cref="INotificationService.StateChanged"/> events.</summary>
public sealed class NotificationServiceTests
{
    private static NotificationRequest Request(
        NotificationSeverity severity = NotificationSeverity.Information,
        NotificationPriority priority = NotificationPriority.Normal,
        bool isSilent = false) =>
        new(severity, priority, "TitleKey", "MessageKey", IsSilent: isSilent);

    private static NotificationService CreateService() =>
        new(new StubNotificationRepository(), new StubSilentModePreferenceStore());

    [Fact]
    public async Task RaiseAsync_PersistsANewUnreadNotification()
    {
        var service = CreateService();

        var dto = await service.RaiseAsync(Request());

        Assert.False(dto.IsRead);
        Assert.NotEmpty(dto.Id);
        var all = await service.GetAllAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task RaiseAsync_RaisesNotificationRaisedEvent()
    {
        var service = CreateService();
        NotificationDto? raised = null;
        service.NotificationRaised += (_, dto) => raised = dto;

        await service.RaiseAsync(Request());

        Assert.NotNull(raised);
    }

    [Fact]
    public async Task RaiseAsync_NonSilentModeNonSilentNotification_RaisesToastRequested()
    {
        var service = CreateService();
        var toastRaised = false;
        service.ToastRequested += (_, _) => toastRaised = true;

        await service.RaiseAsync(Request());

        Assert.True(toastRaised);
    }

    [Fact]
    public async Task RaiseAsync_SilentModeEnabledNonCriticalPriority_DoesNotRaiseToastRequested()
    {
        var service = CreateService();
        await service.SetSilentModeEnabledAsync(true);
        var toastRaised = false;
        service.ToastRequested += (_, _) => toastRaised = true;

        await service.RaiseAsync(Request(priority: NotificationPriority.Normal));

        Assert.False(toastRaised);
    }

    [Fact]
    public async Task RaiseAsync_SilentModeEnabledCriticalPriority_StillRaisesToastRequested()
    {
        var service = CreateService();
        await service.SetSilentModeEnabledAsync(true);
        var toastRaised = false;
        service.ToastRequested += (_, _) => toastRaised = true;

        await service.RaiseAsync(Request(priority: NotificationPriority.Critical));

        Assert.True(toastRaised);
    }

    [Fact]
    public async Task GetAllAsync_OrdersByPriorityDescendingThenMostRecentFirst()
    {
        var service = CreateService();
        await service.RaiseAsync(Request(priority: NotificationPriority.Low));
        await service.RaiseAsync(Request(priority: NotificationPriority.Critical));
        await service.RaiseAsync(Request(priority: NotificationPriority.Normal));

        var all = await service.GetAllAsync();

        Assert.Equal(NotificationPriority.Critical, all[0].Priority);
        Assert.Equal(NotificationPriority.Normal, all[1].Priority);
        Assert.Equal(NotificationPriority.Low, all[2].Priority);
    }

    [Fact]
    public async Task GetUnreadCountAsync_CountsOnlyUnreadNotifications()
    {
        var service = CreateService();
        var first = await service.RaiseAsync(Request());
        await service.RaiseAsync(Request());
        await service.MarkAsReadAsync(first.Id);

        var unreadCount = await service.GetUnreadCountAsync();

        Assert.Equal(1, unreadCount);
    }

    [Fact]
    public async Task MarkAsReadAsync_MarksTheMatchingNotificationRead()
    {
        var service = CreateService();
        var dto = await service.RaiseAsync(Request());

        await service.MarkAsReadAsync(dto.Id);

        var all = await service.GetAllAsync();
        Assert.True(all.Single().IsRead);
    }

    [Fact]
    public async Task MarkAllAsReadAsync_MarksEveryNotificationRead()
    {
        var service = CreateService();
        await service.RaiseAsync(Request());
        await service.RaiseAsync(Request());

        await service.MarkAllAsReadAsync();

        var all = await service.GetAllAsync();
        Assert.All(all, n => Assert.True(n.IsRead));
    }

    [Fact]
    public async Task DismissAsync_RemovesOnlyTheMatchingNotification()
    {
        var service = CreateService();
        var toDismiss = await service.RaiseAsync(Request());
        await service.RaiseAsync(Request());

        await service.DismissAsync(toDismiss.Id);

        var all = await service.GetAllAsync();
        Assert.Single(all);
        Assert.DoesNotContain(all, n => n.Id == toDismiss.Id);
    }

    [Fact]
    public async Task ClearAllAsync_RemovesEveryNotification()
    {
        var service = CreateService();
        await service.RaiseAsync(Request());
        await service.RaiseAsync(Request());

        await service.ClearAllAsync();

        var all = await service.GetAllAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task SetSilentModeEnabledAsync_PersistsThroughGetIsSilentModeEnabledAsync()
    {
        var service = CreateService();

        await service.SetSilentModeEnabledAsync(true);

        Assert.True(await service.GetIsSilentModeEnabledAsync());
    }

    [Fact]
    public async Task MarkAsReadAsync_RaisesStateChanged()
    {
        var service = CreateService();
        var dto = await service.RaiseAsync(Request());
        var stateChangedCount = 0;
        service.StateChanged += (_, _) => stateChangedCount++;

        await service.MarkAsReadAsync(dto.Id);

        Assert.Equal(1, stateChangedCount);
    }
}
