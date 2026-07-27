using Rojan.Server.Domain.Specialists;

namespace Rojan.Server.Application.Tests.Specialists;

/// <summary>In-memory <see cref="ISpecialistRepository"/> test double - filters exactly the same way <c>Infrastructure.Persistence.Repositories.EfSpecialistRepository</c> does, so tenant-isolation behavior is exercised the same way here as it would be against a real database.</summary>
internal sealed class FakeSpecialistRepository : ISpecialistRepository
{
    public List<Specialist> Specialists { get; } = [];

    public Task<Specialist> CreateAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        Specialists.Add(specialist);
        return Task.FromResult(specialist);
    }

    public Task<Specialist?> GetByIdAsync(string organizationId, string specialistId, CancellationToken cancellationToken = default) =>
        Task.FromResult(Specialists.FirstOrDefault(specialist => specialist.Id == specialistId && specialist.OrganizationId == organizationId));

    public Task<IReadOnlyList<Specialist>> GetByOrganizationIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Specialist>>(Specialists.Where(specialist => specialist.OrganizationId == organizationId).ToList());

    public Task<Specialist> UpdateAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        var index = Specialists.FindIndex(existing => existing.Id == specialist.Id && existing.OrganizationId == specialist.OrganizationId);
        if (index >= 0)
        {
            Specialists[index] = specialist;
        }

        return Task.FromResult(specialist);
    }
}
