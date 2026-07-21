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
/// seed data (کیانا رادمنش, سارا امینی, مهسا کریمی) for a cohesive demo -
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
            new Specialist("specialist-1", "کیانا رادمنش", "متخصص ارشد رنگ مو", "kiana.radmanesh@rojan.example",
                "0912-020-1001", SpecialistStatus.Active, "متخصص بالیاژ و رنگ‌های خلاقانه مو."),
            new Specialist("specialist-2", "سارا امینی", "استایلیست و مشاور", "sara.amini@rojan.example",
                "0912-020-1002", SpecialistStatus.Active, "مسئول مشاوره مشتریان جدید و استایل مو."),
            new Specialist("specialist-3", "مهسا کریمی", "درمانگر اسپا", "mahsa.karimi@rojan.example",
                "0912-020-1003", SpecialistStatus.Active, "مانیکور، فیشیال و خدمات اسپا."),
            new Specialist("specialist-4", "نیلوفر صفایی", "استایلیست جونیور", "niloofar.safaei@rojan.example",
                "0912-020-1004", SpecialistStatus.OnLeave, "در حال حاضر در مرخصی زایمان."),
            new Specialist("specialist-5", "پویا احمدپور", "متخصص سابق رنگ مو", "pouya.ahmadpour@rojan.example",
                "0912-020-1005", SpecialistStatus.Inactive, "دیگر همکار این سالن نیست."),
        ];

        _skills =
        [
            new SpecialistSkill("skill-1", "specialist-1", "رنگ مو"),
            new SpecialistSkill("skill-2", "specialist-1", "بالیاژ"),
            new SpecialistSkill("skill-3", "specialist-1", "کوتاهی و استایل"),
            new SpecialistSkill("skill-4", "specialist-2", "مشاوره"),
            new SpecialistSkill("skill-5", "specialist-2", "کوتاهی و استایل"),
            new SpecialistSkill("skill-6", "specialist-2", "شینیون"),
            new SpecialistSkill("skill-7", "specialist-3", "مانیکور"),
            new SpecialistSkill("skill-8", "specialist-3", "فیشیال"),
            new SpecialistSkill("skill-9", "specialist-3", "ماساژ"),
            new SpecialistSkill("skill-10", "specialist-4", "کوتاهی و استایل"),
            new SpecialistSkill("skill-11", "specialist-5", "رنگ مو"),
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
