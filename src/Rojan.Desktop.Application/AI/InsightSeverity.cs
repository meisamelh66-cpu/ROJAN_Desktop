namespace Rojan.Desktop.Application.AI;

/// <summary>Application's own copy of the insight-severity concept - see <see cref="ConversationRole"/>'s doc comment for the mapping rationale.</summary>
public enum InsightSeverity
{
    Info,
    Trend,
    Opportunity,
    Risk,
    Critical,
}
