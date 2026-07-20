namespace Rojan.Desktop.Domain.Customers;

/// <summary>
/// A single customer record, as returned by <see cref="ICustomerRepository"/>.
/// Phase 22A: <see cref="OrganizationId"/>/<see cref="BranchId"/> scope
/// every customer to the organization/branch that owns it - required
/// (never a hardcoded default), so a caller can never accidentally
/// create an unscoped record.
/// </summary>
public sealed record Customer(
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
