namespace Rojan.Desktop.Domain.Salons;

/// <summary>
/// Repository abstraction for salon data. Domain defines the contract;
/// Infrastructure provides the concrete implementation. Deliberately no
/// <c>salonId</c>-scoped reads/writes here (unlike every other Domain
/// repository in this app) - ROJAN_Backend's own <c>/salons</c>/<c>/salons/mine</c>
/// endpoints resolve "which salon" from the caller's own bearer token, not
/// a path parameter, so there is nothing to scope by yet - this repository
/// *is* how a caller finds out which salon(s), if any, they own.
/// </summary>
public interface ISalonRepository
{
    /// <summary>Every salon the signed-in owner manages - empty if none. ROJAN_Backend supports more than one; this app has no salon-switcher UI yet (a known, already-documented Phase 1 limitation - see <c>Salons.ISalonContextService</c>'s own doc comment), so callers here typically only look at the first entry.</summary>
    public Task<IReadOnlyList<Salon>> GetMineAsync(CancellationToken cancellationToken = default);

    public Task<Salon> CreateAsync(Salon salon, CancellationToken cancellationToken = default);

    /// <summary>
    /// QR Ecosystem (Desktop Productionization Sprint 1): a scannable PNG
    /// encoding this salon's real public booking link
    /// (<c>{ROJAN_PUBLIC_BASE_URL}/s/{salon.slug}</c> - ROJAN_Backend's own
    /// <c>GenerateSalonQrCodeUseCase</c> builds the URL, this repository
    /// never needs to know the slug or construct it client-side). Unlike
    /// every other method here, this one *is* salon-scoped by id - the
    /// caller already knows which salon (from <see cref="GetMineAsync"/>
    /// or <c>Application.Salons.ISalonContextService</c>), there is
    /// nothing to discover.
    /// </summary>
    public Task<byte[]> GetQrCodeAsync(string salonId, int sizePx, CancellationToken cancellationToken = default);
}
