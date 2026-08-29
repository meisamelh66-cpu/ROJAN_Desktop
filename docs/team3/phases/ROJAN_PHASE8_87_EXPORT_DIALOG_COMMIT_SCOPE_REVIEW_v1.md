# ROJAN AI — TEAM 3 — PHASE 8.87 — MISSING-GUARD SWEEP — EXPORT DIALOG MICRO-PHASE — COMMIT SCOPE REVIEW v1

**Type:** Pre-commit review. **STRICT MODE — no source change, no test change, no new file, no commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `5640123b95d622fbc3f11a28045e267c24f16975`
**References:** `ROJAN_PHASE8_85_EXPORT_DIALOG_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_86_EXPORT_DIALOG_IMPLEMENTATION_REPORT_v1.md`
**Verdict:** ✅ **READY TO COMMIT** — scope clean, 3 modified + 1 new, build 0/0, 2,678/2,678 tests, architecture 7/7.

---

## A. GIT STATE

```
git rev-parse HEAD        → 5640123b95d622fbc3f11a28045e267c24f16975
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty)   ← nothing staged
git log --oneline -3      → 5640123 guard reporting / 525fd4b guard organization / 66c8490 guard inventory and invoice-cancel
```

| Check | Result |
|---|---|
| HEAD | `5640123` (Reporting mini-wave commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Staging area | **empty** ✅ |
| Modified tracked files | **3** ✅ |
| New tracked files | **1** (`ExportDialogViewModelTests.cs` — the VM had no dedicated test file) ✅ |
| Untracked | `ExportDialogViewModelTests.cs` + `ROJAN_*.md` reports ✅ |

```
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
 M src/Rojan.Desktop.Presentation/ViewModels/Reporting/ExportDialogViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Reporting/StubReportingServices.cs
?? tests/Rojan.Desktop.Presentation.Tests/Reporting/ExportDialogViewModelTests.cs
```

`git diff --stat`: **3 files changed, 60 insertions(+), 5 deletions(-)** (modified) + the new 130-line test file. The 5 deletions are: `ExportDialogViewModel` — `-public sealed class` / `-public ExportDialogViewModel(… 3 params)`; `ReportingPageViewModel` — `-ILogger<…>? logger = null)` (→ + a `,` and the new param) / `-` the 3-arg `new ExportDialogViewModel(...)`; stub — `-return Task.FromResult(new ExportResultDto(...))`. **No method body, no assertion removed.**

Matches Phase 8.85 §G / Phase 8.86 §A exactly (2 prod + 1 stub + 1 new test).

---

## B. SCOPE VERIFICATION

### B.1 `ExportDialogViewModel.cs` — in scope

| Diff element | Verdict |
|---|---|
| `+ using Microsoft.Extensions.Logging` / `…Abstractions` | ✅ |
| `public sealed class` → `public sealed partial class` | ✅ — required for source-generated `[LoggerMessage]` |
| `+ ILogger<ExportDialogViewModel> _logger` field | ✅ additive |
| ctor `(result, exportService, dialogService)` → `(result, exportService, dialogService, ILoggerFactory? loggerFactory = null)` — **appended optional param**; `_logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ?? NullLogger<…>.Instance` | ✅ additive; existing 3-arg construction still compiles |
| `+ _actionErrorMessage` / `_hasActionError` fields + `ActionErrorMessage` / `HasActionError` properties (private-set) | ✅ additive |
| `+ [LoggerMessage(EventId = 1, Level = Error, "Export dialog operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` | ✅ — instance-form, **no `Exception` parameter** |
| `ExportAsync`: `+ ActionErrorMessage = null; HasActionError = false;` at the end of the `try`; `+ #pragma CA1031` + `catch (Exception) { ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(nameof(ExportAsync)); }` inserted **between** the existing `try` and `finally` | ✅ — the `try` body (`await _exportService.ExportAsync(...)`, `StatusMessage = result.Success … ? … : …`) and the `finally { IsExporting = false; }` are **context lines** in the diff (unchanged) |
| `ReportName` / `SelectedFormat` / `IsExporting` / `AvailableFormats` / `ExportCommand` / `CloseCommand` | ✅ not in the diff |

### B.2 `ReportingPageViewModel.cs` — the explicitly-allowed "minimal parent logger forwarding"

`git diff` — **4 substantive lines** (7 with the ctor-param wrap):
```
+ private readonly ILoggerFactory? _loggerFactory;                    // field
  …
-        ILogger<ReportingPageViewModel>? logger = null)
+        ILogger<ReportingPageViewModel>? logger = null,
+        ILoggerFactory? loggerFactory = null)                          // ctor param (appended after logger)
  …
+        _loggerFactory = loggerFactory;                               // assignment
  …
- _dialogService.ShowDialog(new ExportDialogViewModel(CurrentResult, _exportService, _dialogService));
+ _dialogService.ShowDialog(new ExportDialogViewModel(CurrentResult, _exportService, _dialogService, _loggerFactory));   // call-site arg
```

| Confirm | Result |
|---|---|
| `ILoggerFactory` **forwarding only** | ✅ — the only change is: hold an optional `ILoggerFactory?` and pass it to the child dialog. No new logging call in `ReportingPageViewModel` itself. |
| No business-behaviour change | ✅ — `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` / `ToggleSavedAsync` / `DeleteSnapshotAsync` / `ReloadSnapshotsAsync` / `BuildFilters` / `ApplyCatalogFilter` / `Dispose` / the `[LoggerMessage]` signature — **not in the diff** |
| No DI break | ✅ — `loggerFactory` is optional (`= null`); the host DI resolves `ILoggerFactory` automatically for the `AddTransient` VM; no registration change |
| Constructor compatibility preserved | ✅ — `ReportingPageViewModelTests.CreateSut` calls `new ReportingPageViewModel(catalog, execution, snapshotQuery, snapshotCommand, export, dialog, logger)` (7 positional args); the new 8th param is optional → those 22 tests compile and pass unchanged |

### B.3 `StubReportingServices.cs` — additive seams only

`StubReportExportService` gained `+ Exception? ExportException` and `+ ExportResultDto? Result`. `ExportAsync` now: records `LastFormat`, then `if (ExportException is not null) return Task.FromException<T>(...)`, else `return Task.FromResult(Result ?? new ExportResultDto(true, "Exported.", @"C:\temp\report.csv"))`. **With both seams null the return is byte-identical to before** — the 22 pre-existing Reporting tests pass unchanged.

### B.4 `ExportDialogViewModelTests.cs` — new file, 6 tests

New test file (the VM had none). Uses `RecordingLoggerFactory` (Phase 8.43) + `StubDialogService` + `StubReportExportService`. No shared helper changed.

### B.5 Confirmed UNTOUCHED

```
git diff --name-only  →  exactly 3 files + the 1 new test; all under …/Reporting/
```

| Area | Status |
|---|---|
| **`ReportExportService` implementation** (`src/…/Application/Reporting/ReportExportService.cs` — CSV write, PDF/Excel/Print placeholders) | ✅ untouched (not in `git status`) |
| **`ReportingPageViewModel` business logic** — every method except the 4-line forwarding | ✅ untouched |
| `DashboardPageViewModel` / `AnalyticsPageViewModel` | ✅ untouched |
| Backend contracts / `IReportExportService` / `ExportResultDto` / `ExportFormat` / all reporting interfaces + DTOs | ✅ untouched |
| DI registrations (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| RBAC / permission gates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / `IDialogService` | ✅ untouched |
| Shared infrastructure — `Strings.cs` / all `.resx` (`Common_ActionFailedMessage` ships in `794648e`) / `AsyncRelayCommand` / `App.xaml.cs` / every `[LoggerMessage]` signature | ✅ untouched |
| `ReportingPageViewModelTests.cs` | ✅ untouched (its 22 tests unaffected by the optional 8th ctor param) |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

---

## C. EXPORT GUARD REVIEW — `ExportAsync`

```csharp
IsExporting = true;
StatusMessage = string.Empty;
try
{
    var result = await _exportService.ExportAsync(_result, SelectedFormat).ConfigureAwait(true);   // UNCHANGED
    StatusMessage = result.Success && result.FilePath is not null                                   // UNCHANGED
        ? $"{result.Message} ({result.FilePath})"
        : result.Message;
    ActionErrorMessage = null; HasActionError = false;                                              // ADDED (clear-on-success)
}
#pragma warning disable CA1031
catch (Exception)                                                                                    // ADDED
#pragma warning restore CA1031
{
    ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(ExportAsync));
}
finally
{
    IsExporting = false;                                                                             // UNCHANGED
}
```

| Confirm | Result |
|---|---|
| **Existing export behaviour preserved** | ✅ — `await _exportService.ExportAsync(_result, SelectedFormat)` is the same call with the same args; the `StatusMessage` ternary is byte-unchanged |
| **CSV success unchanged** | ✅ — `result.Success && result.FilePath is not null → $"{result.Message} ({result.FilePath})"`; test `ExportCommand_Success_ShowsResultMessageWithPath_…` asserts both the message and the path appear |
| **Placeholder exports unchanged** | ✅ — Pdf/Excel/Print return `Success = false` + a message from `ReportExportService` → the ternary's `: result.Message` branch shows it verbatim; test `ExportCommand_NotYetImplementedFormat_ShowsHonestMessage_NoActionError` asserts the honest message + `HasActionError == false` |
| **`IsExporting` always resets** | ✅ — the `catch` is **inside** the `try`/`finally`, so `finally { IsExporting = false; }` runs on success, on the not-implemented path, and on an exception. Tests `…Failure_DoesNotThrow_…ResetsIsExporting`, `…WithoutLoggerFactory_…`, and `…NotYetImplemented_…` all assert `IsExporting == false`. |
| **Failure surfaces inline** | ✅ — `catch (Exception)` → non-destructive `ActionErrorMessage = Strings.Common_ActionFailedMessage` + `HasActionError = true`; `App.DispatcherUnhandledException` no longer fires for an unexpected export failure. `StatusMessage` is left at `string.Empty` (from the top-of-method reset) — the guard does **not** write the error into `StatusMessage`. |

---

## D. LOGGER ARCHITECTURE REVIEW

| Check | Result |
|---|---|
| **`sealed partial` conversion correct** | ✅ — `public sealed class ExportDialogViewModel : ViewModelBase` → `public sealed partial class …`; required and sufficient for the source generator to emit `LogOperationFailed`'s body. Build: 0 warnings, 0 errors. |
| **`ILoggerFactory` pattern correct** | ✅ — `ILoggerFactory? loggerFactory = null` ctor param (optional, appended last) → resolved once in the ctor: `_logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ?? NullLogger<ExportDialogViewModel>.Instance`. This is the Phase 8.56 (`SpecialistPageViewModel`) inline-resolution idiom. |
| **`NullLogger` fallback exists** | ✅ — `?? NullLogger<ExportDialogViewModel>.Instance` when no factory is supplied. Test `ExportCommand_Failure_WithoutLoggerFactory_UsesNullLogger_NeverThrows` exercises it. |
| **`[LoggerMessage]` source generation valid** | ✅ — `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Export dialog operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` — a **single** `ILogger` field + instance-form attribute → **no `SYSLIB1020`**. `LogOperationFailed(nameof(ExportAsync))` at the one call site. |
| **No `Exception` parameter** | ✅ — the method signature is `(string operation)` only |
| **No `Exception.Message`** | ✅ — `catch (Exception)` binds no variable; nothing reads `.Message` |
| **No file-path logging** | ✅ — the log message template is `"… Operation={Operation}"`; `{Operation}` = `nameof(ExportAsync)` = the literal string `"ExportAsync"`. Test-enforced (`…NoPathOrReportContentLeak`). |

---

## E. SECURITY REVIEW

| Vector | Finding |
|---|---|
| **File-path leakage** → UI | **prevented** on failure — a `UnauthorizedAccessException` whose `.Message` embeds `Access to the path 'C:\…\Revenue_Report.csv' is denied` is never surfaced (`ActionErrorMessage` is the fixed constant `Strings.Common_ActionFailedMessage`). On **success** the path is still shown in `StatusMessage` — **unchanged, by design** (the user asked to export and wants to know where the file went). |
| **File-path leakage** → log | **prevented** — `LogOperationFailed(string operation)` logs `Operation=ExportAsync` only. **Net improvement:** before this change the exception (path included) reached `App.LogUnhandledException`, which logs the full `Exception`. Now the exception is caught locally and logged operation-name-only. |
| **Export data / customer data / employee metrics** | **not exposed** — `catch (Exception)` binds no variable; the guard reads no `_result` field; the CSV payload (`result.Columns` / `result.Rows`) is never touched by `ActionErrorMessage` or the logger |
| **Backend exception bodies** | n/a — Reporting is fake-backed; `ReportExportService` is a local service |

**Seeded-sentinel test verified:** `ExportCommand_Failure_LogsOperationNameOnly_NoPathOrReportContentLeak` seeds `new UnauthorizedAccessException($"Access to the path '{secretPath}' is denied. customer=Amelia Hart total=1,850,000")` and asserts:
- `Assert.Single(loggerFactory.Entries)` — exactly one entry
- `entry.Level == LogLevel.Error`; `entry.Category` contains `ExportDialogViewModel`; `entry.Message` contains `Operation=ExportAsync`
- `Assert.DoesNotContain(secretPath, entry.Message)` **and** `Assert.DoesNotContain("Amelia Hart", entry.Message)`
- `Assert.DoesNotContain(secretPath, sut.ActionErrorMessage)` **and** `Assert.DoesNotContain("Amelia Hart", sut.ActionErrorMessage)`

---

## F. TEST REVIEW

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all 6 projects Passed
```

| Project | Passed | Failed | Skipped | Δ vs `5640123` |
|---|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 | — |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 | — |
| Rojan.Desktop.Presentation.Tests | **735** | 0 | 0 | **+6** |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 | — |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 | — |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 | — |
| **TOTAL** | **2,678** | **0** | **0** | **+6** |

| Expected (Phase 8.87) | Actual | Status |
|---|---|---|
| Tests 2,678 / 2,678 PASS | 2,678 / 2,678 | ✅ |
| Build 0 / 0 | 0 / 0 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

**+6 tests reviewed:**

| Aspect | Coverage |
|---|---|
| **Failure handling** | `ExportCommand_Failure_DoesNotThrow_SetsActionErrorAndResetsIsExporting` — `Record.Exception(...)` null; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`; export attempted (`LastFormat == Csv`) |
| **`IsExporting` reset** | asserted `== false` in `…Failure_DoesNotThrow_…`, `…WithoutLoggerFactory_…`, `…NotYetImplemented_…`, `…Success_…` |
| **Success path** | `ExportCommand_Success_ShowsResultMessageWithPath_…` (message + path in `StatusMessage`); `ExportCommand_NotYetImplementedFormat_ShowsHonestMessage_…` (placeholder message preserved, no `ActionError`); `ExportCommand_SuccessAfterFailure_ClearsActionError` |
| **No leakage** | `ExportCommand_Failure_LogsOperationNameOnly_NoPathOrReportContentLeak` (§E) |
| **Operation-only logging** | same test — `Operation=ExportAsync`, single entry, `Error` level |
| **NullLogger safety** | `ExportCommand_Failure_WithoutLoggerFactory_UsesNullLogger_NeverThrows` |
| **Regression** | 22 pre-existing Reporting tests + the 22 `ReportingPageViewModelTests` pass unchanged (the new optional 8th ctor param is transparent) |

---

## G. COMMIT READINESS

✅ **Ready.** No blockers.

**Staging plan (Phase 8.88 — explicit paths only, no `git add .` / `-A`):**

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/Reporting/ExportDialogViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Reporting/StubReportingServices.cs
git add tests/Rojan.Desktop.Presentation.Tests/Reporting/ExportDialogViewModelTests.cs
git diff --cached --name-only        # expect exactly 4 (3 M + 1 A)
```

**Commit message (EXACT):**

```
fix(desktop): guard report export failures

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

**Post-commit validation to run:** `dotnet build -c Debug` (expect 0/0) · full `dotnet test` (expect 2,678/2,678) · architecture (expect 7/7) · `git log --oneline -3`.

**Checkpoint update (Phase 8.88):** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` — new HEAD; §A banner + audit-phase list; §B commit table + Phase 8.86 detail bullet; §E build/test 2,672 → 2,678 (Presentation 729 → 735); §G Missing-Guard Sweep track — `ExportDialogViewModel` micro-phase ✅ / **Reporting domain fully closed** / Wave E (AI Center) NEXT; §H items 1/2/5/6.

---

## STOP

Phase 8.87 commit scope review complete. **3 modified + 1 new**, all under `…/Reporting/`. The `ExportAsync` guard preserves the CSV-success and "not yet implemented" placeholder behaviour and the `finally { IsExporting = false; }` guarantee (the `catch` sits between the existing `try` and `finally`); an unexpected export failure now surfaces via a new non-destructive `ActionErrorMessage` / `HasActionError` pair. `ExportDialogViewModel` is `sealed partial` with an injected `ILoggerFactory?` resolved once to a single `ILogger` field (`SYSLIB1020`-safe); the `[LoggerMessage]` is operation-name-only. The `ReportingPageViewModel` change is 4 lines of `ILoggerFactory` forwarding — no business logic, no DI break, ctor-compatible. No `Exception.Message` / file path / export data / customer data exposure — UI gets only `Common_ActionFailedMessage`, logging only `Operation=ExportAsync`; the failure log is now *narrower* than today's global full-exception log. `ReportExportService` / `AnalyticsPageViewModel` / `DashboardPageViewModel` / backend contracts / DI / RBAC / Strings untouched. Build 0/0, **2,678/2,678** tests, architecture 7/7.
**Next: Phase 8.88 — Export Dialog micro-phase Commit Execution.** Awaiting authorization.
