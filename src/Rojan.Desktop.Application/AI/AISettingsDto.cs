namespace Rojan.Desktop.Application.AI;

public sealed record AISettingsDto(
    bool InsightsEnabled,
    bool SmartNotificationsEnabled,
    bool DailySummaryEnabled,
    bool AutoGenerateRecommendations);
