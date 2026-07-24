using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>Domain&lt;-&gt;persistence-entity mapping for the Customers vertical slice - internal, only <see cref="EfCustomerRepository"/> calls it, same convention as every Domain&lt;-&gt;Application mapper in this codebase (<c>Application.Customers.CustomerMapper</c>).</summary>
internal static class CustomerEntityMapper
{
    public static DomainCustomers.Customer MapToDomain(CustomerEntity entity) => new(
        entity.Id,
        entity.FullName,
        entity.Company,
        entity.Email,
        entity.Phone,
        entity.Status,
        entity.LifetimeValue,
        entity.LastContactedAt,
        entity.Notes,
        entity.OrganizationId,
        entity.BranchId);

    public static CustomerEntity MapToEntity(DomainCustomers.Customer customer) => new()
    {
        Id = customer.Id,
        FullName = customer.FullName,
        Company = customer.Company,
        Email = customer.Email,
        Phone = customer.Phone,
        Status = customer.Status,
        LifetimeValue = customer.LifetimeValue,
        LastContactedAt = customer.LastContactedAt,
        Notes = customer.Notes,
        OrganizationId = customer.OrganizationId,
        BranchId = customer.BranchId,
    };

    /// <summary>
    /// Applies every field from <paramref name="customer"/> onto the
    /// already-tracked <paramref name="entity"/> - a full replace, matching
    /// <c>FakeCustomerRepository.UpdateCustomerAsync</c>'s own
    /// <c>_customers[index] = customer</c> semantics exactly, including
    /// <see cref="DomainCustomers.Customer.OrganizationId"/>/
    /// <see cref="DomainCustomers.Customer.BranchId"/>. The "never silently
    /// move branch on edit" rule already lives in
    /// <c>Application.Customers.CustomerCommandService.UpdateCustomerAsync</c>
    /// (it re-supplies the existing org/branch before calling this
    /// repository at all) - duplicating that guard here would be a second,
    /// competing copy of the same business rule in the wrong layer.
    /// </summary>
    public static void ApplyTo(CustomerEntity entity, DomainCustomers.Customer customer)
    {
        entity.FullName = customer.FullName;
        entity.Company = customer.Company;
        entity.Email = customer.Email;
        entity.Phone = customer.Phone;
        entity.Status = customer.Status;
        entity.LifetimeValue = customer.LifetimeValue;
        entity.LastContactedAt = customer.LastContactedAt;
        entity.Notes = customer.Notes;
        entity.OrganizationId = customer.OrganizationId;
        entity.BranchId = customer.BranchId;
    }

    public static DomainCustomers.CustomerNote MapToDomain(CustomerNoteEntity entity) =>
        new(entity.Id, entity.CustomerId, entity.Text, entity.CreatedAt);

    public static CustomerNoteEntity MapToEntity(DomainCustomers.CustomerNote note) => new()
    {
        Id = note.Id,
        CustomerId = note.CustomerId,
        Text = note.Text,
        CreatedAt = note.CreatedAt,
    };

    public static DomainCustomers.CustomerTag MapToDomain(CustomerTagEntity entity) =>
        new(entity.Id, entity.CustomerId, entity.Label, entity.CreatedAt);

    public static CustomerTagEntity MapToEntity(DomainCustomers.CustomerTag tag) => new()
    {
        Id = tag.Id,
        CustomerId = tag.CustomerId,
        Label = tag.Label,
        CreatedAt = tag.CreatedAt,
    };

    public static DomainCustomers.CustomerActivity MapToDomain(CustomerActivityEntity entity) =>
        new(entity.Id, entity.CustomerId, entity.Description, entity.OccurredAt);

    public static CustomerActivityEntity MapToEntity(DomainCustomers.CustomerActivity activity) => new()
    {
        Id = activity.Id,
        CustomerId = activity.CustomerId,
        Description = activity.Description,
        OccurredAt = activity.OccurredAt,
    };
}
