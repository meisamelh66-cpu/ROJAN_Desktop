namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Display hint for a <see cref="ReportColumn"/> - drives alignment/formatting in the Report Viewer, not the underlying value's storage (every <see cref="ReportRow"/> value is a display-ready string).</summary>
public enum ReportColumnDataType
{
    Text,
    Number,
    Currency,
    Percentage,
    Date,
}
