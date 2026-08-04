namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The request body <c>POST {ApiVersion.BasePath()}/auth/otp/verify</c>
/// accepts - matches ROJAN_Backend's <c>OtpVerifyRequest</c> field-for-field.
/// <see cref="FullName"/> is only used by the backend the first time
/// <see cref="PhoneNumber"/> completes verification (new-account creation);
/// ignored for an existing account.
/// </summary>
public sealed record OtpVerifyRequest(string PhoneNumber, string Code, string? FullName = null);
