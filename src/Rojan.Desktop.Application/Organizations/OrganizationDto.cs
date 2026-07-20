namespace Rojan.Desktop.Application.Organizations;

public sealed record OrganizationDto(
    string Id,
    string Name,
    string LegalName,
    string Logo,
    string BrandColor,
    string TaxInformation,
    SubscriptionPlan Subscription,
    OrganizationStatus Status,
    DateTimeOffset CreatedDate,
    string Code,
    string Phone,
    string Email,
    string Address,
    string TimeZone,
    string Language,
    string Currency);
