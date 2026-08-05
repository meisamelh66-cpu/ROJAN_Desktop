using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Specialists;

/// <summary>
/// Reception Booking Integration Phase 2 (Specialist Integration): the
/// real, backend-connected <see cref="DomainSpecialists.ISpecialistRepository"/> -
/// replaces <c>EfSpecialistRepository</c> (which stays in the codebase,
/// unreferenced, same convention as every earlier Fake/Ef-&gt;Backend swap -
/// see <c>BackendBookingRepository</c>'s own doc comment). Follows the same
/// shape <c>Infrastructure.Services.BackendServiceRepository</c> just established: <see cref="ISalonContextService"/>
/// resolves the salon, reuses the already-existing <see cref="SpecialistResponse"/>
/// wire contract unchanged (added during Booking Integration for
/// <c>BackendBookingRepository</c>'s specialist-name lookup). Unlike
/// Customer/Booking, <see cref="DomainSpecialists.Specialist"/> has no
/// Organization/Branch fields at all - confirmed by reading
/// <c>SpecialistQueryService</c>/<c>SpecialistCommandService</c>, neither
/// of which reference <c>IEnterpriseContext</c> - so there is nothing to
/// stamp here either.
///
/// Honesty notes on the mapping, all deliberate:
/// <list type="bullet">
/// <item><see cref="DomainSpecialists.Specialist.Title"/>/<see cref="DomainSpecialists.Specialist.Email"/>/<see cref="DomainSpecialists.Specialist.Phone"/>
/// map to <see cref="string.Empty"/> for backend-sourced specialists - none
/// have a ROJAN_Backend equivalent (only <c>displayName</c>/<c>bio</c>/<c>photoUrl</c>
/// and an optional account link exist there), same "honest, not
/// fabricated" precedent as <c>Customer.Notes</c>.</item>
/// <item><see cref="DomainSpecialists.Specialist.Status"/> only ever comes
/// back <see cref="DomainSpecialists.SpecialistStatus.Active"/> or <see cref="DomainSpecialists.SpecialistStatus.Inactive"/> -
/// ROJAN_Backend's <c>Specialist.active</c> is a plain boolean, with no
/// equivalent of <see cref="DomainSpecialists.SpecialistStatus.OnLeave"/>.
/// Same "the gap is a value that's never produced, not a crash" reasoning
/// as <c>BackendServiceRepository</c>'s own <c>ServiceStatus.Seasonal</c>
/// note.</item>
/// <item><see cref="UpdateSpecialistAsync"/> cannot fulfil an actual status
/// change - see its own doc comment.</item>
/// <item><see cref="GetSkillsAsync"/> always returns empty and <see cref="AddSkillAsync"/>/<see cref="RemoveSkillAsync"/>
/// always throw - ROJAN_Backend has no specialist-skill concept at all,
/// same treatment <c>Infrastructure.Services.BackendServiceRepository</c> already gives the
/// (also backend-absent) specialist-to-service assignment relationship.</item>
/// </list>
/// </summary>
public sealed class BackendSpecialistRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService) : DomainSpecialists.ISpecialistRepository
{
    public async Task<IReadOnlyList<DomainSpecialists.Specialist>> GetSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var responses = await FetchAllSpecialistsAsync(salonId, cancellationToken).ConfigureAwait(false);
        return responses.Select(MapSpecialist).ToList();
    }

    public async Task<DomainSpecialists.Specialist?> GetSpecialistByIdAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var response = await apiClient
            .GetAsync<SpecialistResponse>($"/api/v1/salons/{salonId}/specialists/{specialistId}", cancellationToken)
            .ConfigureAwait(false);

        if (response.StatusCode == 404)
        {
            return null;
        }

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapSpecialist(response.Data);
    }

    /// <summary>Always empty - see this class's own doc comment for why ROJAN_Backend has nothing to fetch here.</summary>
    public Task<IReadOnlyList<DomainSpecialists.SpecialistSkill>> GetSkillsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainSpecialists.SpecialistSkill>>([]);

    public async Task<DomainSpecialists.Specialist> CreateSpecialistAsync(DomainSpecialists.Specialist specialist, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new CreateSpecialistRequest(UserId: null, specialist.FullName, NullIfEmpty(specialist.Bio), PhotoUrl: null);

        var response = await apiClient
            .PostAsync<CreateSpecialistRequest, SpecialistResponse>($"/api/v1/salons/{salonId}/specialists", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create specialist (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return MapSpecialist(response.Data);
    }

    /// <summary>
    /// ROJAN_Backend's <c>PUT /specialists/{id}</c> has no status/active
    /// field at all - there is nothing this call could send to change it.
    /// Name/bio are still sent and applied even when a simultaneous status
    /// change was also requested (an honest partial application, not a
    /// silent drop) - only afterwards, if the caller's requested <see cref="DomainSpecialists.Specialist.Status"/>
    /// genuinely differs from what the backend actually returned, does this
    /// throw <see cref="NotSupportedException"/>. Most calls never hit this
    /// at all: <c>SpecialistCommandService.UpdateSpecialistAsync</c> carries
    /// the specialist's current, unchanged status through on every edit
    /// that isn't itself a status change (see that method's own doc
    /// comment).
    /// </summary>
    public async Task<DomainSpecialists.Specialist> UpdateSpecialistAsync(DomainSpecialists.Specialist specialist, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new UpdateSpecialistRequest(specialist.FullName, NullIfEmpty(specialist.Bio), PhotoUrl: null);

        var response = await apiClient
            .PutAsync<UpdateSpecialistRequest, SpecialistResponse>($"/api/v1/salons/{salonId}/specialists/{specialist.Id}", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to update specialist '{specialist.Id}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        var updated = MapSpecialist(response.Data);
        if (specialist.Status != updated.Status)
        {
            throw new NotSupportedException(
                $"ROJAN_Backend's PUT /specialists/{{id}} has no field to change a specialist's active status - " +
                $"cannot transition '{specialist.Id}' to {specialist.Status}. Name/bio were still applied. See " +
                "BackendSpecialistRepository.UpdateSpecialistAsync's own doc comment.");
        }

        return updated;
    }

    public Task<DomainSpecialists.SpecialistSkill> AddSkillAsync(DomainSpecialists.SpecialistSkill skill, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-skill concept - see BackendSpecialistRepository's own doc comment.");

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-skill concept - see BackendSpecialistRepository's own doc comment.");

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to list specialists for.");
    }

    private async Task<List<SpecialistResponse>> FetchAllSpecialistsAsync(string salonId, CancellationToken cancellationToken)
    {
        var response = await apiClient
            .GetAsync<List<SpecialistResponse>>($"/api/v1/salons/{salonId}/specialists", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load specialists (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data;
    }

    private static DomainSpecialists.Specialist MapSpecialist(SpecialistResponse response) => new(
        response.Id,
        response.DisplayName,
        string.Empty,
        string.Empty,
        string.Empty,
        response.Active ? DomainSpecialists.SpecialistStatus.Active : DomainSpecialists.SpecialistStatus.Inactive,
        response.Bio ?? string.Empty);

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
