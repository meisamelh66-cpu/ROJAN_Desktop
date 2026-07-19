namespace Rojan.Desktop.Domain.Services;

/// <summary>Catalog availability of a service, as returned by <see cref="IServiceRepository"/>.</summary>
public enum ServiceStatus
{
    Active,
    Seasonal,
    Discontinued,
}
