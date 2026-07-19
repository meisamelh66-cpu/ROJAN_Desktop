namespace Rojan.Desktop.Application.Customers;

/// <summary>Application-layer shape of a customer note, mapped from <see cref="Rojan.Desktop.Domain.Customers.CustomerNote"/>.</summary>
public sealed record CustomerNoteDto(string Id, string CustomerId, string Text, DateTimeOffset CreatedAt);
