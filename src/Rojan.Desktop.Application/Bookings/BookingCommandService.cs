using Rojan.Desktop.Application.Organizations;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Application.Bookings;

/// <summary>
/// Default <see cref="IBookingCommandService"/> implementation. Enforces
/// <see cref="DomainBookings.BookingRules"/> on every write - an invalid
/// duration or an illegal status transition throws rather than silently
/// writing bad data, now that a real cross-slice workflow
/// (<c>BookingWorkflowService</c>) depends on this service's guarantees.
/// Phase 22A: <see cref="CreateBookingAsync"/> stamps the new booking with
/// the current session's organization/branch (<see cref="IEnterpriseContext"/>).
/// Governance correction (ROJAN Architecture Governance V1.0 / ADR-004):
/// <see cref="CreateBookingAsync"/>/<see cref="RescheduleBookingAsync"/> used
/// to reject a double-booking here via a local, non-atomic overlap scan
/// against every existing booking (<c>EnsureNoConflictAsync</c>, Sprint 3
/// Commits 5-6) - Backend is the only Booking Authority; conflict
/// resolution is never computed client-side, not even as a non-authoritative
/// check, so that scan is removed rather than kept as an advisory hint.
/// Both methods now forward directly to <see cref="_repository"/> and rely
/// entirely on Backend's own atomic conflict check.
/// </summary>
public sealed class BookingCommandService : IBookingCommandService
{
    private readonly DomainBookings.IBookingRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public BookingCommandService(DomainBookings.IBookingRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public bool SupportsInProgressAndNoShowStatuses => _repository.SupportsInProgressAndNoShowStatuses;

    public async Task<BookingDto> CreateBookingAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        if (!DomainBookings.BookingRules.IsValidDuration(request.DurationMinutes))
        {
            throw new ArgumentException($"Duration {request.DurationMinutes} minutes is not valid.", nameof(request));
        }

        var organizationId = _enterpriseContext.CurrentOrganizationId ?? string.Empty;
        var branchId = _enterpriseContext.CurrentBranchId ?? string.Empty;

        var booking = new DomainBookings.Booking(
            Guid.NewGuid().ToString(),
            request.CustomerId,
            request.CustomerName,
            request.ServiceId,
            request.ServiceName,
            request.SpecialistId,
            request.SpecialistName,
            request.ScheduledAt,
            request.DurationMinutes,
            request.Price,
            DomainBookings.BookingStatus.Pending,
            request.Notes,
            organizationId,
            branchId);

        var created = await _repository.CreateBookingAsync(booking, cancellationToken).ConfigureAwait(true);
        return BookingMapper.MapBooking(created);
    }

    public async Task<BookingDto> UpdateBookingStatusAsync(string bookingId, BookingStatus status, CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        var domainStatus = BookingMapper.MapStatusToDomain(status);
        if (!DomainBookings.BookingRules.IsValidTransition(current.Status, domainStatus))
        {
            throw new InvalidOperationException($"Cannot transition booking from {current.Status} to {domainStatus}.");
        }

        var updated = await _repository
            .UpdateBookingStatusAsync(bookingId, domainStatus, cancellationToken)
            .ConfigureAwait(true);
        return BookingMapper.MapBooking(updated);
    }

    public async Task<BookingDto> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        var current = await _repository.GetBookingByIdAsync(bookingId, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Booking '{bookingId}' was not found.");

        if (!DomainBookings.BookingRules.IsActive(current.Status))
        {
            throw new InvalidOperationException($"Booking '{bookingId}' cannot be rescheduled from status {current.Status}.");
        }

        var updated = await _repository.RescheduleBookingAsync(bookingId, newScheduledAt, cancellationToken).ConfigureAwait(true);
        return BookingMapper.MapBooking(updated);
    }
}
