namespace Rojan.Server.Domain.Authentication;

/// <summary>Persistence contract for <see cref="Organization"/> - the concrete implementation (<c>Infrastructure.Persistence.Repositories.EfOrganizationRepository</c>) is EF Core/PostgreSQL, but nothing in this layer or <c>Application</c> needs to know that.</summary>
public interface IOrganizationRepository
{
    public Task<Organization> CreateAsync(Organization organization, CancellationToken cancellationToken = default);

    public Task<Organization?> GetByIdAsync(string organizationId, CancellationToken cancellationToken = default);
}
