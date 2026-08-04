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
/// Sprint 3 Commit 5: <see cref="CreateBookingAsync"/> also rejects a
/// double-booking - a new specialist/time range that overlaps an existing
/// active booking for the same specialist, in the same
/// organization/branch. This is the one safety net both booking-creation
/// paths in this app share: the Wizard already reserves a real Calendar
/// slot before reaching here (via <c>BookingWorkflowService</c>), but the
/// Bookings page's free-text quick-add form never touches Calendar at
/// all, so this check is the only thing standing between it and silently
/// double-booking a specialist. Deliberately checks existing *bookings*
/// (via the already-injected repository), not Calendar's own
/// reserved-slot state - checking Calendar's Booked status here would
/// reject the Wizard's own just-reserved slot, since
/// <c>BookingWorkflowService.CreateBookingAsync</c> reserves the Calendar
/// slot before calling this method.
/// Sprint 3 Commit 6: <see cref="RescheduleBookingAsync"/> reuses the same
/// conflict check (now <see cref="EnsureNoConflictAsync"/>, extracted so
/// both writes share one definition), excluding the booking's own current
/// record from the scan, plus rejects rescheduling a terminal-status
/// booking. As with Create, this method never touches Calendar - that
/// orchestration (release old slot / reserve new slot) is
/// <c>BookingWorkflow.BookingWorkflowService.RescheduleBookingAsync</c>'s
/// job, which calls this method only after its own new-slot reservation
/// already succeeded.
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

        await EnsureNoConflictAsync(
            excludeBookingId: string.Empty,
            request.SpecialistId, request.SpecialistName, request.ScheduledAt, request.DurationMinutes,
            organizationId, branchId, cancellationToken).ConfigureAwait(true);

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

        await EnsureNoConflictAsync(
            bookingId, current.SpecialistId, current.SpecialistName, newScheduledAt, current.DurationMinutes,
            current.OrganizationId, current.BranchId, cancellationToken).ConfigureAwait(true);

        var updated = await _repository.RescheduleBookingAsync(bookingId, newScheduledAt, cancellationToken).ConfigureAwait(true);
        return BookingMapper.MapBooking(updated);
    }

    private async Task EnsureNoConflictAsync(
        string excludeBookingId, string specialistId, string specialistName, DateTimeOffset scheduledAt, int durationMinutes,
        string organizationId, string branchId, CancellationToken cancellationToken)
    {
        var existingBookings = await _repository.GetBookingsAsync(cancellationToken).ConfigureAwait(true);
        var hasConflict = existingBookings.Any(existing =>
            existing.Id != excludeBookingId &&
            existing.OrganizationId == organizationId &&
            existing.BranchId == branchId &&
            DomainBookings.BookingRules.IsActive(existing.Status) &&
            IsSameSpecialist(existing, specialistId, specialistName) &&
            DomainBookings.BookingRules.TimeRangesOverlap(scheduledAt, durationMinutes, existing.ScheduledAt, existing.DurationMinutes));

        if (hasConflict)
        {
            throw new InvalidOperationException("This specialist already has a booking that overlaps the requested time.");
        }
    }

    /// <summary>Matches by id when both sides have a real one (more precise); otherwise falls back to a case-insensitive name match, since the free-text quick-add form never populates a real SpecialistId. Two bookings that both lack any specialist reference never match - there is nothing to conflict over.</summary>
    private static bool IsSameSpecialist(DomainBookings.Booking existing, string specialistId, string specialistName)
    {
        if (!string.IsNullOrEmpty(existing.SpecialistId) && !string.IsNullOrEmpty(specialistId))
        {
            return string.Equals(existing.SpecialistId, specialistId, StringComparison.Ordinal);
        }

        return !string.IsNullOrWhiteSpace(existing.SpecialistName)
            && !string.IsNullOrWhiteSpace(specialistName)
            && string.Equals(existing.SpecialistName, specialistName, StringComparison.OrdinalIgnoreCase);
    }
}
