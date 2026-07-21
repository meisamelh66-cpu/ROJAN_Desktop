namespace Rojan.Desktop.Domain.Automation;

/// <summary>Repository abstraction for approval requests. Domain defines the contract; Infrastructure provides the concrete implementation (local JSON persistence).</summary>
public interface IApprovalRepository
{
    public Task<IReadOnlyList<ApprovalRequest>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<ApprovalRequest?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task SaveAsync(ApprovalRequest request, CancellationToken cancellationToken = default);
}
