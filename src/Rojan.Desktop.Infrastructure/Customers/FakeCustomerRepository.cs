using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Customers;

/// <summary>
/// In-memory <see cref="ICustomerRepository"/> providing static sample
/// data - Phase 09/10 explicitly has no backend integration yet, same as
/// the Dashboard vertical slice in Phase 06B. Instance (not static) mutable
/// state, unlike <c>FakeDashboardRepository</c>: Phase 10 adds real
/// create/update/note/tag commands, so this fake now needs to actually
/// remember writes for the app's lifetime - registered as a DI singleton
/// (see Infrastructure's ServiceCollectionExtensions), so one instance
/// backs the whole running app, which is exactly the in-memory-database
/// behavior a fake repository should have. Read methods that return
/// multiple items snapshot via <c>.ToList()</c> so callers never observe a
/// later write mutating a collection they already received. The small
/// artificial delays are deliberate, same reasoning as
/// <c>FakeDashboardRepository</c>: without them, Loading states would
/// never actually be observable when running the app.
/// </summary>
public sealed class FakeCustomerRepository : ICustomerRepository
{
    private readonly List<Customer> _customers;
    private readonly List<CustomerNote> _notes;
    private readonly List<CustomerTag> _tags;
    private readonly List<CustomerActivity> _activity;

    public FakeCustomerRepository()
    {
        var now = DateTimeOffset.Now;

        // Phase 22A: spread across org-1/branch-1 (ولیعصر), org-1/branch-2
        // (زعفرانیه), and org-2/branch-3 (مرکزی لوکس) - the same seeded ids
        // Infrastructure.Organizations.FakeOrganizationRepository uses - so
        // Organization/Branch Scoping has genuinely mixed data to filter,
        // not a single-tenant fixture that would make isolation vacuously
        // true.
        _customers =
        [
            new Customer(
                "customer-1", "لیلا احمدی", "سالن زیبایی احمدی", "leila.ahmadi@example.com", "0912-100-2231",
                CustomerStatus.Active, "48,200,000 تومان", now.AddDays(-1),
                "ترجیح می‌دهد نوبت‌های عصرگاهی داشته باشد. مشتری ثابت اصلاح رنگ.", "org-1", "branch-1"),
            new Customer(
                "customer-2", "امیر رضایی", string.Empty, "amir.rezaei@example.com", "0912-100-7742",
                CustomerStatus.Prospect, "0 تومان", now.AddDays(-3),
                "معرفی‌شده توسط لیلا احمدی. علاقه‌مند به اولین جلسه مشاوره.", "org-1", "branch-1"),
            new Customer(
                "customer-3", "سارا محمدی", "استودیو زیبایی محمدی", "sara.mohammadi@example.com", "0912-100-5518",
                CustomerStatus.Vip, "121,500,000 تومان", now.AddHours(-6),
                "مشتری ویژه. هر ماه پکیج کامل را رزرو می‌کند.", "org-1", "branch-1"),
            new Customer(
                "customer-4", "علی کریمی", string.Empty, "ali.karimi@example.com", "0912-100-3390",
                CustomerStatus.Churned, "6,100,000 تومان", now.AddMonths(-4),
                "بیش از ۹۰ روز است نوبتی ثبت نکرده. برای پیشنهاد بازگشت علامت‌گذاری شد.", "org-1", "branch-1"),
            new Customer(
                "customer-5", "مریم حسینی", "گروه سلامت حسینی", "maryam.hosseini@example.com", "0912-100-9027",
                CustomerStatus.Active, "73,400,000 تومان", now.AddDays(-9),
                "حساب سازمانی - برای تیم شش‌نفره رزرو می‌کند.", "org-1", "branch-2"),
            new Customer(
                "customer-6", "رضا نوری", string.Empty, "reza.nouri@example.com", "0912-100-4465",
                CustomerStatus.Lead, "0 تومان", now.AddDays(-2),
                "از طریق ویجت رزرو سایت ثبت‌نام کرده، هنوز تماس گرفته نشده.", "org-1", "branch-2"),
            new Customer(
                "customer-7", "نگار صادقی", "زیبایی صادقی", "negar.sadeghi@example.com", "0912-100-6604",
                CustomerStatus.Inactive, "21,500,000 تومان", now.AddMonths(-2),
                "پس از جابه‌جایی موقتاً توقف کرده؛ احتمال بازگشت پس از استقرار.", "org-2", "branch-3"),
        ];

        _notes =
        [
            new CustomerNote("note-1", "customer-1", "به برخی رنگ‌های مو حساسیت دارد - همیشه پیش از خدمات رنگ بررسی شود.", now.AddDays(-30)),
            new CustomerNote("note-2", "customer-1", "از این پس ۱۵ دقیقه زمان بیشتر برای مشاوره درخواست کرده است.", now.AddDays(-5)),
            new CustomerNote("note-3", "customer-3", "یادآوری پیامکی را به ایمیل ترجیح می‌دهد.", now.AddDays(-10)),
            new CustomerNote("note-4", "customer-5", "مسئول اصلی حساب سازمانی، دستیار ایشان مینا است.", now.AddDays(-20)),
        ];

        _tags =
        [
            new CustomerTag("tag-1", "customer-1", "دائمی", now.AddDays(-60)),
            new CustomerTag("tag-2", "customer-1", "متخصص رنگ", now.AddDays(-60)),
            new CustomerTag("tag-3", "customer-3", "ویژه", now.AddDays(-45)),
            new CustomerTag("tag-4", "customer-3", "پکیج ماهانه", now.AddDays(-45)),
            new CustomerTag("tag-5", "customer-5", "سازمانی", now.AddDays(-40)),
            new CustomerTag("tag-6", "customer-4", "هدف بازگشت", now.AddMonths(-4)),
            new CustomerTag("tag-7", "customer-2", "معرفی‌شده", now.AddDays(-3)),
        ];

        _activity =
        [
            new CustomerActivity("activity-1", "customer-1", "مشتری ایجاد شد", now.AddDays(-60)),
            new CustomerActivity("activity-2", "customer-1", "نوبت تکمیل شد", now.AddDays(-30)),
            new CustomerActivity("activity-3", "customer-1", "یادداشت افزوده شد", now.AddDays(-30)),
            new CustomerActivity("activity-4", "customer-1", "پرداخت دریافت شد", now.AddDays(-5)),
            new CustomerActivity("activity-5", "customer-1", "نوبت تکمیل شد", now.AddDays(-1)),

            new CustomerActivity("activity-6", "customer-2", "مشتری ایجاد شد", now.AddDays(-3)),
            new CustomerActivity("activity-7", "customer-2", "معرفی از طرف لیلا احمدی دریافت شد", now.AddDays(-3)),

            new CustomerActivity("activity-8", "customer-3", "مشتری ایجاد شد", now.AddDays(-90)),
            new CustomerActivity("activity-9", "customer-3", "به سطح ویژه ارتقا یافت", now.AddDays(-45)),
            new CustomerActivity("activity-10", "customer-3", "نوبت تکمیل شد", now.AddDays(-10)),
            new CustomerActivity("activity-11", "customer-3", "نوبت تکمیل شد", now.AddHours(-6)),

            new CustomerActivity("activity-12", "customer-4", "مشتری ایجاد شد", now.AddMonths(-6)),
            new CustomerActivity("activity-13", "customer-4", "نوبت تکمیل شد", now.AddMonths(-4)),
            new CustomerActivity("activity-14", "customer-4", "به‌عنوان ازدست‌رفته علامت‌گذاری شد - برای بازگشت پیگیری شود", now.AddMonths(-4)),

            new CustomerActivity("activity-15", "customer-5", "مشتری ایجاد شد", now.AddDays(-40)),
            new CustomerActivity("activity-16", "customer-5", "نوبت حساب سازمانی ایجاد شد", now.AddDays(-9)),

            new CustomerActivity("activity-17", "customer-6", "مشتری ایجاد شد", now.AddDays(-2)),
            new CustomerActivity("activity-18", "customer-6", "از طریق ویجت رزرو سایت ثبت‌نام کرد", now.AddDays(-2)),

            new CustomerActivity("activity-19", "customer-7", "مشتری ایجاد شد", now.AddMonths(-5)),
            new CustomerActivity("activity-20", "customer-7", "نوبت تکمیل شد", now.AddMonths(-2)),
            new CustomerActivity("activity-21", "customer-7", "پس از جابه‌جایی غیرفعال علامت‌گذاری شد", now.AddMonths(-2)),
        ];
    }

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _customers.ToList();
    }

    public async Task<Customer?> GetCustomerByIdAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _customers.FirstOrDefault(customer => customer.Id == customerId);
    }

    public async Task<IReadOnlyList<CustomerNote>> GetNotesAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _notes
            .Where(note => note.CustomerId == customerId)
            .OrderByDescending(note => note.CreatedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<CustomerTag>> GetTagsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _tags
            .Where(tag => tag.CustomerId == customerId)
            .OrderBy(tag => tag.CreatedAt)
            .ToList();
    }

    public async Task<IReadOnlyList<CustomerActivity>> GetActivityAsync(string customerId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _activity
            .Where(activity => activity.CustomerId == customerId)
            .OrderByDescending(activity => activity.OccurredAt)
            .ToList();
    }

    public async Task<Customer> CreateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _customers.Add(customer);
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _customers.FindIndex(existing => existing.Id == customer.Id);
        if (index < 0)
        {
            throw new InvalidOperationException($"Customer '{customer.Id}' was not found.");
        }

        _customers[index] = customer;
        return customer;
    }

    public async Task<CustomerNote> AddNoteAsync(CustomerNote note, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _notes.Add(note);
        return note;
    }

    public async Task<CustomerTag> AddTagAsync(CustomerTag tag, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _tags.Add(tag);
        return tag;
    }

    public async Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _tags.RemoveAll(tag => tag.CustomerId == customerId && tag.Id == tagId);
    }

    public async Task<CustomerActivity> AddActivityAsync(CustomerActivity activity, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _activity.Add(activity);
        return activity;
    }
}
