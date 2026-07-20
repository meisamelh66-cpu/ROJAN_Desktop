namespace Rojan.Desktop.Application.Customers;

/// <summary>Application-layer shape of a customer record, mapped from <see cref="Rojan.Desktop.Domain.Customers.Customer"/> by <see cref="CustomerQueryService"/>.</summary>
public sealed record CustomerDto(
    string Id,
    string FullName,
    string Company,
    string Email,
    string Phone,
    CustomerStatus Status,
    string LifetimeValue,
    DateTimeOffset LastContactedAt,
    string Notes,
    string OrganizationId,
    string BranchId);
