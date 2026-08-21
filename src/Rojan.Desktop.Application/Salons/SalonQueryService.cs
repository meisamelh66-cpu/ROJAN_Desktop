using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Salons;

/// <summary>Default <see cref="ISalonQueryService"/> implementation.</summary>
public sealed class SalonQueryService(DomainSalons.ISalonRepository repository, ISalonContextService contextService) : ISalonQueryService
{
    public async Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default)
    {
        var salons = await repository.GetMineAsync(cancellationToken).ConfigureAwait(true);
        return salons.Count > 0 ? SalonMapper.MapSalon(salons[0]) : null;
    }

    public async Task<byte[]> GetSalonQrCodeAsync(int sizePx, CancellationToken cancellationToken = default)
    {
        var salonId = await contextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(true);
        if (salonId is null)
        {
            throw new InvalidOperationException("The signed-in owner does not manage any salon yet.");
        }

        return await repository.GetQrCodeAsync(salonId, sizePx, cancellationToken).ConfigureAwait(true);
    }
}
