namespace Rojan.Desktop.Domain.Automation;

/// <summary>Repository abstraction for scheduled jobs. Domain defines the contract; Infrastructure provides the concrete implementation (local JSON persistence).</summary>
public interface IScheduledJobRepository
{
    public Task<IReadOnlyList<ScheduledJob>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<ScheduledJob?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task SaveAsync(ScheduledJob job, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
