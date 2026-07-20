using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class AISettingsService : IAISettingsService
{
    private readonly DomainAI.IAIRepository _repository;

    public AISettingsService(DomainAI.IAIRepository repository)
    {
        _repository = repository;
    }

    public async Task<AISettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _repository.GetSettingsAsync(cancellationToken).ConfigureAwait(false);
        return AIMapper.MapSettings(settings);
    }

    public async Task<AISettingsDto> UpdateSettingsAsync(AISettingsDto settings, CancellationToken cancellationToken = default)
    {
        var saved = await _repository.SetSettingsAsync(AIMapper.MapSettings(settings), cancellationToken).ConfigureAwait(false);
        return AIMapper.MapSettings(saved);
    }
}
