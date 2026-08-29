# ROJAN AI — TEAM 3 — PHASE 8.78 — MISSING-GUARD SWEEP WAVE D (ORGANIZATION) — IMPLEMENTATION REPORT v1

**Type:** Implementation. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `66c8490`
**Reference:** `ROJAN_PHASE8_77_WAVE_D_ORGANIZATION_SCOPE_REVIEW_v1.md`
**Result:** Build **0 / 0** · Full suite **2,666 / 2,666 PASS** · Architecture **7 / 7 PASS**

---

## A. FILES CHANGED

`git diff --stat` — **2 files, 521 insertions(+), 73 deletions(-)**. No new file.

| Group | File | Change |
|---|---|---|
| **Production (1)** | `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs` | `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; `SelectedOrganization` / `SelectedBranch` setters re-pointed at 2 new guarded wrappers; `+ ReloadBranchesForSelectionAsync` / `ReloadSettingsForSelectionAsync`; `CreateOrganizationAsync` / `CreateBranchAsync` / `SaveBranchSettingsAsync` / `SwitchRoleAsync` wrapped in `try`/`catch` |
| **Test (1)** | `tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs` | rewritten test doubles (2 upgraded + 1 new, all **private nested**); **+12 tests**; the 2 pre-existing `LoadAsync` tests retained (retargeted to the new query stub) |

**Not touched:** `Strings.cs` / all `.resx` (`Common_ActionFailedMessage` already ships from Wave A `794648e`); `ICurrentSessionService` interface + concrete impl; `IPermissionEngine` / `PermissionEngine` / `PermissionMatrix`; `IOrganizationQueryService` / `IOrganizationCommandService` interfaces + DTOs; DI; the ViewModel constructor; `Shell.MainWindowViewModel`; `AsyncRelayCommand`; `App.xaml.cs`; navigation; RBAC infrastructure; authentication; the `[LoggerMessage]` signature; `LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` **bodies**; **`FakeCurrentSessionService`** and every other shared test double.

The `[LoggerMessage]` used is `OrganizationPageViewModel`'s pre-existing instance-form `LogOperationFailed(string operation)` (Phase 8.27); the class keeps its **single** `ILogger` field → no `SYSLIB1020`.

---

## B. ORGANIZATION GUARDS

### B.1 One additive property pair

```csharp
public string? ActionErrorMessage { get; private set; }   // non-destructive: never touches State/ErrorMessage
public bool    HasActionError      { get; private set; }
```

Private-set, `SetProperty`, additive — **no constructor / DI change**. Same shape as `HrPageViewModel` / `InventoryPageViewModel.ActionErrorMessage` (Waves B/C).

### B.2 The 6 guarded paths

| # | Member | Guard shape | Success-path preserved (inside `try`) | Log call |
|---|---|---|---|---|
| 1 | `CreateOrganizationAsync` | `try { cmd; clear; ActionError=null; await LoadAsync() } catch (Exception) { ActionError; Has=true; log }` | 7 `NewOrg*` field clears + `await LoadAsync()` | `LogOperationFailed(nameof(CreateOrganizationAsync))` |
| 2 | `CreateBranchAsync` | as above (after the unchanged `if (SelectedOrganization is null) return;`) | 8 `NewBranch*` field clears + `await LoadBranchesForSelectedOrganizationAsync()` | `nameof(CreateBranchAsync)` |
| 3 | `SaveBranchSettingsAsync` | `try` wraps from the `BranchSettingsDto` construction (incl. the two `TimeOnly.Parse` calls) through `SetBranchSettingsAsync` + `StatusMessage` (after the unchanged `if (SelectedBranch is null) return;` and the unchanged `workingDays` list building) | `CurrentBranchSettings` assignment + `StatusMessage = Strings.Organizations_SettingsSaved` — success only | `nameof(SaveBranchSettingsAsync)` |
| 4 | `SwitchRoleAsync` | `try { SwitchRoleAsync(role); ActionError=null; OnPropertyChanged×3 } catch (Exception) { SelectedRoleToSwitchTo = _currentSessionService.CurrentRole; ActionError; Has=true; log }` — see §C | 3× `OnPropertyChanged` (`CurrentRole` / `CurrentOrganizationName` / `CurrentBranchName`) — success only | `nameof(SwitchRoleAsync)` |
| 5 | `LoadBranchesForSelectedOrganizationAsync` (setter path) | `ReloadBranchesForSelectionAsync()` — a new thin wrapper: `try { await LoadBranchesForSelectedOrganizationAsync(); ActionError=null } catch (Exception) { ActionError; Has=true; log }`. The `SelectedOrganization` setter now fires `_ = ReloadBranchesForSelectionAsync()`. | n/a (wrapper) | `nameof(LoadBranchesForSelectedOrganizationAsync)` |
| 6 | `LoadSettingsForSelectedBranchAsync` (setter path) | `ReloadSettingsForSelectionAsync()` — identical wrapper; the `SelectedBranch` setter fires `_ = ReloadSettingsForSelectionAsync()`. | n/a (wrapper) | `nameof(LoadSettingsForSelectedBranchAsync)` |

- **`catch (Exception)` with no exception variable** in all 6 → `Exception.Message` / backend body / org / tax / VAT / receipt / branch-contact / role data structurally unreachable on screen and in the log.
- **Validation / `CanExecute` / early-returns unchanged and outside the `try`:** `CreateBranchAsync`'s `if (SelectedOrganization is null) return;`, `SaveBranchSettingsAsync`'s `if (SelectedBranch is null) return;`, the `CanExecute` predicates (`NewOrgName` non-empty / `SelectedOrganization is not null && NewBranchName` non-empty / `SelectedBranch is not null`) — byte-identical.
- **`TimeOnly.Parse` moved inside the `try`** (it is part of the operation, not an early-return validation) → a malformed `SettingsOpenTime` / `SettingsCloseTime` now surfaces inline as `ActionErrorMessage` instead of crashing to the global dialog. `SetBranchSettingsCalls` is not recorded for that path (the throw is before the service call). Test-covered (`SaveBranchSettingsCommand_MalformedTime_…`).
- **`await LoadAsync()` / `await LoadBranchesForSelectedOrganizationAsync()`** stay inside the guarded block for commands 1–2 (self-guarded / propagate to `LoadAsync`'s own catch — Wave A/B/C `CustomerProfileViewModel.SaveChangesAsync` precedent).
- **`State` is never set** by any of the 6 — an Organization command/secondary-load failure does not blank the page.

### B.3 Secondary loads — awaited path byte-unchanged

`LoadAsync` (line ~411) still does `await LoadBranchesForSelectedOrganizationAsync().ConfigureAwait(true);` directly; `LoadBranchesForSelectedOrganizationAsync` (line ~459) still does `await LoadSettingsForSelectedBranchAsync().ConfigureAwait(true);` directly. Only the **fire-and-forget setter path** now routes through the guarded wrappers. So:
- An initial-load branch/settings failure still propagates into `LoadAsync`'s existing top-level catch → `State = Error` (unchanged full-page behaviour, no regression).
- A **user-selection-triggered** branch/settings failure (previously an unobserved task exception → global dialog) now surfaces as a non-destructive inline `ActionErrorMessage`.

Test-covered (`SelectingAnotherOrganization_BranchLoadFails_…`, `SelectingAnotherBranch_SettingsLoadFails_…`).

---

## C. SWITCHROLE HANDLING

```csharp
private async Task SwitchRoleAsync()
{
    try
    {
        await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo).ConfigureAwait(true);   // UNCHANGED
        ActionErrorMessage = null;
        HasActionError = false;
        OnPropertyChanged(nameof(CurrentRole));                                                       // UNCHANGED (success only)
        OnPropertyChanged(nameof(CurrentOrganizationName));
        OnPropertyChanged(nameof(CurrentBranchName));
    }
#pragma warning disable CA1031
    catch (Exception)
#pragma warning restore CA1031
    {
        // The session's role is unchanged (the service threw before persisting).
        // Revert the two-way-bound picker so it agrees with CurrentRole again.
        SelectedRoleToSwitchTo = _currentSessionService.CurrentRole;
        ActionErrorMessage = Strings.Common_ActionFailedMessage;
        HasActionError = true;
        LogOperationFailed(nameof(SwitchRoleAsync));
    }
}
```

| Preserved | How |
|---|---|
| **Existing role-switch logic** | `await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo)` is the same call with the same argument; the `try` only wraps it. |
| **Session behavior** | `ICurrentSessionService` is untouched. The service is the sole persistence authority; on failure it throws *before* persisting, so `CurrentRole` is genuinely unchanged. The 3 `OnPropertyChanged` notifications fire **only on success**. |
| **Editable-state rollback ("restore previous editable state")** | the catch sets `SelectedRoleToSwitchTo = _currentSessionService.CurrentRole` — the two-way-bound role picker snaps back to the session's actual (unchanged) role, so `SelectedRoleToSwitchTo`, `CurrentRole`, and the picker all agree again. This is the Wave D analogue of `CustomerProfileViewModel.SaveChangesAsync`'s `EditableStatus = Customer.Status` revert. |

**Test-enforced:** `SwitchRoleCommand_Failure_DoesNotThrow_RevertsPickerAndLeavesSessionRoleUnchanged` asserts, after a failed switch attempt to `Reception` from `PlatformOwner`: `session.CurrentRole == PlatformOwner` (session unchanged), `sut.SelectedRoleToSwitchTo == PlatformOwner` (picker reverted), `sut.CurrentRole == PlatformOwner`, and the attempt was still made (`session.SwitchRoleAttempts == [Reception]`). `SwitchRoleCommand_Success_SwitchesRoleAndClearsError` confirms a later successful switch clears the error and lands on the new role.

---

## D. SECURITY

Organization is metadata-rich: **organization legal name, tax information, subscription plan, branch address / phone / email / manager, VAT %, receipt header/footer text, working hours, appointment rules, and the `WorkspaceRole → Permission` matrix.**

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — no exception variable bound in any of the 6 guards; `ActionErrorMessage` is only ever `null` or the compile-time constant `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has no `Exception` parameter; `LocalFileLoggerProvider` renders no backend body |
| Backend response payload (org / branch / tax / VAT / receipt data) | **not exposed** on either surface |
| Role / permission data | **not exposed** — `SelectedRoleToSwitchTo` / `_currentSessionService.CurrentRole` are `WorkspaceRole` enum values, **not logged** (operation name only); the revert assigns an enum, surfaces nothing. `PermissionMatrix` / `IPermissionEngine` untouched (test-asserted `PermissionMatrix_IsUnaffectedByCommandFailures`). |
| Internal identifiers (`SelectedOrganization.Id`, `SelectedBranch.Id`) | **not logged** (operation name only), **not shown** (generic string only) |

**Test-enforced:** `CreateOrganizationCommand_Failure_LogsOperationNameOnly_NoOrgOrTaxLeak` and `SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak` seed the stub exception with `OrgBackendSecret = "backend 500: org 'Rojan Holdings LLC' tax=IR-9982 VAT=9% role=PlatformOwner"` and assert `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`, plus `Assert.Contains(… "Operation=<Method>" …)`.

---

## E. TESTS

**+12 tests** (2,654 → 2,666). The 2 pre-existing `LoadAsync` tests are retained (retargeted from `ThrowingOrganizationQueryService` to the new `StubOrganizationQueryService.GetOrganizationsException` seam — same assertions, same behaviour). **No shared test double touched.**

### E.1 Test-double changes (all private nested, in `OrganizationPageViewModelTests.cs`)

| Double | Change |
|---|---|
| `NotSupportedOrganizationCommandService` → `StubOrganizationCommandService` | recording stub: `CreateOrganizationCalls` / `CreateBranchCalls` / `SetBranchSettingsCalls` + additive `Exception?` seams `CreateOrganizationException` / `CreateBranchException` / `SetBranchSettingsException`; success returns a deterministic DTO (`SetBranchSettingsAsync` echoes the arg); `Update*` return the arg (never exercised). |
| `ThrowingOrganizationQueryService` → `StubOrganizationQueryService` | `Organizations` / `Branches` lists + optional `BranchSettings` + `Exception?` seams `GetOrganizationsException` / `GetBranchesException` / `GetBranchSettingsException`; `GetBranchesAsync` filters by `organizationId`. |
| **new** `StubCurrentSessionService` | settable `CurrentRole` (updates on a successful `SwitchRoleAsync`), `SwitchRoleException` seam, `SwitchRoleAttempts` recorder; all other `ICurrentSessionService` members minimal (mirrors `FakeCurrentSessionService`). Used only by the `SwitchRoleAsync` tests. |

### E.2 New tests

| Test | Asserts |
|---|---|
| `CreateOrganizationCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm` | no throw; `HasActionError`; message; `State != Error`; `NewOrgName`/`NewOrgTaxInformation` preserved; command attempted |
| `CreateBranchCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm` | no throw; error set; `NewBranchName`/`NewBranchAddress` preserved; command attempted |
| `SaveBranchSettingsCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotShowSaved` | no throw; error set; `StatusMessage != Organizations_SettingsSaved`; `CurrentBranchSettings == null`; command attempted |
| `SaveBranchSettingsCommand_MalformedTime_DoesNotThrow_SetsActionError` | bad `SettingsOpenTime` → no throw; error set; not "saved"; `SetBranchSettingsCalls` empty (throw before the service call) |
| `SwitchRoleCommand_Failure_DoesNotThrow_RevertsPickerAndLeavesSessionRoleUnchanged` | §C — session unchanged, picker reverted, attempt made |
| `SwitchRoleCommand_Success_SwitchesRoleAndClearsError` | after a failure then a success: `HasActionError == false`, session + picker on the new role |
| `CreateOrganizationCommand_SuccessAfterFailure_ClearsActionError` | fail → clear seam → succeed → error cleared, form cleared |
| `SelectingAnotherOrganization_BranchLoadFails_SetsActionErrorAndDoesNotThrowOrBlankPage` | secondary-load setter path: no throw; `HasActionError`; `State != Error` |
| `SelectingAnotherBranch_SettingsLoadFails_SetsActionError` | secondary-load setter path: no throw; `HasActionError`; `State != Error` |
| `CreateOrganizationCommand_Failure_LogsOperationNameOnly_NoOrgOrTaxLeak` | `Operation=CreateOrganizationAsync` in an `Error` entry; no `OrgBackendSecret` in entries or `ActionErrorMessage` |
| `SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak` | `Operation=SwitchRoleAsync`; no secret leak |
| `PermissionMatrix_IsUnaffectedByCommandFailures` | `PermissionMatrix.Count` unchanged and non-empty after a failed command |

`dotnet test --filter FullyQualifiedName~Organizations` → **14 passed** (2 existing + 12 new).

---

## F. VALIDATION

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020 / CA1031 / CA1848)
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

| Expected (Phase 8.78) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,669 PASS | 2,666 / 2,666 | ✅ (12 added; ~2,669 was a conservative upper bound) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## G. COMMIT READINESS

**Not committed** (per Phase 8.78 STRICT SCOPE). Ready for Phase 8.79 commit scope review.

- **Exactly 2 modified tracked files:**
  ```
  git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
   M src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
   M tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
  ```
- No new file. No `Strings.cs` / `.resx` change. No service / DI / interface / DTO / RBAC / auth / navigation / `ICurrentSessionService` / `IPermissionEngine` / `Shell` / `[LoggerMessage]`-signature / shared-test-double change. `LoadAsync` body unchanged.
- Recommended commit (single, Organization-only, per scope review §G): `fix(desktop): guard organization command failures`. Reporting recommended as a separate follow-on mini-wave.
- Untracked `ROJAN_*.md` reports remain unstaged.

---

## STOP

Phase 8.78 implementation complete. 6 guarded paths in `OrganizationPageViewModel` — `CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync`, `SwitchRoleAsync` (+ role-picker revert on failure), and the two selection-triggered secondary-load setter paths (via new thin wrappers) — each reusing the Wave A–C pattern + the existing `[LoggerMessage]` + the existing `Common_ActionFailedMessage`; one additive `ActionErrorMessage`/`HasActionError` pair. `LoadAsync` body and every shared service / interface / test double untouched. Build 0/0, **2,666/2,666** tests, architecture 7/7.
**Next: Phase 8.79 — Wave D (Organization) Commit Scope Review.** Awaiting authorization.
