namespace Rojan.Desktop.Application.Services;

/// <summary>Application-layer shape of a real salon category, mapped from <see cref="Rojan.Desktop.Domain.Services.ServiceCategoryOption"/> - populates a Create-Service category picker.</summary>
public sealed record ServiceCategoryDto(string Id, string Name);
