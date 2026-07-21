using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// The real ranking algorithm over an empty in-memory index/history/
/// favorites stack - <see cref="MainWindowViewModel"/>'s 4 Global Search
/// constructor parameters, factored out here since none of these
/// navigation/branch-switcher tests exercise Global Search behavior
/// directly, the same reasoning <c>TestHelpServices</c>/
/// <c>TestNotificationServices</c> already establish.
/// </summary>
internal static class TestSearchServices
{
    public static IGlobalSearchIndexService IndexService { get; } = new StubGlobalSearchIndexService();

    public static ISearchRankingService RankingService { get; } = new SearchRankingService();

    public static ISearchHistoryStore CreateHistoryStore() => new StubSearchHistoryStore();

    public static ISearchFavoritesStore CreateFavoritesStore() => new StubSearchFavoritesStore();
}
