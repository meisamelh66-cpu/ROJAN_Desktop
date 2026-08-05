using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using DomainServices = Rojan.Desktop.Domain.Services;

namespace Rojan.Desktop.Infrastructure.Services;

/// <summary>
/// Reception Booking Integration Phase 1 (Service Integration): the real,
/// backend-connected <see cref="DomainServices.IServiceRepository"/> -
/// replaces <c>EfServiceRepository</c> (which stays in the codebase,
/// unreferenced, same convention as every earlier Fake/Ef-&gt;Backend swap -
/// see <c>BackendBookingRepository</c>'s own doc comment). <see cref="ISalonContextService"/>
/// resolves the salon exactly as every other <c>Backend*Repository</c>
/// does.
///
/// There is no flat "get service by id" or "list every service" endpoint
/// on ROJAN_Backend - a service only resolves through its owning category
/// (same reasoning already documented on <see cref="ServiceResponse"/>'s
/// own doc comment, which <c>BackendBookingRepository.BuildServiceLookupAsync</c>
/// already established as a lookup-only pattern). <see cref="FetchAllServicesAsync"/>
/// is the primary-read version of that same fetch-every-category-then-every-
/// category's-services shape - unlike the lookup version, a failure here
/// throws rather than degrading to an empty map, since this *is* the
/// catalog data a caller asked for, not a best-effort cosmetic name
/// resolution.
///
/// Honesty notes on the mapping:
/// <list type="bullet">
/// <item><see cref="MapCategory"/> classifies the backend's real,
/// per-salon, owner-named category into one of the five known
/// <see cref="DomainServices.ServiceCategory"/> values by case-insensitive
/// name match, falling back to <see cref="DomainServices.ServiceCategory.Other"/>
/// - never lossy, since <see cref="DomainServices.Service.CategoryName"/>
/// always carries the real name alongside it. See that enum's own doc
/// comment for why a full entity redesign was not attempted here.</item>
/// <item><see cref="DomainServices.Service.Status"/> only ever comes back
/// <see cref="DomainServices.ServiceStatus.Active"/> or <see cref="DomainServices.ServiceStatus.Discontinued"/> -
/// ROJAN_Backend's <c>Service.active</c> is a plain boolean, with no
/// equivalent of <see cref="DomainServices.ServiceStatus.Seasonal"/>. That
/// value simply never appears for backend-sourced data - same "the gap is
/// a value that's never produced, not a crash" reasoning as
/// <c>BackendBookingRepository.SupportsInProgressAndNoShowStatuses</c>.</item>
/// <item><see cref="GetAssignedSpecialistsAsync"/> always returns empty -
/// ROJAN_Backend has no concept of a specialist-to-service assignment at
/// all (<see cref="DomainServices.Service"/> and <c>Domain.Specialists.Specialist</c>
/// are fully independent there). <see cref="AssignSpecialistAsync"/>/<see cref="UnassignSpecialistAsync"/>
/// throw rather than silently no-op, since there is no backend call that
/// could ever make either one real - same reasoning as
/// <c>BackendBookingRepository.CreateBookingAsync</c>.</item>
/// </list>
/// </summary>
public sealed class BackendServiceRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService) : DomainServices.IServiceRepository
{
    public async Task<IReadOnlyList<DomainServices.Service>> GetServicesAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return await FetchAllServicesAsync(salonId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DomainServices.Service?> GetServiceByIdAsync(string serviceId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var services = await FetchAllServicesAsync(salonId, cancellationToken).ConfigureAwait(false);
        return services.FirstOrDefault(service => service.Id == serviceId);
    }

    /// <summary>Always empty - see this class's own doc comment for why ROJAN_Backend has nothing to fetch here.</summary>
    public Task<IReadOnlyList<DomainServices.SpecialistService>> GetAssignedSpecialistsAsync(string serviceId, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<DomainServices.SpecialistService>>([]);

    public Task<DomainServices.SpecialistService> AssignSpecialistAsync(DomainServices.SpecialistService assignment, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-to-service assignment concept - Service and Specialist are fully " +
            "independent there. See BackendServiceRepository's own doc comment.");

    public Task UnassignSpecialistAsync(string serviceId, string assignmentId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend has no specialist-to-service assignment concept - Service and Specialist are fully " +
            "independent there. See BackendServiceRepository's own doc comment.");

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to list services for.");
    }

    private async Task<List<DomainServices.Service>> FetchAllServicesAsync(string salonId, CancellationToken cancellationToken)
    {
        var categoriesResponse = await apiClient
            .GetAsync<List<ServiceCategoryResponse>>($"/api/v1/salons/{salonId}/categories", cancellationToken)
            .ConfigureAwait(false);

        if (!categoriesResponse.IsSuccess || categoriesResponse.Data is null)
        {
            throw new ApiException($"Failed to load service categories (status {categoriesResponse.StatusCode}): {categoriesResponse.ErrorMessage}");
        }

        var services = new List<DomainServices.Service>();
        foreach (var category in categoriesResponse.Data)
        {
            var servicesResponse = await apiClient
                .GetAsync<List<ServiceResponse>>($"/api/v1/salons/{salonId}/categories/{category.Id}/services", cancellationToken)
                .ConfigureAwait(false);

            if (!servicesResponse.IsSuccess || servicesResponse.Data is null)
            {
                throw new ApiException(
                    $"Failed to load services for category '{category.Id}' (status {servicesResponse.StatusCode}): {servicesResponse.ErrorMessage}");
            }

            services.AddRange(servicesResponse.Data.Select(response => MapService(response, category.Name)));
        }

        return services;
    }

    private static DomainServices.Service MapService(ServiceResponse response, string categoryName) => new(
        response.Id,
        response.Name,
        MapCategory(categoryName),
        response.Active ? DomainServices.ServiceStatus.Active : DomainServices.ServiceStatus.Discontinued,
        response.DurationMinutes,
        FormatToman(response.Price),
        response.Description ?? string.Empty,
        categoryName);

    private static DomainServices.ServiceCategory MapCategory(string categoryName) => categoryName.Trim().ToLowerInvariant() switch
    {
        "hair" => DomainServices.ServiceCategory.Hair,
        "colour" or "color" => DomainServices.ServiceCategory.Colour,
        "nails" => DomainServices.ServiceCategory.Nails,
        "skin" => DomainServices.ServiceCategory.Skin,
        "spa" => DomainServices.ServiceCategory.Spa,
        "consultation" => DomainServices.ServiceCategory.Consultation,
        _ => DomainServices.ServiceCategory.Other,
    };

    private static string FormatToman(decimal amount) => $"{amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} تومان";
}
