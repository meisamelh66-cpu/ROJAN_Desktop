namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Raw counts behind a service's calculated popularity score - see
/// <see cref="ServicePopularityCalculator"/> for the math. Application
/// composes this from Bookings' own data in a later commit; Domain only
/// defines the shape and the calculation, never fetches these numbers
/// itself - <c>Domain.Services</c> deliberately has no dependency on
/// <c>Domain.Bookings</c>, same "vertical slice independence" reasoning
/// <see cref="SpecialistService"/>'s own doc comment already establishes.
/// </summary>
public sealed record ServicePopularityIndicators(
    int CompletedBookingCount,
    int UpcomingBookingCount);
