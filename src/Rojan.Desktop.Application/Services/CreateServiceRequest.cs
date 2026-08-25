namespace Rojan.Desktop.Application.Services;

/// <summary>
/// Input to <see cref="IServiceCommandService.CreateServiceAsync"/>. Real
/// identifiers only - <see cref="CategoryId"/> must come from
/// <see cref="IServiceQueryService.GetCategoriesAsync"/>, never typed by
/// the user. <see cref="Price"/> is a raw <see cref="decimal"/>, matching
/// ROJAN_Backend's own wire contract - never <see cref="ServiceDto.Price"/>'s
/// display-formatted string.
/// </summary>
public sealed record CreateServiceRequest(string CategoryId, string Name, string? Description, int DurationMinutes, decimal Price);
