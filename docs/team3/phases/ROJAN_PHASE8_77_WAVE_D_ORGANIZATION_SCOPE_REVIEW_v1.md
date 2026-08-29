# ROJAN AI — TEAM 3 — PHASE 8.77 — MISSING-GUARD SWEEP WAVE D (ORGANIZATION) — SCOPE REVIEW v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `66c849077ffc0168de75335ec904fb7a0f2d7bea`
**Objective:** Audit the Organization domain's command failures and define Wave D guard scope, using the Wave A/B/C pattern (`794648e`, `a5be831`, `66c8490`).

---

## A. GIT STATE

```
git rev-parse HEAD        → 66c849077ffc0168de75335ec904fb7a0f2d7bea
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `66c8490` (Wave C / Inventory+Accounting commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `66c8490` guard inventory and invoice-cancel · `a5be831` guard HR · `794648e` guard customer/service/specialist |

Baseline test suite (checkpoint §E, `66c8490`): **2,654 / 2,654** — Domain 456, Application 791, Presentation 711, Infrastructure 609, Shell 80, Architecture 7.

---

## B. ORGANIZATION VIEWMODEL INVENTORY

The Organization domain has exactly **one ViewModel**: `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs`.

- No profile ViewModel (branches / branch-settings / permissions are all sections *within* the one page VM; `OrganizationSection.cs` is a plain enum).
- No separate Workspace / Team / Role-management VM. Role/permission handling lives here: the **Permissions** section is a read-only `WorkspaceRole → Permission` reference grid (`PermissionMatrix`, built once from `IPermissionEngine.GetPermissions`), and the **Session** section carries the live **role switcher** (`SwitchRoleCommand` → `ICurrentSessionService.SwitchRoleAsync`). The **Branch Switcher** itself lives in the Shell header (`Shell.MainWindowViewModel` — a Shell concern, out of Presentation Wave D scope).
- Already `sealed partial` with an instance-form operation-name-only `[LoggerMessage(EventId = 1, Level = Error, Message = "Organization page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` and a **single** `ILogger<OrganizationPageViewModel>` field — **no logging-infrastructure change needed.**

### B.1 User-triggered commands & selection-triggered secondary loads

| # | Member | Trigger | Current exception handling | Existing error/state surface | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `CreateOrganizationAsync` | `CreateOrganizationCommand` (`CanExecute`: `NewOrgName` non-empty) | **none** — bare `await _commandService.CreateOrganizationAsync(name, legalName, taxInfo, subscription, code, phone, email, address)` | `State` / `ErrorMessage` (destructive, `LoadAsync` only); no inline command error | generic `App.DispatcherUnhandledException` dialog; 7 `NewOrg*` fields already cleared |
| 2 | `CreateBranchAsync` | `CreateBranchCommand` (`CanExecute`: `SelectedOrganization is not null && NewBranchName` non-empty) | **none** (after an `if (SelectedOrganization is null) return;` early-return) — bare `await _commandService.CreateBranchAsync(orgId, name, code, address, phone, email, manager, timeZone, currency)` | as above | generic dialog; 8 `NewBranch*` fields already cleared |
| 3 | `SaveBranchSettingsAsync` | `SaveBranchSettingsCommand` (`CanExecute`: `SelectedBranch is not null`) | **none** (after `if (SelectedBranch is null) return;`) — builds `BranchSettingsDto` (incl. **`TimeOnly.Parse(SettingsOpenTime/CloseTime)`** — can throw `FormatException` on bad input), then `CurrentBranchSettings = await _commandService.SetBranchSettingsAsync(settings)`, then `StatusMessage = Strings.Organizations_SettingsSaved` | `StatusMessage` (success only); `State`/`ErrorMessage` (destructive) | generic dialog on a malformed time **or** a backend failure |
| 4 | `SwitchRoleAsync` | `SwitchRoleCommand` (no `CanExecute` predicate) | **none** — `await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo)` then 3× `OnPropertyChanged` (`CurrentRole` / `CurrentOrganizationName` / `CurrentBranchName`) | none | **generic dialog on a failed session role switch** — and `SelectedRoleToSwitchTo` (the two-way-bound picker) is left showing the *attempted* role while `CurrentRole` still shows the old one → inconsistent UI |
| 5 | `LoadBranchesForSelectedOrganizationAsync` | `_ = …()` fire-and-forget from the **`SelectedOrganization` setter** (also `await`-ed inside `LoadAsync`) | **none** at the fire-and-forget site — a throw there becomes an unobserved task exception caught by `App`'s surface | `State`/`ErrorMessage` only when reached via the `LoadAsync` await path | generic dialog when the user picks a different organization and its branches fail to load |
| 6 | `LoadSettingsForSelectedBranchAsync` | `_ = …()` fire-and-forget from the **`SelectedBranch` setter** (also `await`-ed inside `LoadBranchesForSelectedOrganizationAsync`) | **none** at the fire-and-forget site | as above | generic dialog when the user picks a different branch and its settings fail to load |

`LoadAsync` itself is already guarded (top-level `catch (Exception exception) { ErrorMessage = exception.Message; State = Error; LogOperationFailed(nameof(LoadAsync)); }` — the `exception.Message` surfacing is the pre-existing "sanitize load-error surfacing" P2 item, **out of Wave D scope**).

### B.2 Backend connectivity

Organization is **fake-backed** (`FakeOrganizationRepository`; checkpoint §D — legacy `IPermissionGate`). `ICurrentSessionService` is a real Presentation-layer service that persists the role choice. Wave D guards are worth doing now: the pattern must be right before any backend contract, exactly as Waves A–C. **P1 — UX consistency**, not P0.

---

## C. COMMAND CLASSIFICATION

### Category A — needs a guard (6)

| Member | Why |
|---|---|
| `CreateOrganizationAsync` | backend write (org + tax/legal metadata) |
| `CreateBranchAsync` | backend write (branch + contact metadata) |
| `SaveBranchSettingsAsync` | backend write (VAT / receipt / hours / rules) + a client-side `TimeOnly.Parse` that can throw |
| `SwitchRoleAsync` | **session-state mutation** — needs a guard **and** a role-picker revert on failure (special case, §D.3) |
| `LoadBranchesForSelectedOrganizationAsync` (setter fire-and-forget path only) | unobserved-task-exception gap on organization re-selection |
| `LoadSettingsForSelectedBranchAsync` (setter fire-and-forget path only) | unobserved-task-exception gap on branch re-selection |

### Category B — UI/state-only, no guard (2)

| Member | Why no guard |
|---|---|
| `SelectSectionCommand` | `RelayCommand` — pure `SelectedSection = (OrganizationSection)param` assignment, no `await`, cannot fail |
| `RefreshCommand` | `AsyncRelayCommand(_ => LoadAsync())` — delegates to the **already-guarded** `LoadAsync` |

### Category C — already guarded (1)

| Member | Guard |
|---|---|
| `LoadAsync` | pre-existing top-level `catch (Exception)` → `State = Error` + `LogOperationFailed(nameof(LoadAsync))` (Phase 8.27, `cbc3a82`). Unchanged by Wave D. |

---

## D. GUARD STRATEGY

### D.1 Wave A/B/C pattern applies — one additive property pair

`OrganizationPageViewModel` gains one **additive** pair (private-set, `SetProperty`, **no constructor / DI change**):

```csharp
public string? ActionErrorMessage { get; private set; }   // non-destructive: never touches State/ErrorMessage
public bool    HasActionError      { get; private set; }
```

Same shape as `HrPageViewModel` / `InventoryPageViewModel.ActionErrorMessage` (Waves B/C).

### D.2 Localization — no change

`Strings.Common_ActionFailedMessage` already ships in `Strings.cs` + all 3 `.resx` (Wave A `794648e`). There is **no `Organizations_SaveError`** string; `Common_ActionFailedMessage` is the correct reuse (Wave B/C precedent). `Organizations_SettingsSaved` (the `SaveBranchSettingsAsync` success `StatusMessage`) is untouched.

### D.3 Per-member transformation

**Commands 1–3 (`CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync`)** — standard:

```csharp
// unchanged: CanExecute predicate + early-return validation stay ABOVE the try
if (SelectedBranch is null) { return; }

try
{
    var workingDays = /* … unchanged list building … */;
    var settings = new BranchSettingsDto(/* … incl. TimeOnly.Parse(SettingsOpenTime/CloseTime) … */);
    CurrentBranchSettings = await _commandService.SetBranchSettingsAsync(settings).ConfigureAwait(true);
    ActionErrorMessage = null; HasActionError = false;
    StatusMessage = Strings.Organizations_SettingsSaved;
}
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
catch (Exception)
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(SaveBranchSettingsAsync));
}
```

- The `TimeOnly.Parse` calls move **inside** the `try` (they are part of the operation, not an early-return validation) → a malformed time string now surfaces inline instead of crashing. `StatusMessage` is set on the success path only; on failure it is left untouched.
- `CreateOrganizationAsync` / `CreateBranchAsync`: the existing form-field clears + `await LoadAsync()` / `await LoadBranchesForSelectedOrganizationAsync()` stay inside the `try` (both reloads are self-guarded / propagate to `LoadAsync`'s own catch — Wave A/B/C `CustomerProfileViewModel.SaveChangesAsync` precedent).

**Command 4 (`SwitchRoleAsync`) — SPECIAL CASE, role-picker revert:**

```csharp
try
{
    await _currentSessionService.SwitchRoleAsync(SelectedRoleToSwitchTo).ConfigureAwait(true);
    ActionErrorMessage = null; HasActionError = false;
    OnPropertyChanged(nameof(CurrentRole));
    OnPropertyChanged(nameof(CurrentOrganizationName));
    OnPropertyChanged(nameof(CurrentBranchName));
}
catch (Exception)
{
    // revert the two-way-bound picker to the session's ACTUAL (unchanged) role
    SelectedRoleToSwitchTo = _currentSessionService.CurrentRole;
    ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true;
    LogOperationFailed(nameof(SwitchRoleAsync));
}
```

- On failure the session's role is unchanged (the service threw before persisting). The revert sets `SelectedRoleToSwitchTo` back to `_currentSessionService.CurrentRole` so the picker and `CurrentRole` agree → **`SelectedRoleToSwitchTo` / session state stay consistent** (the `ROJAN_PHASE8_64_*` §D.3 / row-D requirement). Mirrors `CustomerProfileViewModel.SaveChangesAsync`'s `EditableStatus` revert.
- The 3 `OnPropertyChanged` calls fire on success only.

**Members 5–6 (secondary loads) — guard the fire-and-forget path ONLY, awaited path unchanged:**

The `SelectedOrganization` / `SelectedBranch` setters change from `_ = LoadBranchesForSelectedOrganizationAsync();` to `_ = ReloadBranchesForSelectionAsync();` (two new tiny private wrappers). `LoadAsync` and `LoadBranchesForSelectedOrganizationAsync` keep `await`-ing the **original** methods directly — that path is byte-unchanged.

```csharp
private async Task ReloadBranchesForSelectionAsync()
{
    try
    {
        await LoadBranchesForSelectedOrganizationAsync().ConfigureAwait(true);
        ActionErrorMessage = null; HasActionError = false;
    }
#pragma warning disable CA1031 // …same boundary reasoning…
    catch (Exception)
#pragma warning restore CA1031
    {
        ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true;
        LogOperationFailed(nameof(LoadBranchesForSelectedOrganizationAsync));
    }
}
// + ReloadSettingsForSelectionAsync (identical, wrapping LoadSettingsForSelectedBranchAsync)
```

This closes exactly the unobserved-task-exception gap; the initial-load path (`LoadAsync` → `await LoadBranchesForSelectedOrganizationAsync()`) still throws into `LoadAsync`'s existing catch → no change to the full-page-error behaviour on a first-load failure.

### D.4 Existing properties reused / not reused

| | |
|---|---|
| **Reused** | `LogOperationFailed(string operation)` (existing instance-form `[LoggerMessage]`); `Strings.Common_ActionFailedMessage`; `Strings.Organizations_SettingsSaved`; `_currentSessionService.CurrentRole` (for the revert) |
| **New (additive)** | `ActionErrorMessage` / `HasActionError` (1 pair); `ReloadBranchesForSelectionAsync` / `ReloadSettingsForSelectionAsync` (2 private wrappers); 2 one-line setter edits |
| **NOT touched** | `State` / `ErrorMessage` (destructive surface — Wave D is non-destructive); `StatusMessage` (success only); `LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` bodies; `PermissionMatrix` / `IPermissionEngine`; `ICurrentSessionService` interface / impl; ctor; `[LoggerMessage]` signature |

### D.5 Special rollback / state requirements

| Member | Requirement | Handling |
|---|---|---|
| `SwitchRoleAsync` | picker ↔ session-role consistency on failure | revert `SelectedRoleToSwitchTo = _currentSessionService.CurrentRole` in the catch (§D.3) |
| `CreateOrganizationAsync` / `CreateBranchAsync` | form already cleared before the await today | guard keeps the clears inside the `try` **after** `ActionErrorMessage = null` — so on failure the form is **not** cleared (values kept for retry). Minor UX improvement, no behaviour regression. |
| `SaveBranchSettingsAsync` | `CurrentBranchSettings` reflects backend truth | on failure `CurrentBranchSettings` is left at its last-loaded value (the assignment is inside the `try`, past the throw); the bound Settings* inputs keep the user's edits for retry |
| secondary loads | initial-load error behaviour | unchanged — only the setter fire-and-forget path is wrapped (§D.3) |

---

## E. SECURITY REVIEW

Organization is a metadata-rich domain: **organization legal name, tax information, subscription plan, branch address / phone / email / manager, VAT %, receipt header/footer text, working hours, appointment rules, and the `WorkspaceRole → Permission` matrix.**

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — `catch (Exception)` binds **no variable** in all 6 new guards; `ActionErrorMessage` is only ever `null` or the compile-time constant `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has **no `Exception` parameter**; `LocalFileLoggerProvider` renders no backend body |
| Backend response payload (org/branch/tax/VAT/receipt data) | **not exposed** on either surface |
| Permission / role data | **not exposed** — `SelectedRoleToSwitchTo` and `_currentSessionService.CurrentRole` are `WorkspaceRole` enum values; **not logged** (operation name only); the revert assigns an enum, surfaces nothing. `PermissionMatrix` / `IPermissionEngine` untouched. |
| Internal identifiers (`SelectedOrganization.Id`, `SelectedBranch.Id`) | **not logged** (operation name only), **not shown** (generic string only) |

**Test-enforced:** the Wave D no-leak tests seed the stub exception with a sentinel (`"backend 500: org 'Rojan Holdings LLC' tax=IR-9982 VAT=9% role=PlatformOwner"`) and assert `Assert.DoesNotContain(sentinel, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## F. TEST PLAN

### F.1 Test-infrastructure upgrade (test-file-local, NO shared-stub change)

`OrganizationPageViewModelTests.cs` currently uses `FakeCurrentSessionService` (a **shared** double from the Automation tests — `CurrentRole => PlatformOwner`, `SwitchRoleAsync => Task.CompletedTask`, no seams) plus two private nested stubs (`NotSupportedOrganizationCommandService` — every member throws `NotSupportedException`; `ThrowingOrganizationQueryService` — `GetOrganizationsAsync` throws, the rest return empty). To exercise the Wave D guards, this file's **private nested** doubles are upgraded (nothing shared is touched):

| Double | Change |
|---|---|
| `NotSupportedOrganizationCommandService` → `StubOrganizationCommandService` (private nested) | becomes a recording stub: records each call, returns a deterministic DTO, and honours additive `Exception?` seams `CreateOrganizationException` / `CreateBranchException` / `SetBranchSettingsException`. |
| `ThrowingOrganizationQueryService` → configurable (private nested) | add optional per-method failure hooks / delegates for `GetBranchesAsync` and `GetBranchSettingsAsync` so the secondary-load guards can be exercised; keep the `GetOrganizationsAsync`-throws construction for the existing `LoadAsync` tests. |
| **new** `StubCurrentSessionService` (private nested) | settable `CurrentRole` backing field + a `SwitchRoleException` seam; `SwitchRoleAsync(role)` records the attempt, throws if the seam is set, else updates the backing `CurrentRole`. Used only by the `SwitchRoleAsync` tests; the existing `LoadAsync` tests keep using the shared `FakeCurrentSessionService`. |

No change to `FakeCurrentSessionService`, `PermissionEngine`, or any shared stub.

### F.2 New tests (`OrganizationPageViewModelTests.cs`)

| Category | Tests | Count |
|---|---|---|
| **Failure does not throw + error surfaced** — one per Category-A member: `Record.Exception(() => Cmd.Execute(null))` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage` | `CreateOrganization`, `CreateBranch`, `SaveBranchSettings`, `SwitchRole`, secondary-branch-load (via `SelectedOrganization` setter), secondary-settings-load (via `SelectedBranch` setter) | 6 |
| **State / RBAC preservation** | `CreateOrganization` failure → `NewOrgName`/`NewOrgTaxInformation` preserved, `State != Error`; `SaveBranchSettings` failure → `CurrentBranchSettings` unchanged + `StatusMessage` not set to "saved"; `SwitchRole` failure → **`SelectedRoleToSwitchTo == CurrentRole`** (reverted) and the session's role genuinely unchanged; `PermissionMatrix` untouched | ~4 |
| **`SaveBranchSettings` malformed-time** | bad `SettingsOpenTime` → no throw, `HasActionError`, `Organizations_SettingsSaved` not shown | 1 |
| **Success clears error** | `CreateOrganization` fail → clear seam → succeed → `HasActionError == false`, `ActionErrorMessage == null`; same for `SwitchRole` (and the picker stays on the newly-switched role) | ~2 |
| **No sensitive-data leak** | `CreateOrganization` / `SwitchRole` failure → `Operation=<Method>` in an `Error` entry; `DoesNotContain(sentinel)` in `logger.Entries` **and** `ActionErrorMessage` | 2 |
| **Regression** | the 2 existing `LoadAsync_QueryThrows_LogsError` / `NoLoggerSupplied_…` tests still pass unchanged | (0 new) |

**Estimated new tests: ~15.** Conservative suite projection: **2,654 → ~2,669**.

### F.3 Files changed (Phase 8.78 implementation)

| Group | Files | Count |
|---|---|---|
| Production | `ViewModels/Organizations/OrganizationPageViewModel.cs` | 1 |
| Test | `Organizations/OrganizationPageViewModelTests.cs` | 1 |
| **Total** | | **2** |

No new file, no `Strings.cs` / `.resx` change, no shared-stub change, no new test helper.

---

## G. COMMIT STRATEGY

**Recommendation: a single Organization-only commit — Reporting handled separately.**

```
fix(desktop): guard organization command failures
```

- This phase (8.77) is scoped **"Wave D — Organization"**. Organization alone is already 6 guarded paths, one new property pair, two new wrapper methods, a `SwitchRoleAsync` session-revert special case, and a meaningful test-infra upgrade (3 private nested doubles). It is a self-contained, reviewable unit.
- **Reporting** (`ReportingPageViewModel` — `ReloadSnapshotsAsync`, `ToggleSavedAsync`, `DeleteSnapshotAsync`, per `ROJAN_PHASE8_64_*` §D row D) is a clean, independent 3-method set with no shared surface with Organization. Folding it into this commit would mix two unrelated ViewModels and double the review surface for no isolation benefit.
- The `ROJAN_PHASE8_64_*` §D "one commit — organization and reporting" grouping predates the wave-by-wave cadence that Waves A–C established; splitting is the consistent choice.

**Alternative (combined):** `fix(desktop): guard organization and reporting command failures` in one commit (3 files: `OrganizationPageViewModel` + `ReportingPageViewModel` + a Reporting stub/test). Costs a larger single review; still low risk. **Not recommended** — a separate Reporting mini-wave keeps each commit tight.

**Risk of the Organization commit: LOW-MEDIUM.** Additive `try`/`catch` + one property pair + two thin wrappers; no service / DI / ctor / interface change; fake-backed. The one elevated point: `SwitchRoleAsync` touches session state — mitigated by (a) the service is the sole persistence authority and throws *before* persisting, (b) the explicit picker revert, (c) a dedicated test asserting the session role is genuinely unchanged after a failed switch.

---

## H. PHASE 8.78 RECOMMENDATION

**PHASE 8.78 — MISSING-GUARD SWEEP — WAVE D (ORGANIZATION) — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs`:
  - add `ActionErrorMessage` / `HasActionError` (private-set, additive; no ctor change)
  - wrap `CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync` in the §D.3 `try`/`catch` (validation / `CanExecute` / early-returns stay outside; `TimeOnly.Parse` moves inside; `StatusMessage` success-only); each catch → set the pair + `LogOperationFailed(nameof(Method))`; clear on success
  - wrap `SwitchRoleAsync` with the §D.3 **role-picker revert** (`SelectedRoleToSwitchTo = _currentSessionService.CurrentRole`) in the catch; `OnPropertyChanged` trio on success only
  - add `ReloadBranchesForSelectionAsync` / `ReloadSettingsForSelectionAsync` private guarded wrappers; point the `SelectedOrganization` / `SelectedBranch` setters at them (`LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` keep awaiting the originals — awaited path byte-unchanged)
- `tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs`:
  - upgrade the 2 existing private nested doubles + add 1 new private nested `StubCurrentSessionService` (§F.1) — **no shared-stub change**
  - ~15 new tests (§F.2); the 2 existing tests unchanged

**DO NOT:** change any service / DI / ViewModel constructor / `ICurrentSessionService` interface or impl / `IPermissionEngine` / backend contract / RBAC / `CanExecute` / navigation / `Shell.MainWindowViewModel` / command infrastructure / `[LoggerMessage]` signature / `Strings.cs` / `.resx` / `LoadAsync` body / any shared test double. No commit.

**Risk: LOW-MEDIUM** (per §G). Additive; fake-backed; the session-state touch is bounded and test-covered.

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (single `ILogger` + instance form → no `SYSLIB1020`; no `CA1031` / `CA1848`).
- Full suite → **~2,669 / ~2,669 PASS** (Presentation 711 → ~726; Domain 456, Application 791, Infrastructure 609, Shell 80 unchanged).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_78_WAVE_D_ORGANIZATION_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.79 commit scope review.

**Downstream:** a **Reporting mini-wave** (`ReportingPageViewModel` ×3 — `ReloadSnapshotsAsync` / `ToggleSavedAsync` / `DeleteSnapshotAsync`; `fix(desktop): guard reporting command failures`) as its own audit → review → implement → commit cycle; then Wave E (AI Center ×~12), Wave F (Automation tabs ×~7), Wave G (P2 infra).

---

## STOP

Phase 8.77 scope review complete. HEAD `66c8490`, tracked tree clean, baseline 2,654 / 2,654.
Wave D (Organization) = **6 guarded paths in the one `OrganizationPageViewModel`** — `CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync`, `SwitchRoleAsync` (+ role-picker revert), and the two secondary-load setter fire-and-forget paths — each reusing the Wave A–C pattern + the existing `[LoggerMessage]` + `Common_ActionFailedMessage`; one additive `ActionErrorMessage`/`HasActionError` pair; two thin guarded wrappers. No service / DI / RBAC / `ICurrentSessionService` / `Shell` / localization-file / shared-test-double change. ~2 files, ~15 tests, one commit `fix(desktop): guard organization command failures`; Reporting recommended as a separate follow-on mini-wave.
**Recommended next: Phase 8.78 — Wave D (Organization) Implementation.** Awaiting authorization.
