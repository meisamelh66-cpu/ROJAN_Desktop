using Rojan.Server.Application.DependencyInjection;
using Rojan.Server.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Sprint 8 Commit 1: Backend Foundation. Logging/configuration/environment
// (appsettings.json -> appsettings.{Environment}.json -> User Secrets ->
// environment variables -> command line) all come for free from
// WebApplication.CreateBuilder - nothing custom needed for those.
builder.Services.AddControllers();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>Makes the top-level-statement-generated <c>Program</c> class public so <c>Rojan.Server.Api.Tests</c> can boot this app in-memory via <c>WebApplicationFactory&lt;Program&gt;</c> - no other behavior change.</summary>
public partial class Program
{
}
