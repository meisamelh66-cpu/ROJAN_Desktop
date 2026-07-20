namespace Rojan.Desktop.Application.Help;

/// <summary>Phase 26: Help Search (26.6). Enterprise keyword/topic search over an already-resolved candidate set - see <see cref="HelpSearchCandidate"/>'s own doc comment for why candidates carry plain text rather than localization keys.</summary>
public interface IHelpSearchService
{
    /// <summary>Ranked matches for <paramref name="query"/> among <paramref name="candidates"/>, highest score first. An empty or whitespace-only query returns an empty result list (the caller shows "recent searches"/browsing instead, not every topic unranked).</summary>
    public IReadOnlyList<HelpSearchResultDto> Search(IReadOnlyList<HelpSearchCandidate> candidates, string query);
}
