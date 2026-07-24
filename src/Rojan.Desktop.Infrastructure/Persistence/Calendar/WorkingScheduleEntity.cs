namespace Rojan.Desktop.Infrastructure.Persistence.Calendar;

/// <summary>
/// EF Core persistence model for a specialist's recurring working hours
/// on one day of the week - field-for-field mirror of
/// <see cref="Domain.Calendar.WorkingSchedule"/> minus <c>Breaks</c>,
/// which gets its own child table (<see cref="WorkingScheduleBreakEntity"/>)
/// since a schedule can have several - same "Infrastructure owns the
/// translation, Domain stays persistence-ignorant" reasoning
/// <see cref="Customers.CustomerEntity"/> already establishes. Unlike
/// <c>Domain.Bookings.Booking</c>'s <c>SpecialistId</c>,
/// <see cref="SpecialistId"/> here is also a free-form, unvalidated
/// reference (see <c>Domain.Calendar.WorkingSchedule</c>'s own doc
/// comment - Calendar deliberately does not depend on Domain.Specialists
/// either) - no foreign key to the Specialists table.
/// </summary>
public sealed class WorkingScheduleEntity
{
    public string Id { get; set; } = string.Empty;

    public string SpecialistId { get; set; } = string.Empty;

    public string SpecialistName { get; set; } = string.Empty;

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTime { get; set; }

    public TimeSpan EndTime { get; set; }
}
