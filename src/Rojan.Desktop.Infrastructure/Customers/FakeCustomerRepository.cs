using Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Customers;

/// <summary>
/// In-memory <see cref="ICustomerRepository"/> providing static sample
/// data - Phase 09 explicitly has no backend integration yet, same as the
/// Dashboard vertical slice in Phase 06B. The small artificial delay is
/// deliberate: without it, every load resolves within the same UI frame
/// and the ViewModel's <c>Loading</c> state would never actually be
/// observable, which would leave that state effectively untested by simply
/// running the app.
/// </summary>
public sealed class FakeCustomerRepository : ICustomerRepository
{
    private static readonly IReadOnlyList<Customer> Customers =
    [
        new Customer(
            "customer-1",
            "Amelia Hart",
            "Hart & Co. Salon",
            "amelia.hart@example.com",
            "+1 (555) 010-2231",
            CustomerStatus.Active,
            "$4,820",
            DateTimeOffset.Now.AddDays(-1),
            "Prefers evening appointments. Regular colour touch-up client."),
        new Customer(
            "customer-2",
            "Noah Bennett",
            string.Empty,
            "noah.bennett@example.com",
            "+1 (555) 010-7742",
            CustomerStatus.Lead,
            "$0",
            DateTimeOffset.Now.AddDays(-3),
            "Referred by Amelia Hart. Interested in a first-time consultation."),
        new Customer(
            "customer-3",
            "Sophia Reyes",
            "Reyes Beauty Studio",
            "sophia.reyes@example.com",
            "+1 (555) 010-5518",
            CustomerStatus.Active,
            "$12,150",
            DateTimeOffset.Now.AddHours(-6),
            "VIP tier. Books the full package monthly."),
        new Customer(
            "customer-4",
            "Liam Foster",
            string.Empty,
            "liam.foster@example.com",
            "+1 (555) 010-3390",
            CustomerStatus.Inactive,
            "$610",
            DateTimeOffset.Now.AddMonths(-4),
            "No bookings in over 90 days. Flagged for a re-engagement offer."),
        new Customer(
            "customer-5",
            "Olivia Chen",
            "Chen Wellness Group",
            "olivia.chen@example.com",
            "+1 (555) 010-9027",
            CustomerStatus.Active,
            "$7,340",
            DateTimeOffset.Now.AddDays(-9),
            "Corporate account - books for a team of six."),
        new Customer(
            "customer-6",
            "Ethan Brooks",
            string.Empty,
            "ethan.brooks@example.com",
            "+1 (555) 010-4465",
            CustomerStatus.Lead,
            "$0",
            DateTimeOffset.Now.AddDays(-2),
            "Signed up through the website booking widget, not yet contacted."),
    ];

    public async Task<IReadOnlyList<Customer>> GetCustomersAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return Customers;
    }
}
