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
/// <item><see cref="UpdateSpecialistAsync"/> can only fulfil one status
/// transition - Active to Inactive (deactivation), via ROJAN_Backend's
/// dedicated <c>DELETE /specialists/{id}</c> endpoint. Every other
/// direction (Inactive back to Active/reactivation, anything involving
/// <see cref="DomainSpecialists.SpecialistStatus.OnLeave"/>) still has no
/// backend mutation path at all and still throws - see that method's own
/// doc comment for why this is a deliberate scope boundary, not an
/// oversight.</item>
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
    /// silent drop). If the caller's requested <see cref="DomainSpecialists.Specialist.Status"/>
    /// genuinely differs from what the backend returned, and that
    /// difference is specifically Active -&gt; Inactive, this follows up
    /// with ROJAN_Backend's dedicated <c>DELETE /specialists/{id}</c>
    /// deactivate endpoint (Specialist Deactivation Wiring). Every other
    /// direction - Inactive -&gt; Active (reactivation) or anything
    /// involving <see cref="DomainSpecialists.SpecialistStatus.OnLeave"/> -
    /// still has no backend mutation path at all and still throws
    /// <see cref="NotSupportedException"/>, deliberately: this class must
    /// never fabricate a status change ROJAN_Backend never actually
    /// authorized. Most calls never reach any of this: <c>SpecialistCommandService.UpdateSpecialistAsync</c>
    /// carries the specialist's current, unchanged status through on every
    /// edit that isn't itself a status change (see that method's own doc
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
        if (specialist.Status == updated.Status)
        {
            return updated;
        }

        if (updated.Status == DomainSpecialists.SpecialistStatus.Active && specialist.Status == DomainSpecialists.SpecialistStatus.Inactive)
        {
            return await DeactivateAsync(salonId, updated, cancellationToken).ConfigureAwait(false);
        }

        throw new NotSupportedException(
            $"ROJAN_Backend has no mutation path to change a specialist's status from {updated.Status} to " +
            $"{specialist.Status} - only Active -> Inactive (deactivation) is supported today, via DELETE " +
            "/specialists/{id}. Name/bio were still applied. See BackendSpecialistRepository.UpdateSpecialistAsync's own doc comment.");
    }

    /// <summary>
    /// The Active -&gt; Inactive half of <see cref="UpdateSpecialistAsync"/> -
    /// calls ROJAN_Backend's own dedicated deactivate endpoint (a 204/no-body
    /// response), then returns <paramref name="updated"/> with its
    /// <see cref="DomainSpecialists.Specialist.Status"/> overridden to
    /// <see cref="DomainSpecialists.SpecialistStatus.Inactive"/> - an honest
    /// reflection of a confirmed-successful backend mutation (every other
    /// field already came straight from ROJAN_Backend's own <c>PUT</c>
    /// response), not a locally-invented status.
    /// </summary>
    private async Task<DomainSpecialists.Specialist> DeactivateAsync(string salonId, DomainSpecialists.Specialist updated, CancellationToken cancellationToken)
    {
        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{updated.Id}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to deactivate specialist '{updated.Id}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return updated with { Status = DomainSpecialists.SpecialistStatus.Inactive };
    }

    public Task<DomainSpecialists.SpecialistSkill> AddSkillAsync(DomainSpecialists.SpecialistSkill skill, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-skill concept - see BackendSpecialistRepository's own doc comment.");

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-skill concept - see BackendSpecialistRepository's own doc comment.");

    /// <summary>
    /// Specialist-Service Assignment: real, backend-connected -
    /// ROJAN_Backend's <c>GET /specialists/{id}/services</c> returns the
    /// eligible service ids only (no names - resolved one layer up in
    /// Application). Unlike <see cref="GetSpecialistByIdAsync"/> a missing
    /// specialist here still surfaces as a real failure rather than an
    /// empty list - the caller already knows the specialist exists (it
    /// asked for their profile), so a 404 here means something is
    /// genuinely wrong, not "no assignments yet".
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);

        var response = await apiClient
            .GetAsync<List<Guid>>($"/api/v1/salons/{salonId}/specialists/{specialistId}/services", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load assigned services for specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(id => id.ToString()).ToList();
    }

    /// <summary>Real, backend-connected - ROJAN_Backend's <c>PUT /specialists/{id}/services/{serviceId}</c>, a 204/no-body response. No synthetic assignment id anywhere - the real <c>(specialistId, serviceId)</c> pair is the whole identity, matching ROJAN_Backend's own model.</summary>
    public async Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);

        var response = await apiClient
            .PutAsync<object?, object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/services/{serviceId}", null, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to assign service '{serviceId}' to specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    /// <summary>Real, backend-connected - ROJAN_Backend's <c>DELETE /specialists/{id}/services/{serviceId}</c>, same shape as <see cref="DeactivateAsync"/>'s own DELETE call.</summary>
    public async Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);

        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/specialists/{specialistId}/services/{serviceId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to remove service '{serviceId}' from specialist '{specialistId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

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
