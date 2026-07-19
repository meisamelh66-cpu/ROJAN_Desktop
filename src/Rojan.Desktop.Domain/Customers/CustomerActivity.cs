namespace Rojan.Desktop.Domain.Customers;

/// <summary>A single timeline event for a customer (created, updated, note/tag changes), as returned by <see cref="ICustomerRepository"/>.</summary>
public sealed record CustomerActivity(string Id, string CustomerId, string Description, DateTimeOffset OccurredAt);
