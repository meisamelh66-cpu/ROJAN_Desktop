namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>EF Core persistence model for a specialist-to-service assignment - field-for-field mirror of <see cref="Domain.Services.SpecialistService"/>, see <see cref="ServiceEntity"/>'s own doc comment for why this is a separate mutable class rather than mapping the Domain record directly.</summary>
public sealed class SpecialistServiceEntity
{
    public string Id { get; set; } = string.Empty;

    public string ServiceId { get; set; } = string.Empty;

    public string SpecialistId { get; set; } = string.Empty;

    public string SpecialistName { get; set; } = string.Empty;
}
