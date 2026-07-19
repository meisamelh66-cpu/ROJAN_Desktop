namespace Rojan.Desktop.Domain.Specialists;

/// <summary>Employment/availability status of a specialist, as returned by <see cref="ISpecialistRepository"/>.</summary>
public enum SpecialistStatus
{
    Active,
    OnLeave,
    Inactive,
}
