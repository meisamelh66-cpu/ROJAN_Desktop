using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class TokenUsageTracker : ITokenUsageTracker
{
    private readonly DomainAI.IAIRepository _repository;

    public TokenUsageTracker(DomainAI.IAIRepository repository)
    {
        _repository = repository;
    }

    public async Task<TokenUsageRecordDto> RecordAsync(string sessionId, AIProviderType providerType, int promptTokens, int completionTokens, CancellationToken cancellationToken = default)
    {
        var record = new DomainAI.TokenUsageRecord($"usage-{Guid.NewGuid():N}", sessionId, AIMapper.MapProviderType(providerType), promptTokens, completionTokens, DateTimeOffset.Now);
        var recorded = await _repository.RecordTokenUsageAsync(record, cancellationToken).ConfigureAwait(false);
        return AIMapper.MapTokenUsage(recorded);
    }

    public async Task<IReadOnlyList<TokenUsageRecordDto>> GetUsageHistoryAsync(CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetTokenUsageAsync(cancellationToken).ConfigureAwait(false);
        return records.Select(AIMapper.MapTokenUsage).ToList();
    }

    public async Task<int> GetTotalTokensAsync(CancellationToken cancellationToken = default)
    {
        var records = await _repository.GetTokenUsageAsync(cancellationToken).ConfigureAwait(false);
        return records.Sum(r => r.TotalTokens);
    }
}
