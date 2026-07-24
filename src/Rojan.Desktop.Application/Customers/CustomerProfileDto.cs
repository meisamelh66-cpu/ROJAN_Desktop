namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Everything the Customer 360 profile screen needs for one customer - the
/// full aggregate fetched together as a single unit of work, same reasoning
/// as Dashboard.DashboardOverviewDto. <see cref="BookingSummary"/> (Sprint 4
/// Commit 3) is composed from <c>AppBookings.IBookingQueryService</c> rather
/// than duplicated here - see <see cref="CustomerBookingSummaryDto"/>'s own
/// doc comment. <see cref="Insights"/> (Sprint 4 Commit 5) is calculated
/// from <see cref="BookingSummary"/> and <see cref="Activity"/> by
/// <see cref="CustomerInsightsCalculator"/> - see its own doc comment.
/// </summary>
public sealed record CustomerProfileDto(
    CustomerDto Customer,
    IReadOnlyList<CustomerNoteDto> Notes,
    IReadOnlyList<CustomerTagDto> Tags,
    IReadOnlyList<CustomerActivityDto> Activity,
    IReadOnlyList<CustomerStatDto> Statistics,
    CustomerBookingSummaryDto BookingSummary,
    CustomerInsightsDto Insights);
