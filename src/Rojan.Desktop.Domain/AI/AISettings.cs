namespace Rojan.Desktop.Domain.AI;

/// <summary>Feature-level AI toggles (distinct from <see cref="AIProviderConfiguration"/>'s model selection) - the AI Settings screen's own preferences.</summary>
public sealed record AISettings(
    bool InsightsEnabled,
    bool SmartNotificationsEnabled,
    bool DailySummaryEnabled,
    bool AutoGenerateRecommendations);
