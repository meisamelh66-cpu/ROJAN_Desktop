using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Infrastructure.Help;
using Rojan.Desktop.Presentation.Help;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>
/// The real Help stack (registry/query/search/content-resolver), plus
/// in-memory favorites/recently-viewed stores - <see cref="MainWindowViewModel"/>'s
/// 5 Help constructor parameters, factored out here since none of these
/// navigation/branch-switcher tests exercise Help behavior directly (only
/// <see cref="MainWindowViewModel.OpenHelpCommand"/> would).
/// </summary>
internal static class TestHelpServices
{
    public static IHelpQueryService QueryService { get; } = new HelpQueryService(new HelpTopicRegistry());

    public static IHelpContentResolver ContentResolver { get; } = new HelpContentResolver();

    public static IHelpSearchService SearchService { get; } = new HelpSearchService();

    public static IHelpFavoritesStore CreateFavoritesStore() => new StubHelpFavoritesStore();

    public static IHelpRecentlyViewedStore CreateRecentlyViewedStore() => new StubHelpRecentlyViewedStore();
}
