namespace Rojan.Desktop.Domain.Customers;

/// <summary>
/// A single customer record, as returned by <see cref="ICustomerRepository"/>.
/// Phase 22A: <see cref="OrganizationId"/>/<see cref="BranchId"/> scope
/// every customer to the organization/branch that owns it - required
/// (never a hardcoded default), so a caller can never accidentally
/// create an unscoped record.
///
/// Owner App Customer CRM Integration: <see cref="UserId"/> is the linked
/// backend account, if any - null for a walk-in customer with no app
/// account (and always null for local/EF-backed data, which has no such
/// concept). Trailing, optional, default <see langword="null"/> so every
/// existing positional call site keeps compiling unchanged. ROJAN_Backend's
/// own booking records reference a caller's <c>UserId</c>, not this
/// customer's own <see cref="Id"/> - see <c>Application.Customers.CustomerProfileQueryService.BuildBookingSummaryAsync</c>'s
/// own doc comment for why booking-history composition prefers this field
/// when present.
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
    string BranchId,
    string? UserId = null);
