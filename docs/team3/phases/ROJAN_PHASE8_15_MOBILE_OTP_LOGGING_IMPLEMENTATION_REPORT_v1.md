# ROJAN AI — TEAM 3 — PHASE 8.15 MOBILE OTP LOGGING HARDENING — IMPLEMENTATION REPORT v1

**Type:** Implementation complete. Build + full test suite + architecture tests all green.
**No commit performed.** `HEAD` is still `2453a7f` — this report is the gate before commit authorization.

**Branch:** `feature/team3-desktop-completion`
**Authorization:** `PHASE 8.15 — MOBILE OTP LOGGING — IMPLEMENTATION v1`
**Scope reference:** `ROJAN_PHASE8_14_MOBILE_OTP_LOGGING_SCOPE_REVIEW_v1.md`

---

## A. Files Changed

Exactly 2 — 1 production + 1 test, both on the authorization's allow-list. **No DI, no interface, no
other ViewModel, no new files.**

| File | +/− | Change |
|---|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs` | +16 / −2 | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<MobileOtpLoginViewModel> _logger` field; ctor +3rd param `ILogger<MobileOtpLoginViewModel>? logger = null` + `NullLogger` fallback; +1 `[LoggerMessage(Level = Warning)]`; +3 call sites (one per flow's generic `catch (ApiException)`) |
| `tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs` | +103 / −0 | +2 `using`s; +7 test cases. **Zero edits to existing tests** — the ~24 existing `new MobileOtpLoginViewModel(service, scheduler)` sites are untouched (optional 3rd param) |

`git diff --stat`: `2 files changed, 119 insertions(+), 2 deletions(-)`

**Confirmed NOT modified** (authorization DO-NOT list): authentication flow, validation logic, the
`RequestOtpAsync`/`ResendOtpAsync`/`SignInWithOtpAsync` calls, DI registration
(`ServiceCollectionExtensions.cs`), any interface, any other ViewModel. Verified by the diff being
entirely inside `MobileOtpLoginViewModel`'s constructor + the 3 pre-existing generic catch blocks + one
new private partial method.

---

## B. Security Design

### B.1 What is logged

One `[LoggerMessage]`, called from the generic `catch (ApiException)` fallthrough of all three flows,
**after** the unchanged `ErrorMessage = Strings.Login_Error_Generic;` line:

```csharp
// RequestCodeAsync / ResendCodeAsync / VerifyCodeAsync — generic catch (ApiException):
    ErrorMessage = Strings.Login_Error_Generic;          // unchanged
    LogUnexpectedOtpApiFailure(nameof(RequestCodeAsync)); // added — flow name only

[LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "OTP API request failed during {Operation}")]
private partial void LogUnexpectedOtpApiFailure(string operation);
```

A produced log line is exactly:
```
<timestamp> [Warning] Rojan.Desktop.Presentation.ViewModels.Security.MobileOtpLoginViewModel: OTP API request failed during RequestCodeAsync
```

### B.2 Security rules — compliance

| Rule (authorization) | Compliance |
|---|---|
| DO NOT LOG the **exception object** | The `[LoggerMessage]` method signature is `(string operation)` — **there is no `Exception` parameter.** The exception cannot reach the logger |
| DO NOT LOG **`Exception.Message`** | Never referenced by any logging call. (This matters here specifically: `AuthBootstrapHttpClient` — the client OTP uses — builds `ApiException` messages as `"Request failed with status {code}: {responseBody}"`, embedding the raw backend body. The exception is not passed, so that body cannot leak.) |
| DO NOT LOG **phone number** | Not in the template (`{Operation}` is a compile-time `nameof(...)`); the exception object (which could carry a body echoing the phone) is not passed; validation paths that hold phone data (`ClassifyInvalidPhoneNumber`, missing-phone, E.164 reject) are outside the `try` and log nothing |
| DO NOT LOG **OTP code** | `_code` / `Code` is never referenced by any logging call. `VerifyCodeAsync`'s generic catch logs only `nameof(VerifyCodeAsync)` |
| DO NOT LOG **token** | Tokens live in `SessionService`/`AuthResponse`, never touched by this ViewModel; no log call references anything token-shaped |
| DO NOT LOG **session information** | No session exists on the pre-authentication OTP screen; nothing session-shaped is referenced |
| DO NOT LOG **backend response** | The only carrier of the response body is `ApiException.Message`, which is never passed (B.2 row 1–2) |

### B.3 Logging behaviour — matches spec

| Path | Logs? | Level |
|---|---|---|
| generic `catch (ApiException)` — `RequestCodeAsync` | **Yes** | Warning |
| generic `catch (ApiException)` — `ResendCodeAsync` | **Yes** | Warning |
| generic `catch (ApiException)` — `VerifyCodeAsync` | **Yes** | Warning |
| `catch (ApiRateLimitException)` (all flows) | **No** | — |
| `catch (ApiConnectivityException)` (all flows) | **No** | — |
| `catch (ApiTimeoutException)` (all flows) | **No** | — |
| `catch (ApiAuthenticationException)` (VerifyCode 401/403) | **No** | — |
| validation / normalization (`return` before `try`) | **No** | — |

Content: **operation name only** (`{Operation}` = `nameof(<method>)`). Message text:
`"OTP API request failed during {Operation}"` — matches the authorization's example shape.

### B.4 Behaviour preservation

Every generic `catch (ApiException)` keeps its exact filter and its
`ErrorMessage = Strings.Login_Error_Generic;`. The log call is appended after. No catch removed, no
rethrow, no change to the typed catches, the 403-vs-401 branch, `SignedIn`, `IsBusy`, `IsCodeSent`, the
resend cooldown, phone normalization, or validation. `NullLogger<T>` default = no observable change for
any caller that does not pass a logger.

---

## C. Tests

**+7 test cases** (Presentation.Tests: 578 → 585). All green.

| # | Test | Proves |
|---|---|---|
| 1 | `RequestCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` | generic `ApiException` (message deliberately contains `+989123456789` and `"secret"`) → `ErrorMessage == Login_Error_Generic` (unchanged) **and** exactly one `Warning` entry containing `"RequestCodeAsync"` and **not** containing the phone number or `"secret"` |
| 2 | `ResendCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` | same for the resend flow → `Warning` containing `"ResendCodeAsync"` |
| 3 | `VerifyCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` | same for verify → `Warning` containing `"VerifyCodeAsync"`, not containing the code `"123456"` |
| 4 | `RequestCodeCommand_RateLimited_DoesNotLog` | `ApiRateLimitException` → `ErrorMessage == Login_Mobile_Error_RateLimited` (unchanged) **and** `logger.Entries` is **empty** |
| 5 | `VerifyCodeCommand_AuthRejection_DoesNotLog` `[Theory(401, 403)]` | `ApiAuthenticationException` → `logger.Entries` is **empty** (auth failures never logged) — 2 cases |
| 6 | `NoLoggerSupplied_UsesNullLogger_UnexpectedApiFailureNeverThrows` | no logger passed + generic `ApiException` → `Record.ExceptionAsync` is `null`, `ErrorMessage == Login_Error_Generic` |

- Covers all 6 required test categories (Request/Resend/Verify log Warning; rate-limit doesn't log;
  401/403 doesn't log; no-logger never throws).
- Uses the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`) via `using`, and
  the existing `StubAuthenticationService` `*ExceptionToThrow` properties. **No new test infra.**
- Tests 1 and 3 explicitly assert the phone number / OTP code are **absent** from the log line — a
  direct data-leak regression guard.
- All pre-existing `MobileOtpLoginViewModelTests` (validation messages, phone normalization,
  network/rate-limit/401/403 mapping, `SignedIn` on success, resend cooldown) pass unchanged.

---

## D. Validation

### D.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

(One fix cycle: an initial `statusCode.ToString()` in a test tripped `CA1305` under
`TreatWarningsAsErrors` — replaced with a literal message string; then clean.)

### D.2 Full test suite

```
dotnet test --no-build
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **585** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,528** | **0** | **0** |

- Baseline at `2453a7f`: **2,521**. Now **2,528** = 2,521 + **7 new**. No pre-existing test changed result.

### D.3 Architecture tests

**7 / 7 passing** — unchanged. `Microsoft.Extensions.Logging.Abstractions` is not a forbidden
dependency (`DependencyDirectionTests`); no `System.Windows.Threading`/`Controls` added
(`ViewModelTestabilityTests`).

### D.4 Expected vs actual (per authorization)

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,528 / 2,528, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## E. Commit Readiness

**Ready. Not committed — stopping per the authorization's STOP CONDITION.**

- **Working tree:** the 2 authorized files modified, nothing else tracked. Untracked = `.md` reports only.
- **Proposed staging (explicit paths only — never `git add -A` / `git add .`):**
  ```
  src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs
  tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs
  ```
- **Proposed commit message (isolated authentication commit — per scope review §F.2):**
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

  Adds 7 tests (unexpected-failure logs Warning + no-PII assertion, typed
  failures do not log, NullLogger safety).
  ```
- **Downstream impact:** none on Authentication flow, Booking, Calendar authority, Shift Engine, RBAC,
  or Navigation — the diff is fully contained to `MobileOtpLoginViewModel`'s constructor + 3 existing
  catch blocks.
- **Checkpoint update owed after commit:** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §B (new commit),
  §E (test count 2,521 → 2,528, coverage 7 → 8 of 56), §F item 1 (**fully resolved** — the Phase 8.2
  named-ViewModel set is complete), §G (next action → Logging Wave 2).

---

## STOP

Implementation and validation complete. **No commit performed.** Awaiting commit authorization.
