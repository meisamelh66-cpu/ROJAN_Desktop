# ROJAN AI — TEAM 3 — PHASE 8.27 ORGANIZATION PAGE LOGGING — IMPLEMENTATION REPORT v1

**Type:** Implementation complete. Build + full test suite + architecture tests all green.
**No commit performed** — per the authorization's STOP condition ("WAIT FOR SCOPE REVIEW"). `HEAD` is
still `2ed685a`.

**Branch:** `feature/team3-desktop-completion`
**Authorization:** `PHASE 8.27 — ORGANIZATION LOGGING — IMPLEMENTATION v1`
**Scope reference:** `ROJAN_PHASE8_26_ORGANIZATION_LOGGING_SCOPE_AUDIT_v1.md`

---

## A. Files Changed

Exactly 2 — 1 production (modified) + 1 test (**new**), both on the authorization's allow-list. **No DI,
no interface, no shared stub, no other file.**

| File | Status | +/− | Change |
|---|---|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs` | modified | +12 / −2 | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<OrganizationPageViewModel> _logger` field; ctor +5th optional param `ILogger<OrganizationPageViewModel>? logger = null` + `NullLogger` fallback; +1 `[LoggerMessage(Level = Error)]` partial; +1 call in the `LoadAsync` catch |
| `tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs` | **new** | +88 | 2 tests + 2 private nested stubs (`ThrowingOrganizationQueryService`, `NotSupportedOrganizationCommandService`) + a `CreateSut` helper |

`git diff --stat` (tracked): `1 file changed, 12 insertions(+), 2 deletions(-)` — plus the untracked new
test directory `tests/Rojan.Desktop.Presentation.Tests/Organizations/`.

**Confirmed NOT touched:** DI registration (`ServiceCollectionExtensions.cs`), any interface, Domain,
backend contracts, RBAC, Authentication, Navigation, any shared stub file. `OrganizationPageViewModel`'s
command methods, branch/settings loaders, and permission-grid construction are all unchanged.

---

## B. Logging Implementation

### B.1 Applied shape (the established ROJAN standard)

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed partial class OrganizationPageViewModel : ViewModelBase
{
    private readonly ILogger<OrganizationPageViewModel> _logger;

    public OrganizationPageViewModel(
        IOrganizationQueryService queryService,
        IOrganizationCommandService commandService,
        IPermissionEngine permissionEngine,
        Rojan.Desktop.Presentation.Organizations.ICurrentSessionService currentSessionService,
        ILogger<OrganizationPageViewModel>? logger = null)   // 5th, optional, appended last
    {
        // existing assignments unchanged
        _logger = logger ?? NullLogger<OrganizationPageViewModel>.Instance;
    }

    // LoadAsync catch — AFTER the two unchanged lines:
    //     ErrorMessage = exception.Message;
    //     State = DashboardState.Error;
    //     LogOperationFailed(nameof(LoadAsync));   // added

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Organization page operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
```

- **1 log call site** — the `LoadAsync` broad-catch boundary (the only swallowing `catch (Exception)` in
  the file).
- **Level `Error`**, `[LoggerMessage]` source-gen (CA1848), instance form (one logger field → no
  `SYSLIB1020`).
- **`LogOperationFailed(string operation)`** — signature exactly as the authorization specified; called
  with `nameof(LoadAsync)`.
- **The `Exception` is NOT passed to the logger.**

### B.2 Produced log line

```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Organizations.OrganizationPageViewModel: Organization page operation failed. Operation=LoadAsync
```

---

## C. Security Review

| Prohibited item (per authorization) | In the log line? | Why not |
|---|---|---|
| **Exception object** | **No** | `LogOperationFailed(string operation)` has no `Exception` parameter |
| **`Exception.Message`** | **No** | the call passes `nameof(LoadAsync)` only |
| **Organization name** (`OrganizationDto.Name` / `LegalName` / code / phone / email / address) | **No** | not referenced by any log call; the exception (which could carry a mapped response) is never passed |
| **VAT information** (`BranchSettingsDto.VatPercentage`) | **No** | same |
| **Tax data** (`OrganizationDto.TaxInformation`) | **No** | same |
| **Receipt text** (`ReceiptSettingsDto.HeaderText` / `FooterText`) | **No** | same |
| **Backend response** | **No** | only carried by `Exception.Message`, never passed |
| Employee / customer data | **No** | this page has neither |

`IPermissionEngine` (role→permission grid) is used only in the constructor, far from the catch, and is
never referenced by any log call. **Only the operation name is logged.** ✅

### C.1 Behaviour preservation

The `LoadAsync` catch keeps its exact `catch (Exception exception)` filter, its
`#pragma warning disable CA1031`, and both existing lines:
```csharp
ErrorMessage = exception.Message;   // unchanged
State = DashboardState.Error;        // unchanged
```
The log call is appended **after** them. No catch removed, no rethrow, no user-facing string changed.
Command methods (`CreateOrganizationAsync` etc.), the branch/settings loaders, `SwitchRoleAsync`, and the
permission-reference grid are all untouched.

---

## D. Tests Added

**+2 tests** (Presentation.Tests: 605 → 607). All green. New file:
`tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs`.

| Test | Setup | Assertion |
|---|---|---|
| `LoadAsync_QueryThrows_LogsError` | `ThrowingOrganizationQueryService` (`GetOrganizationsAsync` → `Task.FromException(new InvalidOperationException("boom"))`); `RecordingLogger<OrganizationPageViewModel>` | `State == DashboardState.Error`, `ErrorMessage == "boom"` (**unchanged**) **and** `logger.Entries` contains a `LogLevel.Error` entry with `Message` containing `"LoadAsync"` |
| `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | same throwing query, **no logger passed** | `Record.Exception(() => new OrganizationPageViewModel(...))` is `null` |

### D.1 Test infrastructure

| Item | Source |
|---|---|
| `RecordingLogger<T>` | existing — `tests/.../Specialists/RecordingLogger.cs`, via `using Rojan.Desktop.Presentation.Tests.Specialists;` |
| `FakeCurrentSessionService` | existing — `Rojan.Desktop.Presentation.Tests.Automation` (`internal`), reused via `using`. **No new session stub.** |
| `PermissionEngine` | real — `new PermissionEngine()` from `Rojan.Desktop.Application.Organizations` (as `NavigationServiceTests` does) |
| `ThrowingOrganizationQueryService` | **new — private nested class** in the test file (all 5 `IOrganizationQueryService` members; only `GetOrganizationsAsync` throws) |
| `NotSupportedOrganizationCommandService` | **new — private nested class** in the test file (all 5 `IOrganizationCommandService` members throw `NotSupportedException` — no command is invoked on the load path) |

**No shared stub was modified.** Both new stubs are private nested classes inside the one new test file,
matching the Wave 2B `ThrowingKpiEngineQueryService` precedent.

---

## E. Validation Results

### E.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### E.2 Full test suite

```
dotnet test --no-build
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **607** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,550** | **0** | **0** |

- Baseline at `2ed685a`: **2,548**. Now **2,550** = 2,548 + **2 new**. No pre-existing test changed
  result.

### E.3 Architecture tests

**7 / 7 passing** — unchanged. `Microsoft.Extensions.Logging.Abstractions` not forbidden; no
`System.Windows.Threading`/`Controls` added.

### E.4 Expected vs actual

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,550 / 2,550, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## F. Commit Readiness

**Ready. Not committed — awaiting the Phase 8.28 commit scope review + Phase 8.29 commit execution.**

- **Working tree:** 1 production file modified; 1 new test file untracked (dir
  `tests/Rojan.Desktop.Presentation.Tests/Organizations/`). Everything else untracked = `.md` reports.
- **Proposed staging (explicit paths only — never `git add -A` / `git add .`):**
  ```
  src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
  tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
  ```
- **Proposed commit message:**
  ```
  fix(desktop): add ViewModel diagnostic logging (organization page)

  Add ILogger<T> to OrganizationPageViewModel so its LoadAsync broad-catch
  boundary logs the failure at Error before surfacing the existing Error
  state. Operation name only - the exception is not passed to the logger
  (the page loads org/tax/VAT/receipt data). Follows the established
  optional-ctor-param + NullLogger<T> + [LoggerMessage] pattern; no DI,
  interface, or behaviour change.

  Adds a new OrganizationPageViewModelTests.cs (2 tests: failure-logs-Error
  + NullLogger safety) with private nested Organization service stubs -
  no dedicated test file existed before.
  ```
- **Downstream impact:** none on Authentication, Booking, Calendar authority, Shift Engine, RBAC, or
  Navigation.
- **Checkpoint update owed after commit:** §B (new commit + detail), §E (test count 2,548 → 2,550;
  self-logging coverage 17 → 18 of 56), §F (Wave 2B-2 resolved; Wave 2C next), §G.
- **Deferred:** the uncaught write/loader methods (`CreateOrganizationAsync` etc. — a *missing-guard*
  concern, separate error-handling phase); Wave 2C.

---

## STOP

Implementation and validation complete. **No commit performed.** Awaiting the scope review.
