namespace Rojan.Desktop.Application.Api.Contracts;

/// <summary>The request body <c>POST {ApiVersion.BasePath()}/auth/login</c> accepts - matches ROJAN_Backend's <c>LoginRequest</c> field-for-field.</summary>
public sealed record LoginRequest(string Email, string Password);
