using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Application.Salons;

/// <summary>Domain&lt;-&gt;Application mapping shared by <see cref="SalonQueryService"/> and <see cref="SalonCommandService"/> - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class SalonMapper
{
    public static SalonDto MapSalon(DomainSalons.Salon salon) => new(
        salon.Id,
        salon.Name,
        salon.Description ?? string.Empty,
        salon.Phone,
        salon.Email ?? string.Empty,
        salon.Address,
        salon.Active);
}
