using Rojan.Desktop.Application.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerCommandService"/> implementation. Every
/// mutation also records a <see cref="DomainCustomers.CustomerActivity"/>
/// entry, so the profile's Timeline reflects real actions taken in the
/// running app rather than only the seeded demo data - a business rule
/// (what counts as timeline-worthy) that belongs here, not hidden as a
/// side effect inside the fake repository.
///
/// Phase 22A: <see cref="CreateCustomerAsync"/> stamps the new customer
/// with the current session's organization/branch
/// (<see cref="IEnterpriseContext"/>) - never a hardcoded id.
/// <see cref="UpdateCustomerAsync"/> deliberately preserves the existing
/// record's organization/branch rather than re-stamping from the current
/// session - editing a customer must never silently move it to whichever
/// branch happens to be active, that would need its own explicit
/// "transfer" operation, out of scope here.
/// </summary>
public sealed class CustomerCommandService : ICustomerCommandService
{
    private readonly DomainCustomers.ICustomerRepository _repository;
    private readonly IEnterpriseContext _enterpriseContext;

    public CustomerCommandService(DomainCustomers.ICustomerRepository repository, IEnterpriseContext enterpriseContext)
    {
        _repository = repository;
        _enterpriseContext = enterpriseContext;
    }

    public async Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new DomainCustomers.Customer(
            Guid.NewGuid().ToString(),
            request.FullName,
            request.Company,
            request.Email,
            request.Phone,
            DomainCustomers.CustomerStatus.Lead,
            "$0",
            DateTimeOffset.Now,
            request.Notes,
            _enterpriseContext.CurrentOrganizationId ?? string.Empty,
            _enterpriseContext.CurrentBranchId ?? string.Empty);

        var created = await _repository.CreateCustomerAsync(customer, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(created.Id, "Customer created", cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapCustomer(created);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetCustomerByIdAsync(request.Id, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Customer '{request.Id}' was not found.");

        var customer = new DomainCustomers.Customer(
            request.Id,
            request.FullName,
            request.Company,
            request.Email,
            request.Phone,
            CustomerMapper.MapStatusToDomain(request.Status),
            request.LifetimeValue,
            DateTimeOffset.Now,
            request.Notes,
            existing.OrganizationId,
            existing.BranchId);

        var updated = await _repository.UpdateCustomerAsync(customer, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(updated.Id, "Customer profile updated", cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapCustomer(updated);
    }

    public async Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default)
    {
        var note = new DomainCustomers.CustomerNote(Guid.NewGuid().ToString(), customerId, text, DateTimeOffset.Now);
        var added = await _repository.AddNoteAsync(note, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(customerId, "Note added", cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapNote(added);
    }

    public async Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default)
    {
        var tag = new DomainCustomers.CustomerTag(Guid.NewGuid().ToString(), customerId, label, DateTimeOffset.Now);
        var added = await _repository.AddTagAsync(tag, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(customerId, $"Tag added: {label}", cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapTag(added);
    }

    public async Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        await _repository.RemoveTagAsync(customerId, tagId, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(customerId, "Tag removed", cancellationToken).ConfigureAwait(true);
    }

    private Task<DomainCustomers.CustomerActivity> LogActivityAsync(string customerId, string description, CancellationToken cancellationToken) =>
        _repository.AddActivityAsync(
            new DomainCustomers.CustomerActivity(Guid.NewGuid().ToString(), customerId, description, DateTimeOffset.Now),
            cancellationToken);
}
