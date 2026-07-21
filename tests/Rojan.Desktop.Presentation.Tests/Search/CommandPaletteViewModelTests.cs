using System.Windows.Input;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Tests.Dialogs;
using Rojan.Desktop.Presentation.ViewModels.Search;

namespace Rojan.Desktop.Presentation.Tests.Search;

/// <summary>Exercises <see cref="CommandPaletteViewModel"/>'s candidate aggregation, ranking-triggered search, keyboard-navigation selection, execution (Page navigation / Command invocation), and favorites/recent-search behavior.</summary>
public sealed class CommandPaletteViewModelTests
{
    private static SearchCandidate CustomerCandidate(string id, string title) =>
        new(id, SearchResultType.Customer, title, string.Empty, [], "page:customers");

    private static ModuleDescriptor Module(string id, string title) =>
        new(new ModuleMetadata(id, title, string.Empty, 0), _ => new NoOpViewModel());

    private sealed class NoOpViewModel : ViewModelBase
    {
    }

    private static CommandPaletteViewModel CreateViewModel(
        StubGlobalSearchIndexService? indexService = null,
        StubModuleRegistry? moduleRegistry = null,
        StubNavigationService? navigationService = null,
        StubDialogService? dialogService = null,
        IReadOnlyDictionary<string, ICommand>? commandActions = null) =>
        new(
            indexService ?? new StubGlobalSearchIndexService(),
            new SearchRankingService(),
            new StubSearchHistoryStore(),
            new StubSearchFavoritesStore(),
            moduleRegistry ?? new StubModuleRegistry([]),
            navigationService ?? new StubNavigationService(),
            dialogService ?? new StubDialogService(),
            commandActions ?? new Dictionary<string, ICommand>());

    [Fact]
    public async Task SearchText_MatchingQuery_PopulatesResults()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();

        viewModel.SearchText = "Sarah";
        await Task.Delay(10);

        Assert.Single(viewModel.Results);
        Assert.True(viewModel.IsSearchActive);
    }

    [Fact]
    public async Task SearchText_Empty_ClearsResultsAndDeactivatesSearch()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Sarah";
        await Task.Delay(10);

        viewModel.SearchText = string.Empty;
        await Task.Delay(10);

        Assert.Empty(viewModel.Results);
        Assert.False(viewModel.IsSearchActive);
    }

    [Fact]
    public async Task SearchText_NoMatch_ResultsStayEmpty()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();

        viewModel.SearchText = "zzz999";
        await Task.Delay(10);

        Assert.Empty(viewModel.Results);
    }

    [Fact]
    public async Task SelectNextCommand_MovesSelectedIndexForward()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Alice"), CustomerCandidate("c2", "Alice Two")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Alice";
        await Task.Delay(10);

        Assert.Equal(0, viewModel.SelectedIndex);
        viewModel.SelectNextCommand.Execute(null);

        Assert.Equal(1, viewModel.SelectedIndex);
    }

    [Fact]
    public async Task SelectNextCommand_AtLastIndex_StaysClamped()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Alice")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Alice";
        await Task.Delay(10);

        viewModel.SelectNextCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedIndex);
    }

    [Fact]
    public async Task SelectPreviousCommand_AtFirstIndex_StaysClamped()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Alice"), CustomerCandidate("c2", "Alice Two")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Alice";
        await Task.Delay(10);

        viewModel.SelectPreviousCommand.Execute(null);

        Assert.Equal(0, viewModel.SelectedIndex);
    }

    [Fact]
    public async Task ExecuteSelectedCommand_PageResult_NavigatesToTheMatchingModule()
    {
        var descriptor = Module("customers", "Customers");
        var registry = new StubModuleRegistry([descriptor]);
        var navigation = new StubNavigationService();
        var viewModel = CreateViewModel(moduleRegistry: registry, navigationService: navigation);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Customers";
        await Task.Delay(10);

        viewModel.ExecuteSelectedCommand.Execute(null);
        await Task.Delay(10);

        Assert.Single(navigation.NavigatedDescriptors);
        Assert.Equal("customers", navigation.NavigatedDescriptors[0].Metadata.Id);
    }

    [Fact]
    public async Task ExecuteSelectedCommand_PageResult_ClosesTheDialog()
    {
        var descriptor = Module("customers", "Customers");
        var registry = new StubModuleRegistry([descriptor]);
        var dialogService = new StubDialogService();
        var viewModel = CreateViewModel(moduleRegistry: registry, dialogService: dialogService);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Customers";
        await Task.Delay(10);

        viewModel.ExecuteSelectedCommand.Execute(null);
        await Task.Delay(10);

        Assert.Equal(1, dialogService.CloseCount);
    }

    [Fact]
    public async Task ExecuteSelectedCommand_CommandResult_InvokesTheMappedAction()
    {
        var invoked = false;
        var commandActions = new Dictionary<string, ICommand>
        {
            ["toggle-sidebar"] = new RelayCommand(_ => invoked = true),
        };
        var viewModel = CreateViewModel(commandActions: commandActions);
        await viewModel.InitializeAsync();
        viewModel.SearchText = Strings.Search_Command_ToggleSidebar;
        await Task.Delay(10);

        Assert.NotEmpty(viewModel.Results);
        viewModel.ExecuteSelectedCommand.Execute(null);
        await Task.Delay(10);

        Assert.True(invoked);
    }

    [Fact]
    public async Task ExecuteSelectedCommand_RecordsTheSearchInHistory()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Sarah";
        await Task.Delay(10);

        viewModel.ExecuteSelectedCommand.Execute(null);
        await Task.Delay(10);

        Assert.Contains("Sarah", viewModel.RecentSearches);
    }

    [Fact]
    public async Task ToggleFavoriteCommand_TogglesRowIsFavorite()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Sarah";
        await Task.Delay(10);
        var row = viewModel.Results[0];
        Assert.False(row.IsFavorite);

        row.ToggleFavoriteCommand.Execute(null);
        await Task.Delay(10);

        Assert.True(row.IsFavorite);
    }

    [Fact]
    public async Task ClearHistoryCommand_RemovesEveryRecentSearch()
    {
        var index = new StubGlobalSearchIndexService([CustomerCandidate("c1", "Sarah Johnson")]);
        var viewModel = CreateViewModel(indexService: index);
        await viewModel.InitializeAsync();
        viewModel.SearchText = "Sarah";
        await Task.Delay(10);
        viewModel.ExecuteSelectedCommand.Execute(null);
        await Task.Delay(10);
        Assert.NotEmpty(viewModel.RecentSearches);

        viewModel.ClearHistoryCommand.Execute(null);
        await Task.Delay(10);

        Assert.Empty(viewModel.RecentSearches);
    }

    [Fact]
    public async Task SelectRecentSearchCommand_SetsSearchTextToTheChosenQuery()
    {
        var viewModel = CreateViewModel();
        await viewModel.InitializeAsync();

        viewModel.SelectRecentSearchCommand.Execute("previous query");

        Assert.Equal("previous query", viewModel.SearchText);
    }
}
