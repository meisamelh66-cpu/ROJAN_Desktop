namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body <c>GET {ApiVersion.BasePath()}/users/me/salon-access</c>
/// returns - matches ROJAN_Backend's <c>SalonAccessResponseDto</c>
/// field-for-field (see <c>api/user/SalonAccessDtos.kt</c>). Phase 1 Context
/// Source Alignment: only <see cref="OwnedSalonAccess.SalonId"/>/<see cref="OwnedSalonAccess.SalonName"/>
/// and their <see cref="MembershipAccess"/> counterparts are consumed today
/// (by <see cref="Salons.ISalonContextService"/>); <see cref="SpecialistLinks"/>
/// and every entry's <c>Permissions</c> are kept for completeness/future use,
/// matching this namespace's existing "mirror the backend shape, not just
/// what's used yet" convention (see <c>SalonResponse.cs</c>) - carrying
/// <c>Permissions</c> into <see cref="Salons.SalonContext"/> or any
/// authorization decision is explicitly out of this phase's scope.
/// </summary>
public sealed record SalonAccessResponse(
    IReadOnlyList<OwnedSalonAccess> OwnedSalons,
    IReadOnlyList<MembershipAccess> Memberships,
    IReadOnlyList<SpecialistAccess> SpecialistLinks);

/// <summary>One salon the caller owns - matches ROJAN_Backend's <c>OwnedSalonAccessDto</c> field-for-field.</summary>
public sealed record OwnedSalonAccess(string SalonId, string SalonName, bool Active, IReadOnlyList<string> Permissions);

/// <summary>One salon the caller has an active staff membership in - matches ROJAN_Backend's <c>MembershipAccessDto</c> field-for-field. <see cref="Role"/> is the raw backend <c>SalonRole</c> string (e.g. <c>"MANAGER"</c>/<c>"RECEPTIONIST"</c>), same convention <see cref="Salons.SalonContext.MembershipRole"/> already uses.</summary>
public sealed record MembershipAccess(string MembershipId, string SalonId, string SalonName, bool Active, string Role, IReadOnlyList<string> Permissions);

/// <summary>One salon the caller is linked to as a specialist - matches ROJAN_Backend's <c>SpecialistAccessDto</c> field-for-field. Not yet resolved into <see cref="Salons.SalonContext"/> - see this phase's own plan document for why.</summary>
public sealed record SpecialistAccess(string SpecialistId, string SalonId, string SalonName, bool Active, IReadOnlyList<string> Permissions);
