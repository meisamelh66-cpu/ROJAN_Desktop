using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerCommandService"/> implementation. Every
/// mutation also records a <see cref="DomainCustomers.CustomerActivity"/>
/// entry, so the profile's Timeline reflects real actions taken in the
/// running app rather than only the seeded demo data - a business rule
/// (what counts as timeline-worthy) that belongs here, not hidden as a
/// side effect inside the fake repository.
/// </summary>
public sealed class CustomerCommandService : ICustomerCommandService
{
    private readonly DomainCustomers.ICustomerRepository _repository;

    public CustomerCommandService(DomainCustomers.ICustomerRepository repository)
    {
        _repository = repository;
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
            request.Notes);

        var created = await _repository.CreateCustomerAsync(customer, cancellationToken).ConfigureAwait(true);
        await LogActivityAsync(created.Id, "Customer created", cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapCustomer(created);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var customer = new DomainCustomers.Customer(
            request.Id,
            request.FullName,
            request.Company,
            request.Email,
            request.Phone,
            CustomerMapper.MapStatusToDomain(request.Status),
            request.LifetimeValue,
            DateTimeOffset.Now,
            request.Notes);

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
