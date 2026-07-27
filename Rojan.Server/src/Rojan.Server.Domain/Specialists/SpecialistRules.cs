namespace Rojan.Server.Domain.Specialists;

/// <summary>
/// Sprint 8 Commit 5: Tenant-Aware Specialist API. Validation rules for
/// <see cref="Specialist"/> - same "small deviation from data-plus-
/// repository-only Domain" reasoning <c>Domain.Customers.CustomerRules</c>
/// already establishes.
///
/// <see cref="IsValidEmail"/> deliberately duplicates
/// <c>Domain.Customers.CustomerRules.IsValidEmail</c>'s exact logic rather
/// than referencing it - this module must not depend on
/// <c>Domain.Customers</c> (or any other Domain module) at all, the same
/// vertical-slice-independence rule <c>CustomerRules</c>' own doc comment
/// already establishes. Three duplicated lines is the cost of that
/// independence, not an oversight.
/// </summary>
public static class SpecialistRules
{
    public static bool IsValidName(string fullName) => !string.IsNullOrWhiteSpace(fullName);

    public static bool IsValidPhone(string phone) => !string.IsNullOrWhiteSpace(phone);

    /// <summary>A minimal format check - not full RFC 5322 validation (out of scope for a foundation commit). <see langword="null"/>/empty is always valid - email is optional (see <see cref="Specialist.Email"/>).</summary>
    public static bool IsValidEmail(string? email) =>
        email is null
        || (!string.IsNullOrWhiteSpace(email)
            && email.Count(character => character == '@') == 1
            && !email.StartsWith('@')
            && !email.EndsWith('@')
            && email.Contains('.', StringComparison.Ordinal));

    private static readonly Dictionary<SpecialistStatus, SpecialistStatus[]> ValidTransitions = new()
    {
        [SpecialistStatus.Active] = [SpecialistStatus.Inactive],
        [SpecialistStatus.Inactive] = [SpecialistStatus.Active],
    };

    /// <summary>Whether moving a specialist directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>).</summary>
    public static bool IsValidTransition(SpecialistStatus from, SpecialistStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);
}
