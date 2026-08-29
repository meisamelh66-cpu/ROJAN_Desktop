# ROJAN AI — TEAM 3 — PHASE 8.71 — MISSING-GUARD SWEEP WAVE B (HR) — COMMIT SCOPE REVIEW v1

**Type:** Pre-commit review. **STRICT MODE — no source change, no test change, no new file, no commit, no push, no merge, no rebase, no amend.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `794648e514f4a5bdaf853b1e9544858411fc84dd`
**References:** `ROJAN_PHASE8_69_WAVE_B_HR_SCOPE_REVIEW_v1.md`, `ROJAN_PHASE8_70_WAVE_B_HR_IMPLEMENTATION_REPORT_v1.md`
**Verdict:** ✅ **READY TO COMMIT** — scope clean, 9 files, 0 new, build 0/0, 2,641/2,641 tests, architecture 7/7.

---

## A. GIT STATE

```
git rev-parse HEAD        → 794648e514f4a5bdaf853b1e9544858411fc84dd
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty)   ← nothing staged
```

| Check | Result |
|---|---|
| HEAD | `794648e` (Wave A commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Staging area | **empty** — nothing staged yet ✅ |
| Modified tracked files | **9**, all under `…/HR/` ✅ |
| New tracked files | **0** ✅ |
| Untracked | only `ROJAN_*.md` audit-trail reports (never staged) ✅ |

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

`git diff --stat`: **9 files changed, 682 insertions(+), 63 deletions(-)**. The 63 deletions are entirely the original single-line command bodies re-indented into their `try`-wrapped form (verified line-by-line in the diff below) — no property, validation, service call, or assertion removed.

Matches Phase 8.69 §E.3 estimate (2 prod + 5 stub + 2 test = 9) and Phase 8.70 report §A exactly.

---

## B. SCOPE VERIFICATION

### B.1 Production (2 files) — in scope

| File | Diff summary |
|---|---|
| `HrPageViewModel.cs` | `+ using …Localization;` (1 line, alpha-ordered); `+ _actionErrorMessage` / `_hasActionError` fields (2); `+ ActionErrorMessage` / `HasActionError` properties (18); 10 command methods wrapped in `try { … existing body … } catch (Exception) { ActionError + LogOperationFailed(nameof) }` with the `#pragma warning disable/restore CA1031` boundary comment. **Nothing else.** `LoadAsync`, `SearchAsync`, `ReplaceEmployees`, `ReplaceLeaveRequest`, ctor, all bindable form properties, all `ICommand` wiring, `[LoggerMessage]` signature — untouched. |
| `EmployeeProfileViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; 3 command methods (`ActivateAsync`, `DeactivateAsync`, `SuspendAsync`) wrapped identically. `LoadAsync`, ctor, `[LoggerMessage]` signature — untouched. |

No constructor signature change, no new field of a service/logger type, no DI registration touched.

### B.2 Tests (2 files) — approved HR test files only

| File | Diff summary |
|---|---|
| `HrPageViewModelTests.cs` | `+ using …Localization;`; **+14 `[Fact]`** appended after the last existing test. Zero existing test bodies changed. |
| `EmployeeProfileViewModelTests.cs` | `+ using …Localization;`; **+5 `[Fact]`** + one private helper `LoadingQueryService()`. Zero existing test bodies changed. |

### B.3 Stubs (5 files) — additive HR failure seams only

All five follow the Wave A seam idiom (`Customers.StubCustomerCommandService.CreateCustomerException`): a nullable `Exception?` auto-property; the command records its call **then** returns `Task.FromException<T>(value)` when the property is set, else the original `Task.FromResult(...)` verbatim.

| Stub | New `Exception?` seams | Null-path identical? |
|---|---|---|
| `StubEmployeeCommandService.cs` | `CreateEmployeeException`, `ActivateEmployeeException`, `DeactivateEmployeeException`, `SuspendEmployeeException` | ✅ (verified in diff — original `Task.FromResult` retained as the `:` branch) |
| `StubAttendanceCommandService.cs` | `RecordAttendanceException`, `RequestLeaveException`, `ApproveLeaveException`, `RejectLeaveException` | ✅ |
| `StubShiftCommandService.cs` | `CreateShiftException`, `AssignShiftException` | ✅ |
| `StubCommissionCommandService.cs` | `CreateCommissionRuleException` (GenerateCommissions failure uses the **pre-existing** ctor delegate `_generateCommissions`) | ✅ |
| `StubPayrollCommandService.cs` | `GeneratePayrollException` | ✅ |

`CorrectAttendanceAsync` / `AssignDepartmentAsync` (interface members not exercised by these ViewModels) are untouched. No global/shared stub touched — all five live in `tests/…/HR/`.

### B.4 Confirmed UNTOUCHED

```
git diff --name-only                    → 9 files, all tests/…/HR/ or src/…/ViewModels/HR/
```

| Area | Status |
|---|---|
| Payroll services (`IPayrollCommandService` / `IPayrollQueryService`, Application impls, `FakePayrollRepository`) | ✅ untouched |
| Commission services (`ICommissionCommandService` / `ICommissionQueryService`, impls, fake repo) | ✅ untouched |
| Attendance / Leave services (`IAttendanceCommandService` / `IAttendanceQueryService`) | ✅ untouched |
| Employee / Shift services + all HR DTOs / request records | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| Backend contracts / HTTP clients / API layer | ✅ untouched |
| RBAC / permission gates / `CanExecute` predicates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / shell | ✅ untouched |
| `AsyncRelayCommand` / `RelayCommand` / `ViewModelBase` / `App.xaml.cs` | ✅ untouched |
| `Strings.cs`, `Strings.resx`, `Strings.en.resx`, `Strings.ar.resx` (`Common_ActionFailedMessage` already shipped in Wave A `794648e`) | ✅ untouched |
| Every `[LoggerMessage]` signature / EventId / Level / Message | ✅ untouched |
| `LoadAsync` / `SearchAsync` catches (incl. pre-existing `ErrorMessage = exception.Message`) | ✅ untouched |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

---

## C. GUARD REVIEW — all 13

Pattern verified identical in every guard (diff-confirmed):

```
<validation + request-building: UNCHANGED, outside the try>
try
{
    <original command await + original success body: UNCHANGED>
    ActionErrorMessage = null; HasActionError = false;   // added: clear-on-success
}
#pragma warning disable CA1031  // boundary comment
catch (Exception)               // no exception variable bound
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(<Method>));
}
```

| # | Method | Flow preserved | Validation / `CanExecute` preserved | Service call unchanged |
|---|---|---|---|---|
| 1 | `HrPage.CreateEmployeeAsync` | form clears + `LoadAsync` + re-select inside `try` | `CanExecute` (name+email); `decimal.TryParse` outside `try` | ✅ `CreateEmployeeAsync(request)` |
| 2 | `HrPage.RecordAttendanceAsync` | clears + `TodayAttendance` reload + `Summary` refresh | `if (AttendanceSelectedEmployee is null) return;` + `TimeSpan.TryParse` outside | ✅ `RecordAttendanceAsync(request)` |
| 3 | `HrPage.CreateShiftAsync` | 3 field clears + `Shifts.Add` | `if (!TryParse …) return;` outside; `CanExecute` (label+times) | ✅ `CreateShiftAsync(request)` |
| 4 | `HrPage.AssignShiftAsync` | `ShiftAssignments.Add` | `if (shift/employee null) return;` outside; `CanExecute` | ✅ `AssignShiftAsync(request)` |
| 5 | `HrPage.RequestLeaveAsync` | `LeaveReason` clear + `LeaveRequests.Insert(0, …)` | `if (LeaveSelectedEmployee is null) return;` outside; `CanExecute` (employee+reason) | ✅ `RequestLeaveAsync(request)` |
| 6 | `HrPage.ApproveLeaveAsync` | `ReplaceLeaveRequest(updated)` | `if (leaveRequest is null) return;` outside | ✅ `ApproveLeaveAsync(leaveRequest.Id)` |
| 7 | `HrPage.RejectLeaveAsync` | `ReplaceLeaveRequest(updated)` | `if (leaveRequest is null) return;` outside | ✅ `RejectLeaveAsync(leaveRequest.Id)` |
| 8 | `HrPage.CreateCommissionRuleAsync` | `NewRuleValue`/`Description` clears + `CommissionRules.Add` | `if (employee null \|\| !decimal.TryParse …) return;` outside; `CanExecute` | ✅ `CreateCommissionRuleAsync(request)` |
| 9 | `HrPage.GenerateCommissionsAsync` | transaction inserts + `StatusMessage` — **success branch only**; failure leaves `StatusMessage` untouched | (no validation — command always enabled) | ✅ `GenerateCommissionsFromAccountingAsync()` |
| 10 | `HrPage.GeneratePayrollAsync` | `PayrollBonus`/`Deduction` clears + `PayrollSummaries.Insert(0, …)` | `if (PayrollSelectedEmployee is null) return;` + 2× `decimal.TryParse` outside; `CanExecute` | ✅ `GeneratePayrollSummaryAsync(request)` |
| 11 | `EmployeeProfile.ActivateAsync` | `LoadAsync` then `_onChanged?.Invoke()` — **inside `try`, after** the awaited command | (none) | ✅ `ActivateEmployeeAsync(_employeeId)` |
| 12 | `EmployeeProfile.DeactivateAsync` | `LoadAsync` then `_onChanged?.Invoke()` | (none) | ✅ `DeactivateEmployeeAsync(_employeeId)` |
| 13 | `EmployeeProfile.SuspendAsync` | `LoadAsync` then `_onChanged?.Invoke()` | (none) | ✅ `SuspendEmployeeAsync(_employeeId)` |

**`_onChanged` semantics (11–13):** placed after the awaited command inside `try`, so on a command failure it does **not** fire — a failed lifecycle change no longer triggers a parent `HrPageViewModel` reload. `LoadAsync` is self-guarded (its own catch sets `State = Error`) so it cannot propagate into the command catch. Test-asserted (`ActivateCommand_Failure_…PreservesStateAndOnChanged`).

**Business-logic confirmation:**

| Domain | Confirmation |
|---|---|
| **Payroll calculation** | No net-salary / proration / rounding logic exists in the ViewModel; none added. `decimal.TryParse` of bonus/deduction and `GeneratePayrollRequest` construction are byte-identical and outside the `try`. |
| **Commission calculation** | `CreateCommissionRuleRequest` (type/value/description) and `GenerateCommissionsFromAccountingAsync()` calls unchanged; the "Generated N …" `StatusMessage` wording and its `generated.Count` branch are unchanged and success-only. |
| **Attendance state** | `RecordAttendanceRequest` (today's date, parsed check-in, null check-out/status) unchanged; `AttendanceStatus` still resolved server-side. No timestamp logic touched. |
| **Leave approval / rejection** | `ApproveLeaveAsync(id)` / `RejectLeaveAsync(id)` still pass only the leave-request id; `ReplaceLeaveRequest(updated)` still applied on success; on failure the row is left exactly as-is (test-asserted: stays `LeaveStatus.Pending`). |

---

## D. SECURITY REVIEW

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **Not exposed.** No exception variable is bound in any of the 13 `catch (Exception)` clauses. `ActionErrorMessage` is only ever assigned `null` or the compile-time constant `Strings.Common_ActionFailedMessage`. |
| `Exception.Message` / `.ToString()` → log file | **Not exposed.** `LogOperationFailed(string operation)` (instance-form `[LoggerMessage]`, EventId 1, `Error`) has **no `Exception` parameter**; call sites pass only `nameof(<Method>)`. `LocalFileLoggerProvider` therefore renders no backend body. |
| Salary / base-pay / payroll-net values | **Not exposed** — unreachable on both surfaces per the two rows above; `GeneratePayrollRequest`/`PayrollSummaryDto` never touch `ActionErrorMessage` or the logger. |
| Commission values | **Not exposed** — same. |
| Employee PII (name / email / phone) | **Not exposed** — create-employee form values stay in the bound `NewEmployee*` properties for retry (in-memory, pre-existing); never logged, never placed in `ActionErrorMessage`. |
| Backend response payload | **Not exposed** — no code path forwards it. |
| Internal identifiers (employee / leave / shift GUIDs) | **Not logged** (operation name only), **not shown** (generic string only). |

**UI receives only:** `Strings.Common_ActionFailedMessage` → "The action could not be completed. Please try again." (en) / fa / ar — already shipped in `794648e`, unchanged here.
**Logging receives only:** `Operation=<MethodName>` via the existing `[LoggerMessage]` template `"HR page operation failed. Operation={Operation}"` / `"Employee profile operation failed. Operation={Operation}"`.

**Test-enforced:** failure tests seed the stub exception with `HrBackendSecret = "backend 500: employee Jordan Lee salary=3200 net=2870 commission=518.40 ssn=123"` (EmployeeProfile: `PiiSecret`) and assert `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## E. LOGGING REVIEW

| Check | Result |
|---|---|
| Existing `[LoggerMessage]` reused | ✅ `HrPageViewModel.LogOperationFailed(string operation)` and `EmployeeProfileViewModel.LogOperationFailed(string operation)` — both pre-existing, unchanged signatures. Only new **call sites** added (10 + 3). |
| New logger field / type | ✅ none — `HrPageViewModel` keeps `_logger` + `_loggerFactory` (used only for the `EmployeeProfileViewModel` child); `EmployeeProfileViewModel` keeps its single `_logger`. |
| DI / constructor change | ✅ none |
| Duplicate logging | ✅ none — each guarded method logs **once** in its catch. `LoadAsync` (which some success paths call) has its own separate catch that only fires on a load failure. A create-then-failed-reload cannot double-log into the command catch (reload is self-guarded). |
| `SYSLIB1020` risk | ✅ none — both classes are already `sealed partial` with a **single** `ILogger` field and instance-form `[LoggerMessage]`; that combination already compiled at `794648e`. Build is **0 warnings** (see §F). |
| `CA1848` (raw `_logger.Log*`) | ✅ none — no raw logger call added. |
| `CA1031` | ✅ suppressed locally with the documented `#pragma warning disable/restore CA1031` boundary comment, identical convention to the pre-existing `LoadAsync` catch and to Wave A. |

---

## F. TESTS

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all 6 projects Passed
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

| Expected (Phase 8.71) | Actual | Status |
|---|---|---|
| Tests 2,641 / 2,641 PASS | 2,641 / 2,641 | ✅ |
| Build 0 / 0 | 0 / 0 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

**+19 new tests reviewed:**

| Aspect | Coverage |
|---|---|
| **Failure handling** | 13 tests — one per command: `Record.Exception(() => Cmd.Execute(param))` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`. |
| **State preservation** | `CreateEmployee` → form fields + `State != Error`; `CreateShift`/`RequestLeave`/`CreateCommissionRule` → form fields + list not mutated; `AssignShift`/`GeneratePayroll` → list empty; `ApproveLeave` → existing row stays `Pending`; `EmployeeProfile` → `State == Loaded`, `Profile` unchanged. |
| **`_onChanged` behavior** | `ActivateCommand_Failure_…` asserts `changed == false`; `ActivateCommand_SuccessAfterFailure_…` asserts `changed == true` after the retry succeeds. |
| **Success clears error** | `CreateShiftCommand_SuccessAfterFailure_ClearsActionError`, `GeneratePayrollCommand_SuccessAfterFailure_ClearsActionError`, `ActivateCommand_SuccessAfterFailure_…` — fail → `HasActionError` true → clear seam + resubmit → `HasActionError` false, `ActionErrorMessage` null, entity added. |
| **Sensitive-data leak** | `CreateEmployeeCommand_Failure_LogsOperationNameOnly_NoPiiOrSalaryLeak`, `GeneratePayrollCommand_Failure_LogsOperationNameOnly_NoSalaryLeak`, `ActivateCommand_Failure_LogsOperationNameOnly_NoPiiLeak` — assert `Operation=<Method>` present in a `LogLevel.Error` entry **and** `DoesNotContain(secret)` in both `logger.Entries` and `ActionErrorMessage`. |
| **`GenerateCommissions` `StatusMessage`** | `…LeavesStatusMessageUntouched` asserts `StatusMessage == string.Empty` after a failed generate. |

All new tests use the existing `RecordingLogger<T>` / `RecordingLoggerFactory` and the existing HR stubs (with the additive seams). Async commands complete synchronously in-test because every stub returns an already-completed `Task` (Wave A convention).

---

## G. COMMIT READINESS

✅ **Ready.** No blockers.

**Staging plan (Phase 8.72 — explicit paths only, no `git add .` / `-A`):**

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/StubEmployeeCommandService.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/StubAttendanceCommandService.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/StubShiftCommandService.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/StubCommissionCommandService.cs
git add tests/Rojan.Desktop.Presentation.Tests/HR/StubPayrollCommandService.cs
git diff --cached --name-only        # expect exactly 9
```

**Commit message (EXACT):**

```
fix(desktop): guard HR command failures

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

**Post-commit validation to run:** `dotnet build -c Debug` (expect 0/0) · full `dotnet test` (expect 2,641/2,641) · architecture (expect 7/7) · `git log --oneline -3`.

**Checkpoint update (Phase 8.72):** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` — new HEAD; §B commit table + row; §E test count 2,622 → 2,641 (Presentation 679 → 698); §G Missing-Guard Sweep track: Wave B ✅ / Wave C NEXT; §H next phase.

---

## STOP

Phase 8.71 commit scope review complete. **9 modified files, 0 new**, all under `…/HR/`. All 13 guards preserve validation / `CanExecute` / service calls / success flow; no payroll, commission, attendance, or leave business-logic change. No `Exception.Message` / salary / commission / PII / backend-payload exposure — UI gets only `Common_ActionFailedMessage`, logging only `Operation=nameof(Method)` via the existing `[LoggerMessage]`. No new logger, no DI change, no `SYSLIB1020`. Build 0/0, **2,641/2,641** tests, architecture 7/7.
**Next: Phase 8.72 — Wave B (HR) Commit Execution.** Awaiting authorization.
