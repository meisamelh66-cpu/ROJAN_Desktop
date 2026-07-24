namespace Rojan.Server.Application.Tenancy;

/// <summary>Sprint 8 Commit 3: Multi-Tenant Organization Foundation. One entry of <c>ITenantService.GetCurrentOrganizationBranchesAsync</c>'s result - deliberately minimal (no status, no organization id repeated on every row - the caller already knows which organization it asked about).</summary>
public sealed record BranchSummaryDto(string Id, string Name);
