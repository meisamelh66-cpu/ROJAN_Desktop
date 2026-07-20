using System.Globalization;
using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.AI;

public sealed class PromptBuilder : IPromptBuilder
{
    private const string DeveloperPromptText =
        "Answer concisely, in plain language a salon owner would understand. Prefer concrete numbers from the supplied context over generic advice. If the context doesn't cover the question, say so plainly instead of guessing.";

    private readonly IIntentClassifier _intentClassifier;
    private readonly IPromptTemplateRepository _promptTemplateRepository;
    private readonly IContextProvider _contextProvider;
    private readonly IAnalyticsContextProvider _analyticsContextProvider;

    public PromptBuilder(
        IIntentClassifier intentClassifier,
        IPromptTemplateRepository promptTemplateRepository,
        IContextProvider contextProvider,
        IAnalyticsContextProvider analyticsContextProvider)
    {
        _intentClassifier = intentClassifier;
        _promptTemplateRepository = promptTemplateRepository;
        _contextProvider = contextProvider;
        _analyticsContextProvider = analyticsContextProvider;
    }

    public async Task<PromptContextDto> BuildAsync(
        string userMessage,
        int sessionMessageCount,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken = default)
    {
        var category = _intentClassifier.ClassifyIntent(userMessage);
        var template = await _promptTemplateRepository.GetTemplateForCategoryAsync(category, cancellationToken).ConfigureAwait(false)
            ?? await _promptTemplateRepository.GetTemplateForCategoryAsync(InsightCategory.General, cancellationToken).ConfigureAwait(false);

        var systemPrompt = "You are the ROJAN Business Assistant, an AI embedded in a salon/beauty-business management application. " +
            (template is not null ? template.Body.Replace("{period}", "this month", StringComparison.Ordinal) : "Answer the user's question using the supplied business context.");

        var businessContext = await _contextProvider.GetBusinessContextAsync(cancellationToken).ConfigureAwait(false);
        var analyticsContext = await _analyticsContextProvider.GetAnalyticsContextAsync(AppReporting.AnalyticsPeriod.Monthly, cancellationToken).ConfigureAwait(false);

        var languageContextText = string.Create(CultureInfo.InvariantCulture, $"Respond in {languageContext.LanguageName} ({languageContext.LanguageCode}).{(languageContext.IsRightToLeft ? " This language reads right-to-left." : string.Empty)}");
        var sessionContextText = string.Create(CultureInfo.InvariantCulture, $"This is message #{sessionMessageCount + 1} in the current session.");

        return new PromptContextDto(systemPrompt, DeveloperPromptText, userMessage, businessContext, analyticsContext, languageContextText, sessionContextText);
    }
}
