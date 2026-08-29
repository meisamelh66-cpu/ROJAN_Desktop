# ROJAN AI — TEAM 3 — PHASE 8.14 MOBILE OTP LOGGING HARDENING — SCOPE REVIEW v1

**Type:** Audit + scope only. **No source modified, no logger added, no DI change, no auth-flow change,
no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `2453a7f` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_9_LOGGING_COVERAGE_AUDIT_v1.md`, `ROJAN_PHASE8_10_LOGGING_HARDENING_SCOPE_REVIEW_v1.md` §P4

Every claim below was verified against source this turn.

---

## A. Current Auth Logging State (Task 1 — revalidated from source)

### A.1 `MobileOtpLoginViewModel` today

| Item | Verified state |
|---|---|
| Class | `public sealed class MobileOtpLoginViewModel : ViewModelBase` (not `partial`) |
| Constructor | `MobileOtpLoginViewModel(IAuthenticationService authenticationService, IDelayScheduler delayScheduler)` — **2 dependencies, no `ILogger`** |
| `ILogger` usage | **None anywhere in the file** |
| Fields | `_authenticationService`, `_delayScheduler`, plus UI state (`_phoneNumber`, `_code`, `_errorMessage`, `_isBusy`, `_isCodeSent`, `_canResend`, `_resendCooldownHandle`) |
| DI registration | `services.AddTransient<MobileOtpLoginViewModel>()` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:54`); composed by `LoginWindowViewModel(MobileOtpLoginViewModel)` via DI |
| Test file | `tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs` — ~24 direct `new MobileOtpLoginViewModel(service, scheduler)` sites, no `MakeSut` helper |
| Test stub | `tests/.../Security/StubAuthenticationService.cs` — already has `RequestOtpExceptionToThrow` / `ResendOtpExceptionToThrow` / `SignInWithOtpExceptionToThrow` settable properties, and `LastPhoneNumber` / `LastCode` / `*CallCount` recorders |

### A.2 The three async flows and their exception handling (line-exact)

Each flow: input validation → `try { await _authenticationService.<call> }` → **typed catches** → generic
fallthrough → `finally { IsBusy = false; }`.

| Flow | API call | `catch` ladder (verified) | Generic fallthrough |
|---|---|---|---|
| `RequestCodeAsync` | `RequestOtpAsync(phone)` | `ApiRateLimitException` → `Login_Mobile_Error_RateLimited`; `ApiConnectivityException` → `Login_Error_Network`; `ApiTimeoutException` → `Login_Error_Network` | `catch (ApiException)` → `Login_Error_Generic` (~line 288) |
| `ResendCodeAsync` | `ResendOtpAsync(phone)` | same three | `catch (ApiException)` → `Login_Error_Generic` (~line 329) |
| `VerifyCodeAsync` | `SignInWithOtpAsync(phone, code)` | `ApiAuthenticationException exception` → 403 ? `Login_Mobile_Error_NotAuthorized` : `Login_Mobile_Error_InvalidCode`; `ApiRateLimitException` → `RateLimited`; `ApiConnectivityException`/`ApiTimeoutException` → `Login_Error_Network` | `catch (ApiException)` → `Login_Error_Generic` (~line 396) |

- **No `catch (Exception)` anywhere** — a non-`ApiException` fault propagates to
  `App.OnDispatcherUnhandledException` (which logs at `Error` and shows a dialog).
- Validation paths (`ClassifyInvalidPhoneNumber`, missing-phone, missing-code, E.164 regex) `return`
  **before** the `try` block — no API call, and they hold **phone-derived data**.

### A.3 What already logs upstream — and what does NOT

| Layer | Logging |
|---|---|
| `HttpApiClient` | logs every failure category at `Warning` (`LogApiRequestFailed`, method+path+statuscode+exception-type, **never body/headers** — verified doc comment + all 6 `LogFailure` call sites) |
| **`AuthBootstrapHttpClient`** | **NO logging of any kind** — no `ILogger`, no `LogFailure`. Verified: the class has zero logging members |
| **OTP routing** | `BackendAuthenticationService` calls OTP endpoints through **`AuthBootstrapHttpClient`** (`_authClient`), **not** `HttpApiClient` — verified `BackendAuthenticationService.cs:65/74/86` |

**Consequence (key finding):** unlike Wave 1 (Dashboard/Calendar/Accounting, where `HttpApiClient`
already provided a `Warning` trail), **OTP API failures currently leave *zero* diagnostic trail
anywhere.** The only artifact of a failed OTP request is the on-screen `ErrorMessage`. This *raises* the
value of ViewModel-level logging here — it will be the sole trail — but does **not** relax the data-safety
constraints in §D.

---

## B. Safe Logging Design (Task 2 + Task 3)

### B.1 What to log — the generic `catch (ApiException)` fallthrough ONLY

One `[LoggerMessage]`, called from the generic fallthrough of all three flows, **passing the operation
name only — never the exception object, never its message:**

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed partial class MobileOtpLoginViewModel : ViewModelBase
{
    private readonly ILogger<MobileOtpLoginViewModel> _logger;

    public MobileOtpLoginViewModel(
        IAuthenticationService authenticationService,
        IDelayScheduler delayScheduler,
        ILogger<MobileOtpLoginViewModel>? logger = null)   // optional, appended last
    {
        // existing assignments unchanged
        _logger = logger ?? NullLogger<MobileOtpLoginViewModel>.Instance;
    }

    // in each of the 3 `catch (ApiException)` blocks, AFTER the unchanged
    //   ErrorMessage = Strings.Login_Error_Generic;
    // add:
    //   LogUnexpectedOtpApiFailure(nameof(RequestCodeAsync));   // / ResendCodeAsync / VerifyCodeAsync

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning,
        Message = "OTP flow failed with an unexpected API error. Operation={Operation}")]
    private partial void LogUnexpectedOtpApiFailure(string operation);
}
```

- **No `Exception` parameter.** Deliberate — see §D.1. The generic `catch (ApiException)` catches the
  base type, whose message (from `AuthBootstrapHttpClient.cs:101`) is
  `"Request failed with status {code}: {responseBody}"` — **it embeds the raw backend response body.**
- `{Operation}` is a compile-time `nameof(...)` — a method name, never user or session data.
- Instance-form `[LoggerMessage]` (one logger field — matches `BookingPageViewModel`).

### B.2 What NOT to log, and why (per-path decision — Task 2)

| Failure path | Log? | Level | Reason |
|---|---|---|---|
| generic `catch (ApiException)` (all 3 flows) | **YES** | **Warning** | The "backend did something we didn't map" case — genuinely unexpected, low frequency, the one path worth a trail. Warning matches `HttpApiClient.LogApiRequestFailed`'s own level for API failures |
| `catch (ApiConnectivityException)` | **NO** | — | Expected, extremely common at a pre-session login screen (user offline). Self-explanatory to the user; logging every one = pure noise |
| `catch (ApiTimeoutException)` | **NO** | — | Expected; message *is* safe (`"Request to '/api/v1/auth/otp/request' timed out…"` — relative path, no body), but still low-value noise. (If a backend-health signal is later wanted, this is the safe one to add — flagged, not included) |
| `catch (ApiRateLimitException)` | **NO** | — | Expected (429). Normal impatient-user behaviour (repeated resend clicks). User-actionable message already shown. Backend owns rate-limit telemetry |
| `catch (ApiAuthenticationException)` (VerifyCode 401/403) | **NO** | — | **Deliberately not logged.** 401 (wrong/expired code) is the highest-frequency normal outcome — per-attempt logging would be noise *and* a per-session failed-auth signal that edges toward the "sensitive authentication data" this phase must avoid. 403 (INACTIVE_USER) is rarer but the backend is the correct owner of account-state audit |
| validation / normalization paths (missing phone, `ClassifyInvalidPhoneNumber`, E.164 reject, missing code) | **NO** | — | `return` before any API call; hold phone-derived data. **Must never be logged** |

Levels considered: `Information` is unusable here — `LocalFileLoggerProvider` drops everything below
`Warning`. So the real choice per path is "Warning or nothing". `Error` was considered for the generic
branch (Wave 1 used `Error` for swallowed unexpected failures) — **Warning is recommended** because this
is an API-protocol anomaly with a clean user recovery, on a pre-session screen, and consistency with
`HttpApiClient`'s API-failure level is the stronger precedent. `Error` is a defensible alternative if the
authorizer prefers cross-wave uniformity; both are equally safe.

### B.3 Architecture review (Task 3)

| Check | Result |
|---|---|
| `ILogger<T>` pattern compatibility | Established — 7 precedents post-Wave-1. Instance form, optional ctor param, `[LoggerMessage]` |
| `NullLogger<T>` fallback | `_logger = logger ?? NullLogger<MobileOtpLoginViewModel>.Instance`. `Microsoft.Extensions.Logging.Abstractions` already a Presentation `PackageReference`. Non-breaking for the ~24 existing direct-`new` tests |
| No DI architecture change | `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) already registers open-generic `ILogger<T>`; `MobileOtpLoginViewModel` is `AddTransient` and `LoginWindowViewModel` composes it via DI → the real logger is injected automatically. **Zero DI edits.** |
| No interface change | No interface is involved — `IAuthenticationService`, `IDelayScheduler`, `IApiClient`, `AuthBootstrapHttpClient` all untouched. The change is entirely inside the concrete ViewModel |
| No auth-flow modification | The log call is appended **after** the unchanged `ErrorMessage = Strings.Login_Error_Generic;` line inside an *already-existing* catch. No change to: `SignInWithOtpAsync`/`RequestOtpAsync`/`ResendOtpAsync` calls, the `SignedIn` event, `IsBusy`, `IsCodeSent`, the resend cooldown, phone normalization, validation, or the 403-vs-401 branch. No catch removed, no exception rethrown or suppressed differently |
| `DependencyDirectionTests` | `Microsoft.Extensions.Logging.Abstractions` is not forbidden (only Infrastructure/Domain/Shell/EF) |
| `ViewModelTestabilityTests` | no `System.Windows.Threading`/`Controls` dependency added |
| Architecture suite | **7/7 expected unchanged** |

---

## C. Exact Files

| Category | Count | File | Change |
|---|---|---|---|
| Production | **1** | `src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs` | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<MobileOtpLoginViewModel> _logger` field; ctor +3rd optional param + `NullLogger` fallback; +1 `[LoggerMessage]`; +3 call sites (one per flow's generic `catch (ApiException)`) — est. **+14 / −4 LOC** |
| Test | **1** | `tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs` | +1 `using` (`Rojan.Desktop.Presentation.Tests.Specialists` for `RecordingLogger<T>`); +~5 tests. The ~24 existing `new MobileOtpLoginViewModel(...)` sites are **untouched** (optional 3rd param) |
| DI / interface / new files | **0** | — | — |

**Total file impact: 2.** `RecordingLogger<T>` already exists and is reused cross-namespace — no new test
infra.

---

## D. Security Validation (Task 4)

### D.1 The one real risk, and how the design eliminates it

`AuthBootstrapHttpClient` (the client OTP actually uses) builds exception messages that **embed the raw
backend response body**:

| Line | Throw |
|---|---|
| `AuthBootstrapHttpClient.cs:91` | `new ApiAuthenticationException($"Request was rejected with status {code}: {responseBody}", code)` |
| `AuthBootstrapHttpClient.cs:96` | `new ApiRateLimitException($"Request was rate-limited: {responseBody}")` |
| `AuthBootstrapHttpClient.cs:101` | `new ApiException($"Request failed with status {code}: {responseBody}")` |

`{responseBody}` is the backend's uncontrolled error JSON. We cannot assume Team 1's OTP error responses
never echo the submitted phone number ("no pending challenge for +9891…") — and that contract can change.

**Mitigation: the design never passes the `Exception` (or its `.Message`) to the logger.** Only
`nameof(<method>)` is logged. `LocalFileLoggerProvider` therefore writes exactly:

```
<timestamp> [Warning] Rojan.Desktop.Presentation.ViewModels.Security.MobileOtpLoginViewModel: OTP flow failed with an unexpected API error. Operation=RequestCodeAsync
```

— and nothing else.

### D.2 Confirmed absent from every logged record

| Sensitive item | In the log? | Why not |
|---|---|---|
| **Phone numbers** | **No** | Not in the template (`nameof` only); exception object not passed; validation paths (which hold phone data) not logged |
| **OTP codes** | **No** | `_code` is never referenced by any logging call; `VerifyCodeAsync`'s catch logs only the operation name |
| **Tokens** (access / refresh) | **No** | Tokens live in `SessionService`/`AuthResponse`, never touched by this ViewModel; not in scope of any log call |
| **User identifiers** (user id, email, full name) | **No** | Resolved only *after* a successful `SignInWithOtpAsync` (in `BackendAuthenticationService`), never on the failure path this logs |
| **Session data** | **No** | No session exists yet on the OTP screen (pre-authentication); nothing session-shaped is referenced |
| **Backend response body** | **No** | The exception carrying it is never passed to the logger (§D.1) |
| **Request URL with query** | **No** | OTP is `POST` with phone/code in the JSON **body**, not the URL (`BackendAuthenticationService.cs:66/75/87`); and no URL is logged regardless |

### D.3 Sink safety (unchanged, restated)

`LocalFileLoggerProvider` — `%LocalAppData%\RojanDesktop\logs\`, `Warning`+ only, 14-day retention,
fail-safe writes, **write-only** (never read back by the app). A `Warning` line naming only an operation
cannot become a credential-exposure or second-authority path.

---

## E. Test Plan (Task 4)

All tests use the existing `StubAuthenticationService` (`*ExceptionToThrow` properties) + `RecordingLogger<T>`.

| # | Test | Setup | Assertion |
|---|---|---|---|
| 1 | `RequestCodeCommand_UnexpectedApiException_LogsWarning` | `RequestOtpExceptionToThrow = new ApiException("boom")` | `ErrorMessage == Login_Error_Generic` (**unchanged**) **and** `logger.Entries` has a `Warning` containing `"RequestCodeAsync"` |
| 2 | `VerifyCodeCommand_UnexpectedApiException_LogsWarning` | `SignInWithOtpExceptionToThrow = new ApiException("boom")` | `ErrorMessage == Login_Error_Generic` **and** `Warning` containing `"VerifyCodeAsync"` |
| 3 | `ResendCodeCommand_UnexpectedApiException_LogsWarning` | (send a code first) `ResendOtpExceptionToThrow = new ApiException("boom")` | `Warning` containing `"ResendCodeAsync"` |
| 4 | `RequestCodeCommand_TypedApiFailure_DoesNotLog` *(negative)* | `RequestOtpExceptionToThrow = new ApiRateLimitException("429")` | `ErrorMessage == Login_Mobile_Error_RateLimited` (**unchanged**) **and** `logger.Entries` is **empty** — proves typed/expected branches never log |
| 5 | `VerifyCodeCommand_InvalidCode_DoesNotLog` *(negative)* | `SignInWithOtpExceptionToThrow = new ApiAuthenticationException("401", statusCode: 401)` | `ErrorMessage == Login_Mobile_Error_InvalidCode` **and** `logger.Entries` is **empty** — proves auth failures are never logged |
| 6 | `NoLoggerSupplied_UsesNullLogger_ApiFailureNeverThrows` *(safety)* | `RequestOtpExceptionToThrow = new ApiException("boom")`, **no logger passed** | `Record.Exception(...)` is `null` |

**Existing flows unchanged — regression:** the current tests
`RequestCodeCommand_NetworkFailure_ShowsNetworkErrorMessage` (Connectivity/Timeout),
`RequestCodeCommand_...` generic-`ApiException` → generic message, `RequestCodeCommand_RateLimited_...`,
`VerifyCodeCommand_...` (401/403/429), phone normalization, validation-message tests, `SignedIn` on
success — **all must pass unchanged** (the log call is additive after the existing `ErrorMessage` line;
`NullLogger` default = no observable change). Explicitly re-run, not assumed.

**Total new tests: ~6.** Expected suite after: **2,521 + ~6 ≈ 2,527**, 0 failures. Presentation.Tests
578 → ~584.

**Implementation-time check from Phase 8.10 — RESOLVED:** raw `ApiException` **is** constructible from
the test assembly (`public ApiException(string message)`) and is **already used** in the existing test
`MobileOtpLoginViewModelTests` (`new ApiException("Malformed request")`). No test seam needed.

---

## F. Commit Strategy (Task 5)

### F.1 Options

| | Isolated Authentication commit | Combined with a future logging wave (Wave 2) |
|---|---|---|
| Files | 2 (`MobileOtpLoginViewModel.cs` + its test) | +2 into a ~20-file Wave 2 batch |
| Security-review anchor | **Clean** — one commit, entirely about OTP diagnostics, easy to audit the data-safety of in isolation | Auth-sensitive change buried in a large mechanical batch of non-auth pages |
| Semantic coherence | High — "OTP failure diagnostics" is a distinct concern | Low — Wave 2 is Customer/Service/Inventory/HR/Reporting/… (no auth) |
| Precedent | This engagement isolates every auth change (`801cc65` was its own commit; the whole Phase 7.4 arc kept auth separate). Phase 8.10 §E.2 already flagged MobileOtp as the split-out candidate (its "Option A′") | — |
| Revert granularity | auth log revertible without touching Wave 2 | coupled |

### F.2 Recommendation

**Isolated Authentication commit.** One commit, 2 files, immediately after implementation + validation.

Reasoning: it is the only authentication-touching ViewModel in the entire logging track; isolating it
gives reviewers a small, self-contained diff whose data-safety can be verified at a glance, matches this
engagement's consistent practice of never bundling auth changes with anything else, and keeps Wave 2
(all non-auth pages) a clean mechanical batch. The cost — one extra commit cycle — is trivial and the
engagement's rhythm already assumes per-concern commits.

**Proposed message:**
```
fix(desktop): log unexpected OTP API failures

Log the generic ApiException fallthrough of MobileOtpLoginViewModel's
request/resend/verify flows at Warning, recording only the operation name -
never the exception, its message, the phone number, the code, or any
token/session data (the OTP client, AuthBootstrapHttpClient, embeds the raw
backend response body in exception messages, so the exception is never
passed to the logger). Typed/expected failures (rate-limit, connectivity,
timeout, auth-rejection) are deliberately not logged. Follows the
established optional-ctor-param + NullLogger<T> + [LoggerMessage] pattern;
no DI, interface, or auth-flow change.

Adds ~6 tests (unexpected-failure logs Warning, typed failures do not,
NullLogger safety).
```

### F.3 Sequencing after this review

1. **Phase 8.15 — Implementation** (on authorization): apply §B.1 to the 3 generic catches; add the ~6
   §E tests.
2. **Validate:** build (0/0) + full suite (2,521 + ~6) + architecture (7/7).
3. **Phase 8.16 — Commit Scope Review** (readiness only) → **Phase 8.17 — Commit Execution**: isolated
   commit (F.2), explicit-path staging of the 2 files, then fresh post-commit validation + checkpoint
   update (§B commit row, §E test count, §F item 1 → **fully resolved**, §G next action → Wave 2).

### F.4 Explicitly out of scope

- Logging any typed/expected failure branch (§B.2).
- Adding logging to `AuthBootstrapHttpClient` itself (a separate Infrastructure-layer decision — noted
  as the real structural gap, but not this phase; if pursued it must follow `HttpApiClient.LogFailure`'s
  metadata-only, never-body contract).
- Wave 2 (the ~24 non-auth ViewModels).
- `AccountingPageViewModel.CancelInvoiceAsync` missing try/catch.

---

## STOP

Scope review complete. No implementation performed. Recommendation: **1 production file + 1 test file**,
log **only** the generic `catch (ApiException)` fallthrough of the 3 OTP flows at **Warning**,
**operation-name only (exception never passed)**, **isolated authentication commit**.
