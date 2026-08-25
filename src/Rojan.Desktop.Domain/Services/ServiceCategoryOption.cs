namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Service Catalog Authoring: one real, selectable, per-salon service
/// category, as returned by <see cref="IServiceRepository.GetCategoriesAsync"/> -
/// additive alongside the existing fixed <see cref="ServiceCategory"/> enum
/// (which keeps its own established display/filter role unchanged), not a
/// replacement for it. Exists specifically to give the Create Service form
/// a real record to pick from (a real <see cref="Id"/>), never free text -
/// the same "real record, never a typed name" rule this app's other
/// pickers (Booking's specialist/service steps, Specialist-Service
/// Assignment's service picker) already established.
/// </summary>
public sealed record ServiceCategoryOption(string Id, string Name);
