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
        service.Description);

    public static AssignedSpecialistDto MapAssignment(DomainServices.SpecialistService assignment) =>
        new(assignment.Id, assignment.ServiceId, assignment.SpecialistId, assignment.SpecialistName);

    public static ServiceCategory MapCategory(DomainServices.ServiceCategory category) => category switch
    {
        DomainServices.ServiceCategory.Hair => ServiceCategory.Hair,
        DomainServices.ServiceCategory.Colour => ServiceCategory.Colour,
        DomainServices.ServiceCategory.Nails => ServiceCategory.Nails,
        DomainServices.ServiceCategory.Skin => ServiceCategory.Skin,
        DomainServices.ServiceCategory.Spa => ServiceCategory.Spa,
        DomainServices.ServiceCategory.Consultation => ServiceCategory.Consultation,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown domain service category."),
    };

    public static ServiceStatus MapStatus(DomainServices.ServiceStatus status) => status switch
    {
        DomainServices.ServiceStatus.Active => ServiceStatus.Active,
        DomainServices.ServiceStatus.Seasonal => ServiceStatus.Seasonal,
        DomainServices.ServiceStatus.Discontinued => ServiceStatus.Discontinued,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown domain service status."),
    };
}
