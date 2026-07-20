using System.Runtime.CompilerServices;
using System.Text;
using Providers = Rojan.Desktop.Application.AI.Providers;

namespace Rojan.Desktop.Application.AI;

/// <summary>
/// The AI Module's composition root: wires the Prompt System
/// (<see cref="IPromptBuilder"/>), the active <see cref="Providers.IAIProvider"/>,
/// response cleanup (<see cref="IResponseFormatter"/>), the Conversation
/// System (<see cref="IConversationManager"/>), and usage tracking
/// (<see cref="ITokenUsageTracker"/>) into the single pipeline
/// <see cref="IAIService"/> exposes to Presentation. Every step is async
/// and cancellable end-to-end.
/// </summary>
public sealed class AIOrchestrator : IAIService
{
    private readonly IConversationManager _conversationManager;
    private readonly IPromptBuilder _promptBuilder;
    private readonly Providers.IAIProvider _aiProvider;
    private readonly IResponseFormatter _responseFormatter;
    private readonly ITokenUsageTracker _tokenUsageTracker;
    private readonly IAIConfigurationService _configurationService;

    public AIOrchestrator(
        IConversationManager conversationManager,
        IPromptBuilder promptBuilder,
        Providers.IAIProvider aiProvider,
        IResponseFormatter responseFormatter,
        ITokenUsageTracker tokenUsageTracker,
        IAIConfigurationService configurationService)
    {
        _conversationManager = conversationManager;
        _promptBuilder = promptBuilder;
        _aiProvider = aiProvider;
        _responseFormatter = responseFormatter;
        _tokenUsageTracker = tokenUsageTracker;
        _configurationService = configurationService;
    }

    public async Task<SendMessageResultDto> SendMessageAsync(
        string sessionId,
        string userMessage,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken = default)
    {
        var (userMessageDto, providerRequest, providerType) = await PrepareRequestAsync(sessionId, userMessage, languageContext, cancellationToken).ConfigureAwait(false);

        var response = await _aiProvider.CompleteAsync(providerRequest, cancellationToken).ConfigureAwait(false);
        var formatted = _responseFormatter.Format(response.Content);

        var assistantMessageDto = await _conversationManager
            .AppendMessageAsync(sessionId, ConversationRole.Assistant, formatted, response.CompletionTokens, cancellationToken)
            .ConfigureAwait(false);

        var tokenUsageDto = await _tokenUsageTracker
            .RecordAsync(sessionId, providerType, response.PromptTokens, response.CompletionTokens, cancellationToken)
            .ConfigureAwait(false);

        return new SendMessageResultDto(userMessageDto, assistantMessageDto, tokenUsageDto);
    }

    public async IAsyncEnumerable<string> StreamMessageAsync(
        string sessionId,
        string userMessage,
        LanguageContextDto languageContext,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var (_, providerRequest, providerType) = await PrepareRequestAsync(sessionId, userMessage, languageContext, cancellationToken).ConfigureAwait(false);

        var builder = new StringBuilder();
        await foreach (var chunk in _aiProvider.StreamCompleteAsync(providerRequest, cancellationToken).ConfigureAwait(false))
        {
            builder.Append(chunk);
            yield return chunk;
        }

        var formatted = _responseFormatter.Format(builder.ToString());
        var promptTokens = EstimateTokens(providerRequest.Messages.Sum(m => m.Content.Length));
        var completionTokens = EstimateTokens(formatted.Length);

        await _conversationManager
            .AppendMessageAsync(sessionId, ConversationRole.Assistant, formatted, completionTokens, cancellationToken)
            .ConfigureAwait(false);

        await _tokenUsageTracker
            .RecordAsync(sessionId, providerType, promptTokens, completionTokens, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<(ConversationMessageDto UserMessage, Providers.AIProviderRequestDto ProviderRequest, AIProviderType ProviderType)> PrepareRequestAsync(
        string sessionId,
        string userMessage,
        LanguageContextDto languageContext,
        CancellationToken cancellationToken)
    {
        var existingMessages = await _conversationManager.GetMessagesAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var userMessageDto = await _conversationManager
            .AppendMessageAsync(sessionId, ConversationRole.User, userMessage, EstimateTokens(userMessage.Length), cancellationToken)
            .ConfigureAwait(false);

        var promptContext = await _promptBuilder
            .BuildAsync(userMessage, existingMessages.Count + 1, languageContext, cancellationToken)
            .ConfigureAwait(false);

        var configuration = await _configurationService.GetConfigurationAsync(cancellationToken).ConfigureAwait(false);

        var providerMessages = new List<Providers.AIProviderMessageDto>
        {
            new(ConversationRole.System, string.Join(
                Environment.NewLine,
                promptContext.SystemPrompt,
                promptContext.BusinessContext,
                promptContext.AnalyticsContext,
                promptContext.LanguageContext)),
            new(ConversationRole.Developer, string.Join(Environment.NewLine, promptContext.DeveloperPrompt, promptContext.SessionContext)),
            new(ConversationRole.User, promptContext.UserPrompt),
        };

        var providerRequest = new Providers.AIProviderRequestDto(sessionId, providerMessages, configuration.ModelId);
        return (userMessageDto, providerRequest, configuration.ProviderType);
    }

    /// <summary>Rough token estimate (~4 characters per token), matching <c>MockAIProvider</c>'s own estimate so usage figures stay consistent regardless of which side of the pipeline computed them.</summary>
    private static int EstimateTokens(int characterCount) => Math.Max(1, characterCount / 4);
}
