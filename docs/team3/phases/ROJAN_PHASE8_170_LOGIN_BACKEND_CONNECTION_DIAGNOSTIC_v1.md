# ROJAN_PHASE8_170 — LOGIN BACKEND CONNECTION DIAGNOSTIC — REPORT v1

**Phase:** 8.170 · **Type:** Diagnostic only — no code change · **Date:** 2026-08-29
**Machine:** DESKTOP-93E967G · Windows 10 Pro 19045 x64
**Subject:** Root cause of *"خطا در اتصال به سرور. اتصال اینترنت خود را بررسی کنید."* on the mobile-number login screen (the "ارسال کد" / send-OTP action)
**Build under test:** installed `ROJAN Reception 1.0.0` (`%LOCALAPPDATA%\Programs\ROJAN Reception\`), from `origin/main` `77414de` / local `da0c36b` · working tree clean, 0 source changes

---

## 1. EXECUTIVE SUMMARY

The error is **expected behaviour for a fresh install with no backend**, not a defect.

- On first launch the app defaults to the **Development** API environment → base address **`http://localhost:8080`**.
- Nothing is listening on `localhost:8080` on this machine, so `POST http://localhost:8080/api/v1/auth/otp/request` fails at the socket with **`SocketException: ConnectionRefused` (WinSock 10061)**.
- `AuthBootstrapHttpClient` maps that to `ApiConnectivityException`; `MobileOtpLoginViewModel.RequestCodeAsync` catches it and shows the generic localized string `Strings.Login_Error_Network` — the message observed.
- The production backend `https://api.rojanai.ir` **is DNS-resolvable and TCP-443-reachable from this machine, but HTTPS requests to it time out** (30 s, no response) in this environment. So switching to Production here produces the *same* message via a different path (`ApiTimeoutException`), just 30 s slower.

**No code fix is required for the message itself.** The open decision is the first-launch API-environment default (external gate **B7**) — a fresh production install should not point at `localhost`.

---

## 2. CURRENT API CONFIGURATION (as running)

| Item | Value | Source |
|---|---|---|
| **Active environment** | `ApiEnvironment.Development` | `ApiEnvironmentService.SelectedEnvironment` default — no persisted settings file |
| **Resolved API base URL** | **`http://localhost:8080`** | `ApiEnvironmentService.ResolvedBaseAddress` → `DevelopmentUrl` const |
| `ROJAN_API_BASE_URL` env var | **not set** (User / Machine / Process all empty) | `[Environment]::GetEnvironmentVariable(...)` |
| Persisted `environment.json` | **absent** (`%LOCALAPPDATA%\RojanDesktop\api\environment.json` does not exist) | fresh install → `InitializeAsync` reads nothing → Development |
| `appsettings.json` / app config | **none shipped** next to the installed exe | config precedence is env var → `environment.json` → hardcoded Development |
| Production URL (if switched) | `https://api.rojanai.ir` | `ApiEnvironmentService.ProductionUrlDefault` const (used when `ProductionUrl` override is null) |

**Config precedence** (`ApiEnvironmentService.ResolvedBaseAddress`):
`ROJAN_API_BASE_URL` env var  ▸  else `SelectedEnvironment == Development` → `http://localhost:8080`  ▸  else `ProductionUrl` override  ▸  else `https://api.rojanai.ir`.

---

## 3. THE OTP-SEND CALL PATH

| Layer | Detail |
|---|---|
| ViewModel | `MobileOtpLoginViewModel.RequestCodeAsync()` — normalizes the phone to E.164 (`09164987585` → `+989164987585`), then `await _authenticationService.RequestOtpAsync(phoneNumber)` |
| Service | `IAuthenticationService` → **`BackendAuthenticationService`** (DI: `ServiceCollectionExtensions.AddInfrastructure` line 264 — the real backend service, `LocalAuthenticationService` is unreferenced) |
| `BackendAuthenticationService.RequestOtpAsync` | `_authClient.PostAsync<OtpRequestRequest, OtpIssuedResponse>("/api/v1/auth/otp/request", new OtpRequestRequest(phoneNumber), ct)` |
| HTTP client | **`AuthBootstrapHttpClient`** (singleton) — a bare `new HttpClient(new HttpClientHandler())`, **not** `IHttpClientFactory`, **no** `ILogger`, **no** connectivity short-circuit (by design — see its doc comment). `Timeout = 30 s`. `BaseAddress` = `apiEnvironmentService.ResolvedBaseAddress`, **captured once at construction**. |
| **Endpoint actually hit** | **`POST http://localhost:8080/api/v1/auth/otp/request`**  ·  body `{"phoneNumber":"+989164987585"}`  ·  `Content-Type: application/json` |
| Verify endpoint (later step) | `POST {base}/api/v1/auth/otp/verify` · resend: `POST {base}/api/v1/auth/otp/resend` |

> Because `AuthBootstrapHttpClient` is a **singleton** and its `BaseAddress` is set in the constructor, an API-environment change made in Settings only takes effect after an app **restart** (`ApiEnvironmentService.IsRestartRequired` is set for exactly this reason).

---

## 4. THE REAL EXCEPTION (reproduced this phase)

Reproduced with a stand-alone `System.Net.Http.HttpClient` POST to the same URL/body/timeout the app uses (no repo code changed):

### 4a. `http://localhost:8080` — the current (Development) target

```
FAILED after ~2.3 s
System.Net.Http.HttpRequestException:
    No connection could be made because the target machine actively refused it. (localhost:8080)
  └─ System.Net.Sockets.SocketException:
        No connection could be made because the target machine actively refused it.
        SocketErrorCode = ConnectionRefused   NativeErrorCode = 10061
```

**In-app translation:**
`AuthBootstrapHttpClient.PostAsync` → `catch (HttpRequestException httpException)` →
`throw new ApiConnectivityException("Request failed: No connection could be made because the target machine actively refused it. (localhost:8080)", httpException)` →
`MobileOtpLoginViewModel.RequestCodeAsync` → `catch (ApiConnectivityException)` →
`ErrorMessage = Strings.Login_Error_Network`
= **"خطا در اتصال به سرور. اتصال اینترنت خود را بررسی کنید."** ✅ exact match to the screen.

### 4b. `https://api.rojanai.ir` — if switched to Production (from this machine)

```
DNS:  api.rojanai.ir → 185.8.173.194
TCP:  port 443 connect  → SUCCEEDS (Test-NetConnection TcpTestSucceeded = True)
HTTPS POST /api/v1/auth/otp/request → no response, hangs

FAILED after 30.0 s
System.Threading.Tasks.TaskCanceledException:
    The request was canceled due to the configured HttpClient.Timeout of 30 seconds elapsing.
  └─ System.TimeoutException: A task was canceled.
```

**In-app translation:**
`AuthBootstrapHttpClient.PostAsync` → `catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)` →
`throw new ApiTimeoutException("Request to '/api/v1/auth/otp/request' timed out after 30s.")` →
`MobileOtpLoginViewModel.RequestCodeAsync` → `catch (ApiTimeoutException)` →
`ErrorMessage = Strings.Login_Error_Network` — **same message**, after a 30 s wait.

> The 443 TCP handshake succeeding but the HTTP request hanging indicates **egress filtering in this environment** (consistent with the `api.rojanai.ir` timeouts seen in Phases 8.151 / 8.168). It is not evidence about the production server's own health — a networked QA machine (gate **B2**) is required to confirm that.

---

## 5. HttpClient LOGS

**There are none for this path — by design.**

| Fact | Evidence |
|---|---|
| `AuthBootstrapHttpClient` takes no `ILogger` and is not built via `IHttpClientFactory` | its constructor: `new HttpClient(new HttpClientHandler())` — no logging handler in the pipeline |
| `MobileOtpLoginViewModel.RequestCodeAsync` logs **only** the generic `catch (ApiException)` branch (`LogUnexpectedOtpApiFailure`) | `catch (ApiConnectivityException)` and `catch (ApiTimeoutException)` set `ErrorMessage` and **do not** call the logger — a connectivity/timeout failure is a known, expected state, not an "unexpected API failure" |
| The file logger persists only `LogLevel >= Warning` | `LocalFileLoggerProvider.Config.IsEnabled => logLevel >= LogLevel.Warning` |
| Result | `%LOCALAPPDATA%\RojanDesktop\logs\` is **created at startup but stays empty** for this scenario (matches Phase 8.168 Task E) — no `rojandesktop-2026-08-29.log` file is written |

To capture the failure in-app, one would need `Development`-level logging or a debugger; the `AuthBootstrapHttpClient` catch sites hold the real `HttpRequestException` as `InnerException` on the `ApiConnectivityException` but nothing writes it out.

---

## 6. localhost:8080 vs api.rojanai.ir — REACHABILITY FROM THIS MACHINE

| Target | DNS | TCP connect | HTTP result | App outcome |
|---|---|---|---|---|
| `http://localhost:8080` (current default) | n/a (loopback) | **refused** (10061) | — | `ApiConnectivityException` → network error msg, **~2 s** |
| `https://api.rojanai.ir` (production) | ✅ `185.8.173.194` | ✅ port 443 open | **times out (30 s, no response)** — egress-filtered in this env | `ApiTimeoutException` → **same** network error msg, **~30 s** |

Neither environment can currently complete an OTP request from this machine.

---

## 7. ROOT CAUSE

1. **Primary (why the message appears):** the app is pointed at `http://localhost:8080` (first-launch Development default) and no backend is running there → `ConnectionRefused`. The UI message is the correct, sanitized surface for that.
2. **Secondary (why switching wouldn't help *here*):** `https://api.rojanai.ir` is not usably reachable from this machine/network — HTTPS requests time out despite the TCP port being open. This is an environment egress limitation, not necessarily a server fault.
3. **Design note (not a bug):** the OTP bootstrap client is a singleton with a construction-time base address, so any environment change needs an app restart to take effect.

---

## 8. RECOMMENDATIONS (no change made this phase)

| # | Action | Owner | Notes |
|---|---|---|---|
| R1 | **Decide the first-launch API-environment default** (external gate **B7**). Option 1: default Release builds to `Production` (`https://api.rojanai.ir`) — ~5 lines in `ApiEnvironmentService` + 1 test. Option 2: force the choice in first-run onboarding. Option 3: document that staff must set it in Settings. | Product + DevOps | A fresh production install currently cannot reach any backend without a manual Settings change + restart. |
| R2 | For local verification now: set `ROJAN_API_BASE_URL` to a reachable backend (a dev instance, or production from a permitted network) and relaunch — it overrides everything with no settings change. | QA / dev | Env var is read live in `ResolvedBaseAddress` but still consumed at client construction, so relaunch is needed. |
| R3 | Confirm the real OTP round-trip against `https://api.rojanai.ir` from a **networked QA machine** (external gate **B2**) — this machine can't, and the 443-open/HTTP-timeout pattern here is inconclusive about server health. | QA | |
| R4 | *(Optional, minor)* Consider logging `ApiConnectivityException` / `ApiTimeoutException` on the OTP path at `Warning` (currently silent) so field diagnostics have a trail. Small, deferred — not required for the message to be correct. | Team 3 (future phase) | |
| R5 | *(Optional, minor)* The phone field is pre-filled with the last-used number on a non-clean profile (empty on a truly fresh install) — remembered local state, unrelated to the connection error. No action needed. | — | |

---

## 9. VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / `.csproj` / build-logic changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits / merges / tags | ❌ none |
| Tracked working tree | 0 dirty (HEAD `da0c36b`) |
| Machine changes | none (diagnostic probes only; ROJAN Reception 1.0.0 remains installed from Phase 8.169) |
| Files created this phase | `ROJAN_PHASE8_170_LOGIN_BACKEND_CONNECTION_DIAGNOSTIC_v1.md` + `scratchpad/p170_probe.ps1` (throwaway probe, outside the repo) |

---

**Diagnostic complete. No code changed. STOP.**
