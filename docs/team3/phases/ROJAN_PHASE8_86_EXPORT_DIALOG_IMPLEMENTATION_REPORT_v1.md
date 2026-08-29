# ROJAN AI — TEAM 3 — PHASE 8.86 — MISSING-GUARD SWEEP — EXPORT DIALOG MICRO-PHASE — IMPLEMENTATION REPORT v1

**Type:** Implementation. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `5640123`
**Reference:** `ROJAN_PHASE8_85_EXPORT_DIALOG_SCOPE_AUDIT_v1.md` (Option B — `ILoggerFactory` plumbing)
**Result:** Build **0 / 0** · Full suite **2,678 / 2,678 PASS** · Architecture **7 / 7 PASS**

---

## A. FILES CHANGED

`git diff --stat` — **3 modified + 1 new = 4 files, 60 insertions(+), 5 deletions(-)** (excluding the new test file's ~135 lines).

| Group | File | Change |
|---|---|---|
| **Production (2)** | `src/…/ViewModels/Reporting/ExportDialogViewModel.cs` | `sealed class` → `sealed partial class`; `+ using Microsoft.Extensions.Logging` / `…Abstractions`; `+ ILogger<ExportDialogViewModel> _logger` field; ctor `+ ILoggerFactory? loggerFactory = null` (appended last) → `_logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ?? NullLogger<…>.Instance`; `+ _actionErrorMessage` / `_hasActionError` fields + `ActionErrorMessage` / `HasActionError` properties; `+ [LoggerMessage] LogOperationFailed(string operation)`; `ExportAsync` gains a `catch (Exception)` between the existing `try` and `finally` |
| | `src/…/ViewModels/Reporting/ReportingPageViewModel.cs` | **minimal parent forwarding (approved carve-out):** `+ ILoggerFactory? _loggerFactory` field; ctor `+ ILoggerFactory? loggerFactory = null` (appended after `logger`) + `_loggerFactory = loggerFactory;`; `OpenExportDialog()` passes `_loggerFactory` at the `new ExportDialogViewModel(...)` site. **4 lines. Nothing else.** |
| **Test stub (1)** | `tests/…/Reporting/StubReportingServices.cs` | `StubReportExportService` `+ Exception? ExportException` (when set, `ExportAsync` returns `Task.FromException` — format still recorded) + `+ ExportResultDto? Result` (lets a test drive the honest CSV-success / "not yet implemented" message paths); default path byte-identical |
| **Test (1, NEW)** | `tests/…/Reporting/ExportDialogViewModelTests.cs` | **new file — the VM had no dedicated test file**; **6 tests** |

**Not touched:** `ReportingPageViewModel.LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` / `ToggleSavedAsync` / `DeleteSnapshotAsync` / `ReloadSnapshotsAsync` / `[LoggerMessage]` signature; `DashboardPageViewModel`; `AnalyticsPageViewModel`; `IReportExportService` / `ReportExportService` / `ExportResultDto` / `ExportFormat` (the concrete export service — PDF/Excel/Print placeholders, CSV write logic — is byte-unchanged); backend contracts; **DI registrations** (all params optional; `ILoggerFactory` already provided by `AddLogging()`); RBAC; authentication; navigation; `Strings.cs` / all `.resx` (`Common_ActionFailedMessage` already ships from Wave A `794648e`); `AsyncRelayCommand`; `App.xaml.cs`; `ReportingPageViewModelTests.cs` (its 22 tests use the 7-arg `CreateSut` and are unaffected by the new 8th optional ctor param).

---

## B. LOGGER ARCHITECTURE

Per the audit's **Option B** and the Phase 8.86 ARCHITECTURE PATTERN section:

- `ExportDialogViewModel` becomes `sealed partial class` and takes **`ILoggerFactory? loggerFactory = null`** (optional, appended last). In the constructor it resolves **once**:
  ```csharp
  _logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ?? NullLogger<ExportDialogViewModel>.Instance;
  ```
  This is the Phase 8.56 (`SpecialistPageViewModel`) inline-resolution idiom — a **single** `ILogger<ExportDialogViewModel>` field for the instance-form `[LoggerMessage]`.
- Source-generated logging: `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Export dialog operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` — **no `Exception` parameter**, operation name only.
- **Parent forwarding (the phase's "Allowed: minimal parent logger forwarding if required"):** `ExportDialogViewModel` is `new`'d directly by `ReportingPageViewModel.OpenExportDialog()` (never DI-resolved), so the only way a real factory reaches it in production is for `ReportingPageViewModel` to hold and pass one. `ReportingPageViewModel` gained an **`ILoggerFactory? loggerFactory = null`** ctor param + `_loggerFactory` field and passes it at the `new` site. This is the Phase 8.43 profile-panel plumbing pattern exactly.

| `SYSLIB1020` check | Result |
|---|---|
| `ExportDialogViewModel` | **safe** — one `ILogger` field + instance-form `[LoggerMessage]` (single field). Build: 0 warnings. |
| `ReportingPageViewModel` | **safe** — it already had `ILogger<ReportingPageViewModel> _logger` + instance-form `[LoggerMessage]`; it gained an **`ILoggerFactory`** field (NOT a 2nd `ILogger`), and `ILoggerFactory` is not `ILogger`, so the source generator is unaffected (same reasoning as the 8.43 page parents). Build: 0 warnings. |

| DI impact | Result |
|---|---|
| DI registration change | **none** — every new param is optional (`= null`); `ILoggerFactory` and open-generic `ILogger<T>` are already provided by the host's `AddLogging()`; `ReportingPageViewModel` stays `AddTransient` and resolves the factory automatically |

---

## C. EXPORT GUARD

```csharp
private async Task ExportAsync()
{
    IsExporting = true;
    StatusMessage = string.Empty;
    try
    {
        var result = await _exportService.ExportAsync(_result, SelectedFormat).ConfigureAwait(true);   // UNCHANGED
        StatusMessage = result.Success && result.FilePath is not null                                   // UNCHANGED
            ? $"{result.Message} ({result.FilePath})"
            : result.Message;
        ActionErrorMessage = null; HasActionError = false;
    }
#pragma warning disable CA1031 // Command boundary: an unexpected export failure (file-system / permission / unknown-format) must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
    catch (Exception)
#pragma warning restore CA1031
    {
        ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage;
        HasActionError = true;
        LogOperationFailed(nameof(ExportAsync));
    }
    finally
    {
        IsExporting = false;        // ← UNCHANGED — still runs on every path (success / not-implemented / exception)
    }
}
```

| Requirement | Status |
|---|---|
| `try` wraps the existing export logic | ✅ — the `await _exportService.ExportAsync(...)` call and the `StatusMessage = result.Success … ? … : result.Message` line are byte-unchanged inside the `try` |
| `catch (Exception)` → fixed localized `ActionErrorMessage` + `HasActionError = true` | ✅ — `ActionErrorMessage = Localization.Strings.Common_ActionFailedMessage` (a compile-time constant); no exception variable bound |
| Log `nameof(ExportAsync)` | ✅ — `LogOperationFailed(nameof(ExportAsync))`, operation name only |
| `finally { IsExporting = false; }` **guaranteed** | ✅ — the `finally` is unchanged; it runs after `try`, after `catch`, on every path. Test-enforced (`…Failure_DoesNotThrow_SetsActionErrorAndResetsIsExporting` asserts `IsExporting == false`). |
| Behaviour preservation — CSV success / `StatusMessage` success message / "not yet implemented" placeholders / dialog behaviour | ✅ — the success branch (`StatusMessage = "{Message} ({FilePath})"`) and the handled-failure branch (`StatusMessage = result.Message` for Pdf/Excel/Print) are unchanged; `ExportCommand` / `CloseCommand` / `IsExporting` gate / `SelectedFormat` / `AvailableFormats` untouched. `IReportExportService` / `ReportExportService` byte-unchanged. |

`ActionErrorMessage` is a **new, non-destructive** bindable pair, deliberately distinct from `StatusMessage`: on a handled path `StatusMessage` still carries the honest export result; on an unexpected failure the generic error goes to `ActionErrorMessage` (and `StatusMessage` ends at `string.Empty`, from the existing top-of-method reset — the guard does not write the error into `StatusMessage`).

---

## D. SECURITY

Exports carry **the full report payload** (revenue figures, customer names/ids, employee metrics) and the **target file path**.

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — `catch (Exception)` binds **no variable**; `ActionErrorMessage` is only ever `null` or the constant `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has **no `Exception` parameter`; `LocalFileLoggerProvider` renders nothing but the operation name |
| **File-path leakage** | **prevented** on failure — a `UnauthorizedAccessException` whose `.Message` embeds `Access to the path 'C:\…\Revenue_Report.csv' is denied` is never surfaced (constant string) or logged (operation name only). On **success** the path is still shown in `StatusMessage` — unchanged, by design (the user asked to export). |
| Backend response leakage | n/a — Reporting is fake-backed; `ReportExportService` is local |
| **Report-content leakage** (revenue / customer rows) | **prevented** — no exception variable; the guard reads no `_result` field |

**Net improvement:** before this change, an unexpected export failure reached `App.LogUnhandledException`, which logs the **full exception** (path included) — the one intentional exception-logging site. Now the exception is caught locally and logged **operation-name-only**, so the path no longer reaches the log.

**Test-enforced:** `ExportCommand_Failure_LogsOperationNameOnly_NoPathOrReportContentLeak` seeds `new UnauthorizedAccessException($"Access to the path '{secretPath}' is denied. customer=Amelia Hart total=1,850,000")` and asserts the single `RecordingLoggerFactory` entry has `Operation=ExportAsync`, `Category` contains `ExportDialogViewModel`, and `DoesNotContain(secretPath)` **and** `DoesNotContain("Amelia Hart")` — in both `entry.Message` and `ActionErrorMessage`.

---

## E. TESTS

**+6 tests** (2,672 → 2,678), all in the **new** `ExportDialogViewModelTests.cs`. Reuses `RecordingLoggerFactory` (Phase 8.43) + `StubDialogService`; `StubReportExportService` gained additive `Exception? ExportException` / `ExportResultDto? Result` seams (default path byte-identical — the 22 pre-existing Reporting tests pass unchanged).

| Test | Asserts |
|---|---|
| `ExportCommand_Success_ShowsResultMessageWithPath_AndTogglesIsExporting` | CSV success: `StatusMessage` contains the message **and** the file path; `IsExporting == false`; `HasActionError == false` |
| `ExportCommand_NotYetImplementedFormat_ShowsHonestMessage_NoActionError` | Pdf → `StatusMessage == "PDF export is not yet implemented …"`; `HasActionError == false`; `IsExporting == false` (placeholder behaviour preserved) |
| `ExportCommand_Failure_DoesNotThrow_SetsActionErrorAndResetsIsExporting` | `Record.Exception(...)` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`; **`IsExporting == false`** (finally ran); `StatusMessage == string.Empty` (guard does not write into it); export was attempted (`LastFormat == Csv`) |
| `ExportCommand_Failure_LogsOperationNameOnly_NoPathOrReportContentLeak` | single `Error` entry, `Operation=ExportAsync`, `Category` ~ `ExportDialogViewModel`; `DoesNotContain(secretPath)` + `DoesNotContain("Amelia Hart")` in `entry.Message` **and** `ActionErrorMessage` |
| `ExportCommand_Failure_WithoutLoggerFactory_UsesNullLogger_NeverThrows` | no factory passed → no throw; `HasActionError == true`; `IsExporting == false` |
| `ExportCommand_SuccessAfterFailure_ClearsActionError` | fail → `HasActionError` true → clear seam → succeed → `HasActionError == false`, `ActionErrorMessage == null`, `StatusMessage` shows the success message |

`dotnet test --filter FullyQualifiedName~Reporting` → **28 passed** (22 Reporting + 6 ExportDialog).

---

## F. VALIDATION

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020 / CA1031 / CA1848)
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

| Expected (Phase 8.86) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,672 → ~2,677 | 2,678 / 2,678 | ✅ (+6) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## G. COMMIT READINESS

**Not committed** (per Phase 8.86 STRICT SCOPE). Ready for Phase 8.87 commit scope review.

- **4 tracked changes (3 modified + 1 new):**
  ```
  git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
   M src/Rojan.Desktop.Presentation/ViewModels/Reporting/ExportDialogViewModel.cs
   M src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs
   M tests/Rojan.Desktop.Presentation.Tests/Reporting/StubReportingServices.cs
  ?? tests/Rojan.Desktop.Presentation.Tests/Reporting/ExportDialogViewModelTests.cs
  ```
- The `ReportingPageViewModel.cs` change is the **explicitly-allowed** "minimal parent logger forwarding" (4 lines: field + ctor param + assignment + call-site arg) — no other method or the `[LoggerMessage]` signature is touched.
- No `Strings.cs` / `.resx` change. No DI change. No `AnalyticsPageViewModel` / `DashboardPageViewModel` / `RunReportAsync` / `CancellationToken` / backend-contract / RBAC / auth / navigation change.
- Recommended commit (single): `fix(desktop): guard report export failures`.
- Untracked `ROJAN_*.md` reports remain unstaged.

---

## STOP

Phase 8.86 implementation complete. `ExportDialogViewModel.ExportAsync` now catches unexpected export failures (file-system / permission / unknown-format) into a new non-destructive `ActionErrorMessage` / `HasActionError` pair + a source-generated operation-name-only `[LoggerMessage]`, with the `finally { IsExporting = false; }` guarantee preserved and the CSV-success / "not yet implemented" / dialog behaviour byte-unchanged. `ExportDialogViewModel` is now `sealed partial` with an injected `ILoggerFactory?` (resolved once to a single `ILogger` field — `SYSLIB1020`-safe); `ReportingPageViewModel` forwards its own `ILoggerFactory?` to it (4-line minimal parent change, the allowed carve-out). No DI change; the concrete `ReportExportService` untouched. Build 0/0, **2,678/2,678** tests, architecture 7/7.
**Next: Phase 8.87 — Export Dialog micro-phase Commit Scope Review.** Awaiting authorization.
