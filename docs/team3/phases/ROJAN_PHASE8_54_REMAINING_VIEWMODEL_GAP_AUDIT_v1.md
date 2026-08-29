# ROJAN AI — TEAM 3 — PHASE 8.54 — WAVE 2D — REMAINING VIEWMODEL GAP AUDIT v1

**Type:** Audit only. **No source change. No test change. No logger / stub / DI change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `5b7f6ca157bf32906c2bfccfc29c7fcba39fd901` — `fix(desktop): add ViewModel diagnostic logging (detail panels)` (Phase 8.51, committed 8.53)
**Reference:** `ROJAN_PHASE8_53_DETAIL_PANELS_LOGGING_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F/§G, `ROJAN_PHASE8_9_LOGGING_COVERAGE_AUDIT_v1.md` (the first coverage sweep).

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `5b7f6ca157bf32906c2bfccfc29c7fcba39fd901` |
| HEAD subject | `fix(desktop): add ViewModel diagnostic logging (detail panels)` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. Wave 2C-3c (`5b7f6ca`) committed and validated (build 0/0, 2,606/2,606, arch 7/7). This audit adds no code.

---

## B. INVENTORY

### B.1 Population reconciliation

`find src/Rojan.Desktop.Presentation/ViewModels -name "*ViewModel.cs"` (excluding `obj/`, excluding the `Mvvm/ViewModelBase.cs` base type) = **55 concrete ViewModels**. Pre-8.9 audits cited "71"; Phase 8.9 corrected to "56"; the precise current count is **55** (`LoginViewModel` was double-counted, or a since-removed VM — no functional impact; coverage is stated below against 55).

### B.2 Coverage snapshot (before Wave 2D)

| State | Count | VMs |
|---|---|---|
| **Self-logging** (own `ILogger<T>` + `[LoggerMessage]` + ≥1 instrumented catch, or the MobileOtp typed-fallthrough form) | **32** | Dashboard, Calendar, Accounting, MobileOtp (W1) · Customer(page), Service(page), Inventory(page), HR(page), Reporting (W2A) · Analytics, AiCenter, Salon, QrCodes (W2B) · Organization (W2B-2) · Support, AcceptInvite (W2C-1) · 5 Automation tabs (W2C-2) · CustomerProfile, ServiceProfile, InventoryProfile (W2C-3a) · BookingWizard, BookingPage (W2C-3b) · EmployeeProfile, InvoiceProfile, SpecialistProfile (W2C-3c) · **PosCheckout, SpecialistSchedule, SpecialistAvailability** (Shift-Engine / Booking-Checkout hardening, `da18c18`/`ea03d83`) |
| **Plumbing only** (pass-through, no self-logging, no catch) | 1 | `AutomationPageViewModel` (5 `ILogger<TChild>?` params) — not counted below |
| **Remaining** | **23** | see §C |

### B.3 Remaining 23 ViewModels — per-VM

| # | ViewModel | Parent / lifetime | Ctor takes | Own `ILogger` / `[LoggerMessage]` | Broad `catch (Exception)` | Current error handling |
|---|---|---|---|---|---|---|
| 1 | **`Specialists/SpecialistPageViewModel`** | Shell-registered `AddTransient`; page VM | 7 services + `ILogger<SpecialistScheduleViewModel>?` + `ILogger<SpecialistAvailabilityViewModel>?` + `ILoggerFactory?` (added 8.51) | **none** / **none**; `sealed class` (not `partial`) | **1** — `LoadAsync` (`:250–259`), swallowing, filter-version-guarded | `ErrorMessage = exception.Message; State = DashboardState.Error;` |
| 2 | `Security/LoginViewModel` | `AddTransient` but **no `LoginView`** exists; Shell's `LoginWindow` binds `MobileOtpLoginViewModel` only → **registered-but-unused / retired-implementation** | `IAuthenticationService`, strings | none / none | **0** — 4 **typed** catches (`ApiAuthenticationException`, `ApiConnectivityException`, `ApiTimeoutException`, `ApiException` fallthrough) in `SignInAsync` | each → a fixed `Strings.Login_Error_*`; `finally { IsBusy = false; }` |
| 3 | `Security/LoginWindowViewModel` | Shell `LoginWindow`'s DataContext | `MobileOtpLoginViewModel` | none / none | 0 | none — pure wrapper exposing `MobileLogin` |
| 4 | `Automation/AutomationPageViewModel` | `AddTransient` page VM; constructs the 5 tabs | 7 services + 5 `ILogger<TChild>?` | none / none (5 pass-through params, forwarded to `new`) | 0 | none — the tabs own all load/command logic |
| 5 | `Search/CommandPaletteViewModel` | `AddSingleton` (Shell overlay) | search providers, nav, favorites | none / none | 0 | none — every `async Task` takes a `CancellationToken`; no failure boundary (separate known debt: token propagation) |
| 6 | `Search/SearchResultRowViewModel` | row item, `new`-by `CommandPaletteViewModel` | candidate + activate delegate | none / none | 0 | none — pure row DTO wrapper |
| 7 | `Notifications/NotificationCenterViewModel` | `AddSingleton` | notification store, nav | none / none | 0 | none — subscribes to a store; no I/O boundary |
| 8 | `Notifications/NotificationGroupViewModel` | `new`-by center | group model | none / none | 0 | none — pure grouping holder |
| 9 | `Notifications/NotificationRowViewModel` | `new`-by group | notification model + delegates | none / none | 0 | none — pure row holder |
| 10 | `Notifications/ToastHostViewModel` | `AddSingleton` | toast queue | none / none | 0 | none — pure queue/animation host |
| 11 | `Notifications/ToastNotificationViewModel` | `new`-by host | toast model | none / none | 0 | none — pure holder |
| 12 | `Reporting/ExportDialogViewModel` | `new`-by `ReportingPageViewModel` | export options, confirm delegate | none / none | 0 | none — collects options, delegates the actual export to the parent |
| 13 | `Reporting/FilterEntryViewModel` | `new`-by reporting page | filter descriptor | none / none | 0 | none — pure filter-row holder |
| 14 | `Help/HelpDialogViewModel` | `new`-by Shell | static help content | none / none | 0 | none — static content |
| 15 | `Modules/PlaceholderModuleViewModel` | `new`-by nav for not-yet-built modules | module name | none / none | 0 | none — placeholder |
| 16 | `Settings/SettingsPageViewModel` | `AddTransient` page VM | settings store (local, synchronous) | none / none | 0 | none — reads/writes an in-memory/local settings object; no async I/O boundary |
| 17–23 | `Workspaces/DockedPanelViewModel`, `FloatingWindowHandleViewModel`, `PaneLeafViewModel`, `PaneSplitViewModel`, `TabViewModel`, `WorkspaceHostViewModel`, `WorkspaceOutlineViewModel` | workspace/docking tree nodes, `new`-by the host | layout models + delegates | none / none | 0 | none — pure layout-tree state; no service calls, no failure boundary |

### B.4 The single uninstrumented swallowing catch

A repo-wide scan (`grep -c "catch (Exception"` vs `grep -c "\[LoggerMessage"` across all 55 VMs) returns **exactly one** VM with a broad `catch (Exception)` and no `[LoggerMessage]`:

```
UNINSTRUMENTED: Specialists/SpecialistPageViewModel.cs   (broad catches = 1)
```

Every other broad-catch ViewModel in the Presentation layer is already instrumented.

---

## C. CLASSIFICATION

### Category A — needs logging (1)

| VM | Boundary | Why |
|---|---|---|
| **`SpecialistPageViewModel`** | `LoadAsync` (`:250–259`) | Top-level page-load broad catch; swallows the exception, surfaces `State = DashboardState.Error` + `ErrorMessage`. Identical in kind to `CustomerPageViewModel` / `ServicePageViewModel` / `InventoryPageViewModel` / `HrPageViewModel` (all instrumented in Wave 2A). It is the **last uninstrumented swallowing broad catch** in the Presentation layer. |

### Category B — no logging required (21)

| Sub-group | VMs | Reason |
|---|---|---|
| Pure state / layout holders — no service calls, no `catch` | `SearchResultRowViewModel`, `NotificationGroupViewModel`, `NotificationRowViewModel`, `ToastNotificationViewModel`, `FilterEntryViewModel`, `PlaceholderModuleViewModel`, `HelpDialogViewModel`, all 7 `Workspaces/*` | Nothing to instrument — no failure boundary exists |
| Singleton hosts / overlays — subscribe to a store or manage UI queue; no I/O `catch` | `CommandPaletteViewModel`, `NotificationCenterViewModel`, `ToastHostViewModel` | No swallowing broad `catch`; `CommandPaletteViewModel`'s CancellationToken-propagation gap is a separate, already-tracked P2 (not a logging item) |
| Local/synchronous — `SettingsPageViewModel` | reads/writes a local settings object; no async backend boundary | No `catch`; a future backend-backed settings sync would revisit this |
| Pass-through parent — `AutomationPageViewModel` | forwards `ILogger<TChild>?` to its 5 tabs; owns no load/command logic, no `catch` | Already carries the correct plumbing (Wave 2C-2); nothing to self-log |
| Thin wrapper — `LoginWindowViewModel` | exposes `MobileLogin` only | No logic |

### Category C — architecture concern (2)

| VM | Concern | Detail |
|---|---|---|
| **`SpecialistPageViewModel`** | **`SYSLIB1020` risk on instrumentation** | See §E. It already holds **2 typed `ILogger<T>` fields** (`_scheduleLogger`, `_availabilityLogger`) + **`_loggerFactory`**. Adding a 3rd `ILogger<SpecialistPageViewModel>` field **with an instance-form `[LoggerMessage]`** would fail the build with `SYSLIB1020`. Must use the **static-form `[LoggerMessage]`** pattern (the `AccountingPageViewModel` precedent) **or** derive its own logger inline from `_loggerFactory`. |
| `LoginViewModel` (`Security`) | Symmetry gap + dead code | Its generic `catch (ApiException)` fallthrough in `SignInAsync` currently logs nothing, whereas its sibling `MobileOtpLoginViewModel` logs the analogous fallthrough at `Warning` (Phase 8.15). But `LoginViewModel` has **no view** and is not reachable from Shell (retired credentials-login, kept per architecture decision §C.6). Low value — see §F P2. |

### Category C — non-defects (documented, no action)

| Item | Status |
|---|---|
| `BookingWizardViewModel.SearchNextAvailableDateAsync` (5th catch) | Deliberate, authorizer-approved, test-guarded skip (best-effort cancellable probe) — Phase 8.46 §B.3 / 8.47 |
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry | Known P2 **correctness** debt (not a logging gap); all 3 of its catches are already logged |
| **Legacy exception-passing `[LoggerMessage]` form** in 6 VMs (`PosCheckoutViewModel`, `BookingPageViewModel`, `CalendarPageViewModel`, `DashboardPageViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`) + static-legacy in `AccountingPageViewModel` | Pre-8.15 committed pattern — `[LoggerMessage(... , Exception exception)]`; `SpecialistAvailabilityViewModel` also logs `SpecialistId={SpecialistId}`. Post-8.15 rule is operation-name-only, exception never passed. See §D.3 / §F P2. |

---

## D. SECURITY FINDINGS

### D.1 Category A — `SpecialistPageViewModel.LoadAsync`

Sensitive data reachable at this boundary (via `_queryService.SearchSpecialistsAsync` failing, or the caught `exception`):

| Source | Data |
|---|---|
| `SpecialistDto[]` (the search result being loaded) | specialist `FullName`, `Title`, **`Email`**, **`Phone`**, `Bio`, status |
| `SpecialistSearchFilter` (`BuildFilter()`) | the operator's typed `SearchText` / `SelectedSkill` / status filter |
| `exception` | for an `ApiException`, the **backend response body** embedded in `.Message` |

**Recommendation — operation-name-only:**
```
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist page operation failed. Operation={Operation}")]
private static partial void LogOperationFailed(ILogger logger, string operation);   // static form, NO Exception parameter
```
call: `LogOperationFailed(_logger, nameof(LoadAsync));` as the last statement of the existing catch, after the unchanged `ErrorMessage`/`State` assignment (inside the existing `if (requestVersion == _filterVersion)` guard).

**Forbidden:** specialist name/email/phone/bio, the search-filter text, backend response bodies, `Exception.Message`, the `Exception` object. **Allowed:** `Operation=nameof(LoadAsync)` only.

### D.2 Category B

No security exposure — none of the 21 has a failure boundary that touches a logger.

### D.3 Legacy `[LoggerMessage]` forms (Category C non-defect, but a real disclosure)

The 6 legacy-form VMs pass the caught `Exception` to `[LoggerMessage]`. `LocalFileLoggerProvider` renders it as `exception.ToString()` into the daily log file. For an `ApiException` (thrown by `AuthBootstrapHttpClient`), that string embeds the **raw backend response body**. So backend response bodies for `PosCheckoutViewModel` / `BookingPageViewModel` / `CalendarPageViewModel` / `DashboardPageViewModel` / `SpecialistSchedule` / `SpecialistAvailability` failures **do reach the local log file today**.

- **Severity: P2.** Local file only, 14-day rotation, developer/support-facing, not transmitted anywhere. No PII in the message templates themselves. But it violates the operation-name-only rule adopted from Phase 8.15 onward.
- Not introduced by any Wave 2x work — all pre-8.15 committed code.

---

## E. `SpecialistPageViewModel` — SPECIAL REVIEW

### E.1 Current logger state

| Item | Value |
|---|---|
| Class | `public sealed class SpecialistPageViewModel : ViewModelBase` — **not `partial`** |
| `using` | `Microsoft.Extensions.Logging` present; **`Microsoft.Extensions.Logging.Abstractions` absent** (no `NullLogger` use yet) |
| `ILogger` **fields** | `_scheduleLogger` (`ILogger<SpecialistScheduleViewModel>?`, `:46`), `_availabilityLogger` (`ILogger<SpecialistAvailabilityViewModel>?`, `:47`) — both forwarded verbatim into `new SpecialistProfileViewModel(...)` at `:181`, which passes them on to the two grandchildren |
| `ILoggerFactory` field | `_loggerFactory` (`ILoggerFactory?`, added Phase 8.51) — used at `:181` as `_loggerFactory?.CreateLogger<SpecialistProfileViewModel>()` |
| Own `ILogger<SpecialistPageViewModel>` | **none** |
| `[LoggerMessage]` | **none** |
| Ctor params (order) | `queryService, profileQueryService, commandService, intelligenceEngine, serviceQueryService, scheduleQueryService, scheduleCommandService, scheduleLogger = null, availabilityLogger = null, loggerFactory = null` |
| Broad catch | 1 — `LoadAsync` `:250–259` (swallowing, filter-version-guarded) |
| Other methods | `CreateSpecialistAsync` (no `try`/`catch` — propagates), `OnProfileSpecialistUpdated` (`async void`, calls self-catching `LoadAsync`), `ClearFilters` (no I/O) |

### E.2 Risk level: **LOW — but must not use the instance-form `[LoggerMessage]`**

- With **instance-form** `[LoggerMessage]` + a new `ILogger<SpecialistPageViewModel> _logger` field → the class would hold **3 `ILogger` fields** + an instance `[LoggerMessage]` → **`SYSLIB1020` build failure** (`Directory.Build.props` has `TreatWarningsAsErrors=true`).
- With **static-form** `[LoggerMessage]` (`private static partial void LogOperationFailed(ILogger logger, string operation)`) → **no field-count limit applies** (static methods take `ILogger` as a parameter). Safe. This is exactly the `AccountingPageViewModel` precedent (which also carries 2 `ILogger` fields).

### E.3 Recommended remediation plan (for the follow-on implementation phase)

1. `sealed class` → `sealed partial class`; `+ using Microsoft.Extensions.Logging.Abstractions;`.
2. Add `private readonly ILogger<SpecialistPageViewModel> _logger;`. Populate it **without a new ctor param** by deriving from the logger factory it already has:
   `_logger = loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance;`
   *(Alternative: append one optional `ILogger<SpecialistPageViewModel>? logger = null` ctor param after `loggerFactory` and use `?? NullLogger`. The factory-derived form is zero-ctor-change and consistent with §E.1's existing `_loggerFactory`.)*
3. One **static-form** `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist page operation failed. Operation={Operation}")] private static partial void LogOperationFailed(ILogger logger, string operation);` — **no `Exception` parameter**.
4. `LogOperationFailed(_logger, nameof(LoadAsync));` as the last statement of the `LoadAsync` catch, inside the existing `if (requestVersion == _filterVersion)` block, after `State = DashboardState.Error;`.
5. **Untouched:** `_scheduleLogger`, `_availabilityLogger`, `_loggerFactory`, the `SelectedSpecialist` setter's `new SpecialistProfileViewModel(...)` call, `CreateSpecialistAsync`, `OnProfileSpecialistUpdated`, DI registration.
6. Tests: `SpecialistPageViewModelTests.cs` — the file already exists, is delegate-driven (`StubSpecialistQueryService(_ => Task.FromException…)`), and is in the `…Tests.Specialists` namespace (so `RecordingLogger<T>` is directly available). Add ~3: `LoadAsync` failure logs `Error` / `Operation=LoadAsync` / seeded specialist-PII secret absent; NullLogger safety (construct with no factory → no throw); optionally a stale-result guard (`requestVersion != _filterVersion` → no log). **No shared-stub change**, no new helper.

**Future (Wave 2D+ / not this pass):** if `SpecialistPageViewModel` ever needs to move its grandchild loggers off typed fields, `ILoggerFactory`-only plumbing for all three (schedule/availability/profile) would collapse the field count — a larger refactor of committed Shift-Engine code, not warranted now.

---

## F. PRIORITY ROADMAP

### P0 — must fix

**None.** No security leak introduced by Team 3 work; no crash path; no incorrect behaviour. The logging track is functionally complete for every backend-connected failure boundary.

### P1 — should fix

| Item | Scope | Commit grouping | Test requirements |
|---|---|---|---|
| **`SpecialistPageViewModel.LoadAsync` diagnostic logging** (Category A — the last uninstrumented swallowing broad catch) | 1 production file + 1 test file. `sealed partial`, factory-derived `_logger`, **static-form** `[LoggerMessage]` (SYSLIB1020-safe), 1 call site. No DI / ctor-signature / interface / stub change. | **One isolated commit** — `fix(desktop): add ViewModel diagnostic logging (specialist page)`. Follows the standard rhythm: scope review → implementation (STOP before commit) → commit scope review → commit execution. | +~3 tests: failure logs `Error`/`Operation=LoadAsync`/no-PII-leak (seeded secret), NullLogger safety, (optional) stale-result-guard no-log. Reuse `RecordingLogger<T>`; no shared-stub change. |

### P2 — optional improvement

| Item | Scope | Recommendation |
|---|---|---|
| **Legacy `[LoggerMessage]` harmonization** — 6 VMs pass `Exception` to the logger (`PosCheckout`, `BookingPage`, `Calendar`, `Dashboard`, `SpecialistSchedule`, `SpecialistAvailability`) + `AccountingPage` static-legacy; `SpecialistAvailability` also logs `SpecialistId` | Medium — 6–7 committed production files, each: drop the `Exception` param (and `SpecialistId` arg) from `[LoggerMessage]`, drop it from every call site. ~15–20 call sites. Tests: existing failure tests already assert behaviour; add/extend "no exception / no id in the log line" assertions. | Defer to a dedicated **"logging: harmonize to operation-name-only"** phase. Isolated commit `refactor(desktop): drop exception payload from diagnostic logging`. Not blocking; the risk (backend body in a local rotated file) is contained. |
| `LoginViewModel.SignInAsync` `catch (ApiException)` fallthrough — no log (vs MobileOtp which logs at `Warning`) | Tiny — 1 file, 1 `Warning`-level call, operation-name-only. **But the VM has no view and is unreachable from Shell** (retired credentials login). | **Document as intentional** (retired implementation, architecture §C.6). Instrument only if/when credentials login is re-activated. |
| `CommandPaletteViewModel` `CancellationToken` propagation | out of the logging track | Already on the backlog (§F of the checkpoint), unchanged. |
| `SettingsPageViewModel` — no failure boundary today | n/a | Revisit if/when settings gains a backend sync path. |

### F.1 "Logging coverage: final" statement (after P1 lands)

> Every ViewModel in the ROJAN Desktop Presentation layer with a swallowing broad `catch (Exception)` that surfaces a user-facing error state is instrumented with PII-safe, operation-name-only diagnostic logging at `Error` (MobileOtp at `Warning`). The remaining ~22 uninstrumented ViewModels are pure state/layout holders, thin wrappers, singleton UI hosts, or retired implementations — none has a failure boundary. One deliberate, test-guarded skip: `BookingWizardViewModel.SearchNextAvailableDateAsync` (best-effort cancellable probe). **The logging track is closed.**

---

## G. RECOMMENDED NEXT PHASE

**Phase 8.55 — SpecialistPage Logging Scope Review** (readiness only, no commit), targeting the single P1 item.

- Scope: `SpecialistPageViewModel.cs` + `SpecialistPageViewModelTests.cs`.
- Pattern: `sealed partial` + factory-derived `_logger` + **static-form** `[LoggerMessage]` (SYSLIB1020-safe, `AccountingPageViewModel` precedent) + 1 call at `LoadAsync`'s catch, operation-name-only.
- Then Phase 8.56 implementation (STOP before commit) → 8.57 commit scope review → 8.58 commit execution → checkpoint update → **close the logging track** with the §F.1 statement.
- Defer the P2 legacy-harmonization to its own later phase.

Estimated total for P1: ~1 production file (+~10 lines), ~1 test file (+~35 lines / 3 tests), 4 phases (review/impl/review/commit), coverage **32/55 → 33/55**, test count ~2,606 → ~2,609.

---

## STOP

Audit complete. No source or test change, no logger/stub/DI change, no commit/push/merge/rebase/amend.
HEAD remains `5b7f6ca`. **Awaiting Phase 8.55 authorization** (SpecialistPage Logging Scope Review — the single P1 item).
