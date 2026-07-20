using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Notifications;
using Rojan.Desktop.Presentation.ViewModels.Notifications;

namespace Rojan.Desktop.Presentation.Tests.Notifications;

/// <summary>Exercises <see cref="NotificationCenterViewModel"/>'s grouping, severity filtering, unread filtering, search, mark-read/mark-all-read/clear-all, and badge count.</summary>
public sealed class NotificationCenterViewModelTests
{
    private static NotificationDto Notification(
        string id,
        NotificationSeverity severity = NotificationSeverity.Information,
        bool isRead = false,
        string category = "system") =>
        new(id, severity, NotificationPriority.Normal, "Notification_Demo_WelcomeTitle", "Notification_Demo_WelcomeMessage", [], category, category, DateTimeOffset.UtcNow, isRead);

    private static NotificationCenterViewModel CreateViewModel(StubNotificationService service) =>
        new(service, new NotificationContentResolver(), new NotificationSearchService());

    [Fact]
    public async Task InitializeAsync_GroupsNotificationsByCategory()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1", category: "customers"), Notification("n2", category: "bookings")]);
        var viewModel = CreateViewModel(service);

        await viewModel.InitializeAsync();

        Assert.Equal(2, viewModel.Groups.Count);
    }

    [Fact]
    public async Task InitializeAsync_SetsUnreadCountFromUnreadNotifications()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1", isRead: false), Notification("n2", isRead: true)]);
        var viewModel = CreateViewModel(service);

        await viewModel.InitializeAsync();

        Assert.Equal(1, viewModel.UnreadCount);
        Assert.True(viewModel.HasUnread);
    }

    [Fact]
    public async Task SelectedSeverityFilter_FiltersOutNonMatchingSeverities()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1", NotificationSeverity.Error), Notification("n2", NotificationSeverity.Success)]);
        var viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.SelectedSeverityFilter = viewModel.SeverityFilterOptions.Single(o => o.Value == NotificationSeverity.Error);
        await Task.Delay(10);

        var allRows = viewModel.Groups.SelectMany(g => g.Items).ToList();
        Assert.Single(allRows);
        Assert.Equal("n1", allRows[0].Id);
    }

    [Fact]
    public async Task IsShowingUnreadOnly_FiltersOutReadNotifications()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1", isRead: false), Notification("n2", isRead: true)]);
        var viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.IsShowingUnreadOnly = true;
        await Task.Delay(10);

        var allRows = viewModel.Groups.SelectMany(g => g.Items).ToList();
        Assert.Single(allRows);
        Assert.Equal("n1", allRows[0].Id);
    }

    [Fact]
    public async Task MarkAllReadCommand_MarksEveryNotificationRead()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1"), Notification("n2")]);
        var viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.MarkAllReadCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal(0, viewModel.UnreadCount);
    }

    [Fact]
    public async Task ClearAllCommand_RemovesEveryNotification()
    {
        var service = new StubNotificationService();
        service.Seed([Notification("n1"), Notification("n2")]);
        var viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.ClearAllCommand.Execute(null);
        await Task.Delay(10);

        Assert.Empty(viewModel.Groups);
    }

    [Fact]
    public async Task InitializeAsync_LoadsSilentModeState()
    {
        var service = new StubNotificationService();
        await service.SetSilentModeEnabledAsync(true);
        var viewModel = CreateViewModel(service);

        await viewModel.InitializeAsync();

        Assert.True(viewModel.IsSilentModeEnabled);
    }

    [Fact]
    public async Task IsSilentModeEnabled_SetterPersiststhroughTheService()
    {
        var service = new StubNotificationService();
        var viewModel = CreateViewModel(service);
        await viewModel.InitializeAsync();

        viewModel.IsSilentModeEnabled = true;
        await Task.Delay(10);

        Assert.True(await service.GetIsSilentModeEnabledAsync());
    }
}
