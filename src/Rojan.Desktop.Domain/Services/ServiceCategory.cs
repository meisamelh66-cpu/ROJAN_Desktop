namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Catalog grouping for a service, as returned by <see cref="IServiceRepository"/>.
///
/// Owner App Service Integration: ROJAN_Backend's own category concept is a
/// real, per-salon, owner-named entity (arbitrary text), not a closed set -
/// incompatible with this fixed enum by construction. Rather than redesign
/// this type into an entity (a much larger change, and Presentation's
/// category filter already enumerates these six values directly), <see cref="Other"/>
/// is the honest catch-all a backend category maps to when its name
/// doesn't match one of the five real ones below (case-insensitively) -
/// see <c>Infrastructure.Services.BackendServiceRepository</c>'s own
/// mapping. The category's real, authoritative name is never lost even
/// when it maps to <see cref="Other"/> - see <see cref="Service.CategoryName"/>.
/// </summary>
public enum ServiceCategory
{
    Hair,
    Colour,
    Nails,
    Skin,
    Spa,
    Consultation,
    Other,
}
