using Rojan.Desktop.Application.Organizations;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerCommandService"/> implementation.
///
/// Customer CRM Integration Preparation: this service no longer calls
/// <see cref="DomainCustomers.ICustomerRepository.AddActivityAsync"/> for
/// any mutation. The backend Customer CRM API is the source of truth for
/// the activity timeline and already logs tag add/remove and status
/// changes itself as a side effect of its own use cases, and a note's
/// appearance in the merged timeline makes a separate "Note added" entry
/// redundant; there is no backend equivalent at all for a generic
/// "Customer created"/"Customer profile updated" entry. Logging those
/// here as well would double the entries once <c>BackendCustomerRepository</c>
/// is wired in. See <c>ROJAN_Owner_App_Customer_CRM_Integration_Plan_v1.md</c>
/// §3.3/§5.
///
/// Phase 22A: <see cref="CreateCustomerAsync"/> stamps the new customer
/// with the current session's organization/branch
/// (<see cref="IEnterpriseContext"/>) - never a hardcoded id.
/// <see cref="UpdateCustomerAsync"/> deliberately preserves the existing
/// record's organization/branch rather than re-stamping from the current
/// session - editing a customer must never silently move it to whichever
/// branch happens to be active, that would need its own explicit
/// "transfer" operation, out of scope here.
/// Sprint 4 Commit 1: <see cref="UpdateCustomerAsync"/> enforces
/// <see cref="DomainCustomers.CustomerRules"/> only when the request
/// actually changes status - <c>UpdateCustomerAsync</c> is a full-field
/// replacement (name/company/email/phone/status/lifetime value/notes all
/// in one call, unlike Bookings' dedicated status-only command), so most
/// calls carry the customer's current, unchanged status through
/// unexamined; only a real status change is validated as a transition.
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
            "0 تومان",
            DateTimeOffset.Now,
            request.Notes,
            _enterpriseContext.CurrentOrganizationId ?? string.Empty,
            _enterpriseContext.CurrentBranchId ?? string.Empty);

        var created = await _repository.CreateCustomerAsync(customer, cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapCustomer(created);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await _repository.GetCustomerByIdAsync(request.Id, cancellationToken).ConfigureAwait(true)
            ?? throw new InvalidOperationException($"Customer '{request.Id}' was not found.");

        var newStatus = CustomerMapper.MapStatusToDomain(request.Status);
        if (newStatus != existing.Status && !DomainCustomers.CustomerRules.IsValidTransition(existing.Status, newStatus))
        {
            throw new InvalidOperationException($"Cannot transition customer from {existing.Status} to {newStatus}.");
        }

        var customer = new DomainCustomers.Customer(
            request.Id,
            request.FullName,
            request.Company,
            request.Email,
            request.Phone,
            newStatus,
            request.LifetimeValue,
            DateTimeOffset.Now,
            request.Notes,
            existing.OrganizationId,
            existing.BranchId);

        var updated = await _repository.UpdateCustomerAsync(customer, cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapCustomer(updated);
    }

    public async Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default)
    {
        var note = new DomainCustomers.CustomerNote(Guid.NewGuid().ToString(), customerId, text, DateTimeOffset.Now);
        var added = await _repository.AddNoteAsync(note, cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapNote(added);
    }

    public async Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default)
    {
        var tag = new DomainCustomers.CustomerTag(Guid.NewGuid().ToString(), customerId, label, DateTimeOffset.Now);
        var added = await _repository.AddTagAsync(tag, cancellationToken).ConfigureAwait(true);

        return CustomerMapper.MapTag(added);
    }

    public async Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        await _repository.RemoveTagAsync(customerId, tagId, cancellationToken).ConfigureAwait(true);
    }
}
