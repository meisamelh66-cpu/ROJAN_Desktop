using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;

namespace Rojan.Desktop.Presentation.Search;

/// <summary>
/// Phase 28: Enterprise Global Search &amp; Command Palette. Builds the
/// two candidate groups that need already-localized text - Pages
/// (every registered module, including Settings, itself just another
/// module) and Commands (a small curated set of app-wide actions) -
/// since only Presentation can see <c>Strings</c>. Live business data
/// (Customer/Booking/Specialist/Service/Product) comes from
/// <see cref="IGlobalSearchIndexService"/> instead, already plain text
/// at its own DTO layer; <c>ViewModels.Search.CommandPaletteViewModel</c>
/// combines both before ranking.
/// </summary>
public static class StaticSearchCatalog
{
    /// <summary>One candidate per registered module - <see cref="SearchCandidate.ActionKey"/> is <c>"page:{moduleId}"</c>, resolved back to a <c>ModuleDescriptor</c> and navigated to by <c>CommandPaletteViewModel</c>.</summary>
    public static IReadOnlyList<SearchCandidate> GetPageCandidates(IReadOnlyList<ModuleDescriptor> modules) =>
        modules.Select(m => new SearchCandidate(
            $"page:{m.Metadata.Id}",
            SearchResultType.Page,
            m.Metadata.Title,
            string.Empty,
            [],
            $"page:{m.Metadata.Id}")).ToList();

    /// <summary>A small, curated set of app-wide commands - the "Command execution" requirement. Each <see cref="SearchCandidate.ActionKey"/> is <c>"command:{commandId}"</c>, resolved against the command-action map <c>Shell.MainWindowViewModel</c> supplies when constructing the palette (only Shell has every one of these as an already-wired <see cref="System.Windows.Input.ICommand"/>).</summary>
    public static IReadOnlyList<SearchCandidate> GetCommandCandidates() =>
    [
        Command("toggle-sidebar", Strings.Search_Command_ToggleSidebar),
        Command("toggle-notifications", Strings.Search_Command_ToggleNotifications),
        Command("open-help", Strings.Search_Command_OpenHelp),
        Command("toggle-silent-mode", Strings.Search_Command_ToggleSilentMode),
        Command("go-back", Strings.Search_Command_GoBack),
        Command("go-forward", Strings.Search_Command_GoForward),
        Command("open-branch-switcher", Strings.Search_Command_OpenBranchSwitcher),
        Command("open-workspace-switcher", Strings.Search_Command_OpenWorkspaceSwitcher),
    ];

    private static SearchCandidate Command(string commandId, string title) =>
        new($"command:{commandId}", SearchResultType.Command, title, string.Empty, [], $"command:{commandId}");
}
