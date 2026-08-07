using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Salons;

/// <summary>
/// Default <see cref="ISalonCommandService"/> implementation. Not
/// permission-gated, unlike every other <c>*CommandService</c> in this app
/// (e.g. <c>Customers.CustomerCommandServicePermissionGate</c>) - a
/// deliberate difference, not an oversight: this app's permission system
/// (<see cref="Rojan.Desktop.Application.Organizations.Permission"/>) is
/// keyed off the local, still-fake Organization/Branch/<c>WorkspaceRole</c>
/// model, which a brand-new, salon-less owner may not have a meaningful
/// role within yet - salon ownership on ROJAN_Backend is a property of the
/// signed-in account itself, resolved independently of that local role
/// system (see <see cref="ISalonContextService"/>'s own doc comment for
/// the same reasoning already applied to salon *reads*). Gating salon
/// *creation* behind the local role system would risk locking a real
/// owner out of the one action that lets every other already-gated
/// feature start working at all.
/// </summary>
public sealed class SalonCommandService(
    DomainSalons.ISalonRepository repository,
    ISalonContextService salonContextService) : ISalonCommandService
{
    public async Task<SalonDto> CreateSalonAsync(CreateSalonCommand command, CancellationToken cancellationToken = default)
    {
        var salon = new DomainSalons.Salon(
            Id: string.Empty,
            Name: command.Name,
            Description: NullIfEmpty(command.Description),
            Phone: command.Phone,
            Email: NullIfEmpty(command.Email),
            Address: command.Address,
            Active: true);

        var created = await repository.CreateAsync(salon, cancellationToken).ConfigureAwait(true);

        // The rest of the app (every Backend*Repository's ISalonContextService.GetSalonIdAsync
        // call) must see this new salon on its very next read, not after an app restart - see
        // ISalonContextService.Invalidate's own doc comment for why this call is here.
        salonContextService.Invalidate();

        return SalonMapper.MapSalon(created);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
