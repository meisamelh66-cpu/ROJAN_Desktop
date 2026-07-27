namespace Rojan.Server.Application.Specialists;

/// <summary>Thrown by <c>ISpecialistService.CreateSpecialistAsync</c>/<c>UpdateSpecialistAsync</c> when the request's <c>BranchId</c> does not resolve to a branch within the caller's own organization (does not exist, or belongs to a different tenant) - a client input error, mapped to 400 by <c>Api.Controllers.SpecialistsController</c>, not a tenant-access failure (the caller's own tenant context was fine; what it asked for was not).</summary>
public sealed class InvalidSpecialistBranchException(string branchId) : Exception($"Branch '{branchId}' does not belong to this organization.");
