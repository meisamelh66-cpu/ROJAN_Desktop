namespace Rojan.Desktop.Infrastructure.Persistence.Customers;

/// <summary>EF Core persistence model for a customer note - field-for-field mirror of <see cref="Domain.Customers.CustomerNote"/>, see <see cref="CustomerEntity"/>'s own doc comment for why this is a separate mutable class rather than mapping the Domain record directly.</summary>
public sealed class CustomerNoteEntity
{
    public string Id { get; set; } = string.Empty;

    public string CustomerId { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
