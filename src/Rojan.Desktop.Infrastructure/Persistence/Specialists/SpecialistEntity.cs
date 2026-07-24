using DomainSpecialists = Rojan.Desktop.Domain.Specialists;

namespace Rojan.Desktop.Infrastructure.Persistence.Specialists;

/// <summary>
/// EF Core persistence model for a specialist - field-for-field mirror of
/// <see cref="DomainSpecialists.Specialist"/>, kept as its own mutable
/// class rather than mapping the Domain record directly - same
/// "Infrastructure owns the translation, Domain stays
/// persistence-ignorant" reasoning <see cref="Customers.CustomerEntity"/>
/// already establishes for the Customers vertical slice.
/// <see cref="DomainSpecialists.Specialist"/> has no OrganizationId/BranchId
/// (unlike <c>Customers.Customer</c>) - Specialists is not
/// Organization/Branch-scoped, so this entity has no such columns either;
/// see <see cref="EfSpecialistRepository"/>'s own doc comment.
/// </summary>
public sealed class SpecialistEntity
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DomainSpecialists.SpecialistStatus Status { get; set; }

    public string Bio { get; set; } = string.Empty;
}
