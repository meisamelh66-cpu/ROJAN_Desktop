# ROJAN AI — TEAM 3 — PHASE 8.68 — MISSING-GUARD SWEEP WAVE A — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `5ba554ceb588e5780b87aebdf280538f6b25c485`
**New HEAD:** `794648e514f4a5bdaf853b1e9544858411fc84dd`
**Commit subject:** `fix(desktop): guard customer/service/specialist command failures`

---

## A. COMMIT

```
commit 794648e514f4a5bdaf853b1e9544858411fc84dd
Author:  Meisam Elhaee <meisamelh66@gmail.com>
Date:    Fri Aug 28 09:58:29 2026 -0700

    fix(desktop): guard customer/service/specialist command failures

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_01QKJ9GR9nPK5zfcYKD6kWZj
```

Subject is EXACT as authorized. Trailers match the Team 3 arc convention.

---

## B. STAGING (explicit-path only)

```
git reset
git add <17 explicit paths>          # never git add . / git add -A
git diff --cached --name-only        # 17
```

| Group | Files |
|---|---|
| Production VMs (5) | `Customers/CustomerPageViewModel.cs`, `Customers/CustomerProfileViewModel.cs`, `Services/ServiceProfileViewModel.cs`, `Specialists/SpecialistProfileViewModel.cs`, `Specialists/SpecialistPageViewModel.cs` |
| Localization (4) | `Localization/Strings.cs`, `Strings.resx`, `Strings.en.resx`, `Strings.ar.resx` |
| Test stubs (3) | `Customers/StubCustomerCommandService.cs`, `Services/StubServiceCommandService.cs`, `Specialists/StubSpecialistCommandService.cs` |
| Test VMs (5) | `Customers/CustomerPageViewModelTests.cs`, `Customers/CustomerProfileViewModelTests.cs`, `Services/ServiceProfileViewModelTests.cs`, `Specialists/SpecialistProfileViewModelTests.cs`, `Specialists/SpecialistPageViewModelTests.cs` |

`git show --stat 794648e`: **17 files changed, 592 insertions(+), 42 deletions(-)**. No new file. The 42 deletions are all original single-line command bodies replaced by their `try/catch`-wrapped form + one `SpecialistPageViewModel.LoadAsync` logger-expression line — no property/validation/service/assertion removed. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

| Area | Status |
|---|---|
| `ServicePageViewModel` (precedent only — already guarded) | ✅ untouched (not in commit) |
| `AsyncRelayCommand` / `RelayCommand` (command infrastructure) | ✅ untouched |
| `App.xaml.cs` (`DispatcherUnhandledException` / `LogUnhandledException`) | ✅ untouched |
| `IServiceCommandService` / `ICustomerCommandService` / `ISpecialistCommandService` / any interface / DTO | ✅ untouched |
| DI — `Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs` | ✅ untouched |
| Domain / Infrastructure / Shell / Application projects | ✅ untouched |
| Backend contracts | ✅ untouched |
| RBAC / permission gates | ✅ untouched |
| Authentication | ✅ untouched |
| Navigation / back-stack | ✅ untouched |
| Shared production infrastructure (every `[LoggerMessage]` signature, the Load-boundary `ErrorMessage = exception.Message`) | ✅ untouched |

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
| Rojan.Desktop.Presentation.Tests | 679 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,622** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,622 / 2,622 PASS | 2,622 / 2,622 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,609 (`5ba554c`) → **2,622** (`794648e`), delta **+13** (all `Presentation.Tests`, 666 → 679).

---

## E. WHAT LANDED

**12 unguarded backend-connected write commands are now guarded** with the app's established in-page error pattern (`ServicePageViewModel.CreateServiceAsync` / `ServiceProfileViewModel.SaveChangesAsync` precedent):

| ViewModel | Methods | Error surface |
|---|---|---|
| `CustomerPageViewModel` | `CreateCustomerAsync` | **new** `CreateErrorMessage` / `HasCreateError` → `Strings.Common_ActionFailedMessage` |
| `CustomerProfileViewModel` | `AddNoteAsync`, `AddTagAsync`, `RemoveTagAsync`, `SaveChangesAsync` | **new** `SaveErrorMessage` / `HasSaveError` → `Strings.Common_ActionFailedMessage`; `SaveChangesAsync` catch also reverts `EditableStatus = Customer.Status` |
| `ServiceProfileViewModel` | `AssignSpecialistAsync`, `UnassignSpecialistAsync` | **existing** `SaveErrorMessage` / `HasSaveError` → `Strings.Services_SaveError` |
| `SpecialistProfileViewModel` | `AddSkillAsync`, `RemoveSkillAsync` | **existing** `SaveErrorMessage` / `HasSaveError` → `Strings.Specialists_SaveError` |
| `SpecialistPageViewModel` | `CreateSpecialistAsync` | **new** `CreateErrorMessage` / `HasCreateError` → `Strings.Specialists_SaveError`; logs via a new `private ILogger Logger` computed property (static-form `[LoggerMessage]`, no new field, no `SYSLIB1020`) |

- **No business-behaviour change.** Each guard wraps the existing `await _commandService.X(...)` + `clear form` + `await LoadAsync()` + `re-select` verbatim in a `try`; validation (`if null return`), `CanExecute`, RBAC, and the success path are untouched. The backend remains the sole write authority.
- **Error UX:** on failure the command sets a fixed localized string on an inline, non-destructive error property (not `State = Error`, which replaces the whole panel/page). `App.DispatcherUnhandledException` no longer fires for these 12 paths.
- **Logging:** each catch reuses the ViewModel's **existing** `[LoggerMessage]` (`LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(Logger, nameof(...))`), operation-name-only, once. No new logger, no `ILoggerFactory` added, no `SYSLIB1020`.
- **Security:** `catch (Exception)` with **no exception variable** in all 12 → `Exception.Message` / backend response body / internal identifiers / PII structurally unreachable in both the on-screen message and the log. Test-enforced with seeded backend-body secrets.
- **Localization:** one new key `Common_ActionFailedMessage` in all 3 locale files (`Strings.resx` fa, `Strings.en.resx`, `Strings.ar.resx`) + the `Strings.cs` accessor.
- **Tests:** +13 (failure-does-not-throw, inline-error-set, form-input-preserved, `State != Error`, `EditableStatus` revert, error-clears-on-next-success, operation-only-log no-leak). Shared stubs gained additive `Exception?` seams (null-path byte-identical). 0 existing test bodies changed. No new test helper.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 17 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `794648e`.

---

## G. MISSING-GUARD SWEEP — TRACK PROGRESS

| Wave | Domain | Status |
|---|---|---|
| **A** — Customer / Service / Specialist write commands | backend-connected | ✅ **DONE** — `794648e` (12 methods, 5 VMs) |
| **B** — HR (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3) | fake-backed | **NEXT** |
| C — Inventory (`InventoryPageViewModel` ×3, `InventoryProfileViewModel` ×3) + `AccountingPageViewModel.CancelInvoiceAsync` | fake-backed | pending |
| D — Organization (×4 + 2 secondary loads) + Reporting (×3) | fake-backed | pending |
| E — AI Center (`AiCenterPageViewModel` ×~12) | fake-backed | pending |
| F — Automation tabs (`Workflows`/`ScheduledJobs`/`BusinessRules` ×~7) | fake-backed | pending |
| G (P2) — Workspace / Notification / Settings / CommandPalette (~28) | local / infra | pending, low priority |

The reusable pattern (in-page `try`/`catch` + inline error property + reuse existing `[LoggerMessage]` + `Common_ActionFailedMessage` for domains with no `X_SaveError` string) is now established for Waves B–F.

---

## STOP

Phase 8.68 commit executed and validated. HEAD `794648e`. Build 0/0, 2,622/2,622 tests, architecture 7/7.
**Missing-Guard Sweep Wave A complete** — 12 backend-connected write commands now use the app's in-page
error pattern; no business-behaviour change. Checkpoint updated (`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`).
**Next: Phase 8.69 — Missing-Guard Sweep Wave B (HR) — Scope Audit.** Awaiting authorization.
