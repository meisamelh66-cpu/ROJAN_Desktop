# ROJAN AI — TEAM 3 — PHASE 8.13 VIEWMODEL LOGGING HARDENING (WAVE 1) — COMMIT REPORT v1

**Type:** Commit executed + fresh post-commit validation. **Not pushed, not merged, not rebased, not amended.**
**Branch:** `feature/team3-desktop-completion`

---

## A. Commit Hash

**`2453a7fe0717bad9150492ac68f87056661e2a40`** (`2453a7f`)

- Parent: `94fca6a` (`fix(desktop): bound navigation back-stack depth`)
- Author: Meisam Elhaee — Thu Aug 27 2026 15:15:20 -0700
- Message subject: `fix(desktop): add ViewModel diagnostic logging (wave 1)` (exactly as authorized)
- Trailers: `Co-Authored-By: Claude Sonnet 5`, `Claude-Session: …`

```
git log --oneline -3
2453a7f fix(desktop): add ViewModel diagnostic logging (wave 1)
94fca6a fix(desktop): bound navigation back-stack depth
801cc65 fix(desktop): improve authentication error handling UX
```

---

## B. Files Committed

```
git show --stat 2453a7f
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs        | 16 ++++-
 src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs            | 14 ++++-
 src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs          | 12 +++-
 tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs      | 53 ++++++++++++++++-
 tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs          | 69 ++++++++++++++++++++++
 tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs        | 32 +++++++++-
 6 files changed, 186 insertions(+), 10 deletions(-)
```

**Exactly the 6 authorized files — 3 production + 3 test. Nothing else.**

| File | Change |
|---|---|
| `Dashboard/DashboardPageViewModel.cs` | `sealed`→`sealed partial`; +`ILogger<DashboardPageViewModel> _logger`; ctor +optional `ILogger<…>? logger = null` (4th); `NullLogger` fallback; +`[LoggerMessage] LogLoadFailed(Exception)`; +1 call in the existing `LoadAsync` catch |
| `Calendar/CalendarPageViewModel.cs` | same shape; ctor +optional param (3rd); +`[LoggerMessage] LogLoadFailed(string operation, Exception)`; +3 calls (`InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` catches) |
| `Accounting/AccountingPageViewModel.cs` | same shape; +own `ILogger<AccountingPageViewModel> _logger` beside the pre-existing pass-through `_posCheckoutLogger`; ctor param appended **after** `posCheckoutLogger`; +**static-form** `[LoggerMessage] LogOperationFailed(ILogger, string, Exception)` (two logger fields → `SYSLIB1020`, resolved the same way as `App.LogUnhandledException`); +2 calls (`LoadAsync`, `SearchAsync` catches) |
| 3 test files | +9 `[Fact]` tests total (Dashboard 2, Calendar 4, Accounting 3); `CreateSut`/`MakeSut` helpers gain an optional `RecordingLogger<T>?` param; +2 `using`s each. **No existing test body modified.** |

---

## C. Scope Verification

Performed on the **staged** diff before committing (`git diff --cached`), re-confirmed from
`git show 2453a7f`:

| Check | Result |
|---|---|
| Staging method | `git reset` to clear the index, then **6 explicit `git add <path>`**. **No `git add .`, no `git add -A`.** |
| Staged file count | Exactly 6, all authorized |
| Unstaged tracked changes at commit time | none (`git diff --name-only` empty) |
| `.md` reports staged | none — all remain untracked audit-trail artifacts |
| Working tree after commit | **clean** (0 modified/deleted tracked); untracked = `.md` reports only |
| DI (`ServiceCollectionExtensions.cs`) | **not touched** |
| Interfaces | **not touched** — no `I*.cs` in the diff |
| Domain | **not touched** — no `Rojan.Desktop.Domain` file in the diff |
| Backend contracts | **not touched** |
| RBAC | **not touched** — Dashboard's existing `AccountingView` KPI filter unchanged |
| Authentication | **not touched** — `MobileOtpLoginViewModel` deliberately excluded from this wave |
| Navigation | **not touched** |
| Push / merge / rebase / amend | **none performed** — single fresh commit on `94fca6a` |

---

## D. Logging Architecture Confirmation

| Aspect | Confirmed |
|---|---|
| `ILogger<T>` field | instance field `ILogger<T> _logger`, constructor-injected via an optional `= null` parameter |
| `NullLogger<T>` fallback | `_logger = logger ?? NullLogger<T>.Instance` in all 3 — proven by the 3 `NoLoggerSupplied_UsesNullLogger_…` tests (a direct-`new` with no logger + a throwing dependency never throws) |
| `[LoggerMessage]` source generation | all 3 use source-generated partials, not raw `_logger.LogError` — required (CA1848 active under `TreatWarningsAsErrors`) and the established convention. Dashboard/Calendar: instance form (matches `BookingPageViewModel`); Accounting: static form (matches `App.LogUnhandledException`, forced by its two logger fields) |
| Existing exception flow | **unchanged.** Every catch keeps its `catch (Exception exception)` filter, `#pragma warning disable CA1031`, `ErrorMessage = exception.Message;`, `State = DashboardState.Error;`. The only addition is one `Log…(…, exception)` call **after** those lines. No catch removed, no rethrow, no swallow-behaviour change. Accounting `SearchAsync`'s log sits inside the pre-existing stale-result guard |
| Level | `Error` for all boundaries — clears the `LocalFileLoggerProvider` `Warning` floor, reaches `%LocalAppData%\RojanDesktop\logs\` |
| No sensitive data | No authentication ViewModel in the commit. Templates carry only a compile-time `nameof(...)` operation name — no ids, names, amounts, phone numbers, or tokens. The `Exception` object is passed (formatted as `{Type}: {Message}` by the sink) — identical to the 4 already-logging ViewModels |
| Architecture tests | `DependencyDirectionTests` does not forbid `Microsoft.Extensions.Logging.Abstractions`; `ViewModelTestabilityTests` forbids only `System.Windows.Threading`/`Controls` — neither introduced. **7/7 pass** |

Self-logging ViewModel coverage after this commit: **7 of 56** (`BookingPageViewModel`,
`PosCheckoutViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`,
`DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel`).

---

## E. Validation Results — Fresh, Post-Commit (HEAD = `2453a7f`)

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
| Rojan.Desktop.Presentation.Tests | **578** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,521** | **0** | **0** |

### E.3 Test count delta

| | Total | Presentation.Tests |
|---|---|---|
| Baseline `94fca6a` | 2,512 | 569 |
| **New HEAD `2453a7f`** | **2,521** | **578** |
| Delta | **+9** | +9 |

All +9 are the new Wave 1 logging tests. No pre-existing test changed result.

### E.4 Architecture tests

**7 / 7 passing** — unchanged.

### E.5 Expected vs actual (per authorization)

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,521 / 2,521, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## F. Remaining Backlog

### F.1 Logging coverage — remaining

| Item | Status | Priority |
|---|---|---|
| **`MobileOtpLoginViewModel`** — `Warning`-level log on the unexpected-`ApiException` fallthrough in the 3 OTP flows | Scoped (Phase 8.10 §P4), **not implemented** — deliberately excluded from Wave 1 | **Next logging item** — auth-flow, low urgency (`HttpApiClient` already logs the HTTP failure) |
| **Wave 2** — the ~24 other ViewModels with an unlogged broad `catch (Exception)` (Phase 8.9 §C.3): `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`, `ReportingPageViewModel`, `AnalyticsPageViewModel`, `OrganizationPageViewModel`, `SalonPageViewModel`, `AiCenterPageViewModel`, the 5 Automation tab VMs, and the rest | Identified, not scoped | P3 (aggregate P2), later phases, grouped by module |
| Service-layer logging (Application/Infrastructure services without `ILogger`) | Inventoried in Phase 8.9 §B.3 | P3, out of the ViewModel-coverage track |

### F.2 Non-logging backlog (unchanged from checkpoint §F)

| Item | Status |
|---|---|
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry risk | Documented, unresolved — blocks Accounting's eventual backend connection specifically |
| `AccountingPageViewModel.CancelInvoiceAsync` — missing try/catch (surfaced Phase 8.10) | Deferred to a dedicated error-handling phase |
| `CancellationToken` propagation — `CommandPaletteViewModel` (Search) highest value | Planned, not started |
| Startup UX — no progress indicator across `App.OnStartup`'s 13 blocking stages | Planned, not started |
| RBAC migration for the 6 still-local domains, once each gets backend integration | Sequenced future work, blocked per-domain on backend contract |
| Calendar's dead EF migration/tables (3) | Disclosed tech debt, deferred |
| `RolePermissions` dead enum members (`CustomerEdit`/`ServiceEdit`/`SpecialistEdit`) | Cleanup opportunity, low urgency |

**Upstream-blocked (not Team 3 actionable):** Inventory, HR, Accounting backend integration — all blocked
on Backend/Team 1; Desktop-side prep complete since Phase 8.0.

**No P0. No P1.** Recommended next action: `MobileOtpLoginViewModel` logging (completes the Phase 8.2
named-ViewModel set), or begin Wave 2 by module.

---

## STOP

Commit executed (`2453a7f`), fresh validation green (build 0/0, 2,521/2,521 tests, architecture 7/7),
report written, checkpoint updated. No push, no merge, no rebase, no amend. Awaiting next authorization.
