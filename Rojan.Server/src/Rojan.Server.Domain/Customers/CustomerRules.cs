namespace Rojan.Server.Domain.Customers;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. Validation rules for
/// <see cref="Customer"/> - same "small deviation from data-plus-
/// repository-only Domain" reasoning <c>Domain.Authentication.UserRules</c>
/// already establishes.
///
/// <see cref="IsValidEmail"/> deliberately duplicates
/// <c>Domain.Authentication.UserRules.IsValidEmail</c>'s exact logic
/// rather than referencing it - this module must not depend on
/// <c>Domain.Authentication</c> (or any other Domain module) at all, the
/// same vertical-slice-independence rule the desktop solution's own
/// Domain modules already follow. Three duplicated lines is the cost of
/// that independence, not an oversight.
/// </summary>
public static class CustomerRules
{
    public static bool IsValidName(string name) => !string.IsNullOrWhiteSpace(name);

    public static bool IsValidPhone(string phone) => !string.IsNullOrWhiteSpace(phone);

    /// <summary>A minimal format check - not full RFC 5322 validation (out of scope for a foundation commit). <see langword="null"/>/empty is always valid - email is optional (see <see cref="Customer.Email"/>).</summary>
    public static bool IsValidEmail(string? email) =>
        email is null
        || (!string.IsNullOrWhiteSpace(email)
            && email.Count(character => character == '@') == 1
            && !email.StartsWith('@')
            && !email.EndsWith('@')
            && email.Contains('.', StringComparison.Ordinal));

    private static readonly Dictionary<CustomerStatus, CustomerStatus[]> ValidTransitions = new()
    {
        [CustomerStatus.Active] = [CustomerStatus.Inactive],
        [CustomerStatus.Inactive] = [CustomerStatus.Active],
    };

    /// <summary>Whether moving a customer directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>).</summary>
    public static bool IsValidTransition(CustomerStatus from, CustomerStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
