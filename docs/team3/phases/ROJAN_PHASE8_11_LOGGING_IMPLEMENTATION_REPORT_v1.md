# ROJAN AI — TEAM 3 — PHASE 8.11 VIEWMODEL LOGGING HARDENING — IMPLEMENTATION REPORT v1

**Type:** Implementation complete. Build + full test suite + architecture tests all green.
**No commit performed.** `HEAD` is still `94fca6a` — this report is the gate before commit authorization.

**Branch:** `feature/team3-desktop-completion`
**Authorization:** `PHASE 8.11 — LOGGING HARDENING — IMPLEMENTATION v1` (3 targets: Dashboard, Calendar,
Accounting — MobileOtpLoginViewModel deferred to a later wave)
**Scope reference:** `ROJAN_PHASE8_10_LOGGING_HARDENING_SCOPE_REVIEW_v1.md`

---

## A. Files Changed

Exactly 6 — 3 production + 3 test, all on the authorization's allow-list. **No DI, no interface, no
Domain, no new files.**

| File | +/− | Change |
|---|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs` | +11 / −1 | `sealed`→`sealed partial`; +`ILogger<DashboardPageViewModel>` field; +optional ctor param (4th, `= null`); `NullLogger` fallback; +1 `[LoggerMessage]`; +1 log call in `LoadAsync` catch |
| `src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs` | +13 / −3 | same shape; +optional ctor param (3rd); +1 `[LoggerMessage]` (`{Operation}` discriminator); +3 log calls (`InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` catches) |
| `src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs` | +14 / −2 | same shape; +own `ILogger<AccountingPageViewModel>` field (alongside the pre-existing pass-through `_posCheckoutLogger`); +optional ctor param **appended after** `posCheckoutLogger`; +1 `[LoggerMessage]` (**static form** — see §B.3); +2 log calls (`LoadAsync`, `SearchAsync` catches) |
| `tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs` | +31 / −2 | `CreateSut` gains optional `RecordingLogger<T>?`; +2 tests |
| `tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs` | +68 / −1 | +2 `using`s; +4 tests |
| `tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs` | +50 / −3 | `MakeSut` gains optional `RecordingLogger<T>?`; +2 `using`s; +3 tests |

`git diff --stat`: `6 files changed, 186 insertions(+), 10 deletions(-)`

**Confirmed NOT touched** (authorization DO-NOT list): DI registration (`ServiceCollectionExtensions.cs`),
any interface, Domain, backend contracts, Authentication, RBAC, Calendar authority logic, Navigation.
Verified by the diff being entirely inside the 3 concrete ViewModel classes + their test files.

---

## B. Logging Pattern

### B.1 Applied shape (matches `BookingPageViewModel` / `SpecialistScheduleViewModel`)

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed partial class XxxViewModel : ViewModelBase
{
    private readonly ILogger<XxxViewModel> _logger;

    public XxxViewModel(/* existing deps unchanged */, ILogger<XxxViewModel>? logger = null)
    {
        // existing assignments unchanged
        _logger = logger ?? NullLogger<XxxViewModel>.Instance;
    }

    // inside each pre-existing broad catch, AFTER the unchanged ErrorMessage/State lines:
    //     LogLoadFailed(nameof(TheMethod), exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "... Operation={Operation}")]
    private partial void LogLoadFailed(string operation, Exception exception);
}
```

- **`[LoggerMessage]` source-generated partials**, not `_logger.LogError(...)` directly. Required:
  `Directory.Build.props` sets `TreatWarningsAsErrors=true` and CA1848 (use `LoggerMessage` delegates) is
  active in this build — a direct `_logger.LogError` call would fail the build. This is also the
  established pattern (`BookingPageViewModel`, `PosCheckoutViewModel`, `HttpApiClient`, `App`).
- **Level `Error`** for all three targets (all swallowed load failures). Clears the
  `LocalFileLoggerProvider` `Warning` floor → reaches the file.
- **`NullLogger<T>.Instance` fallback** → every existing test that constructs the VM directly keeps
  working with no change.
- Ctor parameter is **optional (`= null`) and appended last** → all existing positional call sites
  (production DI + tests) compile untouched.

### B.2 Per-target message templates

| Target | `[LoggerMessage]` | Call sites |
|---|---|---|
| Dashboard | `"Dashboard overview load failed."` (single load path, no discriminator needed) | `LoadAsync` catch |
| Calendar | `"Calendar availability load failed. Operation={Operation}"` | `InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` catches — `nameof(...)` passed as `{Operation}` |
| Accounting | `"Accounting operation failed. Operation={Operation}"` | `LoadAsync`, `SearchAsync` catches |

### B.3 One deviation, disclosed

`AccountingPageViewModel` holds **two** `ILogger` fields (`_posCheckoutLogger` for the child
`PosCheckoutViewModel`, plus the new `_logger`). The `[LoggerMessage]` source generator errors
(`SYSLIB1020`) when it cannot pick a logger field implicitly. Resolved by using the **static form** —
`private static partial void LogOperationFailed(ILogger logger, string operation, Exception exception)`
called as `LogOperationFailed(_logger, ...)` — which is the exact pattern `App.LogUnhandledException`
already uses in this codebase for the same reason. Dashboard and Calendar (one logger field each) use
the instance form, matching `BookingPageViewModel`.

### B.4 Behaviour preservation

Every catch block is **unchanged except for one appended log call after the existing lines**:
- `ErrorMessage = exception.Message;` — unchanged
- `State = DashboardState.Error;` — unchanged
- catch scope, exception filter, `#pragma warning disable CA1031` — unchanged
- no catch removed, no exception rethrown or suppressed differently, no user-facing string changed
- Accounting `SearchAsync`: the log call is **inside** the existing `if (searchText == SearchText)`
  guard, so a superseded/stale search failure still does not touch state *or* logs — consistent with the
  existing out-of-order-completion design.

---

## C. Tests Added

**+9 tests** (Presentation.Tests: 569 → 578). All green.

| Target | Test | Proves |
|---|---|---|
| Dashboard | `Constructor_QueryServiceThrows_LogsError` | on load failure: `State == Error` + `ErrorMessage == "boom"` (unchanged) **and** a `LogLevel.Error` entry recorded |
| Dashboard | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | constructing with no logger + a throwing query service never throws |
| Calendar | `InitializeAsync_SpecialistsQueryThrows_LogsErrorWithOperation` | `Error` state + `ErrorMessage` unchanged **and** an `Error` log containing `"InitializeAsync"` |
| Calendar | `LoadDailyAvailabilityAsync_Throws_LogsErrorWithOperation` | `Error` log containing `"LoadDailyAvailabilityAsync"` |
| Calendar | `LoadWeeklyAvailabilityAsync_Throws_LogsErrorWithOperation` | switch to Week mode with a throwing weekly getter → `Error` log containing `"LoadWeeklyAvailabilityAsync"` |
| Calendar | `NoLoggerSupplied_UsesNullLogger_InitializeFailureNeverThrows` | NullLogger safety |
| Accounting | `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` | `Error` state + `ErrorMessage` unchanged **and** an `Error` log containing `"LoadAsync"` |
| Accounting | `SearchAsync_QueryServiceThrows_LogsErrorWithOperation` | throwing search getter → `Error` log containing `"SearchAsync"` |
| Accounting | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | NullLogger safety |

- Uses the existing `RecordingLogger<T>` test double (`tests/.../Specialists/RecordingLogger.cs`),
  reused cross-namespace via `using Rojan.Desktop.Presentation.Tests.Specialists;` — the same way
  `BookingPageViewModelTests` and `PosCheckoutViewModelTests` already reuse it. **No new test helper.**
- The authorization's explicit test requirements — Dashboard failure logs Error; Calendar
  init/daily/weekly failures each log Error; Accounting existing failure paths log Error; NullLogger
  never throws — are **all covered**.
- Every pre-existing test in the 3 files (state transitions, RBAC KPI filtering, view-mode toggling,
  search filtering, revenue summary, POS dialog) passes unchanged.

---

## D. Validation

Run this turn on the working tree (HEAD `94fca6a` + staged-nothing):

### D.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

(Includes a fix cycle: first attempt hit `SYSLIB1020` + `CS8795` on `AccountingPageViewModel` — resolved
via the static-form `[LoggerMessage]`, §B.3 — then clean.)

### D.2 Full test suite

```
dotnet test --no-build
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **578** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,521** | **0** | **0** |

- Baseline at `94fca6a`: **2,512**. Now **2,521** = 2,512 + **9 new**. No pre-existing test changed result.
- Presentation.Tests: 569 → 578 (+9).

### D.3 Architecture tests

**7 / 7 passing** — unchanged. Confirmed:
- `DependencyDirectionTests` — `Microsoft.Extensions.Logging.Abstractions` is not a forbidden dependency
  (only Infrastructure/Domain/Shell/EF Core are); it was already a Presentation `PackageReference`.
- `ViewModelTestabilityTests` — no `System.Windows.Threading` / `System.Windows.Controls` dependency
  introduced.
- `BookingAuthorityTests`, `SharedControlsIndependenceTests` — untouched surface.

### D.4 Expected vs actual (per authorization)

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,521 / 2,521, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## E. Commit Readiness

**Ready. Not committed — stopping per the authorization's STOP CONDITION.**

- **Working tree:** the 6 authorized files modified, nothing else tracked. Untracked = `.md` reports only.
- **Proposed staging (explicit paths only — never `git add -A` / `git add .`):**
  ```
  src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
  src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
  src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
  tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
  tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
  tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
  ```
- **Proposed commit message (single isolated commit):**
  ```
  fix(desktop): add ViewModel diagnostic logging (wave 1)

  Add ILogger<T> to DashboardPageViewModel, CalendarPageViewModel, and
  AccountingPageViewModel so their broad-catch load boundaries log the
  failure at Error before surfacing the existing on-screen Error state.
  Follows the established optional-ctor-param + NullLogger<T> +
  [LoggerMessage] pattern; no DI, interface, or behaviour change - the log
  call is additive, placed after the unchanged ErrorMessage/State handling.

  Adds 9 tests (failure-logs-Error per boundary + NullLogger safety).
  ```
- **Downstream impact:** none on Authentication, Booking, Calendar authority, Shift Engine, RBAC, or
  Navigation — the diff is fully contained to 3 Presentation ViewModel internals + their tests.
- **Deferred (not in this phase):** `MobileOtpLoginViewModel` logging (scope-review P4), and
  `AccountingPageViewModel.CancelInvoiceAsync`'s missing try/catch — both for a later phase.
- **Checkpoint update owed after commit:** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §B (new commit),
  §E (test count 2,512 → 2,521), §F item 1 (partially resolved — 3 of 4 named ViewModels done), §G.

---

## STOP

Implementation and validation complete. **No commit performed.** Awaiting commit authorization.
