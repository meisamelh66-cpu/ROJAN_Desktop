using Microsoft.Extensions.Logging;
using Rojan.Desktop.Application.AI;
using Rojan.Desktop.Presentation.Localization;
using Rojan.Desktop.Presentation.Tests.Settings;
using Rojan.Desktop.Presentation.Tests.Specialists;
using Rojan.Desktop.Presentation.ViewModels.AI;
using Rojan.Desktop.Presentation.ViewModels.Dashboard;

namespace Rojan.Desktop.Presentation.Tests.AI;

public sealed class AiCenterPageViewModelTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Now;

    private static readonly LanguageInfo EnUs = new("en-US", "English", "English", false, "Segoe UI", NumberDigits.Latin, "Usd", "Gregorian", "1.0.0", "1.0", true);

    private static BusinessHealthScoreDto DefaultHealthScore() => new(75m, [new BusinessHealthComponentDto(InsightCategory.Revenue, "Revenue", 75m, 1m)], "Solid.", Now);

    private static BusinessSummaryDto DefaultSummary() => new("Daily Summary", "Business snapshot.", ["Highlight one"], Now);

    private static (AiCenterPageViewModel Sut, StubAIService AiService, StubAIRepository Repository) CreateSut(
        IReadOnlyList<SmartNotificationDto>? notifications = null,
        IReadOnlyList<AIInsightDto>? insights = null,
        IReadOnlyList<AIRecommendationDto>? recommendations = null,
        IReadOnlyList<SuggestedTaskDto>? suggestedTasks = null,
        RecordingLogger<AiCenterPageViewModel>? logger = null)
    {
        var repository = new StubAIRepository();
        var conversationManager = new ConversationManager(repository);
        var aiService = new StubAIService(conversationManager);

        var sut = new AiCenterPageViewModel(
            aiService,
            new StubBusinessHealthService(DefaultHealthScore()),
            new StubSummaryEngine(DefaultSummary()),
            new StubNotificationInsightService(notifications ?? []),
            new StubInsightEngine(insights ?? []),
            new StubRecommendationEngine(recommendations ?? [], suggestedTasks ?? []),
            conversationManager,
            new AIHistoryService(conversationManager),
            new PromptTemplateRepository(repository),
            new AIConfigurationService(repository),
            new AISettingsService(repository),
            new TokenUsageTracker(repository),
            new StubLocalizationService([EnUs], EnUs),
            logger);

        return (sut, aiService, repository);
    }

    [Fact]
    public void Constructor_LoadsHomeDataAndReachesLoadedState()
    {
        var (sut, _, _) = CreateSut();

        Assert.Equal(DashboardState.Loaded, sut.State);
        Assert.NotNull(sut.HealthScore);
        Assert.Equal(75m, sut.HealthScore!.OverallScore);
        Assert.NotNull(sut.DailySummary);
    }

    [Fact]
    public void Constructor_CreatesAndActivatesANewConversationSessionWhenNoneExist()
    {
        var (sut, _, _) = CreateSut();

        Assert.NotNull(sut.CurrentSessionId);
        Assert.Equal("New conversation", sut.CurrentSessionTitle);
        Assert.Empty(sut.Messages);
        Assert.Single(sut.RecentSessions);
    }

    [Fact]
    public void Constructor_LoadsFeatureTogglesFromSettingsService()
    {
        var (sut, _, _) = CreateSut();

        Assert.True(sut.InsightsEnabled);
        Assert.True(sut.SmartNotificationsEnabled);
    }

    [Fact]
    public void SendMessageCommand_WithBlankInput_IsDisabled()
    {
        var (sut, _, _) = CreateSut();
        sut.ChatInputText = "   ";

        Assert.False(sut.SendMessageCommand.CanExecute(null));
    }

    [Fact]
    public void SendMessageCommand_AppendsUserAndAssistantMessagesAndTracksUsage()
    {
        var (sut, aiService, _) = CreateSut();
        sut.ChatInputText = "How is revenue trending?";

        sut.SendMessageCommand.Execute(null);

        Assert.Equal(1, aiService.SendMessageCallCount);
        Assert.Equal("How is revenue trending?", aiService.LastUserMessage);
        Assert.Equal(2, sut.Messages.Count);
        Assert.Equal(string.Empty, sut.ChatInputText);
        Assert.Equal(8, sut.TotalTokens);
        Assert.Single(sut.UsageHistory);
    }

    // Phase 8.23 Logging Wave 2B: the chat SendMessageAsync boundary now logs at Error,
    // operation name only - the user's chat text is never included in the log line.

    [Fact]
    public void SendMessageCommand_ServiceThrows_LogsErrorWithoutLeakingChatText()
    {
        var logger = new RecordingLogger<AiCenterPageViewModel>();
        var (sut, aiService, _) = CreateSut(logger: logger);
        aiService.ResultFactory = (_, _, _) => throw new InvalidOperationException("upstream failed for customer Sarah Johnson");
        sut.ChatInputText = "Is customer Sarah Johnson overdue?";

        sut.SendMessageCommand.Execute(null);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Contains("SendMessageAsync", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("Sarah Johnson", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("overdue", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void NoLoggerSupplied_UsesNullLogger_ChatFailureNeverThrows()
    {
        var (sut, aiService, _) = CreateSut();
        aiService.ResultFactory = (_, _, _) => throw new InvalidOperationException("boom");
        sut.ChatInputText = "Hello";

        var exception = Record.Exception(() => sut.SendMessageCommand.Execute(null));

        Assert.Null(exception);
    }

    [Fact]
    public void NewConversationCommand_CreatesASecondSessionAndSwitchesToChatSection()
    {
        var (sut, _, _) = CreateSut();
        var firstSessionId = sut.CurrentSessionId;
        sut.SelectedSection = AiCenterSection.Home;

        sut.NewConversationCommand.Execute(null);

        Assert.NotEqual(firstSessionId, sut.CurrentSessionId);
        Assert.Empty(sut.Messages);
        Assert.Equal(AiCenterSection.Chat, sut.SelectedSection);
        Assert.Equal(2, sut.RecentSessions.Count);
    }

    [Fact]
    public void TogglePinCommand_MovesASessionIntoPinnedSessions()
    {
        var (sut, _, _) = CreateSut();
        var session = sut.RecentSessions[0];

        sut.TogglePinCommand.Execute(session);

        Assert.Contains(sut.PinnedSessions, s => s.Id == session.Id);
    }

    [Fact]
    public void DeleteSessionCommand_RemovesSessionAndEnsuresANewActiveSessionExists()
    {
        var (sut, _, _) = CreateSut();
        var session = sut.RecentSessions[0];

        sut.DeleteSessionCommand.Execute(session);

        Assert.NotNull(sut.CurrentSessionId);
        Assert.NotEqual(session.Id, sut.CurrentSessionId);
    }

    [Fact]
    public void SearchHistoryCommand_PopulatesSearchResults()
    {
        var (sut, _, _) = CreateSut();
        sut.SearchText = "New";

        sut.SearchHistoryCommand.Execute(null);

        Assert.Contains(sut.SearchResults, s => s.Id == sut.CurrentSessionId);
    }

    [Fact]
    public void ClearHistoryCommand_DeletesUnpinnedSessionsButKeepsPinnedOnes()
    {
        var (sut, _, _) = CreateSut();
        var pinnedSession = sut.RecentSessions[0];
        sut.TogglePinCommand.Execute(pinnedSession);
        sut.NewConversationCommand.Execute(null);

        sut.ClearHistoryCommand.Execute(null);

        Assert.Contains(sut.RecentSessions, s => s.Id == pinnedSession.Id);
        Assert.Single(sut.RecentSessions);
    }

    [Fact]
    public void ExportSessionCommand_SetsExportPreviewText()
    {
        var (sut, _, _) = CreateSut();
        sut.ChatInputText = "Hi there";
        sut.SendMessageCommand.Execute(null);
        var session = sut.RecentSessions[0];

        sut.ExportSessionCommand.Execute(session);

        Assert.NotNull(sut.ExportPreviewText);
        Assert.Contains("Hi there", sut.ExportPreviewText, StringComparison.Ordinal);
    }

    [Fact]
    public void SaveSettingsCommand_PersistsToggleChanges()
    {
        var (sut, _, _) = CreateSut();
        sut.InsightsEnabled = false;
        sut.SmartNotificationsEnabled = false;

        sut.SaveSettingsCommand.Execute(null);

        Assert.False(sut.InsightsEnabled);
        Assert.Equal("Settings saved.", sut.StatusMessage);
    }

    [Fact]
    public void SaveConfigurationCommand_PersistsProviderAndModelSelection()
    {
        var (sut, _, _) = CreateSut();
        sut.SelectedProviderType = AIProviderType.Anthropic;
        sut.ModelIdInput = "claude-test";
        sut.IsProviderEnabled = false;

        sut.SaveConfigurationCommand.Execute(null);

        Assert.Equal(AIProviderType.Anthropic, sut.Configuration!.ProviderType);
        Assert.Equal("claude-test", sut.Configuration.ModelId);
        Assert.False(sut.Configuration.IsEnabled);
    }
}
