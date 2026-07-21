namespace Rojan.Desktop.Application.Reporting;

/// <summary>Application's own copy of the filter-type concept - see <see cref="ReportType"/>'s doc comment for the mapping rationale.</summary>
public enum FilterType
{
    DateRange,
    Branch,
    Employee,
    Specialist,
    Service,
    Customer,
    Status,
    Category,
    Supplier,
}
