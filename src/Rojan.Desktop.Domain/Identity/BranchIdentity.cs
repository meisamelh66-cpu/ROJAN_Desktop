namespace Rojan.Desktop.Domain.Identity;

/// <summary>Phase 25: Enterprise Identity Foundation. The immutable identity of a <see cref="Organizations.Branch"/>, scoped to its owning organization - see <see cref="OrganizationIdentity"/>'s own doc comment for why this stays minimal rather than duplicating the full aggregate.</summary>
public sealed record BranchIdentity(string Id, string OrganizationId, string Name);
