# ROJAN AI — TEAM 3 — PHASE 8.72 — MISSING-GUARD SWEEP WAVE B (HR) — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `794648e514f4a5bdaf853b1e9544858411fc84dd`
**New HEAD:** `a5be83142bbe411beda3daaa115fd18d528bcdf2`
**Commit subject:** `fix(desktop): guard HR command failures`

---

## A. COMMIT

```
commit a5be83142bbe411beda3daaa115fd18d528bcdf2
Author: Meisam Elhaee <meisamelh66@gmail.com>
Date:   Fri Aug 28 10:36:10 2026 -0700

    fix(desktop): guard HR command failures

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

Subject is EXACT as authorized. Trailers match the Team 3 arc convention.

```
git log --oneline -4
a5be831 fix(desktop): guard HR command failures
794648e fix(desktop): guard customer/service/specialist command failures
5ba554c fix(desktop): drop exception payload from diagnostic logging
6a1bced fix(desktop): add ViewModel diagnostic logging (specialist page)
```

---

## B. STAGING (explicit-path only)

```
git reset
git add <9 explicit paths>            # never git add . / git add -A
git diff --cached --name-only         # 9
```

| Group | Files |
|---|---|
| Production VMs (2) | `ViewModels/HR/HrPageViewModel.cs`, `ViewModels/HR/EmployeeProfileViewModel.cs` |
| Test stubs (5) | `HR/StubEmployeeCommandService.cs`, `HR/StubAttendanceCommandService.cs`, `HR/StubShiftCommandService.cs`, `HR/StubCommissionCommandService.cs`, `HR/StubPayrollCommandService.cs` |
| Test VMs (2) | `HR/HrPageViewModelTests.cs`, `HR/EmployeeProfileViewModelTests.cs` |

`git show --stat a5be831`: **9 files changed, 682 insertions(+), 63 deletions(-)**. No new file. The 63 deletions are entirely the original single-line command bodies re-indented into their `try`-wrapped form — no property, validation, service call, or assertion removed. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

| Area | Status |
|---|---|
| Payroll services (`IPayrollCommandService` / `IPayrollQueryService` + impls + `FakePayrollRepository`) | ✅ untouched (not in commit) |
| Commission services (`ICommissionCommandService` / `ICommissionQueryService` + impls + fake repo) | ✅ untouched |
| Attendance / Leave services (`IAttendanceCommandService` / `IAttendanceQueryService`) | ✅ untouched |
| Employee / Shift services + all HR DTOs / request records | ✅ untouched |
| Backend contracts / HTTP clients / API layer | ✅ untouched |
| RBAC / permission gates / `CanExecute` predicates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / shell | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| `AsyncRelayCommand` / `RelayCommand` / `ViewModelBase` / `App.xaml.cs` | ✅ untouched |
| `Strings.cs` / `Strings.resx` / `Strings.en.resx` / `Strings.ar.resx` (`Common_ActionFailedMessage` already shipped in Wave A `794648e`) | ✅ untouched |
| Every `[LoggerMessage]` signature / EventId / Level / Message | ✅ untouched |
| `LoadAsync` / `SearchAsync` catches (incl. pre-existing `ErrorMessage = exception.Message`) | ✅ untouched |
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
| Rojan.Desktop.Presentation.Tests | 698 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,641** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,641 / 2,641 PASS | 2,641 / 2,641 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,622 (`794648e`) → **2,641** (`a5be831`), delta **+19** (all `Presentation.Tests`, 679 → 698).

---

## E. WHAT LANDED

**13 unguarded HR command methods are now guarded** with the app's established in-page error pattern (Wave A / `ServicePageViewModel.CreateServiceAsync` precedent):

| ViewModel | Methods (count) | Error surface |
|---|---|---|
| `HrPageViewModel` | `CreateEmployeeAsync`, `RecordAttendanceAsync`, `CreateShiftAsync`, `AssignShiftAsync`, `RequestLeaveAsync`, `ApproveLeaveAsync`, `RejectLeaveAsync`, `CreateCommissionRuleAsync`, `GenerateCommissionsAsync`, `GeneratePayrollAsync` (**10**) | **new** `ActionErrorMessage` / `HasActionError` → `Strings.Common_ActionFailedMessage` |
| `EmployeeProfileViewModel` | `ActivateAsync`, `DeactivateAsync`, `SuspendAsync` (**3**) | **new** `ActionErrorMessage` / `HasActionError` → `Strings.Common_ActionFailedMessage` |

- **No business-behaviour change.** Each guard wraps the existing `await _commandService.X(...)` + form clears + list mutation / reload / re-select verbatim in a `try`; validation (`if null return`, `TimeSpan.TryParse` / `decimal.TryParse` early-returns), `CanExecute`, RBAC, and the success path are untouched and stay outside the `try`. The backend remains the sole write authority. No payroll / commission / attendance / leave calculation or state logic exists in these ViewModels and none was added.
- **Error UX:** on failure the command sets a fixed localized string on an inline, non-destructive error property (`ActionErrorMessage`; not `State = Error`, which replaces the whole page via `DashboardWidget`). `App.DispatcherUnhandledException` no longer fires for these 13 paths.
- **`_onChanged` semantics** (`ActivateAsync` / `DeactivateAsync` / `SuspendAsync`): `_onChanged?.Invoke()` is inside the `try` after the awaited command, so a failed lifecycle change no longer triggers a parent `HrPageViewModel` reload. `await LoadAsync()` stays inside the guarded block (self-guarded — its own catch sets `State = Error` — so it cannot propagate into the command catch), matching the Wave A `CustomerProfileViewModel.SaveChangesAsync` precedent.
- **`GenerateCommissionsAsync`**: on failure `StatusMessage` is left untouched (nothing was generated); the "Generated N …" message and its `generated.Count` branch are success-path-only and unchanged.
- **Logging:** each catch reuses the ViewModel's **existing** instance-form `[LoggerMessage]` (`LogOperationFailed(nameof(<Method>))`), operation-name-only, once. No new logger, no `ILoggerFactory` added, no DI change, no `SYSLIB1020` (both classes already `sealed partial` with a single `ILogger` field + instance-form `[LoggerMessage]`, compiled clean at `794648e`).
- **Security:** `catch (Exception)` with **no exception variable** in all 13 → `Exception.Message` / backend response body / salary / payroll-net / commission values / internal identifiers / employee PII structurally unreachable in both the on-screen message and the log. Test-enforced with seeded backend-body secrets (`"backend 500: employee Jordan Lee salary=3200 net=2870 commission=518.40 ssn=123"`).
- **Localization:** no change — `Common_ActionFailedMessage` was added in Wave A (`794648e`) across `Strings.cs` + all 3 `.resx` files.
- **Tests:** +19 (per-command failure-does-not-throw + inline-error-set, form-input / list / state preservation, `_onChanged` not-invoked-on-failure / invoked-on-retry, error-clears-on-next-success, operation-only-log no-leak). Shared HR stubs gained additive `Exception?` seams (null-path byte-identical; `GenerateCommissions` failure uses the pre-existing ctor delegate). 0 existing test bodies changed. No new test helper.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 9 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `a5be831`.
- Working tree after commit: tracked tree clean (`git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'` → empty).

---

## G. MISSING-GUARD SWEEP — TRACK PROGRESS

| Wave | Domain | Status |
|---|---|---|
| **A** — Customer / Service / Specialist write commands | backend-connected | ✅ **DONE** — `794648e` (12 methods, 5 VMs) |
| **B** — HR (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3) | fake-backed | ✅ **DONE** — `a5be831` (13 methods, 2 VMs) |
| **C** — Inventory (`InventoryPageViewModel` ×3, `InventoryProfileViewModel` ×3) + `AccountingPageViewModel.CancelInvoiceAsync` | fake-backed | **NEXT** |
| D — Organization (×4 + 2 secondary loads) + Reporting (×3) | fake-backed | pending |
| E — AI Center (`AiCenterPageViewModel` ×~12) | fake-backed | pending |
| F — Automation tabs (`Workflows`/`ScheduledJobs`/`BusinessRules` ×~7) | fake-backed | pending |
| G (P2) — Workspace / Notification / Settings / CommandPalette (~28) | local / infra | pending, low priority |

The reusable pattern (in-page `try`/`catch` + inline error property + reuse existing `[LoggerMessage]` + `Common_ActionFailedMessage`) now has two clean applications (Waves A + B).

---

## STOP

Phase 8.72 commit executed and validated. HEAD `a5be831`. Build 0/0, 2,641/2,641 tests, architecture 7/7.
**Missing-Guard Sweep Wave B (HR) complete** — 13 HR command methods now use the app's in-page error
pattern; no business-behaviour change. Checkpoint updated (`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`).
**Next: Phase 8.73 — Missing-Guard Sweep Wave C (Inventory + invoice-cancel) — Scope Audit.** Awaiting authorization.
