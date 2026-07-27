namespace Rojan.Server.Application.Specialists;

/// <summary>Thrown by <c>ISpecialistService.UpdateSpecialistAsync</c> when <c>UpdateSpecialistRequest.Status</c> is not a recognized <c>Domain.Specialists.SpecialistStatus</c> value, or names a transition <c>Domain.Specialists.SpecialistRules.IsValidTransition</c> rejects - a client input error, mapped to 400.</summary>
public sealed class InvalidSpecialistStatusException(string message) : Exception(message);
