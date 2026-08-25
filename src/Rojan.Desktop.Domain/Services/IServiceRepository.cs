namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Repository abstraction for the service catalog. Domain defines the
/// contract; Infrastructure provides the concrete implementation (a
/// fake/in-memory one for now - Phase 13 explicitly has no backend
/// integration yet, same as every other vertical slice in this app).
///
/// Service Catalog Authoring: <see cref="GetCategoriesAsync"/>/
/// <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/> close
/// the "read-plus-assignment only" gap this interface's own doc comment
/// used to describe - catalog authoring is now in scope. Real identifiers
/// only throughout: both writes take a real <c>categoryId</c> (from
/// <see cref="GetCategoriesAsync"/>, never free text); <see cref="UpdateServiceAsync"/>
/// never takes a category to change it to at all - ROJAN_Backend's own
/// update contract has no field to change a service's category, so this
/// layer must not invent one either, and the caller is expected to carry
/// the existing <see cref="Service.CategoryId"/> forward unchanged. Both
/// writes take <c>price</c> as a raw <see cref="decimal"/>, not
/// <see cref="Service.Price"/>'s display-formatted string - matching
/// ROJAN_Backend's own wire contract exactly and avoiding a
/// string-parse/reformat round trip on every write; <see cref="Service.Price"/>
/// remains a display-only field, unrelated to this layer's write path.
/// Deactivation is folded into <see cref="UpdateServiceAsync"/> via
/// <c>requestedStatus</c>, not a separate method - the same shape
/// <c>Domain.Specialists.ISpecialistRepository.UpdateSpecialistAsync</c>
/// already established for Specialist Deactivation Wiring (a requested
/// Active -&gt; Discontinued transition is detected and followed up with
/// ROJAN_Backend's own dedicated deactivate endpoint).
/// </summary>
public interface IServiceRepository
{
    public Task<IReadOnlyList<Service>> GetServicesAsync(CancellationToken cancellationToken = default);

    public Task<Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<IReadOnlyList<SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default);

    public Task<SpecialistService> AssignSpecialistAsync(SpecialistService assignment, CancellationToken cancellationToken = default);

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default);

    /// <summary>The real, selectable, per-salon categories a new service can be created into - see <see cref="ServiceCategoryOption"/>'s own doc comment.</summary>
    public Task<IReadOnlyList<ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    public Task<Service> CreateServiceAsync(string categoryId, string name, string? description, int durationMinutes, decimal price, CancellationToken cancellationToken = default);

    /// <summary>
    /// Edits a service's fields and/or requests a status change, including
    /// deactivation - see this interface's own doc comment for why
    /// deactivation is folded in here rather than a separate method, and
    /// why <paramref name="categoryId"/> is required (for the URL) but
    /// never changeable. <paramref name="requestedStatus"/> equal to the
    /// service's current status is a plain field edit, not a transition -
    /// same "editing other fields isn't itself a transition" rule
    /// <see cref="ServiceRules.IsValidTransition"/> already documents.
    /// </summary>
    public Task<Service> UpdateServiceAsync(
        string serviceId,
        string categoryId,
        string name,
        string? description,
        int durationMinutes,
        decimal price,
        ServiceStatus requestedStatus,
        CancellationToken cancellationToken = default);
}
