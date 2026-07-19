namespace Rojan.Desktop.Domain.Customers;

/// <summary>A free-text note attached to a customer, as returned by <see cref="ICustomerRepository"/>.</summary>
public sealed record CustomerNote(string Id, string CustomerId, string Text, DateTimeOffset CreatedAt);
