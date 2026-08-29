# ROJAN AI — TEAM 3 — PHASE 8.76 — MISSING-GUARD SWEEP WAVE C (INVENTORY + ACCOUNTING) — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `a5be83142bbe411beda3daaa115fd18d528bcdf2`
**New HEAD:** `66c849077ffc0168de75335ec904fb7a0f2d7bea`
**Commit subject:** `fix(desktop): guard inventory and invoice-cancel command failures`

---

## A. COMMIT

```
commit 66c849077ffc0168de75335ec904fb7a0f2d7bea
Author: Meisam Elhaee <meisamelh66@gmail.com>
Date:   Fri Aug 28 11:07:00 2026 -0700

    fix(desktop): guard inventory and invoice-cancel command failures

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

Subject EXACT as authorized. Trailers match the Team 3 arc convention.

```
git log --oneline -4
66c8490 fix(desktop): guard inventory and invoice-cancel command failures
a5be831 fix(desktop): guard HR command failures
794648e fix(desktop): guard customer/service/specialist command failures
5ba554c fix(desktop): drop exception payload from diagnostic logging
```

---

## B. STAGING (explicit-path only)

```
git reset
git add <7 explicit paths>            # never git add . / git add -A
git diff --cached --name-only         # 7
```

| Group | Files |
|---|---|
| Production VMs (3) | `ViewModels/Inventory/InventoryPageViewModel.cs`, `ViewModels/Inventory/InventoryProfileViewModel.cs`, `ViewModels/Accounting/AccountingPageViewModel.cs` |
| Test stub (1) | `Inventory/StubInventoryCommandService.cs` |
| Test VMs (3) | `Inventory/InventoryPageViewModelTests.cs`, `Inventory/InventoryProfileViewModelTests.cs`, `Accounting/AccountingPageViewModelTests.cs` |

`git show --stat 66c8490`: **7 files changed, 508 insertions(+), 35 deletions(-)**. No new file. The 35 deletions are entirely original single-line command bodies re-indented into their `try`-wrapped form — no property, validation, service call, or assertion removed. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

| Area | Status |
|---|---|
| `PosCheckoutViewModel` / `ChargeAsync` (double-charge risk) | ✅ untouched (not in commit) — `AccountingPageViewModel.OpenPosCheckout`'s `new PosCheckoutViewModel(...)` line is an unchanged context line |
| Payment services (`IPaymentCommandService` / `IPaymentQueryService` + impls + `FakeAccountingRepository`) | ✅ untouched |
| Invoice services (`IInvoiceCommandService` / `IInvoiceQueryService`) | ✅ untouched |
| Inventory services (`IInventoryCommandService` / `IProductQueryService` / `IProductProfileQueryService` / `IInventoryQueryService`) + impls + `FakeInventoryRepository` | ✅ untouched |
| Backend contracts / HTTP clients / API layer | ✅ untouched |
| DTOs / request records (all Inventory & Accounting) | ✅ untouched |
| RBAC / permission gates / `CanExecute` predicates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / shell / `IDialogService` | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| `AsyncRelayCommand` / `RelayCommand` / `ViewModelBase` / `App.xaml.cs` | ✅ untouched |
| `InvoiceProfileViewModel` | ✅ untouched |
| `Strings.cs` / all `.resx` (`Common_ActionFailedMessage` already ships in `794648e`) | ✅ untouched |
| Every `[LoggerMessage]` signature (Inventory instance-form ×2, Accounting static-form) | ✅ untouched |
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
| Rojan.Desktop.Presentation.Tests | 711 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,654** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,654 / 2,654 PASS | 2,654 / 2,654 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,641 (`a5be831`) → **2,654** (`66c8490`), delta **+13** (all `Presentation.Tests`, 698 → 711).

---

## E. WHAT LANDED

**7 unguarded command methods are now guarded** with the app's established in-page error pattern (Wave A/B / `ServicePageViewModel.CreateServiceAsync` precedent):

| ViewModel | Methods (count) | Error surface |
|---|---|---|
| `InventoryPageViewModel` | `CreateProductAsync`, `AddCategoryAsync`, `AddSupplierAsync` (**3**) | **new** `ActionErrorMessage` / `HasActionError` → `Strings.Common_ActionFailedMessage` |
| `InventoryProfileViewModel` | `RecordTransactionAsync`, `MapServiceAsync`, `UnmapServiceAsync` (**3**) | **new** `ActionErrorMessage` / `HasActionError` → `Strings.Common_ActionFailedMessage` |
| `AccountingPageViewModel` | `CancelInvoiceAsync` (**1**) | **new** `ActionErrorMessage` / `HasActionError` → `Strings.Common_ActionFailedMessage`; logs via existing **static-form** `LogOperationFailed(_logger, nameof(CancelInvoiceAsync))` |

### E.1 Inventory reliability status

All 6 Inventory write commands (page + profile) now surface a backend failure inline instead of via the generic global dialog. **Stock consistency preserved:** every `InventoryProfileViewModel` write (`RecordTransaction` / `MapService` / `UnmapService`) is followed by the authoritative `await LoadAsync()` **inside** the guarded block — on failure that reload never runs, so `Stock` / `RecentTransactions` / `ServiceMappings` keep their last-known-good backend values (no stale on-screen count). `AddCategory` / `AddSupplier` append the returned DTO only after the await succeeds → a failure adds nothing. **No manual inventory state recovery was added — none is needed.** Inventory remains fake-backed (`FakeInventoryRepository`); the guards are correct-by-construction ahead of the eventual backend contract.

### E.2 Accounting — `CancelInvoiceAsync` hardening

The Phase 8.10 / checkpoint §F backlog item ("`CancelInvoiceAsync` — no try/catch; a throw becomes an unobserved task exception caught by `App`'s surface") is now closed. The guard wraps the existing single `await _invoiceCommandService.CancelInvoiceAsync(invoiceId)` + `await LoadAsync()` + re-select verbatim; the `if (SelectedInvoice is null) return;` and `var invoiceId = …` lines stay outside the `try`. **No invoice-cancellation logic, payment flow, rollback behaviour, or transaction rule changed** — `CancelInvoiceAsync` is not a payment operation; `PosCheckoutViewModel` / `ChargeAsync` / `IPaymentCommandService` are untouched; no retry loop and no idempotency assumption is introduced. The command stays gated by `CanExecute` (`SelectedInvoice is not null && Status != InvoiceStatus.Cancelled`). The catch reuses the class's existing static-form `[LoggerMessage]` exactly as `LoadAsync` / `SearchAsync` already call it.

### E.3 Cross-cutting

- **No business-behaviour change.** Each guard wraps existing flow only; validation, `CanExecute`, RBAC, success path, and the authoritative reload are untouched. The backend remains the sole write authority.
- **Error UX:** on failure the command sets a fixed localized string on an inline, non-destructive error property (`ActionErrorMessage`; not `State = Error`, which replaces the whole page). `App.DispatcherUnhandledException` no longer fires for these 7 paths.
- **Security:** `catch (Exception)` with **no exception variable** in all 7 → `Exception.Message` / backend body / SKU / cost / supplier / stock values / invoice amounts / payment details / customer billing data structurally unreachable in both the on-screen message and the log. Test-enforced with seeded sentinels (`SKU=WIDGET-9 cost=42.50 …`, `invoice INV-8 total=1,850,000 customer=Amelia Hart card=****4242`).
- **Logging:** each catch reuses the ViewModel's **existing** `[LoggerMessage]` (Inventory instance-form `LogOperationFailed(nameof(<Method>))`, Accounting static-form `LogOperationFailed(_logger, nameof(CancelInvoiceAsync))`), operation-name-only, once. No new logger, no `ILoggerFactory` added, no DI change, no `SYSLIB1020`, no duplicate logging.
- **Localization:** no change — `Common_ActionFailedMessage` was added in Wave A (`794648e`).
- **Tests:** +13 (per-command failure-does-not-throw + inline-error-set, stock/list/form preservation, invoice-list & selection preservation, operation-only-log no-leak, error-clears-on-next-success). `StubInventoryCommandService` gained additive `Exception?` seams (null-path byte-identical); `StubInvoiceCommandService` unchanged (used its pre-existing `cancelInvoice` delegate). 0 existing test bodies changed. No new test helper.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 7 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `66c8490`.
- Working tree after commit: tracked tree clean (`git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'` → empty).

---

## G. MISSING-GUARD SWEEP — TRACK PROGRESS

| Wave | Domain | Status |
|---|---|---|
| **A** — Customer / Service / Specialist write commands | backend-connected | ✅ **DONE** — `794648e` (12 methods, 5 VMs) |
| **B** — HR (`HrPageViewModel` ×10, `EmployeeProfileViewModel` ×3) | fake-backed | ✅ **DONE** — `a5be831` (13 methods, 2 VMs) |
| **C** — Inventory (page ×3, profile ×3) + `AccountingPageViewModel.CancelInvoiceAsync` | fake-backed | ✅ **DONE** — `66c8490` (7 methods, 3 VMs) |
| **D** — Organization (×4 + 2 secondary loads) + Reporting (×3) | fake-backed | **NEXT** |
| E — AI Center (`AiCenterPageViewModel` ×~12) | fake-backed | pending |
| F — Automation tabs (`Workflows`/`ScheduledJobs`/`BusinessRules` ×~7) | fake-backed | pending |
| G (P2) — Workspace / Notification / Settings / CommandPalette (~28) | local / infra | pending, low priority |

---

## STOP

Phase 8.76 commit executed and validated. HEAD `66c8490`. Build 0/0, 2,654/2,654 tests, architecture 7/7.
**Missing-Guard Sweep Wave C complete** — all 6 Inventory write commands + `AccountingPageViewModel.CancelInvoiceAsync`
now use the app's non-destructive in-page error pattern; stock consistency preserved; no accounting/payment-logic
change; the Phase 8.10 `CancelInvoiceAsync` backlog item is closed. Checkpoint updated
(`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`).
**Next: Phase 8.77 — Missing-Guard Sweep Wave D (Organization + Reporting) — Scope Audit.** Awaiting authorization.
