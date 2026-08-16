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
    string BranchId,
    // Reception Stabilization Sprint: additive, trailing - null for a walk-in/guest customer with
    // no linked backend user account. Lets the Booking Wizard tell a bookable customer from one
    // that will 409 on CreateBookingAsync (a real, unchangeable backend business rule - see
    // BookingWorkflow.WorkflowCustomerOptionDto.IsLinkedToAccount) before Reception reaches the
    // last step.
    string? UserId = null);
