using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Application.Specialists;

/// <summary>
/// Default <see cref="ISpecialistCommandService"/> implementation.
///
/// Sprint 5 Commit 3: <see cref="UpdateSpecialistAsync"/> enforces
/// <see cref="DomainSpecialists.SpecialistRules"/> only when the request
/// actually changes status - <c>UpdateSpecialistAsync</c> is a full-field
/// replacement (name/title/email/phone/status/bio all in one call, unlike
/// Bookings' dedicated status-only command), so most calls carry the
/// specialist's current, unchanged status through unexamined; only a real
/// status change is validated as a transition - the same reasoning
/// <c>Customers.CustomerCommandService.UpdateCustomerAsync</c> already
/// established for its own identically-shaped update command in Sprint 4
/// Commit 1.
/// </summary>
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
        var existing = await _repository.GetSpecialistByIdAsync(request.Id, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Specialist '{request.Id}' was not found.");

        var newStatus = SpecialistMapper.MapStatusToDomain(request.Status);
        if (newStatus != existing.Status && !DomainSpecialists.SpecialistRules.IsValidTransition(existing.Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition specialist from {existing.Status} to {newStatus}.");
        }

        var specialist = new DomainSpecialists.Specialist(
            request.Id,
            request.FullName,
            request.Title,
            request.Email,
            request.Phone,
            newStatus,
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
