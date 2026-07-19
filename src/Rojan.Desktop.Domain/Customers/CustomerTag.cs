namespace Rojan.Desktop.Domain.Customers;

/// <summary>A single label attached to a customer, as returned by <see cref="ICustomerRepository"/>.</summary>
public sealed record CustomerTag(string Id, string CustomerId, string Label, DateTimeOffset CreatedAt);
