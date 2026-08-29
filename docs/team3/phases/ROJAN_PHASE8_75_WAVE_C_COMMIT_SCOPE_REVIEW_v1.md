# ROJAN AI — TEAM 3 — PHASE 8.75 — MISSING-GUARD SWEEP WAVE C (INVENTORY + ACCOUNTING) — COMMIT SCOPE REVIEW v1

**Type:** Pre-commit review. **STRICT MODE — no source change, no test change, no new file, no commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `a5be83142bbe411beda3daaa115fd18d528bcdf2`
**References:** `ROJAN_PHASE8_73_WAVE_C_INVENTORY_ACCOUNTING_SCOPE_REVIEW_v1.md`, `ROJAN_PHASE8_74_WAVE_C_IMPLEMENTATION_REPORT_v1.md`
**Verdict:** ✅ **READY TO COMMIT** — scope clean, 7 files, 0 new, build 0/0, 2,654/2,654 tests, architecture 7/7.

---

## A. GIT STATE

```
git rev-parse HEAD        → a5be83142bbe411beda3daaa115fd18d528bcdf2
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty)   ← nothing staged
git log --oneline -3      → a5be831 guard HR command failures / 794648e guard customer/service/specialist / 5ba554c drop exception payload
```

| Check | Result |
|---|---|
| HEAD | `a5be831` (Wave B / HR commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Staging area | **empty** ✅ |
| Modified tracked files | **7** ✅ |
| New tracked files | **0** ✅ |
| Untracked | only `ROJAN_*.md` audit-trail reports ✅ |

```
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Inventory/StubInventoryCommandService.cs
```

`git diff --stat`: **7 files changed, 508 insertions(+), 35 deletions(-)**. The 35 deletions are entirely original single-line command bodies re-indented into their `try`-wrapped form (verified line-by-line below) — no property, validation, service call, or assertion removed.

Matches Phase 8.73 §F.3 estimate (3 prod + 1 stub + 3 test = 7) and Phase 8.74 report §A exactly.

---

## B. SCOPE VERIFICATION

### B.1 Production (3 files) — in scope

| File | Diff summary |
|---|---|
| `InventoryPageViewModel.cs` | `+ using …Localization;` (alpha-ordered); `+ _actionErrorMessage` / `_hasActionError` fields (2); `+ ActionErrorMessage` / `HasActionError` properties (18); `CreateProductAsync` / `AddCategoryAsync` / `AddSupplierAsync` wrapped in `try { … existing body … } catch (Exception) { ActionError + LogOperationFailed(nameof) }` with the `#pragma warning disable/restore CA1031` boundary comment. **Nothing else.** `LoadAsync`, `SearchAsync`, `ReplaceProducts`, ctor, all bindable form properties, `LowStockCount`, all `ICommand` wiring, `[LoggerMessage]` signature — untouched. |
| `InventoryProfileViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; `RecordTransactionAsync` / `MapServiceAsync` / `UnmapServiceAsync` wrapped identically. `LoadAsync`, ctor, `[LoggerMessage]` signature — untouched. |
| `AccountingPageViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; **only `CancelInvoiceAsync`** wrapped. `LoadAsync`, `SearchAsync`, `ReplaceInvoices`, `OpenPosCheckout` (incl. its `new PosCheckoutViewModel(...)`), ctor, both static-form `[LoggerMessage]` call sites in `LoadAsync`/`SearchAsync`, the `[LoggerMessage]` signature — untouched. |

No constructor signature change, no new service/logger field, no DI registration touched, in any of the three.

### B.2 Tests (3 files) — approved Inventory + Accounting test files only

| File | Diff shape (verified) |
|---|---|
| `InventoryPageViewModelTests.cs` | `+ using …Localization;` (1 line); **one append hunk** (`@@ -216,6 +217,111 @@`) — **+5 `[Fact]`** + one private helper `FillNewProductForm`. Zero `-` lines outside the `using` context. |
| `InventoryProfileViewModelTests.cs` | `+ using …Localization;`; **one append hunk** (`@@ -170,4 +171,107 @@`) — **+5 `[Fact]`** + one private helper `LoadingProfileQuery`. Zero existing bodies changed. |
| `AccountingPageViewModelTests.cs` | `+ using …Localization;`; **one insert hunk** (`@@ -208,6 +209,80 @@`, before `OpenPosCheckoutCommand_…`) — **+3 `[Fact]`**. Zero existing bodies changed. |

### B.3 Stub (1 file) — additive Inventory failure seams only

`StubInventoryCommandService.cs`: **+6** `Exception?` auto-properties (`CreateProductException`, `CreateCategoryException`, `CreateSupplierException`, `RecordStockTransactionException`, `MapProductToServiceException`, `UnmapProductFromServiceException`). Each command records its call, then returns `Task.FromException<T>(value)` when the property is set, else the original `Task.FromResult(...)` / `Task.CompletedTask` verbatim (Wave A/B `StubCustomerCommandService` idiom). **Null-path byte-identical** — every pre-existing Inventory test passes unchanged.

**No Accounting stub change** — the `CancelInvoiceAsync` failure tests use the pre-existing `Func<string, CancellationToken, Task<InvoiceDto>>? cancelInvoice` constructor delegate on `StubInvoiceCommandService`.

### B.4 Confirmed UNTOUCHED

```
git diff --name-only  →  7 files, all src/…/ViewModels/{Inventory,Accounting}/ or tests/…/{Inventory,Accounting}/
```

| Area | Status |
|---|---|
| **`PosCheckoutViewModel`** (incl. `ChargeAsync` double-charge risk) | ✅ untouched — not in `git status`; `AccountingPageViewModel.OpenPosCheckout`'s `new PosCheckoutViewModel(...)` line is a context line in the diff, unchanged |
| Payment services (`IPaymentCommandService` / `IPaymentQueryService` + impls + `FakeAccountingRepository`) | ✅ untouched |
| Invoice services (`IInvoiceCommandService` / `IInvoiceQueryService`) | ✅ untouched |
| Inventory services (`IInventoryCommandService` / `IProductQueryService` / `IProductProfileQueryService` / `IInventoryQueryService`) + impls + `FakeInventoryRepository` | ✅ untouched |
| Backend contracts / HTTP clients / API layer | ✅ untouched |
| DTOs / request records (all Inventory & Accounting) | ✅ untouched |
| DI (`Presentation` / `Infrastructure` `ServiceCollectionExtensions.cs`) | ✅ untouched |
| RBAC / permission gates / `CanExecute` predicates | ✅ untouched |
| Authentication / session | ✅ untouched |
| Navigation / back-stack / shell / `IDialogService` | ✅ untouched |
| `AsyncRelayCommand` / `RelayCommand` / `ViewModelBase` / `App.xaml.cs` | ✅ untouched |
| `InvoiceProfileViewModel` | ✅ untouched |
| `Strings.cs` / `Strings.resx` / `Strings.en.resx` / `Strings.ar.resx` (`Common_ActionFailedMessage` already ships in Wave A `794648e`) | ✅ untouched |
| Every `[LoggerMessage]` signature / EventId / Level / Message (Inventory instance-form ×2, Accounting static-form) | ✅ untouched |
| `LoadAsync` / `SearchAsync` catches (incl. pre-existing `ErrorMessage = exception.Message`) | ✅ untouched |
| Domain / Application / Infrastructure / Shell projects | ✅ untouched |

---

## C. INVENTORY GUARD REVIEW — 6 methods

Every guard follows the identical diff-confirmed shape: `<validation + request-building: UNCHANGED, outside try>` → `try { <original command await + original success body: UNCHANGED>; ActionErrorMessage = null; HasActionError = false; }` → `#pragma CA1031` + `catch (Exception)` (no variable) → `{ ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(nameof(<Method>)); }`.

| # | Method | Validation preserved | Service call unchanged | Reload / stock consistency |
|---|---|---|---|---|
| 1 | `InventoryPage.CreateProductAsync` | `if (SelectedNewProductCategory is null \|\| SelectedNewProductSupplier is null) return;` + `CreateProductRequest` construction stay **outside** the `try`; `CanExecute` (SKU + name + category + supplier) unchanged | ✅ `CreateProductAsync(request)` | success path: 6 field clears + `await LoadAsync()` + re-select — inside `try`; on failure the reload is skipped and nothing local mutated |
| 2 | `InventoryPage.AddCategoryAsync` | `CanExecute` (`NewCategoryName` non-empty) unchanged | ✅ `CreateCategoryAsync(NewCategoryName, string.Empty)` | `Categories.Add(created)` inside `try` → a failure adds nothing (test-asserted) |
| 3 | `InventoryPage.AddSupplierAsync` | `CanExecute` (`NewSupplierName` non-empty) unchanged | ✅ `CreateSupplierAsync(new CreateSupplierRequest(NewSupplierName, "", "", ""))` | `Suppliers.Add(created)` inside `try` → a failure adds nothing (test-asserted) |
| 4 | `InventoryProfile.RecordTransactionAsync` | `CanExecute` (`TransactionQuantity != 0`) unchanged | ✅ `RecordStockTransactionAsync(_productId, SelectedTransactionType, TransactionQuantity, TransactionNotes)` | `TransactionQuantity = 1` + `TransactionNotes` clear + **authoritative `await LoadAsync()`** inside `try`; on failure reload skipped → `Stock.QuantityOnHand == 42` / `RecentTransactions` count / input all unchanged (test-asserted) |
| 5 | `InventoryProfile.MapServiceAsync` | `CanExecute` (service name non-empty, qty > 0) unchanged | ✅ `MapProductToServiceAsync(_productId, NewMappingServiceName, NewMappingQuantityPerService)` | field clears + `await LoadAsync()` inside `try`; on failure `ServiceMappings` unchanged |
| 6 | `InventoryProfile.UnmapServiceAsync` | `if (mapping is null) return;` stays **outside** the `try` | ✅ `UnmapProductFromServiceAsync(_productId, mapping.Id)` | `await LoadAsync()` inside `try`; on failure `ServiceMappings` unchanged (test-asserted still 1) |

**Stock consistency:** confirmed — none of the 6 mutates a local collection *without* a subsequent authoritative `await LoadAsync()` (profile) or *before* the awaited service call succeeds (page adds). A failed command leaves the view either unchanged or forcibly re-synced to backend truth. **No manual inventory state recovery** was added — none is needed.

---

## D. ACCOUNTING REVIEW — `AccountingPageViewModel.CancelInvoiceAsync`

Diff (verified): the `if (SelectedInvoice is null) return;` and `var invoiceId = SelectedInvoice.Id;` lines are **unchanged and outside** the new `try`. Inside the `try`: the same `await _invoiceCommandService.CancelInvoiceAsync(invoiceId)` call, then `ActionErrorMessage = null; HasActionError = false;`, then the same `await LoadAsync()` + `SelectedInvoice = Invoices.FirstOrDefault(i => i.Id == invoiceId)`. Catch: `ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(_logger, nameof(CancelInvoiceAsync));`.

| Confirm no change to | Result |
|---|---|
| Invoice cancellation logic | ✅ — identical single `CancelInvoiceAsync(invoiceId)` call, identical reload + re-select; the `try` only wraps them |
| Payment flow | ✅ — none exists in this method; `IPaymentCommandService` / `PosCheckoutViewModel` / `ChargeAsync` not referenced or touched |
| Rollback behavior | ✅ — none existed (no local state to roll back), none added; the catch only surfaces `ActionErrorMessage` |
| Transaction rules | ✅ — `CanExecute` (`SelectedInvoice is not null && Status != InvoiceStatus.Cancelled`) unchanged; **no retry loop and no idempotency assumption** introduced |
| **Only added** | error surface (`ActionErrorMessage` / `HasActionError` → `Common_ActionFailedMessage`) + one call to the **existing** static-form `[LoggerMessage]` (`LogOperationFailed(_logger, nameof(CancelInvoiceAsync))`) |

Static-form logging pattern preserved — `_logger` passed explicitly, matching how `LoadAsync` / `SearchAsync` already call `LogOperationFailed(_logger, nameof(...))`. The class still holds two `ILogger` fields; no new field, no signature change.

---

## E. SECURITY REVIEW

| Domain | Sensitive data | Finding |
|---|---|---|
| **Inventory** | SKU, supplier id/name, unit price / cost, on-hand & reorder stock values, product description | **Not exposed.** `catch (Exception)` binds no variable in any of the 6 → on-screen text is the compile-time constant `Strings.Common_ActionFailedMessage`; the log gets only `Operation=<Method>` via the operation-name-only instance-form `[LoggerMessage]`. Form values stay in the bound `NewProduct*` / `NewMapping*` / `Transaction*` properties for retry (in-memory, pre-existing). |
| **Accounting** | invoice amounts (subtotal/tax/total), payment details, customer billing name/id, receipt text | **Not exposed.** Same two mechanisms. `CancelInvoiceAsync` logs `Operation=CancelInvoiceAsync` only; `invoiceId` is a local — not logged, not shown. No invoice/payment field is read into `ActionErrorMessage` or the logger. |

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — no exception variable; `ActionErrorMessage` only ever assigned `null` or `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed` (instance form: `(string operation)`; static form: `(ILogger logger, string operation)`) has **no `Exception` parameter`; `LocalFileLoggerProvider` renders no backend body |
| Backend response payload | **not exposed** on either surface |
| Internal identifiers (product / category / supplier / mapping / invoice GUIDs) | **not logged** (operation name only), **not shown** (generic string only) |

**UI receives only:** `Strings.Common_ActionFailedMessage` (already shipped in `794648e`).
**Logger receives only:** `Operation=<MethodName>` via the templates `"Inventory page operation failed. Operation={Operation}"` / `"Inventory profile operation failed. Operation={Operation}"` / `"Accounting operation failed. Operation={Operation}"`.

**Test-enforced:** seeded sentinels — Inventory `"backend 500: SKU=WIDGET-9 cost=42.50 supplier=Acme Corp on-hand=7"` (profile: `"SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"`), Accounting `"backend 500: invoice INV-8 total=1,850,000 customer=Amelia Hart card=****4242"` — each `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## F. LOGGING REVIEW

| Check | Result |
|---|---|
| Existing `[LoggerMessage]` reused | ✅ — `InventoryPageViewModel` / `InventoryProfileViewModel` reuse their pre-existing instance-form `LogOperationFailed(string operation)` (added Phase 8.19 / 8.43); `AccountingPageViewModel` reuses its pre-existing static-form `LogOperationFailed(ILogger logger, string operation)`. Only new **call sites** added (3 + 3 + 1). |
| No new logger field / type | ✅ — Inventory VMs keep their single `_logger`; `AccountingPageViewModel` keeps `_logger` + `_posCheckoutLogger` + `_loggerFactory`. No addition. |
| No DI / constructor change | ✅ |
| No duplicate logging | ✅ — each guarded method logs **once** in its catch. `LoadAsync` (called by some success paths) has its own separate catch that only fires on a load failure; a command-then-failed-reload cannot double-log into the command catch (reload is self-guarded). |
| No `SYSLIB1020` | ✅ — no `[LoggerMessage]` signature changed; Inventory VMs stay single-`ILogger` + instance form (compiled clean at `a5be831`); Accounting stays static form. Build = **0 warnings** (§G). |
| No `CA1848` (raw `_logger.Log*`) | ✅ — no raw logger call added |
| `CA1031` | ✅ — suppressed locally with the documented `#pragma warning disable/restore CA1031` boundary comment, identical convention to the pre-existing `LoadAsync` catches and to Waves A/B |

---

## G. TESTS

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all 6 projects Passed
```

| Project | Passed | Failed | Skipped | Δ vs `a5be831` |
|---|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 | — |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 | — |
| Rojan.Desktop.Presentation.Tests | **711** | 0 | 0 | **+13** |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 | — |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 | — |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 | — |
| **TOTAL** | **2,654** | **0** | **0** | **+13** |

| Expected (Phase 8.75) | Actual | Status |
|---|---|---|
| Tests 2,654 / 2,654 PASS | 2,654 / 2,654 | ✅ |
| Build 0 / 0 | 0 / 0 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

**+13 new tests reviewed:**

| Aspect | Coverage |
|---|---|
| **Failure handling** | 7 tests — one per command: `Record.Exception(() => Cmd.Execute(param))` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage`. |
| **Inventory consistency** | `RecordTransaction` failure → `Stock.QuantityOnHand == 42` unchanged, `RecentTransactions` count unchanged, `TransactionQuantity`/`Notes` preserved, `State == Loaded`; `AddCategory`/`AddSupplier` failure → collection not grown; `CreateProduct` failure → form fields preserved, `State != Error`; `MapService`/`UnmapService` failure → `ServiceMappings` unchanged. |
| **Invoice safety** | `CancelInvoiceCommand_Failure_…` → no throw, `HasActionError`, `State != Error`, `Invoices` list & `SelectedInvoice` unchanged, command attempted; `…SuccessAfterFailure_ClearsActionError` proves a later successful cancel clears the inline error. |
| **No sensitive-data leak** | `CreateProductCommand_Failure_LogsOperationNameOnly_NoSkuOrCostLeak`, `RecordTransactionCommand_Failure_LogsOperationNameOnly_NoLeak`, `CancelInvoiceCommand_Failure_LogsOperationNameOnly_NoFinancialLeak` — assert `Operation=<Method>` in a `LogLevel.Error` entry **and** `DoesNotContain(sentinel)` in both `logger.Entries` and `ActionErrorMessage`. |
| **Success clears error** | `CreateProductCommand_SuccessAfterFailure_ClearsActionError`, `MapServiceCommand_SuccessAfterFailure_ClearsActionError`, `CancelInvoiceCommand_SuccessAfterFailure_ClearsActionError`. |

All new tests use the existing `RecordingLogger<T>` and the existing stubs (Inventory with the additive seams; Accounting via the pre-existing `cancelInvoice` delegate). Async commands complete synchronously in-test (every stub returns an already-completed `Task` — Wave A/B convention).

---

## H. COMMIT READINESS

✅ **Ready.** No blockers.

**Staging plan (Phase 8.76 — explicit paths only, no `git add .` / `-A`):**

```
git reset
git add src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryProfileViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Inventory/StubInventoryCommandService.cs
git add tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
git diff --cached --name-only        # expect exactly 7
```

**Commit message (EXACT):**

```
fix(desktop): guard inventory and invoice-cancel command failures

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

**Post-commit validation to run:** `dotnet build -c Debug` (expect 0/0) · full `dotnet test` (expect 2,654/2,654) · architecture (expect 7/7) · `git log --oneline -3`.

**Checkpoint update (Phase 8.76):** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` — new HEAD; §A banner + audit-phase list; §B commit table + Phase 8.74 detail bullet; §E build/test 2,641 → 2,654 (Presentation 698 → 711); §G Missing-Guard Sweep track — Wave C ✅ / Wave D NEXT; §H items 1/2/5/6.

---

## STOP

Phase 8.75 commit scope review complete. **7 modified files, 0 new**, all under `…/Inventory/` or `…/Accounting/`. All 6 Inventory guards + the 1 Accounting guard preserve validation / `CanExecute` / service calls / authoritative reload / stock consistency. `CancelInvoiceAsync` has no invoice-cancellation / payment / rollback / transaction-rule change — `PosCheckoutViewModel` and all payment/invoice services untouched. No `Exception.Message` / SKU / cost / supplier / stock / invoice-amount / payment / billing exposure — UI gets only `Common_ActionFailedMessage`, logging only `Operation=nameof(Method)` via the existing `[LoggerMessage]` (Inventory instance-form, Accounting static-form). No new logger, no DI change, no `SYSLIB1020`, no duplicate logging. Build 0/0, **2,654/2,654** tests, architecture 7/7.
**Next: Phase 8.76 — Wave C Commit Execution.** Awaiting authorization.
