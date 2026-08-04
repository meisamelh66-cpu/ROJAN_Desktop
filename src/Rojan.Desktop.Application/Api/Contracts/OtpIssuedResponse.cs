namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The response body <c>POST {ApiVersion.BasePath()}/auth/otp/request</c>
/// and <c>/auth/otp/resend</c> both return - matches ROJAN_Backend's
/// <c>OtpIssuedResponse</c> field-for-field.
/// </summary>
public sealed record OtpIssuedResponse(string PhoneNumber, long ExpiresInSeconds, long CanResendAfterSeconds);
