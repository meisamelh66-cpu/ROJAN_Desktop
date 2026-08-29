# ROJAN AI — TEAM 3 — PHASE 8.119 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 4 (AUTOMATION TABS) — COMMIT REPORT v1

**Type:** Commit execution. One commit performed. No source/test change beyond what Phase 8.116 + 8.117.1 already produced; no push / merge / rebase / amend.
**Authorization:** Phase 8.119 — APPROVED (reference `ROJAN_PHASE8_118_P2_SUBWAVE4_UPDATED_COMMIT_SCOPE_REVIEW_v1.md`).
**Branch:** `feature/team3-desktop-completion`

---

## A. GIT STATE

| | Before | After |
|---|---|---|
| HEAD | `b509054` | **`d10f9bc2ff0dd4460dcd75bf41f9e246a6b8d300`** |
| Parent | — | `b509054` |
| Branch | `feature/team3-desktop-completion` | unchanged |
| Tracked working tree | 10 modified | **clean** |
| Staged | none | none (committed) |
| Pushed? | — | **No** — local only, as required |

**Staging procedure used:** `git reset` → 10 explicit per-path `git add` (5 prod + 5 test) → review staged diff → `git commit`. **No `git add .` / `git add -A`.**

Staged diff reviewed before commit: **exactly 13 production line-pairs**, every one `- ErrorMessage = exception.Message;` / `+ ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`; plus additive test assertions (`Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, …)`), one `AssertGenericSurfaceNoLeak` helper, and 2 test-file `using Rojan.Desktop.Presentation.Localization;` additions. Nothing else.

### Commit `d10f9bc`

```
fix(desktop): sanitize automation tab error surfacing

Replace raw exception.Message on the Automation tab error surfaces with the
generic localized Strings.Common_ActionFailedMessage. Covers all 13 filtered-catch
sites across WorkflowsTabViewModel (Load/CreateDraft/Publish/RunNow/Rollback),
ScheduledJobsTabViewModel (Load/Create/RunNow), BusinessRulesTabViewModel
(Load/Create), ApprovalsTabViewModel (Load/Decide) and
AutomationDashboardTabViewModel (Load).

Only the ErrorMessage assignment changes. The
catch (Exception exception) when (exception is not OperationCanceledException)
filter, State = DashboardState.Error, LogOperationFailed(nameof(...)) calls and
the await LoadAsync() reload paths are byte-unchanged. No service, contract, DI
or .resx change.

Workflow definitions, cron expressions, business-rule conditions/actions,
approval decision comments and backend payloads no longer reach any UI surface.
Logs remain operation-name-only. No-leak assertions added to the existing
Phase 8.39 failure tests (+0 net tests).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

`10 files changed, 44 insertions(+), 13 deletions(-)`

| File | Δ |
|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Automation/WorkflowsTabViewModel.cs` | 10 (5 sites) |
| `src/Rojan.Desktop.Presentation/ViewModels/Automation/ScheduledJobsTabViewModel.cs` | 6 (3 sites) |
| `src/Rojan.Desktop.Presentation/ViewModels/Automation/BusinessRulesTabViewModel.cs` | 4 (2 sites) |
| `src/Rojan.Desktop.Presentation/ViewModels/Automation/ApprovalsTabViewModel.cs` | 4 (2 sites) |
| `src/Rojan.Desktop.Presentation/ViewModels/Automation/AutomationDashboardTabViewModel.cs` | 2 (1 site) |
| `tests/…/Automation/WorkflowsTabViewModelTests.cs` | 13 (helper + 5 calls) |
| `tests/…/Automation/ScheduledJobsTabViewModelTests.cs` | 6 (3 tests) |
| `tests/…/Automation/BusinessRulesTabViewModelTests.cs` | 4 (2 tests) |
| `tests/…/Automation/ApprovalsTabViewModelTests.cs` | 5 (`using` + 2 tests) |
| `tests/…/Automation/AutomationDashboardTabViewModelTests.cs` | 3 (`using` + 1 test) |

---

## B. AUTOMATION 13 / 13 CLOSURE

`grep -rn "exception.Message" src/Rojan.Desktop.Presentation/ViewModels/Automation/` → **(none)** at `d10f9bc`.

| # | VM · method | `State = Error` | `when` filter | `LogOperationFailed` |
|---|---|---|---|---|
| 1–5 | `WorkflowsTabViewModel` · `LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync` | LoadAsync ✅ | ✅ byte-unchanged | ✅ per-method `nameof` |
| 6–8 | `ScheduledJobsTabViewModel` · `LoadAsync` / `CreateAsync` / `RunNowAsync` | LoadAsync ✅ | ✅ | ✅ |
| 9–10 | `BusinessRulesTabViewModel` · `LoadAsync` / `CreateAsync` | LoadAsync ✅ | ✅ | ✅ |
| 11–12 | `ApprovalsTabViewModel` · `LoadAsync` / `DecideAsync` | LoadAsync ✅ | ✅ | ✅ |
| 13 | `AutomationDashboardTabViewModel` · `LoadAsync` | ✅ | ✅ | ✅ |

**13 / 13 sanitized.** `AutomationPageViewModel` correctly untouched (parent orchestrator — no error surface / no failure boundary). Sub-wave 4 is complete.

**Unchanged (verified in staged diff):** the `catch (Exception exception) when (exception is not OperationCanceledException)` clause at every site (the `when` predicate keeps `exception` bound — unused in body, no compiler warning), every `State = DashboardState.Error`, every `LogOperationFailed(nameof(<Method>))`, both `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Automation … operation failed. Operation={Operation}")]` signatures, the `await LoadAsync()` success-path reloads, all business logic.

---

## C. SECURITY IMPROVEMENT

Every Automation catch body now assigns the fixed localized constant `Strings.Common_ActionFailedMessage` (fa/en/ar all shipped since Wave A `794648e`). `exception.Message` / `.ToString()` / `.InnerException` is structurally unreachable from every bound `ErrorMessage` TextBlock across all 5 Automation tabs.

| Data class | Previously reachable via | Now |
|---|---|---|
| Workflow definitions (step names, descriptions, trigger config) | Workflows `CreateDraftAsync` / `PublishAsync` / `RollbackAsync` | **not reachable** — sentinel `workflow-definition-SECRET-vip` asserted absent |
| Cron expressions | `ScheduledJobsTabViewModel.CreateAsync` | **not reachable** — sentinel `cron-0-9-star-star-1-SECRET` asserted absent |
| Business-rule conditions & actions (field/operator/value, discount %, target workflow id) | `BusinessRulesTabViewModel.CreateAsync` | **not reachable** — sentinel `IF-Customer-is-VIP-SECRET` asserted absent |
| Approval rules / decision comments (free-text manager notes — payroll figures, disciplinary detail, PII) | `ApprovalsTabViewModel.DecideAsync` | **not reachable** — sentinel `approval-comment-SECRET-payroll` asserted absent |
| Dashboard automation data (workflow names via summary + recent-executions strip) | `AutomationDashboardTabViewModel.LoadAsync` | **not reachable** — sentinel `workflow-name-SECRET-9f3` asserted absent |
| Execution details (run detail, org·branch·user ids, execution ids) | Workflows/ScheduledJobs `RunNowAsync`, all `LoadAsync` | **not reachable** — generic constant |
| Backend payloads / internal hosts / file paths / DB fragments | all 13 | **not reachable** — generic constant |

**Logs remain operation-name-only.** All 13 sites call `LogOperationFailed(nameof(<Method>))` — the exception object is never passed to the logger. The Phase 8.39 log no-leak assertions (`Contains("Operation=<Method>", entry.Message)` + `DoesNotContain(Secret, entry.Message)`) are retained and green.

This is distinct from Missing-Guard Sweep Wave F (`7c9c132`), which added *new* filtered guards to the same files. Phase 8.119 changes only the *message string* in *pre-existing* filtered catches — no new guard, no behaviour change, no filter change.

---

## D. TEST STATUS — post-commit at `d10f9bc`

| Gate | Expected | Actual |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Full suite | 2,715 / 2,715 | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain | 456 | 456 ✅ |
| — Presentation | 772 | **772** (no net-new — assertions on existing tests) ✅ |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Automation subset (`FullyQualifiedName~Automation`) | closed | **54 / 54 PASS**, no `= exception.Message` remaining ✅ |

Suite progression: 2,715 (`b509054`) → **2,715** (`d10f9bc`, +0 — P2 sub-wave 4 adds no-leak assertions to existing Phase 8.39 tests).

---

## E. REMAINING P2 WAVES

The "sanitize load-error surfacing" P2 track (`ROJAN_PHASE8_102_*` — 58 Category-A `= exception.Message` UI surfaces / 30 VMs, ~6 domain sub-waves):

| Sub-wave | Scope | Status |
|---|---|---|
| 1 | Reporting + AI Center + Accounting/POS (11 sites) | ✅ `76d3f61` (Phase 8.104, committed 8.106) |
| 2 | Customers + HR + Membership (6 of 7 sites) | ✅ `1260d4e` (Phase 8.108, committed 8.110) — `CustomerProfileViewModel.LoadAsync` (site 7) deferred |
| 3 | Organization + Specialists + Services (8 sites / 7 VMs) | ✅ `b509054` (Phase 8.112, committed 8.114) |
| **4** | **Automation tabs (13 / 13 sites / 5 tab VMs)** | ✅ **`d10f9bc` (Phase 8.116 + 8.117.1, committed 8.119)** |
| 5 | Booking + Calendar + Inventory | **remaining** — `BookingPageViewModel` ×5, `CalendarPageViewModel` ×3, `InventoryPageViewModel` ×2, `InventoryProfileViewModel` ×1 = ~11 sites |
| 6 | Dashboard + Analytics + Salon + QR + Support + Settings | **remaining** — ~8–10 sites, incl. `SettingsPageViewModel`'s 2 `NotSupportedException`→`StatusMessage` branches (Category D, optional) **+ `CustomerProfileViewModel.LoadAsync`** carried over from sub-wave 2 |

**Also outstanding (documented, not authorized):** the 3 local-only infra VMs (`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`) as **P3**; the Phase 8.99.1 `SettingsPage.xaml` visibility-trigger tweak.

`LoginViewModel` / `MobileOtpLoginViewModel` are already correct (typed `ApiException` catches → `Strings.Login_*`).

---

## F. CHECKPOINT

`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` updated: §A HEAD `b509054` → `d10f9bc` + banner + audit-phase list (+8.117/8.118) + commit chain; §B commit table (+`d10f9bc` row); §E build/test (`d10f9bc`, 2,715 → 2,715 +0) + progression line; §F Phase 8.116 + 8.117.1 detail bullet; §G P2 track (sub-wave 4 ✅ 13/13; sub-waves 5–6 + `CustomerProfileViewModel` remain); §H items 1/2/5/6; STOP update-history (Phases 8.117/8.118 review note + Phase 8.119 commit entry). No code changed in performing the checkpoint update.

---

## STOP

Phase 8.119 commit execution complete. **HEAD `d10f9bc`** (`fix(desktop): sanitize automation tab error surfacing`), parent `b509054`, branch `feature/team3-desktop-completion`, **not pushed**. Tracked working tree clean.

**Sub-wave 4 complete — 13 / 13 Automation error surfaces sanitized.** Only `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;` at each; `when` filter / `State = Error` / `LogOperationFailed` / `await LoadAsync()` reloads byte-unchanged; no `using` (prod) / `.resx` / DI / service / contract / stub change. Workflow definitions, cron expressions, business-rule conditions/actions, approval decision comments, dashboard workflow names, execution details and backend payloads no longer reach any UI surface; logs operation-name-only. Build 0/0, 2,715 / 2,715 tests pass, Architecture 7/7, Automation subset 54/54. +0 net tests.

**P2 remaining: sub-wave 5 (Booking + Calendar + Inventory) and sub-wave 6 (Dashboard + Analytics + Salon + QR + Support + Settings + `CustomerProfileViewModel.LoadAsync`).**

**Awaiting next authorization block.**
