# ROJAN AI — TEAM 3 — PHASE 8.16 MOBILE OTP LOGGING HARDENING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No commit, no push, no source change.**
**Mode:** READINESS ONLY — confirms the exact diff, security safety, and staging list before Phase 8.17
(commit execution).

**Branch:** `feature/team3-desktop-completion`
**HEAD:** `2453a7f` (`git rev-parse HEAD` this turn — unchanged, no drift)
**Predecessors:** `ROJAN_PHASE8_14_MOBILE_OTP_LOGGING_SCOPE_REVIEW_v1.md` (scope),
`ROJAN_PHASE8_15_MOBILE_OTP_LOGGING_IMPLEMENTATION_REPORT_v1.md` (impl).

---

## A. Git State

| Item | Value |
|---|---|
| HEAD | `2453a7fe0717bad9150492ac68f87056661e2a40` |
| Branch | `feature/team3-desktop-completion` |
| Staged files | **none** (`git diff --cached` empty) |
| Modified tracked files | **2** (1 production + 1 test) |
| Deleted / renamed | none |
| Untracked | `.md` reports only — no untracked code |

```
git status --porcelain (tracked):
 M src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs
```

`git diff --stat`: `2 files changed, 119 insertions(+), 2 deletions(-)`

**Confirmed: no unrelated tracked changes.** Both files are on the Phase 8.15 authorization's allow-list.

---

## B. Diff Scope

### B.1 Production — `MobileOtpLoginViewModel.cs` (+16 / −2)

| Hunk | Change | Assessment |
|---|---|---|
| usings | +`Microsoft.Extensions.Logging`, +`Microsoft.Extensions.Logging.Abstractions` | additive; `Abstractions` already a Presentation `PackageReference` |
| class decl | `sealed`→`sealed partial` | required for the `[LoggerMessage]` source generator |
| field | +`private readonly ILogger<MobileOtpLoginViewModel> _logger;` | one logger field |
| ctor | +3rd parameter `ILogger<MobileOtpLoginViewModel>? logger = null` (optional, appended last); +`_logger = logger ?? NullLogger<MobileOtpLoginViewModel>.Instance;` | non-breaking — all ~24 existing 2-arg `new MobileOtpLoginViewModel(...)` sites still compile |
| `RequestCodeAsync` generic `catch (ApiException)` | +`LogUnexpectedOtpApiFailure(nameof(RequestCodeAsync));` **after** the unchanged `ErrorMessage = Strings.Login_Error_Generic;` | additive; catch filter and message unchanged |
| `ResendCodeAsync` generic `catch (ApiException)` | +`LogUnexpectedOtpApiFailure(nameof(ResendCodeAsync));` | same |
| `VerifyCodeAsync` generic `catch (ApiException)` | +`LogUnexpectedOtpApiFailure(nameof(VerifyCodeAsync));` | same |
| new method | `[LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "OTP API request failed during {Operation}")] private partial void LogUnexpectedOtpApiFailure(string operation);` + a 4-line security comment | signature takes **only `string operation`** — no `Exception` |

### B.2 Test — `MobileOtpLoginViewModelTests.cs` (+103 / −0)

- +2 `using`s (`Microsoft.Extensions.Logging`, `Rojan.Desktop.Presentation.Tests.Specialists` for `RecordingLogger<T>`)
- +7 new test cases (6 `[Fact]` + 1 `[Theory]` with 2 `[InlineData]`)
- **Zero deletions, zero edits to any existing test.**

### B.3 Confirmed NOT changed

| Concern | Evidence in the diff |
|---|---|
| **Auth flow** | No change to `RequestOtpAsync` / `ResendOtpAsync` / `SignInWithOtpAsync` calls, the `SignedIn` event, `IsBusy`, `IsCodeSent`, `ApplyIssuedChallenge`, or the resend cooldown. The only edits inside the 3 flows are one appended log call per generic catch |
| **API changes** | No API client, contract, DTO, or endpoint touched. `IAuthenticationService` / `AuthBootstrapHttpClient` / `HttpApiClient` not in the diff |
| **DI changes** | `ServiceCollectionExtensions.cs` (Presentation or Infrastructure) **not in the diff.** `AddLogging()` already registers `ILogger<T>`; the `AddTransient<MobileOtpLoginViewModel>()` registration is unchanged and DI injects the logger into the new optional param automatically |
| **Interfaces** | No `I*.cs` in the diff |
| **Validation logic** | `ClassifyInvalidPhoneNumber`, `NormalizePhoneNumber`, `CleanDigits`, E.164 regex, missing-phone / missing-code guards — none touched (all outside the `try` blocks) |
| **403-vs-401 branch** | `catch (ApiAuthenticationException exception)` and its `exception.StatusCode == 403` check — unchanged |
| **Typed catches** | `ApiRateLimitException` / `ApiConnectivityException` / `ApiTimeoutException` catches — unchanged, no log added |
| **Other ViewModels** | none in the diff |

---

## C. Security Validation

### C.1 No sensitive logging — verified against the diff

| Prohibited item | In any logged output? | Why not (from the diff) |
|---|---|---|
| **Exception object** | **No** | `LogUnexpectedOtpApiFailure(string operation)` has no `Exception` parameter. The 3 call sites pass `nameof(RequestCodeAsync)` / `nameof(ResendCodeAsync)` / `nameof(VerifyCodeAsync)` — string literals resolved at compile time |
| **`Exception.Message`** | **No** | never referenced. (Relevant here: `AuthBootstrapHttpClient` — the client OTP uses — builds `ApiException` messages as `"Request failed with status {code}: {responseBody}"`, embedding the raw backend body. That message is never passed to the logger.) |
| **Phone number** | **No** | not in the `Message` template; exception not passed; validation paths that hold phone data are outside the `try` and unchanged |
| **OTP code** | **No** | `Code` / `_code` never referenced by any logging call; `VerifyCodeAsync`'s catch logs only `nameof(VerifyCodeAsync)` |
| **Token** | **No** | no token-shaped value referenced anywhere in the diff |
| **Session information** | **No** | pre-authentication screen; nothing session-shaped referenced |
| **Backend response** | **No** | only carried by `ApiException.Message`, which is never passed |

The produced log line is exactly:
```
<timestamp> [Warning] Rojan.Desktop.Presentation.ViewModels.Security.MobileOtpLoginViewModel: OTP API request failed during RequestCodeAsync
```

### C.2 Logging pattern — verified

| Requirement | Confirmed in diff |
|---|---|
| `ILogger<T>` | `private readonly ILogger<MobileOtpLoginViewModel> _logger;` — instance field, constructor-injected |
| `NullLogger` fallback | `_logger = logger ?? NullLogger<MobileOtpLoginViewModel>.Instance;` |
| Warning level | `[LoggerMessage(EventId = 1, Level = LogLevel.Warning, ...)]` |
| Operation name only | `Message = "OTP API request failed during {Operation}"`, called with `nameof(<method>)` — no other parameter, no interpolated value |
| `[LoggerMessage]` source-gen (not raw `_logger.LogWarning`) | required under `TreatWarningsAsErrors` + CA1848; matches `BookingPageViewModel` / Wave 1 |

### C.3 Which paths log — verified

| Path | Logs? |
|---|---|
| generic `catch (ApiException)` — Request / Resend / Verify | **Yes**, Warning |
| `catch (ApiRateLimitException)` | **No** (unchanged in diff) |
| `catch (ApiConnectivityException)` / `catch (ApiTimeoutException)` | **No** |
| `catch (ApiAuthenticationException)` (401/403) | **No** |
| validation / normalization | **No** |

---

## D. Test Validation

### D.1 Fresh re-run this turn (HEAD `2453a7f` + working tree)

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded. 0 Warning(s), 0 Error(s).** |
| Full test suite | **2,528 / 2,528 passing, 0 failed, 0 skipped** (Domain 456, Presentation **585**, Application 791, Infrastructure 609, Shell 80, Architecture 7) |
| Architecture tests | **7 / 7 passing** |
| Delta vs `2453a7f` baseline (2,521) | **+7** — exactly the 7 new tests; no pre-existing test changed result |

### D.2 Required test coverage (per authorization Task 5)

| Required | Test | ✓ |
|---|---|---|
| Request OTP `ApiException` logs Warning | `RequestCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` — asserts one `Warning` containing `"RequestCodeAsync"`, and **asserts the phone number and `"secret"` are absent** from the line | ✅ |
| Resend OTP `ApiException` logs Warning | `ResendCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` — `Warning` containing `"ResendCodeAsync"` | ✅ |
| Verify OTP `ApiException` logs Warning | `VerifyCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` — `Warning` containing `"VerifyCodeAsync"`, **asserts the code `"123456"` is absent** | ✅ |
| Rate-limit failure does **not** log | `RequestCodeCommand_RateLimited_DoesNotLog` — `ErrorMessage == Login_Mobile_Error_RateLimited` (unchanged) **and** `logger.Entries` empty | ✅ |
| 401/403 auth failure does **not** log | `VerifyCodeCommand_AuthRejection_DoesNotLog` `[Theory(401, 403)]` — `logger.Entries` empty | ✅ |
| No logger supplied never throws | `NoLoggerSupplied_UsesNullLogger_UnexpectedApiFailureNeverThrows` — `Record.ExceptionAsync` is `null`, `ErrorMessage == Login_Error_Generic` | ✅ |

- Every "logs Warning" test also asserts the **unchanged** `ErrorMessage`, proving additivity.
- Tests 1 and 3 are explicit **data-leak regression guards** (seed the exception message with a phone
  number / `"secret"` / the OTP code, assert absent from the log).
- Uses the existing `RecordingLogger<T>` + `StubAuthenticationService` — no new test infrastructure.
- All pre-existing `MobileOtpLoginViewModelTests` (validation, normalization, network/rate-limit/401/403
  mapping, `SignedIn` on success, resend cooldown) pass unchanged.

---

## E. Commit Plan

### E.1 Staging (explicit paths only — never `git add -A` / `git add .`)

```
git add src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs
```

Both files are single-concern (OTP failure diagnostics). The `.md` reports stay untracked.

### E.2 Commit message (isolated authentication commit — per scope review §F.2)

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

### E.3 Post-commit follow-up (Phase 8.17)

1. Fresh validation on the new HEAD (build + full suite + architecture tests).
2. Update `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`: §B (new commit + detail), §E (test count
   2,521 → 2,528; self-logging coverage 7 → 8 of 56), §F item 1 (**fully resolved** — Phase 8.2
   named-ViewModel set complete), §G (next action → Logging Wave 2).

### E.4 Explicitly deferred (not this commit)

- Logging any typed/expected OTP failure branch.
- Adding logging to `AuthBootstrapHttpClient` itself (separate Infrastructure decision).
- Logging Wave 2 (~24 non-auth ViewModels).
- `AccountingPageViewModel.CancelInvoiceAsync` missing try/catch.

---

## F. Readiness Verdict

**READY TO COMMIT.**

- Diff is complete, minimal (2 files, +119/−2), single-concern, matches the Phase 8.15 authorization.
- Build clean, 2,528/2,528 tests green, architecture 7/7 — re-verified this turn.
- No auth-flow, API, DI, or interface change.
- No sensitive value in any log path — verified line-by-line and guarded by explicit no-PII test
  assertions.
- Staging list and commit message specified above, ready for Phase 8.17.

---

## STOP

Commit scope review complete. No commit performed. Awaiting Phase 8.17 (commit execution) authorization.
