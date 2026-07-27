namespace Rojan.Server.Application.Specialists;

/// <summary>
/// Thrown by <c>ISpecialistService.GetSpecialistAsync</c>/<c>UpdateSpecialistAsync</c>
/// when the requested specialist does not exist within the caller's own
/// organization - either because it genuinely does not exist, or because
/// it belongs to a different tenant. Both cases throw this exact same
/// exception, deliberately, same "do not leak" reasoning
/// <c>Application.Customers.CustomerNotFoundException</c> already
/// establishes: <c>Domain.Specialists.ISpecialistRepository.GetByIdAsync</c>
/// itself cannot distinguish the two, and
/// <c>Api.Controllers.SpecialistsController</c> maps this to a plain 404 -
/// an API response must never confirm that another tenant's specialist
/// exists.
/// </summary>
public sealed class SpecialistNotFoundException(string specialistId) : Exception($"Specialist '{specialistId}' was not found.");
