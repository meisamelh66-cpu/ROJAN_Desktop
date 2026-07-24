namespace Rojan.Server.Domain.Customers;

/// <summary>
/// Sprint 8 Commit 4: Tenant-Aware Customer API. The first business
/// module on this backend - deliberately in its own
/// <c>Rojan.Server.Domain.Customers</c> namespace, not
/// <c>Domain.Authentication</c>, matching the desktop solution's own
/// per-module Domain separation (and the same vertical-slice-independence
/// reasoning: this module never references <c>Domain.Authentication</c>
/// types directly - see <see cref="CustomerRules"/>'s own doc comment for
/// why its email check duplicates
/// <c>Domain.Authentication.UserRules.IsValidEmail</c> rather than
/// depending on it).
///
/// <see cref="OrganizationId"/> is required - every customer belongs to
/// exactly one tenant, enforced by <see cref="ICustomerRepository"/>'s own
/// contract (every read is scoped by organization, not just written by
/// convention - see its own doc comment) rather than left to callers to
/// remember. <see cref="BranchId"/> is optional, the same "may belong to
/// the organization only, with no specific branch" shape
/// <c>Domain.Authentication.User.BranchId</c> already establishes -
/// consistency between the two (a customer's branch must belong to the
/// customer's own organization) is validated by
/// <c>Application.Customers.CustomerService</c>, which is allowed to
/// coordinate across both modules; Domain itself is not (see this
/// record's own namespace note above).
/// </summary>
public sealed record Customer(
    string Id,
    string OrganizationId,
    string? BranchId,
    string Name,
    string Phone,
    string? Email,
    CustomerStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public bool IsActive => Status == CustomerStatus.Active;
}
