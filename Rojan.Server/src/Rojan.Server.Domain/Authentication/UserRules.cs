namespace Rojan.Server.Domain.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Validation
/// rules for the authentication domain - same reasoning the desktop
/// solution's own <c>Domain.Customers.CustomerRules</c>/<c>Domain.Specialists.SpecialistRules</c>
/// already establish: a deliberate, small deviation from "Domain is just
/// data + repository contract" wherever a caller-supplied value needs a
/// rule an entity record cannot enforce on itself.
/// </summary>
public static class UserRules
{
    /// <summary>A minimal format check - not full RFC 5322 validation (out of scope for a foundation commit), just enough to reject obviously-malformed input before it reaches password hashing/persistence.</summary>
    public static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email)
        && email.Count(character => character == '@') == 1
        && !email.StartsWith('@')
        && !email.EndsWith('@')
        && email.Contains('.', StringComparison.Ordinal);

    /// <summary>
    /// A user's <see cref="Branch"/>, if any, must belong to the same
    /// <see cref="Organization"/> as the user itself - a branch from a
    /// different tenant must never be assignable. <paramref name="branch"/>
    /// being <see langword="null"/> is always valid (branch assignment is
    /// optional - see <see cref="User.BranchId"/>'s own doc comment).
    /// </summary>
    public static bool IsValidBranchAssignment(string userOrganizationId, Branch? branch) =>
        branch is null || branch.OrganizationId == userOrganizationId;

    /// <summary>
    /// Sprint 8 Commit 3: Multi-Tenant Organization Foundation. A user
    /// belongs to exactly one organization for its entire lifetime -
    /// <see cref="User.OrganizationId"/> is already required and set once
    /// at creation (see <c>Application.Authentication.AuthenticationService.RegisterOrganizationOwnerAsync</c>),
    /// so this makes that invariant an explicit, checkable rule rather
    /// than leaving it implicit. This is the core "cross-organization
    /// relationships are prevented" guarantee
    /// <c>Application.Tenancy.ITenantService</c> relies on: a user's
    /// membership can be checked against a claimed tenant without a
    /// database round-trip.
    /// </summary>
    public static bool BelongsToOrganization(User user, string organizationId) =>
        user.OrganizationId == organizationId;
}
