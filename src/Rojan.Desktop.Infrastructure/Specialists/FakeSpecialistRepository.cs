using Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Specialists;

/// <summary>
/// In-memory <see cref="ISpecialistRepository"/> providing static sample
/// data - Phase 12 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real create/update/skill commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). Seed names match the
/// specialists already referenced by <c>Bookings.FakeBookingRepository</c>'s
/// seed data (Jordan Lee, Priya Nair, Casey Morgan) for a cohesive demo -
/// not a real cross-slice link, just consistent naming. The small
/// artificial delays are deliberate, same reasoning as every other fake
/// repository: without them, Loading states would never actually be
/// observable when running the app.
/// </summary>
public sealed class FakeSpecialistRepository : ISpecialistRepository
{
    private readonly List<Specialist> _specialists;
    private readonly List<SpecialistSkill> _skills;

    public FakeSpecialistRepository()
    {
        _specialists =
        [
            new Specialist("specialist-1", "Jordan Lee", "Senior Colour Specialist", "jordan.lee@rojan.example",
                "+1 (555) 020-1001", SpecialistStatus.Active, "Specializes in balayage and creative colour work."),
            new Specialist("specialist-2", "Priya Nair", "Stylist & Consultant", "priya.nair@rojan.example",
                "+1 (555) 020-1002", SpecialistStatus.Active, "Leads new-client consultations and styling."),
            new Specialist("specialist-3", "Casey Morgan", "Spa Therapist", "casey.morgan@rojan.example",
                "+1 (555) 020-1003", SpecialistStatus.Active, "Manicures, facials, and spa treatments."),
            new Specialist("specialist-4", "Riley Chen", "Junior Stylist", "riley.chen@rojan.example",
                "+1 (555) 020-1004", SpecialistStatus.OnLeave, "Currently on parental leave."),
            new Specialist("specialist-5", "Sam Torres", "Former Colour Specialist", "sam.torres@rojan.example",
                "+1 (555) 020-1005", SpecialistStatus.Inactive, "No longer with the salon."),
        ];

        _skills =
        [
            new SpecialistSkill("skill-1", "specialist-1", "Colour"),
            new SpecialistSkill("skill-2", "specialist-1", "Balayage"),
            new SpecialistSkill("skill-3", "specialist-1", "Haircut & Style"),
            new SpecialistSkill("skill-4", "specialist-2", "Consultation"),
            new SpecialistSkill("skill-5", "specialist-2", "Haircut & Style"),
            new SpecialistSkill("skill-6", "specialist-2", "Updo"),
            new SpecialistSkill("skill-7", "specialist-3", "Manicure"),
            new SpecialistSkill("skill-8", "specialist-3", "Facial"),
            new SpecialistSkill("skill-9", "specialist-3", "Massage"),
            new SpecialistSkill("skill-10", "specialist-4", "Haircut & Style"),
            new SpecialistSkill("skill-11", "specialist-5", "Colour"),
        ];
    }

    public async Task<IReadOnlyList<Specialist>> GetSpecialistsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _specialists.ToList();
    }

    public async Task<Specialist?> GetSpecialistByIdAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _specialists.FirstOrDefault(specialist => specialist.Id == specialistId);
    }

    public async Task<IReadOnlyList<SpecialistSkill>> GetSkillsAsync(string specialistId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _skills.Where(skill => skill.SpecialistId == specialistId).ToList();
    }

    public async Task<Specialist> CreateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _specialists.Add(specialist);
        return specialist;
    }

    public async Task<Specialist> UpdateSpecialistAsync(Specialist specialist, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _specialists.FindIndex(existing => existing.Id == specialist.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Specialist '{specialist.Id}' was not found.");
        }

        _specialists[index] = specialist;
        return specialist;
    }

    public async Task<SpecialistSkill> AddSkillAsync(SpecialistSkill skill, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _skills.Add(skill);
        return skill;
    }

    public async Task RemoveSkillAsync(string specialistId, string skillId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _skills.RemoveAll(skill => skill.SpecialistId == specialistId && skill.Id == skillId);
    }
}
