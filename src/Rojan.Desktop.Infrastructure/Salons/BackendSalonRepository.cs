using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using DomainSalons = Rojan.Desktop.Domain.Salons;

namespace Rojan.Desktop.Infrastructure.Salons;

/// <summary>
/// Phase 1.2 Owner App Create Salon Flow: the real, backend-connected
/// <see cref="DomainSalons.ISalonRepository"/>. Unlike every other
/// <c>Backend*Repository</c> in this app, neither method here is scoped by
/// a <c>salonId</c> obtained through <c>Application.Salons.ISalonContextService</c> -
/// this repository *is* how a caller discovers which salon(s) they own in
/// the first place, so depending on that service here would be circular.
/// No <c>Fake</c>/<c>Ef</c> predecessor exists to replace - Salon is a
/// genuinely new vertical slice for the Owner App, not a swap.
/// </summary>
public sealed class BackendSalonRepository(IApiClient apiClient) : DomainSalons.ISalonRepository
{
    private const string SalonsPath = "/api/v1/salons";
    private const string MinePath = "/api/v1/salons/mine";

    public async Task<IReadOnlyList<DomainSalons.Salon>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetAsync<List<SalonResponse>>(MinePath, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to load the signed-in owner's salons (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data.Select(Map).ToList();
    }

    public async Task<DomainSalons.Salon> CreateAsync(DomainSalons.Salon salon, CancellationToken cancellationToken = default)
    {
        var request = new CreateSalonRequest(salon.Name, NullIfEmpty(salon.Description), salon.Phone, NullIfEmpty(salon.Email), salon.Address);

        var response = await apiClient
            .PostAsync<CreateSalonRequest, SalonResponse>(SalonsPath, request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create salon (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return Map(response.Data);
    }

    /// <summary>QR Ecosystem (Desktop Productionization Sprint 1): fetches the salon's public-booking-link PNG via <c>GET /api/v1/salons/{salonId}/qr-code</c> - the raw-bytes path (<see cref="IApiClient.GetBytesAsync"/>), never JSON.</summary>
    public async Task<byte[]> GetQrCodeAsync(string salonId, int sizePx, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetBytesAsync($"{SalonsPath}/{salonId}/qr-code?size={sizePx}", cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to generate salon QR code (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data;
    }

    private static DomainSalons.Salon Map(SalonResponse response) => new(
        response.Id,
        response.Name,
        response.Description,
        response.Phone,
        response.Email,
        response.Address,
        response.Active);

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
