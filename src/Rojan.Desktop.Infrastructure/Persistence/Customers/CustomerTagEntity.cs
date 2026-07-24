namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core persistence model for a customer tag - field-for-field mirror of <see cref="Domain.Customers.CustomerTag"/>, see <see cref="CustomerEntity"/>'s own doc comment for why this is a separate mutable class rather than mapping the Domain record directly.</summary>
public sealed class CustomerTagEntity
{
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
