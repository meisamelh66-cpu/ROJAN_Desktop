using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

/// <summary>Domain&lt;-&gt;Application mapping for the AI Center vertical slice - internal, only this project's own services call it, matching every other module's Mapper convention (<c>CustomerMapper</c>, <c>ReportingMapper</c>).</summary>
internal static class AIMapper
{
    public static ConversationSessionDto MapSession(DomainAI.ConversationSession session) =>
        new(session.Id, session.Title, session.CreatedAt, session.UpdatedAt, session.IsPinned);

    public static ConversationMessageDto MapMessage(DomainAI.ConversationMessage message) =>
        new(message.Id, message.SessionId, MapRole(message.Role), message.Content, message.CreatedAt, message.TokenCount);

    public static PromptTemplateDto MapTemplate(DomainAI.PromptTemplate template) =>
        new(template.Id, template.Name, MapCategory(template.Category), template.Body, template.IsSystemDefined);

    public static AIProviderConfigurationDto MapProviderConfiguration(DomainAI.AIProviderConfiguration configuration) =>
        new(MapProviderType(configuration.ProviderType), configuration.ModelId, configuration.IsEnabled);

    public static DomainAI.AIProviderConfiguration MapProviderConfiguration(AIProviderConfigurationDto configuration) =>
        new(MapProviderType(configuration.ProviderType), configuration.ModelId, configuration.IsEnabled);

    public static AISettingsDto MapSettings(DomainAI.AISettings settings) =>
        new(settings.InsightsEnabled, settings.SmartNotificationsEnabled, settings.DailySummaryEnabled, settings.AutoGenerateRecommendations);

    public static DomainAI.AISettings MapSettings(AISettingsDto settings) =>
        new(settings.InsightsEnabled, settings.SmartNotificationsEnabled, settings.DailySummaryEnabled, settings.AutoGenerateRecommendations);

    public static TokenUsageRecordDto MapTokenUsage(DomainAI.TokenUsageRecord record) =>
        new(record.Id, record.SessionId, MapProviderType(record.ProviderType), record.PromptTokens, record.CompletionTokens, record.TotalTokens, record.RecordedAt);

    public static AIInsightDto MapInsight(DomainAI.AIInsight insight) =>
        new(insight.Id, MapCategory(insight.Category), MapSeverity(insight.Severity), insight.Title, insight.Description, insight.MetricValue, insight.ChangePercent, insight.GeneratedAt);

    public static AIRecommendationDto MapRecommendation(DomainAI.AIRecommendation recommendation) =>
        new(recommendation.Id, MapCategory(recommendation.Category), MapPriority(recommendation.Priority), recommendation.Title, recommendation.Description, recommendation.SuggestedAction);

    public static SuggestedTaskDto MapSuggestedTask(DomainAI.SuggestedTask task) =>
        new(task.Id, task.Title, MapCategory(task.Category), MapPriority(task.Priority), task.SuggestedDueDate);

    public static SmartNotificationDto MapNotification(DomainAI.SmartNotification notification) =>
        new(notification.Id, MapSeverity(notification.Severity), notification.Message, notification.GeneratedAt);

    public static BusinessHealthComponentDto MapHealthComponent(DomainAI.BusinessHealthComponent component) =>
        new(MapCategory(component.Category), component.Label, component.Score, component.Weight);

    public static BusinessHealthScoreDto MapHealthScore(DomainAI.BusinessHealthScore score) =>
        new(score.OverallScore, score.Components.Select(MapHealthComponent).ToList(), score.Summary, score.ComputedAt);

    public static ConversationRole MapRole(DomainAI.ConversationRole role) => role switch
    {
        DomainAI.ConversationRole.System => ConversationRole.System,
        DomainAI.ConversationRole.Developer => ConversationRole.Developer,
        DomainAI.ConversationRole.User => ConversationRole.User,
        DomainAI.ConversationRole.Assistant => ConversationRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown conversation role."),
    };

    public static DomainAI.ConversationRole MapRole(ConversationRole role) => role switch
    {
        ConversationRole.System => DomainAI.ConversationRole.System,
        ConversationRole.Developer => DomainAI.ConversationRole.Developer,
        ConversationRole.User => DomainAI.ConversationRole.User,
        ConversationRole.Assistant => DomainAI.ConversationRole.Assistant,
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown conversation role."),
    };

    public static AIProviderType MapProviderType(DomainAI.AIProviderType providerType) => providerType switch
    {
        DomainAI.AIProviderType.Mock => AIProviderType.Mock,
        DomainAI.AIProviderType.OpenAI => AIProviderType.OpenAI,
        DomainAI.AIProviderType.Anthropic => AIProviderType.Anthropic,
        DomainAI.AIProviderType.AzureOpenAI => AIProviderType.AzureOpenAI,
        DomainAI.AIProviderType.LocalModel => AIProviderType.LocalModel,
        _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Unknown provider type."),
    };

    public static DomainAI.AIProviderType MapProviderType(AIProviderType providerType) => providerType switch
    {
        AIProviderType.Mock => DomainAI.AIProviderType.Mock,
        AIProviderType.OpenAI => DomainAI.AIProviderType.OpenAI,
        AIProviderType.Anthropic => DomainAI.AIProviderType.Anthropic,
        AIProviderType.AzureOpenAI => DomainAI.AIProviderType.AzureOpenAI,
        AIProviderType.LocalModel => DomainAI.AIProviderType.LocalModel,
        _ => throw new ArgumentOutOfRangeException(nameof(providerType), providerType, "Unknown provider type."),
    };

    public static InsightCategory MapCategory(DomainAI.InsightCategory category) => category switch
    {
        DomainAI.InsightCategory.Revenue => InsightCategory.Revenue,
        DomainAI.InsightCategory.Customer => InsightCategory.Customer,
        DomainAI.InsightCategory.Appointment => InsightCategory.Appointment,
        DomainAI.InsightCategory.Inventory => InsightCategory.Inventory,
        DomainAI.InsightCategory.Hr => InsightCategory.Hr,
        DomainAI.InsightCategory.Payroll => InsightCategory.Payroll,
        DomainAI.InsightCategory.Attendance => InsightCategory.Attendance,
        DomainAI.InsightCategory.Commission => InsightCategory.Commission,
        DomainAI.InsightCategory.General => InsightCategory.General,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown insight category."),
    };

    public static InsightSeverity MapSeverity(DomainAI.InsightSeverity severity) => severity switch
    {
        DomainAI.InsightSeverity.Info => InsightSeverity.Info,
        DomainAI.InsightSeverity.Trend => InsightSeverity.Trend,
        DomainAI.InsightSeverity.Opportunity => InsightSeverity.Opportunity,
        DomainAI.InsightSeverity.Risk => InsightSeverity.Risk,
        DomainAI.InsightSeverity.Critical => InsightSeverity.Critical,
        _ => throw new ArgumentOutOfRangeException(nameof(severity), severity, "Unknown insight severity."),
    };

    public static DomainAI.InsightCategory MapCategory(InsightCategory category) => category switch
    {
        InsightCategory.Revenue => DomainAI.InsightCategory.Revenue,
        InsightCategory.Customer => DomainAI.InsightCategory.Customer,
        InsightCategory.Appointment => DomainAI.InsightCategory.Appointment,
        InsightCategory.Inventory => DomainAI.InsightCategory.Inventory,
        InsightCategory.Hr => DomainAI.InsightCategory.Hr,
        InsightCategory.Payroll => DomainAI.InsightCategory.Payroll,
        InsightCategory.Attendance => DomainAI.InsightCategory.Attendance,
        InsightCategory.Commission => DomainAI.InsightCategory.Commission,
        InsightCategory.General => DomainAI.InsightCategory.General,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown insight category."),
    };

    public static RecommendationPriority MapPriority(DomainAI.RecommendationPriority priority) => priority switch
    {
        DomainAI.RecommendationPriority.Low => RecommendationPriority.Low,
        DomainAI.RecommendationPriority.Medium => RecommendationPriority.Medium,
        DomainAI.RecommendationPriority.High => RecommendationPriority.High,
        DomainAI.RecommendationPriority.Urgent => RecommendationPriority.Urgent,
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown recommendation priority."),
    };
}
