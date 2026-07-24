using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Rojan.Server.Application.DependencyInjection;
using Rojan.Server.Infrastructure.DependencyInjection;
using Rojan.Server.Infrastructure.Security;

var builder = WebApplication.CreateBuilder(args);

// Sprint 8 Commit 1: Backend Foundation. Logging/configuration/environment
// (appsettings.json -> appsettings.{Environment}.json -> User Secrets ->
// environment variables -> command line) all come for free from
// WebApplication.CreateBuilder - nothing custom needed for those.
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. JWT bearer
// authentication - registered so a future commit's [Authorize]-protected
// endpoint can use it, but nothing in this commit requires it (the three
// auth endpoints below are explicitly anonymous). Reads the same Jwt:*
// configuration JwtTokenService itself binds via IOptions<JwtOptions> -
// duplicated here (rather than reading JwtOptions through DI) only
// because JwtBearerOptions needs the signing key at authentication-scheme
// registration time, before the service provider exists yet to resolve
// IOptions<JwtOptions> from. Fails fast on a missing signing key, the
// same posture AddInfrastructure already takes for a missing connection
// string - better than SymmetricSecurityKey's own less useful exception
// for an empty key.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey))
{
    throw new InvalidOperationException("Configuration key 'Jwt:SigningKey' is not configured.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>Makes the top-level-statement-generated <c>Program</c> class public so <c>Rojan.Server.Api.Tests</c> can boot this app in-memory via <c>WebApplicationFactory&lt;Program&gt;</c> - no other behavior change.</summary>
public partial class Program
{
}
