# ROJAN AI — TEAM 3 — PHASE 8.79 — MISSING-GUARD SWEEP WAVE D (ORGANIZATION) — COMMIT SCOPE REVIEW v1

**Type:** Pre-commit review. **STRICT MODE — no source change, no test change, no new file, no commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `66c849077ffc0168de75335ec904fb7a0f2d7bea`
**References:** `ROJAN_PHASE8_77_WAVE_D_ORGANIZATION_SCOPE_REVIEW_v1.md`, `ROJAN_PHASE8_78_WAVE_D_IMPLEMENTATION_REPORT_v1.md`
**Verdict:** ✅ **READY TO COMMIT** — scope clean, 2 files, 0 new, build 0/0, 2,666/2,666 tests, architecture 7/7. One cosmetic note (§B.4).

---

## A. GIT STATE

```
git rev-parse HEAD        → 66c849077ffc0168de75335ec904fb7a0f2d7bea
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty)   ← nothing staged
git log --oneline -3      → 66c8490 guard inventory and invoice-cancel / a5be831 guard HR / 794648e guard customer/service/specialist
```

| Check | Result |
|---|---|
| HEAD | `66c8490` (Wave C commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Staging area | **empty** ✅ |
| Modified tracked files | **2** ✅ |
| New tracked files | **0** ✅ |
| Untracked | only `ROJAN_*.md` reports ✅ |

```
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
 M src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
```

`git diff --stat`: **2 files changed, 521 insertions(+), 73 deletions(-)**.
- Production (`OrganizationPageViewModel.cs`): +201/−?  — the deletions are original single-line command bodies re-indented into their `try`-wrapped form (verified line-by-line) plus the two setter lines re-pointed at the new wrappers. No property / validation / service call / `LoadAsync` body removed.
- Test (`OrganizationPageViewModelTests.cs`): +393/−73 — the file's private nested test doubles were rewritten (2 upgraded + 1 new); the 2 pre-existing `LoadAsync` test **methods keep their names and every assertion** (only the query-stub they construct changed: `new ThrowingOrganizationQueryService(ex)` → `new StubOrganizationQueryService { GetOrganizationsException = ex }`).

Matches Phase 8.77 §F.3 estimate (1 prod + 1 test = 2) and Phase 8.78 report §A exactly.

---

## B. SCOPE VERIFICATION

### B.1 Production (1 file) — in scope

| Diff element | Verdict |
|---|---|
| `+ _actionErrorMessage` / `_hasActionError` fields (2) | ✅ additive |
| `+ ActionErrorMessage` / `HasActionError` properties (18, incl. doc comment) | ✅ additive, private-set |
| `SelectedOrganization` setter: `_ = LoadBranchesForSelectedOrganizationAsync()` → `_ = ReloadBranchesForSelectionAsync()` (1 line) | ✅ re-point to new guarded wrapper |
| `SelectedBranch` setter: `_ = LoadSettingsForSelectedBranchAsync()` → `_ = ReloadSettingsForSelectionAsync()` (1 line) | ✅ re-point |
| `+ ReloadBranchesForSelectionAsync` / `+ ReloadSettingsForSelectionAsync` (2 private wrappers) | ✅ additive |
| `CreateOrganizationAsync` / `CreateBranchAsync` / `SaveBranchSettingsAsync` / `SwitchRoleAsync` wrapped in `try { …existing body… } catch (Exception) { … }` with `#pragma warning disable/restore CA1031` | ✅ in scope |
| **`LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` bodies** | ✅ **not in the diff** — byte-unchanged |
| ctor / `[LoggerMessage]` signature / `PermissionMatrix` build / `SelectSectionCommand` / `RefreshCommand` | ✅ not in the diff |

### B.2 Test (1 file) — approved private nested doubles only

| Double | Change | In scope? |
|---|---|---|
| `NotSupportedOrganizationCommandService` → `StubOrganizationCommandService` (private nested) | recording stub (`CreateOrganizationCalls` / `CreateBranchCalls` / `SetBranchSettingsCalls`) + additive `Exception?` seams (`CreateOrganizationException` / `CreateBranchException` / `SetBranchSettingsException`); success returns a deterministic DTO; `Update*` echo the arg | ✅ approved (§F.1) |
| `ThrowingOrganizationQueryService` → `StubOrganizationQueryService` (private nested) | `Organizations` / `Branches` lists + `Exception?` seams (`GetOrganizationsException` / `GetBranchesException` / `GetBranchSettingsException`); `GetBranchesAsync` filters by `organizationId` | ✅ approved (§F.1) |
| **new** `StubCurrentSessionService` (private nested) | settable `CurrentRole` (updated on a successful switch) + `SwitchRoleException` seam + `SwitchRoleAttempts` recorder; other `ICurrentSessionService` members minimal | ✅ approved (§F.1) |
| `+12` `[Fact]` methods | ✅ | |
| 2 pre-existing `[Fact]` (`LoadAsync_QueryThrows_LogsError`, `NoLoggerSupplied_…`) | retained; assertions unchanged | ✅ |

### B.3 Confirmed UNTOUCHED

```
git diff --name-only  →  exactly 2 files, both under …/Organizations/
```

| Area | Status |
|---|---|
| RBAC infrastructure (`IPermissionGate` / `RolePermissions` / permission gates) | ✅ untouched |
| `IPermissionEngine` / `PermissionEngine` | ✅ untouched — `PermissionMatrix` still built from `_permissionEngine.GetPermissions` in the ctor, unchanged; test `PermissionMatrix_IsUnaffectedByCommandFailures` confirms |
| `ICurrentSessionService` **interface** + concrete Shell implementation | ✅ untouched |
| Authentication / session persistence | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| Navigation / back-stack / `Shell.MainWindowViewModel` (Branch Switcher) | ✅ untouched |
| Backend contracts / HTTP clients / `IOrganizationQueryService` / `IOrganizationCommandService` interfaces + DTOs | ✅ untouched |
| **Shared test doubles** — `FakeCurrentSessionService` (Automation), `RecordingLogger` helpers, every other stub | ✅ untouched |
| Other ViewModels (HR / Inventory / Accounting / Reporting / AI / Automation / …) | ✅ untouched |
| `Strings.cs` / `Strings.resx` / `Strings.en.resx` / `Strings.ar.resx` (`Common_ActionFailedMessage` already ships in Wave A `794648e`; `Organizations_SettingsSaved` unchanged) | ✅ untouched |
| `AsyncRelayCommand` / `RelayCommand` / `App.xaml.cs` | ✅ untouched |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

### B.4 Cosmetic note (non-blocking)

The rewritten `OrganizationPageViewModelTests.cs` was saved **UTF-8 without BOM / LF line endings**, whereas sibling test files are **UTF-8 with BOM / CRLF**. With the repo's `core.autocrlf=true` and no `.gitattributes`, `git add` normalizes the working copy to **LF in the committed blob** — consistent with how git stores every file in this repo's index — so *the commit content is line-ending-consistent with the rest of the tree*. The only residual difference is the absent BOM. Impact: **none** — `dotnet build` is 0 warnings / 0 errors, all analyzers pass, all 2,666 tests pass, no `.editorconfig` BOM rule is enforced here. Options for Phase 8.80: (a) accept as-is (recommended — zero functional impact), or (b) a one-keystroke BOM re-save before staging if strict byte-parity with siblings is desired. Not a correctness issue either way.

---

## C. COMMAND GUARD REVIEW — 4 commands

Every guard is the diff-confirmed shape: `<early-return validation + list building: UNCHANGED, outside try>` → `try { <original command await + original success body: UNCHANGED>; ActionErrorMessage = null; HasActionError = false; }` → `#pragma CA1031` + `catch (Exception)` (no variable) → `{ ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(nameof(<Method>)); }`.

| Command | Existing business logic | Validation | Success path | `ActionErrorMessage` |
|---|---|---|---|---|
| `CreateOrganizationAsync` | `await _commandService.CreateOrganizationAsync(name, legalName, taxInfo, subscription, code, phone, email, address)` — same call, same 8 args, same order | `CanExecute`: `NewOrgName` non-empty — unchanged (no early-return in the body) | 7 `NewOrg*` field clears + `await LoadAsync()` — unchanged, inside `try` | set **only** in the catch |
| `CreateBranchAsync` | `await _commandService.CreateBranchAsync(orgId, name, code, address, phone, email, manager, timeZone, currency)` — unchanged | `if (SelectedOrganization is null) return;` stays **outside** the `try`; `CanExecute` unchanged | 8 `NewBranch*` clears + `await LoadBranchesForSelectedOrganizationAsync()` — unchanged, inside `try` | catch only |
| `SaveBranchSettingsAsync` | `workingDays` list building **outside** the `try` (unchanged); `BranchSettingsDto` construction (incl. `TimeOnly.Parse`) + `CurrentBranchSettings = await _commandService.SetBranchSettingsAsync(settings)` — unchanged, inside `try` | `if (SelectedBranch is null) return;` stays **outside** the `try`; `CanExecute` unchanged | `StatusMessage = Strings.Organizations_SettingsSaved` — unchanged, **success only** | catch only |
| `SwitchRoleAsync` | `await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo)` — unchanged (see §D) | (no `CanExecute` predicate) | 3× `OnPropertyChanged` (`CurrentRole` / `CurrentOrganizationName` / `CurrentBranchName`) — unchanged, **success only** | catch only + role revert (§D) |

**`TimeOnly.Parse` placement:** the two `TimeOnly.Parse(SettingsOpenTime/CloseTime, CultureInfo.InvariantCulture)` calls moved **into** the `try` (they are part of the operation, not an early-return validation gate). Effect: a malformed time string now surfaces as `ActionErrorMessage` instead of crashing the page. Test-covered (`SaveBranchSettingsCommand_MalformedTime_…`, which also asserts `SetBranchSettingsCalls` stays empty — the throw is before the service call).

**Confirmed:** no business-rule computation added/removed/reordered; no `Domain.*` reference; no backend-contract change; the `await LoadAsync()` / `await LoadBranchesForSelectedOrganizationAsync()` reloads (self-guarded / propagate to `LoadAsync`'s own catch) stay inside the guarded block per Wave A–C precedent. `ActionErrorMessage` / `HasActionError` are the **only** new state, set **only** in the catch (and cleared on the success path).

---

## D. SESSION / ROLE REVIEW — `SwitchRoleAsync`

```csharp
try
{
    await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo).ConfigureAwait(true);   // UNCHANGED call
    ActionErrorMessage = null; HasActionError = false;
    OnPropertyChanged(nameof(CurrentRole));                                                       // UNCHANGED, success only
    OnPropertyChanged(nameof(CurrentOrganizationName));
    OnPropertyChanged(nameof(CurrentBranchName));
}
catch (Exception)
{
    SelectedRoleToSwitchTo = _currentSessionService.CurrentRole;   // revert the two-way-bound picker
    ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true;
    LogOperationFailed(nameof(SwitchRoleAsync));
}
```

| Confirm | Result |
|---|---|
| **Previous selected role restored on failure** | ✅ `SelectedRoleToSwitchTo = _currentSessionService.CurrentRole` in the catch — the picker snaps back to the session's actual (unchanged) role. Test `SwitchRoleCommand_Failure_DoesNotThrow_RevertsPickerAndLeavesSessionRoleUnchanged` asserts `sut.SelectedRoleToSwitchTo == PlatformOwner` after a failed switch to `Reception`. |
| **Session consistency preserved** | ✅ `ICurrentSessionService` is the sole persistence authority; on failure it throws **before** persisting, so `CurrentRole` is genuinely unchanged (test asserts `session.CurrentRole == PlatformOwner`). After the revert, `SelectedRoleToSwitchTo`, `sut.CurrentRole`, and `session.CurrentRole` all agree. |
| **No RBAC behavior change** | ✅ no permission gate / `IPermissionEngine` / `RolePermissions` referenced; `PermissionMatrix` untouched (test `PermissionMatrix_IsUnaffectedByCommandFailures`). |
| **No permission mutation** | ✅ the catch assigns a `WorkspaceRole` enum to a bound property; it grants/revokes nothing. The 3 `OnPropertyChanged` notifications fire on success only, so a failed switch does not signal a role change to any subscriber. |
| **Existing role-switch logic** | ✅ `await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo)` — same method, same argument; the attempt is still made on every invoke (test asserts `session.SwitchRoleAttempts == [Reception]`). |

`SwitchRoleCommand_Success_SwitchesRoleAndClearsError` confirms a subsequent successful switch clears `HasActionError` and lands both the session and the picker on the new role.

---

## E. SECONDARY LOAD REVIEW

| Confirm | Result |
|---|---|
| **Only the fire-and-forget secondary paths are guarded** | ✅ the `SelectedOrganization` setter now fires `_ = ReloadBranchesForSelectionAsync()`, the `SelectedBranch` setter `_ = ReloadSettingsForSelectionAsync()`. Each wrapper is `try { await <original>(); ActionErrorMessage = null; HasActionError = false; } catch (Exception) { ActionErrorMessage = …Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(nameof(<original>)); }`. |
| **`LoadAsync` unchanged** | ✅ not in the diff. `LoadAsync` still `await`s `LoadBranchesForSelectedOrganizationAsync()` **directly** (line ~411); `LoadBranchesForSelectedOrganizationAsync` still `await`s `LoadSettingsForSelectedBranchAsync()` **directly** (line ~459). So an initial-load branch/settings failure still propagates into `LoadAsync`'s existing top-level catch → `State = Error` (no regression). Only a **user-selection-triggered** failure — previously an unobserved task exception → global dialog — now surfaces as a non-destructive inline `ActionErrorMessage`. |
| Method bodies of the two secondary loads | ✅ byte-unchanged (not in the diff) |

Tests `SelectingAnotherOrganization_BranchLoadFails_…` and `SelectingAnotherBranch_SettingsLoadFails_…` exercise the setter path (2-org / 2-branch fixtures, exception seam set **after** construction so the initial load is clean) and assert no throw, `HasActionError`, and `State != DashboardState.Error`.

---

## F. SECURITY REVIEW

| Vector | Finding |
|---|---|
| Organization legal name / tax information | **not exposed** — `catch (Exception)` binds **no variable** in any of the 6 guards; `ActionErrorMessage` is only ever `null` or the compile-time constant `Strings.Common_ActionFailedMessage`; never reads an org/DTO field |
| VAT % / receipt header/footer text | **not exposed** — same; `SaveBranchSettingsAsync`'s catch touches no `Settings*` value |
| Branch contact data (address / phone / email / manager) | **not exposed** — same; `CreateBranchAsync`'s catch reads nothing |
| Roles / permissions | **not exposed** — `SelectedRoleToSwitchTo` / `_currentSessionService.CurrentRole` are `WorkspaceRole` enum values; **not logged** (operation name only); the revert assigns an enum and surfaces nothing; `PermissionMatrix` / `IPermissionEngine` untouched |
| `Exception.Message` → UI | **not exposed** (no exception variable; constant string only) |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has **no `Exception` parameter**; `LocalFileLoggerProvider` renders no backend body |
| Backend response payload | **not exposed** on either surface |
| Internal identifiers (`SelectedOrganization.Id`, `SelectedBranch.Id`) | **not logged** (operation name only), **not shown** (generic string only) |

**Logger receives only:** `Operation=<MethodName>` via the template `"Organization page operation failed. Operation={Operation}"` — `CreateOrganizationAsync` / `CreateBranchAsync` / `SaveBranchSettingsAsync` / `SwitchRoleAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync`.

**Test-enforced:** `CreateOrganizationCommand_Failure_LogsOperationNameOnly_NoOrgOrTaxLeak` and `SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak` seed `OrgBackendSecret = "backend 500: org 'Rojan Holdings LLC' tax=IR-9982 VAT=9% role=PlatformOwner"` and assert `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`, plus `Assert.Contains(… "Operation=<Method>" …)`.

---

## G. LOGGING REVIEW

| Check | Result |
|---|---|
| Existing `[LoggerMessage]` reused | ✅ `OrganizationPageViewModel.LogOperationFailed(string operation)` — pre-existing instance-form (Phase 8.27 `cbc3a82`), unchanged signature. Only new **call sites** added (4 commands + 2 wrappers = 6). |
| No new logger field / type | ✅ — the class keeps its single `ILogger<OrganizationPageViewModel> _logger`; no addition |
| No DI / constructor change | ✅ |
| No `SYSLIB1020` | ✅ — single `ILogger` field + instance-form `[LoggerMessage]` (compiled clean at `66c8490` and every prior wave); `dotnet build -c Debug` → **0 warnings** |
| No `CA1848` (raw `_logger.Log*`) | ✅ — no raw logger call added |
| No duplicate logging | ✅ — each guarded path logs **once** in its catch. `LoadAsync` (awaited from `CreateOrganizationAsync` / `CreateBranchAsync` on the success path) has its own separate catch; a command-then-failed-reload cannot double-log into the command catch (reload is self-guarded). The secondary-load wrappers log `nameof(LoadBranchesForSelectedOrganizationAsync)` / `nameof(LoadSettingsForSelectedBranchAsync)` — distinct operation names, no overlap with the command logs. |
| `CA1031` | ✅ — suppressed locally with the documented `#pragma warning disable/restore CA1031` boundary comment, identical convention to the pre-existing `LoadAsync` catch and Waves A–C |

---

## H. TEST REVIEW

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all 6 projects Passed
```

| Project | Passed | Failed | Skipped | Δ vs `66c8490` |
|---|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 | — |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 | — |
| Rojan.Desktop.Presentation.Tests | **723** | 0 | 0 | **+12** |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 | — |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 | — |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 | — |
| **TOTAL** | **2,666** | **0** | **0** | **+12** |

| Expected (Phase 8.79) | Actual | Status |
|---|---|---|
| Tests 2,666 / 2,666 PASS | 2,666 / 2,666 | ✅ |
| Build 0 / 0 | 0 / 0 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

**+12 tests reviewed:**

| Aspect | Coverage |
|---|---|
| **Failure handling** | `CreateOrganization` / `CreateBranch` / `SaveBranchSettings` / `SwitchRole` + both secondary-load setter paths — `Record.Exception(...)` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`; `State != Error`; form fields preserved; `SaveBranchSettings` also asserts `StatusMessage != Organizations_SettingsSaved` and `CurrentBranchSettings == null`; malformed-time path asserts `SetBranchSettingsCalls` empty |
| **Role rollback** | `SwitchRoleCommand_Failure_…RevertsPickerAndLeavesSessionRoleUnchanged` — session role unchanged, picker reverted, attempt still made (§D) |
| **Session safety** | same test + `SwitchRoleCommand_Success_SwitchesRoleAndClearsError` (session + picker land on the new role after a retry) |
| **No sensitive leakage** | `CreateOrganizationCommand_Failure_LogsOperationNameOnly_NoOrgOrTaxLeak`, `SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak` — `Operation=<Method>` present; sentinel absent from `logger.Entries` **and** `ActionErrorMessage` |
| **Success clears error** | `CreateOrganizationCommand_SuccessAfterFailure_ClearsActionError` |
| **RBAC untouched** | `PermissionMatrix_IsUnaffectedByCommandFailures` |
| **Regression** | 2 pre-existing `LoadAsync` tests pass unchanged (assertions intact; only the query-stub constructor swapped) |

---

## I. COMMIT READINESS

✅ **Ready.** No blockers (§B.4 is cosmetic).

**Staging plan (Phase 8.80 — explicit paths only, no `git add .` / `-A`):**

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
git diff --cached --name-only        # expect exactly 2
```

**Commit message (EXACT):**

```
fix(desktop): guard organization command failures

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

**Post-commit validation to run:** `dotnet build -c Debug` (expect 0/0) · full `dotnet test` (expect 2,666/2,666) · architecture (expect 7/7) · `git log --oneline -3`.

**Checkpoint update (Phase 8.80):** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` — new HEAD; §A banner + audit-phase list; §B commit table + Phase 8.78 detail bullet; §E build/test 2,654 → 2,666 (Presentation 711 → 723); §G Missing-Guard Sweep track — Wave D/Organization ✅ / **Reporting mini-wave NEXT** (then Wave E); §H items 1/2/5/6.

---

## STOP

Phase 8.79 commit scope review complete. **2 modified files, 0 new**, both under `…/Organizations/`. All 4 command guards + the 2 secondary-load wrapper paths preserve validation / `CanExecute` / service calls / success paths / `LoadAsync` body; `SwitchRoleAsync` reverts the role picker to the session's actual (unchanged) role on failure with no RBAC or permission mutation. No `Exception.Message` / org / tax / VAT / receipt / branch-contact / role / permission exposure — UI gets only `Common_ActionFailedMessage`, logging only `Operation=nameof(Method)` via the existing instance-form `[LoggerMessage]`. No new logger, no DI change, no `SYSLIB1020`, no duplicate logging. No RBAC / `IPermissionEngine` / `ICurrentSessionService` / DI / shared-test-double change. Build 0/0, **2,666/2,666** tests, architecture 7/7.
**Next: Phase 8.80 — Wave D (Organization) Commit Execution.** Awaiting authorization.
