using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.Salons;

/// <summary>
/// Default <see cref="ISalonSessionAdapter"/> implementation. Stateless,
/// dependency-free pure mapping - see the interface's own doc comment for
/// why this exists and what it replaces.
/// </summary>
public sealed class SalonSessionAdapter : ISalonSessionAdapter
{
    public OrganizationDto ToOrganizationDto(SalonContext salonContext) =>
        new(
            Id: salonContext.SalonId,
            Name: salonContext.SalonName,
            LegalName: salonContext.SalonName,
            Logo: string.Empty,
            BrandColor: string.Empty,
            TaxInformation: string.Empty,
            Subscription: SubscriptionPlan.Trial,
            Status: OrganizationStatus.Active,
            CreatedDate: DateTimeOffset.UtcNow,
            Code: string.Empty,
            Phone: string.Empty,
            Email: string.Empty,
            Address: string.Empty,
            TimeZone: string.Empty,
            Language: string.Empty,
            Currency: string.Empty);

    public WorkspaceRole ToWorkspaceRole(SalonContext salonContext)
    {
        if (salonContext.IsOwner)
        {
            return WorkspaceRole.OrganizationOwner;
        }

        return salonContext.MembershipRole switch
        {
            "MANAGER" => WorkspaceRole.OrganizationManager,
            _ => WorkspaceRole.Reception,
        };
    }
}
