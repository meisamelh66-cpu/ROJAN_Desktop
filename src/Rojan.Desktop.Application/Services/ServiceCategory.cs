namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Application's own copy of <see cref="Rojan.Desktop.Domain.Services.ServiceCategory"/> -
/// distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>:
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="ServiceMapper"/>.
/// </summary>
public enum ServiceCategory
{
    Hair,
    Colour,
    Nails,
    Skin,
    Spa,
    Consultation,
}
