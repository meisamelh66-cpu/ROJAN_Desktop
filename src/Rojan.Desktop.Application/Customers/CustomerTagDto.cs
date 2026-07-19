namespace Rojan.Desktop.Application.Customers;

/// <summary>Application-layer shape of a customer tag, mapped from <see cref="Rojan.Desktop.Domain.Customers.CustomerTag"/>.</summary>
public sealed record CustomerTagDto(string Id, string CustomerId, string Label, DateTimeOffset CreatedAt);
