using Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Tests.Specialists;

/// <summary>In-memory, mutable <see cref="ISpecialistRepository"/> test double - same reasoning as Customers.StubCustomerRepository.</summary>
internal sealed class StubSpecialistRepository : ISpecialistRepository
{
    public List<Specialist> Specialists { get; } = [];

    public List<SpecialistSkill> Skills { get; } = [];

    public List<(string SpecialistId, string ServiceId)> ServiceAssignments { get; } = [];

    public StubSpecialistRepository()
    {
    }

    public StubSpecialistRepository(IReadOnlyList<Specialist> specialists)
    {
        Specialists.AddRange(specialists);
    }

    public Task<IReadOnlyList<Specialist>> GetSpecialistsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Specialist>>(Specialists.ToList());

    public Task<Specialist?> GetSpecialistByIdAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Specialists.FirstOrDefault(specialist => specialist.Id == specialistId));

    public Task<IReadOnlyList<SpecialistSkill>> GetSkillsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SpecialistSkill>>(Skills.Where(skill => skill.SpecialistId == specialistId).ToList());

    public Task<Specialist> CreateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        Specialists.Add(specialist);
        return Task.FromResult(specialist);
    }

    public Task<Specialist> UpdateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        var index = Specialists.FindIndex(existing => existing.Id == specialist.Id);
        if (index >= 0)
        {
            Specialists[index] = specialist;
        }

        return Task.FromResult(specialist);
    }

    public Task<SpecialistSkill> AddSkillAsync(SpecialistSkill skill, CancellationToken cancellationToken = default)
    {
        Skills.Add(skill);
        return Task.FromResult(skill);
    }

    public Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        Skills.RemoveAll(skill => skill.SpecialistId == specialistId && skill.Id == skillId);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<string>> GetAssignedServiceIdsAsync(string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(
            ServiceAssignments.Where(assignment => assignment.SpecialistId == specialistId).Select(assignment => assignment.ServiceId).ToList());

    public Task AssignServiceAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        if (!ServiceAssignments.Contains((specialistId, serviceId)))
        {
            ServiceAssignments.Add((specialistId, serviceId));
        }

        return Task.CompletedTask;
    }

    public Task RemoveServiceAssignmentAsync(string specialistId, string serviceId, CancellationToken cancellationToken = default)
    {
        ServiceAssignments.RemoveAll(assignment => assignment.SpecialistId == specialistId && assignment.ServiceId == serviceId);
        return Task.CompletedTask;
    }
}
