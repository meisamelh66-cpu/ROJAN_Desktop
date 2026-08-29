# ROJAN AI — TEAM 3 — PHASE 8.85 — MISSING-GUARD SWEEP — EXPORT DIALOG MICRO-PHASE — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `5640123b95d622fbc3f11a28045e267c24f16975`
**Objective:** Audit `ExportDialogViewModel.ExportAsync`'s failure boundary and decide whether it should be completed before Wave E (AI Center).

---

## A. GIT STATE

```
git rev-parse HEAD        → 5640123b95d622fbc3f11a28045e267c24f16975
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `5640123` (Reporting mini-wave commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `5640123` guard reporting · `525fd4b` guard organization · `66c8490` guard inventory and invoice-cancel |

Baseline test suite (checkpoint §E, `5640123`): **2,672 / 2,672** — Domain 456, Application 791, Presentation 729, Infrastructure 609, Shell 80, Architecture 7.

---

## B. EXPORT INVENTORY — `src/Rojan.Desktop.Presentation/ViewModels/Reporting/ExportDialogViewModel.cs`

| Aspect | Finding |
|---|---|
| **Class type** | `public sealed class ExportDialogViewModel : ViewModelBase` — **NOT `partial`**; **no `ILogger` field**; **no `[LoggerMessage]`**. 80 lines. |
| **Logger availability** | **none.** The class has no logging of any kind. Its only construction site — `ReportingPageViewModel.OpenExportDialog()` — does `_dialogService.ShowDialog(new ExportDialogViewModel(CurrentResult, _exportService, _dialogService))` and passes no logger. |
| **Constructor dependencies** | `(ReportResultDto result, IReportExportService exportService, IDialogService dialogService)` — 3 required, all non-null. No optional params. |
| **`ExportAsync` flow** | `IsExporting = true; StatusMessage = string.Empty;` → `try { var result = await _exportService.ExportAsync(_result, SelectedFormat).ConfigureAwait(true); StatusMessage = result.Success && result.FilePath is not null ? $"{result.Message} ({result.FilePath})" : result.Message; } finally { IsExporting = false; }` |
| **Existing exception handling** | **`try`/`finally` with NO `catch`.** The `finally` only resets `IsExporting = false`; any exception then propagates out unchanged. |
| **User-facing error surfaces** | `StatusMessage` (string) — carries the export result message and, on a successful CSV export, the **file path**: `$"{result.Message} ({result.FilePath})"`. `IsExporting` (bool) drives a busy indicator. There is no `State`/`ErrorMessage` and no `ActionErrorMessage`. |
| **Commands** | `ExportCommand` (`AsyncRelayCommand`, `CanExecute` = `!IsExporting`) → `ExportAsync`; `CloseCommand` (`RelayCommand`, sync — `_dialogService.CloseDialog()`). |
| **Dedicated test file** | **none exists** — `ExportDialogViewModel` has no `*Tests.cs`. Its 2 current behaviours (`OpenExportDialogCommand_AfterRunningReport_ShowsExportDialog`, `…CanExecute_FalseWithNoResult`) are asserted from `ReportingPageViewModelTests`. |

### B.1 What `IReportExportService.ExportAsync` actually does (concrete `ReportExportService`)

```csharp
public Task<ExportResultDto> ExportAsync(ReportResultDto result, ExportFormat format, CancellationToken ct = default) => format switch
{
    ExportFormat.Csv   => Task.FromResult(ExportCsv(result)),          // ← ExportCsv runs SYNCHRONOUSLY inside Task.FromResult
    ExportFormat.Pdf   => Task.FromResult(NotYetImplemented("PDF")),   // returns Success=false — does NOT throw
    ExportFormat.Excel => Task.FromResult(NotYetImplemented("Excel")), // returns Success=false
    ExportFormat.Print => Task.FromResult(NotYetImplemented("Print")), // returns Success=false
    _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unknown export format."),   // ← throws
};

private static ExportResultDto ExportCsv(ReportResultDto result)
{
    // … builds CSV text from result.Columns + result.Rows (the full report payload) …
    var directory = Path.Combine(Path.GetTempPath(), "RojanDesktopExports");
    Directory.CreateDirectory(directory);                              // ← can throw IOException / UnauthorizedAccessException
    var filePath = Path.Combine(directory, fileName);
    File.WriteAllText(filePath, builder.ToString(), Encoding.UTF8);    // ← can throw IOException / UnauthorizedAccessException / PathTooLongException / DirectoryNotFoundException
    return new ExportResultDto(true, $"Exported {result.Rows.Count} rows to CSV.", filePath);
}
```

- `ExportFormat` is **synchronous** — `ExportCsv` executes eagerly inside `Task.FromResult(...)`, so a file-IO failure throws **synchronously** out of `ExportAsync` (not as a faulted `Task`). It surfaces at the `await _exportService.ExportAsync(...)` call in `ExportDialogViewModel.ExportAsync`.
- **Expected** failures (Pdf/Excel/Print "not yet implemented") are already returned as `ExportResultDto(Success: false, Message, FilePath: null)` — **not thrown**. `ExportDialogViewModel` surfaces `result.Message` correctly for these.

---

## C. FAILURE BOUNDARY REVIEW — `ExportAsync`

| Check | Finding |
|---|---|
| **`try`/`finally` behavior** | `finally` resets `IsExporting = false` on every path (success, exception, cancellation). Correct as far as it goes. |
| **Missing `catch`** | **Confirmed.** There is no `catch`. Any exception from `await _exportService.ExportAsync(...)` propagates past the `finally` into the `AsyncRelayCommand`'s `async void` `Execute`. |
| **Dispatcher / global-handler behavior** | The unhandled exception becomes an **unobserved `async void` task exception**. On the UI thread this reaches `App.DispatcherUnhandledException`, which **logs it (via `App.LogUnhandledException`) and shows the generic modal recovery dialog**, then recovers. It does **not** crash the app. So this is **P1 (UX consistency), not P0.** |
| **File-generation failure** | `Directory.CreateDirectory` / `File.WriteAllText` throwing (temp dir read-only, disk full, AV lock on the target path, path too long) → uncaught → modal system dialog instead of an inline "export failed" message in the dialog the user is looking at. Plausible in the field. |
| **Permission failure** | `UnauthorizedAccessException` from `File.WriteAllText` — same path. Its `.Message` typically **embeds the full target file path** (`Access to the path 'C:\…\Revenue_Report_20260828_120000.csv' is denied`). Currently that reaches `App.LogUnhandledException` (which logs the full `Exception` by design — the one intentional exception-logging site) but **not** the UI. |
| **Export-service failure** | An unexpected `IReportExportService` implementation exception (e.g. the `_ => throw new ArgumentOutOfRangeException(...)` for an unknown `ExportFormat`, or a future real Pdf/Excel implementation that throws) → uncaught → modal dialog. |
| **Cancellation** | `ExportCommand` passes no `CancellationToken`; `ExportAsync(_result, SelectedFormat)` uses the interface's `default` token. No `OperationCanceledException` path. (CSV export is a fast in-memory + single-file-write; not a cancellable long-run. Out of scope.) |

**Net:** every *unexpected* export failure (as opposed to the already-handled "not implemented" stubs) currently escapes to the global handler. This is the last unguarded user-triggered action in the Reporting domain after the Phase 8.82 mini-wave.

---

## D. ARCHITECTURE REVIEW

| Fact | Value |
|---|---|
| **`ILogger` availability in `ExportDialogViewModel`** | **none** — no field, no ctor param, no `[LoggerMessage]`. There is **no existing logger to reuse.** |
| **`ILogger` in the parent `ReportingPageViewModel`** | a single `ILogger<ReportingPageViewModel> _logger` field + an instance-form `[LoggerMessage] LogOperationFailed(string operation)`. It is `sealed partial`. It has **no `ILoggerFactory`**. |
| **DI impact of any option** | **zero DI registration change** for A/B — `ReportingPageViewModel` is `AddTransient`; open-generic `ILogger<T>` and `ILoggerFactory` are already provided by `AddLogging()`; every new param is optional (`= null`). |
| **`SYSLIB1020` risk** | **Option B: none.** `ExportDialogViewModel` would get **one** `ILogger<ExportDialogViewModel>` field + an instance-form `[LoggerMessage]` → single-field → safe. `ReportingPageViewModel` would get an **`ILoggerFactory`** field (NOT a 2nd `ILogger`) → `ILoggerFactory` is not `ILogger`, the source generator is unaffected → safe. This is exactly the Phase 8.43 profile-panel pattern (`Customer`/`Service`/`InventoryPageViewModel` each already carry `ILogger<TSelf>` + instance `[LoggerMessage]` and gained an `ILoggerFactory` for their child panels with no `SYSLIB1020`). |

### Option A — local guard + "existing LoggerMessage"

**Not literally possible as stated** — `ExportDialogViewModel` has no `[LoggerMessage]` to reuse. The viable variant is **Option A-lite: guard with a generic `StatusMessage`, NO logging:**

```csharp
catch (Exception)
{
    StatusMessage = Localization.Strings.Common_ActionFailedMessage;
}
```

- **Files:** `ExportDialogViewModel.cs` only (+ `StubReportExportService` `Exception?` seam + a new `ExportDialogViewModelTests.cs`). **No parent change, no `sealed partial`.**
- **Trade-off:** breaks the sweep's "every guarded action logs its operation name once" discipline. `App.LogUnhandledException` currently *does* log these (with the full exception) — Option A-lite would **stop** that logging entirely (the exception is now swallowed), replacing a rich-but-global log with **no** log. That is a diagnostic regression for a file-IO failure class.

### Option B — `ILoggerFactory` plumbing (Phase 8.43 pattern)

- `ExportDialogViewModel`: `sealed class` → `sealed partial class`; `+ ILogger<ExportDialogViewModel>? logger = null` ctor param (appended after `dialogService`) + `_logger` field with `?? NullLogger<ExportDialogViewModel>.Instance`; `+ [LoggerMessage(EventId = 1, Level = Error, "Export dialog operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);`; `catch (Exception) { StatusMessage = Localization.Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(ExportAsync)); }` inside `ExportAsync`, before the `finally`.
- `ReportingPageViewModel`: `+ ILoggerFactory? loggerFactory = null` ctor param (appended after `logger`) + `_loggerFactory` field; `OpenExportDialog()` → `new ExportDialogViewModel(CurrentResult, _exportService, _dialogService, _loggerFactory?.CreateLogger<ExportDialogViewModel>())`.
- **Files:** 2 prod (`ExportDialogViewModel.cs`, `ReportingPageViewModel.cs`) + `StubReportingServices.cs` (`StubReportExportService` `+ Exception? ExportException`) + **new** `ExportDialogViewModelTests.cs` + `ReportingPageViewModelTests.cs` (1 parent-forwarding test). **5 files.**
- **Consistency:** matches Waves A–D + the Reporting mini-wave (`ActionErrorMessage`/`StatusMessage` inline + operation-name-only `[LoggerMessage]`) and the 8.43 `ILoggerFactory` parent-plumbing precedent exactly.

### Option C — defer to Wave G

Fold `ExportDialogViewModel` into a later "dialog / infra VMs" pass (Wave G, alongside `PosCheckoutViewModel` etc.). Costs a context re-load; and Wave G is currently scoped "P2, low priority, ~28 methods" — a P1 file-generation gap would be under-prioritized there.

---

## E. SECURITY REVIEW

Exports carry **the full report payload** — revenue figures, customer names/ids, employee performance metrics — plus, on success, the **local file path**.

| Vector | Current | Under Option B (or A-lite) |
|---|---|---|
| `Exception.Message` → UI (`StatusMessage`) | not reached (exception escapes before `StatusMessage` is set) — but a naive future guard using `exception.Message` **would** leak a `UnauthorizedAccessException` path or an `IOException` detail | **prevented** — `catch (Exception)` with **no exception variable**; `StatusMessage` set to the fixed constant `Strings.Common_ActionFailedMessage` |
| File-path leakage → UI | **on success** `StatusMessage` shows `({result.FilePath})` — a `%TEMP%\RojanDesktopExports\…` path (by design; the user asked to export and wants to know where). On **failure** the path currently reaches only `App.LogUnhandledException`. | on **failure**: **not shown** (generic constant), **not logged** (Option B logs `Operation=ExportAsync` only; Option A-lite logs nothing). Success behaviour unchanged. |
| File-path leakage → log | `App.LogUnhandledException` currently logs the full exception (path included) — the one intentional exception-logging site | Option B: the exception is caught locally and `LogOperationFailed(nameof(ExportAsync))` logs **operation name only** → the path no longer reaches the log. A net **improvement**. Option A-lite: no log at all. |
| Backend payload leakage | Reporting is fake-backed; `ReportExportService` is local — no backend body involved | n/a |
| Report-content leakage (revenue / customer rows) | `ExportCsv` builds the CSV from `result.Rows` in memory; an IO exception message would not normally contain row data, but a no-variable catch guarantees it | **prevented** — no exception variable, generic `StatusMessage` |

**Conclusion:** any guard here **must** use a no-exception-variable `catch` and a fixed localized `StatusMessage`. Under Option B, logging is operation-name-only (`LogOperationFailed(string operation)` has no `Exception` parameter) — strictly better than today's global full-exception log for this path.

---

## F. CLASSIFICATION

| Question | Answer |
|---|---|
| **P0 / P1 / P2** | **P1.** `App.DispatcherUnhandledException` recovers every occurrence on the UI thread (logs + modal + continue) — no crash, no data loss. But it is a real UX-consistency gap: a failed export throws a generic system modal instead of an inline "export failed — please try again" in the dialog the user is actively looking at, in the exact domain just guarded (Reporting mini-wave `5640123`). File-IO failures (disk full, read-only temp, AV lock, path length) are plausible in the field. |
| **Does this block Wave E?** | **No.** `ExportDialogViewModel` is entirely independent of `AiCenterPageViewModel`. Wave E can proceed without it. |
| **Is it the last Reporting-domain gap?** | **Yes** — after the Phase 8.82 mini-wave, `ExportDialogViewModel.ExportAsync` is the only remaining unguarded user-triggered action in the Reporting domain. |

---

## G. RECOMMENDATION

**Recommend: Option 1 — Phase 8.86 `ExportDialog` implementation, via Option B (`ILoggerFactory` plumbing).**

Rationale:
- It is **small, self-contained, and in-context now** — doing it immediately after the Reporting mini-wave avoids a later context re-load and closes the Reporting domain completely.
- **Option B over Option A-lite:** Option A-lite would *silently swallow* export exceptions that `App.LogUnhandledException` currently logs (with full detail) — a diagnostic regression. Option B keeps a breadcrumb (`Operation=ExportAsync`, operation-name-only, no path/content) while *also* fixing the leak risk and the UX. The `ILoggerFactory` parent-plumbing is a proven, zero-DI-change, `SYSLIB1020`-safe move (Phase 8.43 did it for 3 page parents).
- **Option C (defer to Wave G) is not recommended** — it de-prioritizes a P1 into a P2 bucket and pays the context-reload cost anyway.

### Phase 8.86 — proposed scope

**PHASE 8.86 — MISSING-GUARD SWEEP — EXPORT DIALOG MICRO-PHASE — IMPLEMENTATION v1**

| Item | Detail |
|---|---|
| **Production (2)** | `ExportDialogViewModel.cs`: `sealed class` → `sealed partial class`; `+ using Microsoft.Extensions.Logging` / `…Abstractions`; `+ ILogger<ExportDialogViewModel>? logger = null` ctor param (appended last) + `_logger` field (`?? NullLogger<…>.Instance`); `+ [LoggerMessage(EventId = 1, Level = Error, Message = "Export dialog operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);`; wrap `ExportAsync`'s body in `try { …existing… } catch (Exception) { StatusMessage = Localization.Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(ExportAsync)); } finally { IsExporting = false; }` (the existing `finally` stays; the `catch` is inserted before it). **`ReportingPageViewModel.cs`:** `+ ILoggerFactory? loggerFactory = null` ctor param (appended after `logger`) + `_loggerFactory` field; `OpenExportDialog()` passes `_loggerFactory?.CreateLogger<ExportDialogViewModel>()` at the `new ExportDialogViewModel(...)` site. **No other `ReportingPageViewModel` change** — `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` / `ToggleSavedAsync` / `DeleteSnapshotAsync` / `[LoggerMessage]` signature untouched. |
| **Test stub (1)** | `tests/…/Reporting/StubReportingServices.cs` — `StubReportExportService` `+ Exception? ExportException` (additive; when set, `ExportAsync` throws it; null path byte-identical). |
| **Tests (2)** | **new** `tests/…/Reporting/ExportDialogViewModelTests.cs` — ~4 tests (success → `StatusMessage` shows message + path, `IsExporting` toggles back; failure → `Record.Exception` is null, `StatusMessage == Strings.Common_ActionFailedMessage`, `IsExporting == false`, no file-path / report-content in `StatusMessage`; failure → `RecordingLogger` entry `Operation=ExportAsync` operation-name-only, `DoesNotContain` seeded path/row sentinel; NullLogger safety). `tests/…/Reporting/ReportingPageViewModelTests.cs` — 1 test: `LoggerFactory_ForwardedToExportDialogChild_ExportFailureIsLoggedViaTheFactory` (mirrors the 8.43 parent-forwarding tests, using `RecordingLoggerFactory`). |
| **Files affected** | **5** (2 prod + 1 stub + 2 test). No new file except `ExportDialogViewModelTests.cs`. No `Strings.cs` / `.resx` change (`Common_ActionFailedMessage` ships). No DI change. |
| **Estimated tests** | **+5** (≈ 2,672 → ≈ 2,677). |
| **Risk** | **LOW-MEDIUM.** `sealed`→`sealed partial` + one optional ctor param on each of 2 VMs (no DI, no `SYSLIB1020` — Phase 8.43 precedent); `catch` inserted around an already-`try`/`finally`'d body; fake-backed. The one judgement point: the `catch` sits inside the existing `try`, *before* the `finally`, so `IsExporting = false` still runs on every path. |
| **Validation expectation** | `dotnet build -c Debug` → **0 warnings / 0 errors** (no `SYSLIB1020` / `CA1031` / `CA1848`). Full suite → **≈ 2,677 / ≈ 2,677 PASS** (Presentation 729 → ≈ 734; other projects unchanged). Architecture tests → **7 / 7 PASS**. Standard rhythm: 8.86 implementation (STOP before commit) → 8.87 commit scope review → 8.88 commit execution → checkpoint update. |

**Downstream:** after 8.88 the Reporting domain is fully closed → **Wave E — AI Center** (`AiCenterPageViewModel` ×~12 — `ROJAN_PHASE8_64_*` §D) → Wave F (Automation tabs ×~7) → Wave G (P2 infra). Separately, the "sanitize load-error surfacing" P2 phase should prioritize `ReportingPageViewModel`'s three `= exception.Message` leaks.

---

## STOP

Phase 8.85 audit complete. HEAD `5640123`, tracked tree clean, baseline 2,672 / 2,672.
`ExportDialogViewModel.ExportAsync` is a `sealed`-class dialog VM with a `try`/`finally` and **no `catch`** — an unexpected export failure (`Directory.CreateDirectory` / `File.WriteAllText` IO/permission exception, or an unknown-format throw) escapes to `App.DispatcherUnhandledException` (recovered, not a crash → **P1, not P0; does not block Wave E**). It is the last unguarded user-triggered action in the Reporting domain. It has **no `ILogger`** and **no `[LoggerMessage]`**, so a guard needs either **A-lite** (guard + generic `StatusMessage`, no log — a diagnostic regression vs. today's global exception log) or **B** (`sealed partial` + optional `ILogger?` + `ReportingPageViewModel` `ILoggerFactory?` plumbing — Phase 8.43 pattern, zero DI change, `SYSLIB1020`-safe). Security: any guard must use a no-exception-variable `catch` + fixed localized `StatusMessage` to keep the file path / report rows out; Option B's operation-name-only log is a net improvement over today's full-exception global log for this path.
**Recommendation: Phase 8.86 — implement via Option B.** ~5 files, ~+5 tests, LOW-MEDIUM risk. Awaiting authorization.
