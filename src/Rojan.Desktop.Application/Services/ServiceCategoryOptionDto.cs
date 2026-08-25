namespace Rojan.Desktop.Application.Services;

/// <summary>Application-layer mirror of <see cref="Rojan.Desktop.Domain.Services.ServiceCategoryOption"/>, mapped by <see cref="ServiceMapper"/> - the real, selectable category record the Create Service picker binds to. Never free text.</summary>
public sealed record ServiceCategoryOptionDto(string Id, string Name);
