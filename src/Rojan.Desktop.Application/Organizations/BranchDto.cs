namespace Rojan.Desktop.Application.Organizations;

public sealed record BranchDto(
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
