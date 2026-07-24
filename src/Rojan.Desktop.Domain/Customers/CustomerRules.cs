namespace Rojan.Desktop.Domain.Customers;

/// <summary>
/// Validation rules for the customer relationship lifecycle - a deliberate
/// deviation from this codebase's usual "Domain is just data + repository
/// contract" minimalism, same reasoning <c>Bookings.BookingRules</c>
/// already established: now that <c>Application.Customers.CustomerCommandService</c>
/// accepts a caller-supplied status on every update, an illegal jump (e.g.
/// straight from Lead to Vip with no relationship in between) must be
/// rejected rather than silently written.
///
/// Unlike <see cref="Bookings.BookingRules"/>, no status here is fully
/// terminal - a customer relationship is ongoing, not a one-shot event
/// like a booking. Every status has at least one way out, including
/// <see cref="CustomerStatus.Churned"/> -&gt; <see cref="CustomerStatus.Lead"/>,
/// modeling a win-back campaign re-engaging a former customer as a fresh
/// lead rather than permanently closing the book on them.
/// </summary>
public static class CustomerRules
{
    private static readonly Dictionary<CustomerStatus, CustomerStatus[]> ValidTransitions = new()
    {
        [CustomerStatus.Lead] = [CustomerStatus.Prospect, CustomerStatus.Active, CustomerStatus.Churned],
        [CustomerStatus.Prospect] = [CustomerStatus.Active, CustomerStatus.Churned],
        [CustomerStatus.Active] = [CustomerStatus.Vip, CustomerStatus.Inactive, CustomerStatus.Churned],
        [CustomerStatus.Vip] = [CustomerStatus.Active, CustomerStatus.Inactive, CustomerStatus.Churned],
        [CustomerStatus.Inactive] = [CustomerStatus.Active, CustomerStatus.Churned],
        [CustomerStatus.Churned] = [CustomerStatus.Lead],
    };

    /// <summary>Whether moving a customer directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>) - editing a customer's other fields without changing status is not a transition at all and must never be rejected here.</summary>
    public static bool IsValidTransition(CustomerStatus from, CustomerStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
