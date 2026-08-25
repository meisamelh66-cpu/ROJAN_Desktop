namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// Repository abstraction for specialist data. Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 12 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
///
/// Specialist-Service Assignment: <see cref="GetAssignedServiceIdsAsync"/>/
/// <see cref="AssignServiceAsync"/>/<see cref="RemoveServiceAssignmentAsync"/>
/// are keyed on the real <c>(specialistId, serviceId)</c> pair only - no
/// synthetic assignment id, matching ROJAN_Backend's own
/// <c>GET/PUT/DELETE /specialists/{id}/services/{serviceId}</c> shape
/// exactly. This is deliberately a different, specialist-centric model
/// from the pre-existing, free-text-name, synthetic-id
/// <see cref="Services.SpecialistService"/>/<c>IServiceRepository.*SpecialistAsync</c>
/// members (service-centric, and never backend-connected) - see
/// <c>Infrastructure.Specialists.BackendSpecialistRepository</c>'s own doc
/// comment for the full reasoning. That older model is untouched by this
/// addition.
/// </summary>
public interface ISpecialistRepository
{
    public Task<IReadOnlyList<Specialist>> GetSpecialistsAsync(CancellationToken cancellationToken = default);

    public Task<Specialist?> GetSpecialistByIdAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistSkill>> GetSkillsAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task<Specialist> CreateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default);

    public Task<Specialist> UpdateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default);

    public Task<SpecialistSkill> AddSkillAsync(SpecialistSkill skill, CancellationToken cancellationToken = default);

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default);

    /// <summary>The real, backend-owned service ids this specialist is eligible to perform. Names are resolved one layer up (Application), never here - this stays a thin, honest mirror of ROJAN_Backend's own id-only response.</summary>
    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default);

    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default);

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default);
}
