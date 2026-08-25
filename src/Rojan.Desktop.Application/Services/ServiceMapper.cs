using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Application.Services;

/// <summary>Domain&lt;-&gt;Application mapping shared by every Services use case - same reasoning as <c>Customers.CustomerMapper</c>.</summary>
internal static class ServiceMapper
{
    public static ServiceDto MapService(DomainServices.Service service) => new(
        service.Id,
        service.Name,
        MapCategory(service.Category),
        MapStatus(service.Status),
        service.DurationMinutes,
        service.Price,
        service.Description,
        service.CategoryName,
        service.CategoryId,
        ServicePriceParser.Parse(service.Price));

    public static AssignedSpecialistDto MapAssignment(DomainServices.SpecialistService assignment) =>
        new(assignment.Id, assignment.ServiceId, assignment.SpecialistId, assignment.SpecialistName);

    /// <summary>Service Catalog Authoring.</summary>
    public static ServiceCategoryOptionDto MapCategoryOption(DomainServices.ServiceCategoryOption category) =>
        new(category.Id, category.Name);

    public static ServiceCategory MapCategory(DomainServices.ServiceCategory category) => category switch
    {
        DomainServices.ServiceCategory.Hair => ServiceCategory.Hair,
        DomainServices.ServiceCategory.Colour => ServiceCategory.Colour,
        DomainServices.ServiceCategory.Nails => ServiceCategory.Nails,
        DomainServices.ServiceCategory.Skin => ServiceCategory.Skin,
        DomainServices.ServiceCategory.Spa => ServiceCategory.Spa,
        DomainServices.ServiceCategory.Consultation => ServiceCategory.Consultation,
        DomainServices.ServiceCategory.Other => ServiceCategory.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown domain service category."),
    };

    public static ServiceStatus MapStatus(DomainServices.ServiceStatus status) => status switch
    {
        DomainServices.ServiceStatus.Active => ServiceStatus.Active,
        DomainServices.ServiceStatus.Seasonal => ServiceStatus.Seasonal,
        DomainServices.ServiceStatus.Discontinued => ServiceStatus.Discontinued,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain service status."),
    };

    /// <summary>Application -&gt; Domain direction, needed by <see cref="ServiceQueryService.SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/> to compare a caller-supplied <see cref="ServiceCategory"/> filter value against Domain data - same reasoning as <c>Customers.CustomerMapper.MapStatusToDomain</c>.</summary>
    public static DomainServices.ServiceCategory MapCategoryToDomain(ServiceCategory category) => category switch
    {
        ServiceCategory.Hair => DomainServices.ServiceCategory.Hair,
        ServiceCategory.Colour => DomainServices.ServiceCategory.Colour,
        ServiceCategory.Nails => DomainServices.ServiceCategory.Nails,
        ServiceCategory.Skin => DomainServices.ServiceCategory.Skin,
        ServiceCategory.Spa => DomainServices.ServiceCategory.Spa,
        ServiceCategory.Consultation => DomainServices.ServiceCategory.Consultation,
        ServiceCategory.Other => DomainServices.ServiceCategory.Other,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown application service category."),
    };

    /// <summary>Application -&gt; Domain direction, needed by <see cref="ServiceQueryService.SearchServicesAsync(ServiceSearchFilter, CancellationToken)"/> to compare a caller-supplied <see cref="ServiceStatus"/> filter value against Domain data - same reasoning as <c>Customers.CustomerMapper.MapStatusToDomain</c>.</summary>
    public static DomainServices.ServiceStatus MapStatusToDomain(ServiceStatus status) => status switch
    {
        ServiceStatus.Active => DomainServices.ServiceStatus.Active,
        ServiceStatus.Seasonal => DomainServices.ServiceStatus.Seasonal,
        ServiceStatus.Discontinued => DomainServices.ServiceStatus.Discontinued,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown application service status."),
    };
}
