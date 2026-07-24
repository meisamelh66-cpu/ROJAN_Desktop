using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>
/// EF Core persistence model for a catalog service - field-for-field
/// mirror of <see cref="DomainServices.Service"/>, kept as its own mutable
/// class rather than mapping the Domain record directly - same
/// "Infrastructure owns the translation, Domain stays
/// persistence-ignorant" reasoning <see cref="Customers.CustomerEntity"/>/
/// <see cref="Specialists.SpecialistEntity"/> already establish. Like
/// <c>Domain.Specialists.Specialist</c> in the previous commit,
/// <see cref="DomainServices.Service"/> has no OrganizationId/BranchId -
/// Services is not Organization/Branch-scoped either (confirmed by reading
/// <c>Application.Services.ServiceQueryService</c>/<c>ServiceCommandService</c>,
/// neither of which reference <c>IEnterpriseContext</c>).
/// </summary>
public sealed class ServiceEntity
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public DomainServices.ServiceCategory Category { get; set; }

    public DomainServices.ServiceStatus Status { get; set; }

    public int DurationMinutes { get; set; }

    public string Price { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
}
