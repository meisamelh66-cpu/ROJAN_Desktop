namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// Raw counts behind a specialist's calculated performance score - see
/// <see cref="SpecialistPerformanceCalculator"/> for the math. Application
/// composes this from Bookings' own data in a later commit; Domain only
/// defines the shape and the calculation, never fetches these numbers
/// itself - <c>Domain.Specialists</c> deliberately has no dependency on
/// <c>Domain.Bookings</c>, same "vertical slice independence" reasoning
/// <see cref="Specialist"/>'s own doc comment already establishes.
/// </summary>
public sealed record SpecialistPerformanceIndicators(
    int CompletedBookingCount,
    int CancelledBookingCount,
    int NoShowBookingCount);
