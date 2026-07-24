namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Combinable filter criteria for <see cref="ICustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, CancellationToken)"/> -
/// every field is optional and ANDed together when present. A filter with
/// every field left at its default is equivalent to no filtering at all:
/// <see cref="CustomerQueryService.SearchCustomersAsync(CustomerSearchFilter, CancellationToken)"/>
/// returns the exact same result set, in the same order, as
/// <see cref="ICustomerQueryService.GetCustomersAsync"/> in that case -
/// "no filter applied" behaves identically to before this filter existed.
/// <see cref="LastContactedFrom"/>/<see cref="LastContactedTo"/> filter by
/// <see cref="Domain.Customers.Customer.LastContactedAt"/>, the one date
/// field a customer record actually carries.
/// </summary>
public sealed record CustomerSearchFilter(
    string? SearchText = null,
    string? FullName = null,
    string? Company = null,
    string? Email = null,
    string? Phone = null,
    CustomerStatus? Status = null,
    string? Tag = null,
    DateOnly? LastContactedFrom = null,
    DateOnly? LastContactedTo = null)
{
    public static CustomerSearchFilter Empty { get; } = new();
}
