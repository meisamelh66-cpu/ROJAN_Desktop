namespace Rojan.Desktop.Application.Customers;

/// <summary>Read-only use case Presentation depends on to load one customer's Customer 360 profile (details, notes, tags, timeline, statistics) as a single unit of work.</summary>
public interface ICustomerProfileQueryService
{
    public Task<CustomerProfileDto> GetProfileAsync(string customerId, CancellationToken cancellationToken = default);
}
