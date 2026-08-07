using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Salons;

/// <summary>Default <see cref="ISalonQueryService"/> implementation.</summary>
public sealed class SalonQueryService(DomainSalons.ISalonRepository repository) : ISalonQueryService
{
    public async Task<SalonDto?> GetMySalonAsync(CancellationToken cancellationToken = default)
    {
        var salons = await repository.GetMineAsync(cancellationToken).ConfigureAwait(true);
        return salons.Count > 0 ? SalonMapper.MapSalon(salons[0]) : null;
    }
}
