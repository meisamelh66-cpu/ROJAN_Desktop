namespace Rojan.Desktop.Domain.Services;

/// <summary>A salon's real, owner-named category, as needed to populate a category picker for <see cref="IServiceRepository.CreateServiceAsync"/> - distinct from the fixed <see cref="ServiceCategory"/> enum, which classifies/filters, never routes.</summary>
public sealed record ServiceCategoryOption(string Id, string Name);
