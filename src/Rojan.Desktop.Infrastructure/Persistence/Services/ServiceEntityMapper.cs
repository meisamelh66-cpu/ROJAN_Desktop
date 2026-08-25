using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Persistence.Services;

/// <summary>Domain&lt;-&gt;persistence-entity mapping for the Services vertical slice - internal, only <see cref="EfServiceRepository"/> calls it, same convention as every other Domain&lt;-&gt;entity mapper in this codebase (<see cref="Customers.CustomerEntityMapper"/>, <see cref="Specialists.SpecialistEntityMapper"/>).</summary>
internal static class ServiceEntityMapper
{
    public static DomainServices.Service MapToDomain(ServiceEntity entity) => new(
        entity.Id,
        entity.Name,
        entity.Category,
        entity.Status,
        entity.DurationMinutes,
        entity.Price,
        entity.Description,
        CategoryName: null,
        CategoryId: string.Empty);

    public static ServiceEntity MapToEntity(DomainServices.Service service) => new()
    {
        Id = service.Id,
        Name = service.Name,
        Category = service.Category,
        Status = service.Status,
        DurationMinutes = service.DurationMinutes,
        Price = service.Price,
        Description = service.Description,
    };

    public static DomainServices.SpecialistService MapToDomain(SpecialistServiceEntity entity) =>
        new(entity.Id, entity.ServiceId, entity.SpecialistId, entity.SpecialistName);

    public static SpecialistServiceEntity MapToEntity(DomainServices.SpecialistService assignment) => new()
    {
        Id = assignment.Id,
        ServiceId = assignment.ServiceId,
        SpecialistId = assignment.SpecialistId,
        SpecialistName = assignment.SpecialistName,
    };
}
