namespace Rojan.Desktop.Domain.Specialists;

/// <summary>
/// Repository abstraction for specialist data. Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 12 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
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
}
