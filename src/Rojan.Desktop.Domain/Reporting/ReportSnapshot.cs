namespace Rojan.Desktop.Domain.Reporting;

/// <summary>
/// A lightweight record that a report was run - the Reporting slice's
/// only genuinely owned, mutable aggregate (everything else it shows is
/// read-through aggregation of sibling modules' own data). Powers both
/// "Recent Reports" (every run, newest first) and "Saved Reports"
/// (<see cref="IsSaved"/> true - user-pinned so it survives being pushed
/// out of the recent list). <see cref="ReportDefinitionId"/>/
/// <see cref="ReportName"/> is the same "id plus denormalized display
/// name" shape every cross-slice reference in this app uses (e.g.
/// <c>Bookings.Booking.CustomerId</c>/<c>CustomerName</c>).
/// </summary>
public sealed record ReportSnapshot(
    string Id,
    string ReportDefinitionId,
    string ReportName,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ReportFilter> AppliedFilters,
    int RowCount,
    bool IsSaved);
