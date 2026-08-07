namespace Rojan.Desktop.Application.Salons;

/// <summary>Read-only use case Presentation depends on to load salon data - the only way Presentation ever reaches it, never through Domain/Infrastructure directly.</summary>
public interface ISalonQueryService
{
    /// <summary>The signed-in owner's salon, or <see langword="null"/> if they own none yet. If the owner manages more than one, the first one the backend returns is used - same "no salon-switcher UI yet" limitation <see cref="ISalonContextService"/> already documents.</summary>
    public Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default);
}
