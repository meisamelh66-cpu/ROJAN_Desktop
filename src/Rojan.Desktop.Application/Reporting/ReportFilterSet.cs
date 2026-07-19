using System.Globalization;

namespace Rojan.Desktop.Application.Reporting;

/// <summary>
/// The Filter Engine's parsing half - turns the generic
/// <see cref="ReportFilterDto"/> list every report receives into typed
/// values <see cref="ReportExecutionQueryService"/>'s aggregation branches
/// can use directly. A <see cref="FilterType.DateRange"/> filter's
/// <c>Value</c> is always <c>"yyyy-MM-dd|yyyy-MM-dd"</c> (start|end);
/// every other filter type carries a single raw id/status/category value.
/// </summary>
internal sealed class ReportFilterSet
{
    private readonly IReadOnlyList<ReportFilterDto> _filters;

    public ReportFilterSet(IReadOnlyList<ReportFilterDto> filters)
    {
        _filters = filters;
    }

    public (DateTimeOffset Start, DateTimeOffset End)? DateRange
    {
        get
        {
            var filter = _filters.FirstOrDefault(f => f.FilterType == FilterType.DateRange);
            if (filter is null)
            {
                return null;
            }

            var parts = filter.Value.Split('|');
            if (parts.Length != 2)
            {
                return null;
            }

            if (!DateTimeOffset.TryParse(parts[0], CultureInfo.InvariantCulture, DateTimeStyles.None, out var start) ||
                !DateTimeOffset.TryParse(parts[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var end))
            {
                return null;
            }

            return (start, end);
        }
    }

    public string? Get(FilterType filterType) => _filters.FirstOrDefault(f => f.FilterType == filterType)?.Value;

    public bool Matches(FilterType filterType, string? candidateId)
    {
        var value = Get(filterType);
        return value is null || string.Equals(value, candidateId, StringComparison.Ordinal);
    }

    public bool IsWithinDateRange(DateTimeOffset value)
    {
        var range = DateRange;
        return range is null || (value >= range.Value.Start && value <= range.Value.End);
    }
}
