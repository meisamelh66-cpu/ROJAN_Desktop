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

    private static string DetailsPath(string token) => $"/api/v1/invites/{token}";

    private static string AcceptPath(string token) => $"/api/v1/invites/{token}/accept";
}
