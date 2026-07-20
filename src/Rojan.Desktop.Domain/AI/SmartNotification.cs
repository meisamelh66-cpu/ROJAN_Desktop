namespace Rojan.Desktop.Domain.AI;

/// <summary>One NotificationInsightService alert - a single-sentence, immediately-actionable distillation of an <see cref="AIInsight"/>, for the Smart Notifications feature.</summary>
public sealed record SmartNotification(
    string Id,
    InsightSeverity Severity,
    string Message,
    DateTimeOffset GeneratedAt);
