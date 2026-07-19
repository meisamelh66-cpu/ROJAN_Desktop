using Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Services;

/// <summary>
/// In-memory <see cref="IServiceRepository"/> providing static sample
/// data - Phase 13 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real specialist-assignment commands, so it needs to remember
/// writes for the app's lifetime - registered as a DI singleton (see
/// Infrastructure's ServiceCollectionExtensions). Assigned-specialist seed
/// names match the specialists already seeded in
/// <c>Specialists.FakeSpecialistRepository</c> for a cohesive demo - not a
/// real cross-slice link, just consistent naming. The small artificial
/// delays are deliberate, same reasoning as every other fake repository:
/// without them, Loading states would never actually be observable when
/// running the app.
/// </summary>
public sealed class FakeServiceRepository : IServiceRepository
{
    private readonly List<Service> _services;
    private readonly List<SpecialistService> _assignments;

    public FakeServiceRepository()
    {
        _services =
        [
            new Service("service-1", "Haircut & Style", ServiceCategory.Hair, ServiceStatus.Active,
                60, "$65", "Classic cut and blow-dry finish."),
            new Service("service-2", "Colour Touch-Up", ServiceCategory.Colour, ServiceStatus.Active,
                90, "$120", "Root touch-up and single-process colour."),
            new Service("service-3", "Full Package - Balayage & Style", ServiceCategory.Colour, ServiceStatus.Active,
                150, "$220", "Balayage highlights with a finishing style."),
            new Service("service-4", "Manicure", ServiceCategory.Nails, ServiceStatus.Active,
                45, "$40", "Classic manicure with polish."),
            new Service("service-5", "Facial Renewal", ServiceCategory.Skin, ServiceStatus.Active,
                60, "$85", "Deep-cleansing facial treatment."),
            new Service("service-6", "Massage", ServiceCategory.Spa, ServiceStatus.Active,
                60, "$95", "Relaxation massage therapy."),
            new Service("service-7", "Consultation", ServiceCategory.Consultation, ServiceStatus.Active,
                30, "$0", "First-time client consultation, complimentary."),
            new Service("service-8", "Keratin Smoothing Treatment", ServiceCategory.Hair, ServiceStatus.Seasonal,
                120, "$180", "Frizz-reducing keratin treatment, seasonal offering."),
            new Service("service-9", "Perm Styling", ServiceCategory.Hair, ServiceStatus.Discontinued,
                90, "$110", "Classic perm styling, no longer offered."),
        ];

        _assignments =
        [
            new SpecialistService("assignment-1", "service-1", "specialist-1", "Jordan Lee"),
            new SpecialistService("assignment-2", "service-1", "specialist-2", "Priya Nair"),
            new SpecialistService("assignment-3", "service-2", "specialist-1", "Jordan Lee"),
            new SpecialistService("assignment-4", "service-3", "specialist-1", "Jordan Lee"),
            new SpecialistService("assignment-5", "service-4", "specialist-3", "Casey Morgan"),
            new SpecialistService("assignment-6", "service-5", "specialist-3", "Casey Morgan"),
            new SpecialistService("assignment-7", "service-6", "specialist-3", "Casey Morgan"),
            new SpecialistService("assignment-8", "service-7", "specialist-2", "Priya Nair"),
        ];
    }

    public async Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _services.ToList();
    }

    public async Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _services.FirstOrDefault(service => service.Id == serviceId);
    }

    public async Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _assignments.Where(assignment => assignment.ServiceId == serviceId).ToList();
    }

    public async Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _assignments.Add(assignment);
        return assignment;
    }

    public async Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _assignments.RemoveAll(assignment => assignment.ServiceId == serviceId && assignment.Id == assignmentId);
    }
}
