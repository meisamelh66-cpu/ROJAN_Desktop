# ROJAN AI — TEAM 3 — PHASE 8.70 — MISSING-GUARD SWEEP WAVE B (HR) — IMPLEMENTATION REPORT v1

**Type:** Implementation. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `794648e`
**Reference:** `ROJAN_PHASE8_69_WAVE_B_HR_SCOPE_REVIEW_v1.md`
**Result:** Build **0 / 0** · Full suite **2,641 / 2,641 PASS** · Architecture **7 / 7 PASS**

---

## A. FILES CHANGED

`git diff --stat` — **9 files, 682 insertions(+), 63 deletions(-)**. All under `…/HR/`. No new file.

| Group | File | Change |
|---|---|---|
| **Production (2)** | `src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; 10 command methods wrapped in `try`/`catch` |
| | `src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; 3 command methods wrapped in `try`/`catch` |
| **Test stubs (5)** | `tests/…/HR/StubEmployeeCommandService.cs` | `+ Exception?` seams: `CreateEmployeeException`, `ActivateEmployeeException`, `DeactivateEmployeeException`, `SuspendEmployeeException` |
| | `tests/…/HR/StubAttendanceCommandService.cs` | `+ Exception?` seams: `RecordAttendanceException`, `RequestLeaveException`, `ApproveLeaveException`, `RejectLeaveException` |
| | `tests/…/HR/StubShiftCommandService.cs` | `+ Exception?` seams: `CreateShiftException`, `AssignShiftException` |
| | `tests/…/HR/StubCommissionCommandService.cs` | `+ Exception?` seam: `CreateCommissionRuleException` (GenerateCommissions failure uses the existing ctor delegate) |
| | `tests/…/HR/StubPayrollCommandService.cs` | `+ Exception?` seam: `GeneratePayrollException` |
| **Test VMs (2)** | `tests/…/HR/HrPageViewModelTests.cs` | `+ using …Localization;`; **+14 tests** |
| | `tests/…/HR/EmployeeProfileViewModelTests.cs` | `+ using …Localization;`; **+5 tests** |

**Not touched:** `Strings.cs`, all `.resx` files (`Common_ActionFailedMessage` already exists from Wave A), every HR / Application / Domain service and interface, DI, `AsyncRelayCommand`, `App.xaml.cs`, navigation, RBAC, every `[LoggerMessage]` signature, `LoadAsync` / `SearchAsync`, all shared production infrastructure. No existing test body changed.

---

## B. GUARD IMPLEMENTATION DETAILS

### B.1 One additive property pair per ViewModel

Both VMs gained (private-set, additive, **no constructor / DI change**):

```csharp
public string? ActionErrorMessage { get; private set; }   // via SetProperty
public bool    HasActionError      { get; private set; }   // via SetProperty
```

`ActionErrorMessage` is **non-destructive** — it never touches `State` / `ErrorMessage`, so an HR write failure no longer blanks the page (`DashboardWidget` still shows `Loaded`/`Empty` content). Same shape as `Customers.CustomerProfileViewModel.SaveErrorMessage` / `HasSaveError` (Wave A).

### B.2 Per-method transformation (uniform across all 13)

```csharp
// unchanged: CanExecute predicate + early-return validation stay ABOVE the try
if (PayrollSelectedEmployee is null) { return; }
_ = decimal.TryParse(PayrollBonus, out var bonus);
_ = decimal.TryParse(PayrollDeduction, out var deduction);
var request = new GeneratePayrollRequest(PayrollSelectedEmployee.Id, PayrollMonth, PayrollYear, bonus, deduction);

try
{
    var created = await _payrollCommandService.GeneratePayrollSummaryAsync(request).ConfigureAwait(true);
    ActionErrorMessage = null;
    HasActionError = false;

    PayrollBonus = string.Empty;
    PayrollDeduction = string.Empty;
    PayrollSummaries.Insert(0, created);
}
#pragma warning disable CA1031 // Command boundary: a failed HR write must surface inline, not via the global dialog - same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
catch (Exception)
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(GeneratePayrollAsync));
}
```

- **`catch (Exception)` with no exception variable** in all 13 — `Exception.Message` / backend body / identifiers / PII are structurally unbindable.
- On **success**: `ActionErrorMessage = null; HasActionError = false;` clears any prior error before the rest of the success path runs (form clears, list mutation, reload, re-select — all preserved verbatim).
- On **failure**: fixed localized `Strings.Common_ActionFailedMessage` + `HasActionError = true` + exactly one `LogOperationFailed(nameof(<Method>))` call on the VM's **existing** instance-form `[LoggerMessage]`. No new logger, no `ILoggerFactory` change, no `SYSLIB1020`.
- Early-return validation (`if (x is null) return;`, `TimeSpan.TryParse` / `decimal.TryParse` guards) and `CanExecute` predicates are **outside** the `try`, byte-identical to before — a rejected parse or unselected row is not a failure and sets no error.

### B.3 `HrPageViewModel` — 10 methods guarded

| Method | `catch` → | Success-path preserved |
|---|---|---|
| `CreateEmployeeAsync` | `ActionError` + `LogOperationFailed(nameof(CreateEmployeeAsync))` | 4 `NewEmployee*` field clears, `await LoadAsync()`, re-select created row |
| `RecordAttendanceAsync` | `nameof(RecordAttendanceAsync)` | `AttendanceCheckInTime`/`AttendanceNotes` clears, `TodayAttendance` reload, `Summary` refresh |
| `CreateShiftAsync` | `nameof(CreateShiftAsync)` | 3 shift-field clears, `Shifts.Add(created)` |
| `AssignShiftAsync` | `nameof(AssignShiftAsync)` | `ShiftAssignments.Add(created)` |
| `RequestLeaveAsync` | `nameof(RequestLeaveAsync)` | `LeaveReason` clear, `LeaveRequests.Insert(0, created)` |
| `ApproveLeaveAsync` | `nameof(ApproveLeaveAsync)` | `ReplaceLeaveRequest(updated)` (in-place list swap) |
| `RejectLeaveAsync` | `nameof(RejectLeaveAsync)` | `ReplaceLeaveRequest(updated)` |
| `CreateCommissionRuleAsync` | `nameof(CreateCommissionRuleAsync)` | `NewRuleValue`/`NewRuleDescription` clears, `CommissionRules.Add(created)` |
| `GenerateCommissionsAsync` | `nameof(GenerateCommissionsAsync)` | transaction inserts + `StatusMessage` set — **only on success**; on failure `StatusMessage` is left untouched |
| `GeneratePayrollAsync` | `nameof(GeneratePayrollAsync)` | `PayrollBonus`/`PayrollDeduction` clears, `PayrollSummaries.Insert(0, created)` |

### B.4 `EmployeeProfileViewModel` — 3 methods guarded

| Method | `catch` → | Success-path preserved |
|---|---|---|
| `ActivateAsync` | `ActionError` + `LogOperationFailed(nameof(ActivateAsync))` | `await LoadAsync()` then `_onChanged?.Invoke()` |
| `DeactivateAsync` | `nameof(DeactivateAsync)` | `await LoadAsync()` then `_onChanged?.Invoke()` |
| `SuspendAsync` | `nameof(SuspendAsync)` | `await LoadAsync()` then `_onChanged?.Invoke()` |

`await LoadAsync()` is kept inside the guarded block (it is self-guarded — its own catch sets `State = Error` — so it never propagates into the command catch), consistent with the Wave A `CustomerProfileViewModel.SaveChangesAsync` precedent. `_onChanged?.Invoke()` therefore fires **only** when the command await succeeded — a failed lifecycle change no longer triggers a parent `HrPageViewModel` reload.

---

## C. HR BEHAVIOR PRESERVATION

| Concern | Status |
|---|---|
| **Payroll calculation flow** | untouched — `decimal.TryParse` of bonus/deduction, `GeneratePayrollRequest` construction, and `PayrollSummaries.Insert(0, created)` are byte-identical; the guard only wraps them. No net-salary / proration logic exists in the VM and none was added. |
| **Commission calculation flow** | untouched — `CreateCommissionRuleRequest` / `GenerateCommissionsFromAccountingAsync` calls and the `StatusMessage` wording are unchanged; the "Generated N …" message is still built from `generated.Count` on the success path only. |
| **Attendance timestamps / state rules** | untouched — `TimeSpan.TryParse` of the check-in time and `RecordAttendanceRequest` (with `DateOnly.FromDateTime(DateTime.Today)`, null check-out) are unchanged; `AttendanceStatus` is still decided by the service. |
| **Leave approval / rejection behavior** | untouched — `ApproveLeaveAsync(id)` / `RejectLeaveAsync(id)` still call the service with the leave-request id and still apply `ReplaceLeaveRequest(updated)` on success; on failure the row is left exactly as it was (test-asserted: status stays `Pending`). |
| **EmployeeProfile activate / deactivate / suspend** | preserved — service call + `LoadAsync` reload + `_onChanged` on success; on failure none of the three run beyond the (already-recorded) service call, and the loaded `Profile` + `State` are unchanged (test-asserted). |
| **`CanExecute` gating / RBAC** | untouched — every command predicate (`!string.IsNullOrWhiteSpace(...)`, `… is not null`) is unchanged and still outside the `try`. No permission check was added, removed, or moved. |
| **Constructor-time `_ = LoadAsync()`** | untouched. |
| **`LoadAsync` / `SearchAsync`** | untouched — including the pre-existing `ErrorMessage = exception.Message` Load-boundary surfacing (separate deferred P2 item). |

---

## D. SECURITY REVIEW

HR carries the most sensitive data in the sweep — salary / base pay, payroll net figures, bonus / deduction, commission values, attendance, leave reasons, and employee PII (name / email / phone).

| Vector | Outcome |
|---|---|
| `Exception.Message` on screen | **unreachable** — no exception variable is bound in any of the 13 catches; the on-screen text is the fixed constant `Strings.Common_ActionFailedMessage` (“The action could not be completed. Please try again.”). |
| `Exception.Message` / `.ToString()` in the log file | **unreachable** — `LogOperationFailed(string operation)` takes only `nameof(Method)`; the source-generated logger has no `Exception` parameter, so `LocalFileLoggerProvider` never renders a backend body. |
| Backend response payload (salary, payroll, commission, PII) | **unreachable** on both surfaces per the two rows above. |
| Internal identifiers (employee / leave / shift GUIDs) | **not logged** (operation name only), **not shown** (generic string only). |
| Employee PII from the create-employee form | preserved in the bound `NewEmployee*` properties for retry (in-memory, unchanged); never written anywhere new. |
| New log volume | one `LogLevel.Error` entry per failed command — matches Wave A and every instrumented VM; above the `Warning` file-log floor. |

**Test-enforced:** every Wave B failure test seeds the stub exception with `HrBackendSecret = "backend 500: employee Jordan Lee salary=3200 net=2870 commission=518.40 ssn=123"` (EmployeeProfile: `PiiSecret = "Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200"`) and asserts `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## E. TESTS

**+19 tests** (2,622 → 2,641). No existing test modified. Reuses `RecordingLogger<T>` and the existing HR stubs; stub changes are additive `Exception?` seams only (null path byte-identical — all pre-existing HR tests still pass unchanged). No global stub touched.

### E.1 `HrPageViewModelTests.cs` — +14

| Test | Asserts |
|---|---|
| `CreateEmployeeCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm` | no throw; `HasActionError`; message `== Strings.Common_ActionFailedMessage`; `State != Error`; `NewEmployeeFullName`/`Email` preserved; command was attempted |
| `RecordAttendanceCommand_Failure_…PreservesForm` | no throw; error set; `AttendanceCheckInTime`/`AttendanceNotes` preserved |
| `CreateShiftCommand_Failure_…DoesNotAddShift` | no throw; error set; `Shifts` empty; label preserved |
| `AssignShiftCommand_Failure_…DoesNotAddAssignment` | no throw; error set; `ShiftAssignments` empty |
| `RequestLeaveCommand_Failure_…PreservesReason` | no throw; error set; `LeaveRequests` empty; `LeaveReason` preserved |
| `ApproveLeaveCommand_Failure_…LeavesRowUnchanged` | no throw; error set; existing row still `LeaveStatus.Pending` |
| `RejectLeaveCommand_Failure_…SetsActionError` | no throw; error set + message |
| `CreateCommissionRuleCommand_Failure_…PreservesForm` | no throw; error set; `CommissionRules` empty; `NewRuleValue`/`Description` preserved |
| `GenerateCommissionsCommand_Failure_…LeavesStatusMessageUntouched` | no throw; error set; `CommissionTransactions` empty; `StatusMessage == string.Empty` |
| `GeneratePayrollCommand_Failure_…DoesNotInsertSummary` | no throw; error set; `PayrollSummaries` empty; `PayrollBonus` preserved |
| `CreateEmployeeCommand_Failure_LogsOperationNameOnly_NoPiiOrSalaryLeak` | log entry `Error` + `Operation=CreateEmployeeAsync`; `DoesNotContain(HrBackendSecret)` in entries **and** `ActionErrorMessage` |
| `GeneratePayrollCommand_Failure_LogsOperationNameOnly_NoSalaryLeak` | log entry `Error` + `Operation=GeneratePayrollAsync`; no secret leak |
| `CreateShiftCommand_SuccessAfterFailure_ClearsActionError` | fail → `HasActionError` true; clear seam + resubmit → `HasActionError` false, `ActionErrorMessage` null, `Shifts` has 1 |
| `GeneratePayrollCommand_SuccessAfterFailure_ClearsActionError` | same pattern; `PayrollSummaries` has 1 |

### E.2 `EmployeeProfileViewModelTests.cs` — +5

| Test | Asserts |
|---|---|
| `ActivateCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesStateAndOnChanged` | no throw; `HasActionError` + message; `State == Loaded`; `Profile` unchanged; **`onChanged` NOT invoked**; command attempted |
| `DeactivateCommand_Failure_DoesNotThrow_SetsActionError` | no throw; error set + message |
| `SuspendCommand_Failure_DoesNotThrow_SetsActionError` | no throw; error set + message |
| `ActivateCommand_Failure_LogsOperationNameOnly_NoPiiLeak` | log entry `Error` + `Operation=ActivateAsync`; `DoesNotContain(PiiSecret)` in entries **and** `ActionErrorMessage` |
| `ActivateCommand_SuccessAfterFailure_ClearsActionErrorAndInvokesOnChanged` | fail → error set; clear seam + resubmit → error cleared, `onChanged` invoked |

---

## F. VALIDATION

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020 / CA1031 / CA1848)
dotnet test  -c Debug --no-build → all 6 test projects Passed
```

| Project | Passed | Failed | Skipped | Δ vs `794648e` |
|---|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 | — |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 | — |
| Rojan.Desktop.Presentation.Tests | **698** | 0 | 0 | **+19** |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 | — |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 | — |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 | — |
| **TOTAL** | **2,641** | **0** | **0** | **+19** |

| Expected (Phase 8.70) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,644 PASS | 2,641 / 2,641 | ✅ (19 added; the ~2,644 estimate was a conservative upper bound) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## G. COMMIT READINESS

**Not committed** (per Phase 8.70 STRICT SCOPE). Ready for Phase 8.71 commit scope review.

- **Exactly 9 modified tracked files**, all under `…/HR/` (2 prod + 5 stub + 2 test). Verified:
  ```
  git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
   M src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs
   M src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/StubAttendanceCommandService.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/StubCommissionCommandService.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/StubEmployeeCommandService.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/StubPayrollCommandService.cs
   M tests/Rojan.Desktop.Presentation.Tests/HR/StubShiftCommandService.cs
  ```
- No new file. No `Strings.cs` / `.resx` change. No service / DI / interface / DTO / RBAC / navigation / `[LoggerMessage]` / shared-infra change.
- Recommended commit (single, per scope review §F): `fix(desktop): guard HR command failures`.
- Untracked `ROJAN_*.md` reports remain unstaged.

---

## STOP

Phase 8.70 implementation complete. 13 HR command guards (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3), each reusing the Wave A pattern + the existing `[LoggerMessage]` + the existing `Common_ActionFailedMessage` string; one additive `ActionErrorMessage`/`HasActionError` pair per VM. No service / DI / RBAC / payroll-commission-attendance-leave business-logic / localization-file change. Build 0/0, 2,641/2,641 tests, architecture 7/7.
**Next: Phase 8.71 — Wave B (HR) Commit Scope Review.** Awaiting authorization.
