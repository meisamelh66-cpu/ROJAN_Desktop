using System.Collections.ObjectModel;
using System.Windows.Input;
using Rojan.Desktop.Application.Notifications;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.Notifications;

namespace Rojan.Desktop.Presentation.ViewModels.Notifications;

/// <summary>
/// Phase 27: Enterprise Notification Center. Owns the panel's entire
/// visible state - grouped/filtered/searched notifications, the badge
/// count, and the Silent Mode toggle - refreshed whenever
/// <see cref="INotificationService.StateChanged"/>/<see cref="INotificationService.NotificationRaised"/>
/// fires, so the panel and badge counter always reflect the latest
/// state without polling. Constructed once by <c>Shell.MainWindowViewModel</c>
/// and exposed as a property (not per-open like
/// <c>ViewModels.Help.HelpDialogViewModel</c>, since the Notification
/// Center has no per-context state to seed - it is always the same
/// app-wide list), the same reasoning that keeps this class independently
/// unit-testable without constructing all of <c>MainWindowViewModel</c>'s
/// other dependencies.
/// </summary>
public sealed class NotificationCenterViewModel : ViewModelBase
{
    private readonly INotificationService _notificationService;
    private readonly INotificationContentResolver _contentResolver;
    private readonly INotificationSearchService _searchService;

    private string _searchText = string.Empty;
    private bool _isShowingUnreadOnly;
    private bool _isSilentModeEnabled;
    private int _unreadCount;
    private NotificationSeverityFilterOption _selectedSeverityFilter;

    public NotificationCenterViewModel(
        INotificationService notificationService,
        INotificationContentResolver contentResolver,
        INotificationSearchService searchService)
    {
        _notificationService = notificationService;
        _contentResolver = contentResolver;
        _searchService = searchService;

        SeverityFilterOptions =
        [
            new NotificationSeverityFilterOption(Strings.Notification_Filter_All, null),
            new NotificationSeverityFilterOption(Strings.GetEnumLabel(nameof(NotificationSeverity.Information)), NotificationSeverity.Information),
            new NotificationSeverityFilterOption(Strings.GetEnumLabel(nameof(NotificationSeverity.Success)), NotificationSeverity.Success),
            new NotificationSeverityFilterOption(Strings.GetEnumLabel(nameof(NotificationSeverity.Warning)), NotificationSeverity.Warning),
            new NotificationSeverityFilterOption(Strings.GetEnumLabel(nameof(NotificationSeverity.Error)), NotificationSeverity.Error),
        ];
        _selectedSeverityFilter = SeverityFilterOptions[0];

        Groups = new ObservableCollection<NotificationGroupViewModel>();

        SelectSeverityFilterCommand = new RelayCommand(parameter =>
        {
            if (parameter is NotificationSeverityFilterOption option)
            {
                SelectedSeverityFilter = option;
            }
        });
        MarkAllReadCommand = new AsyncRelayCommand(_ => MarkAllReadAsync());
        ClearAllCommand = new AsyncRelayCommand(_ => ClearAllAsync());

        _notificationService.StateChanged += OnServiceStateChanged;
        _notificationService.NotificationRaised += OnNotificationRaised;
    }

    public ObservableCollection<NotificationGroupViewModel> Groups { get; }

    public IReadOnlyList<NotificationSeverityFilterOption> SeverityFilterOptions { get; }

    public NotificationSeverityFilterOption SelectedSeverityFilter
    {
        get => _selectedSeverityFilter;
        set
        {
            if (SetProperty(ref _selectedSeverityFilter, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    public bool IsShowingUnreadOnly
    {
        get => _isShowingUnreadOnly;
        set
        {
            if (SetProperty(ref _isShowingUnreadOnly, value))
            {
                _ = RefreshAsync();
            }
        }
    }

    /// <summary>Silent Mode's UI-facing toggle - the setter persists the new value through <see cref="INotificationService.SetSilentModeEnabledAsync"/> (fire-and-forget, same "setter triggers async work" shape <c>Shell.MainWindowViewModel.CurrentBranch</c> already establishes) rather than requiring a separate command.</summary>
    public bool IsSilentModeEnabled
    {
        get => _isSilentModeEnabled;
        set
        {
            if (SetProperty(ref _isSilentModeEnabled, value))
            {
                _ = _notificationService.SetSilentModeEnabledAsync(value);
            }
        }
    }

    /// <summary>The Badge Counter requirement's bound value.</summary>
    public int UnreadCount
    {
        get => _unreadCount;
        private set => SetProperty(ref _unreadCount, value);
    }

    public bool HasUnread => UnreadCount > 0;

    public ICommand SelectSeverityFilterCommand { get; }

    public ICommand MarkAllReadCommand { get; }

    public ICommand ClearAllCommand { get; }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _isSilentModeEnabled = await _notificationService.GetIsSilentModeEnabledAsync(cancellationToken).ConfigureAwait(true);
        OnPropertyChanged(nameof(IsSilentModeEnabled));
        await RefreshAsync(cancellationToken).ConfigureAwait(true);
    }

    private async Task MarkAsReadAsync(string notificationId) =>
        await _notificationService.MarkAsReadAsync(notificationId).ConfigureAwait(true);

    private async Task DismissAsync(string notificationId) =>
        await _notificationService.DismissAsync(notificationId).ConfigureAwait(true);

    private async Task MarkAllReadAsync() =>
        await _notificationService.MarkAllAsReadAsync().ConfigureAwait(true);

    private async Task ClearAllAsync() =>
        await _notificationService.ClearAllAsync().ConfigureAwait(true);

    private async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var notifications = await _notificationService.GetAllAsync(cancellationToken).ConfigureAwait(true);
        UnreadCount = notifications.Count(n => !n.IsRead);
        OnPropertyChanged(nameof(HasUnread));

        var filtered = notifications
            .Where(n => SelectedSeverityFilter.Value is null || n.Severity == SelectedSeverityFilter.Value)
            .Where(n => !IsShowingUnreadOnly || !n.IsRead)
            .ToList();

        var resolved = filtered.Select(_contentResolver.Resolve).ToList();

        var trimmedQuery = SearchText.Trim();
        var now = DateTimeOffset.UtcNow;

        Dictionary<string, NotificationSearchResultDto> searchResultsById;
        List<ResolvedNotification> displayOrder;
        if (trimmedQuery.Length == 0)
        {
            searchResultsById = [];
            displayOrder = resolved;
        }
        else
        {
            var candidates = resolved.Select(r => new NotificationSearchCandidate(r.Id, r.Title, r.Message)).ToList();
            var results = _searchService.Search(candidates, trimmedQuery)
                .Where(r => r.Score > 0)
                .ToList();
            searchResultsById = results.ToDictionary(r => r.NotificationId);
            var resolvedById = resolved.ToDictionary(r => r.Id);
            displayOrder = results
                .Where(r => resolvedById.ContainsKey(r.NotificationId))
                .Select(r => resolvedById[r.NotificationId])
                .ToList();
        }

        var rows = displayOrder.Select(r =>
        {
            searchResultsById.TryGetValue(r.Id, out var searchResult);
            return new NotificationRowViewModel(
                r,
                now,
                searchResult?.TitleHighlights,
                searchResult?.MessageHighlights,
                MarkAsReadAsync,
                DismissAsync);
        }).ToList();

        Groups.Clear();
        foreach (var group in rows.GroupBy(r => r.Notification.GroupLabel))
        {
            Groups.Add(new NotificationGroupViewModel(group.Key, group.ToList()));
        }
    }

    private void OnServiceStateChanged(object? sender, EventArgs e) => _ = RefreshAsync();

    private void OnNotificationRaised(object? sender, NotificationDto e) => _ = RefreshAsync();
}
