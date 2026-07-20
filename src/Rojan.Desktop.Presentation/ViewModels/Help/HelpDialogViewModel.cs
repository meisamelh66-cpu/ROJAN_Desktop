using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Help;
using Rojan.Desktop.Presentation.Dialogs;
using Rojan.Desktop.Presentation.Help;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;

namespace Rojan.Desktop.Presentation.ViewModels.Help;

/// <summary>
/// Phase 26.4/26.6/26.7: the Context Help Dialog's ViewModel - shown via
/// <see cref="IDialogService.ShowDialog"/> the same way
/// <c>Accounting.PosCheckoutViewModel</c>/<c>Reporting.ExportDialogViewModel</c>
/// already are, constructed directly with <c>new</c> by its opener
/// (<c>Shell.MainWindowViewModel</c>'s <c>OpenHelpCommand</c>) rather than
/// resolved from DI, since it needs a runtime context (which module/page)
/// no constructor-injected dependency can supply. Owns search, back/
/// forward navigation, breadcrumb, related topics, recently viewed, and a
/// favorite toggle - see this phase's own doc comment on each concern's
/// method for why it lives here rather than a lower layer (search
/// scoring is pure Application logic; resolving a topic to display text
/// only Presentation can do; everything else is dialog-session UI state
/// that has no reason to exist anywhere else).
/// </summary>
public sealed class HelpDialogViewModel : ViewModelBase
{
    private readonly IHelpQueryService _helpQueryService;
    private readonly IHelpContentResolver _helpContentResolver;
    private readonly IHelpSearchService _helpSearchService;
    private readonly IHelpFavoritesStore _favoritesStore;
    private readonly IHelpRecentlyViewedStore _recentlyViewedStore;
    private readonly IDialogService _dialogService;

    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();
    private List<HelpSearchCandidate>? _searchCandidates;

    private ResolvedHelpContent? _currentContent;
    private bool _isLoading;
    private string _searchText = string.Empty;
    private bool _isFavorite;
    private bool _isSearchActive;

    public HelpDialogViewModel(
        IHelpQueryService helpQueryService,
        IHelpContentResolver helpContentResolver,
        IHelpSearchService helpSearchService,
        IHelpFavoritesStore favoritesStore,
        IHelpRecentlyViewedStore recentlyViewedStore,
        IDialogService dialogService)
    {
        _helpQueryService = helpQueryService;
        _helpContentResolver = helpContentResolver;
        _helpSearchService = helpSearchService;
        _favoritesStore = favoritesStore;
        _recentlyViewedStore = recentlyViewedStore;
        _dialogService = dialogService;

        SearchResults = new ObservableCollection<HelpSearchResultDto>();
        RecentSearches = new ObservableCollection<string>();
        RelatedTopics = new ObservableCollection<RelatedTopicItem>();
        RecentlyViewed = new ObservableCollection<RelatedTopicItem>();
        Breadcrumbs = new ObservableCollection<string>();

        BackCommand = new AsyncRelayCommand(_ => GoBackAsync(), _ => CanGoBack);
        ForwardCommand = new AsyncRelayCommand(_ => GoForwardAsync(), _ => CanGoForward);
        NavigateToTopicCommand = new AsyncRelayCommand(parameter => NavigateToTopicAsync((string)parameter!));
        ToggleFavoriteCommand = new AsyncRelayCommand(_ => ToggleFavoriteAsync());
        SelectSearchResultCommand = new AsyncRelayCommand(parameter => SelectSearchResultAsync((HelpSearchResultDto)parameter!));
        ClearSearchCommand = new RelayCommand(_ => SearchText = string.Empty);
        CloseCommand = new RelayCommand(_ => _dialogService.CloseDialog());
    }

    public ResolvedHelpContent? CurrentContent
    {
        get => _currentContent;
        private set => SetProperty(ref _currentContent, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        private set => SetProperty(ref _isFavorite, value);
    }

    /// <summary>True while <see cref="SearchText"/> is non-empty - the dialog shows <see cref="SearchResults"/> instead of <see cref="CurrentContent"/> while this is set.</summary>
    public bool IsSearchActive
    {
        get => _isSearchActive;
        private set => SetProperty(ref _isSearchActive, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = ApplySearchAsync(value);
            }
        }
    }

    public ObservableCollection<HelpSearchResultDto> SearchResults { get; }

    /// <summary>Most-recent first, capped small (Phase 26.6's "Recent searches") - the dialog shows these when the search box is focused but empty.</summary>
    public ObservableCollection<string> RecentSearches { get; }

    public ObservableCollection<RelatedTopicItem> RelatedTopics { get; }

    public ObservableCollection<RelatedTopicItem> RecentlyViewed { get; }

    /// <summary>"Help Home › {Current Topic Title}" - Phase 26.7's breadcrumb, rebuilt on every navigation.</summary>
    public ObservableCollection<string> Breadcrumbs { get; }

    public bool CanGoBack => _backStack.Count > 0;

    public bool CanGoForward => _forwardStack.Count > 0;

    public ICommand BackCommand { get; }

    public ICommand ForwardCommand { get; }

    public ICommand NavigateToTopicCommand { get; }

    public ICommand ToggleFavoriteCommand { get; }

    public ICommand SelectSearchResultCommand { get; }

    public ICommand ClearSearchCommand { get; }

    public ICommand CloseCommand { get; }

    /// <summary>Resolves and displays the best-matching topic for the given context - called once, right after construction, by whoever opens this dialog (mirrors every page ViewModel's own constructor-then-LoadAsync shape).</summary>
    public async Task InitializeAsync(string moduleId, string? pageId = null, CancellationToken cancellationToken = default)
    {
        IsLoading = true;
        try
        {
            _backStack.Clear();
            _forwardStack.Clear();
            var topic = await _helpQueryService.GetTopicForContextAsync(moduleId, pageId, cancellationToken).ConfigureAwait(true);
            await ShowTopicAsync(topic, cancellationToken).ConfigureAwait(true);

            var recentIds = await _recentlyViewedStore.GetRecentTopicIdsAsync(cancellationToken).ConfigureAwait(true);
            await PopulateTopicListAsync(RecentlyViewed, recentIds, cancellationToken).ConfigureAwait(true);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task NavigateToTopicAsync(string topicId)
    {
        if (CurrentContent is not null)
        {
            _backStack.Push(CurrentContent.TopicId);
            _forwardStack.Clear();
        }

        SearchText = string.Empty;
        var topic = await _helpQueryService.GetTopicByIdAsync(topicId).ConfigureAwait(true);
        await ShowTopicAsync(topic).ConfigureAwait(true);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private async Task GoBackAsync()
    {
        if (_backStack.Count == 0 || CurrentContent is null)
        {
            return;
        }

        _forwardStack.Push(CurrentContent.TopicId);
        var topic = await _helpQueryService.GetTopicByIdAsync(_backStack.Pop()).ConfigureAwait(true);
        await ShowTopicAsync(topic).ConfigureAwait(true);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private async Task GoForwardAsync()
    {
        if (_forwardStack.Count == 0 || CurrentContent is null)
        {
            return;
        }

        _backStack.Push(CurrentContent.TopicId);
        var topic = await _helpQueryService.GetTopicByIdAsync(_forwardStack.Pop()).ConfigureAwait(true);
        await ShowTopicAsync(topic).ConfigureAwait(true);
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(CanGoForward));
    }

    private async Task ShowTopicAsync(HelpTopicDto? topic, CancellationToken cancellationToken = default)
    {
        if (topic is null)
        {
            return;
        }

        var resolved = _helpContentResolver.Resolve(topic);
        CurrentContent = resolved;
        IsSearchActive = false;

        Breadcrumbs.Clear();
        Breadcrumbs.Add(Strings.Help_Breadcrumb_Home);
        Breadcrumbs.Add(resolved.Title);

        var favorites = await _favoritesStore.GetFavoriteTopicIdsAsync(cancellationToken).ConfigureAwait(true);
        IsFavorite = favorites.Contains(resolved.TopicId);

        await _recentlyViewedStore.RecordViewedAsync(resolved.TopicId, cancellationToken).ConfigureAwait(true);

        await PopulateTopicListAsync(RelatedTopics, resolved.RelatedTopicIds, cancellationToken).ConfigureAwait(true);
    }

    private async Task PopulateTopicListAsync(ObservableCollection<RelatedTopicItem> target, IReadOnlyList<string> topicIds, CancellationToken cancellationToken)
    {
        target.Clear();
        foreach (var topicId in topicIds)
        {
            if (CurrentContent is not null && topicId == CurrentContent.TopicId)
            {
                continue;
            }

            var topic = await _helpQueryService.GetTopicByIdAsync(topicId, cancellationToken).ConfigureAwait(true);
            if (topic is not null)
            {
                target.Add(new RelatedTopicItem(topic.Id, _helpContentResolver.Resolve(topic).Title));
            }
        }
    }

    private async Task ToggleFavoriteAsync()
    {
        if (CurrentContent is null)
        {
            return;
        }

        IsFavorite = await _favoritesStore.ToggleFavoriteAsync(CurrentContent.TopicId).ConfigureAwait(true);
    }

    /// <summary>Builds (and caches) the searchable candidate set from every topic's already-resolved content, then delegates the actual matching/scoring/highlighting to <see cref="IHelpSearchService"/> - see that interface's own doc comment for why the algorithm lives in Application while the text it searches is resolved here.</summary>
    private async Task ApplySearchAsync(string query)
    {
        IsSearchActive = query.Trim().Length > 0;
        if (!IsSearchActive)
        {
            SearchResults.Clear();
            return;
        }

        _searchCandidates ??= await BuildSearchCandidatesAsync().ConfigureAwait(true);

        var results = _helpSearchService.Search(_searchCandidates, query);
        SearchResults.Clear();
        foreach (var result in results)
        {
            SearchResults.Add(result);
        }
    }

    private async Task<List<HelpSearchCandidate>> BuildSearchCandidatesAsync()
    {
        var topics = await _helpQueryService.GetAllTopicsAsync().ConfigureAwait(true);
        return topics
            .Select(_helpContentResolver.Resolve)
            .Select(content => new HelpSearchCandidate(content.TopicId, content.Title, content.Description, content.Overview))
            .ToList();
    }

    private async Task SelectSearchResultAsync(HelpSearchResultDto result)
    {
        if (!RecentSearches.Contains(SearchText, StringComparer.CurrentCultureIgnoreCase) && SearchText.Trim().Length > 0)
        {
            RecentSearches.Insert(0, SearchText.Trim());
            while (RecentSearches.Count > 5)
            {
                RecentSearches.RemoveAt(RecentSearches.Count - 1);
            }
        }

        await NavigateToTopicAsync(result.TopicId).ConfigureAwait(true);
    }
}
