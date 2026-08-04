using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using DomainBookings = Rojan.Desktop.Domain.Bookings;

namespace Rojan.Desktop.Infrastructure.Bookings;

/// <summary>
/// Owner App Booking Integration: the real, backend-connected
/// <see cref="DomainBookings.IBookingRepository"/> - replaces
/// <c>EfBookingRepository</c> (which stays in the codebase,
/// unreferenced, same convention as every earlier Fake/Ef-&gt;Backend
/// swap - see <c>BackendDashboardRepository</c>'s own doc comment).
///
/// Three honesty notes on the mapping, all deliberate, all documented in
/// <c>ROJAN_Booking_Integration_Implementation_Report_v1.md</c>:
/// <list type="bullet">
/// <item><see cref="DomainBookings.Booking.CustomerName"/> is set to the
/// raw <c>customerId</c> - ROJAN_Backend has no endpoint to resolve a
/// user's display name from another caller's session (only
/// <c>GET /users/me</c>, the caller's own profile). Deferred to the
/// Customer CRM phase, which was explicitly separated from this task.</item>
/// <item><see cref="DomainBookings.Booking.ServiceName"/>/<see cref="DomainBookings.Booking.Price"/>
/// resolve via <see cref="BuildServiceLookupAsync"/> and
/// <see cref="DomainBookings.Booking.SpecialistName"/> via
/// <see cref="ResolveSpecialistNameAsync"/> - both are best-effort: a
/// failed lookup falls back to the raw id rather than failing the whole
/// booking load, since a booking is still meaningfully useful without a
/// resolved name.</item>
/// <item><see cref="DomainBookings.Booking.OrganizationId"/>/<see cref="DomainBookings.Booking.BranchId"/>
/// are stamped with the session's *current* <see cref="IEnterpriseContext"/>
/// values (not a real backend concept - ROJAN_Backend only has
/// <c>salonId</c>) purely so <c>BookingQueryService.ScopeToCurrentSession</c>'s
/// existing Organization/Branch filter does not silently discard every
/// backend-sourced booking. The real tenant boundary is already enforced
/// by <see cref="ISalonContextService"/> resolving the correct
/// <c>salonId</c> for every call - this local filter becomes a harmless
/// no-op for backend data, not a redesign of it.</item>
/// </list>
///
/// <see cref="CreateBookingAsync"/> throws - see its own doc comment for
/// why an owner-initiated create has no backend endpoint to call yet.
/// <see cref="SupportsInProgressAndNoShowStatuses"/> is <see langword="false"/> -
/// ROJAN_Backend's <c>BookingStatus</c> has only 4 states, no equivalent
/// for <see cref="DomainBookings.BookingStatus.InProgress"/>/<see cref="DomainBookings.BookingStatus.NoShow"/>.
/// </summary>
public sealed class BackendBookingRepository(
    IApiClient apiClient,
    ISalonContextService salonContextService,
    IEnterpriseContext enterpriseContext) : DomainBookings.IBookingRepository
{
    private const int PageSize = 100;

    public bool SupportsInProgressAndNoShowStatuses => false;

    public async Task<IReadOnlyList<DomainBookings.Booking>> GetBookingsAsync(CancellationToken cancellationToken = default)
    {
        var salonId = await ResolveSalonIdAsync(cancellationToken).ConfigureAwait(false);
        var responses = await FetchAllBookingsAsync(salonId, cancellationToken).ConfigureAwait(false);
        var servicesById = await BuildServiceLookupAsync(salonId, cancellationToken).ConfigureAwait(false);
        var specialistNames = new Dictionary<string, string>(StringComparer.Ordinal);

        var bookings = new List<DomainBookings.Booking>(responses.Count);
        foreach (var response in responses)
        {
            var specialistName = await ResolveSpecialistNameAsync(salonId, response.SpecialistId, specialistNames, cancellationToken).ConfigureAwait(false);
            bookings.Add(MapBooking(response, servicesById, specialistName));
        }

        return bookings;
    }

    public async Task<DomainBookings.Booking?> GetBookingByIdAsync(string bookingId, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetAsync<BookingResponse>($"/api/v1/bookings/{bookingId}", cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == 404)
        {
            return null;
        }

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load booking '{bookingId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        var servicesById = await BuildServiceLookupAsync(response.Data.SalonId, cancellationToken).ConfigureAwait(false);
        var specialistNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var specialistName = await ResolveSpecialistNameAsync(response.Data.SalonId, response.Data.SpecialistId, specialistNames, cancellationToken).ConfigureAwait(false);

        return MapBooking(response.Data, servicesById, specialistName);
    }

    /// <summary>
    /// ROJAN_Backend's <c>POST /api/v1/bookings</c> always resolves the new
    /// booking's customer from the *caller's own* bearer token - there is
    /// no request field for a different customer id, and no separate
    /// owner-initiated endpoint. Calling it from the Owner App's session
    /// (the salon owner's own account) would silently create a booking
    /// where the owner is recorded as the customer - wrong data, not a
    /// missing feature, so this throws rather than doing that. Not one of
    /// this phase's four required flows (view/details/status-update/
    /// cancel-confirm) - deferred until the backend exposes an
    /// owner-initiated creation path.
    /// </summary>
    public Task<DomainBookings.Booking> CreateBookingAsync(DomainBookings.Booking booking, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException(
            "ROJAN_Backend's POST /api/v1/bookings always attributes the new booking to the calling user as its customer - " +
            "there is no owner-initiated \"create a booking for this customer\" endpoint. Calling it from the Owner App's own " +
            "session would incorrectly make the salon owner the booking's customer. See BackendBookingRepository's own doc " +
            "comment and ROJAN_Booking_Integration_Implementation_Report_v1.md.");

    public async Task<DomainBookings.Booking> UpdateBookingStatusAsync(string bookingId, DomainBookings.BookingStatus status, CancellationToken cancellationToken = default)
    {
        var path = status switch
        {
            DomainBookings.BookingStatus.Confirmed => $"/api/v1/bookings/{bookingId}/confirm",
            DomainBookings.BookingStatus.Cancelled => $"/api/v1/bookings/{bookingId}/cancel",
            DomainBookings.BookingStatus.Completed => $"/api/v1/bookings/{bookingId}/complete",
            _ => throw new NotSupportedException(
                $"ROJAN_Backend has no endpoint to set a booking's status to {status} - only Confirmed/Cancelled/Completed " +
                "are reachable (see IBookingRepository.SupportsInProgressAndNoShowStatuses)."),
        };

        var response = await apiClient.PatchAsync<BookingResponse>(path, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to update booking '{bookingId}' to {status} (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return await MapWithLookupsAsync(response.Data, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DomainBookings.Booking> RescheduleBookingAsync(string bookingId, DateTimeOffset newScheduledAt, CancellationToken cancellationToken = default)
    {
        var request = new RescheduleBookingRequest(newScheduledAt.DateTime);
        var response = await apiClient
            .PutAsync<RescheduleBookingRequest, BookingResponse>($"/api/v1/bookings/{bookingId}/reschedule", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to reschedule booking '{bookingId}' (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return await MapWithLookupsAsync(response.Data, cancellationToken).ConfigureAwait(false);
    }

    private async Task<DomainBookings.Booking> MapWithLookupsAsync(BookingResponse response, CancellationToken cancellationToken)
    {
        var servicesById = await BuildServiceLookupAsync(response.SalonId, cancellationToken).ConfigureAwait(false);
        var specialistNames = new Dictionary<string, string>(StringComparer.Ordinal);
        var specialistName = await ResolveSpecialistNameAsync(response.SalonId, response.SpecialistId, specialistNames, cancellationToken).ConfigureAwait(false);
        return MapBooking(response, servicesById, specialistName);
    }

    private async Task<string> ResolveSalonIdAsync(CancellationToken cancellationToken)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false);
        return salonId ?? throw new ApiException("The signed-in owner does not manage any salon yet - there is nothing to list bookings for.");
    }

    private async Task<List<BookingResponse>> FetchAllBookingsAsync(string salonId, CancellationToken cancellationToken)
    {
        var all = new List<BookingResponse>();
        var page = 0;

        while (true)
        {
            var response = await apiClient
                .GetAsync<PagedResponse<BookingResponse>>($"/api/v1/salons/{salonId}/bookings?page={page}&size={PageSize}", cancellationToken)
                .ConfigureAwait(false);

            if (!response.IsSuccess || response.Data is null)
            {
                throw new ApiException($"Failed to load bookings (status {response.StatusCode}): {response.ErrorMessage}");
            }

            all.AddRange(response.Data.Content);

            page++;
            if (page >= response.Data.TotalPages || response.Data.Content.Count == 0)
            {
                break;
            }
        }

        return all;
    }

    /// <summary>
    /// Builds a salon-wide serviceId-&gt;<see cref="ServiceResponse"/> map by
    /// enumerating every category then every category's services - there
    /// is no flat "get service by id" endpoint on ROJAN_Backend (see
    /// <see cref="ServiceResponse"/>'s own doc comment). Resolved fresh on
    /// every call rather than cached across calls, matching this app's
    /// existing "reload everything fresh" convention elsewhere - a known,
    /// documented tradeoff (more requests per load) rather than a stale-cache
    /// risk. A failed categories/services fetch degrades gracefully to an
    /// empty map (bookings still load, just without resolved service
    /// names/prices) rather than failing the whole booking list.
    /// </summary>
    private async Task<Dictionary<string, ServiceResponse>> BuildServiceLookupAsync(string salonId, CancellationToken cancellationToken)
    {
        var servicesById = new Dictionary<string, ServiceResponse>(StringComparer.Ordinal);

        var categoriesResponse = await apiClient
            .GetAsync<List<ServiceCategoryResponse>>($"/api/v1/salons/{salonId}/categories", cancellationToken)
            .ConfigureAwait(false);

        if (!categoriesResponse.IsSuccess || categoriesResponse.Data is null)
        {
            return servicesById;
        }

        foreach (var category in categoriesResponse.Data)
        {
            var servicesResponse = await apiClient
                .GetAsync<List<ServiceResponse>>($"/api/v1/salons/{salonId}/categories/{category.Id}/services", cancellationToken)
                .ConfigureAwait(false);

            if (!servicesResponse.IsSuccess || servicesResponse.Data is null)
            {
                continue;
            }

            foreach (var service in servicesResponse.Data)
            {
                servicesById[service.Id] = service;
            }
        }

        return servicesById;
    }

    /// <summary>Resolves and caches (within <paramref name="cache"/>, scoped to one <see cref="GetBookingsAsync"/>/single-booking call) a specialist's display name. Falls back to the raw id on a failed lookup rather than failing the whole booking - same best-effort reasoning as <see cref="BuildServiceLookupAsync"/>.</summary>
    private async Task<string> ResolveSpecialistNameAsync(string salonId, string specialistId, Dictionary<string, string> cache, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(specialistId))
        {
            return string.Empty;
        }

        if (cache.TryGetValue(specialistId, out var cached))
        {
            return cached;
        }

        var response = await apiClient
            .GetAsync<SpecialistResponse>($"/api/v1/salons/{salonId}/specialists/{specialistId}", cancellationToken)
            .ConfigureAwait(false);

        var name = response.IsSuccess && response.Data is not null ? response.Data.DisplayName : specialistId;
        cache[specialistId] = name;
        return name;
    }

    private DomainBookings.Booking MapBooking(BookingResponse response, Dictionary<string, ServiceResponse> servicesById, string specialistName)
    {
        var hasService = servicesById.TryGetValue(response.ServiceId, out var service);
        var durationMinutes = (int)Math.Round((response.EndTime - response.StartTime).TotalMinutes);

        return new DomainBookings.Booking(
            response.Id,
            response.CustomerId,
            response.CustomerId, // CustomerName: unresolved - no backend endpoint exists yet (see this class's own doc comment).
            response.ServiceId,
            hasService ? service!.Name : response.ServiceId,
            response.SpecialistId,
            specialistName,
            // The backend sends a timezone-less LocalDateTime - treated as the app's own local
            // wall-clock time, matching BookingPageViewModel.CreateBookingAsync's own existing
            // "use DateTimeOffset.Now.Offset for a local date/time with no offset of its own" convention.
            new DateTimeOffset(response.StartTime, DateTimeOffset.Now.Offset),
            durationMinutes,
            hasService ? FormatToman(service!.Price) : string.Empty,
            MapStatus(response.Status),
            response.Notes ?? string.Empty,
            enterpriseContext.CurrentOrganizationId ?? string.Empty,
            enterpriseContext.CurrentBranchId ?? string.Empty);
    }

    private static DomainBookings.BookingStatus MapStatus(string backendStatus) => backendStatus switch
    {
        "PENDING" => DomainBookings.BookingStatus.Pending,
        "CONFIRMED" => DomainBookings.BookingStatus.Confirmed,
        "CANCELLED" => DomainBookings.BookingStatus.Cancelled,
        "COMPLETED" => DomainBookings.BookingStatus.Completed,
        _ => throw new ApiException($"Unknown booking status '{backendStatus}' returned by the backend."),
    };

    private static string FormatToman(decimal amount) => $"{amount.ToString("N0", System.Globalization.CultureInfo.InvariantCulture)} تومان";
}
