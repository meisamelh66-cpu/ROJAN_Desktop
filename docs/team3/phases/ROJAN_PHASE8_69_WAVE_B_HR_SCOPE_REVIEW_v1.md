# ROJAN AI — TEAM 3 — PHASE 8.69 — MISSING-GUARD SWEEP WAVE B (HR) — SCOPE REVIEW v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI / commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `794648e514f4a5bdaf853b1e9544858411fc84dd`
**Objective:** Audit and prepare Wave B reliability hardening — HR backend-connected command-failure guards, using the Wave A pattern committed at `794648e`.

---

## A. GIT STATE

```
git rev-parse HEAD      → 794648e514f4a5bdaf853b1e9544858411fc84dd
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `794648e` (Wave A commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** — zero modified / staged / deleted tracked files ✅ |
| Untracked | only `ROJAN_*.md` audit-trail reports (expected, never staged) |
| Last 3 commits | `794648e` guard customer/service/specialist command failures · `5ba554c` · `6a1bced` |

No tracked modifications. Baseline is clean for Wave B.

Baseline test suite (from checkpoint §E, `794648e`): **2,622 / 2,622** — Domain 456, Application 791, Presentation 679, Infrastructure 609, Shell 80, Architecture 7.

---

## B. HR COMMAND INVENTORY

Two ViewModels in scope: `src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs`, `.../HR/EmployeeProfileViewModel.cs`.
Both are already `sealed partial`, already have an instance-form `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "… Operation={Operation}")] private partial void LogOperationFailed(string operation);` (operation-name-only, no Exception), and each already calls it from its `LoadAsync` catch. **No logging-infrastructure change is needed** — Wave B only adds call sites to the existing signature.

### B.1 `HrPageViewModel` — 10 user-triggered command methods

| # | Method | Command / `CanExecute` | Current handling | Success-path side effects | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `CreateEmployeeAsync` | `CreateEmployeeCommand` (name+email non-empty) | **none** — bare `await _employeeCommandService.CreateEmployeeAsync` | clears 4 `NewEmployee*` fields; `await LoadAsync()`; re-selects created row | generic `App.DispatcherUnhandledException` dialog; form already cleared |
| 2 | `RecordAttendanceAsync` | `RecordAttendanceCommand` (`AttendanceSelectedEmployee` set) | **none** | clears `AttendanceCheckInTime`/`AttendanceNotes`; reloads `TodayAttendance`; refreshes `Summary` | generic dialog |
| 3 | `CreateShiftAsync` | `CreateShiftCommand` (label+start+end non-empty) | **none** (after a `TimeSpan.TryParse` early-return guard) | clears 3 shift fields; `Shifts.Add(created)` | generic dialog |
| 4 | `AssignShiftAsync` | `AssignShiftCommand` (shift+employee set) | **none** (after null early-return) | `ShiftAssignments.Add(created)` | generic dialog |
| 5 | `RequestLeaveAsync` | `RequestLeaveCommand` (employee set + reason non-empty) | **none** (after null early-return) | clears `LeaveReason`; `LeaveRequests.Insert(0, created)` | generic dialog |
| 6 | `ApproveLeaveAsync` | `ApproveLeaveCommand` (param `LeaveRequestDto`) | **none** (after null early-return) | `ReplaceLeaveRequest(updated)` (in-place list swap) | generic dialog — row action |
| 7 | `RejectLeaveAsync` | `RejectLeaveCommand` (param `LeaveRequestDto`) | **none** (after null early-return) | `ReplaceLeaveRequest(updated)` | generic dialog — row action |
| 8 | `CreateCommissionRuleAsync` | `CreateCommissionRuleCommand` (employee set + value non-empty) | **none** (after `decimal.TryParse` early-return) | clears `NewRuleValue`/`NewRuleDescription`; `CommissionRules.Add(created)` | generic dialog |
| 9 | `GenerateCommissionsAsync` | `GenerateCommissionsCommand` | **none** | inserts generated transactions; sets `StatusMessage` (“Generated N …”) | generic dialog |
| 10 | `GeneratePayrollAsync` | `GeneratePayrollCommand` (`PayrollSelectedEmployee` set) | **none** (after null early-return) | clears `PayrollBonus`/`PayrollDeduction`; `PayrollSummaries.Insert(0, created)` | generic dialog |

**Existing error/state surface on `HrPageViewModel`:**
- `State` (`DashboardState`) + `ErrorMessage` — **destructive**: `DashboardState.Error` replaces the whole page body via `DashboardWidget`. Set only by `LoadAsync` / `SearchAsync`. `ErrorMessage = exception.Message` there is the pre-existing Load-boundary surfacing (out of scope — “sanitize load-error surfacing” is a separate deferred P2 item).
- `StatusMessage` — transient **success** feedback (“Generated 1 new commission …”). Not an error channel.
- **No non-destructive inline command-error property exists.** This is the gap Wave B closes.

### B.2 `EmployeeProfileViewModel` — 3 user-triggered command methods

| # | Method | Command | Current handling | Success-path side effects | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `ActivateAsync` | `ActivateCommand` | **none** — `await _commandService.ActivateEmployeeAsync` | `await LoadAsync()`; `_onChanged?.Invoke()` (parent `HrPageViewModel.LoadAsync`) | generic dialog |
| 2 | `DeactivateAsync` | `DeactivateCommand` | **none** | `await LoadAsync()`; `_onChanged?.Invoke()` | generic dialog |
| 3 | `SuspendAsync` | `SuspendCommand` | **none** | `await LoadAsync()`; `_onChanged?.Invoke()` | generic dialog |

**Existing error/state surface on `EmployeeProfileViewModel`:** `State` + `ErrorMessage`, set only by `LoadAsync` (destructive Error state). **No action-error property.** (Audit 8.64 line 50 flagged this: “`LoadAsync` catch only — needs an action error property”.)

### B.3 Backend-connectivity note

HR services are **fake-backed** today (`Fake*Repository`; backend has no HR write endpoints yet — Phase 8.0). Wave B guards are still worth doing now: the pattern must be correct and consistent before the eventual backend connection, exactly as Wave A established it for the (real-backend) Customer/Service/Specialist modules. Classification is **P1 — UX consistency**, not P0: `App.DispatcherUnhandledException` already prevents any crash.

---

## C. GUARD STRATEGY

### C.1 Wave A pattern applies — with one additive property pair per ViewModel

The Wave A pattern is: local `try` around the existing command body + inline **non-destructive** localized error property + reuse the existing `[LoggerMessage]` (`LogOperationFailed(nameof(Method))`, operation-name-only, once). It applies to Wave B unchanged, except neither HR ViewModel yet has a non-destructive command-error property, so each needs one **additive** pair (private-set, no constructor change, no DI change) — the same move Wave A made for `CustomerProfileViewModel` (`SaveErrorMessage`/`HasSaveError`) and `CustomerPageViewModel` (`CreateErrorMessage`/`HasCreateError`).

**Proposed new property pairs** (names for authorizer confirmation in Phase 8.70):

| ViewModel | New pair | Localized value | Rationale |
|---|---|---|---|
| `HrPageViewModel` | `ActionErrorMessage` / `HasActionError` | `Strings.Common_ActionFailedMessage` | HR commands span create / record / assign / approve / generate — no single “save” or “create” verb fits; one shared inline area is adequate because only one HR section is visible at a time (same reasoning audit 8.64 gave AI Center). |
| `EmployeeProfileViewModel` | `ActionErrorMessage` / `HasActionError` | `Strings.Common_ActionFailedMessage` | Matches audit 8.64’s “needs an action error property” wording; lifecycle verbs (activate/deactivate/suspend) are “actions”. |

Same identifier in both keeps the module uniform. `Common_ActionFailedMessage` **already exists** in `Strings.cs` + all 3 `.resx` files (added in Wave A) — **no new localization key, no `.resx` edit in Wave B.**

### C.2 Per-method transformation (identical shape for all 13)

```csharp
// unchanged: early-return validation stays ABOVE the try (a rejected parse / null selection is not a failure)
if (PayrollSelectedEmployee is null) { return; }
_ = decimal.TryParse(PayrollBonus, out var bonus);
_ = decimal.TryParse(PayrollDeduction, out var deduction);

try
{
    var request = new GeneratePayrollRequest(PayrollSelectedEmployee.Id, PayrollMonth, PayrollYear, bonus, deduction);
    var created = await _payrollCommandService.GeneratePayrollSummaryAsync(request).ConfigureAwait(true);

    ActionErrorMessage = null; HasActionError = false;   // clear on success
    PayrollBonus = string.Empty;
    PayrollDeduction = string.Empty;
    PayrollSummaries.Insert(0, created);
}
#pragma warning disable CA1031 // Command boundary: a failed HR write must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A precedent).
catch (Exception)
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(GeneratePayrollAsync));
}
```

- **`catch (Exception)` with no exception variable** in all 13 → `Exception.Message`, backend response body, internal identifiers, and PII are structurally unreachable in both the on-screen message and the log entry (Wave A rule).
- **`CreateEmployeeAsync` / `ActivateAsync` / `DeactivateAsync` / `SuspendAsync`** call `await LoadAsync()` on the success path. `LoadAsync` is self-guarded (its own catch sets `State = Error`), so a reload failure cannot propagate into the new command catch — it is safe to keep `await LoadAsync()` (and, for `EmployeeProfileViewModel`, `_onChanged?.Invoke()`) inside the guarded block, matching the Wave A precedent (`CustomerProfileViewModel.SaveChangesAsync` wraps `await cmd` + `await LoadAsync()` together). `_onChanged?.Invoke()` therefore fires only when the command await succeeded.
- **`GenerateCommissionsAsync`**: on failure, set `ActionErrorMessage` and do **not** touch `StatusMessage` (leave any prior success text alone; nothing was generated).
- **`State` is never set by these guards** — HR command failures must not blank the page. `DashboardState` stays `Loaded`.
- Log call: the VM’s **existing** `LogOperationFailed(nameof(<Method>))` — once, in the catch. No new logger, no `ILoggerFactory` addition, no `SYSLIB1020` risk (instance-form `[LoggerMessage]` + single `ILogger` field already compiles in both VMs).

### C.3 Explicitly NOT changed

| Area | Confirmation |
|---|---|
| `IEmployeeCommandService` / `IAttendanceCommandService` / `IShiftCommandService` / `ICommissionCommandService` / `IPayrollCommandService` and every query service / DTO / request record | untouched |
| Application-layer HR services, `Fake*Repository`, backend contracts | untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`), ViewModel constructors, `ILoggerFactory` plumbing | untouched |
| RBAC / permission gates / `CanExecute` predicates | untouched — every `CanExecute` and every early-return validation preserved verbatim |
| **Payroll / commission / attendance / leave business logic** | untouched — parsing, request construction, list mutations, `ReplaceLeaveRequest`, `Summary` refresh all preserved; guard is purely a `try`/`catch` wrapper |
| `AsyncRelayCommand` / `RelayCommand` / `App.xaml.cs` / navigation | untouched |
| `LoadAsync` / `SearchAsync` catches, the existing `ErrorMessage = exception.Message` Load surfacing | untouched (separate deferred P2) |
| `[LoggerMessage]` signatures / event IDs / levels / messages | untouched — logging track is CLOSED; Wave B only adds call sites |
| Localization `.resx` files | untouched — `Common_ActionFailedMessage` already present |

---

## D. SECURITY REVIEW

HR is the highest-sensitivity domain in the sweep: it carries **salary / base-pay, payroll net figures, bonus / deduction, commission values, attendance records, leave reasons, and employee PII (name, email, phone)**. A backend failure response for any of these commands could embed such data in `ApiException.Message` / the response body.

| Vector | Wave B outcome |
|---|---|
| `Exception.Message` on screen | **unreachable** — no exception variable is bound in any of the 13 catches; the on-screen text is the fixed constant `Strings.Common_ActionFailedMessage` (“The action could not be completed. Please try again.”). |
| `Exception.Message` / `Exception.ToString()` in the log file | **unreachable** — `LogOperationFailed(string operation)` takes only `nameof(Method)`; the source-generated logger has no Exception parameter. `LocalFileLoggerProvider` therefore never renders a backend body. |
| Backend response payload (salary, payroll, PII) | **unreachable** on both surfaces, per the two rows above. |
| Internal identifiers (employee GUIDs, leave-request IDs, shift IDs) | **not logged** — operation name only. Not shown — generic string only. |
| PII (name / email / phone from the create-employee form) | **not logged, not surfaced.** Form field values are preserved in the bound `NewEmployee*` properties for retry (in-memory, same as before), never written anywhere new. |
| New log volume | one `LogLevel.Error` entry per failed command, above the `Warning` floor — matches Wave A and every other instrumented VM. |

The Wave B test plan (§E) **enforces** this: each stub command seam throws an exception whose message contains a seeded secret sentinel (e.g. `"SALARY-LEAK-8300 net=…"`), and tests assert `logger.Entries` and the ViewModel’s `ActionErrorMessage` both `DoesNotContain` that sentinel.

---

## E. TEST PLAN

### E.1 Stub seams (additive `Exception?` — null-path byte-identical)

Same seam idiom as Wave A (`StubCustomerCommandService.CreateCustomerException` → `Task.FromException<T>(…)` when set). Five HR stub command services:

| Stub file | New `Exception?` properties |
|---|---|
| `HR/StubEmployeeCommandService.cs` | `CreateEmployeeException`, `ActivateEmployeeException`, `DeactivateEmployeeException`, `SuspendEmployeeException` |
| `HR/StubAttendanceCommandService.cs` | `RecordAttendanceException`, `RequestLeaveException`, `ApproveLeaveException`, `RejectLeaveException` |
| `HR/StubShiftCommandService.cs` | `CreateShiftException`, `AssignShiftException` |
| `HR/StubCommissionCommandService.cs` | `CreateCommissionRuleException`, `GenerateCommissionsException` |
| `HR/StubPayrollCommandService.cs` | `GeneratePayrollException` |

Each: when the property is non-null, return `Task.FromException<T>(value)` **before** recording the call is optional — Wave A records the call then throws (lets a test still assert the request was attempted); keep that convention. Null path unchanged.

### E.2 New tests (`HrPageViewModelTests.cs`, `EmployeeProfileViewModelTests.cs`)

| Category | Tests | Count |
|---|---|---|
| **Failure does not throw + error surfaced** — one per command: `Assert.Null(Record.Exception(() => Cmd.Execute(param)))`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`; `State != DashboardState.Error` (HrPage) / `State == DashboardState.Loaded` (EmployeeProfile, LoadAsync not re-run) | 10 (HrPage) + 3 (EmployeeProfile) | 13 |
| **State preservation** — representative: `CreateEmployeeAsync` failure leaves `NewEmployeeFullName`/`Email` intact + list unchanged; `AssignShiftAsync` failure does not add to `ShiftAssignments`; `ApproveLeaveAsync` failure leaves the `LeaveRequestDto` row unchanged; `EmployeeProfileViewModel` failure leaves `Profile` unchanged **and `_onChanged` not invoked** (counter callback) | ~4 | 4 |
| **Logging — operation name only** — representative (`GeneratePayrollAsync`, `SuspendAsync`): `RecordingLogger` entry `Level == Error` && `Message.Contains("GeneratePayrollAsync")`; `Assert.DoesNotContain` seeded salary/PII sentinel in `entry.Message` **and** in `ActionErrorMessage` | 2 | 2 |
| **Error clears on next success** — representative (`CreateShiftAsync`, `ActivateAsync`): fail once → `HasActionError` true → clear the seam → execute again → `HasActionError == false`, `ActionErrorMessage == null` | 2 | 2 |
| **No-logger / NullLogger safety** — already covered by existing `NoLoggerSupplied_*` tests; extend one to also fire a failing command without throwing | 1 | 1 |

**Estimated new tests: ~22** (13 core + 9 supporting). Conservative suite projection: **2,622 → ~2,644**.

### E.3 Files changed (Phase 8.70 implementation)

| Group | Files | Count |
|---|---|---|
| Production | `ViewModels/HR/HrPageViewModel.cs`, `ViewModels/HR/EmployeeProfileViewModel.cs` | 2 |
| Test stubs | 5 HR `Stub*CommandService.cs` (§E.1) | 5 |
| Test VMs | `HR/HrPageViewModelTests.cs`, `HR/EmployeeProfileViewModelTests.cs` | 2 |
| **Total** | | **9** |

No new localization file, no new stub file, no new test helper, no `Strings.cs` edit.

---

## F. COMMIT STRATEGY

**Recommendation: a single Wave B commit.**

```
fix(desktop): guard HR command failures
```

Rationale:
- Both ViewModels are one module (`ViewModels/HR/…`), share the exact same pattern and the same new property name, and `EmployeeProfileViewModel` is a child ViewModel constructed by `HrPageViewModel` — they are not independently shippable.
- Total surface is small (9 files, ~2 prod files with mechanical `try`/`catch` wrappers).
- Matches the Wave A precedent (one commit for 5 VMs across 3 modules) and audit 8.64 line 129 (“**one commit** — `fix(desktop): guard HR command failures`”).
- A split (`HrPage` / `EmployeeProfile`) would produce a first commit whose tests reference a pattern the second commit repeats, with no isolation or bisection benefit.

Standard rhythm: 8.70 implementation (STOP before commit) → 8.71 commit scope review → 8.72 commit execution → checkpoint update.

---

## G. PHASE 8.70 RECOMMENDATION

**PHASE 8.70 — MISSING-GUARD SWEEP — WAVE B (HR) — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs` — add `ActionErrorMessage` / `HasActionError` (private-set, additive); wrap all 10 command methods (§B.1) in the §C.2 `try`/`catch`; each catch → set the pair + `LogOperationFailed(nameof(Method))`; clear the pair on each success path. No ctor change.
- `src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs` — add `ActionErrorMessage` / `HasActionError`; wrap `ActivateAsync` / `DeactivateAsync` / `SuspendAsync` (command await + `await LoadAsync()` + `_onChanged?.Invoke()`); catch → set the pair + `LogOperationFailed(nameof(Method))`. No ctor change.
- 5 HR `Stub*CommandService.cs` — additive `Exception?` seams (§E.1), null-path byte-identical.
- `tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs`, `.../HR/EmployeeProfileViewModelTests.cs` — ~22 new tests (§E.2). No existing test body changed.

**DO NOT:** change services / DI / ViewModel constructors / backend contracts / RBAC / `CanExecute` / navigation / command infrastructure / `[LoggerMessage]` signatures / `Strings.cs` / `.resx` files / payroll-commission-attendance-leave business logic. No commit.

**Risk: LOW.** Purely additive `try`/`catch` around existing awaits + one bindable property pair per VM (no ctor change, no DI). Fake-backed module — zero backend contract exposure. Only judgement point (already settled in §C.2): keep the self-guarded `await LoadAsync()` inside the guarded block, consistent with Wave A’s `CustomerProfileViewModel.SaveChangesAsync`.

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (no `SYSLIB1020`, no `CA1031`, no `CA1848`).
- Full suite → **~2,644 / ~2,644 PASS** (Presentation 679 → ~701; all other projects unchanged: Domain 456, Application 791, Infrastructure 609, Shell 80).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_70_WAVE_B_HR_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.71 commit scope review.

**Downstream (unchanged from audit 8.64):** Wave C = Inventory pages + profile + `AccountingPageViewModel.CancelInvoiceAsync` (`fix(desktop): guard inventory and invoice-cancel command failures`); then D (Organization + Reporting), E (AI Center), F (Automation tabs), G (P2 infra).

---

## STOP

Phase 8.69 scope review complete. HEAD `794648e`, tracked tree clean, baseline 2,622 / 2,622.
Wave B = 13 HR command guards across 2 ViewModels (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3), each reusing the Wave A pattern + the existing `[LoggerMessage]` + the existing `Common_ActionFailedMessage` string; one additive `ActionErrorMessage`/`HasActionError` pair per VM; no service / DI / RBAC / business-logic / localization-file change. ~9 files, ~22 tests, one commit `fix(desktop): guard HR command failures`.
**Recommended next: Phase 8.70 — Wave B (HR) Implementation.** Awaiting authorization.
