# ROJAN AI — TEAM 3 — PHASE 8.56 — SPECIALIST PAGE LOGGING (WAVE 2D / final P1) — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `5b7f6ca` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_55_SPECIALIST_PAGE_LOGGING_SCOPE_REVIEW_v1.md`, `ROJAN_PHASE8_54_REMAINING_VIEWMODEL_GAP_AUDIT_v1.md` §E/§F.
**Scope:** `SpecialistPageViewModel` self-logging only — the last uninstrumented swallowing broad catch in the Presentation layer.

---

## A. FILES CHANGED (2 — both modified, 0 new)

`git diff --stat`: **2 files changed, 64 insertions(+), 1 deletion(-)**

| # | File | Change |
|---|---|---|
| 1 | `src/…/ViewModels/Specialists/SpecialistPageViewModel.cs` | `sealed class` → `sealed partial class`; `+ using Microsoft.Extensions.Logging.Abstractions;` (for `NullLogger`); 1 **static-form** `[LoggerMessage]` (`private static partial void LogOperationFailed(ILogger logger, string operation)` — **no `Exception` parameter**); 1 call site in the `LoadAsync` catch. **No new field. No new ctor parameter.** |
| 2 | `tests/…/Specialists/SpecialistPageViewModelTests.cs` | +3 tests (`using Microsoft.Extensions.Logging;` already present). No existing test body changed. |

The **1 deletion** is the single-line `public sealed class SpecialistPageViewModel : ViewModelBase` replaced by its `sealed partial` form — no behavioural line removed.

### A.1 NOT touched

`SpecialistProfileViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`, the other profile panels, `BookingWizardViewModel`, `BookingPageViewModel`, DI registration, Domain, Infrastructure, Shell, Application, backend contracts / DTOs / interfaces, RBAC, authentication, navigation, `StubSpecialistQueryService` / `StubSpecialistCommandService` / `StubSpecialistProfileQueryService`, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`. The `SelectedSpecialist` setter's `new SpecialistProfileViewModel(...)` call, `CreateSpecialistAsync`, `OnProfileSpecialistUpdated`, `ClearFilters`, `_scheduleLogger`, `_availabilityLogger`, `_loggerFactory` — all unchanged.

---

## B. STATIC `[LoggerMessage]` DESIGN

```csharp
// Static form (ILogger passed explicitly) because this class already holds two ILogger
// fields (_scheduleLogger / _availabilityLogger, forwarded to the profile child's
// grandchildren) - an instance-form [LoggerMessage] plus a third ILogger field would trip
// SYSLIB1020. Same shape as Accounting.AccountingPageViewModel. No Exception parameter:
// the log line carries only the operation name (Phase 8.15+ security rule).
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist page operation failed. Operation={Operation}")]
private static partial void LogOperationFailed(ILogger logger, string operation);
```

- **Static form** — the `ILogger` is a **method parameter**, not a field, so the source generator's multi-field check (`SYSLIB1020`) does not apply regardless of how many `ILogger` fields the class holds. Direct precedent: `AccountingPageViewModel.LogOperationFailed(ILogger, string, Exception)`.
- **Signature `(ILogger logger, string operation)` — no `Exception` parameter.** This deliberately diverges from `AccountingPageViewModel`'s *legacy* `(ILogger, string, Exception)` static form: the operation-name-only rule adopted from Phase 8.15 onward forbids passing the exception to the logger.
- **`EventId = 1`, `Level = LogLevel.Error`** — consistent with every other page/profile VM (clears the `LocalFileLoggerProvider` `Warning` floor).

### B.1 Logger source — no new field, no new ctor param

Per the Phase 8.56 authorization ("Do NOT add `ILogger<SpecialistPageViewModel>` field", "Use existing logger infrastructure"), the logger is derived **inline at the call site** from the `ILoggerFactory` the class already takes (Phase 8.51):

```csharp
LogOperationFailed(
    _loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance,
    nameof(LoadAsync));
```

- `_loggerFactory` is `ILoggerFactory?` — registered by `AddLogging()`, injected by DI in production → a real `ILogger` with category `…SpecialistPageViewModel`.
- When `_loggerFactory` is `null` (no DI, or a test that passes none) → `NullLogger<SpecialistPageViewModel>.Instance` → the call is a safe no-op.
- `ILoggerFactory.CreateLogger` caches loggers by category internally (the default `LoggerFactory` does), and this is the rare error path — negligible cost.
- **Zero constructor-signature change** → every one of the ≥25 existing `new SpecialistPageViewModel(...)` call sites (production `AddTransient` resolution + all tests) compiles and runs unchanged.

---

## C. SYSLIB1020 RESOLUTION

| Check | Result |
|---|---|
| Class held 2 `ILogger` fields (`_scheduleLogger`, `_availabilityLogger`) before this change | ✅ unchanged — still 2, no 3rd `ILogger` field added |
| `[LoggerMessage]` form | **static** (`LogOperationFailed(ILogger logger, string operation)`) — field-count-agnostic |
| Instance-form `[LoggerMessage]` avoided | ✅ — an instance-form partial method + a 3rd `ILogger` field would have emitted `SYSLIB1020`, which `TreatWarningsAsErrors=true` turns into a build failure |
| `dotnet build -c Debug` | **Build succeeded. 0 Warning(s) 0 Error(s)** — no `SYSLIB1020` |

---

## D. SECURITY REVIEW

The only log line this change can produce:
```
<ts> [Error] …SpecialistPageViewModel: Specialist page operation failed. Operation=LoadAsync
```

| Aspect | Confirmed |
|---|---|
| `Exception` object | **never passed** — static `[LoggerMessage]` signature is `(ILogger logger, string operation)`, no `Exception` param |
| `Exception.Message` | **never logged** — call site passes `nameof(LoadAsync)` only; the pre-existing `ErrorMessage = exception.Message` (UI) is unchanged, never routed to the logger |
| Backend response bodies (`ApiException.Message`) | never logged |
| **Specialist data** — `FullName` / `Title` / **`Email`** / **`Phone`** / `Bio` / `Status` from the loading `SpecialistDto[]` | never referenced by the log call |
| **Search / filter data** — `SearchText` / `SelectedSkill` / status filter (`BuildFilter()`) | never referenced |
| Identifiers — specialist ids, `_filterVersion` | never logged |
| Tokens / session | not held by this VM |
| Level / EventId | `Error` / `1` |
| Behaviour | `#pragma` unchanged; `ErrorMessage` / `State` assignments unchanged; the call sits **inside** the existing `if (requestVersion == _filterVersion)` staleness guard and **after** `State = DashboardState.Error;` — log strictly appended last |

**Test-enforced:** `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` seeds `"Jordan Lee / jordan.lee@rojan.example / 555-0100 / balayage bio / search=colour"` into the thrown exception and asserts `Assert.DoesNotContain(PageSecret, entry.Message)` + `Assert.Contains("Operation=LoadAsync", entry.Message)` + category contains `SpecialistPageViewModel`.

---

## E. TESTS

### E.1 Added (3) — `SpecialistPageViewModelTests.cs`

| # | Test | Asserts |
|---|---|---|
| 1 | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` | throwing filter query + `RecordingLoggerFactory`; `State == DashboardState.Error`; one `Error` entry, category contains `SpecialistPageViewModel`, message contains `Operation=LoadAsync`, seeded PII/search secret absent |
| 2 | `LoadAsync_Failure_WithoutLoggerFactory_UsesNullLogger_NeverThrows` | throwing filter query, **no** logger factory; `State == DashboardState.Error`, `ErrorMessage == "boom"`, no throw |
| 3 | `LoadAsync_StaleFailure_SupersededByNewerLoad_LogsNothing` | slow first load (TCS) + a newer load via `SearchText = "colour"` bumps `_filterVersion`; then the stale first load faults with the seeded secret → asserts `State == DashboardState.Empty` (newer result stands) **and** `loggerFactory.Entries` is **empty** (the log call respects the `requestVersion == _filterVersion` guard) |

Test-double note: `StubSpecialistQueryService.SearchSpecialistsAsync(SpecialistSearchFilter)` delegates to `_searchSpecialistsByFilter`, which **defaults to `(_, ct) => _getSpecialists(ct)`** — so `new StubSpecialistQueryService(_ => Task.FromException<IReadOnlyList<SpecialistDto>>(...))` makes `LoadAsync`'s filter query throw with **no stub change**. `RecordingLogger<T>` / `RecordingLoggerFactory` reused (the test file is already in namespace `…Tests.Specialists`). **No shared-stub change, no new test helper, no existing test body modified.**

### E.2 Behaviour preservation

- All pre-existing `SpecialistPageViewModelTests` (~25) pass unchanged — directory load/empty/error, filter/search, auto-selection, `SelectedSpecialist` setter constructing the child + wiring `SpecialistUpdated`, `CreateSpecialistAsync`, `ClearFilters`, `OnProfileSpecialistUpdated` reload.
- The Phase 8.51 `LoggerFactory_ForwardedToSpecialistProfileChild_…` test still passes: its `queryService` returns a valid list → `SpecialistPageViewModel.LoadAsync` succeeds → no page-level log → its `Assert.Single(loggerFactory.Entries)` still sees only the child's entry.

### E.3 Fresh full run (working tree, uncommitted)

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **666** | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,609** | **0** | **0** |

Delta from baseline `5b7f6ca` (2,606): **+3** (Presentation.Tests 663 → 666).

---

## F. VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → 2,609 / 2,609 passing   0 failed   0 skipped
Architecture tests                → 7 / 7 passing
```

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,609 / 2,609 | 2,609 / 2,609 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## G. COMMIT READINESS

| Gate | Status |
|---|---|
| Scope = `SpecialistPageViewModel` + its test file only | ✅ |
| No `SpecialistProfileViewModel` / other profile panels / BookingWizard / BookingPageViewModel / DI / Domain / backend / RBAC / auth / navigation change | ✅ (not in `git status`) |
| **No `ILogger<SpecialistPageViewModel>` field added** — logger derived inline from the existing `_loggerFactory` | ✅ |
| **No new ctor parameter** — every existing call site unchanged | ✅ |
| Static-form `[LoggerMessage]`, `(ILogger logger, string operation)` — no `Exception` param → no `SYSLIB1020` | ✅ (build 0/0) |
| 1 log call, `nameof`-only, inside the existing staleness guard, after the unchanged `ErrorMessage`/`State` | ✅ |
| No specialist name/email/phone/bio, no search text, no backend body, no exception object/message reachable | ✅ (test-enforced) |
| Existing logger behaviour (`_scheduleLogger` / `_availabilityLogger` / `_loggerFactory` → child) preserved | ✅ |
| No shared stub modified; no existing test body changed; no new file | ✅ |
| Build 0/0 · Tests 2,609/2,609 · Architecture 7/7 | ✅ |

Working tree: **2 files** — `git status --porcelain`:
```
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs
```

Recommended commit subject (per Phase 8.55 §F.5): `fix(desktop): add ViewModel diagnostic logging (specialist page)`

This is the **last P1 item**. After it commits, the checkpoint's "logging coverage: final" statement (Phase 8.54 §F.1) can be recorded and the logging track closed; the P2 legacy-`[LoggerMessage]`-harmonization remains a separately-scoped future option.

---

## STOP

Implementation complete. Build 0/0, 2,609/2,609 tests, architecture 7/7. Working tree modified across
exactly 2 files (1 production + 1 test). **Nothing committed, pushed, merged, rebased, or amended.**
HEAD remains `5b7f6ca`. Awaiting Phase 8.57 commit scope review.
