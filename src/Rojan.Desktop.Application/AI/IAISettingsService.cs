namespace Rojan.Desktop.Application.AI;

/// <summary>The Settings screen's feature-toggle surface (distinct from <see cref="IAIConfigurationService"/>'s model selection).</summary>
public interface IAISettingsService
{
    public Task<AISettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default);

    public Task<AISettingsDto> UpdateSettingsAsync(AISettingsDto settings, CancellationToken cancellationToken = default);
}
