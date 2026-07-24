namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core persistence model for a customer timeline event - field-for-field mirror of <see cref="Domain.Customers.CustomerActivity"/>, see <see cref="CustomerEntity"/>'s own doc comment for why this is a separate mutable class rather than mapping the Domain record directly.</summary>
public sealed class CustomerActivityEntity
{
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTimeOffset OccurredAt { get; set; }
}
