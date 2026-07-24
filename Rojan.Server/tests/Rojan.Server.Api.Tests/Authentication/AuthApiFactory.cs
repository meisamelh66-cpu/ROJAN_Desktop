using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rojan.Server.Infrastructure.Persistence;

namespace Rojan.Server.Api.Tests.Authentication;

/// <summary>
/// Sprint 8 Commit 2: Tenant-Aware Authentication Foundation. A
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that boots the real
/// <c>Program</c> - real DI wiring, real controllers, real JWT bearer
/// setup - but swaps <see cref="RojanServerDbContext"/>'s Npgsql provider
/// for EF Core's InMemory one, so the register/login/refresh flow can be
/// exercised end-to-end without a live PostgreSQL instance. This is the
/// standard, documented pattern for testing an EF-Core-backed ASP.NET
/// Core API - <see cref="Program"/> itself is completely unaware its
/// database provider has been swapped.
///
/// The environment is forced to <c>Development</c> two ways (the process
/// environment variable directly, and <c>IWebHostBuilder.UseEnvironment</c>)
/// - <c>Program.cs</c> reads <c>Jwt:SigningKey</c> and throws immediately
/// if it is missing, before any <c>ConfigureWebHost</c> callback below
/// gets a chance to run, so the environment must resolve to
/// <c>Development</c> (where <c>appsettings.Development.json</c>'s local
/// placeholder key lives) before <c>Program.cs</c>'s own top-level code
/// executes, not after.
/// </summary>
public sealed class AuthApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"auth-tests-{Guid.NewGuid():N}";

    public AuthApiFactory()
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            var dbContextOptionsDescriptor = services.SingleOrDefault(descriptor => descriptor.ServiceType == typeof(DbContextOptions<RojanServerDbContext>));
            if (dbContextOptionsDescriptor is not null)
            {
                services.Remove(dbContextOptionsDescriptor);
            }

            services.AddDbContext<RojanServerDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
