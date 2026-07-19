namespace Rojan.Desktop.Application.Customers;

/// <summary>Input to <see cref="ICustomerCommandService.UpdateCustomerAsync"/> - a full replacement of the editable fields, matching <see cref="Rojan.Desktop.Domain.Customers.Customer"/>'s shape as an immutable record.</summary>
public sealed record UpdateCustomerRequest(
    string Id,
    string FullName,
    string Company,
    string Email,
    string Phone,
    CustomerStatus Status,
    string LifetimeValue,
    string Notes);
