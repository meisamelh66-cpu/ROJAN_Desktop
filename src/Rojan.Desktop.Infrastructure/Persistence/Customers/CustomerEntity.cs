using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>
/// EF Core persistence model for a customer - field-for-field mirror of
/// <see cref="DomainCustomers.Customer"/>, kept as its own mutable class
/// (rather than mapping the Domain record directly) so Domain never picks
/// up an EF-friendly parameterless constructor or settable properties
/// purely for the ORM's sake. <see cref="CustomerEntityMapper"/> is the
/// only place that translates between the two - same "Infrastructure owns
/// the translation, Domain stays persistence-ignorant" reasoning
/// <c>Application.Customers.CustomerMapper</c> already establishes one
/// layer up (Domain&lt;-&gt;Application). Reuses <see cref="DomainCustomers.CustomerStatus"/>
/// directly rather than inventing a third mirror enum - Infrastructure is
/// already allowed to see Domain types (it implements
/// <see cref="DomainCustomers.ICustomerRepository"/>), unlike Application/
/// Presentation, which is why Application's own status enum exists but
/// Infrastructure never needed one.
/// </summary>
public sealed class CustomerEntity
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Company { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Phone { get; set; } = string.Empty;

    public DomainCustomers.CustomerStatus Status { get; set; }

    public string LifetimeValue { get; set; } = string.Empty;

    public DateTimeOffset LastContactedAt { get; set; }

    public string Notes { get; set; } = string.Empty;

    public string OrganizationId { get; set; } = string.Empty;

    public string BranchId { get; set; } = string.Empty;
}
