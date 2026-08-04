namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The <c>user</c> object nested inside <see cref="AuthResponse"/> - matches
/// ROJAN_Backend's <c>UserResponse</c> field-for-field. Mobile-First
/// Authentication: <see cref="Email"/> and <see cref="PhoneNumber"/> are both
/// nullable now - a phone-only account (created via <c>/auth/otp/verify</c>)
/// has no email, and an existing email/password account has no phone number
/// until it verifies one. At least one of the two is always present.
/// </summary>
public sealed record AuthUserResponse(string Id, string? Email, string? PhoneNumber, string FullName, string Role);

/// <summary>
/// The response body <c>POST {ApiVersion.BasePath()}/auth/login</c>,
/// <c>/auth/refresh</c>, and <c>/auth/otp/verify</c> all return - matches
/// ROJAN_Backend's <c>AuthResponse</c> field-for-field (see that project's
/// <c>api/auth/AuthDtos.kt</c>).
/// </summary>
public sealed record AuthResponse(
    AuthUserResponse User,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
