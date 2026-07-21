namespace Rojan.Desktop.Domain.Automation;

/// <summary>Repository abstraction for business rules. Domain defines the contract; Infrastructure provides the concrete implementation (local JSON persistence).</summary>
public interface IBusinessRuleRepository
{
    public Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken cancellationToken = default);

    public Task<BusinessRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    public Task SaveAsync(BusinessRule rule, CancellationToken cancellationToken = default);

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default);
}
