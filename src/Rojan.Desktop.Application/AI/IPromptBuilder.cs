using AppReporting = Rojan.Desktop.Application.Reporting;

namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The reusable Prompt System's composition root - builds a
/// <see cref="PromptContextDto"/> from a <see cref="PromptTemplateDto"/>
/// (picked via <see cref="IIntentClassifier"/>) plus every required
/// context block: Business Context (<see cref="IContextProvider"/>),
/// Analytics Context (<see cref="IAnalyticsContextProvider"/>), Language
/// Context (supplied by the caller, never looked up directly - see
/// <see cref="LanguageContextDto"/>'s own doc comment), and Session
/// Context (message count so far). <see cref="AIOrchestrator"/> is this
/// interface's only consumer.
/// </summary>
public interface IPromptBuilder
{
    public Task<PromptContextDto> BuildAsync(
        string userMessage,
        int sessionMessageCount,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken = default);
}
