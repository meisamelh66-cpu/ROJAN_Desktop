namespace Rojan.Desktop.Domain.Salons;

/// <summary>
/// Phase 1.2 Owner App Create Salon Flow: a single salon record, as
/// returned by <see cref="ISalonRepository"/>. <see cref="Id"/> is a
/// throwaway/placeholder value on the object passed into
/// <see cref="ISalonRepository.CreateAsync"/> - the repository returns the
/// real, backend-assigned one, same "pass a full domain object in, get the
/// server-authoritative one back" convention every other Create flow in
/// this app already uses (e.g. <c>Customers.ICustomerRepository.CreateCustomerAsync</c>).
/// </summary>
public sealed record Salon(
    string Id,
    string Name,
    string? Description,
    string Phone,
    string? Email,
    string Address,
    bool Active);
