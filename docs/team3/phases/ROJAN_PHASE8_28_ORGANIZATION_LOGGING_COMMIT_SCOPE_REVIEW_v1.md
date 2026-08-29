# ROJAN AI — TEAM 3 — PHASE 8.28 ORGANIZATION PAGE LOGGING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No commit, no push, no source change.**
**Mode:** READINESS ONLY — confirms the exact diff, security safety, and staging list before Phase 8.29
(commit execution).

**Branch:** `feature/team3-desktop-completion`
**HEAD:** `2ed685a` (`git rev-parse HEAD` this turn — unchanged, no drift)
**Predecessors:** `ROJAN_PHASE8_26_ORGANIZATION_LOGGING_SCOPE_AUDIT_v1.md` (audit),
`ROJAN_PHASE8_27_ORGANIZATION_LOGGING_IMPLEMENTATION_REPORT_v1.md` (impl).

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `2ed685ac73636e07a828d8b55dd1a5221dc09657` |
| Branch | `feature/team3-desktop-completion` |
| Staged files | **none** (`git diff --cached` empty) |
| Modified tracked files | **1** — `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs` |
| New (untracked) code | **1** — `tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs` (new directory `Organizations/`) |
| Deleted / renamed | none |
| Other untracked | `.md` reports only |

```
git status --porcelain (non-report):
 M src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
?? tests/Rojan.Desktop.Presentation.Tests/Organizations/
```

`git diff --stat` (tracked): `1 file changed, 12 insertions(+), 2 deletions(-)`

**Confirmed: no unrelated tracked changes.** The only modified tracked file and the only new code file
are both on the Phase 8.27 authorization's allow-list.

---

## B. Diff Scope Review (Task 2)

### B.1 Production — `OrganizationPageViewModel.cs` (+12 / −2)

| Hunk | Change | Assessment |
|---|---|---|
| usings | +`Microsoft.Extensions.Logging`, +`Microsoft.Extensions.Logging.Abstractions` | additive; `Abstractions` already a Presentation `PackageReference` |
| class decl | `sealed`→`sealed partial` | required for the `[LoggerMessage]` source generator |
| field | +`private readonly ILogger<OrganizationPageViewModel> _logger;` | one logger field |
| ctor | +5th parameter `ILogger<OrganizationPageViewModel>? logger = null` (optional, appended last); +`_logger = logger ?? NullLogger<OrganizationPageViewModel>.Instance;` | non-breaking — the existing 4-arg positional call sites (DI + `NavigationServiceTests`) still compile |
| `LoadAsync` catch | +`LogOperationFailed(nameof(LoadAsync));` **after** the unchanged `ErrorMessage = exception.Message;` / `State = DashboardState.Error;` | additive; catch filter, `#pragma`, and both state lines unchanged |
| new method | `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Organization page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` + a 2-line security comment | signature takes **only `string operation`** — no `Exception` |

**Only the `LoadAsync` boundary is instrumented.** The uncaught write/loader methods
(`CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync`, `SwitchRoleAsync`,
`LoadBranchesForSelectedOrganizationAsync`, `LoadSettingsForSelectedBranchAsync`) are **not in the diff**
— correctly out of scope (a *missing-guard* concern, not a swallowing `catch (Exception)`).

### B.2 Test — `OrganizationPageViewModelTests.cs` (new, +88)

- New file, new directory `tests/Rojan.Desktop.Presentation.Tests/Organizations/`.
- 6 `using`s; namespace `Rojan.Desktop.Presentation.Tests.Organizations`.
- 1 private `CreateSut(...)` helper; **2 `[Fact]` tests**; **2 private nested stub classes**
  (`ThrowingOrganizationQueryService`, `NotSupportedOrganizationCommandService`).
- Reuses `RecordingLogger<T>`, `Rojan.Desktop.Presentation.Tests.Automation.FakeCurrentSessionService`,
  and the real `PermissionEngine` via `using` — **no shared/production stub is referenced for
  modification, and none is modified.**

### B.3 Confirmed NOT changed (Task 2)

| Area | Evidence |
|---|---|
| **DI** | `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` — not in the diff. `OrganizationPageViewModel` stays `AddTransient`; `AddLogging()` fills the new optional param |
| **Interfaces** | no `I*.cs` in the diff — `IOrganizationQueryService`, `IOrganizationCommandService`, `IPermissionEngine`, `ICurrentSessionService` all untouched |
| **Domain** | no `Rojan.Desktop.Domain` file in the diff |
| **Backend contracts** | none touched |
| **RBAC** | no permission gate / `RolePermissions` file touched; `IPermissionEngine` unchanged (used only in the ctor's permission-grid build, far from the change) |
| **Authentication** | no auth file touched |
| **Navigation** | no `NavigationService` / `INavigationService` file touched (`NavigationServiceTests` still compiles against the 5-param ctor via the optional default) |
| **Shared production stubs** | **none touched.** `RecordingLogger.cs`, `StubAutomationServices.cs` (which holds `FakeCurrentSessionService`), and every other test double are unmodified — referenced via `using` only. The two new Organization stubs are **private nested classes** inside the new test file |

---

## C. Security Validation (Task 3)

### C.1 Pattern

| Check | Confirmed in diff |
|---|---|
| `ILogger<OrganizationPageViewModel>` | instance field, constructor-injected via the optional 5th param |
| `NullLogger<T>.Instance` | `_logger = logger ?? NullLogger<OrganizationPageViewModel>.Instance;` — proven by `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` |
| `[LoggerMessage(Level = Error)]` | `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Organization page operation failed. Operation={Operation}")]`; source-generated partial (CA1848); instance form (one logger field → no `SYSLIB1020`) |

### C.2 No sensitive logging — verified line-by-line

The produced log line is always exactly:
```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Organizations.OrganizationPageViewModel: Organization page operation failed. Operation=LoadAsync
```

| Prohibited item | In the log line? | Why not |
|---|---|---|
| **Exception object** | **No** | `LogOperationFailed(string operation)` has no `Exception` parameter |
| **`Exception.Message`** | **No** | the single call passes `nameof(LoadAsync)` only |
| **Organization name** (name / legal name / code / phone / email / address) | **No** | not referenced by any log call; the exception is never passed |
| **Tax information** (`OrganizationDto.TaxInformation`) | **No** | same |
| **VAT** (`BranchSettingsDto.VatPercentage`) | **No** | same |
| **Receipt text** (`ReceiptSettingsDto.HeaderText` / `FooterText`) | **No** | same |
| **Backend response** | **No** | only carried by `Exception.Message`, never passed |

**Operation-name-only logging confirmed.** ✅

---

## D. Behaviour Review (Task 4)

| Signal | Confirmed unchanged (per diff) |
|---|---|
| `ErrorMessage` | `LoadAsync` catch: `ErrorMessage = exception.Message;` line untouched; log appended after |
| `State` | `LoadAsync` catch: `State = DashboardState.Error;` line untouched (and `State = Loading` / `Empty` / `Loaded` in the try body are not in the diff) |
| Permission checks | `_permissionEngine.GetPermissions(role)` in the ctor's `PermissionMatrix` build — not in the diff |
| Command methods | `CreateOrganizationAsync` / `CreateBranchAsync` / `SaveBranchSettingsAsync` / `SwitchRoleAsync` — not in the diff |
| Existing catch behaviour | `catch (Exception exception)` filter + `#pragma warning disable CA1031` — unchanged; no catch removed, no rethrow |
| `StatusMessage` (on successful settings save) | not in the diff |

**Only logging is appended.** ✅

---

## E. Test Validation (Task 5)

### E.1 Fresh re-run this turn (HEAD `2ed685a` + working tree)

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded. 0 Warning(s), 0 Error(s).** |
| Full test suite | **2,550 / 2,550 passing, 0 failed, 0 skipped** (Domain 456, Presentation **607**, Application 791, Infrastructure 609, Shell 80, Architecture 7) |
| `OrganizationPageViewModelTests` (filtered) | **2 / 2 passing** |
| Architecture tests | **7 / 7 passing** |
| Delta vs `2ed685a` baseline (2,548) | **+2** — the 2 new tests; no pre-existing test changed result |

### E.2 New-test coverage

| Requirement | Test | ✓ |
|---|---|---|
| `LoadAsync` failure logs Error | `LoadAsync_QueryThrows_LogsError` — asserts a `LogLevel.Error` entry whose `Message` contains `"LoadAsync"` | ✅ |
| `State` / `ErrorMessage` preserved | same test — also asserts `State == DashboardState.Error` and `ErrorMessage == "boom"` (the pre-existing behaviour) | ✅ |
| NullLogger safety | `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` — `Record.Exception(() => CreateSut(queryService))` is `null` | ✅ |

### E.3 Stub review

| Stub | Kind | Members | Shared file touched? |
|---|---|---|---|
| `ThrowingOrganizationQueryService` | `private sealed` nested class in the test file | all 5 `IOrganizationQueryService` members — only `GetOrganizationsAsync` throws; the rest return empty/null (never reached on the failure path) | **No** |
| `NotSupportedOrganizationCommandService` | `private sealed` nested class in the test file | all 5 `IOrganizationCommandService` members throw `NotSupportedException` — no command is invoked on the load path | **No** |
| `FakeCurrentSessionService` | **existing** — `Rojan.Desktop.Presentation.Tests.Automation` (`internal`) | reused via `using` | **No — unmodified** |
| `RecordingLogger<T>` | **existing** — `tests/.../Specialists/RecordingLogger.cs` | reused via `using` | **No — unmodified** |
| `PermissionEngine` | **real** — `Rojan.Desktop.Application.Organizations` | `new PermissionEngine()` | n/a |

**Private nested stubs only. No shared stub modification.** ✅

---

## F. Commit Plan (Task 6)

### F.1 Staging (explicit paths only — never `git add -A` / `git add .`)

```
git add src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
```

Both files are single-concern (Organization page diagnostic logging + its first dedicated test file).
The `.md` reports stay untracked.

### F.2 Commit message (single isolated commit)

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

### F.3 Post-commit follow-up (Phase 8.29)

1. Fresh validation on the new HEAD (build + full suite + architecture tests).
2. Update `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`: §B (new commit + detail), §E (test count
   2,548 → 2,550; self-logging coverage 17 → 18 of 56), §F (Organization row resolved; Wave 2C next),
   §G.

### F.4 Explicitly deferred

- The uncaught Organization write/loader methods (`CreateOrganizationAsync` etc.) — a *missing-guard*
  concern for a separate error-handling phase.
- Wave 2C — 2C-1 (`Support`/`AcceptInvite`, the latter needs an auth-adjacent data-safety review),
  2C-2 (Automation tabs + parent plumbing), 2C-3 (detail/profile VMs + `BookingWizardViewModel`).
- Shared-stub throw hooks; `AuthBootstrapHttpClient` logging.

---

## G. Readiness Verdict

**READY TO COMMIT.**

- Diff is complete, minimal (1 production file +12/−2, 1 new test file +88), single-concern, matches the
  Phase 8.27 authorization exactly.
- Build clean, 2,550/2,550 tests green (Organization's 2 verified in isolation), architecture 7/7 —
  re-verified this turn.
- No change to DI, interfaces, Domain, backend contracts, RBAC, Authentication, Navigation, or shared
  production stubs. `OrganizationPageViewModel`'s command methods and permission-grid build are untouched.
- No sensitive value in the log path — the exception is never passed; the template carries only a
  `nameof` operation name.
- Existing `State` / `ErrorMessage` behaviour verified unchanged; test-enforced.
- New tests cover failure-logs-Error, state preservation, and NullLogger safety; both new stubs are
  private nested classes.
- Staging list and commit message specified above, ready for Phase 8.29.

---

## STOP

Commit scope review complete. No commit performed. Awaiting Phase 8.29 (commit execution) authorization.
