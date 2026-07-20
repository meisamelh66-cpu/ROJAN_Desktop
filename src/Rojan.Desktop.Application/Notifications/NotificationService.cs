using DomainNotifications = Rojan.Desktop.Domain.Notifications;

namespace Rojan.Desktop.Application.Notifications;

/// <summary>
/// Default <see cref="INotificationService"/>. Mints
/// <see cref="DomainNotifications.AppNotification.Id"/> (<see cref="Guid.NewGuid"/>)
/// and <see cref="DomainNotifications.AppNotification.CreatedAt"/>
/// (<see cref="DateTimeOffset.UtcNow"/>) itself - the same "caller
/// supplies intent, service mints identity/timestamp" shape
/// <c>Security.LocalSessionService</c> already established for session
/// issuance. Every mutation goes through
/// <see cref="DomainNotifications.INotificationRepository"/> and then
/// raises <see cref="StateChanged"/>, so any number of Presentation
/// consumers (badge counter, Notification Center list, toast host) can
/// each react independently without polling. Translates between
/// Domain's and Application's own <c>NotificationSeverity</c>/
/// <c>NotificationPriority</c> enums at this boundary - see
/// <see cref="NotificationSeverity"/>'s own doc comment for why
/// Application keeps a separate copy rather than reusing Domain's.
/// </summary>
public sealed class NotificationService : INotificationService
{
    private readonly DomainNotifications.INotificationRepository _repository;
    private readonly ISilentModePreferenceStore _silentModeStore;

    public NotificationService(DomainNotifications.INotificationRepository repository, ISilentModePreferenceStore silentModeStore)
    {
        _repository = repository;
        _silentModeStore = silentModeStore;
    }

    public event EventHandler<NotificationDto>? NotificationRaised;

    public event EventHandler<NotificationDto>? ToastRequested;

    public event EventHandler? StateChanged;

    public async Task<NotificationDto> RaiseAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var notification = new DomainNotifications.AppNotification(
            Id: Guid.NewGuid().ToString("N"),
            Severity: ToDomain(request.Severity),
            Priority: ToDomain(request.Priority),
            TitleKey: request.TitleKey,
            MessageKey: request.MessageKey,
            MessageArgs: request.MessageArgs ?? [],
            Category: request.Category,
            GroupKey: request.GroupKey,
            CreatedAt: DateTimeOffset.UtcNow,
            IsRead: false,
            IsSilent: request.IsSilent);

        await _repository.AddAsync(notification, cancellationToken).ConfigureAwait(false);

        var dto = Map(notification);
        NotificationRaised?.Invoke(this, dto);

        var isSilentModeEnabled = await _silentModeStore.GetIsEnabledAsync(cancellationToken).ConfigureAwait(false);
        if (DomainNotifications.NotificationRules.ShouldShowToast(notification, isSilentModeEnabled))
        {
            ToastRequested?.Invoke(this, dto);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
        return dto;
    }

    public async Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return notifications
            .OrderByDescending(n => DomainNotifications.NotificationRules.PriorityRank(n.Priority))
            .ThenByDescending(n => n.CreatedAt)
            .Select(Map)
            .ToList();
    }

    public async Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return notifications.Count(n => !n.IsRead);
    }

    public async Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        var notification = notifications.FirstOrDefault(n => n.Id == notificationId);
        if (notification is null || notification.IsRead)
        {
            return;
        }

        await _repository.UpdateAsync(notification with { IsRead = true }, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task MarkAllAsReadAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _repository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        // Materialized before the loop starts: some INotificationRepository
        // implementations (e.g. an in-memory store used in tests) return
        // their live backing list rather than a snapshot copy, so mutating
        // it via UpdateAsync while still enumerating the un-materialized
        // Where(...) result would throw InvalidOperationException.
        var unread = notifications.Where(n => !n.IsRead).ToList();
        foreach (var notification in unread)
        {
            await _repository.UpdateAsync(notification with { IsRead = true }, cancellationToken).ConfigureAwait(false);
        }

        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task DismissAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        await _repository.RemoveAsync(notificationId, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
        await _repository.ClearAsync(cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public Task<bool> GetIsSilentModeEnabledAsync(CancellationToken cancellationToken = default) =>
        _silentModeStore.GetIsEnabledAsync(cancellationToken);

    public async Task SetSilentModeEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default)
    {
        await _silentModeStore.SetIsEnabledAsync(isEnabled, cancellationToken).ConfigureAwait(false);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static NotificationDto Map(DomainNotifications.AppNotification notification) => new(
        notification.Id,
        ToApplication(notification.Severity),
        ToApplication(notification.Priority),
        notification.TitleKey,
        notification.MessageKey,
        notification.MessageArgs,
        notification.Category,
        DomainNotifications.NotificationRules.GroupKeyFor(notification),
        notification.CreatedAt,
        notification.IsRead);

    private static DomainNotifications.NotificationSeverity ToDomain(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Success => DomainNotifications.NotificationSeverity.Success,
        NotificationSeverity.Warning => DomainNotifications.NotificationSeverity.Warning,
        NotificationSeverity.Error => DomainNotifications.NotificationSeverity.Error,
        _ => DomainNotifications.NotificationSeverity.Information,
    };

    private static NotificationSeverity ToApplication(DomainNotifications.NotificationSeverity severity) => severity switch
    {
        DomainNotifications.NotificationSeverity.Success => NotificationSeverity.Success,
        DomainNotifications.NotificationSeverity.Warning => NotificationSeverity.Warning,
        DomainNotifications.NotificationSeverity.Error => NotificationSeverity.Error,
        _ => NotificationSeverity.Information,
    };

    private static DomainNotifications.NotificationPriority ToDomain(NotificationPriority priority) => priority switch
    {
        NotificationPriority.Low => DomainNotifications.NotificationPriority.Low,
        NotificationPriority.High => DomainNotifications.NotificationPriority.High,
        NotificationPriority.Critical => DomainNotifications.NotificationPriority.Critical,
        _ => DomainNotifications.NotificationPriority.Normal,
    };

    private static NotificationPriority ToApplication(DomainNotifications.NotificationPriority priority) => priority switch
    {
        DomainNotifications.NotificationPriority.Low => NotificationPriority.Low,
        DomainNotifications.NotificationPriority.High => NotificationPriority.High,
        DomainNotifications.NotificationPriority.Critical => NotificationPriority.Critical,
        _ => NotificationPriority.Normal,
    };
}
