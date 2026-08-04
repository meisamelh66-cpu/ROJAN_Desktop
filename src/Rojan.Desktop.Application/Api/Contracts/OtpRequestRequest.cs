namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>
/// The request body <c>POST {ApiVersion.BasePath()}/auth/otp/request</c> and
/// <c>/auth/otp/resend</c> both accept - matches ROJAN_Backend's
/// <c>OtpRequestRequest</c>/<c>OtpResendRequest</c> field-for-field (both are
/// identical shapes on the backend too - see that project's
/// <c>api/auth/OtpDtos.kt</c>). <see cref="PhoneNumber"/> must be E.164
/// (e.g. <c>+989123456789</c>) - the backend rejects anything else with 400.
/// </summary>
public sealed record OtpRequestRequest(string PhoneNumber);
