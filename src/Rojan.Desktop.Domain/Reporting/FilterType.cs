namespace Rojan.Desktop.Domain.Reporting;

/// <summary>The Filter Engine's supported filter dimensions - which of these a given report honors is report-specific.</summary>
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
}
