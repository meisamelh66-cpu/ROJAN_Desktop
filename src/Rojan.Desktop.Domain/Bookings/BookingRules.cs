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
}
