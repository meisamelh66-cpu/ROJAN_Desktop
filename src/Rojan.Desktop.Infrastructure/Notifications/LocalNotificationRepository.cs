using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Notifications;

namespace Rojan.Desktop.Infrastructure.Notifications;

/// <summary>
/// Default <see cref="INotificationRepository"/>. Persists to
/// <c>%LocalAppData%\RojanDesktop\notifications\history.json</c> - same
/// "one concern, one file" shape every other persisted service in this
/// app uses. Capped at <see cref="MaxEntries"/>, oldest-first eviction -
/// the Notification History requirement is a bounded window, not an
/// unbounded audit log, so a long-running session never accumulates
/// disk/memory without limit (Phase 27's Performance requirement).
/// </summary>
public sealed class LocalNotificationRepository : INotificationRepository
{
    public const int MaxEntries = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalNotificationRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalNotificationRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<AppNotification>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AppNotification>>(ReadPersisted());

    public Task AddAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        var notifications = ReadPersisted();
        notifications.Insert(0, notification);
        if (notifications.Count > MaxEntries)
        {
            notifications.RemoveRange(MaxEntries, notifications.Count - MaxEntries);
        }

        Persist(notifications);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(AppNotification notification, CancellationToken cancellationToken = default)
    {
        var notifications = ReadPersisted();
        var index = notifications.FindIndex(n => n.Id == notification.Id);
        if (index >= 0)
        {
            notifications[index] = notification;
            Persist(notifications);
        }

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var notifications = ReadPersisted();
        if (notifications.RemoveAll(n => n.Id == notificationId) > 0)
        {
            Persist(notifications);
        }

        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        Persist([]);
        return Task.CompletedTask;
    }

    private List<AppNotification> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<AppNotification>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Persist(List<AppNotification> notifications)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(notifications, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "notifications", "history.json");
}
