namespace Rojan.Desktop.Domain.Reporting;

/// <summary>One column of a <see cref="ReportDefinition"/>'s output shape - <see cref="Key"/> is the matching key into every <see cref="ReportRow"/>'s <c>Values</c> dictionary.</summary>
public sealed record ReportColumn(string Key, string Header, ReportColumnDataType DataType);
