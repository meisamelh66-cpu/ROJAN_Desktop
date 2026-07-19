using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>Default <see cref="ISpecialistCommandService"/> implementation.</summary>
public sealed class SpecialistCommandService : ISpecialistCommandService
{
    private readonly DomainSpecialists.ISpecialistRepository _repository;

    public SpecialistCommandService(DomainSpecialists.ISpecialistRepository repository)
    {
        _repository = repository;
    }

    public async Task<SpecialistDto> CreateSpecialistAsync(CreateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        var specialist = new DomainSpecialists.Specialist(
            Guid.NewGuid().ToString(),
            request.FullName,
            request.Title,
            request.Email,
            request.Phone,
            DomainSpecialists.SpecialistStatus.Active,
            request.Bio);

        var created = await _repository.CreateSpecialistAsync(specialist, cancellationToken).ConfigureAwait(true);
        return SpecialistMapper.MapSpecialist(created);
    }

    public async Task<SpecialistDto> UpdateSpecialistAsync(UpdateSpecialistRequest request, CancellationToken cancellationToken = default)
    {
        var specialist = new DomainSpecialists.Specialist(
            request.Id,
            request.FullName,
            request.Title,
            request.Email,
            request.Phone,
            SpecialistMapper.MapStatusToDomain(request.Status),
            request.Bio);

        var updated = await _repository.UpdateSpecialistAsync(specialist, cancellationToken).ConfigureAwait(true);
        return SpecialistMapper.MapSpecialist(updated);
    }

    public async Task<SpecialistSkillDto> AddSkillAsync(string specialistId, string name, CancellationToken cancellationToken = default)
    {
        var skill = new DomainSpecialists.SpecialistSkill(Guid.NewGuid().ToString(), specialistId, name);
        var added = await _repository.AddSkillAsync(skill, cancellationToken).ConfigureAwait(true);
        return SpecialistMapper.MapSkill(added);
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default) =>
        _repository.RemoveSkillAsync(specialistId, skillId, cancellationToken);
}
