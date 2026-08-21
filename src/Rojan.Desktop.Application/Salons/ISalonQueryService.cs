namespace Rojan.Desktop.Application.Salons;

/// <summary>Read-only use case Presentation depends on to load salon data - the only way Presentation ever reaches it, never through Domain/Infrastructure directly.</summary>
public interface ISalonQueryService
{
    /// <summary>The signed-in owner's salon, or <see langword="null"/> if they own none yet. If the owner manages more than one, the first one the backend returns is used - same "no salon-switcher UI yet" limitation <see cref="ISalonContextService"/> already documents.</summary>
    public Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default);

    /// <summary>QR Ecosystem (Desktop Productionization Sprint 1): the "Customer QR" - a scannable PNG encoding the current salon's real public booking link. Resolves the salon id via <see cref="ISalonContextService"/> itself (same "caller doesn't need to already know the salon id" convenience every other salon-scoped query service method in this app gives) rather than taking one as a parameter.</summary>
    public Task<byte[]> GetSalonQrCodeAsync(int sizePx, CancellationToken cancellationToken = default);
}
