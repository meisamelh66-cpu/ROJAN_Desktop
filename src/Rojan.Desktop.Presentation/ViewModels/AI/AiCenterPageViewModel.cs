using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Mvvm;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.ViewModels.AI;

/// <summary>
/// Drives the AI Center page - the only ViewModel in the app that composes
/// (nearly) every other AI Application service: <see cref="IAIService"/>
/// (Chat/Business Assistant), <see cref="IBusinessHealthService"/>/
/// <see cref="ISummaryEngine"/>/<see cref="INotificationInsightService"/>
/// (Home), <see cref="IInsightEngine"/> (Insight Dashboard - Trend/Risk/
/// Opportunity detection across every business area),
/// <see cref="IRecommendationEngine"/> (Recommendations Panel/Action
/// Center), <see cref="IConversationManager"/>/<see cref="IAIHistoryService"/>
/// (History/Conversation Viewer), and <see cref="IAIConfigurationService"/>/
/// <see cref="IAISettingsService"/>/<see cref="ITokenUsageTracker"/>/
/// <see cref="IPromptTemplateRepository"/> (Settings: Model Selector,
/// feature toggles, Usage Dashboard, Prompt Templates). Every load/send
/// path is async; the Chat send path never blocks the UI thread while the
/// (Mock) provider is "thinking."
/// </summary>
public sealed partial class AiCenterPageViewModel : ViewModelBase
{
    private readonly IAIService _aiService;
    private readonly IBusinessHealthService _businessHealthService;
    private readonly ISummaryEngine _summaryEngine;
    private readonly INotificationInsightService _notificationInsightService;
    private readonly IInsightEngine _insightEngine;
    private readonly IRecommendationEngine _recommendationEngine;
    private readonly IConversationManager _conversationManager;
    private readonly IAIHistoryService _aiHistoryService;
    private readonly IPromptTemplateRepository _promptTemplateRepository;
    private readonly IAIConfigurationService _configurationService;
    private readonly IAISettingsService _settingsService;
    private readonly ITokenUsageTracker _tokenUsageTracker;
    private readonly ILocalizationService _localizationService;
    private readonly ILogger<AiCenterPageViewModel> _logger;

    private DashboardState _state = DashboardState.Loading;
    private string? _errorMessage;
    private string? _actionErrorMessage;
    private bool _hasActionError;
    private AiCenterSection _selectedSection = AiCenterSection.Home;
    private BusinessHealthScoreDto? _healthScore;
    private BusinessSummaryDto? _dailySummary;
    private string? _currentSessionId;
    private string _currentSessionTitle = "New conversation";
    private string _chatInputText = string.Empty;
    private bool _isSending;
    private string _searchText = string.Empty;
    private string? _exportPreviewText;
    private AIProviderConfigurationDto? _configuration;
    private AIProviderType _selectedProviderType;
    private string _modelIdInput = string.Empty;
    private bool _isProviderEnabled;
    private bool _insightsEnabled;
    private bool _smartNotificationsEnabled;
    private bool _dailySummaryEnabled;
    private bool _autoGenerateRecommendations;
    private int _totalTokens;
    private string _statusMessage = string.Empty;

    public AiCenterPageViewModel(
        IAIService aiService,
        IBusinessHealthService businessHealthService,
        ISummaryEngine summaryEngine,
        INotificationInsightService notificationInsightService,
        IInsightEngine insightEngine,
        IRecommendationEngine recommendationEngine,
        IConversationManager conversationManager,
        IAIHistoryService aiHistoryService,
        IPromptTemplateRepository promptTemplateRepository,
        IAIConfigurationService configurationService,
        IAISettingsService settingsService,
        ITokenUsageTracker tokenUsageTracker,
        ILocalizationService localizationService,
        ILogger<AiCenterPageViewModel>? logger = null)
    {
        _aiService = aiService;
        _businessHealthService = businessHealthService;
        _summaryEngine = summaryEngine;
        _notificationInsightService = notificationInsightService;
        _insightEngine = insightEngine;
        _recommendationEngine = recommendationEngine;
        _conversationManager = conversationManager;
        _aiHistoryService = aiHistoryService;
        _promptTemplateRepository = promptTemplateRepository;
        _configurationService = configurationService;
        _settingsService = settingsService;
        _tokenUsageTracker = tokenUsageTracker;
        _localizationService = localizationService;
        _logger = logger ?? NullLogger<AiCenterPageViewModel>.Instance;

        Notifications = new ObservableCollection<SmartNotificationDto>();
        Insights = new ObservableCollection<AIInsightDto>();
        Recommendations = new ObservableCollection<AIRecommendationDto>();
        SuggestedTasks = new ObservableCollection<SuggestedTaskDto>();
        RecentSessions = new ObservableCollection<ConversationSessionDto>();
        PinnedSessions = new ObservableCollection<ConversationSessionDto>();
        SearchResults = new ObservableCollection<ConversationSessionDto>();
        Messages = new ObservableCollection<ConversationMessageDto>();
        Templates = new ObservableCollection<PromptTemplateDto>();
        UsageHistory = new ObservableCollection<TokenUsageRecordDto>();
        AvailableProviderTypes = _configurationService.GetAvailableProviderTypes();

        LoadCommand = new AsyncRelayCommand(_ => LoadAsync());
        SelectSectionCommand = new RelayCommand(section => SelectedSection = (AiCenterSection)section!);
        SendMessageCommand = new AsyncRelayCommand(_ => SendMessageAsync(), _ => !IsSending && !string.IsNullOrWhiteSpace(ChatInputText) && CurrentSessionId is not null);
        NewConversationCommand = new AsyncRelayCommand(_ => NewConversationAsync());
        OpenConversationCommand = new AsyncRelayCommand(session => OpenConversationAsync((ConversationSessionDto)session!));
        TogglePinCommand = new AsyncRelayCommand(session => TogglePinAsync((ConversationSessionDto)session!));
        DeleteSessionCommand = new AsyncRelayCommand(session => DeleteSessionAsync((ConversationSessionDto)session!));
        SearchHistoryCommand = new AsyncRelayCommand(_ => SearchHistoryAsync());
        ClearHistoryCommand = new AsyncRelayCommand(_ => ClearHistoryAsync());
        ExportSessionCommand = new AsyncRelayCommand(session => ExportSessionAsync((ConversationSessionDto)session!));
        SaveSettingsCommand = new AsyncRelayCommand(_ => SaveSettingsAsync());
        SaveConfigurationCommand = new AsyncRelayCommand(_ => SaveConfigurationAsync());

        _ = LoadAsync();
    }

    public ObservableCollection<SmartNotificationDto> Notifications { get; }

    public ObservableCollection<AIInsightDto> Insights { get; }

    public ObservableCollection<AIRecommendationDto> Recommendations { get; }

    public ObservableCollection<SuggestedTaskDto> SuggestedTasks { get; }

    public ObservableCollection<ConversationSessionDto> RecentSessions { get; }

    public ObservableCollection<ConversationSessionDto> PinnedSessions { get; }

    public ObservableCollection<ConversationSessionDto> SearchResults { get; }

    public ObservableCollection<ConversationMessageDto> Messages { get; }

    public ObservableCollection<PromptTemplateDto> Templates { get; }

    public ObservableCollection<TokenUsageRecordDto> UsageHistory { get; }

    public IReadOnlyList<AIProviderType> AvailableProviderTypes { get; }

    public ICommand LoadCommand { get; }

    public ICommand SelectSectionCommand { get; }

    public ICommand SendMessageCommand { get; }

    public ICommand NewConversationCommand { get; }

    public ICommand OpenConversationCommand { get; }

    public ICommand TogglePinCommand { get; }

    public ICommand DeleteSessionCommand { get; }

    public ICommand SearchHistoryCommand { get; }

    public ICommand ClearHistoryCommand { get; }

    public ICommand ExportSessionCommand { get; }

    public ICommand SaveSettingsCommand { get; }

    public ICommand SaveConfigurationCommand { get; }

    public DashboardState State
    {
        get => _state;
        private set => SetProperty(ref _state, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Non-destructive inline error shown when a user-triggered AI Center action
    /// (new/open/pin/delete conversation, search/clear history, export, save
    /// settings/config) fails - unlike <see cref="ErrorMessage"/>/<see cref="State"/>
    /// it never blanks the page, and unlike <see cref="StatusMessage"/> it does
    /// not clobber the last chat / save status. Production Hardening missing-guard
    /// sweep (Wave E), same shape as HR.HrPageViewModel.ActionErrorMessage.
    /// </summary>
    public string? ActionErrorMessage
    {
        get => _actionErrorMessage;
        private set => SetProperty(ref _actionErrorMessage, value);
    }

    public bool HasActionError
    {
        get => _hasActionError;
        private set => SetProperty(ref _hasActionError, value);
    }

    public AiCenterSection SelectedSection
    {
        get => _selectedSection;
        set => SetProperty(ref _selectedSection, value);
    }

    public BusinessHealthScoreDto? HealthScore
    {
        get => _healthScore;
        private set => SetProperty(ref _healthScore, value);
    }

    public BusinessSummaryDto? DailySummary
    {
        get => _dailySummary;
        private set => SetProperty(ref _dailySummary, value);
    }

    public string? CurrentSessionId
    {
        get => _currentSessionId;
        private set => SetProperty(ref _currentSessionId, value);
    }

    public string CurrentSessionTitle
    {
        get => _currentSessionTitle;
        private set => SetProperty(ref _currentSessionTitle, value);
    }

    public string ChatInputText
    {
        get => _chatInputText;
        set => SetProperty(ref _chatInputText, value);
    }

    public bool IsSending
    {
        get => _isSending;
        private set => SetProperty(ref _isSending, value);
    }

    public string SearchText
    {
        get => _searchText;
        set => SetProperty(ref _searchText, value);
    }

    public string? ExportPreviewText
    {
        get => _exportPreviewText;
        private set => SetProperty(ref _exportPreviewText, value);
    }

    public AIProviderConfigurationDto? Configuration
    {
        get => _configuration;
        private set => SetProperty(ref _configuration, value);
    }

    public AIProviderType SelectedProviderType
    {
        get => _selectedProviderType;
        set => SetProperty(ref _selectedProviderType, value);
    }

    public string ModelIdInput
    {
        get => _modelIdInput;
        set => SetProperty(ref _modelIdInput, value);
    }

    public bool IsProviderEnabled
    {
        get => _isProviderEnabled;
        set => SetProperty(ref _isProviderEnabled, value);
    }

    public bool InsightsEnabled
    {
        get => _insightsEnabled;
        set => SetProperty(ref _insightsEnabled, value);
    }

    public bool SmartNotificationsEnabled
    {
        get => _smartNotificationsEnabled;
        set => SetProperty(ref _smartNotificationsEnabled, value);
    }

    public bool DailySummaryEnabled
    {
        get => _dailySummaryEnabled;
        set => SetProperty(ref _dailySummaryEnabled, value);
    }

    public bool AutoGenerateRecommendations
    {
        get => _autoGenerateRecommendations;
        set => SetProperty(ref _autoGenerateRecommendations, value);
    }

    public int TotalTokens
    {
        get => _totalTokens;
        private set => SetProperty(ref _totalTokens, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    private async Task LoadAsync()
    {
        State = DashboardState.Loading;
        ErrorMessage = null;

        try
        {
            HealthScore = await _businessHealthService.ComputeScoreAsync().ConfigureAwait(true);
            DailySummary = await _summaryEngine.GetDailySummaryAsync().ConfigureAwait(true);
            ReplaceCollection(Notifications, await _notificationInsightService.GetSmartNotificationsAsync().ConfigureAwait(true));
            ReplaceCollection(Insights, await _insightEngine.GenerateInsightsAsync().ConfigureAwait(true));
            ReplaceCollection(Recommendations, await _recommendationEngine.GenerateRecommendationsAsync().ConfigureAwait(true));
            ReplaceCollection(SuggestedTasks, await _recommendationEngine.GenerateSuggestedTasksAsync().ConfigureAwait(true));
            ReplaceCollection(Templates, await _promptTemplateRepository.GetTemplatesAsync().ConfigureAwait(true));

            await ReloadSessionsAsync().ConfigureAwait(true);
            await EnsureActiveSessionAsync().ConfigureAwait(true);

            Configuration = await _configurationService.GetConfigurationAsync().ConfigureAwait(true);
            SelectedProviderType = Configuration.ProviderType;
            ModelIdInput = Configuration.ModelId;
            IsProviderEnabled = Configuration.IsEnabled;

            ApplySettings(await _settingsService.GetSettingsAsync().ConfigureAwait(true));

            ReplaceCollection(UsageHistory, await _tokenUsageTracker.GetUsageHistoryAsync().ConfigureAwait(true));
            TotalTokens = await _tokenUsageTracker.GetTotalTokensAsync().ConfigureAwait(true);

            State = DashboardState.Loaded;
        }
#pragma warning disable CA1031 // Top-level load boundary: any failure must surface as the Error state, not crash the page - same justified broad catch as every other page ViewModel in this app.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
            LogOperationFailed(nameof(LoadAsync));
        }
    }

    // Security: logs the operation name only - never the exception, its message,
    // the user's chat text, AI responses, or any backend/token detail.
    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "AI Center page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);

    private async Task ReloadSessionsAsync()
    {
        ReplaceCollection(RecentSessions, await _aiHistoryService.GetRecentConversationsAsync().ConfigureAwait(true));
        ReplaceCollection(PinnedSessions, await _aiHistoryService.GetPinnedConversationsAsync().ConfigureAwait(true));
    }

    private async Task EnsureActiveSessionAsync()
    {
        if (CurrentSessionId is null)
        {
            if (RecentSessions.Count > 0)
            {
                CurrentSessionId = RecentSessions[0].Id;
                CurrentSessionTitle = RecentSessions[0].Title;
            }
            else
            {
                var created = await _conversationManager.CreateSessionAsync("New conversation").ConfigureAwait(true);
                CurrentSessionId = created.Id;
                CurrentSessionTitle = created.Title;
                await ReloadSessionsAsync().ConfigureAwait(true);
            }
        }

        await LoadMessagesAsync().ConfigureAwait(true);
    }

    private async Task LoadMessagesAsync()
    {
        if (CurrentSessionId is null)
        {
            return;
        }

        ReplaceCollection(Messages, await _conversationManager.GetMessagesAsync(CurrentSessionId).ConfigureAwait(true));
    }

    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(ChatInputText) || CurrentSessionId is null)
        {
            return;
        }

        var text = ChatInputText;
        ChatInputText = string.Empty;
        IsSending = true;
        try
        {
            var language = _localizationService.CurrentLanguage;
            var result = await _aiService
                .SendMessageAsync(CurrentSessionId, text, new LanguageContextDto(language.Code, language.NativeName, language.IsRightToLeft))
                .ConfigureAwait(true);

            Messages.Add(result.UserMessage);
            Messages.Add(result.AssistantMessage);
            UsageHistory.Add(result.TokenUsage);
            TotalTokens += result.TokenUsage.TotalTokens;
            await ReloadSessionsAsync().ConfigureAwait(true);
        }
#pragma warning disable CA1031 // The Chat Window must surface any failure as a status message, not crash the page.
        catch (Exception exception)
#pragma warning restore CA1031
        {
            StatusMessage = exception.Message;
            LogOperationFailed(nameof(SendMessageAsync));
        }
        finally
        {
            IsSending = false;
        }
    }

    private async Task NewConversationAsync()
    {
        try
        {
            var created = await _conversationManager.CreateSessionAsync("New conversation").ConfigureAwait(true);
            CurrentSessionId = created.Id;
            CurrentSessionTitle = created.Title;
            Messages.Clear();
            await ReloadSessionsAsync().ConfigureAwait(true);
            SelectedSection = AiCenterSection.Chat;
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(NewConversationAsync));
        }
    }

    private async Task OpenConversationAsync(ConversationSessionDto session)
    {
        try
        {
            CurrentSessionId = session.Id;
            CurrentSessionTitle = session.Title;
            await LoadMessagesAsync().ConfigureAwait(true);
            SelectedSection = AiCenterSection.Chat;
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(OpenConversationAsync));
        }
    }

    private async Task TogglePinAsync(ConversationSessionDto session)
    {
        try
        {
            await _conversationManager.TogglePinAsync(session.Id).ConfigureAwait(true);
            await ReloadSessionsAsync().ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(TogglePinAsync));
        }
    }

    private async Task DeleteSessionAsync(ConversationSessionDto session)
    {
        try
        {
            await _conversationManager.DeleteSessionAsync(session.Id).ConfigureAwait(true);
            if (CurrentSessionId == session.Id)
            {
                CurrentSessionId = null;
                Messages.Clear();
            }

            await ReloadSessionsAsync().ConfigureAwait(true);
            if (CurrentSessionId is null)
            {
                await EnsureActiveSessionAsync().ConfigureAwait(true);
            }

            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(DeleteSessionAsync));
        }
    }

    private async Task SearchHistoryAsync()
    {
        try
        {
            ReplaceCollection(SearchResults, await _aiHistoryService.SearchAsync(SearchText).ConfigureAwait(true));
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(SearchHistoryAsync));
        }
    }

    private async Task ClearHistoryAsync()
    {
        try
        {
            await _conversationManager.ClearHistoryAsync().ConfigureAwait(true);
            CurrentSessionId = null;
            Messages.Clear();
            await ReloadSessionsAsync().ConfigureAwait(true);
            await EnsureActiveSessionAsync().ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(ClearHistoryAsync));
        }
    }

    private async Task ExportSessionAsync(ConversationSessionDto session)
    {
        try
        {
            ExportPreviewText = await _conversationManager.ExportSessionAsync(session.Id).ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
        }
#pragma warning disable CA1031 // Command boundary: a failed export must surface inline, not via the global dialog - and the catch binds no exception variable, so no partial transcript / generated content can leak.
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(ExportSessionAsync));
        }
    }

    private async Task SaveSettingsAsync()
    {
        var updated = new AISettingsDto(InsightsEnabled, SmartNotificationsEnabled, DailySummaryEnabled, AutoGenerateRecommendations);
        try
        {
            ApplySettings(await _settingsService.UpdateSettingsAsync(updated).ConfigureAwait(true));
            ActionErrorMessage = null;
            HasActionError = false;
            StatusMessage = "Settings saved.";
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(SaveSettingsAsync));
        }
    }

    private async Task SaveConfigurationAsync()
    {
        try
        {
            Configuration = await _configurationService
                .SetConfigurationAsync(SelectedProviderType, ModelIdInput, IsProviderEnabled)
                .ConfigureAwait(true);
            ActionErrorMessage = null;
            HasActionError = false;
            StatusMessage = "Model configuration saved.";
        }
#pragma warning disable CA1031 // Command boundary: a failed AI Center action must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
        catch (Exception)
#pragma warning restore CA1031
        {
            ActionErrorMessage = Strings.Common_ActionFailedMessage;
            HasActionError = true;
            LogOperationFailed(nameof(SaveConfigurationAsync));
        }
    }

    private void ApplySettings(AISettingsDto settings)
    {
        InsightsEnabled = settings.InsightsEnabled;
        SmartNotificationsEnabled = settings.SmartNotificationsEnabled;
        DailySummaryEnabled = settings.DailySummaryEnabled;
        AutoGenerateRecommendations = settings.AutoGenerateRecommendations;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> collection, IEnumerable<T> items)
    {
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }
}
