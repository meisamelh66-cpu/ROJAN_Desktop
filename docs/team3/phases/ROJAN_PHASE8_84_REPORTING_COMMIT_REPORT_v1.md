# ROJAN AI — TEAM 3 — PHASE 8.84 — MISSING-GUARD SWEEP — REPORTING MINI-WAVE — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `525fd4b8fe5014577fcc01ff5f5e68b7cab92083`
**New HEAD:** `5640123b95d622fbc3f11a28045e267c24f16975`
**Commit subject:** `fix(desktop): guard reporting command failures`

---

## A. COMMIT

```
commit 5640123b95d622fbc3f11a28045e267c24f16975
Author: Meisam Elhaee <meisamelh66@gmail.com>
Date:   Fri Aug 28 12:11:33 2026 -0700

    fix(desktop): guard reporting command failures

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

Subject EXACT as authorized. Trailers match the Team 3 arc convention.

```
git log --oneline -4
5640123 fix(desktop): guard reporting command failures
525fd4b fix(desktop): guard organization command failures
66c8490 fix(desktop): guard inventory and invoice-cancel command failures
a5be831 fix(desktop): guard HR command failures
```

---

## B. STAGING (explicit-path only)

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Reporting/StubReportingServices.cs
git add tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs
git diff --cached --name-only        # 3
```

Never `git add .` / `git add -A`. Staged diff reviewed before commit.

`git show --stat 5640123`: **3 files changed, 184 insertions(+), 12 deletions(-)**. No new file. The 12 deletions are the 2 original command bodies re-indented into `try` form + stub returns reshaped into `<seam> is not null ? Task.FromException : <original>` ternaries + the `CreateSut` signature/body reshape. No property / validation / assertion removed; **zero existing test bodies changed**. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

**2 guarded methods in `ReportingPageViewModel`:** `ToggleSavedAsync` (`LogOperationFailed(nameof(ToggleSavedAsync))`), `DeleteSnapshotAsync` (`LogOperationFailed(nameof(DeleteSnapshotAsync))`) — each `try { existing command await + await ReloadSnapshotsAsync() + clear-on-success } catch (Exception) { ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage; HasActionError = true; log }`, with the `#pragma warning disable/restore CA1031` boundary comment.

| Area | Status |
|---|---|
| `DashboardPageViewModel` | ✅ untouched (not in commit) |
| `AnalyticsPageViewModel` | ✅ untouched |
| `ExportDialogViewModel` | ✅ untouched (the disclosed `try`/`finally`-no-`catch` gap remains for its own micro-phase) |
| **`RunReportAsync`** | ✅ untouched (not in the diff) |
| **`CancellationToken` logic** — `_runCancellation` / `CancellationTokenSource` / `token` threading / `catch (OperationCanceledException)` | ✅ untouched |
| Backend contracts / `IReportSnapshotCommandService` + all reporting interfaces + DTOs | ✅ untouched |
| Application-layer reporting services / `Fake*` reporting repositories | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| RBAC / permission gates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / `IDialogService` | ✅ untouched |
| **Strings infrastructure** — `Strings.cs` / `Strings.resx` / `Strings.en.resx` / `Strings.ar.resx` (`Common_ActionFailedMessage` already ships in `794648e`) | ✅ untouched |
| **`[LoggerMessage]` signatures** — `ReportingPageViewModel.LogOperationFailed(string operation)` and every other | ✅ untouched (only new call sites added) |
| `AsyncRelayCommand` / `RelayCommand` / `App.xaml.cs` | ✅ untouched |
| Inside `ReportingPageViewModel`: `LoadAsync`, `RunReportAsync`, `RerunSnapshotAsync`, `ReloadSnapshotsAsync`, `BuildFilters`, `OpenExportDialog`, `ApplyCatalogFilter`, ctor, `Dispose` | ✅ untouched |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

---

## D. POST-COMMIT VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | 729 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,672** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,672 / 2,672 PASS | 2,672 / 2,672 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,666 (`525fd4b`) → **2,672** (`5640123`), delta **+6** (all `Presentation.Tests`, 723 → 729).

---

## E. WHAT LANDED

### E.1 Guard status

**2 unguarded backend-connected snapshot commands are now guarded** with the app's established non-destructive in-page error pattern (Wave A–D precedent):

| Method | Before | After |
|---|---|---|
| `ToggleSavedAsync` (pin / unpin a saved report) | bare `await _snapshotCommandService.ToggleSavedAsync(...)` + `await ReloadSnapshotsAsync()` → a throw becomes an unobserved `async void` task exception → generic `App.DispatcherUnhandledException` modal dialog | on failure sets `ActionErrorMessage = Strings.Common_ActionFailedMessage` + `HasActionError = true` on an inline non-destructive property; logs `Operation=ToggleSavedAsync` once |
| `DeleteSnapshotAsync` (delete a report snapshot) | bare `await _snapshotCommandService.DeleteSnapshotAsync(...)` + `await ReloadSnapshotsAsync()` → generic modal dialog | on failure sets the same inline pair; logs `Operation=DeleteSnapshotAsync` once |

Guarding these two also covers the private `ReloadSnapshotsAsync` helper — it now has no remaining unguarded caller (`LoadAsync` / `RunReportAsync` are already guarded). `AnalyticsPageViewModel` needed nothing (audited clean, `ROJAN_PHASE8_81_*` §B.3).

- **No business-behaviour change.** Each guard wraps the existing command call + the `await ReloadSnapshotsAsync()` follow-on verbatim; saved-state / snapshot-list consistency preserved (the VM never mutates a snapshot locally — it re-reads from the query service). On failure the reload does not complete, so `RecentSnapshots` / `SavedSnapshots` keep their last-known-good contents.
- **Non-destructive & non-clobbering:** the new `ActionErrorMessage` / `HasActionError` pair (additive, private-set, no ctor change) touches **neither** `State` / `ErrorMessage` (page not blanked) **nor** `StatusMessage` (which continues to carry the last report-run result — "N rows" / "Run cancelled"). `App.DispatcherUnhandledException` no longer fires for these 2 paths.
- **Report generation / cancellation / export untouched** — `RunReportAsync` (incl. its `CancellationTokenSource` / `token` threading and `catch (OperationCanceledException)`), `RerunSnapshotAsync`, `OpenExportDialog` / `ExportDialogViewModel` are all outside the diff.

### E.2 Security result

`Reporting` carries revenue data, customer metrics, employee performance, inventory analytics, the full report-row payload, and applied-filter values. The 2 new guards use `catch (Exception)` with **no exception variable** → `Exception.Message` / backend response body / report content / customer identifiers are structurally unreachable in both the on-screen `ActionErrorMessage` (a fixed localized constant) and the log (`LogOperationFailed(string operation)` has no `Exception` parameter — operation name only). Snapshot ids are not logged or shown. **Test-enforced** with a seeded sentinel (`"backend 500: snapshot revenue-2026-Q1 total=1,850,000 customer=Amelia Hart"` — asserted absent from `logger.Entries` **and** `ActionErrorMessage`).

**Not changed (disclosed P2):** `ReportingPageViewModel`'s three pre-existing `= exception.Message` surfacings (`LoadAsync` → `ErrorMessage`; `RunReportAsync` / `RerunSnapshotAsync` → `StatusMessage`) — the "sanitize load-error surfacing" P2, for which Reporting is flagged as the top priority (`ROJAN_PHASE8_81_*` §D.2). Those methods are not in this commit.

### E.3 Logging

Reuses `ReportingPageViewModel`'s **existing** instance-form `[LoggerMessage(EventId = 1, Level = Error, "Reporting page operation failed. Operation={Operation}")] LogOperationFailed(string operation)` (Phase 8.19 Wave 2A), operation-name-only, once per guarded method. Single `ILogger` field → **no `SYSLIB1020`**. No new logger, no `ILoggerFactory`, no DI change. Distinct operation names → no duplicate logging.

### E.4 Tests

**+6** (2,666 → 2,672). Reuses `RecordingLogger<T>` + the existing Reporting stubs (with additive `Exception?` seams `ToggleSavedException` / `DeleteSnapshotException` on `StubReportSnapshotCommandService`, `GetRecentSnapshotsException` on `StubReportSnapshotQueryService` — null-path byte-identical). `CreateSut` gained 2 optional stub params; the 16 pre-existing Reporting tests are byte-unaffected and pass unchanged. Coverage: failure-does-not-throw, `StatusMessage`-not-clobbered + `State != Error`, snapshot-list preservation, reload-fails-after-success, success-clears-error, operation-only-logging ×2 with the no-leak sentinel.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 3 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `5640123`.
- Working tree after commit: tracked tree clean (`git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'` → empty).

---

## G. MISSING-GUARD SWEEP — TRACK PROGRESS

| Wave | Domain | Status |
|---|---|---|
| **A** — Customer / Service / Specialist write commands | backend-connected | ✅ **DONE** — `794648e` (12 methods, 5 VMs) |
| **B** — HR (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3) | fake-backed | ✅ **DONE** — `a5be831` (13 methods, 2 VMs) |
| **C** — Inventory (page ×3, profile ×3) + `AccountingPageViewModel.CancelInvoiceAsync` | fake-backed | ✅ **DONE** — `66c8490` (7 methods, 3 VMs) |
| **D** — Organization (`OrganizationPageViewModel` — 4 commands + 2 secondary-load setter paths) | fake-backed | ✅ **DONE** — `525fd4b` (6 paths, 1 VM) |
| **Reporting mini-wave** — `ReportingPageViewModel` (`ToggleSavedAsync`, `DeleteSnapshotAsync`) | fake-backed | ✅ **DONE** — `5640123` (2 methods, 1 VM) |
| **`ExportDialogViewModel` micro-phase** — `ExportAsync` (`try`/`finally`, no `catch`) | local file-gen | **NEXT** (needs `ILoggerFactory` plumbing — disclosed `ROJAN_PHASE8_81_*` §B.4) |
| **E** — AI Center (`AiCenterPageViewModel` ×~12) | fake-backed | pending |
| **F** — Automation tabs (`Workflows`/`ScheduledJobs`/`BusinessRules` ×~7) | fake-backed | pending |
| **G (P2)** — Workspace / Notification / Settings / CommandPalette (~28) | local / infra | pending, low priority |

---

## H. NEXT PHASE RECOMMENDATION

**Option 1 (recommended) — Phase 8.85: `ExportDialogViewModel` micro-phase.** Guard `ExportDialogViewModel.ExportAsync` (currently `try`/`finally` with no `catch`; an unexpected file-IO exception escapes as an unobserved task exception). Needs the Wave-2C treatment: `sealed` → `sealed partial`, an optional `ILogger<ExportDialogViewModel>? logger = null` ctor param + `NullLogger` fallback + instance-form `[LoggerMessage]`, **and** `ReportingPageViewModel` gaining an `ILoggerFactory? loggerFactory = null` ctor param + field to plumb it at the `new ExportDialogViewModel(...)` site (the 8.43 profile-panel pattern). ~2 files, ~4–5 tests, LOW-MEDIUM risk. Keeps the dialog-VM file-gen gap from bleeding into Wave E.

**Option 2 — Phase 8.85: Wave E — AI Center Scope Audit.** `AiCenterPageViewModel` (~12 command methods per `ROJAN_PHASE8_64_*` §D — `ReloadSessionsAsync` / `NewConversationAsync` / `TogglePinAsync` / `DeleteSessionAsync` / `ClearHistoryAsync` / `ExportSessionAsync` / `SaveSettingsAsync` / … mostly local history ops), the largest remaining wave. Defers `ExportDialogViewModel` to fold into Wave G (dialog / infra VMs).

Either is valid. **Recommend Option 1** — `ExportDialogViewModel` is a genuine P1 file-generation gap in the domain just guarded, it's small, and doing it now while the Reporting context is fresh is cheaper than a context re-load later.

---

## STOP

Phase 8.84 commit executed and validated. HEAD `5640123`. Build 0/0, 2,672/2,672 tests, architecture 7/7.
**Missing-Guard Sweep Reporting mini-wave complete** — `ReportingPageViewModel.ToggleSavedAsync` and
`DeleteSnapshotAsync` now use the app's non-destructive in-page error pattern (new `ActionErrorMessage` /
`HasActionError` pair, distinct from both `State`/`ErrorMessage` and `StatusMessage`); no revenue / customer /
report-content exposure; report generation / cancellation / export untouched. Checkpoint updated
(`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`).
**Next: Phase 8.85 — recommended: `ExportDialogViewModel` micro-phase Scope Audit (else Wave E — AI Center Scope Audit).** Awaiting authorization.
