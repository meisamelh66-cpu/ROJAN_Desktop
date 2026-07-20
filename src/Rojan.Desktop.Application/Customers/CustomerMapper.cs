using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Domain&lt;-&gt;Application mapping shared by every Customers use case
/// (<see cref="CustomerQueryService"/>, <see cref="CustomerProfileQueryService"/>,
/// <see cref="CustomerCommandService"/>) - extracted once Phase 10 added a
/// second and third consumer of the same Domain-to-DTO mapping, rather than
/// duplicating it. Internal: only this project's own services call it,
/// Presentation only ever sees the DTOs it produces.
/// </summary>
internal static class CustomerMapper
{
    public static CustomerDto MapCustomer(DomainCustomers.Customer customer) => new(
        customer.Id,
        customer.FullName,
        customer.Company,
        customer.Email,
        customer.Phone,
        MapStatus(customer.Status),
        customer.LifetimeValue,
        customer.LastContactedAt,
        customer.Notes,
        customer.OrganizationId,
        customer.BranchId);

    public static CustomerNoteDto MapNote(DomainCustomers.CustomerNote note) =>
        new(note.Id, note.CustomerId, note.Text, note.CreatedAt);

    public static CustomerTagDto MapTag(DomainCustomers.CustomerTag tag) =>
        new(tag.Id, tag.CustomerId, tag.Label, tag.CreatedAt);

    public static CustomerActivityDto MapActivity(DomainCustomers.CustomerActivity activity) =>
        new(activity.Id, activity.CustomerId, activity.Description, activity.OccurredAt);

    public static CustomerStatus MapStatus(DomainCustomers.CustomerStatus status) => status switch
    {
        DomainCustomers.CustomerStatus.Lead => CustomerStatus.Lead,
        DomainCustomers.CustomerStatus.Prospect => CustomerStatus.Prospect,
        DomainCustomers.CustomerStatus.Active => CustomerStatus.Active,
        DomainCustomers.CustomerStatus.Vip => CustomerStatus.Vip,
        DomainCustomers.CustomerStatus.Inactive => CustomerStatus.Inactive,
        DomainCustomers.CustomerStatus.Churned => CustomerStatus.Churned,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain customer status."),
    };

    /// <summary>Application -&gt; Domain direction, needed only by <see cref="CustomerCommandService"/> when building a Domain entity to persist from a caller-supplied request.</summary>
    public static DomainCustomers.CustomerStatus MapStatusToDomain(CustomerStatus status) => status switch
    {
        CustomerStatus.Lead => DomainCustomers.CustomerStatus.Lead,
        CustomerStatus.Prospect => DomainCustomers.CustomerStatus.Prospect,
        CustomerStatus.Active => DomainCustomers.CustomerStatus.Active,
        CustomerStatus.Vip => DomainCustomers.CustomerStatus.Vip,
        CustomerStatus.Inactive => DomainCustomers.CustomerStatus.Inactive,
        CustomerStatus.Churned => DomainCustomers.CustomerStatus.Churned,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application customer status."),
    };
}
