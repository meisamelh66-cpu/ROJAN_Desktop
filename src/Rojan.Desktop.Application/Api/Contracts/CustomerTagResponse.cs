namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body ROJAN_Backend's <c>POST</c>/<c>GET .../tags</c>
/// endpoints return - matches that project's <c>CustomerTagResponse</c>
/// field-for-field (see <c>api/customer/CustomerDtos.kt</c>). Carries the
/// tag's real server-generated <see cref="Id"/> - required by
/// <c>DELETE .../tags/{tagId}</c>, which is exactly what the
/// <c>GET .../tags</c> endpoint (Customer CRM Integration Preparation) was
/// added to make resolvable after a fresh profile load.
/// </summary>
public sealed record CustomerTagResponse(string Id, string Label, DateTimeOffset CreatedAt);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/customers/{id}/tags</c> accepts - matches ROJAN_Backend's <c>AddCustomerTagRequest</c> field-for-field.</summary>
public sealed record AddCustomerTagRequest(string Label);
