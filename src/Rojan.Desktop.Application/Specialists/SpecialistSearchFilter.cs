namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Combinable filter criteria for <see cref="ISpecialistQueryService.SearchSpecialistsAsync(SpecialistSearchFilter, CancellationToken)"/> -
/// every field is optional and ANDed together when present, same shape as
/// <c>Customers.CustomerSearchFilter</c>/<c>Bookings.BookingSearchFilter</c>/
/// <c>Services.ServiceSearchFilter</c>. A filter with every field left at
/// its default is equivalent to no filtering at all:
/// <see cref="SpecialistQueryService.SearchSpecialistsAsync(SpecialistSearchFilter, CancellationToken)"/>
/// returns the exact same result set, in the same order, as
/// <see cref="ISpecialistQueryService.GetSpecialistsAsync"/> in that case -
/// "no filter applied" behaves identically to before this filter existed.
///
/// No Category field: unlike <c>Services.Service</c>,
/// <see cref="Domain.Specialists.Specialist"/> has no category of its own
/// to filter by - not invented here, per this commit's "support only data
/// that already exists" scope.
///
/// <see cref="Skill"/> stands in for what the calling task describes as a
/// "skill/service filter": <see cref="Domain.Specialists.ISpecialistRepository"/>
/// has no way to look up which *services* a specialist is assigned to (that
/// relationship is only queryable the other way around, from
/// <c>Services.IServiceRepository.GetAssignedSpecialistsAsync</c>, and
/// building the reverse lookup would mean adding to the Services module,
/// out of scope for a Specialists-only commit) - <see cref="Domain.Specialists.SpecialistSkill"/>
/// is the specialist-owned relationship that actually exists today, the
/// same "closest real relationship, not an invented one" reasoning
/// <c>CustomerSearchFilter.Tag</c> already established for an analogous
/// per-entity collection.
/// </summary>
public sealed record SpecialistSearchFilter(
    string? SearchText = null,
    SpecialistStatus? Status = null,
    string? Skill = null)
{
    public static SpecialistSearchFilter Empty { get; } = new();
}
