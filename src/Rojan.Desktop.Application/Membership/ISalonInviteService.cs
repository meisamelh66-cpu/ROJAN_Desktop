namespace Rojan.Desktop.Application.Membership;

/// <summary>Read/accept use case Presentation depends on for the invite-accept flow - the only way Presentation ever reaches it, never through Domain/Infrastructure directly, same convention <c>Salons.ISalonQueryService</c> already establishes.</summary>
public interface ISalonInviteService
{
    public Task<SalonInviteDetailsDto> GetDetailsAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Accepts the invite *and* persists the resulting membership locally (via <see cref="IAcceptedMembershipStore"/>) as one operation from the caller's perspective - Presentation never touches that Domain-typed store directly, only this Application-layer facade.</summary>
    public Task<AcceptedMembershipDto> AcceptAsync(string token, string salonName, CancellationToken cancellationToken = default);
}
