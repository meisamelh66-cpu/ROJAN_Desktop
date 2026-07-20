namespace Rojan.Desktop.Domain.AI;

/// <summary>What kind of signal an <see cref="AIInsight"/> represents - the InsightEngine's Trend/Risk/Opportunity Detection requirement expressed as a classification rather than three separate engines.</summary>
public enum InsightSeverity
{
    Info,
    Trend,
    Opportunity,
    Risk,
    Critical,
}
