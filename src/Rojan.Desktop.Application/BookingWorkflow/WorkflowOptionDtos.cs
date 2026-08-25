namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>
/// A pickable customer for the Booking Wizard's first step.
/// Reception Stabilization Sprint: <see cref="IsLinkedToAccount"/> (additive, trailing) mirrors
/// <c>Customers.CustomerDto.UserId is not null</c> - a customer with no linked backend user
/// account (a walk-in/guest) will always fail booking creation with a
/// <c>409 CUSTOMER_NOT_LINKED_TO_ACCOUNT</c>, a real backend business rule this app cannot change.
/// Lets the wizard warn Reception about this before the final step rather than after. Defaults to
/// <see langword="false"/> (the conservative "not known to be bookable" reading) for any
/// construction site that hasn't been updated to pass it explicitly.
/// </summary>
public sealed record WorkflowCustomerOptionDto(string Id, string FullName, bool IsLinkedToAccount = false);

/// <summary>A pickable, Active-only catalog service for the Booking Wizard's service-selection step - Seasonal/Discontinued services are deliberately excluded, a business rule that belongs here (orchestration), not in the Services slice itself.</summary>
public sealed record WorkflowServiceOptionDto(string Id, string Name, int DurationMinutes, string Price);

/// <summary>
/// A pickable, Active-only specialist for the Booking Wizard's
/// specialist-selection step. Booking Eligibility Filter:
/// <see cref="AssignedServiceIds"/> is the real, backend-owned service
/// eligibility list (<see cref="Specialists.ISpecialistQueryService.GetAssignedServiceIdsAsync"/>) -
/// empty means "no restriction, eligible for every service" (ROJAN_Backend's
/// own opt-in default), not "eligible for nothing".
/// </summary>
public sealed record WorkflowSpecialistOptionDto(string Id, string FullName, IReadOnlyList<string> AssignedServiceIds);

/// <summary>Everything the wizard's picker steps need, fetched together as a single unit of work - the "booking options query" this phase requires, coordinating Customers, Services, and Specialists in one call.</summary>
public sealed record BookingOptionsDto(
    IReadOnlyList<WorkflowCustomerOptionDto> Customers,
    IReadOnlyList<WorkflowServiceOptionDto> Services,
    IReadOnlyList<WorkflowSpecialistOptionDto> Specialists);
