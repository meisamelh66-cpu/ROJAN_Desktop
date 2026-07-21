namespace Rojan.Desktop.Application.Support;

/// <summary>Support Center message submission ("ارسال پیام"/"ارتباط با Super Admin"/"گزارش خطا"/"پیشنهادات و انتقادات") - one shape, discriminated by <see cref="SupportMessageType"/>.</summary>
public interface ISupportMessageService
{
    /// <summary>Every submitted message, most recent first.</summary>
    public Task<IReadOnlyList<SupportMessageDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Validates and persists a new message. Throws <see cref="InvalidOperationException"/> if required fields are missing.</summary>
    public Task<SupportMessageDto> SubmitAsync(SupportMessageType type, string subject, string body, string senderName, string senderEmail, CancellationToken cancellationToken = default);
}
