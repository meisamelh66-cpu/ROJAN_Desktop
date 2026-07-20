using Rojan.Desktop.Domain.Notifications;
using Rojan.Desktop.Infrastructure.Notifications;

namespace Rojan.Desktop.Infrastructure.Tests.Notifications;

/// <summary>Exercises <see cref="LocalNotificationRepository"/> against a temp file - persistence round-trip, update/remove/clear, and the <see cref="LocalNotificationRepository.MaxEntries"/> eviction cap.</summary>
public sealed class LocalNotificationRepositoryTests : IDisposable
{
    private readonly string _filePath;

    public LocalNotificationRepositoryTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "history.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static AppNotification Notification(string id) =>
        new(id, NotificationSeverity.Information, NotificationPriority.Normal, "TitleKey", "MessageKey", [], "system", null, DateTimeOffset.UtcNow, false, false);

    [Fact]
    public async Task GetAllAsync_NoPersistedFile_ReturnsEmptyList()
    {
        var repository = new LocalNotificationRepository(_filePath);

        var notifications = await repository.GetAllAsync();

        Assert.Empty(notifications);
    }

    [Fact]
    public async Task AddAsync_InsertsAtTheFront()
    {
        var repository = new LocalNotificationRepository(_filePath);

        await repository.AddAsync(Notification("n1"));
        await repository.AddAsync(Notification("n2"));

        var notifications = await repository.GetAllAsync();
        Assert.Equal(["n2", "n1"], notifications.Select(n => n.Id));
    }

    [Fact]
    public async Task UpdateAsync_ReplacesTheMatchingEntry()
    {
        var repository = new LocalNotificationRepository(_filePath);
        await repository.AddAsync(Notification("n1"));

        await repository.UpdateAsync(Notification("n1") with { IsRead = true });

        var notifications = await repository.GetAllAsync();
        Assert.True(notifications.Single().IsRead);
    }

    [Fact]
    public async Task RemoveAsync_RemovesOnlyTheMatchingEntry()
    {
        var repository = new LocalNotificationRepository(_filePath);
        await repository.AddAsync(Notification("n1"));
        await repository.AddAsync(Notification("n2"));

        await repository.RemoveAsync("n1");

        var notifications = await repository.GetAllAsync();
        Assert.Single(notifications);
        Assert.Equal("n2", notifications[0].Id);
    }

    [Fact]
    public async Task ClearAsync_RemovesEveryEntry()
    {
        var repository = new LocalNotificationRepository(_filePath);
        await repository.AddAsync(Notification("n1"));
        await repository.AddAsync(Notification("n2"));

        await repository.ClearAsync();

        Assert.Empty(await repository.GetAllAsync());
    }

    [Fact]
    public async Task AddAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var repository = new LocalNotificationRepository(_filePath);
        for (var i = 0; i < LocalNotificationRepository.MaxEntries + 2; i++)
        {
            await repository.AddAsync(Notification($"n{i}"));
        }

        var notifications = await repository.GetAllAsync();

        Assert.Equal(LocalNotificationRepository.MaxEntries, notifications.Count);
        Assert.DoesNotContain(notifications, n => n.Id == "n0");
        Assert.Contains(notifications, n => n.Id == $"n{LocalNotificationRepository.MaxEntries + 1}");
    }

    [Fact]
    public async Task AddAsync_PersistsAcrossInstances()
    {
        var first = new LocalNotificationRepository(_filePath);
        await first.AddAsync(Notification("n1"));

        var second = new LocalNotificationRepository(_filePath);
        var notifications = await second.GetAllAsync();

        Assert.Single(notifications);
    }
}
