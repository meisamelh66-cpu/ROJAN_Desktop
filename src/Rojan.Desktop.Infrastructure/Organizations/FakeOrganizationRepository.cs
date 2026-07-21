using Rojan.Desktop.Domain.Organizations;

namespace Rojan.Desktop.Infrastructure.Organizations;

/// <summary>
/// In-memory <see cref="IOrganizationRepository"/>. Seeds two
/// organizations - "گروه زیبایی روژان" (two branches, ولیعصر و زعفرانیه)
/// and "سالن زیبایی لوکس" (one branch) - so the platform has real,
/// genuinely multi-tenant content on first launch, and so
/// <see cref="GetBranchesAsync"/>'s organization-scoped filtering has more
/// than one organization to actually prove isolation against. Registered
/// as a singleton (same reasoning as every other Fake repository with
/// real writes).
/// </summary>
public sealed class FakeOrganizationRepository : IOrganizationRepository
{
    private readonly List<Organization> _organizations;
    private readonly List<Branch> _branches;
    private readonly List<BranchSettings> _branchSettings;

    public FakeOrganizationRepository()
    {
        var now = DateTimeOffset.Now;

        _organizations =
        [
            new Organization("org-1", "گروه زیبایی روژان", "شرکت گروه زیبایی روژان", string.Empty, "#8E28E7", "TIN-10293847", SubscriptionPlan.Enterprise, OrganizationStatus.Active, now.AddYears(-2), "RBG", "021-88900100", "info@rojanbeauty.example", "تهران، خیابان ولیعصر، برج تجاری مرکزی", "Asia/Tehran", "fa-IR", "تومان"),
            new Organization("org-2", "سالن زیبایی لوکس", "شرکت سالن زیبایی لوکس", string.Empty, "#2FC6C6", "TIN-55219087", SubscriptionPlan.Professional, OrganizationStatus.Active, now.AddYears(-1), "LSC", "021-88900200", "info@luxesalon.example", "تهران، خیابان کریمخان زند", "Asia/Tehran", "fa-IR", "تومان"),
        ];

        _branches =
        [
            new Branch("branch-1", "org-1", "شعبه ولیعصر", "VLI-01", "خیابان ولیعصر، پلاک ۱۲", "021-88900101", "valiasr@rojanbeauty.example", "بهرام رستمی", "Asia/Tehran", "تومان", BranchStatus.Active),
            new Branch("branch-2", "org-1", "شعبه زعفرانیه", "ZAF-01", "خیابان زعفرانیه، پلاک ۸۸", "021-88900102", "zaferanieh@rojanbeauty.example", "سارا امینی", "Asia/Tehran", "تومان", BranchStatus.Active),
            new Branch("branch-3", "org-2", "شعبه مرکزی لوکس", "LUX-01", "خیابان کریمخان زند، پلاک ۴۰۰", "021-88900201", "central@luxesalon.example", "کیانا رادمنش", "Asia/Tehran", "تومان", BranchStatus.Active),
        ];

        _branchSettings =
        [
            new BranchSettings(
                "branch-1",
                new BusinessHours(new TimeOnly(9, 0), new TimeOnly(19, 0)),
                [DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday],
                8.5m,
                new ReceiptSettings("سالن روژان - ولیعصر", "از حضور شما سپاسگزاریم!", true),
                new AppointmentRules(2, 60, true),
                new NotificationSettings(true, true, 24)),
            new BranchSettings(
                "branch-2",
                new BusinessHours(new TimeOnly(10, 0), new TimeOnly(20, 0)),
                [DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday],
                8.5m,
                new ReceiptSettings("سالن روژان - زعفرانیه", "از حضور شما سپاسگزاریم!", true),
                new AppointmentRules(4, 45, false),
                new NotificationSettings(true, false, 12)),
            new BranchSettings(
                "branch-3",
                new BusinessHours(new TimeOnly(9, 30), new TimeOnly(18, 30)),
                [DayOfWeek.Monday, DayOfWeek.Wednesday, DayOfWeek.Friday, DayOfWeek.Saturday],
                7.75m,
                new ReceiptSettings("سالن زیبایی لوکس", "امیدواریم دوباره شما را ببینیم.", false),
                new AppointmentRules(1, 90, true),
                new NotificationSettings(false, true, 6)),
        ];
    }

    public Task<IReadOnlyList<Organization>> GetOrganizationsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Organization>>(_organizations.ToList());

    public Task<Organization?> GetOrganizationByIdAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_organizations.FirstOrDefault(o => o.Id == organizationId));

    public Task<Organization> CreateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        _organizations.Add(organization);
        return Task.FromResult(organization);
    }

    public Task<Organization> UpdateOrganizationAsync(Organization organization, CancellationToken cancellationToken = default)
    {
        var index = _organizations.FindIndex(o => o.Id == organization.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Organization '{organization.Id}' was not found.");
        }

        _organizations[index] = organization;
        return Task.FromResult(organization);
    }

    public Task<IReadOnlyList<Branch>> GetBranchesAsync(string organizationId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Branch>>(_branches.Where(b => b.OrganizationId == organizationId).ToList());

    public Task<Branch?> GetBranchByIdAsync(string branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_branches.FirstOrDefault(b => b.Id == branchId));

    public Task<Branch> CreateBranchAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        _branches.Add(branch);
        return Task.FromResult(branch);
    }

    public Task<Branch> UpdateBranchAsync(Branch branch, CancellationToken cancellationToken = default)
    {
        var index = _branches.FindIndex(b => b.Id == branch.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Branch '{branch.Id}' was not found.");
        }

        _branches[index] = branch;
        return Task.FromResult(branch);
    }

    public Task<BranchSettings?> GetBranchSettingsAsync(string branchId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_branchSettings.FirstOrDefault(s => s.BranchId == branchId));

    public Task<BranchSettings> SetBranchSettingsAsync(BranchSettings settings, CancellationToken cancellationToken = default)
    {
        var index = _branchSettings.FindIndex(s => s.BranchId == settings.BranchId);
        if (index < 0)
        {
            _branchSettings.Add(settings);
        }
        else
        {
            _branchSettings[index] = settings;
        }

        return Task.FromResult(settings);
    }
}
