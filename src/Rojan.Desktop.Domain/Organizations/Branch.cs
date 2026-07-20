namespace Rojan.Desktop.Domain.Organizations;

/// <summary>
/// One physical/operating location under an <see cref="Organization"/>.
/// <see cref="Manager"/> is a plain display name, not an
/// <c>HR.Employee</c> reference - Domain modules never reference each
/// other (the same isolation every other module in this app already
/// follows), so Organizations does not depend on HR's Domain types.
/// </summary>
public sealed record Branch(
    string Id,
    string OrganizationId,
    string Name,
    string Code,
    string Address,
    string Phone,
    string Email,
    string Manager,
    string TimeZone,
    string Currency,
    BranchStatus Status);
