namespace Rojan.Desktop.Application.Customers;

/// <summary>A single labeled statistic for the Customer 360 profile's statistics cards - deliberately just Label/Value, no trend fields, unlike Dashboard.KpiMetricDto (a profile stat isn't a KPI reading, and Customers stays independent of the Dashboard vertical slice).</summary>
public sealed record CustomerStatDto(string Label, string Value);
