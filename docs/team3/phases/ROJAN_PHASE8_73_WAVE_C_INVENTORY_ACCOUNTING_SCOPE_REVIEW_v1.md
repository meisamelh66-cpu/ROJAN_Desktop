# ROJAN AI — TEAM 3 — PHASE 8.73 — MISSING-GUARD SWEEP WAVE C (INVENTORY + ACCOUNTING) — SCOPE REVIEW v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / accounting-logic / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `a5be83142bbe411beda3daaa115fd18d528bcdf2`
**Objective:** Audit Wave C — Inventory backend-connected command failures + Accounting invoice-cancellation safety — using the Wave A/B pattern (`794648e`, `a5be831`).

---

## A. GIT STATE

```
git rev-parse HEAD        → a5be83142bbe411beda3daaa115fd18d528bcdf2
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `a5be831` (Wave B / HR commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `a5be831` guard HR command failures · `794648e` guard customer/service/specialist · `5ba554c` drop exception payload |

Baseline test suite (checkpoint §E, `a5be831`): **2,641 / 2,641** — Domain 456, Application 791, Presentation 698, Infrastructure 609, Shell 80, Architecture 7.

---

## B. INVENTORY COMMAND INVENTORY

Two ViewModels: `src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs`, `.../Inventory/InventoryProfileViewModel.cs`.
Both are already `sealed partial` with an instance-form operation-name-only `[LoggerMessage(EventId = 1, Level = Error, "… Operation={Operation}")] private partial void LogOperationFailed(string operation);` (added Phase 8.19 / 8.43) and a single `ILogger` field — **no logging-infrastructure change needed**; Wave C only adds call sites.

### B.1 `InventoryPageViewModel` — 3 user-triggered command methods

| # | Method | Command / `CanExecute` | Current exception handling | Success-path side effects | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `CreateProductAsync` | `CreateProductCommand` (SKU + name non-empty, category + supplier selected) | **none** — bare `await _commandService.CreateProductAsync(request)` (after a `SelectedNewProductCategory/Supplier is null` early-return) | clears 6 `NewProduct*` fields; `await LoadAsync()`; re-selects created row | generic `App.DispatcherUnhandledException` dialog; form already cleared |
| 2 | `AddCategoryAsync` | `AddCategoryCommand` (`NewCategoryName` non-empty) | **none** | `NewCategoryName` clear; `Categories.Add(created)` | generic dialog |
| 3 | `AddSupplierAsync` | `AddSupplierCommand` (`NewSupplierName` non-empty) | **none** | `NewSupplierName` clear; `Suppliers.Add(created)` | generic dialog |

**Existing error/state surface:** `State` (`DashboardState`) + `ErrorMessage` — **destructive** (`DashboardState.Error` replaces the page body via `DashboardWidget`); set only by `LoadAsync` / `SearchAsync`. `ErrorMessage = exception.Message` there is the pre-existing Load-boundary surfacing (out of scope — "sanitize load-error surfacing" P2). **No non-destructive inline command-error property exists.**

### B.2 `InventoryProfileViewModel` — 3 user-triggered command methods

| # | Method | Command / `CanExecute` | Current exception handling | Success-path side effects | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `RecordTransactionAsync` | `RecordTransactionCommand` (`TransactionQuantity != 0`) | **none** — `await _commandService.RecordStockTransactionAsync(_productId, type, qty, notes)` | `TransactionQuantity = 1`; `TransactionNotes` clear; `await LoadAsync()` (refetches true stock + history) | generic dialog |
| 2 | `MapServiceAsync` | `MapServiceCommand` (service name non-empty, qty > 0) | **none** — `await _commandService.MapProductToServiceAsync(...)` | `NewMappingServiceName` clear; `NewMappingQuantityPerService = 1`; `await LoadAsync()` | generic dialog |
| 3 | `UnmapServiceAsync` | `UnmapServiceCommand` (param `ServiceProductMappingDto`) | **none** (after null early-return) — `await _commandService.UnmapProductFromServiceAsync(_productId, mapping.Id)` | `await LoadAsync()` | generic dialog |

**Existing error/state surface:** `State` + `ErrorMessage`, set only by `LoadAsync` (destructive Error state). **No action-error property** (audit 8.64 line 52: "`LoadAsync` catch only — needs an action error property").

**Inventory-consistency note:** none of these 6 methods mutates a local collection *before or without* a subsequent authoritative reload. `RecordTransaction` / `MapService` / `UnmapService` call the service then `await LoadAsync()` (which refetches stock + transactions + mappings from the backend). `AddCategory` / `AddSupplier` append the *returned* DTO only after the await succeeds. So a backend failure leaves the local view either unchanged (page adds) or forcibly re-synced to backend truth (profile reloads) — the guard does not introduce a divergence risk.

### B.3 Backend-connectivity

Inventory is **fake-backed** (`FakeInventoryRepository`; backend has zero Inventory code — Phase 8.0, re-confirmed exhaustively). Wave C guards are still worth doing: the pattern must be correct before the eventual connection, exactly as Waves A/B. Classification **P1 — UX consistency**, not P0 (`App.DispatcherUnhandledException` already prevents any crash).

---

## C. ACCOUNTING AUDIT — `AccountingPageViewModel.CancelInvoiceAsync` (report only, no change)

```csharp
private async Task CancelInvoiceAsync()
{
    if (SelectedInvoice is null) { return; }

    var invoiceId = SelectedInvoice.Id;
    await _invoiceCommandService.CancelInvoiceAsync(invoiceId).ConfigureAwait(true);
    await LoadAsync().ConfigureAwait(true);
    SelectedInvoice = Invoices.FirstOrDefault(invoice => invoice.Id == invoiceId);
}
```

| Aspect | Finding |
|---|---|
| **Current guard status** | **Unguarded.** Bare `await _invoiceCommandService.CancelInvoiceAsync(invoiceId)`. A throw becomes an unobserved `async void` task exception caught by `App.DispatcherUnhandledException` → generic dialog. (Audit 8.10 / checkpoint §F disclosed this exact gap.) |
| **`CanExecute`** | `SelectedInvoice is not null && SelectedInvoice.Status != InvoiceStatus.Cancelled` — preserved verbatim by any guard (stays outside the `try`). |
| **Double-charge / payment risk** | **Not applicable to this method.** `CancelInvoiceAsync` performs no payment — it asks the backend to move an invoice to `Cancelled`. The disclosed double-charge risk lives in `PosCheckoutViewModel.ChargeAsync` (checkpoint §F / Phase 7.4.4) and is **out of Wave C scope** — no change is proposed to `PosCheckoutViewModel`. Wrapping `CancelInvoiceAsync` in `try`/`catch` adds no retry loop and no re-invocation path; the command is re-enabled by `CanExecute` only while `Status != Cancelled`, so a *successful* backend cancel that then failed on reload cannot be re-cancelled (the reload, once it eventually succeeds, disables the command). |
| **Payment state handling** | None in this method. Invoice/payment state is 100 % backend-authoritative; the VM only reflects it via `LoadAsync`'s refetch. |
| **Existing rollback behavior** | None — the method has no local state to roll back. On success it clears nothing and mutates no collection directly; it calls `await LoadAsync()` (self-guarded) and re-selects by id. A guard needs only to surface the failure; there is nothing to revert. |
| **Logger behavior** | `AccountingPageViewModel` uses the **static-form** `[LoggerMessage(EventId = 1, Level = Error, "Accounting operation failed. Operation={Operation}")] private static partial void LogOperationFailed(ILogger logger, string operation)` — static because the class holds **two** `ILogger` fields (`_logger`, `_posCheckoutLogger`), so an instance-form generator pick would trip `SYSLIB1020`. `LoadAsync` / `SearchAsync` already call `LogOperationFailed(_logger, nameof(...))`. A `CancelInvoiceAsync` guard reuses this exact call — **no new logger, no signature change.** |
| **Backend connectivity** | Accounting is **fake-backed** (`FakeAccountingRepository`; checkpoint §D). P1, not P0. |

---

## D. GUARD STRATEGY

### D.1 Wave A/B pattern applies — one additive property pair per ViewModel

The Wave A/B pattern (local `try` around the existing command body + non-destructive inline localized error property + reuse the existing `[LoggerMessage]`, operation-name-only, once) applies to all 7 Wave C methods unchanged. Each of the three ViewModels needs one **additive** `ActionErrorMessage` / `HasActionError` pair (private-set, `SetProperty`, **no constructor / DI change**) — the same move Waves A/B made.

| ViewModel | New pair | Localized value | Notes |
|---|---|---|---|
| `InventoryPageViewModel` | `ActionErrorMessage` / `HasActionError` | `Strings.Common_ActionFailedMessage` | one shared inline area (only one quick-add form visible at a time — same reasoning as Wave B `HrPageViewModel`) |
| `InventoryProfileViewModel` | `ActionErrorMessage` / `HasActionError` | `Strings.Common_ActionFailedMessage` | matches audit 8.64 "needs an action error property" |
| `AccountingPageViewModel` | `ActionErrorMessage` / `HasActionError` | `Strings.Common_ActionFailedMessage` | **Deviation from audit 8.64 line 129** ("reuses `ErrorMessage`/`State`"): that note predates Wave A. Reusing `State = Error` is **destructive** (blanks the whole invoice page for a failed row action). The non-destructive pair — consistent with Waves A/B — is the right call; recommend authorizer confirm. |

**No new localization key** — `Common_ActionFailedMessage` already ships in `Strings.cs` + all 3 `.resx` files (Wave A `794648e`). No `Inventory_SaveError` / `Accounting_SaveError` string exists, so `Common_ActionFailedMessage` is the correct reuse (Wave B precedent).

### D.2 Per-method transformation (identical shape for all 7)

```csharp
// unchanged: CanExecute predicate + early-return validation + request-building stay ABOVE the try
if (SelectedNewProductCategory is null || SelectedNewProductSupplier is null) { return; }
var request = new CreateProductRequest(...);

try
{
    var created = await _commandService.CreateProductAsync(request).ConfigureAwait(true);
    ActionErrorMessage = null; HasActionError = false;   // clear on success

    NewProductSku = string.Empty; /* … existing field clears … */
    await LoadAsync().ConfigureAwait(true);
    SelectedProduct = Products.FirstOrDefault(p => p.Id == created.Id);
}
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
catch (Exception)
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(CreateProductAsync));                 // Inventory: instance form
    // AccountingPageViewModel.CancelInvoiceAsync → LogOperationFailed(_logger, nameof(CancelInvoiceAsync));   // static form
}
```

- **`catch (Exception)` with no exception variable** in all 7 → `Exception.Message` / backend body / SKU / cost / supplier / invoice amount / customer billing data structurally unreachable on screen and in the log.
- **`await LoadAsync()`** (self-guarded — its own catch sets `State = Error`) stays inside the guarded block for `CreateProductAsync`, all 3 `InventoryProfileViewModel` methods, and `CancelInvoiceAsync`, consistent with the Wave A/B `CustomerProfileViewModel.SaveChangesAsync` precedent. A reload failure cannot propagate into the command catch.
- **No `State` mutation** by these guards — a failed Inventory/Accounting command must not blank the page.

### D.3 Exceptions / edge cases

| Concern | Resolution |
|---|---|
| **Financial operations** | `CancelInvoiceAsync` is the only Accounting method in scope; it is *not* a payment operation. No `PosCheckoutViewModel` / `IPaymentCommandService` change. No idempotency assumption is introduced — the guard only surfaces the failure. |
| **Inventory consistency** | Every profile write is followed by `await LoadAsync()` (authoritative refetch) inside the guard; page adds append the returned DTO only post-await. No local stock/mapping mutation happens without backend confirmation, so a failed command cannot leave a stale count on screen. |
| **Transaction state** | `RecordStockTransactionAsync` failure → catch → `ActionErrorMessage`; the reload never runs, so `RecentTransactions` / `Stock` keep their last-known-good values (test-asserted). |
| **`AddCategory` / `AddSupplier`** | `Categories.Add` / `Suppliers.Add` are inside the `try` after the await, so a failure adds nothing (test-asserted). |

### D.4 Explicitly NOT changed

`IInventoryCommandService` / `IProductQueryService` / `IProductProfileQueryService` / `IInventoryQueryService` / `IInvoiceCommandService` / `IInvoiceQueryService` / `IPaymentCommandService` / `IPaymentQueryService` and every DTO/request record; Application-layer Inventory & Accounting services; `Fake*Repository`; backend contracts; DI; ViewModel constructors; `ILoggerFactory` plumbing; RBAC / `CanExecute`; `PosCheckoutViewModel` (incl. `ChargeAsync` double-charge risk); `InvoiceProfileViewModel`; `AsyncRelayCommand`; `App.xaml.cs`; navigation; `LoadAsync` / `SearchAsync` catches (incl. `ErrorMessage = exception.Message`); every `[LoggerMessage]` signature; `Strings.cs` / all `.resx` files.

---

## E. SECURITY REVIEW

| Domain | Sensitive data on a failed command | Wave C exposure |
|---|---|---|
| **Inventory** | SKU, supplier id/name, unit price / cost, on-hand & reorder stock values, product description | **None.** `catch (Exception)` binds no variable → on-screen text is the fixed constant `Strings.Common_ActionFailedMessage`; the log gets only `Operation=<Method>` via the existing operation-name-only `[LoggerMessage]`. Form values (SKU, price, names) stay in the bound `NewProduct*` / `NewMapping*` / `Transaction*` properties for retry — in-memory, never logged, never placed in `ActionErrorMessage`. |
| **Accounting** | invoice amounts (subtotal/tax/total), payment details, customer billing name/id, receipt text | **None.** Same two mechanisms. `CancelInvoiceAsync` never reads or forwards any invoice/payment field into `ActionErrorMessage` or the logger — it logs `Operation=CancelInvoiceAsync` only. `invoiceId` is a local — **not logged**. |

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | unreachable (no exception variable; constant string only) |
| `Exception.Message` / `.ToString()` → log file | unreachable (`LogOperationFailed` has no `Exception` parameter; `LocalFileLoggerProvider` renders no backend body) |
| Backend response payload | unreachable on both surfaces |
| Internal identifiers (product / category / supplier / mapping / invoice GUIDs) | not logged (operation name only), not shown (generic string only) |

**Test-enforced:** each Wave C failure test seeds the stub exception / `cancelInvoice` delegate with a secret sentinel (Inventory: `"backend 500: SKU=WIDGET-9 cost=42.50 supplier=Acme Corp on-hand=7"`; Accounting: `"backend 500: invoice INV-8 total=1,850,000 customer=Amelia Hart card=****4242"`) and asserts `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## F. TEST PLAN

### F.1 Stub seams (additive `Exception?` — null-path byte-identical)

| Stub file | Change |
|---|---|
| `tests/…/Inventory/StubInventoryCommandService.cs` | **+6** `Exception?` auto-properties: `CreateProductException`, `CreateCategoryException`, `CreateSupplierException`, `RecordStockTransactionException`, `MapProductToServiceException`, `UnmapProductFromServiceException`. Each command records its call, then returns `Task.FromException<T>(value)` when set, else the original `Task.FromResult(...)` verbatim. (Wave A/B `StubCustomerCommandService` idiom.) |
| `tests/…/Accounting/StubInvoiceCommandService.cs` | **no change** — already exposes a `Func<string, CancellationToken, Task<InvoiceDto>>? cancelInvoice` constructor delegate; the `CancelInvoiceAsync` failure test passes `cancelInvoice: (_, _) => Task.FromException<InvoiceDto>(new InvoiceDto…)`. |

### F.2 New tests

| File | Tests | Count |
|---|---|---|
| `Inventory/InventoryPageViewModelTests.cs` | 3 × failure-does-not-throw + `HasActionError` + message (`CreateProduct` / `AddCategory` / `AddSupplier`); `CreateProduct` → form fields preserved + `State != Error`; `AddCategory`/`AddSupplier` → collection not grown; 1 × `CreateProduct` success-after-failure clears error; 1 × no-leak (SKU/cost/supplier sentinel, operation-only log) | ~6 |
| `Inventory/InventoryProfileViewModelTests.cs` | 3 × failure-does-not-throw + `HasActionError` + message (`RecordTransaction` / `MapService` / `UnmapService`); `RecordTransaction` → `Stock`/`RecentTransactions` unchanged, `TransactionQuantity` preserved; 1 × `MapService` success-after-failure clears error; 1 × no-leak | ~5 |
| `Accounting/AccountingPageViewModelTests.cs` | 1 × `CancelInvoice` failure-does-not-throw + `HasActionError` + message + `State != Error` + invoice list & selection unchanged; 1 × no-leak (invoice-total/customer sentinel, operation-only `Operation=CancelInvoiceAsync` log); 1 × `CancelInvoice` success-after-failure clears error | ~3 |

**Estimated new tests: ~14.** Conservative suite projection: **2,641 → ~2,655**.

### F.3 Files changed (Phase 8.74 implementation)

| Group | Files | Count |
|---|---|---|
| Production | `ViewModels/Inventory/InventoryPageViewModel.cs`, `ViewModels/Inventory/InventoryProfileViewModel.cs`, `ViewModels/Accounting/AccountingPageViewModel.cs` | 3 |
| Test stub | `Inventory/StubInventoryCommandService.cs` | 1 |
| Test VMs | `Inventory/InventoryPageViewModelTests.cs`, `Inventory/InventoryProfileViewModelTests.cs`, `Accounting/AccountingPageViewModelTests.cs` | 3 |
| **Total** | | **7** |

No new file, no `Strings.cs` / `.resx` change, no new test helper, no Accounting stub change.

---

## G. COMMIT STRATEGY

**Recommendation: a single Wave C commit.**

```
fix(desktop): guard inventory and invoice-cancel command failures
```

Rationale:
- All 7 methods take the **identical** mechanical `try`/`catch` + `ActionErrorMessage` shape; the Accounting piece is exactly one method with no financial-logic change (`CancelInvoiceAsync` is not a payment op).
- Small surface (3 prod files, 7 total). Matches audit 8.64 line 130 (**one commit** — `fix(desktop): guard inventory and invoice-cancel command failures`).
- A standalone 1-method Accounting commit would add ceremony without isolation or bisection value.

**Risk of the single commit: LOW.** Purely additive `try`/`catch` + one bindable property pair per VM (no ctor / DI change). Fake-backed domains — zero backend contract exposure. The one judgement point (settled in §D.1): `AccountingPageViewModel` gets the non-destructive `ActionErrorMessage` pair rather than reusing the destructive `State = Error` the 8.64 note guessed at.

**Alternative (if the authorizer wants financial isolation):** split into `fix(desktop): guard inventory command failures` (Inventory, 5 files) + `fix(desktop): guard invoice-cancel command failure` (Accounting, 3 files: `AccountingPageViewModel.cs` + its test + no stub). Costs a second full audit→review→commit cycle for one method. Not recommended.

---

## H. PHASE 8.74 RECOMMENDATION

**PHASE 8.74 — MISSING-GUARD SWEEP — WAVE C (INVENTORY + ACCOUNTING) — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs` — add `ActionErrorMessage` / `HasActionError` (private-set, additive); wrap `CreateProductAsync`, `AddCategoryAsync`, `AddSupplierAsync` in the §D.2 `try`/`catch`; each catch → set the pair + `LogOperationFailed(nameof(Method))`; clear the pair on each success path. No ctor change.
- `src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs` — add `ActionErrorMessage` / `HasActionError`; wrap `RecordTransactionAsync`, `MapServiceAsync`, `UnmapServiceAsync` (command await + form clears + `await LoadAsync()`); catch → set the pair + `LogOperationFailed(nameof(Method))`. No ctor change.
- `src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs` — add `ActionErrorMessage` / `HasActionError`; wrap `CancelInvoiceAsync` (command await + `await LoadAsync()` + re-select); catch → set the pair + `LogOperationFailed(_logger, nameof(CancelInvoiceAsync))` (static form). No ctor change. **`PosCheckoutViewModel`, `InvoiceProfileViewModel`, `OpenPosCheckout`, `LoadAsync`, `SearchAsync` untouched.**
- `tests/…/Inventory/StubInventoryCommandService.cs` — additive `Exception?` seams (§F.1), null-path byte-identical.
- `tests/…/Inventory/InventoryPageViewModelTests.cs`, `tests/…/Inventory/InventoryProfileViewModelTests.cs`, `tests/…/Accounting/AccountingPageViewModelTests.cs` — ~14 new tests (§F.2). No existing test body changed.

**DO NOT:** change any service / DI / ViewModel constructor / backend contract / RBAC / `CanExecute` / navigation / command infrastructure / `[LoggerMessage]` signature / `Strings.cs` / `.resx` / **accounting or payment logic** / `PosCheckoutViewModel`. No commit.

**Risk: LOW.** Additive `try`/`catch` around existing awaits + one bindable property pair per VM (no ctor, no DI). Fake-backed modules. `CancelInvoiceAsync` introduces no payment-idempotency assumption — it is not a payment operation and no retry loop is added.

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (no `SYSLIB1020` — Inventory VMs keep their single `ILogger` + instance form; `AccountingPageViewModel` keeps its static form; no `CA1031` / `CA1848`).
- Full suite → **~2,655 / ~2,655 PASS** (Presentation 698 → ~712; Domain 456, Application 791, Infrastructure 609, Shell 80 unchanged).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_74_WAVE_C_INVENTORY_ACCOUNTING_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.75 commit scope review.

**Downstream (unchanged):** Wave D — Organization (×4 + 2 secondary loads) + Reporting (×3) (`fix(desktop): guard organization and reporting command failures`); then E (AI Center ×~12), F (Automation tabs ×~7), G (P2 infra: Workspace / Notification / Settings / CommandPalette).

---

## STOP

Phase 8.73 scope review complete. HEAD `a5be831`, tracked tree clean, baseline 2,641 / 2,641.
Wave C = **7 command guards** — `InventoryPageViewModel` ×3 (`CreateProduct` / `AddCategory` / `AddSupplier`), `InventoryProfileViewModel` ×3 (`RecordTransaction` / `MapService` / `UnmapService`), `AccountingPageViewModel.CancelInvoiceAsync` ×1 — each reusing the Wave A/B pattern + the existing `[LoggerMessage]` + `Common_ActionFailedMessage`; one additive `ActionErrorMessage`/`HasActionError` pair per VM. `CancelInvoiceAsync` is not a payment operation — no accounting/payment-logic change, no `PosCheckoutViewModel` touch, no idempotency assumption. ~7 files, ~14 tests, one commit `fix(desktop): guard inventory and invoice-cancel command failures`.
**Recommended next: Phase 8.74 — Wave C (Inventory + Accounting) Implementation.** Awaiting authorization.
