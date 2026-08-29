# ROJAN Reception (Desktop) — HANDOFF v1.0

**Prepared by:** Team 3 · **Date:** 2026-08-29 · **Phase:** 8.172
**Status:** Desktop code COMPLETE and frozen. Full public release gated on external items — see §21 and §20.

---

## 1. Project location

| | |
|---|---|
| Working copy (this engagement) | `C:\AndroidProjects\ROJAN_Desktop_team3` (a git worktree of the main checkout at `C:\AndroidProjects\ROJAN_Desktop`) |
| Solution file | `Rojan.Desktop.sln` at the repo root |
| Source | `src/` — 6 projects (`Common`, `Domain`, `Application`, `Infrastructure`, `Presentation`, `Shell`) |
| Tests | `tests/` — 6 projects (`Domain.Tests`, `Application.Tests`, `Presentation.Tests`, `Infrastructure.Tests`, `Shell.Tests`, `ArchitectureTests`) |
| Build / packaging scripts | `build/` (`publish.ps1`, `publish-installer.ps1`, `get-version.ps1`, `generate-icon.ps1`, `installer/RojanReception.iss`) |
| CI | `.github/workflows/ci.yml` (PR gate), `.github/workflows/release.yml` (tag → packaged GitHub Release) |
| Team 3 audit trail | `docs/team3/` (README + `phases/` + `checkpoints/`) |

## 2. Repository information

| | |
|---|---|
| Remote | `origin` → `https://github.com/meisamelh66-cpu/ROJAN_Desktop.git` |
| Default branch | `main` |
| `main` tip (code baseline) | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) — `merge: supersede origin/main Service Catalog + Shift Engine fork` (tree byte-identical to the pre-merge branch tip `58a2c88`) |
| Existing tag | `v1.0.0` → `d518218` (earlier Productionization checkpoint, predates the Team 3 hardening) |
| Release tag (this phase) | `ROJAN-DESKTOP-v1.0.0` → the final Team 3 baseline commit (see §4) |

## 3. Branch information

| | |
|---|---|
| Team 3 branch | `feature/team3-desktop-completion` |
| Relationship to `main` | contains everything on `main` (`77414de`) plus documentation-only commits (`docs/team3/**`). `src/` and `tests/` trees are byte-identical to `main`. |

## 4. Final commit SHA

- **Code baseline (on `main`):** `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1`
- **Team 3 final baseline commit (branch, includes the full `docs/team3/` audit trail):** recorded in `ROJAN_PHASE8_172_DESKTOP_HANDOFF_FINAL_REPORT_v1.md` §B and tagged `ROJAN-DESKTOP-v1.0.0`. `src/` + `tests/` identical to `77414de`.

## 5. Build instructions

```powershell
# from the repo root
dotnet restore Rojan.Desktop.sln
dotnet build   Rojan.Desktop.sln -c Release --no-restore
dotnet test    Rojan.Desktop.sln -c Release --no-build
```

`Directory.Build.props` sets `TreatWarningsAsErrors=true`, `Deterministic=true`, `AnalysisMode=Recommended` solution-wide — a warning fails the build.

## 6. Required tools

| Tool | Purpose | Notes |
|---|---|---|
| .NET SDK 8.0.x | build / test / publish | no `global.json` — any 8.0.x SDK works; validated with `8.0.424` |
| PowerShell 7 (`pwsh`) | `build/*.ps1` scripts | Windows PowerShell 5.1 also runs them |
| Inno Setup 6 | installer packaging (`publish-installer.ps1`) | `winget install --id JRSoftware.InnoSetup`; script auto-detects `%ProgramFiles(x86)%\Inno Setup 6\ISCC.exe`, `%ProgramFiles%\…`, or `%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe` |
| Windows SDK `signtool.exe` | code signing (optional) | only needed when passing `-CertificatePath`; preinstalled on GitHub `windows-latest` runners |
| Windows 10/11 x64 | target platform | `net8.0-windows`, WPF |

## 7. .NET SDK / runtime versions

| | |
|---|---|
| Target framework | `net8.0-windows` (WPF) |
| Runtime identifier | `win-x64` |
| Deployment | **self-contained, single-file** (`--self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`) — the end-user machine needs **no** .NET runtime installed |
| Validated SDK | `8.0.424` (`C:\Program Files\dotnet\sdk`) |
| Validated shared runtimes (build machine only) | `Microsoft.NETCore.App 8.0.30`, `Microsoft.WindowsDesktop.App 8.0.30` |

## 8. Debug build command

```powershell
dotnet build Rojan.Desktop.sln -c Debug
```

## 9. Release build command

```powershell
dotnet build Rojan.Desktop.sln -c Release
```

## 10. Test command

```powershell
dotnet test Rojan.Desktop.sln -c Release --no-build
# or per-project, e.g.:
dotnet test tests/Rojan.Desktop.ArchitectureTests -c Release
```

## 11. Installer build command

```powershell
# unsigned (the only path exercised so far — no certificate):
pwsh build/publish-installer.ps1
#   -> fresh self-contained single-file win-x64 Release publish (build/publish.ps1)
#   -> publish/Rojan.Desktop.Shell.exe  +  artifacts/RojanDesktop-v1.0.0-win-x64.zip
#   -> ISCC.exe build/installer/RojanReception.iss  ->  artifacts/ROJAN Reception Setup.exe

# signed (needs an Authenticode .pfx + signtool):
pwsh build/publish-installer.ps1 -CertificatePath <path.pfx> -CertificatePassword <pw>
#   signs the payload exe, the installer, and the embedded uninstaller (RFC-3161 timestamped)

# ZIP only, no installer:
pwsh build/publish.ps1
```

Version for every artifact comes from `Directory.Build.props` `<VersionPrefix>` via `build/get-version.ps1` — the single source of truth. `release.yml` runs this exact chain on a `v*` tag and verifies the tag equals the committed version.

## 12. Environment configuration

**No `appsettings.json` is shipped.** The API base address is resolved by `Rojan.Desktop.Infrastructure.Api.ApiEnvironmentService.ResolvedBaseAddress`, in this precedence:

1. **`ROJAN_API_BASE_URL`** environment variable (absolute URL) — overrides everything.
2. Persisted selection in `%LOCALAPPDATA%\RojanDesktop\api\environment.json` (written by Settings → API environment; **restart-required**).
3. Hardcoded default: **`ApiEnvironment.Development` → `http://localhost:8080`**.
4. When the environment is `Production` with no custom URL: `ApiEnvironmentService.ProductionUrlDefault` = **`https://api.rojanai.ir`**.

A fresh install has no settings file → **Development / `http://localhost:8080`**. See §20 and §21 for the open decision (external gate B7).

Other per-user data (all under `%LOCALAPPDATA%\RojanDesktop\`):
`database\rojan.db` (SQLite), `identity\device.json` (device/install identity), `security\` (DPAPI-encrypted session), `logs\` (Warning+ file log), `api\environment.json`, plus theme/language settings files.

## 13. Backend integration points

| Concern | Endpoint(s) | Client |
|---|---|---|
| OTP request | `POST {base}/api/v1/auth/otp/request` | `AuthBootstrapHttpClient` |
| OTP resend | `POST {base}/api/v1/auth/otp/resend` | `AuthBootstrapHttpClient` |
| OTP verify → tokens | `POST {base}/api/v1/auth/otp/verify` | `AuthBootstrapHttpClient` |
| Email/password login → tokens | `POST {base}/api/v1/auth/login` | `AuthBootstrapHttpClient` |
| Token refresh | `POST {base}/api/v1/auth/refresh` | `AuthBootstrapHttpClient` (via `BackendSessionService`) |
| All other domain calls (salon, dashboard, customers, services, specialists, booking, calendar, QR, support, automation) | various `{base}/api/v1/...` | `HttpApiClient` (`IApiClient`) — auth header, retry, connectivity short-circuit, 401→refresh→retry |

Contract types: `src/Rojan.Desktop.Application/Api/Contracts/`.
Domains with **no backend yet** (Desktop runs on `Fake*Repository`): Inventory, HR, Accounting, POS — see §20.

## 14. Authentication flow

Primary (mobile OTP), driven by `MobileOtpLoginViewModel`:

```
phone entry ──normalize→ E.164──▶ RequestOtpAsync ──▶ POST /auth/otp/request
                                                        │
   ◀──────────── OtpChallenge (expiry, resend cooldown) ┘
code entry ──▶ SignInWithOtpAsync ──▶ POST /auth/otp/verify
                                        │
   AuthResponse { user, accessToken(+expiry), refreshToken(+expiry) }
                                        ▼
   BackendSessionService.CreateSessionFromTokensAsync  ──▶ session state = Authenticated
                                        ▼
                                  Dashboard / main shell
```

Secondary: email + password → `POST /auth/login` (same token handling).
Device identity (`IDeviceRegistrationService`) is ensured before verify/login and sent with the request.

## 15. Token / session handling

- **`BackendSessionService`** (`ISessionService`) owns the access + refresh token pair and the `AuthenticationState`.
- **Persistence:** `ISecureStorageService` → **`DpapiSecureStorageService`** — the `PersistedSession` JSON (session identity + both tokens) is encrypted with **Windows DPAPI** (`DataProtectionScope.CurrentUser`) and stored under `%LOCALAPPDATA%\RojanDesktop\security\`. (The legacy `LocalSessionService` wrote plaintext and is unreferenced.)
- **Access-token attachment:** `HttpApiClient.AttachAuthenticationHeader` adds `Authorization: Bearer <accessToken>` only while the token is unexpired.
- **Refresh:** on a `401`, `HttpApiClient` calls `BackendSessionService.RefreshAsync` → `POST /api/v1/auth/refresh` with the refresh token, updates storage, retries the original request once. The two auth-bootstrap calls go through `AuthBootstrapHttpClient` (never the generic pipeline) to avoid refresh recursion.
- **Sign-out / expiry:** `SessionService.ExpireAsync` clears state and storage.

## 16. Error handling layer

- **Transport → typed exceptions.** `AuthBootstrapHttpClient` and `HttpApiClient` map failures to `Rojan.Desktop.Application.Api` types: `ApiConnectivityException` (no base address / offline / `HttpRequestException`), `ApiTimeoutException` (request exceeded 30 s), `ApiAuthenticationException` (401/403, carries status), `ApiRateLimitException` (429), `ApiException` (any other non-2xx — message includes status + body for the *log*, not the UI).
- **ViewModel boundary.** Every backend-connected, user-triggered command is wrapped in a guard that catches these and sets a safe state — `DashboardState.Error` + a **generic localized message** (`Strings.Common_ActionFailedMessage`, or a flow-specific string like `Strings.Login_Error_Network`). **58/58 Category-A error surfaces were sanitized** (Team 3 P2 track): no backend body, stack trace, URL, PII, financial data, AI prompt, automation payload, or token ever reaches a UI element.
- **Logging.** ViewModel `[LoggerMessage]` templates log **operation name only** (never the exception object). `HttpApiClient.LogFailure` logs category + method + relative path + status + exception *type name* — never a body or `Authorization` value. Only `App.LogUnhandledException` (Shell) and `HttpApiClient` deliberately log richer detail; both documented since Phase 8.15.
- **File log:** `LocalFileLoggerProvider` → `%LOCALAPPDATA%\RojanDesktop\logs\rojandesktop-yyyy-MM-dd.log`, **`LogLevel >= Warning` only**, written immediately (`File.AppendAllText`). A connectivity/timeout failure on the OTP screen is *not* logged (it is an expected state) — the directory is created at startup but stays empty until something at Warning+ occurs.

## 17. Retry logic

`Rojan.Desktop.Application.Security.RetryPolicy` (`IRetryPolicy`), used by `HttpApiClient` and `SyncQueueService`:

- **`MaxAttempts = 5`** (first try + up to 4 retries).
- Exponential backoff: delay before attempt *n* (n>1) = `500 ms · 2^(n-2)` **+ up to 100 ms jitter**.
- Retries transport/5xx/timeout-class failures; **not** 4xx (401 is handled separately by the refresh-and-retry-once path; 403/429 surface immediately).
- `AuthBootstrapHttpClient` has **no retry** — a login/OTP/refresh failure is reported immediately.

## 18. Offline behavior

- **`ConnectivityService`** (`IConnectivityService`) tracks `ConnectionState` (Online / Offline).
- `HttpApiClient.EnsureConnectivity` short-circuits: if `Offline`, it throws `ApiConnectivityException("No network connection is available.")` **without** attempting a socket — fast, no timeout wait.
- `AuthBootstrapHttpClient` deliberately has **no** connectivity short-circuit (it must always actually try — a wrong "offline" reading must never block sign-in), so the OTP path attempts the socket and maps the real failure.
- Unsent mutations queue in **`SyncQueueService`** (SQLite-backed) and replay with the retry policy when connectivity returns.
- The UI shows a generic localized message on any connectivity/timeout failure; it never distinguishes "server down" from "no internet" to the user.

## 19. Database initialization

- **`SqlitePersistenceOptions`** — default path `%LOCALAPPDATA%\RojanDesktop\database\rojan.db`, connection string `Data Source=<path>`.
- **`App.xaml.cs` (`OnStartup`)** — immediately after the host starts and **before** any other `InitializeAsync` or window construction:
  ```csharp
  using var db = _host.Services.GetRequiredService<IDbContextFactory<RojanDbContext>>().CreateDbContext();
  db.Database.MigrateAsync().GetAwaiter().GetResult();
  ```
  `MigrateAsync` creates the file if absent and applies every pending EF Core migration (never `EnsureCreated` — the `Migrations/` history is authoritative). WAL mode is used, so `rojan.db-wal` / `rojan.db-shm` appear alongside.
- Verified on this machine (Phase 8.168): a fresh install creates `rojan.db` (+ WAL) and `identity/device.json` on first launch with no backend reachable.

## 20. Known external dependencies

| Dependency | State | Owner |
|---|---|---|
| Backend API `https://api.rojanai.ir` | live; DNS + TCP-443 reachable. Auth/salon/dashboard/customers/services/specialists/booking/calendar/QR/support/automation contracts implemented. | Team 1 / Backend |
| Inventory / HR / Accounting / POS APIs | **do not exist** — Desktop runs on `Fake*Repository` with full layers + tests; ready to connect per contract. Not v1.0-blocking ("coming soon" scope). | Team 1 |
| POS `/charge` idempotency | unverified; `PosCheckoutViewModel.ChargeAsync` is re-chargeable after a failed payment. POS is out of v1.0 scope. | Product + Backend |
| Code-signing certificate | **not procured** — installer + uninstaller are unsigned (SmartScreen "Unknown Publisher"). Signing is fully wired (`publish-installer.ps1`, `.iss` `#ifdef SignInstaller`, `release.yml` secrets) — a parameter, not a redesign. | Release Engineering |
| SMS OTP delivery | backend-side. | Backend |

## 21. Future integration checklist

**Team 3 (Desktop) — COMPLETE.** Nothing outstanding: code frozen on `main` `77414de`, build 0/0 (Debug + Release), 2,715/2,715 tests + 7/7 architecture, installer built + install/first-run/uninstall validated on Windows 10, error surfaces 58/58 sanitized, audit trail archived.

**External — required before a full public v1.0 release** (see `docs/team3/phases/ROJAN_PHASE8_153_*` for the tracking board with exit conditions):

- [ ] **B1 — Code signing.** Procure an Authenticode certificate (EV recommended); run `publish-installer.ps1 -CertificatePath …` or set the `CODE_SIGNING_CERT_BASE64` / `CODE_SIGNING_CERT_PASSWORD` CI secrets. Verify `signtool verify /pa` succeeds + named publisher. — *Release Engineering*
- [ ] **B7 — First-launch API environment.** Decide: (1) flip the Release-build default to `https://api.rojanai.ir` (~5 LOC in `ApiEnvironmentService` + 1 test), (2) first-run onboarding prompt, or (3) ship as-is + document. Currently a fresh install points at `http://localhost:8080`. — *Product + DevOps*
- [ ] **B-DOCS — Release notes.** Replace `CHANGELOG.md` `## [Unreleased]` with a dated `## [1.0.0]` covering the hardening (draft in `ROJAN_PHASE8_151_*` TASK F); refresh `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md`; consolidate Known Issues. — *Team 3 draft → Product approve*
- [ ] **B8 — Product sign-off.** Ratify v1.0 scope (Inventory/HR/Accounting/POS as "coming soon"); approve the release checklist; authorize the release tag (naming the commit SHA + tag string; note the existing `v1.0.0` tag points at old `d518218`). — *Product*
- [ ] **B4 — Release pipeline.** Set the CI signing secrets; ensure the audit-trail commit is on `main`; push the authorized version tag → `release.yml` produces a signed installer + checksum + GitHub Release. Never yet run against a tag. — *DevOps*
- [ ] **B3 — Clean-VM install.** Install the signed `ROJAN Reception Setup.exe` on fresh Windows 10 **and** Windows 11 VMs with no .NET runtime; verify launch + shortcut + uninstall. — *QA / Release Engineering*
- [ ] **B2 — Live OTP login.** From a network with backend access, sign in with a real phone + OTP against `https://api.rojanai.ir`; confirm session creation and a real dashboard. — *QA*
- [ ] **Production deployment.** Publish the GitHub Release; sync `ROJAN_Web` `release-registry.ts` with the signed installer URL + SHA-256. — *Release Engineering*

**Connecting Inventory / HR / Accounting / POS (post-v1.0):** Team 1 publishes the API contracts → swap the `Fake*Repository` registrations in `Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` for real `Backend*Repository` implementations against `{base}/api/v1/...` → add integration tests. The Application/Presentation layers and their tests are already in place.
