using System.Runtime.CompilerServices;
using System.Text;

namespace Rojan.Desktop.Application.AI.Providers;

/// <summary>
/// The only <see cref="IAIProvider"/> this phase ships a real
/// implementation for - deterministic, template-based replies derived
/// from the request's own content (never a network call, never an API
/// key). Genuinely demonstrates <see cref="StreamCompleteAsync"/> by
/// yielding the same reply word-by-word with a small artificial delay,
/// the same "Loading states must be observable" reasoning every
/// <c>FakeXxxRepository</c> in this app already uses.
/// </summary>
public sealed class MockAIProvider : IAIProvider
{
    public AIProviderType ProviderType => AIProviderType.Mock;

    public Task<AIProviderResponseDto> CompleteAsync(AIProviderRequestDto request, CancellationToken cancellationToken = default)
    {
        var reply = BuildReply(request);
        var promptTokens = EstimateTokens(request.Messages.Sum(m => m.Content.Length));
        var completionTokens = EstimateTokens(reply.Length);
        return Task.FromResult(new AIProviderResponseDto(reply, promptTokens, completionTokens));
    }

    public async IAsyncEnumerable<string> StreamCompleteAsync(AIProviderRequestDto request, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var reply = BuildReply(request);
        var words = reply.Split(' ');
        for (var i = 0; i < words.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(15, cancellationToken).ConfigureAwait(false);
            yield return i == 0 ? words[i] : " " + words[i];
        }
    }

    private static string BuildReply(AIProviderRequestDto request)
    {
        var userMessage = request.Messages.LastOrDefault(m => m.Role == ConversationRole.User)?.Content ?? string.Empty;
        var businessContext = request.Messages.FirstOrDefault(m => m.Role == ConversationRole.System)?.Content ?? string.Empty;

        var builder = new StringBuilder();
        builder.Append("Based on the current business data");
        if (!string.IsNullOrWhiteSpace(businessContext))
        {
            builder.Append(", here is what stands out");
        }

        builder.Append(": ").Append(SummarizeContext(businessContext));
        builder.Append(" Regarding \"").Append(Truncate(userMessage, 80)).Append("\" - this is a mock response; connect a real provider under Settings to enable live answers.");
        return builder.ToString();
    }

    private static string SummarizeContext(string businessContext)
    {
        if (string.IsNullOrWhiteSpace(businessContext))
        {
            return "no additional business context was supplied for this request.";
        }

        var firstLine = businessContext.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(firstLine) ? "business context was supplied but empty." : firstLine;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : string.Concat(value.AsSpan(0, maxLength), "...");

    /// <summary>Rough token estimate (~4 characters per token) - the Mock provider's stand-in for a real tokenizer, matching order-of-magnitude behavior without a tokenizer dependency.</summary>
    private static int EstimateTokens(int characterCount) => Math.Max(1, characterCount / 4);
}
