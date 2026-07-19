namespace Rojan.Desktop.Application.BookingWorkflow;

/// <summary>A pickable customer for the Booking Wizard's first step.</summary>
public sealed record WorkflowCustomerOptionDto(string Id, string FullName);

/// <summary>A pickable, Active-only catalog service for the Booking Wizard's service-selection step - Seasonal/Discontinued services are deliberately excluded, a business rule that belongs here (orchestration), not in the Services slice itself.</summary>
public sealed record WorkflowServiceOptionDto(string Id, string Name, int DurationMinutes, string Price);

/// <summary>A pickable, Active-only specialist for the Booking Wizard's specialist-selection step.</summary>
public sealed record WorkflowSpecialistOptionDto(string Id, string FullName);

/// <summary>Everything the wizard's picker steps need, fetched together as a single unit of work - the "booking options query" this phase requires, coordinating Customers, Services, and Specialists in one call.</summary>
public sealed record BookingOptionsDto(
    IReadOnlyList<WorkflowCustomerOptionDto> Customers,
    IReadOnlyList<WorkflowServiceOptionDto> Services,
    IReadOnlyList<WorkflowSpecialistOptionDto> Specialists);
