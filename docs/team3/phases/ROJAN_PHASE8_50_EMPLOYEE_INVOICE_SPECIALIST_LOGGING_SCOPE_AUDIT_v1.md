# ROJAN AI — TEAM 3 — PHASE 8.50 — EMPLOYEE / INVOICE / SPECIALIST PROFILE LOGGING (WAVE 2C-3c) — SCOPE AUDIT v1

**Type:** Audit only. **No source change. No test change. No logger / stub added. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `884cec36a6bbedea4b723227abbacb6dd3224441` — `fix(desktop): add ViewModel diagnostic logging (booking wizard)` (Phase 8.47, committed 8.49)
**Reference:** `ROJAN_PHASE8_49_BOOKINGWIZARD_LOGGING_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F/§G, `ROJAN_TEAM3_NEXT_STEPS_v1.md` "LATER — Wave 2C-3c".

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `884cec36a6bbedea4b723227abbacb6dd3224441` |
| HEAD subject | `fix(desktop): add ViewModel diagnostic logging (booking wizard)` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. Wave 2C-3b (`884cec3`) committed and validated (build 0/0, 2,594/2,594, arch 7/7). This audit adds no code.

---

## B. VIEWMODEL INVENTORY

### B.1 `EmployeeProfileViewModel` — `src/…/ViewModels/HR/EmployeeProfileViewModel.cs` (111 lines)

| Aspect | Current |
|---|---|
| Declaration | `public sealed class EmployeeProfileViewModel : ViewModelBase` — **not `partial`** |
| Constructor | `(string employeeId, IEmployeeQueryService queryService, IEmployeeCommandService commandService, Action? onChanged = null)` |
| Dependencies | `IEmployeeQueryService`, `IEmployeeCommandService` (Application), `Action?` callback |
| Existing `ILogger` field / `[LoggerMessage]` | **none** / **none**; no `Microsoft.Extensions.Logging` using |
| Lifetime | **Not DI-registered.** Constructed fresh per employee-selection by the parent, lives while that employee is selected |
| Parent creation path | `HrPageViewModel` `SelectedEmployee` setter (`:250`): `new EmployeeProfileViewModel(value.Id, _employeeQueryService, _employeeCommandService, () => _ = LoadAsync())` |
| Constructor-time load | ctor ends with `_ = LoadAsync();` (safe fire-and-forget) |
| Catch boundaries | **1** — `LoadAsync` (`:82–88`): `catch (Exception exception) { ErrorMessage = exception.Message; State = DashboardState.Error; }` |
| Uncaught action methods | `ActivateAsync` / `DeactivateAsync` / `SuspendAsync` — **no `try`/`catch`** → missing-guard, **out of the logging track** (a future dedicated error-handling phase), not instrumented here |

### B.2 `InvoiceProfileViewModel` — `src/…/ViewModels/Accounting/InvoiceProfileViewModel.cs` (110 lines)

| Aspect | Current |
|---|---|
| Declaration | `public sealed class InvoiceProfileViewModel : ViewModelBase` — **not `partial`** |
| Constructor | `(string invoiceId, IInvoiceQueryService queryService)` — no `Action?` |
| Dependencies | `IInvoiceQueryService` (Application) only |
| Existing `ILogger` field / `[LoggerMessage]` | **none** / **none**; no `Microsoft.Extensions.Logging` using |
| Lifetime | **Not DI-registered.** Constructed fresh per invoice-selection by the parent |
| Parent creation path | `AccountingPageViewModel` `SelectedInvoice` setter (`:113`): `new InvoiceProfileViewModel(value.Id, _invoiceQueryService)` |
| Constructor-time load | ctor ends with `_ = LoadAsync();` |
| Catch boundaries | **1** — `LoadAsync` (`:102–108`): `catch (Exception exception) { ErrorMessage = exception.Message; State = DashboardState.Error; }` |
| Uncaught action methods | none — **read-only** panel (invoice mutation/cancel lives on the parent `AccountingPageViewModel`) |

### B.3 `SpecialistProfileViewModel` — `src/…/ViewModels/Specialists/SpecialistProfileViewModel.cs` (454 lines)

| Aspect | Current |
|---|---|
| Declaration | `public sealed class SpecialistProfileViewModel : ViewModelBase` — **not `partial`**. **Already has `using Microsoft.Extensions.Logging;`** |
| Constructor | `(string specialistId, ISpecialistProfileQueryService profileQueryService, ISpecialistCommandService commandService, IIntelligenceEngine intelligenceEngine, IServiceQueryService serviceQueryService, ISpecialistScheduleQueryService scheduleQueryService, ISpecialistScheduleCommandService scheduleCommandService, ILogger<SpecialistScheduleViewModel>? scheduleLogger = null, ILogger<SpecialistAvailabilityViewModel>? availabilityLogger = null)` |
| Existing `ILogger` **fields** | **none for itself.** `scheduleLogger` / `availabilityLogger` are ctor **parameters** passed straight into `new SpecialistScheduleViewModel(...)` / `new SpecialistAvailabilityViewModel(...)` at `:88–89` — **not stored as fields** |
| Existing `[LoggerMessage]` | **none** |
| Lifetime | **Not DI-registered.** Constructed fresh per specialist-selection by the parent. Owns two grandchildren (`Schedule`, `Availability`), each self-loading |
| Parent creation path | `SpecialistPageViewModel` `SelectedSpecialist` setter (`:181`): `new SpecialistProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine, _serviceQueryService, _scheduleQueryService, _scheduleCommandService, _scheduleLogger, _availabilityLogger)` |
| Constructor-time load | ctor ends with `_ = LoadAsync();` |
| Catch boundaries | **4**: `LoadAsync` (`:305–311`, `catch (Exception exception)` → `ErrorMessage = exception.Message; State = Error`); `SaveChangesAsync` (`:370–377`, `catch (Exception)` no var → `EditableStatus = Specialist.Status; SaveErrorMessage = Strings.Specialists_SaveError; HasSaveError = true`); `AssignServiceAsync` (`:423–429`, `catch (Exception)` no var → `AssignmentErrorMessage = Strings.Specialists_AssignmentError; HasAssignmentError = true`); `RemoveServiceAssignmentAsync` (`:446–452`, same shape as AssignService) |
| Uncaught action methods | `AddSkillAsync` / `RemoveSkillAsync` — **no `try`/`catch`** → missing-guard, out of the logging track, not instrumented |

### B.4 Summary — 6 instrumentable catches across 3 child VMs

| VM | Instrument | Skip (no catch) |
|---|---|---|
| `EmployeeProfileViewModel` | `LoadAsync` | `ActivateAsync`, `DeactivateAsync`, `SuspendAsync` |
| `InvoiceProfileViewModel` | `LoadAsync` | — |
| `SpecialistProfileViewModel` | `LoadAsync`, `SaveChangesAsync`, `AssignServiceAsync`, `RemoveServiceAssignmentAsync` | `AddSkillAsync`, `RemoveSkillAsync` |

All 6 catch bodies already assign the user-facing message/state; the log call is appended **after**, as the last statement of the existing `#pragma warning disable CA1031` broad catch. No new catch.

---

## C. LIFECYCLE / PARENT PLUMBING ANALYSIS

All three are **type B — child ViewModels `new`-ed by a parent page ViewModel** inside a `SelectedX` property setter. None is DI-registered. The parent page VMs are `AddTransient` and each already resolves `ILogger<T>` / `ILoggerFactory` from DI (`AddLogging()`).

### C.1 Parent logger state

| Parent | `sealed`/`partial` | Own `ILogger<TSelf>` field | Own `[LoggerMessage]` | Other `ILogger` fields | Child `new` site |
|---|---|---|---|---|---|
| `HrPageViewModel` | `sealed partial` | **yes** (`_logger`, `:33`) | **yes — instance-form** (`:396`, `(string operation)`) | none | `:250` |
| `AccountingPageViewModel` | `sealed partial` | **yes** (`_logger`, `:33`) | **yes — static-form** (`:195`, `(ILogger logger, string operation, Exception exception)` — legacy, exception-passing) | `_posCheckoutLogger` (`ILogger<PosCheckoutViewModel>?`, `:32`) | `:113` |
| `SpecialistPageViewModel` | `sealed` (not partial) | **no** | **no** | `_scheduleLogger`, `_availabilityLogger` (typed grandchild pass-through, `:46–47`) | `:181` |

### C.2 SYSLIB1020 analysis & recommended plumbing per parent

`SYSLIB1020` ("multiple `ILogger` fields") fires **only when the class contains an instance-form `[LoggerMessage]`**.

| Parent | Adding a typed `ILogger<TChild>` field would… | **Recommended** |
|---|---|---|
| `HrPageViewModel` | **trip `SYSLIB1020`** — it has `_logger` + an instance-form `[LoggerMessage]`; a 2nd `ILogger` field is illegal | **`ILoggerFactory? loggerFactory = null`** pass-through (identical to Wave 2C-3a `Customer`/`Service`/`InventoryPageViewModel` and Wave 2C-3b `BookingPageViewModel`). `_loggerFactory?.CreateLogger<EmployeeProfileViewModel>()` at `:250`. Its instance `[LoggerMessage]` untouched. |
| `AccountingPageViewModel` | **not** trip `SYSLIB1020` (its `[LoggerMessage]` is **static-form**, which has no field-count limit) — a 3rd typed `ILogger<InvoiceProfileViewModel>?` field *would* compile | **`ILoggerFactory? loggerFactory = null`** pass-through — for cross-wave consistency and smallest blast radius (no touching the committed static-form `[LoggerMessage]` / `_posCheckoutLogger` / `PosCheckoutViewModel` call site). `_loggerFactory?.CreateLogger<InvoiceProfileViewModel>()` at `:113`. *(Acceptable alternative: a 3rd typed `ILogger<InvoiceProfileViewModel>?` field — static form permits it. `ILoggerFactory` preferred.)* |
| `SpecialistPageViewModel` | **not** trip `SYSLIB1020` (the class has **no `[LoggerMessage]` at all**) — a 3rd typed `ILogger<SpecialistProfileViewModel>?` field is safe, mirroring its existing `_scheduleLogger` / `_availabilityLogger` | **typed `ILogger<SpecialistProfileViewModel>? specialistProfileLogger = null`** pass-through (matches this class's own established style + the `AutomationPageViewModel` precedent for "parent with no own logger"). Passed at `:181` as the wizard-analog new last arg. *(Acceptable alternative: `ILoggerFactory` — more future-proof if Wave 2D later adds an instance `[LoggerMessage]` to this class; see §G.4.)* |

### C.3 Child logger shape (all 3 — identical to every prior wave)

- `sealed` → `sealed partial`
- `+ using Microsoft.Extensions.Logging;` `+ using Microsoft.Extensions.Logging.Abstractions;` (SpecialistProfile already has the first)
- `private readonly ILogger<TSelf> _logger;` — **exactly one `ILogger` field per child** → **instance-form** `[LoggerMessage]`, `SYSLIB1020`-safe in the child (SpecialistProfile's `scheduleLogger`/`availabilityLogger` are params, not fields — no conflict)
- ctor `+ ILogger<TSelf>? logger = null` — **appended last** (after `Action? onChanged` for Employee; sole trailing optional for Invoice; **after `availabilityLogger`** for SpecialistProfile)
- `_logger = logger ?? NullLogger<TSelf>.Instance;`
- one instance-form `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "<domain> profile operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` — **signature `(string operation)`, no `Exception` parameter**
- `LogOperationFailed(nameof(<Method>))` as the **last statement** of each existing broad catch, after the unchanged message/state assignment

Messages: `"Employee profile operation failed. Operation={Operation}"`, `"Invoice profile operation failed. Operation={Operation}"`, `"Specialist profile operation failed. Operation={Operation}"`.

### C.4 DI impact

**None.** No registration added/changed. `ILoggerFactory` / open-generic `ILogger<T>` already provided by `AddLogging()`. Every new ctor param is optional (`= null`) → existing DI resolution and all existing call sites/tests compile unchanged.

---

## D. SECURITY CONSTRAINTS

### D.1 Sensitive data reachable from the instrumented boundaries

| VM | Source | Fields |
|---|---|---|
| `EmployeeProfileViewModel.LoadAsync` | `EmployeeProfileDto` | `EmployeeDto` (name, employee no.), **`EmployeeDetailDto?`** (contact/address/role/**compensation** if present), `AttendanceDto[]`, `ShiftAssignmentDto[]`, `LeaveRequestDto[]`, **`CommissionTransactionDto[]`** (financial) |
| `InvoiceProfileViewModel.LoadAsync` | `InvoiceProfileDto` | `InvoiceDto` (**totals/amounts**, customer ref), `InvoiceItemDto[]` (line items + prices), **`PaymentDto[]`** (payment amounts/methods), **`ReceiptDto[]`** (receipt text) |
| `SpecialistProfileViewModel.LoadAsync` | `SpecialistProfileDto` | `SpecialistDto` (`FullName`, `Title`, **`Email`**, **`Phone`**, `Bio`), `SpecialistSkillDto[]`, `AssignedServiceDto[]`; plus `SpecialistIntelligenceDto` (**performance score / booking counts / cancellation / no-show**) |
| `SpecialistProfileViewModel.SaveChangesAsync` | `UpdateSpecialistRequest` | `FullName`, `Title`, **`Email`**, **`Phone`**, `Status`, `Bio` |
| `SpecialistProfileViewModel.AssignServiceAsync` / `RemoveServiceAssignmentAsync` | `serviceId` / `assignment.ServiceId` | service identifiers |
| all boundaries | backend response bodies embedded in `ApiException.Message` | — |

### D.2 The rule (non-negotiable)

**ALLOWED in a log line:** `Operation=<nameof(Method)>` and nothing else.

**FORBIDDEN — must never appear in any log this change produces:**
- Employee name / employee number / contact / address / role / **salary / compensation / commission amounts**
- Invoice **amounts / totals / line-item prices**, **payment amounts / methods**, receipt text, customer reference
- Specialist **name / email / phone / bio**, **performance score / booking / cancellation / no-show counts**
- Service / skill / assignment identifiers
- Customer information of any kind
- Backend response bodies
- `Exception.Message`
- the `Exception` object itself

### D.3 How the design guarantees it

| Guarantee | Mechanism |
|---|---|
| `Exception` object never passed | child `[LoggerMessage]` signature is `(string operation)` — no `Exception` parameter |
| `Exception.Message` never logged | call sites pass `nameof(<Method>)` only; the pre-existing `ErrorMessage = exception.Message` (Employee/Invoice/SpecialistProfile `LoadAsync`) / `Strings.*` (Specialist save/assign) assignments are unchanged UI behaviour, never routed to the logger |
| No field data logged | the message template is a constant with one `string` argument |
| Test-enforced | each failure test seeds a recognizable secret (employee name + salary; invoice total + payment; specialist email + phone + performance) into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)` |

Level `Error` (clears the `LocalFileLoggerProvider` `Warning` floor). `EventId = 1` per class.

---

## E. TEST STRATEGY

### E.1 Existing coverage (all present, all delegate/hook-driven — no stub change needed)

| VM | Test file | Test double(s) | Throwing seam |
|---|---|---|---|
| `EmployeeProfileViewModel` | `tests/…/HR/EmployeeProfileViewModelTests.cs` | `StubEmployeeQueryService` (`getProfile:` delegate), `StubEmployeeCommandService` | `getProfile: (_, _) => Task.FromException<EmployeeProfileDto>(...)` — already used by `Constructor_ProfileQueryThrows_…` |
| `InvoiceProfileViewModel` | `tests/…/Accounting/InvoiceProfileViewModelTests.cs` | `StubInvoiceQueryService` (`getProfile:` delegate) | `getProfile: (_, _) => Task.FromException<InvoiceProfileDto>(...)` — already used |
| `SpecialistProfileViewModel` | `tests/…/Specialists/SpecialistProfileViewModelTests.cs` | `StubSpecialistProfileQueryService` (`(_, _) => Task` ctor delegate), `StubSpecialistCommandService` (`UpdateSpecialistException`, and — to verify — `AssignServiceException` / `RemoveServiceAssignmentException` hooks; if absent, a **private nested** throwing stub, no shared-stub change) | `Task.FromException<SpecialistProfileDto>(...)` / `UpdateSpecialistException` — both already used |

`RecordingLogger<T>` and `RecordingLoggerFactory` (`tests/…/Specialists/`, committed `7aa1d1b`) reused. **No new test helper.** HR + Accounting test files add `using Rojan.Desktop.Presentation.Tests.Specialists;` (same as the Phase 8.43 profile-panel tests); the Specialist test file is already in that namespace.

Parent tests exist: `HrPageViewModelTests.cs`, `AccountingPageViewModelTests.cs`, `SpecialistPageViewModelTests.cs`.

### E.2 New tests (~11–13)

| # | File | Test | Asserts |
|---|---|---|---|
| 1 | `EmployeeProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` | one `Error` entry, `Operation=LoadAsync`, seeded name+salary secret absent |
| 2 | `EmployeeProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | no logger arg → `State == Error`, `ErrorMessage == "boom"`, no throw |
| 3 | `InvoiceProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoFinancialLeak` | `Operation=LoadAsync`; seeded total + payment secret absent |
| 4 | `InvoiceProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | as #2 |
| 5–8 | `SpecialistProfileViewModelTests` | `LoadAsync` / `SaveChangesCommand` / `AssignServiceCommand` / `RemoveServiceAssignmentCommand` failure-logs `Operation=<method>`, seeded email/phone/performance secret absent; `SaveChangesCommand` test also re-asserts `EditableStatus` reverts + `HasSaveError` (behaviour preserved) | as described |
| 9 | `SpecialistProfileViewModelTests` | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | as #2 |
| 10 | `HrPageViewModelTests` | `LoggerFactory_ForwardedToEmployeeProfileChild_ChildLoadFailureIsLoggedViaTheFactory` | select an employee with a failing profile query → `RecordingLoggerFactory` single `Error`, category contains `EmployeeProfileViewModel`, `Operation=LoadAsync`, secret absent |
| 11 | `AccountingPageViewModelTests` | `LoggerFactory_ForwardedToInvoiceProfileChild_ChildLoadFailureIsLoggedViaTheFactory` | same shape for `InvoiceProfileViewModel` |
| 12 | `SpecialistPageViewModelTests` | `Logger_ForwardedToSpecialistProfileChild_ChildLoadFailureIsLogged` | typed `RecordingLogger<SpecialistProfileViewModel>` forwarded via the parent → single `Error`, `Operation=LoadAsync`, secret absent |

**No existing test body modified.** Parent-test helper (`MakeSut` / equivalent) gains one optional `ILoggerFactory?` / `ILogger<SpecialistProfileViewModel>?` param, forwarded as the new last ctor arg — additive, existing callers unaffected.

---

## F. IMPLEMENTATION RECOMMENDATION

### F.1 Scope — 6 files (3 production child + 3 production parent) + 6 test files = **12 files, 0 new**

| # | File | Change |
|---|---|---|
| 1 | `src/…/ViewModels/HR/EmployeeProfileViewModel.cs` | `sealed`→`sealed partial`; +2 `using`; `ILogger<EmployeeProfileViewModel> _logger`; ctor `+ ILogger<…>? logger = null` (after `Action? onChanged`); `?? NullLogger<…>.Instance`; 1 instance-form `[LoggerMessage]`; **1** call — `LoadAsync` |
| 2 | `src/…/ViewModels/Accounting/InvoiceProfileViewModel.cs` | same shape; ctor `+ ILogger<InvoiceProfileViewModel>? logger = null` (sole trailing optional); **1** call — `LoadAsync` |
| 3 | `src/…/ViewModels/Specialists/SpecialistProfileViewModel.cs` | `sealed`→`sealed partial`; `+ using …Abstractions;` (Logging already present); `ILogger<SpecialistProfileViewModel> _logger`; ctor `+ ILogger<SpecialistProfileViewModel>? logger = null` **appended after `availabilityLogger`**; `?? NullLogger<…>.Instance`; 1 instance-form `[LoggerMessage]`; **4** calls — `LoadAsync`, `SaveChangesAsync`, `AssignServiceAsync`, `RemoveServiceAssignmentAsync` |
| 4 | `src/…/ViewModels/HR/HrPageViewModel.cs` | `+ ILoggerFactory? loggerFactory = null` ctor param (after existing `logger`) + `_loggerFactory` field; `:250` `new` passes `_loggerFactory?.CreateLogger<EmployeeProfileViewModel>()`. Existing `_logger` + instance `[LoggerMessage]` untouched |
| 5 | `src/…/ViewModels/Accounting/AccountingPageViewModel.cs` | `+ ILoggerFactory? loggerFactory = null` ctor param (append last) + `_loggerFactory` field; `:113` `new` passes `_loggerFactory?.CreateLogger<InvoiceProfileViewModel>()`. Existing `_logger` / `_posCheckoutLogger` / static-form `[LoggerMessage]` / `PosCheckoutViewModel` call site untouched |
| 6 | `src/…/ViewModels/Specialists/SpecialistPageViewModel.cs` | `+ ILogger<SpecialistProfileViewModel>? specialistProfileLogger = null` ctor param (after `availabilityLogger`) + field; `:181` `new` passes it as the new last arg. Existing `_scheduleLogger` / `_availabilityLogger` untouched |
| 7–12 | the 6 corresponding test files | +~12 tests (see §E.2); additive helper params only |

### F.2 Not touched

Interfaces / DTOs, DI registration (`Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs`), Domain, Infrastructure, Shell, Application, backend contracts, RBAC, authentication, navigation, `PosCheckoutViewModel`, `SpecialistScheduleViewModel` / `SpecialistAvailabilityViewModel` (grandchildren — their logger plumbing is from earlier phases, unchanged), the profile panels / page VMs from Waves 2A/2C-3a/2C-3b, `BookingWizardViewModel`, shared stubs, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`.

### F.3 Validation gates (before and after commit)

```
dotnet build -c Debug   → 0 warnings / 0 errors   (watch SYSLIB1020 — HrPage keeps its single ILogger field; AccountingPage/SpecialistPage designs avoid it)
dotnet test  -c Debug   → 2,594 + ~12 = ~2,606 / all pass
architecture tests      → 7 / 7
```

Expected test-count delta: **+12** (≈2,594 → ≈2,606).

### F.4 Risk assessment

| Risk | Level | Mitigation |
|---|---|---|
| `SYSLIB1020` on `HrPageViewModel` (instance `[LoggerMessage]` + a 2nd logger) | **avoided** | `ILoggerFactory`, not a 2nd `ILogger` field — the proven Wave 2C-3a/b pattern |
| Touching `AccountingPageViewModel`'s committed static-form `[LoggerMessage]` / `_posCheckoutLogger` | **avoided** | `ILoggerFactory` is purely additive; the static-form method and `PosCheckoutViewModel` wiring are not in the diff |
| `SpecialistProfileViewModel` param ordering (2 existing grandchild-logger params) | **low** | new self-logger param appended **after** `availabilityLogger`; all existing `new SpecialistProfileViewModel(...)` sites (≥15 in tests) pass 7 positional args and stop before the optional loggers → still compile |
| `SpecialistPageViewModel` future `SYSLIB1020` (Wave 2D) | **disclosed, deferred** | see §G.4 — not this wave's problem; a typed 3rd field is safe now (no `[LoggerMessage]` in the class) |
| Constructor-time load fires the log during `new` | **by design** | identical to every prior profile-panel wave; tests construct with a `RecordingLogger` and assert one entry |
| Behaviour regression in Specialist save/assign revert logic | **low** | log call appended strictly after the existing `EditableStatus`/`SaveErrorMessage`/`AssignmentErrorMessage` assignments; behaviour-preservation re-asserted in tests |

---

## G. COMMIT STRATEGY

### G.1 Recommendation — **one isolated commit**

```
fix(desktop): add ViewModel diagnostic logging (detail panels)
```

(subject per `ROJAN_TEAM3_NEXT_STEPS_v1.md` "LATER — Wave 2C-3c").

### G.2 Why not split

| TASK 7 split trigger | Assessment |
|---|---|
| "Invoice has higher financial sensitivity" | The `nameof`-only + no-`Exception` rule makes leakage **structurally impossible and identical in kind** for all three — Invoice's amounts are no more reachable than Employee's salary or Specialist's performance data. A financial-only diff buys no extra safety. |
| "Parent plumbing differs" | Two parents use `ILoggerFactory`, one uses a typed `ILogger<TChild>?` — but all three are the **same one-line pass-through pattern** (new optional param → field → `?.CreateLogger`/pass at the `new` site). The variance is a single well-understood choice per parent, documented in §C.2, not a structural difference. |
| "Security review needs isolation" | One PII-focused diff covering three structurally identical child VMs is **easier** to review as a unit than three fragmented commits. |
| Precedent | **Wave 2C-3a bundled 3 profile-panel child VMs + 3 parents into one commit (`7aa1d1b`).** Wave 2C-3c is the direct analog — same shape, same wave, same review surface. |

### G.3 Fallback (only if the authorizer wants Invoice isolated)

Split into two commits: `(Employee + Specialist)` then `(Invoice)`. Both parents in commit 1 use `ILoggerFactory` (HrPage) / typed (SpecialistPage); commit 2 is the `AccountingPageViewModel` + `InvoiceProfileViewModel` pair. Adds one extra scope-review → commit-execution cycle for no security gain — **not recommended**.

### G.4 Disclosed follow-up (not this wave)

`SpecialistPageViewModel` has an uninstrumented swallowing broad `catch` in its own `LoadAsync` (`:248`) and is a Wave 2D candidate. Once it gains its own instance-form `[LoggerMessage]`, it will hold `_scheduleLogger` + `_availabilityLogger` + `specialistProfileLogger` + `_logger` = 4 `ILogger` fields → `SYSLIB1020`, forcing a static-form or `ILoggerFactory` refactor **at that point**. This wave's typed-field choice is the locally-consistent one; the Wave 2D refactor is pre-existing debt (it already carries 2 grandchild logger fields), not introduced here. Flag for the Wave 2D audit.

---

## H. OPEN QUESTIONS FOR THE AUTHORIZER

1. **Confirm one isolated commit** (§G.1) vs. the Invoice split (§G.3). Recommendation: **one commit**.
2. **Confirm `SpecialistPageViewModel` uses a typed `ILogger<SpecialistProfileViewModel>?` pass-through** (§C.2, consistent with its existing grandchild loggers) vs. `ILoggerFactory` (more future-proof, §G.4). Recommendation: **typed**, with the Wave 2D SYSLIB1020 tension disclosed.

---

## STOP

Audit complete. No source or test change, no logger/stub added, no commit/push/merge/rebase/amend.
HEAD remains `884cec3`. **Awaiting Wave 2C-3c implementation authorization (Phase 8.51).**
