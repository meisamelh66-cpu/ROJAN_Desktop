using System.Globalization;
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
/// <c>BackendBookingRepository.CreateBookingAsync</c>. <b>Note (Service
/// Catalog Management pass):</b> a real <c>PUT</c>/<c>DELETE
/// /specialists/{id}/services/{serviceId}</c> pair was found to exist on
/// <c>SpecialistController</c> during this pass's verification - this
/// doc-comment claim is now known stale, but wiring it is Specialist-Service
/// Assignment work, explicitly out of scope for this change; left
/// unmodified, not silently "fixed while in here."</item>
/// </list>
///
/// Service Catalog Management: <see cref="CreateServiceAsync"/>/<see cref="UpdateServiceAsync"/>/
/// <see cref="DeactivateServiceAsync"/> are real, against
/// <c>ServiceController</c>'s <c>POST</c>/<c>PUT {serviceId}</c>/<c>DELETE
/// {serviceId}</c> - all three already existed on ROJAN_Backend, verified
/// directly this pass; no new backend contract was needed. <see cref="MapService"/>
/// now captures <see cref="ServiceResponse.CategoryId"/> into
/// <see cref="DomainServices.Service.CategoryId"/> - previously dropped
/// entirely, the exact gap this pass's own plan named as the required fix,
/// without which Update/Deactivate could never address the real
/// <c>/categories/{categoryId}/services/{serviceId}</c> route. There is
/// deliberately no "reactivate" method - ROJAN_Backend's <c>ServiceController</c>
/// has <c>create</c>/<c>list</c>/<c>get</c>/<c>update</c>/<c>deactivate</c>
/// only, no endpoint anywhere sets <c>active</c> back to <see langword="true"/>;
/// this is a real, reported gap (documented as BLOCKED / BACKEND CONTRACT
/// REQUIRED in this pass's implementation report), not one this class works
/// around or fabricates a client-side answer for.
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

    public async Task<IReadOnlyList<DomainServices.ServiceCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var categories = await FetchCategoriesAsync(salonId, cancellationToken).ConfigureAwait(false);
        return categories.Select(category => new DomainServices.ServiceCategoryOption(category.Id, category.Name)).ToList();
    }

    public async Task<DomainServices.Service> CreateServiceAsync(DomainServices.Service service, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new CreateServiceRequest(service.Name, NullIfEmpty(service.Description), service.DurationMinutes, ParsePrice(service.Price));

        var response = await apiClient
            .PostAsync<CreateServiceRequest, ServiceResponse>($"/api/v1/salons/{salonId}/categories/{service.CategoryId}/services", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create service (status {response.StatusCode}): {response.ErrorMessage}");
        }

        var categoryName = await ResolveCategoryNameAsync(salonId, response.Data.CategoryId, cancellationToken).ConfigureAwait(false);
        return MapService(response.Data, categoryName);
    }

    public async Task<DomainServices.Service> UpdateServiceAsync(DomainServices.Service service, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var request = new UpdateServiceRequest(service.Name, NullIfEmpty(service.Description), service.DurationMinutes, ParsePrice(service.Price));

        var response = await apiClient
            .PutAsync<UpdateServiceRequest, ServiceResponse>(
                $"/api/v1/salons/{salonId}/categories/{service.CategoryId}/services/{service.Id}", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to update service '{service.Id}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        var categoryName = await ResolveCategoryNameAsync(salonId, response.Data.CategoryId, cancellationToken).ConfigureAwait(false);
        return MapService(response.Data, categoryName);
    }

    /// <summary>
    /// The only activate/deactivate operation ROJAN_Backend actually exposes for a service
    /// (<c>DELETE .../services/{serviceId}</c>, <c>@Operation(summary = "Deactivate a service")</c>
    /// on <c>ServiceController</c>) - there is no endpoint anywhere that sets <c>active</c> back
    /// to <see langword="true"/>, confirmed by reading every route on that controller. A
    /// "reactivate" capability would need a new Backend endpoint - Team 1/Backend approval
    /// required, not something this class can invent.
    /// </summary>
    public async Task DeactivateServiceAsync(string categoryId, string serviceId, CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);

        var response = await apiClient
            .DeleteAsync<object?>($"/api/v1/salons/{salonId}/categories/{categoryId}/services/{serviceId}", cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess)
        {
            throw new ApiException($"Failed to deactivate service '{serviceId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to list services for.");
    }

    private async Task<List<DomainServices.Service>> FetchAllServicesAsync(string salonId, CancellationToken cancellationToken)
    {
        var categories = await FetchCategoriesAsync(salonId, cancellationToken).ConfigureAwait(false);

        var services = new List<DomainServices.Service>();
        foreach (var category in categories)
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

    private async Task<List<ServiceCategoryResponse>> FetchCategoriesAsync(string salonId, CancellationToken cancellationToken)
    {
        var categoriesResponse = await apiClient
            .GetAsync<List<ServiceCategoryResponse>>($"/api/v1/salons/{salonId}/categories", cancellationToken)
            .ConfigureAwait(false);

        if (!categoriesResponse.IsSuccess || categoriesResponse.Data is null)
        {
            throw new ApiException($"Failed to load service categories (status {categoriesResponse.StatusCode}): {categoriesResponse.ErrorMessage}");
        }

        return categoriesResponse.Data;
    }

    /// <summary>Resolves the owning category's real name for a single service response (Create/Update return one <see cref="ServiceResponse"/>, not the whole catalog) - re-enumerates categories rather than caching, matching this class's existing "reload everything fresh" convention (see <see cref="ServiceResponse"/>'s own doc comment). Falls back to the raw id on a failed lookup rather than failing an otherwise-successful create/update, same best-effort reasoning as <c>BackendBookingRepository.ResolveSpecialistNameAsync</c>.</summary>
    private async Task<string> ResolveCategoryNameAsync(string salonId, string categoryId, CancellationToken cancellationToken)
    {
        var categories = await FetchCategoriesAsync(salonId, cancellationToken).ConfigureAwait(false);
        return categories.FirstOrDefault(category => category.Id == categoryId)?.Name ?? categoryId;
    }

    /// <summary>Parses the exact invariant-culture decimal string <see cref="Application.Services.ServiceCommandService"/> produces for a write payload - never the free-text, currency-symbol-decorated display string <c>ServicePriceParser</c> handles, so no currency-symbol stripping is needed or attempted here.</summary>
    private static decimal ParsePrice(string price) =>
        decimal.TryParse(price, System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0m;

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static DomainServices.Service MapService(ServiceResponse response, string categoryName) => new(
        response.Id,
        response.Name,
        MapCategory(categoryName),
        response.Active ? DomainServices.ServiceStatus.Active : DomainServices.ServiceStatus.Discontinued,
        response.DurationMinutes,
        FormatToman(response.Price),
        response.Description ?? string.Empty,
        categoryName,
        response.CategoryId);

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
