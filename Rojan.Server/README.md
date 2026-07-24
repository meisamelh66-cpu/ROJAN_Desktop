# Rojan.Server

ASP.NET Core backend for ROJAN, built with Clean Architecture, matching
the layering convention already established by the desktop solution
(`RojanDesktop.sln`) at the repo root.

## Status: Sprint 8 Commit 1 - Backend Foundation

This commit is infrastructure foundation only. There is no business
implementation yet: no Customer/Specialist/Service/Booking APIs, no
authentication, no JWT, no refresh tokens, no sync worker. Only one
controller exists (`GET /health`). Everything here exists to make the
*next* commit (the first real vertical slice) a matter of adding code
inside an already-working, already-tested shell - not standing up the
shell itself.

## Structure

```
Rojan.Server.sln
Directory.Build.props        # shared MSBuild settings (net8.0, nullable, warnings-as-errors)
Directory.Packages.props     # Central Package Management, isolated from the desktop solution
docker-compose.yml           # local dev only: Postgres + the API
src/
  Rojan.Server.Domain          # no outer-layer dependencies. No business entities yet.
  Rojan.Server.Application     # orchestration only. No outer-layer dependencies except Domain.
  Rojan.Server.Infrastructure  # EF Core/PostgreSQL, persistence. Depends on Application + Domain.
  Rojan.Server.Api             # ASP.NET Core host/controllers. Depends on Application + Infrastructure.
tests/
  Rojan.Server.Application.Tests
  Rojan.Server.Infrastructure.Tests
  Rojan.Server.Api.Tests        # boots the real app via WebApplicationFactory<Program>
```

Same dependency direction the desktop solution enforces via its own
`ArchitectureTests`: Domain depends on nothing, Application depends only
on Domain, Infrastructure depends on Application + Domain, Api depends on
Application + Infrastructure.

## Why a separate `Directory.Build.props`/`Directory.Packages.props`

The repo-root `Directory.Build.props`/`Directory.Packages.props` govern
the desktop solution (`net8.0-windows`, its own Central Package
Management catalog). MSBuild/NuGet resolve these files by walking up from
a project's own directory and stopping at the **first** one found - so
`Rojan.Server/Directory.Build.props` and `Rojan.Server/Directory.Packages.props`
fully shadow the desktop's for everything under this folder, keeping the
two solutions' build configuration and package versions completely
independent. **Do not delete either file** - without them, `dotnet add
package` here would silently write into the desktop's central package
file instead (this happened once while setting this project up and was
reverted - see git history if curious).

## Running locally

### Without Docker

Requires a local PostgreSQL instance reachable at the connection string
in `appsettings.Development.json` (defaults to
`Host=localhost;Port=5432;Database=rojan_dev;Username=postgres;Password=postgres` -
not a secret, just a local dev default; override for anything real, see
below).

```
dotnet run --project src/Rojan.Server.Api
```

Then `GET http://localhost:5213/health` (or whatever port `dotnet run`
reports) returns `{"status":"ok"}`.

### With Docker

```
docker compose up
```

Starts PostgreSQL 16 and the API together; the API is on `http://localhost:8080/health`.

## Overriding the connection string for anything real

Never put a real connection string/credential in `appsettings.json` or
`appsettings.Development.json` - both are committed to source control.
Use one of:

- User Secrets (already initialized - `dotnet user-secrets set
  "ConnectionStrings:DefaultConnection" "..." --project src/Rojan.Server.Api`)
- The `ConnectionStrings__DefaultConnection` environment variable (double
  underscore - ASP.NET Core's standard nested-key convention)

## Migrations

No migration has been generated yet - `RojanServerDbContext` has zero
`DbSet<T>` properties today (no business entities exist, per this
commit's own scope). The EF Core tooling is fully wired though
(`RojanServerDbContextFactory` implements `IDesignTimeDbContextFactory<T>`
so `dotnet ef` works without running the full host):

```
dotnet ef migrations add InitialCreate --project src/Rojan.Server.Infrastructure --startup-project src/Rojan.Server.Api
```

This is deliberately not run yet - an empty migration adds no value.
Whichever future commit adds the first real entity should run this then.
