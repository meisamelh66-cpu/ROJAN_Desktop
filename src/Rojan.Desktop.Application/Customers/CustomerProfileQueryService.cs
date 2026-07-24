using System.Globalization;
using Rojan.Desktop.Application.Organizations;
using AppBookings = Rojan.Desktop.Application.Bookings;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerProfileQueryService"/> implementation -
/// fetches the customer plus its notes/tags/activity from
/// <see cref="DomainCustomers.ICustomerRepository"/> and assembles the
/// aggregate <see cref="CustomerProfileDto"/>, including the statistics
/// cards (computed here, not stored - a business rule about what a
/// profile's key numbers are, which belongs in Application, not Domain
/// or Infrastructure).
///
/// Phase 22A: a customer outside the current organization/branch is
/// treated exactly like a non-existent one (same
/// <see cref="InvalidOperationException"/>, not a distinct "forbidden"
/// error) - Organization/Branch Scoping must not even confirm that a
/// given id exists elsewhere.
///
/// Sprint 4 Commit 3: <see cref="CustomerProfileDto.BookingSummary"/> is
/// composed over <see cref="AppBookings.IBookingQueryService.GetBookingsAsync"/>
/// (already Organization/Branch-scoped by <c>Bookings.BookingQueryService</c>
/// itself, so no extra scoping is needed here) - the Customer module
/// consumes Booking's existing Application-layer query surface rather than
/// duplicating any <c>Domain.Bookings</c> logic, same "Application composes
/// over a sibling module" shape <c>Reporting.KpiEngineQueryService</c>
/// already establishes. Both layers stay same-layer siblings (Application
/// depending on Application), never a Domain-level or circular reference.
///
/// Sprint 4 Commit 5: <see cref="CustomerProfileDto.Insights"/> is computed
/// by <see cref="CustomerInsightsCalculator"/> from the same
/// <see cref="CustomerBookingSummaryDto"/> and mapped Activity this method
/// already builds - no new repository call, no new storage.
/// </summary>
public sealed class CustomerProfileQueryService : ICustomerProfileQueryService
{
    private readonly DomainCustomers.ICustomerRepository _repository;
    private readonly AppBookings.IBookingQueryService _bookingQueryService;
    private readonly IEnterpriseContext _enterpriseContext;

    public CustomerProfileQueryService(
        DomainCustomers.ICustomerRepository repository,
        AppBookings.IBookingQueryService bookingQueryService,
        IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _bookingQueryService = bookingQueryService;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<CustomerProfileDto> GetProfileAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetCustomerByIdAsync(customerId, cancellationToken).ConfigureAwait(true);
        if (customer is null || customer.OrganizationId != _enterpriseContext.CurrentOrganizationId ||
            (_enterpriseContext.CurrentBranchId is not null && customer.BranchId != _enterpriseContext.CurrentBranchId))
        {
            throw new InvalidOperationException($"Customer '{customerId}' was not found.");
        }

        var notes = await _repository.GetNotesAsync(customerId, cancellationToken).ConfigureAwait(true);
        var tags = await _repository.GetTagsAsync(customerId, cancellationToken).ConfigureAwait(true);
        var activity = await _repository.GetActivityAsync(customerId, cancellationToken).ConfigureAwait(true);
        var bookingSummary = await BuildBookingSummaryAsync(customerId, cancellationToken).ConfigureAwait(true);

        var customerDto = CustomerMapper.MapCustomer(customer);
        var mappedActivity = activity.Select(CustomerMapper.MapActivity).ToList();
        var insights = CustomerInsightsCalculator.Calculate(bookingSummary, mappedActivity, DateTimeOffset.Now);

        return new CustomerProfileDto(
            customerDto,
            notes.Select(CustomerMapper.MapNote).ToList(),
            tags.Select(CustomerMapper.MapTag).ToList(),
            mappedActivity,
            BuildStatistics(customerDto, notes.Count, tags.Count),
            bookingSummary,
            insights);
    }

    /// <summary>Filters the already-scoped booking list down to this customer, then splits into upcoming (soonest-first) and past (most-recent-first) by <see cref="AppBookings.BookingDto.ScheduledAt"/> relative to now. A customer with no bookings at all returns <see cref="CustomerBookingSummaryDto.Empty"/> - never a failure.</summary>
    private async Task<CustomerBookingSummaryDto> BuildBookingSummaryAsync(string customerId, CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueryService.GetBookingsAsync(cancellationToken).ConfigureAwait(true);
        var customerBookings = bookings.Where(booking => booking.CustomerId == customerId).ToList();

        if (customerBookings.Count == 0)
        {
            return CustomerBookingSummaryDto.Empty;
        }

        var now = DateTimeOffset.Now;
        var upcoming = customerBookings.Where(booking => booking.ScheduledAt >= now).OrderBy(booking => booking.ScheduledAt).ToList();
        var past = customerBookings.Where(booking => booking.ScheduledAt < now).OrderByDescending(booking => booking.ScheduledAt).ToList();
        var completedCount = customerBookings.Count(booking => booking.Status == AppBookings.BookingStatus.Completed);
        var lastAppointmentDate = past.Count > 0 ? past[0].ScheduledAt : (DateTimeOffset?)null;

        return new CustomerBookingSummaryDto(upcoming, past, customerBookings.Count, completedCount, lastAppointmentDate);
    }

    private static IReadOnlyList<CustomerStatDto> BuildStatistics(CustomerDto customer, int noteCount, int tagCount) =>
    [
        new("ارزش کل خرید", customer.LifetimeValue),
        new("وضعیت", customer.Status.ToString()),
        new("یادداشت‌ها", noteCount.ToString(CultureInfo.InvariantCulture)),
        new("برچسب‌ها", tagCount.ToString(CultureInfo.InvariantCulture)),
        new("آخرین تماس", customer.LastContactedAt.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture)),
    ];
}
