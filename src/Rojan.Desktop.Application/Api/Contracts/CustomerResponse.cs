namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body every ROJAN_Backend Customer CRM endpoint that
/// returns a customer sends - matches that project's <c>CustomerResponse</c>
/// field-for-field (see <c>api/customer/CustomerDtos.kt</c>).
/// <see cref="UserId"/> is the linked backend account, if any - null for a
/// walk-in customer with no app account; <c>Infrastructure.Customers.BackendCustomerRepository</c>
/// uses it to correctly resolve booking history and lifetime value, since
/// ROJAN_Backend's own booking records reference a <c>UserId</c>, not this
/// customer's own <c>Id</c>. <see cref="Status"/> stays a raw string rather
/// than a C# enum - same reasoning <see cref="BookingResponse.Status"/>
/// already established, mapped explicitly by the consuming repository.
/// </summary>
public sealed record CustomerResponse(
    string Id,
    string SalonId,
    string? UserId,
    string FullName,
    string? PhoneNumber,
    string? Email,
    string? Company,
    string Status,
    decimal LifetimeValue,
    IReadOnlyList<string> Tags,
    bool Active,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/customers</c> accepts - matches ROJAN_Backend's <c>CreateCustomerRequest</c> field-for-field. New customers always start as <c>LEAD</c> server-side, so there is no status field here.</summary>
public sealed record CreateCustomerRequest(string FullName, string? PhoneNumber, string? Email, string? Company);

/// <summary>The request body <c>PATCH {ApiVersion.BasePath()}/salons/{salonId}/customers/{id}</c> accepts - matches ROJAN_Backend's <c>UpdateCustomerRequest</c> field-for-field. Every field means "leave unchanged" when absent server-side, but <c>Infrastructure.Customers.BackendCustomerRepository.UpdateCustomerAsync</c> always sends every field, since <c>ICustomerCommandService.UpdateCustomerAsync</c> is itself a full-field replacement (see that method's own doc comment).</summary>
public sealed record UpdateCustomerRequest(string? FullName, string? PhoneNumber, string? Email, string? Company, string? Status);

/// <summary>The response body <c>POST/GET {ApiVersion.BasePath()}/salons/{salonId}/customers/identity</c> return - matches ROJAN_Backend's <c>CustomerIdentityResponse</c> field-for-field. Structurally narrower than <see cref="CustomerResponse"/>, not merely a subset in practice - there is no <c>Company</c>/tags/lifetime-value/status field to omit, because the type never carries one. See <c>Application.Customers.ICustomerIdentityService</c>.</summary>
public sealed record CustomerIdentityResponse(string Id, string SalonId, string FullName, string? PhoneNumber, string? Email, bool Active);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/customers/identity</c> accepts - matches ROJAN_Backend's <c>CreateCustomerIdentityRequest</c> field-for-field. The Reception-safe counterpart to <see cref="CreateCustomerRequest"/> - no <c>Company</c> field to even omit.</summary>
public sealed record CreateCustomerIdentityRequest(string FullName, string? PhoneNumber, string? Email);
