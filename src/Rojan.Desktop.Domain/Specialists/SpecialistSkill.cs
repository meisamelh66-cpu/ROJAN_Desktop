namespace Rojan.Desktop.Domain.Specialists;

/// <summary>A single skill/specialty attached to a specialist, as returned by <see cref="ISpecialistRepository"/>.</summary>
public sealed record SpecialistSkill(string Id, string SpecialistId, string Name);
