# ROJAN AI — TEAM 3 — PHASE 8.51 — DETAIL PANELS LOGGING (WAVE 2C-3c) — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `884cec3` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_50_EMPLOYEE_INVOICE_SPECIALIST_LOGGING_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_49_BOOKINGWIZARD_LOGGING_COMMIT_REPORT_v1.md`
**Scope:** `EmployeeProfileViewModel` / `InvoiceProfileViewModel` / `SpecialistProfileViewModel` self-logging + `HrPageViewModel` / `AccountingPageViewModel` / `SpecialistPageViewModel` `ILoggerFactory` plumbing only.

---

## SCOPE CORRECTION — RESOLVED (Phase 8.51 Scope Correction Authorization)

The original Phase 8.51 authorization's **LOGGING BOUNDARIES** section listed only 5 explicit call sites
for `SpecialistProfileViewModel` (`LoadAsync`, `AssignServiceAsync`, `RemoveServiceAssignmentAsync`) while
stating "Total: 6 logging call sites." This report initially flagged the gap; the **Phase 8.51 Scope
Correction Authorization** then confirmed that **`SpecialistProfileViewModel.SaveChangesAsync`** — the 6th
instrumentable swallowing catch identified in the Phase 8.50 audit (§B.4), accidentally omitted from the
explicit list — **must be instrumented**.

**Correction applied:** `LogOperationFailed(nameof(SaveChangesAsync))` added as the last statement of that
catch (after the unchanged `EditableStatus = Specialist.Status; SaveErrorMessage = Strings.Specialists_SaveError;
HasSaveError = true;` — behaviour byte-unchanged), plus one corresponding test. **All other Phase 8.51
changes kept exactly as-is.**

**Final: 6 instrumented call sites, +12 tests, 2,606 / 2,606 passing.** No open items.

---

## A. FILES CHANGED (12 — all modified, 0 new)

`git diff --stat`: **12 files changed, 282 insertions(+), 16 deletions(-)** (includes the SaveChangesAsync correction)

### A.1 Production child ViewModels (3)

| # | File | Change | Instrumented catches |
|---|---|---|---|
| 1 | `src/…/ViewModels/HR/EmployeeProfileViewModel.cs` | `sealed`→`sealed partial`; +2 `using`; `ILogger<EmployeeProfileViewModel> _logger`; ctor reformatted, `+ ILogger<…>? logger = null` **after** `Action? onChanged = null`; `?? NullLogger<…>.Instance`; 1 instance-form `[LoggerMessage(EventId=1, Level=Error, "Employee profile operation failed. Operation={Operation}")]` | `LoadAsync` |
| 2 | `src/…/ViewModels/Accounting/InvoiceProfileViewModel.cs` | same shape; ctor reformatted, `+ ILogger<InvoiceProfileViewModel>? logger = null` (sole trailing optional); message `"Invoice profile operation failed. …"` | `LoadAsync` |
| 3 | `src/…/ViewModels/Specialists/SpecialistProfileViewModel.cs` | `sealed`→`sealed partial`; `+ using …Abstractions;` (Logging already present); `ILogger<SpecialistProfileViewModel> _logger`; ctor `+ ILogger<SpecialistProfileViewModel>? logger = null` **appended after `availabilityLogger`**; `?? NullLogger<…>.Instance`; message `"Specialist profile operation failed. …"` | `LoadAsync`, `SaveChangesAsync`, `AssignServiceAsync`, `RemoveServiceAssignmentAsync` |

**6 instrumented catch sites.** Each `LogOperationFailed(nameof(<Method>))` appended as the **last statement** of the existing `#pragma warning disable CA1031` broad catch — after the unchanged `ErrorMessage = exception.Message;` / `State = DashboardState.Error;` (`LoadAsync` ×3), after the unchanged `EditableStatus = Specialist.Status; SaveErrorMessage = Strings.Specialists_SaveError; HasSaveError = true;` (`SaveChangesAsync`), and after the unchanged `AssignmentErrorMessage = Strings.Specialists_AssignmentError; HasAssignmentError = true;` (`AssignServiceAsync` / `RemoveServiceAssignmentAsync`).

### A.2 Production parent ViewModels (3) — `ILoggerFactory` for ALL three (per authorization)

| # | File | Change |
|---|---|---|
| 4 | `src/…/ViewModels/HR/HrPageViewModel.cs` | `+ private readonly ILoggerFactory? _loggerFactory;`; ctor `+ ILoggerFactory? loggerFactory = null` (appended **after** the existing optional `ILogger<HrPageViewModel>? logger`); `_loggerFactory = loggerFactory;`; `SelectedEmployee` setter (`:250`) `new` passes `_loggerFactory?.CreateLogger<EmployeeProfileViewModel>()`. Existing `_logger` + instance `[LoggerMessage]` untouched |
| 5 | `src/…/ViewModels/Accounting/AccountingPageViewModel.cs` | `+ private readonly ILoggerFactory? _loggerFactory;`; ctor `+ ILoggerFactory? loggerFactory = null` (appended last, after `logger`); `_loggerFactory = loggerFactory;`; `SelectedInvoice` setter (`:113`) `new` passes `_loggerFactory?.CreateLogger<InvoiceProfileViewModel>()`. Existing `_logger` / `_posCheckoutLogger` / **static-form** `[LoggerMessage]` / `PosCheckoutViewModel` call site untouched |
| 6 | `src/…/ViewModels/Specialists/SpecialistPageViewModel.cs` | `+ private readonly ILoggerFactory? _loggerFactory;`; ctor `+ ILoggerFactory? loggerFactory = null` (appended **after** `availabilityLogger`); `_loggerFactory = loggerFactory;`; `SelectedSpecialist` setter (`:181`) `new` passes `_loggerFactory?.CreateLogger<SpecialistProfileViewModel>()` as the new last arg. Existing `_scheduleLogger` / `_availabilityLogger` typed pass-throughs untouched |

### A.3 Tests (6 modified, 0 new) — **+12 tests**

| # | File | Change |
|---|---|---|
| 7 | `tests/…/HR/EmployeeProfileViewModelTests.cs` | +2 `using`; **+2** (`LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak`, `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows`) |
| 8 | `tests/…/Accounting/InvoiceProfileViewModelTests.cs` | +2 `using`; **+2** (failure-log no-financial-leak, no-logger NullLogger safety) |
| 9 | `tests/…/Specialists/SpecialistProfileViewModelTests.cs` | +1 `using`; **+5** (`LoadAsync` / `SaveChangesCommand` / `AssignServiceCommand` / `RemoveServiceAssignmentCommand` failure-logs no-PII-leak, no-logger safety) |
| 10 | `tests/…/HR/HrPageViewModelTests.cs` | `MakeSut` +optional `RecordingLoggerFactory? loggerFactory = null` param (forwarded as new last ctor arg); **+1** (`LoggerFactory_ForwardedToEmployeeProfileChild_…`) |
| 11 | `tests/…/Accounting/AccountingPageViewModelTests.cs` | `MakeSut` +optional `RecordingLoggerFactory? loggerFactory = null` param; **+1** (`LoggerFactory_ForwardedToInvoiceProfileChild_…`) |
| 12 | `tests/…/Specialists/SpecialistPageViewModelTests.cs` | +1 `using`; **+1** (`LoggerFactory_ForwardedToSpecialistProfileChild_…`) — constructed inline (no `MakeSut` in this file) |

**No existing test body modified** (0 removed test lines). **No shared production stub modified** — `StubEmployeeQueryService` / `StubInvoiceQueryService` (`getProfile:` delegates), `StubSpecialistProfileQueryService` (`(_, _) => Task` delegate), and `StubSpecialistCommandService` (pre-existing `AssignServiceException` / `RemoveServiceAssignmentException` hooks) all accept throwing tasks as-is. **No new test helper** — `RecordingLogger<T>` and `RecordingLoggerFactory` (from `7aa1d1b`) reused; HR + Accounting test files add `using Rojan.Desktop.Presentation.Tests.Specialists;`, the Specialist test files are already in that namespace.

### A.4 NOT touched

`BookingWizardViewModel`, `BookingPageViewModel`, the Wave 2C-3a profile panels (`Customer`/`Service`/`InventoryProfileViewModel` + their page parents), DI registration (`Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs`), Domain, Infrastructure, Shell, Application, backend contracts / DTOs / interfaces, RBAC, authentication, navigation, `PosCheckoutViewModel`, `SpecialistScheduleViewModel` / `SpecialistAvailabilityViewModel` (grandchildren), `SpecialistProfileViewModel.SaveChangesAsync` (see flag), `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, all shared stubs.

---

## B. LoggerFactory PLUMBING

Per authorization: **`ILoggerFactory` for all three parents.**

| Parent | Why `ILoggerFactory` (not a 2nd `ILogger` field) |
|---|---|
| `HrPageViewModel` | Holds `ILogger<HrPageViewModel> _logger` + an **instance-form** `[LoggerMessage]` — a 2nd `ILogger` field would fail the source generator with **`SYSLIB1020`**. `ILoggerFactory` is not `ILogger` → no conflict; its instance `[LoggerMessage]` is untouched. |
| `AccountingPageViewModel` | Already holds 2 `ILogger` fields (`_logger` + `_posCheckoutLogger`) with a **static-form** `[LoggerMessage]`. `ILoggerFactory` keeps blast radius minimal (no edit to the committed static-form method or the `PosCheckoutViewModel` wiring). |
| `SpecialistPageViewModel` | Already holds 2 typed grandchild-logger fields (`_scheduleLogger`, `_availabilityLogger`). `ILoggerFactory` (per authorization) keeps consistency across the wave and future-proofs against the disclosed Wave 2D `SYSLIB1020` risk (Phase 8.50 §G.4). |

**Pattern (identical for all three):**
- Parent ctor gains `ILoggerFactory? loggerFactory = null` — one optional param, appended **after** the previously-last param.
- `private readonly ILoggerFactory? _loggerFactory;`; `_loggerFactory = loggerFactory;`.
- At the child `new` site (inside the `SelectedX` setter): `_loggerFactory?.CreateLogger<TChild>()` as the child's last ctor arg. `null` → child falls back to `NullLogger<TChild>.Instance`.
- `ILoggerFactory` registered by `AddLogging()`; all new params optional → **no DI change, no call-site breakage**.

**Child shape (all 3):** `sealed partial`, exactly **one** `ILogger<TSelf> _logger` field → **instance-form** `[LoggerMessage]`, `SYSLIB1020`-safe in the child (SpecialistProfile's `scheduleLogger`/`availabilityLogger` are ctor *parameters* passed straight to the grandchildren, not fields — no conflict). Optional ctor param appended last; `?? NullLogger<TSelf>.Instance`.

**`dotnet build -c Debug` → 0 warnings / 0 errors — no `SYSLIB1020`.**

---

## C. LOGGING BOUNDARIES

| VM | Method | Call added | Existing behaviour (unchanged) |
|---|---|---|---|
| `EmployeeProfileViewModel` | `LoadAsync` | `LogOperationFailed(nameof(LoadAsync));` | `ErrorMessage = exception.Message; State = Error;` |
| `InvoiceProfileViewModel` | `LoadAsync` | `LogOperationFailed(nameof(LoadAsync));` | `ErrorMessage = exception.Message; State = Error;` |
| `SpecialistProfileViewModel` | `LoadAsync` | `LogOperationFailed(nameof(LoadAsync));` | `ErrorMessage = exception.Message; State = Error;` |
| `SpecialistProfileViewModel` | `AssignServiceAsync` | `LogOperationFailed(nameof(AssignServiceAsync));` | `AssignmentErrorMessage = Strings.Specialists_AssignmentError; HasAssignmentError = true;` |
| `SpecialistProfileViewModel` | `RemoveServiceAssignmentAsync` | `LogOperationFailed(nameof(RemoveServiceAssignmentAsync));` | `AssignmentErrorMessage = Strings.Specialists_AssignmentError; HasAssignmentError = true;` |
| `SpecialistProfileViewModel` | `SaveChangesAsync` | `LogOperationFailed(nameof(SaveChangesAsync));` (added per the Scope Correction Authorization) | `EditableStatus = Specialist.Status; SaveErrorMessage = Strings.Specialists_SaveError; HasSaveError = true;` — revert byte-unchanged |

`EmployeeProfileViewModel.ActivateAsync/DeactivateAsync/SuspendAsync` and `SpecialistProfileViewModel.AddSkillAsync/RemoveSkillAsync` have **no `try`/`catch`** → not modified (missing-guard, out of the logging track).

`[LoggerMessage]` signature is `(string operation)` in all three classes — **no `Exception` parameter**. Level `Error`. `EventId = 1` per class.

Reachable log lines:
```
[Error] …EmployeeProfileViewModel:   Employee profile operation failed. Operation=LoadAsync
[Error] …InvoiceProfileViewModel:    Invoice profile operation failed. Operation=LoadAsync
[Error] …SpecialistProfileViewModel: Specialist profile operation failed. Operation={LoadAsync|SaveChangesAsync|AssignServiceAsync|RemoveServiceAssignmentAsync}
```

---

## D. SECURITY REVIEW

| Aspect | Confirmed |
|---|---|
| `Exception` object | **never passed** — signature `(string operation)`, no `Exception` param |
| `Exception.Message` | **never logged** — call sites pass `nameof(...)`; the pre-existing `ErrorMessage = exception.Message` (Load boundaries) is unchanged UI behaviour, never routed to the logger |
| Backend response body | never logged |
| **Employee** — name / employee no. / contact / **salary** / **commission amounts** / attendance / leave | never referenced by a log call |
| **Invoice** — **amounts / totals / line-item prices** / **payment amounts / methods** / receipt text / customer ref | never referenced |
| **Specialist** — name / title / **email** / **phone** / **bio** / **performance score / booking / cancellation / no-show counts** | never referenced |
| Service / skill / assignment identifiers | never referenced |
| Customer information | never referenced |
| Tokens | not held by these VMs |
| Level / EventId | `Error` / `1` |
| Behaviour | `#pragma` unchanged; `ErrorMessage` / `State` / `AssignmentErrorMessage` / `HasAssignmentError` / `SaveErrorMessage` / `HasSaveError` / `SaveChangesAsync` `EditableStatus` revert / grandchild construction all unchanged; log strictly appended last |

**Test-enforced no-leak** — each failure test seeds a recognizable secret into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)`:
- Employee: `"Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200"`
- Invoice: `"Amelia Hart / total 43.20 / Cash payment 43.20 / receipt"`
- Specialist (child ×4 — Load / SaveChanges / Assign / Remove): `"jordan.lee@rojan.example / 555-0100 / Specializes in balayage / performance 55"`
- Parent forwarding ×3: distinct seeded secrets, all asserted absent

---

## E. TESTS

### E.1 Added (12)

| # | File | Test | Asserts |
|---|---|---|---|
| 1 | `EmployeeProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` | one `Error` entry, `Operation=LoadAsync`, name/email/phone/salary secret absent |
| 2 | `EmployeeProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | 3-arg ctor → `State == Error`, `ErrorMessage == "boom"`, no throw |
| 3 | `InvoiceProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoFinancialLeak` | `Operation=LoadAsync`; total/payment secret absent |
| 4 | `InvoiceProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | 2-arg ctor → `State == Error`, `ErrorMessage == "boom"` |
| 5 | `SpecialistProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` | `Operation=LoadAsync`; email/phone/bio/performance secret absent |
| 6 | `SpecialistProfileViewModelTests` | `AssignServiceCommand_Failure_LogsErrorWithOperationNameOnly_NoLeak` | `HasAssignmentError`, `Operation=AssignServiceAsync`, secret absent |
| 7 | `SpecialistProfileViewModelTests` | `RemoveServiceAssignmentCommand_Failure_LogsErrorWithOperationNameOnly_NoLeak` | `HasAssignmentError`, `Operation=RemoveServiceAssignmentAsync`, secret absent |
| 8 | `SpecialistProfileViewModelTests` | `SaveChangesCommand_Failure_LogsErrorWithOperationNameOnly_AndStillRevertsEditableStatus` | `HasSaveError`, **`EditableStatus` reverts to `Active`** (behaviour preserved), `Operation=SaveChangesAsync`, secret absent |
| 9 | `SpecialistProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | 7-arg ctor → `State == Error`, `ErrorMessage == "boom"` |
| 10 | `HrPageViewModelTests` | `LoggerFactory_ForwardedToEmployeeProfileChild_ChildLoadFailureIsLoggedViaTheFactory` | child auto-selected; `RecordingLoggerFactory` single `Error`, category contains `EmployeeProfileViewModel`, `Operation=LoadAsync`, secret absent |
| 11 | `AccountingPageViewModelTests` | `LoggerFactory_ForwardedToInvoiceProfileChild_ChildLoadFailureIsLoggedViaTheFactory` | same shape for `InvoiceProfileViewModel` |
| 12 | `SpecialistPageViewModelTests` | `LoggerFactory_ForwardedToSpecialistProfileChild_ChildLoadFailureIsLoggedViaTheFactory` | same shape for `SpecialistProfileViewModel` |

### E.2 Behaviour preservation

All pre-existing `EmployeeProfileViewModelTests` / `InvoiceProfileViewModelTests` / `SpecialistProfileViewModelTests` / `HrPageViewModelTests` / `AccountingPageViewModelTests` / `SpecialistPageViewModelTests` pass unchanged — including `SpecialistProfileViewModel.SaveChangesAsync`'s `EditableStatus` revert + `HasSaveError` tests, the assign/remove no-corruption tests, and the parent auto-selection tests.

### E.3 Fresh full run (working tree, uncommitted)

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **663** | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,606** | **0** | **0** |

Delta from baseline `884cec3` (2,594): **+12** (Presentation.Tests 651 → 663).

---

## F. VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → 2,606 / 2,606 passing   0 failed   0 skipped
Architecture tests                → 7 / 7 passing
```

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,606 / 2,606 | **2,606 / 2,606** | ✅ (+12 — 6 boundaries after the SaveChangesAsync correction) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## G. COMMIT READINESS

| Gate | Status |
|---|---|
| Scope = 3 detail-profile VMs + 3 parent plumbing + their 6 test files | ✅ |
| No BookingWizard / BookingPageViewModel / Wave 2C-3a profile panels / DI / Domain / backend / RBAC / auth / navigation change | ✅ (not in `git status`) |
| `ILoggerFactory` (not a 2nd `ILogger` field) in all 3 parents → no `SYSLIB1020` | ✅ (build 0/0) |
| Each child: exactly one `ILogger<T>` field, `NullLogger` fallback, instance-form `[LoggerMessage]` `(string operation)` — no `Exception` | ✅ |
| 6 log calls `nameof`-only; no employee salary/commission / invoice amounts/payments / specialist email/phone/bio/performance / backend body | ✅ |
| Behaviour append-only after existing error handling; `SaveChangesAsync` `EditableStatus` revert / grandchild construction / auto-selection unchanged | ✅ |
| No shared production stub modified; no existing test body changed; no new file | ✅ |
| Build 0/0 · Tests 2,606/2,606 · Architecture 7/7 | ✅ |
| `SpecialistProfileViewModel.SaveChangesAsync` instrumentation | ✅ added per the Phase 8.51 Scope Correction Authorization (6th boundary) |

Working tree: **12 files** — `git status --porcelain`:
```
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/InvoiceProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/InvoiceProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistProfileViewModelTests.cs
```

Recommended commit subject (per Phase 8.50 §G.1): `fix(desktop): add ViewModel diagnostic logging (detail panels)`

---

## STOP

Implementation complete (including the Phase 8.51 Scope Correction: `SpecialistProfileViewModel.SaveChangesAsync`
is now the 6th instrumented boundary). Build 0/0, 2,606/2,606 tests, architecture 7/7. Working tree modified
across exactly 12 files (6 production + 6 test). **Nothing committed, pushed, merged, rebased, or amended.**
HEAD remains `884cec3`. No open items. Awaiting Phase 8.52 commit scope review.
