namespace Rojan.Desktop.Application.Specialists;

/// <summary>Write use cases for Specialists - same command-side pattern <c>Customers.ICustomerCommandService</c> established in Phase 10.</summary>
public interface ISpecialistCommandService
{
    public Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default);

    public Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default);

    public Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default);

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default);

    /// <summary>Specialist-Service Assignment: assigns a real, catalog service id to this specialist. See <see cref="Domain.Specialists.ISpecialistRepository"/>'s own doc comment for why this is keyed on the real (specialistId, serviceId) pair, no synthetic id.</summary>
    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default);

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default);
}
