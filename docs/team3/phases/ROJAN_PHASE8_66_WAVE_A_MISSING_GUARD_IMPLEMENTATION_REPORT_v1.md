# ROJAN AI — TEAM 3 — PHASE 8.66 — MISSING-GUARD SWEEP WAVE A (Customer / Service / Specialist commands) — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `5ba554c` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_65_WAVE_A_MISSING_GUARD_SCOPE_REVIEW_v1.md`
**Scope:** the 12 unguarded backend-connected Customer/Service/Specialist write commands.

---

## A. FILES CHANGED (17 — all modified, 0 new)

`git diff --stat`: **17 files changed, 592 insertions(+), 42 deletions(-)**

### A.1 Production ViewModels (5)

| # | File | Change |
|---|---|---|
| 1 | `Customers/CustomerPageViewModel.cs` | `+ using …Localization;`; `+ _createErrorMessage`/`_hasCreateError` fields + `CreateErrorMessage`/`HasCreateError` **new bindable pair** (mirrors `ServicePageViewModel`); `CreateCustomerAsync` wrapped in `try`/`catch (Exception)` — success clears the flags, failure sets `CreateErrorMessage = Strings.Common_ActionFailedMessage; HasCreateError = true; LogOperationFailed(nameof(CreateCustomerAsync))` |
| 2 | `Customers/CustomerProfileViewModel.cs` | `+ using …Localization;`; `+ _saveErrorMessage`/`_hasSaveError` fields + `SaveErrorMessage`/`HasSaveError` **new bindable pair** (mirrors `ServiceProfileViewModel`); `AddNoteAsync` / `AddTagAsync` / `RemoveTagAsync` / `SaveChangesAsync` each wrapped — failure sets `SaveErrorMessage = Strings.Common_ActionFailedMessage; HasSaveError = true; LogOperationFailed(nameof(<Method>))`; `SaveChangesAsync` catch additionally **reverts `EditableStatus = Customer.Status`** (mirrors `ServiceProfileViewModel.SaveChangesAsync`) |
| 3 | `Services/ServiceProfileViewModel.cs` | `AssignSpecialistAsync` / `UnassignSpecialistAsync` wrapped — failure reuses the **existing** `SaveErrorMessage`/`HasSaveError` + `Strings.Services_SaveError` + `LogOperationFailed(nameof(<Method>))`. No new property. |
| 4 | `Specialists/SpecialistProfileViewModel.cs` | `AddSkillAsync` / `RemoveSkillAsync` wrapped — failure reuses the **existing** `SaveErrorMessage`/`HasSaveError` + `Strings.Specialists_SaveError` + `LogOperationFailed(nameof(<Method>))`. No new property. |
| 5 | `Specialists/SpecialistPageViewModel.cs` | `+ using …Localization;`; `+ _createErrorMessage`/`_hasCreateError` fields + `CreateErrorMessage`/`HasCreateError` **new bindable pair**; `+ private ILogger Logger => _loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance;` **helper** (removes the duplicated expression — `LoadAsync`'s existing `LogOperationFailed(…, nameof(LoadAsync))` call now uses `Logger` too); `CreateSpecialistAsync` wrapped — failure sets `CreateErrorMessage = Strings.Specialists_SaveError; HasCreateError = true; LogOperationFailed(Logger, nameof(CreateSpecialistAsync))` |

### A.2 Localization (4) — one new key `Common_ActionFailedMessage`

| File | Change |
|---|---|
| `Localization/Strings.cs` | `+ public static string Common_ActionFailedMessage => Get(nameof(Common_ActionFailedMessage));` (with doc comment) — placed next to `Common_ErrorDialogMessage` |
| `Localization/Strings.resx` (fa / invariant) | `+ <data name="Common_ActionFailedMessage"><value>انجام این عملیات ممکن نشد. لطفاً دوباره تلاش کنید.</value></data>` |
| `Localization/Strings.en.resx` | `+ <value>The action could not be completed. Please try again.</value>` |
| `Localization/Strings.ar.resx` | `+ <value>تعذّر إكمال العملية. يُرجى المحاولة مرة أخرى.</value>` |

### A.3 Shared test stubs (3) — additive `Exception?` seams (null-path byte-identical)

| Stub | Added |
|---|---|
| `tests/…/Customers/StubCustomerCommandService.cs` | `CreateCustomerException`, `UpdateCustomerException`, `AddNoteException`, `AddTagException`, `RemoveTagException` — each guarded with `Task.FromException` (calls still recorded first) |
| `tests/…/Services/StubServiceCommandService.cs` | `AssignSpecialistException`, `UnassignSpecialistException` |
| `tests/…/Specialists/StubSpecialistCommandService.cs` | `CreateSpecialistException`, `AddSkillException`, `RemoveSkillException` |

Same seam pattern as Wave 2C-2 (`StubAutomationServices` +16) / Wave 2C-3c (`StubSpecialistCommandService` +2). **No shared behaviour changed** — every hook defaults `null` → identical to before.

### A.4 Test ViewModel files (5) — **+13 tests, 0 existing bodies changed**

| File | Added |
|---|---|
| `Customers/CustomerPageViewModelTests.cs` | +1 `using`; **+2** (`CreateCustomerCommand_BackendThrows_…`, `CreateCustomerCommand_Succeeds_ClearsAnyPriorInlineCreateError`) |
| `Customers/CustomerProfileViewModelTests.cs` | +1 `using`; **+5** (`AddNoteCommand` / `AddTagCommand` / `RemoveTagCommand` / `SaveChangesCommand` backend-throws + `AddNoteCommand_Succeeds_ClearsAnyPriorInlineError`) |
| `Services/ServiceProfileViewModelTests.cs` | +1 `using`; **+2** (`AssignSpecialistCommand` / `UnassignSpecialistCommand` backend-throws) |
| `Specialists/SpecialistProfileViewModelTests.cs` | +1 `using`; **+2** (`AddSkillCommand` / `RemoveSkillCommand` backend-throws) |
| `Specialists/SpecialistPageViewModelTests.cs` | +1 `using`; **+2** (`CreateSpecialistCommand` backend-throws via factory + without-factory no-throw) |

### A.5 NOT touched

`AsyncRelayCommand`, `App.xaml.cs`, DI registration, Domain, backend contracts / DTOs / interfaces, RBAC, authentication, navigation, any `[LoggerMessage]` **signature** (logging track closed — reused as-is), the Load-boundary catches' `ErrorMessage = exception.Message` (pre-existing, separate P2), `ServicePageViewModel` (already guarded — precedent only), and every HR/Inventory/Accounting/Org/Reporting/AI/Automation/infra VM (Waves B–G).

---

## B. GUARD IMPLEMENTATION DETAILS

All 12 methods follow the established `ServicePageViewModel.CreateServiceAsync` / `ServiceProfileViewModel.SaveChangesAsync` shape:

```csharp
try
{
    await _commandService.<Command>(...).ConfigureAwait(true);
    <ErrorProp> = null;
    <HasErrorFlag> = false;
    <clear form input, if any>
    await LoadAsync().ConfigureAwait(true);
    <re-select created entity, if any>
}
#pragma warning disable CA1031 // Write/Create boundary: any failure must surface as a safe inline message and preserve the form, never crash or leak internal detail - same justified broad catch as Services.ServicePageViewModel/ServiceProfileViewModel's own write boundaries.
catch (Exception)
#pragma warning restore CA1031
{
    <revert edit buffer, SaveChangesAsync only>
    <ErrorProp> = <fixed localized string>;
    <HasErrorFlag> = true;
    LogOperationFailed(nameof(<Method>));   // reuse the VM's existing [LoggerMessage]
}
```

| Method | Error property | Localized string | Extra |
|---|---|---|---|
| `CustomerPageViewModel.CreateCustomerAsync` | new `CreateErrorMessage`/`HasCreateError` | `Common_ActionFailedMessage` | — |
| `CustomerProfileViewModel.AddNoteAsync` / `AddTagAsync` / `RemoveTagAsync` | new `SaveErrorMessage`/`HasSaveError` | `Common_ActionFailedMessage` | — |
| `CustomerProfileViewModel.SaveChangesAsync` | new `SaveErrorMessage`/`HasSaveError` | `Common_ActionFailedMessage` | reverts `EditableStatus = Customer.Status` in the catch |
| `ServiceProfileViewModel.AssignSpecialistAsync` / `UnassignSpecialistAsync` | **existing** `SaveErrorMessage`/`HasSaveError` | `Services_SaveError` | — |
| `SpecialistProfileViewModel.AddSkillAsync` / `RemoveSkillAsync` | **existing** `SaveErrorMessage`/`HasSaveError` | `Specialists_SaveError` | — |
| `SpecialistPageViewModel.CreateSpecialistAsync` | new `CreateErrorMessage`/`HasCreateError` | `Specialists_SaveError` | logs via `LogOperationFailed(Logger, nameof(...))` (static-form `[LoggerMessage]`) |

- **No new logger.** Every catch calls the ViewModel's **existing** `LogOperationFailed(...)` (instance-form for 4 VMs; static-form via the new `Logger` helper for `SpecialistPageViewModel`). Operation-name-only.
- **No duplicate logging.** Each catch logs **once**; the global `App.LogUnhandledException` no longer fires for these paths (the exception is now caught locally).
- **`#pragma warning disable CA1031`** with a boundary comment — the same justified-broad-catch convention every other write in this app uses. No other analyzer suppression.

---

## C. BEHAVIOUR PRESERVATION

| Preserved | How |
|---|---|
| Existing service calls | unchanged — same `await _commandService.<X>(...)`, same arguments, same order |
| Existing validation / `CanExecute` | untouched (`AddNoteCommand` still gated on `!IsNullOrWhiteSpace(NewNoteText)`, `CreateCustomerCommand` on `NewCustomerFullName`, etc.) |
| Existing state transitions | success path identical — form-field clears, `LoadAsync()` reload, `SelectedCustomer`/`SelectedSpecialist` re-selection all still happen (moved inside the `try`, before the `catch`) |
| Existing RBAC behaviour | unchanged — no permission gate touched; the backend remains the sole write authority |
| `CustomerProfileViewModel.SaveChangesAsync` edit-buffer rollback | **added, matching the `ServiceProfileViewModel` precedent** — on failure `EditableStatus` reverts to `Customer.Status` so a rejected status change is never left displayed as applied. (There was no rollback before because there was no catch.) |
| `ServiceProfileViewModel.SaveChangesAsync` / `DeactivateAsync` existing revert behaviour | untouched — those methods were already guarded (Wave 2C-3a), not in Wave A scope |
| `SpecialistProfileViewModel.SaveChangesAsync` existing revert behaviour | untouched — already guarded (Wave 2C-3c) |
| Global `DispatcherUnhandledException` handler | untouched — it simply no longer receives these 12 exceptions (they are caught locally now) |
| All pre-existing tests | pass unchanged (679/679 Presentation; the `Command_Executed_CallsCommandService…` success tests still green) |

**No business-behaviour change:** no service added/removed/reordered, no RBAC change, no `Domain.*Rules` change, no backend contract change.

---

## D. SECURITY REVIEW

| Concern | Result |
|---|---|
| Backend response bodies to the user | ✅ never — every catch sets a **fixed localized string** (`Common_ActionFailedMessage` / `Services_SaveError` / `Specialists_SaveError`), never `exception.Message` |
| `Exception.Message` to the user | ✅ never — `catch (Exception)` (no variable) in all 12; the exception object is not referenced |
| Backend bodies / identifiers to the **log** | ✅ never — reuses the operation-name-only `[LoggerMessage]` (`LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(Logger, nameof(...))`) — the Phase 8.61 harmonization already removed all exception/identifier payloads |
| PII (customer/specialist name, email, phone) | ✅ never referenced by a new catch or the new `Common_ActionFailedMessage` string |
| Internal identifiers (`_customerId`, `_serviceId`, `_specialistId`, tag/skill/assignment ids) | ✅ never logged or surfaced |
| New string content | `Common_ActionFailedMessage` = "The action could not be completed. Please try again." (en) — generic, no detail |

**Test-enforced:** the `…_LogsOperationOnly` tests seed a `"HTTP 500: backend response body / … PII secret"` string into the thrown exception and assert `Assert.DoesNotContain(backendBody, entry.Message)` + `Assert.Contains("<Method>Async", entry.Message)` (log line) while asserting the on-screen error equals the fixed `Strings.*` value.

---

## E. TESTS

### E.1 Added (13)

| # | Test | Asserts |
|---|---|---|
| 1 | `CustomerPageViewModelTests.CreateCustomerCommand_BackendThrows_…_LogsOperationOnly` | `Execute` does not throw; `HasCreateError` true, `CreateErrorMessage == Strings.Common_ActionFailedMessage`; form fields preserved; `State != Error`; log entry `Operation`-only, no backend body |
| 2 | `CustomerPageViewModelTests.CreateCustomerCommand_Succeeds_ClearsAnyPriorInlineCreateError` | after a failed then successful create → `HasCreateError` false, `CreateErrorMessage` null |
| 3 | `CustomerProfileViewModelTests.AddNoteCommand_BackendThrows_…_PreservesInput_LogsOperationOnly` | no throw; `HasSaveError` true, message = fixed string; `NewNoteText` preserved; `State == Loaded`; log operation-only, no body |
| 4 | `…AddTagCommand_BackendThrows_…` | no throw; `HasSaveError`; `NewTagText` preserved |
| 5 | `…RemoveTagCommand_BackendThrows_…` | no throw; `HasSaveError` |
| 6 | `…SaveChangesCommand_BackendThrows_…_RevertsEditableStatus` | no throw; `HasSaveError`; **`EditableStatus` reverts to `Active`**; `Customer.Status == Active` |
| 7 | `…AddNoteCommand_Succeeds_ClearsAnyPriorInlineError` | error clears on next success |
| 8 | `ServiceProfileViewModelTests.AssignSpecialistCommand_BackendThrows_…_PreservesInput_LogsOperationOnly` | no throw; `HasSaveError`, message = `Services_SaveError`; `NewSpecialistName` preserved; log operation-only |
| 9 | `…UnassignSpecialistCommand_BackendThrows_…` | no throw; `HasSaveError` |
| 10 | `SpecialistProfileViewModelTests.AddSkillCommand_BackendThrows_…_PreservesInput_LogsOperationOnly` | no throw; `HasSaveError`, message = `Specialists_SaveError`; `NewSkillText` preserved; log operation-only |
| 11 | `…RemoveSkillCommand_BackendThrows_…` | no throw; `HasSaveError` |
| 12 | `SpecialistPageViewModelTests.CreateSpecialistCommand_BackendThrows_…_LogsOperationOnly` | no throw; `HasCreateError`, message = `Specialists_SaveError`; form preserved; `State != Error`; `RecordingLoggerFactory` entry category `SpecialistPageViewModel`, operation-only, no body |
| 13 | `…CreateSpecialistCommand_WithoutLoggerFactory_BackendThrows_DoesNotThrow` | no factory → `NullLogger` path → still no throw, `HasCreateError` true |

Reuse `RecordingLogger<T>` / `RecordingLoggerFactory`. **No new test helper.**

### E.2 Fresh full run (working tree, uncommitted)

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **679** | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,622** | **0** | **0** |

Delta from baseline `5ba554c` (2,609): **+13** (Presentation.Tests 666 → 679).

---

## F. VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build  → 2,622 / 2,622 passing   0 failed   0 skipped
Architecture tests                → 7 / 7 passing
```

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,624 PASS | **2,622 / 2,622** | ✅ (+13; the "~2,624" estimate was an upper bound) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

New resx keys are present in all 3 locale files (`Strings.resx` / `en` / `ar`) so `Strings.Common_ActionFailedMessage` resolves in every culture — verified by the tests asserting equality against it.

---

## G. COMMIT READINESS

| Gate | Status |
|---|---|
| Scope = 5 Wave A VMs + 4 localization + 3 stubs + 5 test files | ✅ 17 files |
| `ServicePageViewModel` NOT modified (precedent only) | ✅ (not in `git status`) |
| No `AsyncRelayCommand` / `App.xaml.cs` / DI / Domain / backend contract / RBAC / auth / navigation / command-infrastructure change | ✅ |
| No new architecture pattern — reuses `ServicePageViewModel.CreateServiceAsync` / `ServiceProfileViewModel.SaveChangesAsync` shape | ✅ |
| Every catch reuses the VM's existing `[LoggerMessage]`, operation-name-only, once | ✅ |
| No `Exception.Message` / backend body / PII / identifier surfaced or logged | ✅ (test-enforced) |
| `CustomerProfileViewModel.SaveChanges` reverts `EditableStatus`; existing `ServiceProfileViewModel` / `SpecialistProfileViewModel` revert behaviour untouched | ✅ |
| Reload calls kept inside the guarded block (precedent) | ✅ |
| No shared-stub *behaviour* change (additive `Exception?` seams only); no existing test body changed | ✅ |
| Build 0/0 · Tests 2,622/2,622 · Architecture 7/7 | ✅ |

Working tree: **17 files** — `git status --porcelain`:
```
 M src/Rojan.Desktop.Presentation/Localization/Strings.ar.resx
 M src/Rojan.Desktop.Presentation/Localization/Strings.cs
 M src/Rojan.Desktop.Presentation/Localization/Strings.en.resx
 M src/Rojan.Desktop.Presentation/Localization/Strings.resx
 M src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Services/ServiceProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Customers/StubCustomerCommandService.cs
 M tests/Rojan.Desktop.Presentation.Tests/Services/ServiceProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Services/StubServiceCommandService.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/StubSpecialistCommandService.cs
```

Recommended commit subject (per Phase 8.65 §F): `fix(desktop): guard customer/service/specialist command failures`

---

## STOP

Implementation complete. Build 0/0, 2,622/2,622 tests, architecture 7/7. Working tree modified across
exactly 17 files (5 production VMs + 4 localization + 3 test stubs + 5 test files). No business-behaviour
change — 12 write commands now surface a failure via the app's in-page error pattern instead of the
generic global dialog. **Nothing committed, pushed, merged, rebased, or amended.** HEAD remains `5ba554c`.
Awaiting Phase 8.67 commit scope review.
