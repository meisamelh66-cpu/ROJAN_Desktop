using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using DomainMembership = Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Infrastructure.Membership;

/// <summary>
/// Reception Production Integration: the real, backend-connected
/// <see cref="DomainMembership.ISalonInviteRepository"/>. Same shape as
/// <c>Salons.BackendSalonRepository</c> - no <c>Fake</c>/<c>Ef</c>
/// predecessor to replace, no salon-scoping prerequisite (this repository
/// *is* how a caller with no salon membership yet discovers one).
/// </summary>
public sealed class BackendSalonInviteRepository(IApiClient apiClient) : DomainMembership.ISalonInviteRepository
{
    public async Task<DomainMembership.SalonInviteDetails> GetDetailsAsync(string token, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetAsync<SalonInviteDetailsResponse>(DetailsPath(token), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to resolve invite (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new DomainMembership.SalonInviteDetails(response.Data.SalonName, response.Data.Role);
    }

    public async Task<DomainMembership.AcceptedMembership> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default)
    {
        var response = await apiClient
            .PostAsync<object?, SalonInviteAcceptedResponse>(AcceptPath(token), null, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to accept invite (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new DomainMembership.AcceptedMembership(response.Data.SalonId, salonName, response.Data.Role);
    }

    /// <summary>QR Ecosystem: creates a new staff invite via <c>POST /api/v1/salons/{salonId}/invites</c> - salon-scoped, unlike <see cref="GetDetailsAsync"/>/<see cref="AcceptAsync"/> above (see this interface's own doc comment for why).</summary>
    public async Task<DomainMembership.CreatedSalonInvite> CreateAsync(string salonId, DomainMembership.SalonRole role, CancellationToken cancellationToken = default)
    {
        var request = new CreateSalonInviteRequest(MapRole(role));

        var response = await apiClient
            .PostAsync<CreateSalonInviteRequest, CreateSalonInviteResponse>(InvitesPath(salonId), request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create invite (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new DomainMembership.CreatedSalonInvite(response.Data.Id, response.Data.Token);
    }

    /// <summary>QR Ecosystem: fetches the invite's accept-link PNG via <c>GET /api/v1/salons/{salonId}/invites/{inviteId}/qr-code</c> - the same <see cref="IApiClient.GetBytesAsync"/> raw-bytes path <c>Salons.BackendSalonRepository.GetQrCodeAsync</c> uses, so the URL itself is never constructed client-side.</summary>
    public async Task<byte[]> GetInviteQrCodeAsync(string salonId, string inviteId, int sizePx, CancellationToken cancellationToken = default)
    {
        var response = await apiClient.GetBytesAsync(QrCodePath(salonId, inviteId, sizePx), cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to generate invite QR code (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return response.Data;
    }

    private static string MapRole(DomainMembership.SalonRole role) => role switch
    {
        DomainMembership.SalonRole.Manager => "MANAGER",
        DomainMembership.SalonRole.Receptionist => "RECEPTIONIST",
        _ => throw new ArgumentOutOfRangeException(nameof(role), role, "Unknown salon role."),
    };

    private static string DetailsPath(string token) => $"/api/v1/invites/{token}";

    private static string AcceptPath(string token) => $"/api/v1/invites/{token}/accept";

    private static string InvitesPath(string salonId) => $"/api/v1/salons/{salonId}/invites";

    private static string QrCodePath(string salonId, string inviteId, int sizePx) => $"/api/v1/salons/{salonId}/invites/{inviteId}/qr-code?size={sizePx}";
}
