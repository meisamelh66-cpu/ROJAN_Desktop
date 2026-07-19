using System.Globalization;
using DomainCustomers = Rojan.Desktop.Domain.Customers;

namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Default <see cref="ICustomerProfileQueryService"/> implementation -
/// fetches the customer plus its notes/tags/activity from
/// <see cref="DomainCustomers.ICustomerRepository"/> and assembles the
/// aggregate <see cref="CustomerProfileDto"/>, including the statistics
/// cards (computed here, not stored - a business rule about what a
/// profile's key numbers are, which belongs in Application, not Domain
/// or Infrastructure).
/// </summary>
public sealed class CustomerProfileQueryService : ICustomerProfileQueryService
{
    private readonly DomainCustomers.ICustomerRepository _repository;

    public CustomerProfileQueryService(DomainCustomers.ICustomerRepository repository)
    {
        _repository = repository;
    }

    public async Task<CustomerProfileDto> GetProfileAsync(string customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _repository.GetCustomerByIdAsync(customerId, cancellationToken).ConfigureAwait(true);
        if (customer is null)
        {
            throw new InvalidOperationException($"Customer '{customerId}' was not found.");
        }

        var notes = await _repository.GetNotesAsync(customerId, cancellationToken).ConfigureAwait(true);
        var tags = await _repository.GetTagsAsync(customerId, cancellationToken).ConfigureAwait(true);
        var activity = await _repository.GetActivityAsync(customerId, cancellationToken).ConfigureAwait(true);

        var customerDto = CustomerMapper.MapCustomer(customer);

        return new CustomerProfileDto(
            customerDto,
            notes.Select(CustomerMapper.MapNote).ToList(),
            tags.Select(CustomerMapper.MapTag).ToList(),
            activity.Select(CustomerMapper.MapActivity).ToList(),
            BuildStatistics(customerDto, notes.Count, tags.Count));
    }

    private static IReadOnlyList<CustomerStatDto> BuildStatistics(CustomerDto customer, int noteCount, int tagCount) =>
    [
        new("Lifetime Value", customer.LifetimeValue),
        new("Status", customer.Status.ToString()),
        new("Notes", noteCount.ToString(CultureInfo.InvariantCulture)),
        new("Tags", tagCount.ToString(CultureInfo.InvariantCulture)),
        new("Last Contacted", customer.LastContactedAt.ToString("MMM d, yyyy", CultureInfo.InvariantCulture)),
    ];
}
