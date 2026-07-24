namespace Rojan.Server.Infrastructure.Security;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. Binds the
/// <c>Jwt</c> configuration section (<c>appsettings.json</c>/
/// <c>appsettings.Development.json</c>/User Secrets/environment
/// variables - same precedence every other configuration value in this
/// solution already uses, nothing custom). <see cref="SigningKey"/> is
/// empty in the committed <c>appsettings.json</c> - same "fail fast in
/// Production, real local default only in Development" treatment
/// <c>ConnectionStrings:DefaultConnection</c> already establishes (see
/// <c>Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure</c>'s
/// own doc comment).
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public int AccessTokenLifetimeMinutes { get; set; } = 60;
}
