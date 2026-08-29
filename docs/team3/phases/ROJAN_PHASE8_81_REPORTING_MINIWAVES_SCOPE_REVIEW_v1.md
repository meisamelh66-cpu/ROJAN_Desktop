# ROJAN AI — TEAM 3 — PHASE 8.81 — MISSING-GUARD SWEEP — REPORTING MINI-WAVE — SCOPE REVIEW v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `525fd4b8fe5014577fcc01ff5f5e68b7cab92083`
**Objective:** Audit the remaining Reporting-related command-failure boundaries before Wave E (AI Center), and define the Reporting mini-wave guard scope, using the Wave A–D pattern (`794648e`, `a5be831`, `66c8490`, `525fd4b`).

---

## A. GIT STATE

```
git rev-parse HEAD        → 525fd4b8fe5014577fcc01ff5f5e68b7cab92083
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `525fd4b` (Wave D / Organization commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `525fd4b` guard organization · `66c8490` guard inventory and invoice-cancel · `a5be831` guard HR |

Baseline test suite (checkpoint §E, `525fd4b`): **2,666 / 2,666** — Domain 456, Application 791, Presentation 723, Infrastructure 609, Shell 80, Architecture 7.

---

## B. REPORTING INVENTORY

### B.1 ViewModels

| ViewModel | Role | Relevance |
|---|---|---|
| `src/…/ViewModels/Reporting/ReportingPageViewModel.cs` | report catalog + Report Viewer (Run/Cancel) + Saved/Recent snapshots + Export | **the mini-wave target** |
| `src/…/ViewModels/Reporting/ExportDialogViewModel.cs` | the Export dialog (`ShowDialog` from `ReportingPageViewModel.OpenExportDialog`) | **disclosed gap** — §B.4 |
| `src/…/ViewModels/Analytics/AnalyticsPageViewModel.cs` | Analytics Dashboard (period switch + KPI cards + charts) | **clean — nothing to guard** (§B.3) |
| `src/…/ViewModels/Reporting/FilterEntryViewModel` | one filter row (value + type) — pure state holder, no commands, no `await` | out of scope |

`ReportingPageViewModel` is already `sealed partial` + `IDisposable`, with a **single** `ILogger<ReportingPageViewModel>` field and an instance-form operation-name-only `[LoggerMessage(EventId = 1, Level = Error, "Reporting page operation failed. Operation={Operation}")] LogOperationFailed(string operation)` (Phase 8.19 Wave 2A). **No logging-infrastructure change needed.**

### B.2 `ReportingPageViewModel` — every user-triggered command

| # | Command → method | Kind | Current exception handling | Error/Status surface used | Logger | User impact today on failure |
|---|---|---|---|---|---|---|
| 1 | `SelectSectionCommand` | `RelayCommand`, sync — `SelectedSection = (ReportingSection)param` | n/a — cannot fail | — | — | none |
| 2 | `SelectReportCommand` | `RelayCommand`, sync — sets `SelectedReport` + `SelectedSection` | n/a | — | — | none |
| 3 | `AddFilterCommand` | `RelayCommand`, sync — `AdditionalFilters.Add(new FilterEntryViewModel(...))` | n/a | — | — | none |
| 4 | `RemoveFilterCommand` | `RelayCommand`, sync — `AdditionalFilters.Remove(...)` | n/a | — | — | none |
| 5 | `RunReportCommand` → `RunReportAsync` | `AsyncRelayCommand`, long-running, cancellable | **guarded** — `catch (OperationCanceledException)` → "Run cancelled"; `catch (Exception exception)` → **`StatusMessage = exception.Message`** + `LogOperationFailed(nameof(RunReportAsync))`; `finally { IsRunning = false; }` | `StatusMessage` (both success "N rows" and failure) | ✅ operation-name-only log | already recovered inline; but `StatusMessage` shows the raw `exception.Message` (P2 leak — §D) |
| 6 | `CancelCommand` | `RelayCommand`, sync — `_runCancellation?.Cancel()` | n/a | — | — | none |
| 7 | `OpenExportDialogCommand` → `OpenExportDialog` | `RelayCommand`, sync — `_dialogService.ShowDialog(new ExportDialogViewModel(...))` | n/a (constructs + shows a dialog) | — | — | none here; the export runs inside `ExportDialogViewModel` (§B.4) |
| 8 | `ToggleSavedCommand` → `ToggleSavedAsync` | `AsyncRelayCommand` | **NONE** — `await _snapshotCommandService.ToggleSavedAsync(snapshot.Id, !snapshot.IsSaved)` then `await ReloadSnapshotsAsync()` | none | reachable but not called | **generic `App.DispatcherUnhandledException` dialog** on a failed pin/unpin |
| 9 | `DeleteSnapshotCommand` → `DeleteSnapshotAsync` | `AsyncRelayCommand` | **NONE** — `await _snapshotCommandService.DeleteSnapshotAsync(snapshot.Id)` then `await ReloadSnapshotsAsync()` | none | reachable but not called | **generic dialog** on a failed snapshot delete |
| 10 | `RerunSnapshotCommand` → `RerunSnapshotAsync` | `AsyncRelayCommand` | **guarded** — `catch (Exception exception)` → **`StatusMessage = exception.Message`** + `LogOperationFailed(nameof(RerunSnapshotAsync))`; `finally { IsRunning = false; }` | `StatusMessage` | ✅ | already recovered inline; `StatusMessage` shows raw `exception.Message` (P2 leak — §D). Does **not** thread a `CancellationToken` (unlike `RunReportAsync`). |
| 11 | `LoadCommand` → `LoadAsync` | `AsyncRelayCommand` | **guarded** — top-level `catch (Exception exception)` → `ErrorMessage = exception.Message` + `State = Error` + `LogOperationFailed(nameof(LoadAsync))` | `State` / `ErrorMessage` (destructive) | ✅ | already recovered; `ErrorMessage` shows raw `exception.Message` (P2 leak — §D) |

**Private helper — `ReloadSnapshotsAsync`:** unguarded itself (`await GetRecentSnapshotsAsync()` + `await GetSavedSnapshotsAsync()`), but **every caller except `ToggleSavedAsync` / `DeleteSnapshotAsync` is a guarded method** (`LoadAsync`, `RunReportAsync`). So guarding #8 and #9 leaves `ReloadSnapshotsAsync` with **no remaining unguarded caller** — no separate guard for it is needed.

### B.3 `AnalyticsPageViewModel` — clean

| Command | Kind | Verdict |
|---|---|---|
| `SelectPeriodCommand` | `RelayCommand`, sync — `SelectedPeriod = (AnalyticsPeriod)param`; the `SelectedPeriod` setter fires `_ = LoadAsync()` | Category B — no guard; `LoadAsync` is self-guarded |
| `LoadCommand` → `LoadAsync` | `AsyncRelayCommand` | Category C — already guarded (top-level `catch (Exception)` → `State = Error` + `LogOperationFailed(nameof(LoadAsync))`, Phase 8.23) |

**Zero unguarded command methods. Nothing for the Reporting mini-wave to do in `AnalyticsPageViewModel`.**

### B.4 `ExportDialogViewModel.ExportAsync` — disclosed gap (recommend NOT in this mini-wave)

```csharp
private async Task ExportAsync()
{
    IsExporting = true;
    StatusMessage = string.Empty;
    try
    {
        var result = await _exportService.ExportAsync(_result, SelectedFormat).ConfigureAwait(true);
        StatusMessage = result.Success && result.FilePath is not null
            ? $"{result.Message} ({result.FilePath})"
            : result.Message;
    }
    finally { IsExporting = false; }        // ← try/finally with NO catch
}
```

- **Expected** export failures are already handled *without throwing* — `IReportExportService.ExportAsync` returns a `result` object; Pdf/Excel/Print honestly return `Success = false` + a "not yet implemented" `Message`; CSV writes a real file and returns its path. `ExportAsync` surfaces `result.Message` (+ path) in its own `StatusMessage`.
- **Unexpected** failures (disk full, permission denied mid-write, path too long, IO error) would **throw** and escape as an unobserved `async void` task exception → `App.DispatcherUnhandledException`. This is a genuine **P1** file-generation gap.
- **Why not in this mini-wave:** `ExportDialogViewModel` is a plain `sealed class` with **no `ILogger` and no `[LoggerMessage]`**. Guarding it *with* logging (the wave convention) needs the full Wave-2C treatment: `sealed` → `sealed partial`, an optional `ILogger<ExportDialogViewModel>? logger = null` ctor param, `NullLogger` fallback, an instance-form `[LoggerMessage]`, **and** `ReportingPageViewModel` gaining an `ILoggerFactory? loggerFactory = null` ctor param + field to plumb it at the `new ExportDialogViewModel(...)` site. That is 2 constructor changes across 2 files — larger and riskier than a "guard", and it mixes a dialog-VM logging-plumbing change into a snapshot-command mini-wave.
- **Recommendation:** disclose it here; handle `ExportDialogViewModel.ExportAsync` as **its own micro-phase** (or fold it into Wave G — dialog/infra VMs), where the `ILoggerFactory` plumbing can be done deliberately alongside `PosCheckoutViewModel` / other dialog VMs.

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — backend-connected write/action needing a guard** | `ReportingPageViewModel.ToggleSavedAsync`, `ReportingPageViewModel.DeleteSnapshotAsync` | **guard in Phase 8.82** |
| **B — read-only / report-generation failure** | `RunReportAsync`, `RerunSnapshotAsync` (report execution) — already inline-recovered via `StatusMessage`; `OpenExportDialog` / `CancelCommand` / `SelectSection` / `SelectReport` / `AddFilter` / `RemoveFilter` — sync UI/state-only | **do not modify** (TASK 4 — report generation is a delicate area) |
| **C — already guarded** | `LoadAsync` (top-level `State = Error`), `RunReportAsync` (`catch (OperationCanceledException)` + `catch (Exception)` → `StatusMessage` + log), `RerunSnapshotAsync` (`catch (Exception)` → `StatusMessage` + log); `AnalyticsPageViewModel.LoadAsync` | **do not modify** |
| **D — global-handler acceptable** | none in Reporting — the 2 Category-A methods are P1 (UX consistency: a failed pin/delete of a saved report should not throw a modal system dialog), the rest are Category B/C | — |
| **Disclosed gap (own micro-phase)** | `ExportDialogViewModel.ExportAsync` — `try`/`finally` with no `catch` (§B.4) | **not in this mini-wave** |

---

## D. SECURITY

Reporting is a high-sensitivity domain: **revenue data, customer metrics, employee performance, inventory analytics, the full report-row payload (`ReportResultDto.Rows`), and the applied filter values (date ranges, ids).**

### D.1 The 2 new guards — no exposure

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — `catch (Exception)` binds **no variable** in the 2 new guards; `ActionErrorMessage` is only ever `null` or the compile-time constant `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has **no `Exception` parameter`; `LocalFileLoggerProvider` renders no backend body |
| Report payloads / financial details / customer identifiers / backend exception bodies | **not exposed** on either surface — the 2 guarded methods touch a `snapshot.Id` (an opaque id) and never read a row/metric/filter value into `ActionErrorMessage` or the logger |
| Snapshot id | **not logged** (operation name only), **not shown** (generic string only) |

### D.2 Pre-existing leaks (OUT OF SCOPE — flagged for the "sanitize load-error surfacing" P2)

`ReportingPageViewModel` already has **three** `= exception.Message` surfacings that predate this sweep and are the "sanitize load-error surfacing" P2 item (checkpoint §F backlog; `ROJAN_PHASE8_65_*` §D):
- `LoadAsync` catch → `ErrorMessage = exception.Message` (destructive Error state)
- `RunReportAsync` catch → `StatusMessage = exception.Message`
- `RerunSnapshotAsync` catch → `StatusMessage = exception.Message`

These render an `ApiException`/backend body straight into the UI. **Given Reporting's data sensitivity, this is the highest-value target for the deferred P2 phase** — recommend it be prioritized there (Reporting + the parallel `Inventory` / `Accounting` / `HR` / `Organization` Load-catch leaks handled in one pass). **The Reporting mini-wave does not touch them** (Category B/C — already guarded, do not modify).

---

## E. GUARD STRATEGY

### E.1 Wave A–D pattern applies

`ReportingPageViewModel` gains one **additive** pair (private-set, `SetProperty`, **no constructor / DI change**):

```csharp
public string? ActionErrorMessage { get; private set; }   // non-destructive: never touches State/ErrorMessage/StatusMessage
public bool    HasActionError      { get; private set; }
```

Same shape as `HrPageViewModel` / `OrganizationPageViewModel.ActionErrorMessage` (Waves B–D). `Common_ActionFailedMessage` reused (no `Reporting_SaveError` string exists → **no `.resx` change**).

### E.2 Per-method transformation (identical for both)

```csharp
private async Task ToggleSavedAsync(ReportSnapshotDto snapshot)
{
    try
    {
        await _snapshotCommandService.ToggleSavedAsync(snapshot.Id, !snapshot.IsSaved).ConfigureAwait(true);
        await ReloadSnapshotsAsync().ConfigureAwait(true);   // unchanged — the guard covers a reload failure too
        ActionErrorMessage = null; HasActionError = false;
    }
#pragma warning disable CA1031 // Command boundary: a failed snapshot pin/delete must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
    catch (Exception)
#pragma warning restore CA1031
    {
        ActionErrorMessage = Strings.Common_ActionFailedMessage;
        HasActionError = true;
        LogOperationFailed(nameof(ToggleSavedAsync));
    }
}
// DeleteSnapshotAsync — identical shape, LogOperationFailed(nameof(DeleteSnapshotAsync))
```

- `catch (Exception)` with **no exception variable** in both → no `Exception.Message` / backend body / report data.
- The `await ReloadSnapshotsAsync()` stays inside the guarded block: a reload failure after a successful toggle/delete also surfaces as `ActionErrorMessage` (the snapshot list may be momentarily stale but nothing crashes) — consistent with Wave A–D keeping the follow-on reload inside the guard.
- `State` / `ErrorMessage` / `StatusMessage` are **never** set by these guards — a failed pin/delete does not blank the page and does not clobber the last report-run status.
- Logging: reuse the **existing** instance-form `[LoggerMessage]`, operation-name-only, once. Single `ILogger` field → **no `SYSLIB1020`**. Distinct operation names → no double-logging.

### E.3 Files / risk

| Item | Detail |
|---|---|
| **Production (1)** | `ViewModels/Reporting/ReportingPageViewModel.cs` — `+ ActionErrorMessage` / `HasActionError` (pair); `ToggleSavedAsync` + `DeleteSnapshotAsync` wrapped. `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` / `ReloadSnapshotsAsync` / ctor / `[LoggerMessage]` signature — untouched. |
| **Test stub (1)** | `tests/…/Reporting/StubReportingServices.cs` — `StubReportSnapshotCommandService` gains additive `Exception?` seams `ToggleSavedException` / `DeleteSnapshotException` (this file is Reporting-namespace-local, used only by `ReportingPageViewModelTests`). Optionally `StubReportSnapshotQueryService` gains a `GetRecentSnapshotsException` seam for one "reload-fails-inside-the-guard" test. |
| **Test (1)** | `tests/…/Reporting/ReportingPageViewModelTests.cs` — ~7–8 new tests. |
| **Total** | **3 files.** No new file, no `Strings.cs` / `.resx` change, no ctor / DI / service change, no new test helper. |
| **Risk** | **LOW.** 2 additive `try`/`catch` + 1 property pair; fake-backed; `RunReportAsync` / cancellation / snapshot-record / export all untouched. |

---

## F. TEST PLAN

| Category | Tests | Count |
|---|---|---|
| **Failure does not throw + error surfaced** | `ToggleSavedCommand` failure → `Record.Exception(...)` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`; `State != Error`; `StatusMessage` unchanged. Same for `DeleteSnapshotCommand`. | 2 |
| **List / state preservation** | `DeleteSnapshot` failure → `SavedSnapshots` / `RecentSnapshots` unchanged (reload not reached, or reached and re-synced); `ToggleSaved` failure → the snapshot's `IsSaved` not flipped locally | ~2 |
| **Reload-fails-inside-the-guard** | toggle succeeds but `ReloadSnapshotsAsync` throws → no throw, `HasActionError == true` | 1 |
| **Success clears error** | `ToggleSaved` fail → clear seam → succeed → `HasActionError == false`, `ActionErrorMessage == null` | 1 |
| **No sensitive-data leak** | `DeleteSnapshot` failure → `Operation=DeleteSnapshotAsync` in an `Error` entry; `DoesNotContain(sentinel)` in `logger.Entries` **and** `ActionErrorMessage` (sentinel: `"backend 500: snapshot revenue-2026-Q1 total=1,850,000 customer=Amelia Hart"`) | 1 |
| **Regression** | existing `RunReportAsync` / `LoadAsync` / cancellation tests pass unchanged | (0 new) |

**Estimated new tests: ~7–8.** Conservative suite projection: **2,666 → ~2,674**.

---

## G. COMMIT STRATEGY

**Recommendation: a single Reporting mini-wave commit.**

```
fix(desktop): guard reporting command failures
```

- Small, self-contained: 2 guarded methods in one ViewModel, 1 additive property pair, additive stub seams, ~7 tests, 3 files.
- Matches the Wave A–D cadence (one atomic commit per domain slice).
- `AnalyticsPageViewModel` needs nothing (§B.3); `ExportDialogViewModel.ExportAsync` is disclosed as a separate micro-phase (§B.4) — folding either in would either be a no-op or would drag `ILoggerFactory` plumbing into a snapshot-command commit.
- **Not deferred:** the 2 gaps are real P1 UX-consistency issues (a failed pin/delete of a saved report throwing a modal system dialog is exactly the inconsistency this sweep exists to close), and the fix is trivial and low-risk. Deferring would leave the Reporting domain the only page-VM domain with unguarded snapshot commands after Waves A–D.

Standard rhythm: 8.82 implementation (STOP before commit) → 8.83 commit scope review → 8.84 commit execution → checkpoint update.

---

## H. PHASE 8.82 RECOMMENDATION

**PHASE 8.82 — MISSING-GUARD SWEEP — REPORTING MINI-WAVE — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs`:
  - add `ActionErrorMessage` / `HasActionError` (private-set, additive; no ctor change)
  - wrap `ToggleSavedAsync` and `DeleteSnapshotAsync` in the §E.2 `try`/`catch` (the `snapshot` early-cast in the command lambda and the `await ReloadSnapshotsAsync()` follow-on stay as-is, the latter inside the guard); each catch → set the pair + `LogOperationFailed(nameof(Method))`; clear on success
  - **do not touch** `LoadAsync`, `RunReportAsync`, `RerunSnapshotAsync`, `ReloadSnapshotsAsync`, `OpenExportDialog`, `BuildFilters`, the ctor, `Dispose`, or the `[LoggerMessage]` signature
- `tests/Rojan.Desktop.Presentation.Tests/Reporting/StubReportingServices.cs`:
  - `StubReportSnapshotCommandService` — additive `Exception?` seams `ToggleSavedException` / `DeleteSnapshotException` (null-path byte-identical); optionally `StubReportSnapshotQueryService.GetRecentSnapshotsException`
- `tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs`:
  - ~7–8 new tests (§F); existing tests unchanged

**DO NOT:** modify `AnalyticsPageViewModel`; modify `ExportDialogViewModel` (disclosed gap, separate micro-phase); modify report-generation (`RunReportAsync` / `RerunSnapshotAsync`) or its `CancellationToken` handling; change any service / DI / ViewModel constructor / backend contract / RBAC / navigation / `IDialogService` / command infrastructure / `[LoggerMessage]` signature / `Strings.cs` / `.resx`; touch the pre-existing `= exception.Message` surfacings (P2). No commit.

**Risk: LOW.** 2 additive `try`/`catch` around existing awaits + one bindable property pair (no ctor, no DI). Fake-backed domain (`Fake*` reporting repositories). Report execution, cancellation, snapshot-record, and export paths all untouched.

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (single `ILogger` + instance form → no `SYSLIB1020`; no `CA1031` / `CA1848`).
- Full suite → **~2,674 / ~2,674 PASS** (Presentation 723 → ~731; Domain 456, Application 791, Infrastructure 609, Shell 80 unchanged).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_82_REPORTING_MINIWAVE_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.83 commit scope review.

**Downstream:** `ExportDialogViewModel.ExportAsync` micro-phase (or Wave G) → **Wave E — AI Center** (`AiCenterPageViewModel` ×~12) → Wave F (Automation tabs ×~7) → Wave G (P2 infra). Separately, a "sanitize load-error surfacing" P2 phase should prioritize `ReportingPageViewModel`'s three `= exception.Message` leaks (§D.2).

---

## STOP

Phase 8.81 scope review complete. HEAD `525fd4b`, tracked tree clean, baseline 2,666 / 2,666.
The Reporting mini-wave = **2 guarded methods** in `ReportingPageViewModel` (`ToggleSavedAsync`, `DeleteSnapshotAsync` — the only unguarded backend-connected snapshot commands; guarding them also covers the private `ReloadSnapshotsAsync` helper), each reusing the Wave A–D pattern + the existing `[LoggerMessage]` + `Common_ActionFailedMessage`; one additive `ActionErrorMessage`/`HasActionError` pair. `AnalyticsPageViewModel` is clean (nothing to do). `ExportDialogViewModel.ExportAsync` (try/finally, no catch) is a disclosed P1 gap recommended for its own micro-phase because it needs `ILoggerFactory` plumbing. Report generation / cancellation / export untouched. ~3 files, ~7–8 tests, one commit `fix(desktop): guard reporting command failures`.
**Recommended next: Phase 8.82 — Reporting mini-wave Implementation.** Awaiting authorization.
