# ROJAN AI — TEAM 3 — PHASE 8.10 VIEWMODEL LOGGING HARDENING — COMMIT SCOPE REVIEW v1

**Type:** Scope review only. **No source modified, no logger added, no DI change, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `94fca6a` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_9_LOGGING_COVERAGE_AUDIT_v1.md`

Every target below was re-read from source this turn — the Phase 8.9 report was used as a starting
point, not taken on faith.

---

## A. Current State (Task 1 — revalidated from source)

| # | Target | Ctor signature (verified) | Existing `ILogger` | Broad-catch boundaries (verified line refs) |
|---|---|---|---|---|
| P1 | `ViewModels/Dashboard/DashboardPageViewModel.cs` | `(IDashboardQueryService, IPermissionEngine, ICurrentSessionService)` | **None** | **1** — `LoadAsync` `catch (Exception exception)` @ `:284` → `ErrorMessage = exception.Message; State = Error` |
| P2 | `ViewModels/Calendar/CalendarPageViewModel.cs` | `(ICalendarQueryService, IServiceQueryService)` | **None** | **3** — `InitializeAsync` @ `:213`, `LoadDailyAvailabilityAsync` @ `:256`, `LoadWeeklyAvailabilityAsync` @ `:295` — each → `ErrorMessage = exception.Message; State = Error` |
| P3 | `ViewModels/Accounting/AccountingPageViewModel.cs` | `(IInvoiceQueryService, IInvoiceCommandService, IPaymentQueryService, IPaymentCommandService, IDialogService, ILogger<PosCheckoutViewModel>? posCheckoutLogger = null)` | **Pass-through only** — holds `ILogger<PosCheckoutViewModel>` to forward to the child `PosCheckoutViewModel` (`:215`); **does not log itself** | **2** — `LoadAsync` @ `:145`, `SearchAsync` @ `:175` — each → `ErrorMessage = exception.Message; State = Error` |
| P4 | `ViewModels/Security/MobileOtpLoginViewModel.cs` | `(IAuthenticationService, IDelayScheduler)` | **None** | **0 broad catches.** 3 async flows (`RequestCodeAsync`, `ResendCodeAsync`, `VerifyCodeAsync`), each with *typed* catches ending in a generic `catch (ApiException) → ErrorMessage = Strings.Login_Error_Generic` (@ `:288`, `:329`, `:396`) |

### A.1 Corrections / clarifications vs Phase 8.9

- **P3 (Accounting):** the change is "add its **own** `ILogger<AccountingPageViewModel>`", not "introduce
  logging" — the file already `using`s `Microsoft.Extensions.Logging` and carries a pass-through logger.
- **P3:** `CancelInvoiceAsync` (`:200`) has **no** try/catch at all — a throw there becomes an unobserved
  task exception (caught by `App`'s `TaskScheduler.UnobservedTaskException` surface). **Out of scope**
  for this phase (it is a *missing guard*, not a *missing log on an existing guard*); noted for a future
  error-handling phase.
- **P4 (MobileOtp):** there is **no `catch (Exception)`** to attach a log to. The Phase 8.9 recommendation
  stands: log at **`Warning`** in the *generic* `catch (ApiException)` fallthrough only — the branch that
  means "an API error shape we did not anticipate". The typed branches (`ApiRateLimitException`,
  `ApiConnectivityException`, `ApiTimeoutException`, `ApiAuthenticationException`) are **expected,
  mapped, high-frequency** outcomes and must **not** be logged (noise; and per-phone auth-failure logging
  is a minor privacy concern best avoided).
- **P2 (Calendar):** `LoadDailyAvailabilityAsync` and `LoadWeeklyAvailabilityAsync` are structurally
  identical boundaries — one shared `[LoggerMessage]` with an `{Operation}` discriminator (the
  `BookingPageViewModel.LogOperationFailed` pattern) covers all three.

---

## B. Exact File Scope (Task 2)

### B.1 The established pattern (from `BookingPageViewModel` / `PosCheckoutViewModel` / `SpecialistScheduleViewModel`, verified this turn)

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;   // for NullLogger<T>

public sealed partial class XxxViewModel : ViewModelBase      // add 'partial' (source generator)
{
    private readonly ILogger<XxxViewModel> _logger;

    public XxxViewModel(/* existing deps unchanged */, ILogger<XxxViewModel>? logger = null)  // optional, appended last
    {
        // existing assignments unchanged
        _logger = logger ?? NullLogger<XxxViewModel>.Instance;
    }

    // in each broad catch, AFTER setting ErrorMessage/State:
    //     LogOperationFailed(nameof(TheAsyncMethod), exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Xxx operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation, Exception exception);
}
```

### B.2 Per-target production change

| Target | `class`→`partial` | new `using`s | ctor param added | `[LoggerMessage]` methods | log call sites | Est. net LOC |
|---|---|---|---|---|---|---|
| **DashboardPageViewModel** | yes | `Microsoft.Extensions.Logging`, `…Logging.Abstractions` | `ILogger<DashboardPageViewModel>? logger = null` (4th) | 1 (`Level = Error`) | 1 (in `LoadAsync` catch) | ~+12 |
| **CalendarPageViewModel** | yes | both | `ILogger<CalendarPageViewModel>? logger = null` (3rd) | 1 (`Level = Error`, `{Operation}`) | 3 (Initialize / Daily / Weekly) | ~+16 |
| **AccountingPageViewModel** | yes | none (already imports `…Logging`); add `…Logging.Abstractions` | `ILogger<AccountingPageViewModel>? logger = null` (**appended after** `posCheckoutLogger`, so existing positional callers are unaffected) | 1 (`Level = Error`, `{Operation}`) | 2 (`LoadAsync` / `SearchAsync`) | ~+12 |
| **MobileOtpLoginViewModel** | yes | both | `ILogger<MobileOtpLoginViewModel>? logger = null` (3rd) | 1 (**`Level = Warning`**, `{Operation}`) | 3 (generic `catch (ApiException)` in each flow) | ~+14 |

**Ctor parameter ordering rule:** every new `ILogger<T>?` parameter is **appended as the last
parameter with a `= null` default**. This is the only ordering that keeps every existing positional
call site (production DI + all tests) compiling untouched. For Accounting specifically, that means the
signature becomes `(…, IDialogService dialogService, ILogger<PosCheckoutViewModel>? posCheckoutLogger = null, ILogger<AccountingPageViewModel>? logger = null)`.

### B.3 Per-target test change

| Test file | Existing SUT construction (verified) | Change |
|---|---|---|
| `Dashboard/DashboardPageViewModelTests.cs` | private `CreateSut(StubDashboardQueryService, WorkspaceRole)` helper, ~10 call sites | add optional `RecordingLogger<DashboardPageViewModel>? logger = null` to `CreateSut`; `using Rojan.Desktop.Presentation.Tests.Specialists;` for `RecordingLogger<T>` |
| `Calendar/CalendarPageViewModelTests.cs` | **15 direct** `new CalendarPageViewModel(queryService, serviceQuery)` sites, no helper | leave all 15 as-is (optional param → still compile). New logging tests construct inline with a `RecordingLogger`; add the `using` |
| `Accounting/AccountingPageViewModelTests.cs` | private `MakeSut(...)` helper, ~10 call sites | add optional `RecordingLogger<AccountingPageViewModel>? logger = null` to `MakeSut`; add the `using` |
| `Security/MobileOtpLoginViewModelTests.cs` | **~24 direct** `new MobileOtpLoginViewModel(service, scheduler)` sites, no helper | leave all as-is (optional 3rd param). New tests construct inline with a `RecordingLogger`; add the `using` |

**`RecordingLogger<T>` already exists** — `tests/Rojan.Desktop.Presentation.Tests/Specialists/RecordingLogger.cs`
(records `(LogLevel, string)` per call, `IsEnabled` always true). It is already reused cross-namespace by
`BookingPageViewModelTests` and `PosCheckoutViewModelTests` via `using`. **No new test helper file
needed.**

### B.4 File count

| Category | Count | Files |
|---|---|---|
| Production | **4** | the 4 `*ViewModel.cs` above |
| Test | **4** | the 4 `*ViewModelTests.cs` above |
| DI / composition root | **0** | — |
| Interface | **0** | — |
| New files | **0** | `RecordingLogger<T>` already exists |
| **Total** | **8** | |

---

## C. Architecture Validation (Task 3)

| Check | Result |
|---|---|
| `ILogger<T>` pattern compatibility | **Confirmed.** 4 existing precedents (`BookingPageViewModel`, `PosCheckoutViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`) use the exact `[LoggerMessage]` + optional-ctor-param shape B.1 reproduces |
| `NullLogger<T>` fallback | **Confirmed.** `Microsoft.Extensions.Logging.Abstractions` is already a direct `PackageReference` in `Rojan.Desktop.Presentation.csproj`. `?? NullLogger<T>.Instance` makes every new param non-breaking for existing tests that `new` the VM directly |
| No DI architecture changes | **Confirmed.** `services.AddLogging()` (Infrastructure DI) registers the open-generic `ILogger<T>`. All 4 targets are `AddTransient` in `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` (lines 54, 57, 65, 67) — DI auto-injects the real logger into the new optional param with **zero** registration edits. `MobileOtpLoginViewModel` is composed by `LoginWindowViewModel(MobileOtpLoginViewModel)` via DI, so it too gets the real logger automatically |
| No interface changes | **Confirmed.** No `INavigationService`-style abstraction involved. `IDashboardQueryService` / `ICalendarQueryService` / `IInvoiceQueryService` / `IAuthenticationService` etc. are untouched — the change is entirely inside the concrete ViewModel constructors + private methods |
| No domain impact | **Confirmed.** Presentation-layer only. No business rule, no permission decision, no backend call, no data-authority change. `Booking` / `Calendar` / `Shift Engine` / `RBAC` / `Authentication` authority all untouched — `MobileOtpLoginViewModel`'s change is a `Warning` log in an *already-existing* generic catch, adding no behaviour to the auth flow itself |
| `DependencyDirectionTests` | **No violation.** It forbids Presentation→`Infrastructure`/`Domain`/`Shell`/`Microsoft.EntityFrameworkCore` only — not `Microsoft.Extensions.Logging.Abstractions` |
| `ViewModelTestabilityTests` | **No violation.** It forbids `System.Windows.Threading` / `System.Windows.Controls` deps — logging adds neither |
| Architecture test suite | **7/7 expected unchanged** |
| `LocalFileLoggerProvider` `Warning` floor | All new logs are `Error` (P1–P3) or `Warning` (P4) — **all clear the floor**, all reach the file |
| CA1848 (allocation-free logging) | Satisfied — `[LoggerMessage]` source-generated partials, same as every precedent |

---

## D. Test Plan (Task 5)

### D.1 Existing tests affected

**None break.** Every affected test file constructs its SUT via a direct `new` or a local `MakeSut`/
`CreateSut` helper; an appended optional `ILogger<T>? = null` parameter is source-compatible with all of
them. Concretely re-verified:

| File | Call sites | After change |
|---|---|---|
| `DashboardPageViewModelTests` | ~10 via `CreateSut` | pass unchanged (helper gains optional param, defaults to null → `NullLogger`) |
| `CalendarPageViewModelTests` | 15 direct `new` | pass unchanged (optional 3rd param) |
| `AccountingPageViewModelTests` | ~10 via `MakeSut` | pass unchanged |
| `MobileOtpLoginViewModelTests` | ~24 direct `new` | pass unchanged (optional 3rd param) |

Baseline to preserve: **2,512 / 2,512** (Presentation.Tests = 569 of that).

### D.2 New tests required

Using `RecordingLogger<T>` and each file's existing stub services:

| Target | New test(s) | Assertion |
|---|---|---|
| Dashboard | `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` | after the existing `StubDashboardQueryService(_ => Task.FromException(...))` path, `logger.Entries` contains a `LogLevel.Error` entry. (Extends the existing `Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage` scenario) |
| Dashboard | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | mirrors `BookingPageViewModelTests.NoLoggerSupplied_UsesNullLogger_...` — the `NullLogger` default is a genuine safe no-op |
| Calendar | `InitializeAsync_SpecialistQueryThrows_LogsError` | `RecordingLogger` gets a `LogLevel.Error` entry when `GetScheduledSpecialistsAsync` throws |
| Calendar | `LoadDailyAvailabilityAsync_Throws_LogsError` | representative of the daily/weekly boundaries (weekly is structurally identical — one test + a code comment, or a `[Theory]` over both view modes) |
| Calendar | `NoLoggerSupplied_UsesNullLogger_InitializeFailureNeverThrows` | NullLogger safety |
| Accounting | `LoadAsync_InvoiceQueryThrows_LogsError` | `LogLevel.Error` recorded |
| Accounting | `SearchAsync_Throws_LogsError` | `LogLevel.Error` recorded when the search query throws and `searchText` still matches |
| Accounting | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | NullLogger safety |
| MobileOtp | `RequestCodeAsync_UnexpectedApiException_LogsWarning` | raw `ApiException` from `IAuthenticationService.RequestOtpAsync` → `logger.Entries` contains `LogLevel.Warning`; `ErrorMessage == Strings.Login_Error_Generic` (unchanged) |
| MobileOtp | `VerifyCodeAsync_UnexpectedApiException_LogsWarning` | same for the verify flow |
| MobileOtp | `RequestCodeAsync_RateLimited_DoesNotLog` | **negative** test — a *typed* `ApiRateLimitException` produces **no** log entry (proves only the generic branch logs) |
| MobileOtp | `NoLoggerSupplied_UsesNullLogger_...` | NullLogger safety |

**Total new tests: ~13** (≈3–4 per target). Expected suite after: **2,512 + ~13 ≈ 2,525**, 0 failures.

**Implementation-time check (flagged, not blocking):** confirm the OTP test stub
(`StubAuthenticationService` in `MobileOtpLoginViewModelTests`) can be made to throw a **raw
`ApiException`** (base type), and that `ApiException` is constructible from the test assembly. If its
constructor is `internal`, use an existing derived exception that still routes through the generic
`catch (ApiException)` — or add a minimal test seam. Resolve during Phase 8.10b, before finalizing the
MobileOtp tests.

### D.3 Regression checks

1. `dotnet build` — expect **0 warnings, 0 errors** (watch specifically for CA1848 / source-generator
   `partial` errors).
2. Full suite — expect **2,512 + new**, 0 failed, 0 skipped.
3. Architecture tests — expect **7 / 7**, unchanged.
4. Spot-confirm the 4 targets' *existing* behaviour tests (state transitions, RBAC KPI filtering,
   out-of-order search guard, OTP normalization/validation) all still pass — the log calls are strictly
   additive, placed *after* the existing `ErrorMessage`/`State` assignments.

---

## E. Commit Strategy (Task 4)

### E.1 Option A vs Option B

| | Option A — one commit (all 4 targets) | Option B — one commit per module |
|---|---|---|
| Commits | 1 | 4 (Dashboard, Calendar, Accounting, Security) |
| Scope-review / execution cycles | 1 | 4 |
| File isolation benefit | n/a — **the 4 production files are disjoint**, no shared file, no ordering dependency | none gained (already disjoint) |
| Review burden | one pattern, applied 4×, ~50 LOC total — easy to read as a set | 4× the ceremony for the same total diff |
| Revert granularity | revert = lose all 4 (all trivially re-appliable) | per-module revert |
| Precedent in this engagement | `da18c18` bundled Booking **and** Checkout hardening in one commit | — |
| Bisect signal | one commit flips coverage 4→8 VMs | finer, but no defect risk that would need bisecting a pure-additive log call |

### E.2 Recommendation

**Option A — a single commit** covering all 4 production files + 4 test files.

Reasoning:
- The change is one mechanical concern ("add diagnostic logging to the swallowing broad-catch boundaries
  of 4 ViewModels"), applied identically 4 times.
- The 4 production files are completely disjoint — Option B's per-file isolation buys nothing that
  Option A doesn't already have.
- Total diff is small (~4 files, ~55 net production LOC + ~13 tests).
- It is purely additive (a log call after existing error handling) — near-zero defect surface, nothing a
  finer bisect granularity would help with.
- Matches this engagement's own precedent (`da18c18`).

**One reserved caveat:** `MobileOtpLoginViewModel` differs slightly — `Warning` not `Error`, auth-flow
context, no pre-existing broad catch. If the authorizer wants the auth-touching change isolated for
audit clarity, split it as **Option A′: two commits** — (1) Dashboard + Calendar + Accounting
(`fix(desktop): add ViewModel diagnostic logging`), (2) MobileOtpLogin
(`fix(desktop): log unexpected OTP API failures`). This is the only split with a defensible rationale;
a full 4-way split (Option B) is not recommended.

### E.3 Proposed commit (Option A)

**Staging (explicit paths only — never `git add -A` / `git add .`):**
```
src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs
tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs
```

**Message:**
```
fix(desktop): add ViewModel diagnostic logging (wave 1)

Add ILogger<T> to the four highest-value ViewModels whose broad-catch load
boundaries currently swallow exceptions into an on-screen message with no
diagnostic trail: DashboardPageViewModel, CalendarPageViewModel,
AccountingPageViewModel (Error), and MobileOtpLoginViewModel (Warning, on
the unexpected-ApiException fallthrough only). Follows the established
optional-ctor-param + NullLogger<T> + [LoggerMessage] pattern; no DI,
interface, or behaviour change - the log call is additive, after the
existing ErrorMessage/State handling.
```

### E.4 Sequencing after this review

1. **Phase 8.10b — Implementation**: apply B.1 to all 4, priority order Dashboard → Calendar →
   Accounting → MobileOtp; resolve the D.2 `ApiException`-constructability check.
2. **Validate**: build (0/0) + full suite (2,512 + ~13) + architecture (7/7).
3. **Phase 8.10c — Commit Scope Review** (readiness only): confirm the exact staged diff.
4. **Phase 8.10d — Commit Execution**: single commit (Option A), explicit-path staging, then fresh
   post-commit validation + checkpoint update (§B new commit, §F item 1 resolved, §E test count, §G next
   action → §C.3 "Wave 2" or another §F item).

### E.5 Explicitly out of scope

- `AccountingPageViewModel.CancelInvoiceAsync` missing try/catch (A.1) — separate error-handling phase.
- The ~24 other unlogged broad-catch ViewModels (Phase 8.9 §C.3) — "Wave 2+", later phases.
- Any change to `LocalFileLoggerProvider`, its `Warning` floor, `AddLogging()`, or `ConfigureLogging`.
- Service-layer logging.

---

## STOP

Scope review complete. No implementation performed. Recommendation: **Option A** (single commit, 8 files,
4 production + 4 test), priority order Dashboard → Calendar → Accounting → MobileOtpLogin.
