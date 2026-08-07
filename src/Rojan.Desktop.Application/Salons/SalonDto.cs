namespace Rojan.Desktop.Application.Salons;

/// <summary>Application-layer shape of a salon, mapped from <see cref="Domain.Salons.Salon"/> by <see cref="SalonMapper"/>. <see cref="Description"/>/<see cref="Email"/> are never null here (empty string instead) - same "Presentation never has to null-check an optional field" convention <c>Customers.CustomerDto</c> already establishes.</summary>
public sealed record SalonDto(
    string Id,
    string Name,
    string Description,
    string Phone,
    string Email,
    string Address,
    bool Active);
