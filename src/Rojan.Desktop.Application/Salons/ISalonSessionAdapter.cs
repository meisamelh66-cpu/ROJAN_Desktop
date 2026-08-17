using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Salons;

/// <summary>
/// Salon Adapter Migration Phase 1: translates a resolved real
/// <see cref="SalonContext"/> into Desktop's legacy Organization-shaped
/// types (<see cref="OrganizationDto"/>, <see cref="WorkspaceRole"/>) so
/// existing consumers built against that shape keep working unchanged
/// while the migration to a Salon-native session type proceeds in later
/// phases. Extracted, with no behavior change, from what was previously
/// inline in <c>Shell.Organizations.CurrentSessionService</c>'s own
/// <c>ApplyRealMembership</c>/<c>MapRole</c> methods - see
/// ROJAN_Desktop_Salon_Adapter_Migration_Plan.md for the full phased plan
/// this is Phase 1 of.
/// </summary>
public interface ISalonSessionAdapter
{
    /// <summary>
    /// Maps a real Salon session onto the legacy <see cref="OrganizationDto"/>
    /// shape. Only <see cref="OrganizationDto.Id"/>/<see cref="OrganizationDto.Name"/>
    /// carry real data - every other field (LegalName/Logo/Subscription/
    /// Code/TimeZone/etc.) has no equivalent on ROJAN_Backend's Salon and is
    /// populated with honest defaults, not fabricated values standing in
    /// for real data that simply does not exist in this backend's model.
    /// </summary>
    OrganizationDto ToOrganizationDto(SalonContext salonContext);

    /// <summary>
    /// Maps a real Salon session's ownership/membership onto Desktop's
    /// local <see cref="WorkspaceRole"/> enum. An owner's role is never a
    /// <c>SalonRole</c> membership (see ROJAN_Backend's own doc comment on
    /// that enum) - only the non-owner branch maps a backend role string.
    /// Falls back to <see cref="WorkspaceRole.Reception"/> for any role
    /// string this Desktop app doesn't otherwise recognize (e.g. a manager
    /// invite accepted here, out of this phase's own scope) rather than
    /// throwing - a real, backend-confirmed membership must never fail to
    /// resolve into *some* usable session.
    /// </summary>
    WorkspaceRole ToWorkspaceRole(SalonContext salonContext);
}
