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

    /// <summary>
    /// Remediation Phase 2 (Role Mapping Hardening): the pre-hardening
    /// version of this method mapped <c>IsOwner: false</c> to
    /// <see cref="WorkspaceRole.OrganizationManager"/> for
    /// <c>MembershipRole == "MANAGER"</c> and <see cref="WorkspaceRole.Reception"/>
    /// for *everything else*, including a bare Specialist link (which has
    /// no <see cref="SalonContext.MembershipRole"/> at all - a Specialist
    /// link is not a <c>SalonMembership</c> row) and any unrecognized role
    /// string. That default over-granted: the real backend Specialist-only
    /// relationship (<c>SalonPermissionResolver.kt</c>) is scoped to
    /// <c>MANAGE_SCHEDULE_OWN</c> alone, never Reception's real
    /// <c>MANAGE_BOOKINGS</c>/<c>VIEW_CUSTOMER_IDENTITY</c>/etc - see
    /// ROJAN_DESKTOP_RBAC_MIGRATION_MAP_v1.md's own "Gap 3" for the full
    /// account this migration closes.
    ///
    /// Hardened mapping, identity-preserving (Owner/Manager/Reception/
    /// Specialist each map to their own distinct capability, never widened
    /// into a neighbor's):
    /// <list type="bullet">
    /// <item><see cref="SalonContext.IsOwner"/> -&gt; <see cref="WorkspaceRole.OrganizationOwner"/> (unchanged).</item>
    /// <item><c>MembershipRole == "MANAGER"</c> -&gt; <see cref="WorkspaceRole.OrganizationManager"/> (unchanged).</item>
    /// <item><c>MembershipRole == "RECEPTIONIST"</c> -&gt; <see cref="WorkspaceRole.Reception"/> (now an explicit
    /// match, not a fallthrough default).</item>
    /// <item>No membership row (<c>MembershipRole is null</c>) but the real backend permission
    /// set carries exactly the Specialist-only signature (<c>MANAGE_SCHEDULE_OWN</c>, per
    /// <c>SalonPermissionResolver.kt</c>'s own documented resolution) -&gt;
    /// <see cref="WorkspaceRole.Specialist"/> - new: this case previously fell into the
    /// Reception default.</item>
    /// <item>Anything else - an unrecognized <see cref="SalonContext.MembershipRole"/> string, or no
    /// recognizable relationship at all -&gt; <see cref="WorkspaceRole.Unknown"/>, which
    /// <see cref="Domain.Organizations.RolePermissions"/> grants zero permissions - fail closed,
    /// never fail open onto Reception.</item>
    /// </list>
    /// </summary>
    public WorkspaceRole ToWorkspaceRole(SalonContext salonContext)
    {
        if (salonContext.IsOwner)
        {
            return WorkspaceRole.OrganizationOwner;
        }

        return salonContext.MembershipRole switch
        {
            "MANAGER" => WorkspaceRole.OrganizationManager,
            "RECEPTIONIST" => WorkspaceRole.Reception,
            null when IsBareSpecialistLink(salonContext.Permissions) => WorkspaceRole.Specialist,
            _ => WorkspaceRole.Unknown,
        };
    }

    /// <summary>
    /// A bare Specialist link (no <see cref="SalonContext.MembershipRole"/>) is recognized by its
    /// real backend permission signature, not guessed: <c>SalonPermissionResolver.resolve()</c>
    /// grants a Specialist-only relationship exactly <c>{MANAGE_SCHEDULE_OWN}</c> and nothing else
    /// - verified directly against ROJAN_Backend source. Any other permission set reaching here
    /// with no membership role is not a relationship this mapping recognizes, and falls through to
    /// <see cref="WorkspaceRole.Unknown"/> instead.
    /// </summary>
    private static bool IsBareSpecialistLink(IReadOnlySet<string> permissions) =>
        permissions.Count == 1 && permissions.Contains("MANAGE_SCHEDULE_OWN");
}
