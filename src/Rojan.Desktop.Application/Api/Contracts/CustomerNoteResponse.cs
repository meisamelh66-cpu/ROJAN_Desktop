namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body ROJAN_Backend's <c>POST</c>/<c>GET .../notes</c>
/// endpoints return - matches that project's <c>CustomerNoteResponse</c>
/// field-for-field (see <c>api/customer/CustomerDtos.kt</c>).
/// <see cref="AuthorId"/> is whichever staff account wrote it - the backend
/// serves multiple logins per salon, unlike this single-owner desktop
/// client.
/// </summary>
public sealed record CustomerNoteResponse(string Id, string AuthorId, string Text, DateTimeOffset CreatedAt);

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/salons/{salonId}/customers/{id}/notes</c> accepts - matches ROJAN_Backend's <c>AddCustomerNoteRequest</c> field-for-field.</summary>
public sealed record AddCustomerNoteRequest(string Text);
