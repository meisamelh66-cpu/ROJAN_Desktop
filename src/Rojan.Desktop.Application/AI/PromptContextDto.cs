namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The Prompt System's fully-composed context, built by
/// <see cref="IPromptBuilder"/> from a <see cref="PromptTemplateDto"/>
/// plus every context block this phase's Prompt System requires: System
/// Prompt, Developer Prompt, User Prompt, Business Context, Analytics
/// Context (from <see cref="IAnalyticsContextProvider"/>), Language
/// Context (supplied by the caller - Application never reaches into the
/// Localization Platform directly, see <see cref="IPromptBuilder"/>'s own
/// doc comment), and Session Context. <see cref="AIOrchestrator"/> flattens
/// this into the actual provider request.
/// </summary>
public sealed record PromptContextDto(
    string SystemPrompt,
    string DeveloperPrompt,
    string UserPrompt,
    string BusinessContext,
    string AnalyticsContext,
    string LanguageContext,
    string SessionContext);
