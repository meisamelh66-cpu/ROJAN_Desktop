namespace Rojan.Desktop.Domain.Support;

public interface ISupportMessageRepository
{
    public Task<IReadOnlyList<SupportMessage>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task SaveAsync(SupportMessage message, CancellationToken cancellationToken = default);
}
