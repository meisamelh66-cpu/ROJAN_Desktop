# ROJAN AI — TEAM 3 — PHASE 8.12 VIEWMODEL LOGGING HARDENING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No commit, no push, no merge, no rebase, no amend, no source change.**
**Mode:** READINESS ONLY — this gate confirms the exact diff, architecture safety, and staging list
before Phase 8.13 (commit execution) is authorized.

**Branch:** `feature/team3-desktop-completion`
**HEAD:** `94fca6a` (`git rev-parse HEAD` this turn — unchanged, no drift)
**Predecessors:** `ROJAN_PHASE8_9_LOGGING_COVERAGE_AUDIT_v1.md` (audit),
`ROJAN_PHASE8_10_LOGGING_HARDENING_SCOPE_REVIEW_v1.md` (scope), `ROJAN_PHASE8_11_LOGGING_IMPLEMENTATION_REPORT_v1.md` (impl).

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `94fca6af883c2cbd6faaf62256efd5159c28312b` |
| Branch | `feature/team3-desktop-completion` |
| Staged files | **none** (`git diff --cached` empty) |
| Modified tracked files | **6** — 3 production + 3 test (listed below) |
| Deleted / renamed tracked files | none |
| Untracked files | `.md` reports only (this engagement's audit trail) — **no untracked code** |

```
git status --porcelain (tracked):
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
```

`git diff --stat`: `6 files changed, 186 insertions(+), 10 deletions(-)`

**Confirmed: no unrelated tracked changes.** Every modified file is on the Phase 8.11 authorization's
allow-list.

---

## B. Diff Scope (Task 2)

### B.1 Production files (3 — exactly as expected)

| File | +/− | What changed |
|---|---|---|
| `Dashboard/DashboardPageViewModel.cs` | +11 / −1 | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<DashboardPageViewModel> _logger` field; ctor +4th param `ILogger<DashboardPageViewModel>? logger = null`; `_logger = logger ?? NullLogger<…>.Instance`; +`[LoggerMessage] LogLoadFailed`; +1 call in the existing `LoadAsync` catch |
| `Calendar/CalendarPageViewModel.cs` | +13 / −3 | same shape; ctor +3rd param; +`[LoggerMessage] LogLoadFailed(string operation, Exception)`; +3 calls (`InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` catches) |
| `Accounting/AccountingPageViewModel.cs` | +14 / −2 | same shape; +own `ILogger<AccountingPageViewModel> _logger` field beside the pre-existing pass-through `_posCheckoutLogger`; ctor param **appended after** `posCheckoutLogger` (positional callers unaffected); +**static-form** `[LoggerMessage] LogOperationFailed(ILogger, string, Exception)` (two logger fields → `SYSLIB1020`, same resolution as `App.LogUnhandledException`); +2 calls (`LoadAsync`, `SearchAsync` catches) |

### B.2 Test files (3 — the corresponding three)

| File | +/− | What changed |
|---|---|---|
| `Dashboard/DashboardPageViewModelTests.cs` | +31 / −2 | +2 `using`s; `CreateSut` gains `RecordingLogger<…>? logger = null`; **+2 tests** |
| `Calendar/CalendarPageViewModelTests.cs` | +68 / −1 | +2 `using`s; **+4 tests** (the 15 existing direct `new` sites untouched — optional 3rd param) |
| `Accounting/AccountingPageViewModelTests.cs` | +50 / −3 | +2 `using`s; `MakeSut` gains `RecordingLogger<…>? logger = null` (passed as named arg `logger:`); **+3 tests** |

**No existing test body was modified** — only two helper signatures (`CreateSut`, `MakeSut`) gained a
trailing optional parameter that defaults to `null`.

### B.3 Confirmed NOT changed

| Area | Evidence |
|---|---|
| **DI** | `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` — not in the diff. `AddLogging()` (Infrastructure) already supplies open-generic `ILogger<T>`; the 3 targets are `AddTransient` and receive the real logger automatically |
| **Interfaces** | No `I*.cs` in the diff. `IDashboardQueryService` / `ICalendarQueryService` / `IServiceQueryService` / `IInvoiceQueryService` / `IPaymentQueryService` etc. untouched — the change is entirely inside the concrete ViewModel classes |
| **Domain** | No `Rojan.Desktop.Domain` file in the diff |
| **Backend contracts** | No API client, DTO, or contract file touched |
| **RBAC** | No permission gate, `RolePermissions`, `IPermissionEngine`, or `IBackendPermissionGate` file touched. Dashboard's existing `AccountingView` KPI filter is unchanged (its test still passes) |
| **Authentication** | No auth file touched. `MobileOtpLoginViewModel` (the auth-flow target from scope-review P4) was **deliberately excluded** from this phase |
| **Navigation** | No `NavigationService` / `INavigationService` file touched |

---

## C. Architecture Validation (Task 3)

### C.1 Logging pattern

| Check | Result |
|---|---|
| `ILogger<T>` usage | Instance field `ILogger<T> _logger`, constructor-injected. Dashboard/Calendar use the instance-form `[LoggerMessage]`; Accounting uses the static form (`ILogger` passed explicitly) — both are pre-existing patterns in this codebase (`BookingPageViewModel` / `App` respectively) |
| `NullLogger<T>` fallback | `_logger = logger ?? NullLogger<T>.Instance` in all 3 — verified from the diff. Makes the optional parameter non-breaking; proven by the 3 `NoLoggerSupplied_UsesNullLogger_…` tests |
| `[LoggerMessage]` source generation | Used in all 3 (not raw `_logger.LogError`) — required because `Directory.Build.props` sets `TreatWarningsAsErrors=true` with CA1848 active; also the established convention. Build is clean (§D), so the generator is satisfied |
| Existing exception flow unchanged | **Confirmed line-by-line.** Every catch keeps its exact `catch (Exception exception)` filter, its `#pragma warning disable CA1031`, its `ErrorMessage = exception.Message;` and `State = DashboardState.Error;`. The only addition is one `Log…(…, exception)` call placed **after** those lines. No catch removed, no rethrow, no swallow-behaviour change. Accounting `SearchAsync`'s log call sits **inside** the pre-existing `if (searchText == SearchText)` stale-result guard, so out-of-order-completion behaviour is unchanged |
| Architecture-test impact | `DependencyDirectionTests` does not forbid `Microsoft.Extensions.Logging.Abstractions` (only Infrastructure/Domain/Shell/EF); `ViewModelTestabilityTests` forbids only `System.Windows.Threading`/`Controls` — neither introduced. **7/7 pass (§D)** |

### C.2 No sensitive Authentication data logged — verified

- **No authentication ViewModel is in this commit.** `MobileOtpLoginViewModel` (phone numbers, OTP
  codes, tokens) is untouched and deferred.
- **Log message templates contain no identifiers or payload:**
  - Dashboard: `"Dashboard overview load failed."`
  - Calendar: `"Calendar availability load failed. Operation={Operation}"` — `{Operation}` is a compile-time
    method name (`nameof(InitializeAsync)` etc.), never user or session data. `specialistId` / `serviceId`
    are **not** in the template.
  - Accounting: `"Accounting operation failed. Operation={Operation}"` — no invoice id, customer name, or
    amount in the template.
- The `Exception` object is passed to the logger (formatted by `LocalFileLoggerProvider` as
  `{ExceptionType}: {ExceptionMessage}`) — identical to what the 4 already-logging ViewModels
  (`BookingPageViewModel`, `PosCheckoutViewModel`, `SpecialistScheduleViewModel`,
  `SpecialistAvailabilityViewModel`) do, and an accepted convention for this diagnostic sink.
- `LocalFileLoggerProvider` is write-only (`%LocalAppData%\RojanDesktop\logs\`, 14-day retention, never
  read back by the app) — it cannot become a data-exfiltration or second-authority path.

---

## D. Test Validation (Task 4)

### D.1 Fresh re-run this turn (HEAD `94fca6a` + working tree)

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded. 0 Warning(s), 0 Error(s).** |
| Full test suite | **2,521 / 2,521 passing, 0 failed, 0 skipped** (Domain 456, Presentation **578**, Application 791, Infrastructure 609, Shell 80, Architecture 7) |
| Architecture tests | **7 / 7 passing** |
| Delta vs `94fca6a` baseline (2,512) | **+9** — exactly the 9 new Phase 8.11 tests; no pre-existing test changed result |

### D.2 Coverage of the authorization's required assertions

| Required | Test | ✓ |
|---|---|---|
| Dashboard — load failure logging | `Constructor_QueryServiceThrows_LogsError` (asserts `State==Error`, `ErrorMessage=="boom"`, **and** an `Error` log entry) | ✅ |
| Calendar — initialize failure | `InitializeAsync_SpecialistsQueryThrows_LogsErrorWithOperation` (`Error` log containing `"InitializeAsync"`) | ✅ |
| Calendar — daily failure | `LoadDailyAvailabilityAsync_Throws_LogsErrorWithOperation` (`Error` log containing `"LoadDailyAvailabilityAsync"`) | ✅ |
| Calendar — weekly failure | `LoadWeeklyAvailabilityAsync_Throws_LogsErrorWithOperation` (Week-mode switch, `Error` log containing `"LoadWeeklyAvailabilityAsync"`) | ✅ |
| Accounting — load failure | `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` | ✅ |
| Accounting — search failure | `SearchAsync_QueryServiceThrows_LogsErrorWithOperation` | ✅ |
| NullLogger safety | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` (Dashboard + Accounting), `NoLoggerSupplied_UsesNullLogger_InitializeFailureNeverThrows` (Calendar) | ✅ |

- Every test also asserts the **unchanged** user-visible outcome (`State`, `ErrorMessage`) alongside the
  new log assertion — proving the change is purely additive.
- Uses the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`) via `using`, the
  same cross-namespace reuse `BookingPageViewModelTests` / `PosCheckoutViewModelTests` already do. **No
  new test-infra file.**
- All 3 targets' pre-existing behaviour tests (state transitions, RBAC KPI filter, view-mode toggling,
  search filter, revenue summary, POS dialog) pass unchanged.

---

## E. Commit Plan (Task 5)

### E.1 Staging (explicit paths only — never `git add -A` / `git add .`)

```
git add src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
```

All 6 files are single-concern (ViewModel diagnostic logging only) — no interactive staging or
`git apply --cached` patch isolation needed. The `.md` reports remain untracked.

### E.2 Commit message (single isolated commit — no bundling)

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

### E.3 Post-commit follow-up (Phase 8.13)

1. Fresh validation on the new HEAD (build + full suite + architecture tests).
2. Update `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`: §B (new commit row), §E (test count
   2,512 → 2,521), §F item 1 (**partially resolved** — 3 of 4 named ViewModels done; `MobileOtpLoginViewModel`
   still pending), §G (next action).

### E.4 Explicitly deferred (not this commit)

- `MobileOtpLoginViewModel` logging (scope-review P4) — auth-flow, `Warning` level, later wave.
- `AccountingPageViewModel.CancelInvoiceAsync` missing try/catch — separate error-handling phase.
- Phase 8.9 §C.3's ~24 other unlogged broad-catch ViewModels — later waves.

---

## F. Readiness Verdict

**READY TO COMMIT.**

- Diff is complete, minimal, single-concern, and matches the Phase 8.11 authorization exactly (3
  production + 3 test files).
- Build clean, 2,521/2,521 tests green, architecture 7/7 — re-verified this turn.
- No scope leakage into DI, interfaces, Domain, backend, RBAC, Authentication, or Navigation.
- No sensitive data in any log template; no authentication ViewModel in the commit.
- Staging list and commit message specified above, ready to execute on Phase 8.13 authorization.

---

## STOP

Commit scope review complete. No commit performed. Awaiting Phase 8.13 (commit execution) authorization.
