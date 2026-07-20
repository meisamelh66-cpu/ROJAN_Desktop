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

        // Phase 22A: spread across org-1/branch-1 (Downtown), org-1/branch-2
        // (Uptown), and org-2/branch-3 (Luxe Central) - the same seeded ids
        // Infrastructure.Organizations.FakeOrganizationRepository uses - so
        // Organization/Branch Scoping has genuinely mixed data to filter,
        // not a single-tenant fixture that would make isolation vacuously
        // true.
        _customers =
        [
            new Customer(
                "customer-1", "Amelia Hart", "Hart & Co. Salon", "amelia.hart@example.com", "+1 (555) 010-2231",
                CustomerStatus.Active, "$4,820", now.AddDays(-1),
                "Prefers evening appointments. Regular colour touch-up client.", "org-1", "branch-1"),
            new Customer(
                "customer-2", "Noah Bennett", string.Empty, "noah.bennett@example.com", "+1 (555) 010-7742",
                CustomerStatus.Prospect, "$0", now.AddDays(-3),
                "Referred by Amelia Hart. Interested in a first-time consultation.", "org-1", "branch-1"),
            new Customer(
                "customer-3", "Sophia Reyes", "Reyes Beauty Studio", "sophia.reyes@example.com", "+1 (555) 010-5518",
                CustomerStatus.Vip, "$12,150", now.AddHours(-6),
                "VIP tier. Books the full package monthly.", "org-1", "branch-1"),
            new Customer(
                "customer-4", "Liam Foster", string.Empty, "liam.foster@example.com", "+1 (555) 010-3390",
                CustomerStatus.Churned, "$610", now.AddMonths(-4),
                "No bookings in over 90 days. Flagged for a re-engagement offer.", "org-1", "branch-1"),
            new Customer(
                "customer-5", "Olivia Chen", "Chen Wellness Group", "olivia.chen@example.com", "+1 (555) 010-9027",
                CustomerStatus.Active, "$7,340", now.AddDays(-9),
                "Corporate account - books for a team of six.", "org-1", "branch-2"),
            new Customer(
                "customer-6", "Ethan Brooks", string.Empty, "ethan.brooks@example.com", "+1 (555) 010-4465",
                CustomerStatus.Lead, "$0", now.AddDays(-2),
                "Signed up through the website booking widget, not yet contacted.", "org-1", "branch-2"),
            new Customer(
                "customer-7", "Grace Kim", "Kim Aesthetics", "grace.kim@example.com", "+1 (555) 010-6604",
                CustomerStatus.Inactive, "$2,150", now.AddMonths(-2),
                "Paused after relocating; may return once settled.", "org-2", "branch-3"),
        ];

        _notes =
        [
            new CustomerNote("note-1", "customer-1", "Allergic to certain hair dyes - always check before a colour service.", now.AddDays(-30)),
            new CustomerNote("note-2", "customer-1", "Requested an extra 15 minutes for consultations going forward.", now.AddDays(-5)),
            new CustomerNote("note-3", "customer-3", "Prefers text reminders over email.", now.AddDays(-10)),
            new CustomerNote("note-4", "customer-5", "Primary contact for the corporate account is her assistant, Maria.", now.AddDays(-20)),
        ];

        _tags =
        [
            new CustomerTag("tag-1", "customer-1", "Regular", now.AddDays(-60)),
            new CustomerTag("tag-2", "customer-1", "Colour Specialist", now.AddDays(-60)),
            new CustomerTag("tag-3", "customer-3", "VIP", now.AddDays(-45)),
            new CustomerTag("tag-4", "customer-3", "Monthly Package", now.AddDays(-45)),
            new CustomerTag("tag-5", "customer-5", "Corporate", now.AddDays(-40)),
            new CustomerTag("tag-6", "customer-4", "Win-back Target", now.AddMonths(-4)),
            new CustomerTag("tag-7", "customer-2", "Referral", now.AddDays(-3)),
        ];

        _activity =
        [
            new CustomerActivity("activity-1", "customer-1", "Customer created", now.AddDays(-60)),
            new CustomerActivity("activity-2", "customer-1", "Booking completed", now.AddDays(-30)),
            new CustomerActivity("activity-3", "customer-1", "Note added", now.AddDays(-30)),
            new CustomerActivity("activity-4", "customer-1", "Payment received", now.AddDays(-5)),
            new CustomerActivity("activity-5", "customer-1", "Booking completed", now.AddDays(-1)),

            new CustomerActivity("activity-6", "customer-2", "Customer created", now.AddDays(-3)),
            new CustomerActivity("activity-7", "customer-2", "Referral received from Amelia Hart", now.AddDays(-3)),

            new CustomerActivity("activity-8", "customer-3", "Customer created", now.AddDays(-90)),
            new CustomerActivity("activity-9", "customer-3", "Upgraded to VIP tier", now.AddDays(-45)),
            new CustomerActivity("activity-10", "customer-3", "Booking completed", now.AddDays(-10)),
            new CustomerActivity("activity-11", "customer-3", "Booking completed", now.AddHours(-6)),

            new CustomerActivity("activity-12", "customer-4", "Customer created", now.AddMonths(-6)),
            new CustomerActivity("activity-13", "customer-4", "Booking completed", now.AddMonths(-4)),
            new CustomerActivity("activity-14", "customer-4", "Marked as churned - flagged for re-engagement", now.AddMonths(-4)),

            new CustomerActivity("activity-15", "customer-5", "Customer created", now.AddDays(-40)),
            new CustomerActivity("activity-16", "customer-5", "Corporate account booking created", now.AddDays(-9)),

            new CustomerActivity("activity-17", "customer-6", "Customer created", now.AddDays(-2)),
            new CustomerActivity("activity-18", "customer-6", "Signed up via website booking widget", now.AddDays(-2)),

            new CustomerActivity("activity-19", "customer-7", "Customer created", now.AddMonths(-5)),
            new CustomerActivity("activity-20", "customer-7", "Booking completed", now.AddMonths(-2)),
            new CustomerActivity("activity-21", "customer-7", "Marked inactive after relocation", now.AddMonths(-2)),
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
