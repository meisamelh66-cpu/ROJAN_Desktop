using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Search;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Modules;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Navigation;
using Rojan.Desktop.Presentation.Search;

namespace Rojan.Desktop.Presentation.ViewModels.Search;

/// <summary>
/// Phase 28: Enterprise Global Search &amp; Command Palette. Combines
/// live business-data candidates (<see cref="IGlobalSearchIndexService"/>)
/// with the static, already-localized Page/Command catalog
/// (<see cref="StaticSearchCatalog"/>), ranks them
/// (<see cref="ISearchRankingService"/>), and executes whichever result
/// the user selects - a Page result navigates
/// (<see cref="INavigationService.NavigateTo(ModuleDescriptor)"/>), a
/// Command result invokes the matching entry in the constructor's
/// <c>commandActions</c> map (built and supplied by
/// <c>Shell.MainWindowViewModel</c>, the one place every one of those
/// commands already exists as a wired <see cref="ICommand"/> - the same
/// "constructed via <c>new</c> by its opener, opener supplies broader
/// context" shape <c>ViewModels.Help.HelpDialogViewModel</c> already
/// establishes). Constructed fresh per open (not DI-registered) so its
/// candidate cache/search state never leaks between sessions.
/// </summary>
public sealed class CommandPaletteViewModel : ViewModelBase
{
    private readonly IGlobalSearchIndexService _searchIndexService;
    private readonly ISearchRankingService _rankingService;
    private readonly ISearchHistoryStore _historyStore;
    private readonly ISearchFavoritesStore _favoritesStore;
    private readonly IModuleRegistry _moduleRegistry;
    private readonly INavigationService _navigationService;
    private readonly IDialogService _dialogService;
    private readonly IReadOnlyDictionary<string, ICommand> _commandActions;

    private IReadOnlyList<SearchCandidate>? _staticCandidates;
    private IReadOnlySet<string> _favoriteIds = new HashSet<string>();
    private string _searchText = string.Empty;
    private int _selectedIndex = -1;

    public CommandPaletteViewModel(
        IGlobalSearchIndexService searchIndexService,
        ISearchRankingService rankingService,
        ISearchHistoryStore historyStore,
        ISearchFavoritesStore favoritesStore,
        IModuleRegistry moduleRegistry,
        INavigationService navigationService,
        IDialogService dialogService,
        IReadOnlyDictionary<string, ICommand> commandActions)
    {
        _searchIndexService = searchIndexService;
        _rankingService = rankingService;
        _historyStore = historyStore;
        _favoritesStore = favoritesStore;
        _moduleRegistry = moduleRegistry;
        _navigationService = navigationService;
        _dialogService = dialogService;
        _commandActions = commandActions;

        Results = new ObservableCollection<SearchResultRowViewModel>();
        RecentSearches = new ObservableCollection<string>();
        FavoriteResults = new ObservableCollection<SearchResultRowViewModel>();

        SelectPreviousCommand = new RelayCommand(_ => MoveSelection(-1), _ => Results.Count > 0);
        SelectNextCommand = new RelayCommand(_ => MoveSelection(1), _ => Results.Count > 0);
        ExecuteSelectedCommand = new AsyncRelayCommand(_ => ExecuteSelectedAsync(), _ => SelectedIndex >= 0 && SelectedIndex < Results.Count);
        ExecuteResultCommand = new AsyncRelayCommand(parameter => ExecuteAsync((SearchResultRowViewModel)parameter!));
        SelectRecentSearchCommand = new RelayCommand(parameter => SearchText = (string)parameter!);
        ClearHistoryCommand = new AsyncRelayCommand(_ => ClearHistoryAsync());
        CloseCommand = new RelayCommand(_ => _dialogService.CloseDialog());
    }

    public ObservableCollection<SearchResultRowViewModel> Results { get; }

    public ObservableCollection<string> RecentSearches { get; }

    public ObservableCollection<SearchResultRowViewModel> FavoriteResults { get; }

    public bool IsSearchActive => SearchText.Trim().Length > 0;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(IsSearchActive));
                _ = RefreshResultsAsync();
            }
        }
    }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set => SetProperty(ref _selectedIndex, value);
    }

    public ICommand SelectPreviousCommand { get; }

    public ICommand SelectNextCommand { get; }

    public ICommand ExecuteSelectedCommand { get; }

    public ICommand ExecuteResultCommand { get; }

    public ICommand SelectRecentSearchCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public ICommand CloseCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _favoriteIds = await _favoritesStore.GetFavoriteIdsAsync(cancellationToken).ConfigureAwait(true);

        var recent = await _historyStore.GetRecentSearchesAsync(cancellationToken).ConfigureAwait(true);
        RecentSearches.Clear();
        foreach (var query in recent)
        {
            RecentSearches.Add(query);
        }

        await RefreshFavoriteResultsAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task<IReadOnlyList<SearchCandidate>> GetAllCandidatesAsync(CancellationToken cancellationToken)
    {
        _staticCandidates ??= [
            .. StaticSearchCatalog.GetPageCandidates(_moduleRegistry.Modules),
            .. StaticSearchCatalog.GetCommandCandidates(),
        ];

        var liveCandidates = await _searchIndexService.GetCandidatesAsync(cancellationToken).ConfigureAwait(true);
        return [.. _staticCandidates, .. liveCandidates];
    }

    private async Task RefreshResultsAsync(CancellationToken cancellationToken = default)
    {
        if (!IsSearchActive)
        {
            Results.Clear();
            SelectedIndex = -1;
            return;
        }

        var candidates = await GetAllCandidatesAsync(cancellationToken).ConfigureAwait(true);
        var ranked = _rankingService.Rank(candidates, SearchText, _favoriteIds);

        Results.Clear();
        foreach (var result in ranked)
        {
            Results.Add(new SearchResultRowViewModel(result, ToggleFavoriteAsync));
        }

        SelectedIndex = Results.Count > 0 ? 0 : -1;
    }

    private async Task RefreshFavoriteResultsAsync(CancellationToken cancellationToken)
    {
        FavoriteResults.Clear();
        if (_favoriteIds.Count == 0)
        {
            return;
        }

        var candidates = await GetAllCandidatesAsync(cancellationToken).ConfigureAwait(true);
        var favoriteCandidates = candidates.Where(c => _favoriteIds.Contains(c.Id)).ToList();
        var ranked = favoriteCandidates
            .Select(c => new SearchResultDto(c.Id, c.Type, c.Title, c.Subtitle, c.ActionKey, 0, true, []))
            .OrderBy(r => r.Title, StringComparer.CurrentCultureIgnoreCase);

        foreach (var result in ranked)
        {
            FavoriteResults.Add(new SearchResultRowViewModel(result, ToggleFavoriteAsync));
        }
    }

    private void MoveSelection(int delta)
    {
        if (Results.Count == 0)
        {
            return;
        }

        SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, Results.Count - 1);
    }

    private async Task ExecuteSelectedAsync()
    {
        if (SelectedIndex < 0 || SelectedIndex >= Results.Count)
        {
            return;
        }

        await ExecuteAsync(Results[SelectedIndex]).ConfigureAwait(true);
    }

    private async Task ExecuteAsync(SearchResultRowViewModel result)
    {
        if (IsSearchActive)
        {
            await _historyStore.RecordSearchAsync(SearchText).ConfigureAwait(true);

            var recent = await _historyStore.GetRecentSearchesAsync().ConfigureAwait(true);
            RecentSearches.Clear();
            foreach (var query in recent)
            {
                RecentSearches.Add(query);
            }
        }

        if (result.ActionKey.StartsWith("page:", StringComparison.Ordinal))
        {
            var moduleId = result.ActionKey["page:".Length..];
            var descriptor = _moduleRegistry.Modules.FirstOrDefault(m => m.Metadata.Id == moduleId);
            if (descriptor is not null)
            {
                _navigationService.NavigateTo(descriptor);
            }
        }
        else if (result.ActionKey.StartsWith("command:", StringComparison.Ordinal))
        {
            var commandId = result.ActionKey["command:".Length..];
            if (_commandActions.TryGetValue(commandId, out var command) && command.CanExecute(null))
            {
                command.Execute(null);
            }
        }

        _dialogService.CloseDialog();
    }

    private async Task ToggleFavoriteAsync(SearchResultRowViewModel row)
    {
        var isNowFavorite = await _favoritesStore.ToggleFavoriteAsync(row.Id).ConfigureAwait(true);
        row.IsFavorite = isNowFavorite;
        _favoriteIds = await _favoritesStore.GetFavoriteIdsAsync().ConfigureAwait(true);
        await RefreshFavoriteResultsAsync(CancellationToken.None).ConfigureAwait(true);
    }

    private async Task ClearHistoryAsync()
    {
        await _historyStore.ClearAsync().ConfigureAwait(true);
        RecentSearches.Clear();
    }
}
