namespace Rojan.Desktop.Application.Customers;

/// <summary>Application-layer shape of a customer timeline event, mapped from <see cref="Rojan.Desktop.Domain.Customers.CustomerActivity"/>.</summary>
public sealed record CustomerActivityDto(string Id, string CustomerId, string Description, DateTimeOffset OccurredAt);
