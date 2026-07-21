using Rojan.Desktop.Application.Notifications;

namespace Rojan.Desktop.Application.Tests.Automation;

/// <summary>Minimal <see cref="INotificationService"/> test double recording every <see cref="RaiseAsync"/> call, so a test can assert a business rule/workflow step actually raised a notification without exercising the real Phase 27 subsystem.</summary>
internal sealed class StubNotificationService : INotificationService
{
    public List<NotificationRequest> RaisedRequests { get; } = [];

    public event EventHandler<NotificationDto>? NotificationRaised;

    public event EventHandler<NotificationDto>? ToastRequested;

    public event EventHandler? StateChanged;

    public Task<NotificationDto> RaiseAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        RaisedRequests.Add(request);
        var dto = new NotificationDto(Guid.NewGuid().ToString("N"), request.Severity, request.Priority, request.TitleKey, request.MessageKey, request.MessageArgs ?? [], request.Category, request.GroupKey ?? request.Category, DateTimeOffset.UtcNow, IsRead: false);
        NotificationRaised?.Invoke(this, dto);
        ToastRequested?.Invoke(this, dto);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return Task.FromResult(dto);
    }

    public Task<IReadOnlyList<NotificationDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<NotificationDto>>([]);

    public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task MarkAllAsReadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task DismissAsync(string notificationId, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task ClearAllAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<bool> GetIsSilentModeEnabledAsync(CancellationToken cancellationToken = default) => Task.FromResult(false);

    public Task SetSilentModeEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
