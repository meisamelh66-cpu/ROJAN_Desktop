# ROJAN AI — TEAM 3 — PHASE 8.17 MOBILE OTP LOGGING HARDENING — COMMIT REPORT v1

**Type:** Commit executed + fresh post-commit validation. **Not pushed, not merged, not rebased, not amended.**
**Branch:** `feature/team3-desktop-completion`

---

## A. Commit Hash

**`31f4b63a3a4d859349365fe75acd7b4df9f27cf2`** (`31f4b63`)

- Parent: `2453a7f` (`fix(desktop): add ViewModel diagnostic logging (wave 1)`)
- Author: Meisam Elhaee — Thu Aug 27 2026 16:09:31 -0700
- Subject: `fix(desktop): log unexpected OTP API failures` (exactly as authorized)
- Trailers: `Co-Authored-By: Claude Sonnet 5`, `Claude-Session: …`

```
git log --oneline -4
31f4b63 fix(desktop): log unexpected OTP API failures
2453a7f fix(desktop): add ViewModel diagnostic logging (wave 1)
94fca6a fix(desktop): bound navigation back-stack depth
801cc65 fix(desktop): improve authentication error handling UX
```

---

## B. Files Committed

```
git show --stat 31f4b63
 src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs        | 18 +++-
 tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs      | 103 +++++++++++++++++++++
 2 files changed, 119 insertions(+), 2 deletions(-)
```

**Exactly the 2 authorized files. Nothing else.**

| File | Change |
|---|---|
| `MobileOtpLoginViewModel.cs` | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<MobileOtpLoginViewModel> _logger` field; ctor +3rd optional param `ILogger<MobileOtpLoginViewModel>? logger = null` + `NullLogger` fallback; +1 `[LoggerMessage(Level = Warning)]` partial (`LogUnexpectedOtpApiFailure(string operation)`); +3 call sites — one per flow's generic `catch (ApiException)`, appended after the unchanged `ErrorMessage = Strings.Login_Error_Generic;` |
| `MobileOtpLoginViewModelTests.cs` | +2 `using`s; +7 test cases. **No existing test modified.** The ~24 existing 2-arg `new MobileOtpLoginViewModel(...)` sites are untouched (optional 3rd param) |

---

## C. Scope Verification

Performed on the **staged** diff before committing (`git diff --cached`), re-confirmed from
`git show 31f4b63`:

| Check | Result |
|---|---|
| Staging method | `git reset` to clear the index, then **2 explicit `git add <path>`**. **No `git add .`, no `git add -A`.** |
| Staged file count | Exactly 2, both authorized |
| Unstaged tracked changes at commit time | none (`git diff --name-only` empty) |
| `.md` reports staged | none — all remain untracked |
| Working tree after commit | **clean** (0 modified/deleted tracked); untracked = `.md` reports only |
| **Auth flow changes** | **None.** `RequestOtpAsync` / `ResendOtpAsync` / `SignInWithOtpAsync` calls, `SignedIn`, `IsBusy`, `IsCodeSent`, `ApplyIssuedChallenge`, resend cooldown, 403-vs-401 branch — all unchanged. Only 3 appended log calls inside pre-existing generic catches |
| **API changes** | **None** — no API client, contract, DTO, or endpoint touched |
| **DI changes** | **None** — `ServiceCollectionExtensions.cs` not in the diff. `AddLogging()` already registers `ILogger<T>`; `AddTransient<MobileOtpLoginViewModel>()` unchanged; DI injects the logger into the new optional param automatically |
| **Interfaces** | **None** — no `I*.cs` in the diff |
| **Validation logic** | **None** — `ClassifyInvalidPhoneNumber`, `NormalizePhoneNumber`, `CleanDigits`, E.164 regex, missing-phone/missing-code guards all untouched |
| **Other ViewModels** | **None** in the diff |
| Push / merge / rebase / amend | **none performed** — single fresh commit on `2453a7f` |

---

## D. Security Confirmation

The log record produced by every path is exactly:
```
<timestamp> [Warning] Rojan.Desktop.Presentation.ViewModels.Security.MobileOtpLoginViewModel: OTP API request failed during <RequestCodeAsync|ResendCodeAsync|VerifyCodeAsync>
```

| Prohibited item | In any logged output? | Why not |
|---|---|---|
| Exception object | **No** | `LogUnexpectedOtpApiFailure(string operation)` has no `Exception` parameter |
| `Exception.Message` | **No** | never referenced — critical here because `AuthBootstrapHttpClient` (the OTP client) embeds the raw backend response body in `ApiException` messages |
| Phone number | **No** | not in the `Message` template; exception not passed; validation paths (which hold phone data) unchanged and outside the `try` |
| OTP code | **No** | `Code` / `_code` never referenced by any log call |
| Token | **No** | no token-shaped value referenced |
| Session information | **No** | pre-authentication screen; nothing session-shaped referenced |
| Backend response body | **No** | only carried by `ApiException.Message`, never passed |

- Content logged: **operation name only** (`{Operation}` = compile-time `nameof(<method>)`).
- Level: **Warning** — matches `HttpApiClient.LogApiRequestFailed`; clears the `LocalFileLoggerProvider`
  `Warning` floor.
- Paths that log: generic `catch (ApiException)` fallthrough of all 3 flows **only**.
- Paths that do **not** log: `ApiRateLimitException`, `ApiConnectivityException`, `ApiTimeoutException`,
  `ApiAuthenticationException` (401/403), and all validation/normalization paths.
- Test-enforced: `RequestCodeCommand_UnexpectedApiException_LogsWarningWithOperationOnly` seeds the
  exception message with a phone number and `"secret"` and asserts both are **absent** from the log
  line; `VerifyCodeCommand_...` asserts the OTP code `"123456"` is absent.

---

## E. Validation Results — Fresh, Post-Commit (HEAD = `31f4b63`)

### E.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### E.2 Full test suite

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

### E.3 Test count delta

| | Total | Presentation.Tests |
|---|---|---|
| Baseline `2453a7f` | 2,521 | 578 |
| **New HEAD `31f4b63`** | **2,528** | **585** |
| Delta | **+7** | +7 |

All +7 are the new OTP logging tests. No pre-existing test changed result.

### E.4 Architecture tests

**7 / 7 passing** — unchanged.

### E.5 Expected vs actual (per authorization)

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,528 / 2,528, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## F. Remaining Backlog

### F.1 Logging coverage

| Item | Status |
|---|---|
| **Phase 8.2 named-ViewModel set** (`MobileOtpLoginViewModel`, `DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel`) | **FULLY RESOLVED** — `2453a7f` (Wave 1: Dashboard/Calendar/Accounting) + `31f4b63` (this commit: MobileOtp) |
| **Logging Wave 2** — the ~24 other ViewModels with an unlogged broad `catch (Exception)` (Phase 8.9 §C.3): `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`, `ReportingPageViewModel`, `AnalyticsPageViewModel`, `OrganizationPageViewModel`, `SalonPageViewModel`, `AiCenterPageViewModel`, the 5 Automation tab VMs, and the rest | **Identified, not scoped** — the recommended next logging phase, grouped by module, one wave per commit |
| `AuthBootstrapHttpClient` has no logging of its own (structural gap — OTP's only trail is now this ViewModel) | Disclosed in Phase 8.14 §A.3; a separate Infrastructure-layer decision, not this track |
| Service-layer logging (Application/Infrastructure services without `ILogger`) | Inventoried Phase 8.9 §B.3; P3, out of the ViewModel track |

Self-logging ViewModel coverage: **8 of 56**.

### F.2 Non-logging backlog (unchanged)

| Item | Status |
|---|---|
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry risk | Documented, unresolved — blocks Accounting's eventual backend connection |
| `AccountingPageViewModel.CancelInvoiceAsync` — missing try/catch | Deferred to a dedicated error-handling phase |
| `CancellationToken` propagation — `CommandPaletteViewModel` (Search) highest value | Planned, not started |
| Startup UX — no progress indicator across `App.OnStartup`'s 13 blocking stages | Planned, not started |
| RBAC migration for the 6 still-local domains | Sequenced future work, per-domain backend-contract-blocked |
| Calendar's dead EF migration/tables (3) | Disclosed tech debt, deferred |
| `RolePermissions` dead enum members | Cleanup opportunity, low urgency |

**Upstream-blocked (not Team 3 actionable):** Inventory, HR, Accounting backend integration — blocked on
Backend/Team 1; Desktop-side prep complete since Phase 8.0.

**No P0. No P1.** Recommended next action: **Logging Wave 2** — audit → scope review → implement, one
module-group per commit.

---

## STOP

Commit executed (`31f4b63`), fresh validation green (build 0/0, 2,528/2,528 tests, architecture 7/7),
report written, checkpoint updated. No push, no merge, no rebase, no amend. Awaiting next authorization.
