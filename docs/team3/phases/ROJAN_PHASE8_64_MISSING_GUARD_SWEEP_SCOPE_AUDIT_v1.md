# ROJAN AI — TEAM 3 — PHASE 8.64 — MISSING-GUARD SWEEP — SCOPE AUDIT v1

**Type:** Audit only. **No source change. No test change. No guard / dialog / service change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `5ba554ceb588e5780b87aebdf280538f6b25c485` — `fix(desktop): drop exception payload from diagnostic logging` (Phase 8.61, committed 8.63)
**Reference:** `ROJAN_PHASE8_63_LEGACY_LOGGERMESSAGE_COMMIT_REPORT_v1.md`, `ROJAN_PHASE8_59_FINAL_LOGGING_CLOSURE_AUDIT_v1.md` §E.2, `ROJAN_PHASE8_54_REMAINING_VIEWMODEL_GAP_AUDIT_v1.md` §F P1.1, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F.

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `5ba554ceb588e5780b87aebdf280538f6b25c485` |
| HEAD subject | `fix(desktop): drop exception payload from diagnostic logging` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. This audit adds no code.

---

## B. COMMAND FAILURE AUDIT

### B.1 What happens today when an unguarded command method throws

`Presentation/Mvvm/AsyncRelayCommand.Execute` is `async void` with a bare `try { await _execute(p); } finally { _isExecuting = false; … }` — **no `catch`**. So when a bound command method awaits a service call that throws and has no `try`/`catch` of its own:

1. the exception propagates out of `_execute(parameter)`;
2. `finally` still runs (re-entrancy flag reset);
3. the fault surfaces as an **unhandled `async void` exception** → WPF `Dispatcher.UnhandledException`;
4. `App.xaml.cs`'s `DispatcherUnhandledException` handler **logs it** (`LogUnhandledException` — the one place an `Exception` is still logged, by design), **shows the app's generic modal error dialog**, and sets `e.Handled = true` → **the app recovers. No crash. No data corruption** (the backend is the sole authority — a failed write simply did not happen; failed local list refreshes leave stale-but-valid data).

**The gap is UX/reliability consistency, not stability:** a backend/network failure on any of these user-triggered commands shows a *generic, developer-worded, modal* dialog instead of this app's established **in-page, contextual, non-destructive, retryable** error pattern (`ErrorMessage`/`State`, or `SaveErrorMessage`/`HasSaveError`, or `AssignmentErrorMessage`, etc.) that every guarded command already uses.

### B.2 Inventory — unguarded user-triggered command methods

Scan: every `AsyncRelayCommand`/`RelayCommand`-bound `…Async` method that `await`s a `_xService`/`_xEngine`/`_xClient`/`_xStore` call and has **no `try`/`catch`** (directly or transitively — methods that only `await` a self-guarding `LoadAsync()` are excluded).

| ViewModel | Domain | Unguarded command methods | Existing error surface available? | User impact |
|---|---|---|---|---|
| `Customers/CustomerPageViewModel` | Customer (backend) | `CreateCustomerAsync` | has `ErrorMessage`/`State` (Load) + `NewCustomerFullName` form | generic dialog on a failed customer create; typed input context lost |
| `Customers/CustomerProfileViewModel` | Customer (backend) | `AddNoteAsync`, `AddTagAsync`, `RemoveTagAsync`, `SaveChangesAsync` | **`LoadAsync` catch only** — no save/action error property; needs one | generic dialog on note/tag/edit-save failure |
| `Services/ServiceProfileViewModel` | Service (backend) | `AssignSpecialistAsync`, `UnassignSpecialistAsync` | has `SaveErrorMessage`/`HasSaveError` (save/deactivate) — reusable | generic dialog on assign/unassign failure |
| `Specialists/SpecialistProfileViewModel` | Specialist (backend) | `AddSkillAsync`, `RemoveSkillAsync` | has `SaveErrorMessage`, `AssignmentErrorMessage` — reusable | generic dialog on skill add/remove failure |
| `Specialists/SpecialistPageViewModel` | Specialist (backend) | `CreateSpecialistAsync` | has `ErrorMessage`/`State` (Load) + form fields | generic dialog on a failed specialist create |
| `HR/HrPageViewModel` | HR (fake-backed) | `CreateEmployeeAsync`, `RecordAttendanceAsync`, `CreateShiftAsync`, `AssignShiftAsync`, `RequestLeaveAsync`, `ApproveLeaveAsync`, `RejectLeaveAsync`, `CreateCommissionRuleAsync`, `GenerateCommissionsAsync`, `GeneratePayrollAsync` | has `ErrorMessage`/`State` + `StatusMessage` | **10 methods** — generic dialog on every HR write |
| `HR/EmployeeProfileViewModel` | HR (fake-backed) | `ActivateAsync`, `DeactivateAsync`, `SuspendAsync` | **`LoadAsync` catch only** — needs an action error property | generic dialog on lifecycle change |
| `Inventory/InventoryPageViewModel` | Inventory (fake-backed) | `CreateProductAsync`, `AddCategoryAsync`, `AddSupplierAsync` | has `ErrorMessage`/`State` + form fields | generic dialog on inventory create |
| `Inventory/InventoryProfileViewModel` | Inventory (fake-backed) | `RecordTransactionAsync`, `MapServiceAsync`, `UnmapServiceAsync` | **`LoadAsync` catch only** — needs an action error property | generic dialog on stock/mapping actions |
| `Accounting/AccountingPageViewModel` | Accounting (fake-backed) | `CancelInvoiceAsync` (known backlog item — Phase 8.10) | has `ErrorMessage`/`State` | generic dialog on invoice cancel |
| `Organizations/OrganizationPageViewModel` | Org (fake-backed) | `CreateOrganizationAsync`, `CreateBranchAsync`, `SaveBranchSettingsAsync`, `SwitchRoleAsync`, + secondary loads `LoadBranchesForSelectedOrganizationAsync` / `LoadSettingsForSelectedBranchAsync` (selection-setter-triggered) | has `ErrorMessage`/`State`; branch-settings has its own save area | **`SwitchRoleAsync` mutates the session** — a failed role switch through the generic dialog is especially confusing |
| `Reporting/ReportingPageViewModel` | Reporting (fake-backed) | `ReloadSnapshotsAsync`, `ToggleSavedAsync`, `DeleteSnapshotAsync` | has `ErrorMessage`/`State` (Load/Run) | generic dialog on snapshot save-toggle / delete |
| `AI/AiCenterPageViewModel` | AI (fake-backed) | `ReloadSessionsAsync`, `EnsureActiveSessionAsync`, `LoadMessagesAsync`, `NewConversationAsync`, `OpenConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `SearchHistoryAsync`, `ClearHistoryAsync`, `ExportSessionAsync`, `SaveSettingsAsync`, `SaveConfigurationAsync` | has `ErrorMessage`/`State` (Load) + a chat error area (SendMessage) | **~12 methods** — mostly local history ops; low failure rate |
| `Automation/WorkflowsTabViewModel` | Automation (fake-backed) | `LoadVersionHistoryAsync`, `ArchiveAsync`, `DeleteAsync` | has `ErrorMessage`/`State` (filtered catches on Load/Create/Publish/RunNow/Rollback) | inconsistent — some tab actions guarded, some not |
| `Automation/ScheduledJobsTabViewModel` | Automation (fake-backed) | `ToggleEnabledAsync`, `DeleteAsync` | as above | as above |
| `Automation/BusinessRulesTabViewModel` | Automation (fake-backed) | `ToggleEnabledAsync`, `DeleteAsync` | as above | as above |
| `Settings/SettingsPageViewModel` | Local / infra | `ApplyLanguageAsync`, `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync` | inline status area | local services — near-zero failure rate |
| `Notifications/NotificationCenterViewModel` | Local / infra | `MarkAsReadAsync`, `DismissAsync`, `MarkAllReadAsync`, `ClearAllAsync`, `RefreshAsync` | none | local notification store — near-zero failure rate |
| `Workspaces/WorkspaceHostViewModel` | Local / infra | ~15 (`SplitAsync`, `CloseTabAsync`, `ClosePaneAsync`, `FloatOutAsync`, `ToggleDockPanelAsync`, `OpenModuleInPaneAsync`, `ResizeSplitAsync`, `ConfirmNameAsync`, `SwitchWorkspaceAsync`, `DuplicateWorkspaceAsync`, `DeleteWorkspaceAsync`, `ResetWorkspaceAsync`, `RefreshWorkspaceListsAsync`, `SaveAsync`, …) | none | local layout-persistence store — near-zero failure rate; generic dialog on a workspace-split failure is rare but jarring |
| `Search/CommandPaletteViewModel` | Local / infra | `RefreshResultsAsync`, `ExecuteAsync`, `ToggleFavoriteAsync`, `ClearHistoryAsync` | none (overlay) | local favorites/history stores + in-memory index |

**Total: ~80 unguarded user-triggered command methods across ~19 ViewModels.**

### B.3 Severity

| Severity | Finding |
|---|---|
| **P0 — crash / data corruption** | **NONE.** `App.DispatcherUnhandledException` recovers every one (logged, dialog, `e.Handled = true`). The backend is the sole write authority — a failed command does not persist partial state. The one real correctness risk (`PosCheckoutViewModel.ChargeAsync` double-charge-on-retry) is in an **already-guarded** method and is separately tracked / backend-idempotency-blocked. |
| **P1 — user-facing reliability** | **The whole sweep.** ~80 user-triggered command methods surface a backend/network failure through the *generic global modal dialog* instead of the app's in-page error pattern. Inconsistent with every guarded command in the same VMs. Real polish/reliability issue for a production client. Ranked into waves in §E. |
| **P2 — UX improvement** | Global dialog message quality (developer-worded); the ~28 infra-VM methods (`WorkspaceHost`, `NotificationCenter`, `Settings`, `CommandPalette`) — local services, failure rate near zero, low value; CancellationToken (§D). |

---

## C. GLOBAL ERROR UX — FAILURE-PATH ANALYSIS

### C.1 The three failure paths in use today

| Path | Where | Shape |
|---|---|---|
| **In-page `ErrorMessage`/`State`** | every guarded `LoadAsync`/`SearchAsync`; guarded creates (`ServicePage.CreateService`, `SalonPage.CreateSalon`, `QrCodes.GenerateReceptionInvite`, Automation Load/Create/RunNow) | `DashboardWidget` shows an inline error + Retry; page content replaced |
| **In-page action error** (`SaveErrorMessage`/`HasSaveError`, `AssignmentErrorMessage`/`HasAssignmentError`, `MessageError`/`ApplicationError`, `InputErrorMessage`, `LookupErrorMessage`) | `ServiceProfile.SaveChanges/Deactivate`, `SpecialistProfile.Save/Assign/Remove`, `Support.Submit*`, `AcceptInvite`, `SpecialistSchedule.TryMutateAsync`, `BookingWizard.*` | inline, non-destructive — the form/action area shows the error, the rest of the panel stays usable |
| **Global `DispatcherUnhandledException` dialog** | `App.xaml.cs` — the ~80 unguarded methods land here | generic modal `MessageBox`, developer-worded, `e.Handled = true`, app continues |

### C.2 The inconsistency

Within a single ViewModel, some commands use path 1/2 and others fall through to path 3 — e.g. `HrPageViewModel.LoadAsync` shows an inline error + Retry, but `HrPageViewModel.CreateEmployeeAsync` shows the generic global dialog. `CustomerProfileViewModel.LoadAsync` is inline; `CustomerProfileViewModel.SaveChangesAsync` is global. This is the target of the sweep: **every user-triggered command failure should use path 1 or 2** (the in-page pattern), reserving path 3 for genuinely unexpected faults.

### C.3 Classification

| | |
|---|---|
| **P0 — crash / data corruption** | none (§B.3) |
| **P1 — reliability** | make the ~80 command methods use the in-page pattern (waves, §E) |
| **P2 — UX** | improve the global dialog copy; add a "the action may not have completed — reload to check" affordance for commands that also refresh |

---

## D. CANCELLATIONTOKEN FINDINGS

| Component | State today | Impact | Priority |
|---|---|---|---|
| `Search/CommandPaletteViewModel` — `RefreshResultsAsync` / `GetAllCandidatesAsync` | the `_searchIndexService` / `_favoritesStore` / `_historyStore` methods **accept** a `CancellationToken`, but the VM calls them with `default` and creates **no per-keystroke CTS** — rapid typing starts overlapping index scans with no cancellation and no `_filterVersion`-style stale guard | wasted work; on the *last* keystroke a slow stale scan can leave briefly-wrong results until the next input | **P2** (Phase 8.2's "highest-value CT item" — but functionally an annoyance, not a correctness bug: ranking is deterministic and self-corrects) |
| Page-search VMs (`Customer`/`Service`/`Inventory`/`Booking`/`Specialist` pages, `Accounting.SearchAsync`) | use a `_filterVersion` staleness guard — stale results are **discarded correctly**, but the superseded backend call still runs to completion | wasted backend round-trips during fast typing; no incorrect UI | **P2** (efficiency) |
| `Reporting/ReportingPageViewModel.RunReportAsync` | **already threads a `CancellationToken`** to `_executionQueryService.RunReportAsync(id, filters, token)` | the one genuinely long-running op already supports cancellation | ✅ done |
| `BookingWorkflow/BookingWizardViewModel.SearchNextAvailableDateAsync` | **already** uses `_nextAvailableDateSearchCts` + cancel-on-supersede | ✅ done |
| `Notifications/NotificationCenterViewModel.RefreshAsync` | has a `CancellationToken` param; call chain likely passes `default` at the command binding | local store; negligible | P3 |
| `WorkspaceHostViewModel`, `Settings` | synchronous-ish local persistence; no long ops | negligible | — |

**No P0 / P1 CancellationToken finding.** The `_filterVersion` guard already prevents incorrect UI everywhere it matters; the only genuinely long op (report execution) already supports cancellation. CT work is **P2 efficiency + CommandPalette polish**.

---

## E. PRIORITY ROADMAP

### E.1 P0 — must fix

**None.**

### E.2 P1 — missing-guard sweep (ranked waves)

Each wave: for every listed method, wrap the service `await`(s) in a `try`/`catch (Exception exception)` in the established pattern, surface the failure through the VM's existing in-page error property (or add a minimal one where noted), and append `LogOperationFailed(nameof(<Method>))` reusing the VM's **existing** `[LoggerMessage]` (already present from the logging track — no new logging plumbing). Where a method does `await command; await LoadAsync()`, wrap **only the command await** (or word the message "the action may not have completed") so a post-command reload failure — which `LoadAsync` already self-guards — is not mis-attributed.

| Wave | ViewModels | Methods (≈) | Est. files (prod / test) | Test requirements | Commit grouping |
|---|---|---|---|---|---|
| **A — Customer / Service / Specialist writes** (backend-connected, RBAC-gated) | `CustomerPageViewModel`, `CustomerProfileViewModel`, `ServiceProfileViewModel`, `SpecialistProfileViewModel`, `SpecialistPageViewModel` | **~12** | 5 / 5 | per method: failure → in-page error set (not thrown); `State`/form/selection not corrupted; `[LoggerMessage]` still fires `Operation=<method>`; no exception/PII in log. `CustomerProfileViewModel` / needs a new `ActionErrorMessage`/`HasActionError` pair (note+tag+save) — test its visibility flag | **one commit** — `fix(desktop): guard customer/service/specialist command failures` |
| **B — HR** | `HrPageViewModel` (10), `EmployeeProfileViewModel` (3) | **~13** | 2 / 2 | as A; `HrPageViewModel` reuses `ErrorMessage`/`StatusMessage`; `EmployeeProfileViewModel` needs an action error property | **one commit** — `fix(desktop): guard HR command failures` |
| **C — Inventory + Accounting** | `InventoryPageViewModel` (3), `InventoryProfileViewModel` (3), `AccountingPageViewModel` (`CancelInvoiceAsync`) | **~7** | 3 / 3 | as A; `InventoryProfileViewModel` needs an action error property; `CancelInvoiceAsync` reuses `ErrorMessage`/`State` | **one commit** — `fix(desktop): guard inventory and invoice-cancel command failures` |
| **D — Organization + Reporting** | `OrganizationPageViewModel` (4 cmd + 2 secondary loads), `ReportingPageViewModel` (3) | **~9** | 2 / 2 | as A; `SwitchRoleAsync` failure must leave `SelectedRoleToSwitchTo` / session state consistent (test-assert) | **one commit** — `fix(desktop): guard organization and reporting command failures` |
| **E — AI Center** | `AiCenterPageViewModel` (~12) | **~12** | 1 / 1 | as A; most are local history ops — a shared inline error area is fine | **one commit** — `fix(desktop): guard AI Center command failures` |
| **F — Automation tabs** | `WorkflowsTabViewModel` (3), `ScheduledJobsTabViewModel` (2), `BusinessRulesTabViewModel` (2) | **~7** | 3 / 3 | match the tabs' existing `catch (Exception) when (exception is not OperationCanceledException)` shape on Toggle/Delete/Archive/VersionHistory | **one commit** — `fix(desktop): guard remaining automation tab command failures` |

**P1 sweep total: ~60 methods, ~16 production files, ~16 test files, 6 commits.** Waves A–F; each is its own audit → scope-review → implement → commit-review → commit cycle (or a lighter audit-folded-into-scope-review for the well-understood ones after Wave A sets the pattern).

### E.3 P2 — improvements

| Item | Scope |
|---|---|
| **Infra-VM guards** — `WorkspaceHostViewModel` (~15), `NotificationCenterViewModel` (5), `Settings` (4), `CommandPaletteViewModel` (~4) | ~28 methods, 4 VMs. Local services (layout / notification / settings / favorites stores) — failure rate near zero. Lowest value in the sweep. Own late wave `fix(desktop): guard workspace/notification/settings command failures`. |
| **CancellationToken** — `CommandPaletteViewModel` search first (per-keystroke CTS + cancel-on-supersede, reusing the `BookingWizardViewModel` precedent); then thread a token through page-search `_filterVersion` reloads for efficiency | design-heavier; own audit. |
| **Global dialog copy** — friendlier `App.xaml.cs` `DispatcherUnhandledException` message; optional "reload to verify" affordance | small, Shell-only. |
| **Startup UX** — progress indicator across `App.OnStartup`'s blocking stages (the critical session path is already Retry/Exit-guarded via `InitializeSessionWithRetry`; `_host.StartAsync().GetAwaiter().GetResult()` is the one remaining unguarded startup await) | Shell + a startup progress VM; own audit. |
| Dead-code cleanup — Calendar's 3 dead EF tables; `RolePermissions` dead enum members | disclosed tech debt. |

### E.4 Blocked (upstream — not Team 3 actionable)

| Item | Blocker |
|---|---|
| Inventory / HR / Accounting **backend integration** | Backend has zero Inventory code (Phase 8.0); `Fake*Repository` for all three. (The Wave B/C guards are still worth doing — the pattern must be right for the eventual connection.) |
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry | backend payment-idempotency unverified from this codebase — documented, gates Accounting's eventual connection. |

---

## F. NEXT PHASE RECOMMENDATION — **Phase 8.65: Missing-Guard Sweep Wave A — Implementation Scope Review**

| Field | Detail |
|---|---|
| **Goal** | Bring the **backend-connected** Customer / Service / Specialist write commands to the app's in-page error pattern — the highest-value, most-consistent subset of the sweep — and set the reusable pattern for Waves B–F. |
| **Scope — production (5 files)** | `Customers/CustomerPageViewModel.cs` (`CreateCustomerAsync`), `Customers/CustomerProfileViewModel.cs` (`AddNoteAsync`, `AddTagAsync`, `RemoveTagAsync`, `SaveChangesAsync` — **+ a new `ActionErrorMessage` / `HasActionError` inline pair**), `Services/ServiceProfileViewModel.cs` (`AssignSpecialistAsync`, `UnassignSpecialistAsync` — reuse `SaveErrorMessage`/`HasSaveError`), `Specialists/SpecialistProfileViewModel.cs` (`AddSkillAsync`, `RemoveSkillAsync` — reuse `AssignmentErrorMessage`/`HasAssignmentError`), `Specialists/SpecialistPageViewModel.cs` (`CreateSpecialistAsync` — reuse `ErrorMessage`/`State`). ~12 methods. |
| **Per method** | wrap the `await _commandService.X(...)` (and, where it follows, the `await LoadAsync()`) in `try { … } catch (Exception exception) { <surface via in-page error property>; LogOperationFailed(nameof(<Method>)); }` using the VM's **existing** `[LoggerMessage]`. No `#pragma`/new-catch beyond the established `#pragma warning disable CA1031` boundary comment. Preserve every success-path behaviour (form clears, `LoadAsync` reload, selection). |
| **Scope — tests (5 files)** | the 5 corresponding `*ViewModelTests.cs`. Per method: a failure test asserting (a) no exception thrown out of `Command.Execute`, (b) the correct in-page error property is set, (c) `State`/form/selection is not corrupted, (d) the `[LoggerMessage]` fires `Operation=<method>` with no exception/PII leak (reuse `RecordingLogger<T>`). ~+15 tests. `CustomerProfileViewModel` also: `HasActionError` visibility flag + clears on next successful action. **No shared-stub change** — the command stubs already carry per-operation `Exception?` hooks (`UpdateCustomerException`, `AssignSpecialistException`, `AddSkillException`, `CreateSpecialistException`, etc. — verify each; add a private nested throwing stub only where genuinely missing). |
| **NOT touched** | `AsyncRelayCommand`, `App.xaml.cs`, DI, Domain, backend contracts, RBAC, authentication, navigation, the HR / Inventory / Accounting / Org / Reporting / AI / Automation / infra VMs (Waves B–G), any `[LoggerMessage]` signature (logging track is closed). |
| **Risk** | **LOW-MEDIUM.** Additive `try`/`catch` around existing awaits; no service, DI, or command-infrastructure change. Main design point: word the catch so a post-command `LoadAsync` failure (already self-guarded) is not mis-attributed — wrap only the command await, or use a neutral "the action may not have completed" message. `CustomerProfileViewModel` gains one new bindable property pair (additive, no ctor change). |
| **Validation** | build 0 warnings / 0 errors; full suite ~2,609 → ~2,624; architecture 7/7. |
| **Commit** | one isolated commit — `fix(desktop): guard customer/service/specialist command failures`. Standard rhythm: 8.65 scope review → 8.66 implementation (STOP before commit) → 8.67 commit scope review → 8.68 commit execution → checkpoint update. Then Waves B–F as follow-on phases. |

---

## STOP

Audit complete. **No P0.** The missing-guard sweep is a **P1 reliability/UX-consistency** item — ~80 unguarded user-triggered command methods across ~19 ViewModels surface backend failures through the generic global dialog instead of the app's in-page error pattern (all recovered by `App.DispatcherUnhandledException`, no crash / no data corruption). Ranked into 6 P1 waves (A–F, ~60 methods, backend/business domains) + a P2 infra wave (~28 methods) + P2 CancellationToken/Startup items. No source or test change, no commit/push/merge/rebase/amend. HEAD remains `5ba554c`. **Awaiting Phase 8.65 authorization** (recommended: Missing-Guard Sweep Wave A — Customer/Service/Specialist write commands).
