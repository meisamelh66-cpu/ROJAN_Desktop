namespace Rojan.Desktop.Domain.Organizations;

/// <summary>
/// One tenant of the Enterprise Multi-Branch platform - the top of the
/// Organization -> Branch -> (existing modules' own data) hierarchy.
/// <see cref="BrandColor"/> is a plain hex string (e.g. "#8E28E7")
/// carried as organization-administered data, not a design-system token -
/// distinct from and never a substitute for the shared Fluent 2
/// <c>Rojan.Brush.*</c> resources every page still renders with.
/// </summary>
public sealed record Organization(
    string Id,
    string Name,
    string LegalName,
    string Logo,
    string BrandColor,
    string TaxInformation,
    SubscriptionPlan Subscription,
    OrganizationStatus Status,
    DateTimeOffset CreatedDate);
