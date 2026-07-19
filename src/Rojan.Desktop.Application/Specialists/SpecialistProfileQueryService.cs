using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Default <see cref="ISpecialistProfileQueryService"/> implementation -
/// fetches the specialist plus their skills from
/// <see cref="DomainSpecialists.ISpecialistRepository"/> and assembles
/// the aggregate <see cref="SpecialistProfileDto"/>.
/// </summary>
public sealed class SpecialistProfileQueryService : ISpecialistProfileQueryService
{
    private readonly DomainSpecialists.ISpecialistRepository _repository;

    public SpecialistProfileQueryService(DomainSpecialists.ISpecialistRepository repository)
    {
        _repository = repository;
    }

    public async Task<SpecialistProfileDto> GetProfileAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        var specialist = await _repository.GetSpecialistByIdAsync(specialistId, cancellationToken).ConfigureAwait(true);
        if (specialist is null)
        {
            throw new InvalidOperationException($"Specialist '{specialistId}' was not found.");
        }

        var skills = await _repository.GetSkillsAsync(specialistId, cancellationToken).ConfigureAwait(true);

        return new SpecialistProfileDto(
            SpecialistMapper.MapSpecialist(specialist),
            skills.Select(SpecialistMapper.MapSkill).ToList());
    }
}
