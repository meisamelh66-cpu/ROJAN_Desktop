namespace Rojan.Desktop.Domain.Reporting;

/// <summary>Static metadata for one KPI the engine can compute - <see cref="Unit"/> is a display suffix/prefix hint (e.g. <c>"$"</c>, <c>"%"</c>, <c>""</c>), not a currency/locale concern (that stays Presentation's job via <c>ICurrencyFormatter</c>).</summary>
public sealed record KpiDefinition(string Id, string Name, KpiType KpiType, string Unit);
