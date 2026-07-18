namespace Rojan.Desktop.Domain.Customers;

/// <summary>A single customer record, as returned by <see cref="ICustomerRepository"/>.</summary>
public sealed record Customer(
    string Id,
    string FullName,
    string Company,
    string Email,
    string Phone,
    CustomerStatus Status,
    string LifetimeValue,
    DateTimeOffset LastContactedAt,
    string Notes);
