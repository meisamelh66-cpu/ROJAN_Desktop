namespace Rojan.Desktop.Application.AI;

/// <summary>The Usage Dashboard's write+read surface - records every exchange's token cost and aggregates it back out.</summary>
public interface ITokenUsageTracker
{
    public Task<TokenUsageRecordDto> RecordAsync(string sessionId, AIProviderType providerType, int promptTokens, int completionTokens, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<TokenUsageRecordDto>> GetUsageHistoryAsync(CancellationToken cancellationToken = default);

    public Task<int> GetTotalTokensAsync(CancellationToken cancellationToken = default);
}
