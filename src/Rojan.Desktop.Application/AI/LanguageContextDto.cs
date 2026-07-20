namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The Prompt System's Language Context block, supplied by the caller
/// (a Presentation ViewModel, via <c>ILocalizationService</c>) rather than
/// looked up by Application.AI itself - Application never depends on the
/// Localization Platform's concrete services directly, only on this plain
/// data shape, keeping the two platforms decoupled per this phase's
/// explicit "use abstraction interfaces, avoid tight coupling" instruction.
/// </summary>
public sealed record LanguageContextDto(string LanguageCode, string LanguageName, bool IsRightToLeft);
