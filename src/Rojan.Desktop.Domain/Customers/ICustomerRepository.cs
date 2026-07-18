namespace Rojan.Desktop.Domain.Customers;

/// <summary>
/// Repository abstraction for customer data. Domain defines the contract;
/// Infrastructure provides the concrete implementation (a fake/in-memory
/// one for now - Phase 09 explicitly has no backend integration yet, same
/// as the Dashboard vertical slice in Phase 06B).
/// </summary>
public interface ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default);
}
