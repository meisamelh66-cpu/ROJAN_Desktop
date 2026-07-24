namespace Rojan.Desktop.Domain.Bookings;

/// <summary>
/// Validation rules for the booking lifecycle - a deliberate deviation from
/// this codebase's usual "Domain is just data + repository contract"
/// minimalism, added because Phase 15 explicitly requires Domain-level
/// booking validation rules now that a real cross-slice workflow writes
/// through this slice.
/// </summary>
public static class BookingRules
{
    // Sprint 3 Commit 3: Pending -> InProgress added so a walk-in/early
    // arrival can start service without first being explicitly Confirmed -
    // every other transition here is unchanged from Phase 15.
    private static readonly Dictionary<BookingStatus, BookingStatus[]> ValidTransitions = new()
    {
        [BookingStatus.Pending] = [BookingStatus.Confirmed, BookingStatus.InProgress, BookingStatus.Cancelled],
        [BookingStatus.Confirmed] = [BookingStatus.InProgress, BookingStatus.Cancelled, BookingStatus.NoShow],
        [BookingStatus.InProgress] = [BookingStatus.Completed, BookingStatus.Cancelled],
        [BookingStatus.Completed] = [],
        [BookingStatus.Cancelled] = [],
        [BookingStatus.NoShow] = [],
    };

    /// <summary>Whether moving a booking directly from <paramref name="from"/> to <paramref name="to"/> is legal. <see cref="BookingStatus.Completed"/>, <see cref="BookingStatus.Cancelled"/>, and <see cref="BookingStatus.NoShow"/> are terminal - no transition out of them is valid.</summary>
    public static bool IsValidTransition(BookingStatus from, BookingStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>A booking's duration must be positive and within a single working day (capped at 8 hours) - guards against nonsensical zero/negative durations or a slot-selection bug producing an unbounded duration.</summary>
    public static bool IsValidDuration(int durationMinutes) =>
        durationMinutes is > 0 and <= 480;

    /// <summary>
    /// Whether two (start, duration) time ranges overlap at all - the pure
    /// interval-overlap check behind Sprint 3 Commit 5's double-booking
    /// guard (<c>Application.Bookings.BookingCommandService.CreateBookingAsync</c>
    /// composes this against every existing active booking for the same
    /// specialist, the same "Domain owns the predicate, Application
    /// composes it against repository data" split <see cref="IsValidTransition"/>
    /// already follows).
    /// </summary>
    public static bool TimeRangesOverlap(DateTimeOffset firstStart, int firstDurationMinutes, DateTimeOffset secondStart, int secondDurationMinutes)
    {
        var firstEnd = firstStart.AddMinutes(firstDurationMinutes);
        var secondEnd = secondStart.AddMinutes(secondDurationMinutes);
        return firstStart < secondEnd && secondStart < firstEnd;
    }

    /// <summary>
    /// Whether a booking in this status still occupies real schedule time.
    /// Pending/Confirmed/InProgress do; the three terminal statuses
    /// (Completed/Cancelled/NoShow) don't, so they never conflict with a
    /// new/rescheduled booking and can never themselves be rescheduled
    /// (Sprint 3 Commit 6). Promoted here from a private helper duplicated
    /// between the double-booking conflict check and the new
    /// reschedule-eligibility check, so both share one definition.
    /// </summary>
    public static bool IsActive(BookingStatus status) =>
        status is BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.InProgress;
}
