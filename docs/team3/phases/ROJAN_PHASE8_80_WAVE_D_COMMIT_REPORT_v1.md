# ROJAN AI — TEAM 3 — PHASE 8.80 — MISSING-GUARD SWEEP WAVE D (ORGANIZATION) — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `66c849077ffc0168de75335ec904fb7a0f2d7bea`
**New HEAD:** `525fd4b8fe5014577fcc01ff5f5e68b7cab92083`
**Commit subject:** `fix(desktop): guard organization command failures`

---

## A. COMMIT

```
commit 525fd4b8fe5014577fcc01ff5f5e68b7cab92083
Author: Meisam Elhaee <meisamelh66@gmail.com>
Date:   Fri Aug 28 11:37:05 2026 -0700

    fix(desktop): guard organization command failures

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

Subject EXACT as authorized. Trailers match the Team 3 arc convention.

```
git log --oneline -4
525fd4b fix(desktop): guard organization command failures
66c8490 fix(desktop): guard inventory and invoice-cancel command failures
a5be831 fix(desktop): guard HR command failures
794648e fix(desktop): guard customer/service/specialist command failures
```

---

## B. STAGING (explicit-path only)

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
git diff --cached --name-only        # 2
```

Never `git add .` / `git add -A`. Staged diff reviewed before commit.

`git show --stat 525fd4b`: **2 files changed, 521 insertions(+), 73 deletions(-)**. No new file.
- `OrganizationPageViewModel.cs` — 6 guarded paths (§C); deletions are original command bodies re-indented into `try` form + the 2 setter lines re-pointed at the new wrappers. `LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` bodies **not touched**.
- `OrganizationPageViewModelTests.cs` — 3 private nested test doubles rewritten (2 upgraded + 1 new); **+12 tests**; the 2 pre-existing `LoadAsync` tests keep their names and assertions (query-stub constructor swapped only).

All untracked `ROJAN_*.md` reports remain unstaged.

**Line-ending note (cosmetic, non-blocking — from the Phase 8.79 review §B.4):** the rewritten test file is stored **LF in the committed blob** (`git ls-files --eol` → `i/lf`), consistent with how git's index stores every file in this repo (`core.autocrlf=true`, no `.gitattributes`). The only residual difference from sibling test files is the absent UTF-8 BOM. Impact: none — build 0/0, all analyzers pass, all tests pass, no `.editorconfig` BOM rule.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

**6 guarded paths in `OrganizationPageViewModel`:**

| Path | Guard | Log call |
|---|---|---|
| `CreateOrganizationAsync` | `try { cmd + 7 field clears + await LoadAsync() } catch (Exception) { ActionErrorMessage = Common_ActionFailedMessage; HasActionError = true; log }` | `nameof(CreateOrganizationAsync)` |
| `CreateBranchAsync` | as above (after the unchanged `if (SelectedOrganization is null) return;`) — 8 field clears + `await LoadBranchesForSelectedOrganizationAsync()` | `nameof(CreateBranchAsync)` |
| `SaveBranchSettingsAsync` | `try` wraps the `BranchSettingsDto` build (incl. `TimeOnly.Parse`) + `SetBranchSettingsAsync` + `StatusMessage` (after the unchanged `if (SelectedBranch is null) return;` and `workingDays` list building) | `nameof(SaveBranchSettingsAsync)` |
| `SwitchRoleAsync` | `try { SwitchRoleAsync(role) + OnPropertyChanged×3 } catch (Exception) { SelectedRoleToSwitchTo = _currentSessionService.CurrentRole; ActionErrorMessage; HasActionError = true; log }` — see §E | `nameof(SwitchRoleAsync)` |
| `LoadBranchesForSelectedOrganizationAsync` (setter fire-and-forget path) | new wrapper `ReloadBranchesForSelectionAsync`; `SelectedOrganization` setter now fires `_ = ReloadBranchesForSelectionAsync()` | `nameof(LoadBranchesForSelectedOrganizationAsync)` |
| `LoadSettingsForSelectedBranchAsync` (setter fire-and-forget path) | new wrapper `ReloadSettingsForSelectionAsync`; `SelectedBranch` setter fires `_ = ReloadSettingsForSelectionAsync()` | `nameof(LoadSettingsForSelectedBranchAsync)` |

| Area | Status |
|---|---|
| RBAC infrastructure (`IPermissionGate` / `RolePermissions` / permission gates) | ✅ untouched (not in commit) |
| `IPermissionEngine` / `PermissionEngine` / `PermissionMatrix` | ✅ untouched — test `PermissionMatrix_IsUnaffectedByCommandFailures` confirms |
| `ICurrentSessionService` **interface** + concrete Shell (production) implementation | ✅ untouched |
| Authentication / session persistence | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| Navigation / back-stack / **Shell contracts** / `Shell.MainWindowViewModel` (Branch Switcher) | ✅ untouched |
| Backend contracts / `IOrganizationQueryService` / `IOrganizationCommandService` interfaces + DTOs | ✅ untouched |
| Other ViewModels (HR / Inventory / Accounting / Reporting / AI / Automation / …) | ✅ untouched |
| Shared production infrastructure — `AsyncRelayCommand` / `App.xaml.cs` / `Strings.cs` / all `.resx` / `[LoggerMessage]` signature | ✅ untouched (`Common_ActionFailedMessage` already ships in `794648e`; `Organizations_SettingsSaved` unchanged) |
| Shared test doubles — `FakeCurrentSessionService`, `RecordingLogger` helpers, every other stub | ✅ untouched |
| `LoadAsync` / `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` **bodies** | ✅ untouched |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

---

## D. POST-COMMIT VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | 723 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,666** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,666 / 2,666 PASS | 2,666 / 2,666 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,654 (`66c8490`) → **2,666** (`525fd4b`), delta **+12** (all `Presentation.Tests`, 711 → 723).

---

## E. WHAT LANDED

**6 unguarded paths in the one `OrganizationPageViewModel` are now guarded** with the app's established non-destructive in-page error pattern (Wave A–C precedent):

### E.1 Organization guard status

| Path | Behaviour after Wave D |
|---|---|
| `CreateOrganizationAsync` / `CreateBranchAsync` / `SaveBranchSettingsAsync` | a backend failure (or, for save-settings, a malformed `SettingsOpenTime` / `SettingsCloseTime`) sets `ActionErrorMessage = Strings.Common_ActionFailedMessage` + `HasActionError = true` on an inline, non-destructive property; the page is not blanked (`State` untouched); the form keeps the user's input for retry; `StatusMessage` is set to "saved" on the success path only |
| `SwitchRoleAsync` | see §E.2 |
| secondary loads (org / branch re-selection) | a failure on the **user-selection-triggered** fire-and-forget path (previously an unobserved task exception → generic global dialog) now surfaces as `ActionErrorMessage`. The initial-load path (`LoadAsync` → `await LoadBranchesForSelectedOrganizationAsync()`) is **unchanged** — still propagates to `LoadAsync`'s existing top-level catch → `State = Error` |

- **No business-behaviour change.** Each guard wraps existing flow only; validation (`if (Selected… is null) return;`), `CanExecute` predicates, `workingDays` list building, and every success-path side effect (form clears, `await LoadAsync()` reload, `StatusMessage`, `OnPropertyChanged`) are untouched. `TimeOnly.Parse` moved inside the `try` (it is part of the operation, not a validation gate) so a bad time input surfaces inline instead of crashing.
- **`State` is never set** by any of the 6 — an Organization command/secondary-load failure does not blank the page.
- **Logging:** each catch reuses `OrganizationPageViewModel`'s **existing** instance-form `[LoggerMessage(EventId = 1, Level = Error, "Organization page operation failed. Operation={Operation}")] LogOperationFailed(string operation)` (Phase 8.27), operation-name-only, once. Single `ILogger` field → no `SYSLIB1020`. No new logger, no `ILoggerFactory`, no DI change. The two secondary-load wrappers log distinct operation names (`nameof(LoadBranchesForSelectedOrganizationAsync)` / `nameof(LoadSettingsForSelectedBranchAsync)`) — no overlap with the command logs, no double-logging.
- **Security:** `catch (Exception)` with **no exception variable** in all 6 → `Exception.Message` / backend response body / organization legal name / tax information / VAT % / receipt header/footer text / branch address·phone·email·manager / `WorkspaceRole` / permission data / internal identifiers all structurally unreachable in both the on-screen message and the log. Test-enforced with a seeded sentinel (`"backend 500: org 'Rojan Holdings LLC' tax=IR-9982 VAT=9% role=PlatformOwner"`).
- **Localization:** no change — `Common_ActionFailedMessage` was added in Wave A (`794648e`); `Organizations_SettingsSaved` untouched.

### E.2 SwitchRole safety

On a failed role switch:
- **The session's role is unchanged** — `ICurrentSessionService` is the sole persistence authority and throws *before* persisting. `CurrentRole` still returns the prior role.
- **The two-way-bound picker is reverted** — the catch sets `SelectedRoleToSwitchTo = _currentSessionService.CurrentRole`, so `SelectedRoleToSwitchTo`, `sut.CurrentRole`, and the session's `CurrentRole` all agree again (the Wave-D analogue of `CustomerProfileViewModel.SaveChangesAsync`'s `EditableStatus` revert).
- **No RBAC behaviour change, no permission mutation** — no permission gate / `IPermissionEngine` / `RolePermissions` referenced; the catch assigns a `WorkspaceRole` enum to a bound property and grants/revokes nothing; `PermissionMatrix` is untouched.
- **The 3 `OnPropertyChanged` notifications (`CurrentRole` / `CurrentOrganizationName` / `CurrentBranchName`) fire on the success path only** — a failed switch does not signal a role change to any subscriber.
- Test-enforced: `SwitchRoleCommand_Failure_DoesNotThrow_RevertsPickerAndLeavesSessionRoleUnchanged` (session role stays `PlatformOwner`, picker reverts, attempt still made), `SwitchRoleCommand_Success_SwitchesRoleAndClearsError` (a later successful switch clears the error and lands both session and picker on the new role), `SwitchRoleCommand_Failure_LogsOperationNameOnly_NoRoleOrPermissionLeak`.

### E.3 Tests

**+12** (2,654 → 2,666). The 2 pre-existing `LoadAsync` tests retained (retargeted to the new query-stub seam — assertions unchanged). Test doubles: `NotSupportedOrganizationCommandService` → `StubOrganizationCommandService` (recording + additive `Exception?` seams), `ThrowingOrganizationQueryService` → `StubOrganizationQueryService` (lists + additive `Exception?` seams), **new** `StubCurrentSessionService` (settable `CurrentRole` + `SwitchRoleException` seam). All **private nested** — **no shared test double touched**. 0 existing test bodies changed beyond the query-stub constructor swap. No new test helper.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 2 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `525fd4b`.
- Working tree after commit: tracked tree clean (`git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'` → empty).

---

## G. MISSING-GUARD SWEEP — TRACK PROGRESS

| Wave | Domain | Status |
|---|---|---|
| **A** — Customer / Service / Specialist write commands | backend-connected | ✅ **DONE** — `794648e` (12 methods, 5 VMs) |
| **B** — HR (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3) | fake-backed | ✅ **DONE** — `a5be831` (13 methods, 2 VMs) |
| **C** — Inventory (page ×3, profile ×3) + `AccountingPageViewModel.CancelInvoiceAsync` | fake-backed | ✅ **DONE** — `66c8490` (7 methods, 3 VMs) |
| **D** — Organization (`OrganizationPageViewModel` — 4 commands + 2 secondary-load setter paths) | fake-backed | ✅ **DONE** — `525fd4b` (6 paths, 1 VM) |
| **D-Reporting** (mini-wave) — `ReportingPageViewModel` (`ReloadSnapshotsAsync` / `ToggleSavedAsync` / `DeleteSnapshotAsync`) | fake-backed | **NEXT** |
| **E** — AI Center (`AiCenterPageViewModel` ×~12) | fake-backed | pending |
| **F** — Automation tabs (`Workflows`/`ScheduledJobs`/`BusinessRules` ×~7) | fake-backed | pending |
| **G (P2)** — Workspace / Notification / Settings / CommandPalette (~28) | local / infra | pending, low priority |

Per the Phase 8.77 scope review §G, **Reporting** (the other half of the `ROJAN_PHASE8_64_*` §D grouping) is handled as its own audit → review → implement → commit mini-wave rather than folded into this Organization commit.

---

## STOP

Phase 8.80 commit executed and validated. HEAD `525fd4b`. Build 0/0, 2,666/2,666 tests, architecture 7/7.
**Missing-Guard Sweep Wave D (Organization) complete** — 4 command guards (`CreateOrganizationAsync`,
`CreateBranchAsync`, `SaveBranchSettingsAsync`, `SwitchRoleAsync`) + 2 secondary-load setter-path guards
in `OrganizationPageViewModel`; `SwitchRoleAsync` reverts the role picker to the session's actual
(unchanged) role on failure with no RBAC or permission mutation; no service / DI / `ICurrentSessionService`
/ `IPermissionEngine` / Shell / localization-file / shared-test-double change. Checkpoint updated
(`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`).
**Next: Phase 8.81 — Missing-Guard Sweep Reporting mini-wave (then Wave E — AI Center) — Scope Audit.** Awaiting authorization.
