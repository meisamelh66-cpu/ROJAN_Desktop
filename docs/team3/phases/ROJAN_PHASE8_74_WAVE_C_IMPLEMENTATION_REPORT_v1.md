# ROJAN AI — TEAM 3 — PHASE 8.74 — MISSING-GUARD SWEEP WAVE C (INVENTORY + ACCOUNTING) — IMPLEMENTATION REPORT v1

**Type:** Implementation. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `a5be831`
**Reference:** `ROJAN_PHASE8_73_WAVE_C_INVENTORY_ACCOUNTING_SCOPE_REVIEW_v1.md`
**Result:** Build **0 / 0** · Full suite **2,654 / 2,654 PASS** · Architecture **7 / 7 PASS**

---

## A. FILES CHANGED

`git diff --stat` — **7 files, 508 insertions(+), 35 deletions(-)**. No new file.

| Group | File | Change |
|---|---|---|
| **Production (3)** | `src/…/ViewModels/Inventory/InventoryPageViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; `CreateProductAsync` / `AddCategoryAsync` / `AddSupplierAsync` wrapped in `try`/`catch` |
| | `src/…/ViewModels/Inventory/InventoryProfileViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; `RecordTransactionAsync` / `MapServiceAsync` / `UnmapServiceAsync` wrapped in `try`/`catch` |
| | `src/…/ViewModels/Accounting/AccountingPageViewModel.cs` | `+ using …Localization;`; `+ _actionErrorMessage` / `_hasActionError` fields; `+ ActionErrorMessage` / `HasActionError` properties; `CancelInvoiceAsync` wrapped in `try`/`catch` |
| **Test stub (1)** | `tests/…/Inventory/StubInventoryCommandService.cs` | `+ Exception?` seams: `CreateProductException`, `CreateCategoryException`, `CreateSupplierException`, `RecordStockTransactionException`, `MapProductToServiceException`, `UnmapProductFromServiceException` |
| **Test VMs (3)** | `tests/…/Inventory/InventoryPageViewModelTests.cs` | `+ using …Localization;`; **+5 tests** |
| | `tests/…/Inventory/InventoryProfileViewModelTests.cs` | `+ using …Localization;`; **+5 tests** |
| | `tests/…/Accounting/AccountingPageViewModelTests.cs` | `+ using …Localization;`; **+3 tests** |

**Not touched:** `Strings.cs` + all `.resx` (`Common_ActionFailedMessage` already ships from Wave A `794648e`); every Inventory / Invoice / Payment service + interface + DTO + request record; Application-layer services; `Fake*Repository`; DI; ViewModel constructors; `ILoggerFactory` plumbing; RBAC / `CanExecute`; **`PosCheckoutViewModel`** (incl. `ChargeAsync` double-charge risk); `InvoiceProfileViewModel`; `AsyncRelayCommand`; `App.xaml.cs`; navigation; `LoadAsync` / `SearchAsync`; every `[LoggerMessage]` signature. No existing test body changed. No Accounting stub change.

---

## B. INVENTORY GUARDS

### B.1 One additive property pair per ViewModel

Both `InventoryPageViewModel` and `InventoryProfileViewModel` gained (private-set, `SetProperty`, additive, **no constructor / DI change**):

```csharp
public string? ActionErrorMessage { get; private set; }   // non-destructive: never touches State/ErrorMessage
public bool    HasActionError      { get; private set; }
```

Same shape as `HrPageViewModel` / `EmployeeProfileViewModel.ActionErrorMessage` (Wave B).

### B.2 Per-method transformation (identical across all 6)

```csharp
// unchanged: CanExecute predicate + early-return validation + request-building stay ABOVE the try
if (SelectedNewProductCategory is null || SelectedNewProductSupplier is null) { return; }
var request = new CreateProductRequest(...);

try
{
    var created = await _commandService.CreateProductAsync(request).ConfigureAwait(true);
    ActionErrorMessage = null; HasActionError = false;   // clear on success

    NewProductSku = string.Empty; /* … 5 more field clears … */
    await LoadAsync().ConfigureAwait(true);
    SelectedProduct = Products.FirstOrDefault(p => p.Id == created.Id);
}
#pragma warning disable CA1031 // Command boundary: a failed write must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A).
catch (Exception)
#pragma warning restore CA1031
{
    ActionErrorMessage = Strings.Common_ActionFailedMessage;
    HasActionError = true;
    LogOperationFailed(nameof(CreateProductAsync));
}
```

| ViewModel | Method | `catch` → | Success-path preserved (inside `try`) |
|---|---|---|---|
| `InventoryPageViewModel` | `CreateProductAsync` | `ActionError` + `LogOperationFailed(nameof(CreateProductAsync))` | 6 `NewProduct*` field clears, `await LoadAsync()`, re-select created row |
| | `AddCategoryAsync` | `nameof(AddCategoryAsync)` | `NewCategoryName` clear, `Categories.Add(created)` |
| | `AddSupplierAsync` | `nameof(AddSupplierAsync)` | `NewSupplierName` clear, `Suppliers.Add(created)` |
| `InventoryProfileViewModel` | `RecordTransactionAsync` | `nameof(RecordTransactionAsync)` | `TransactionQuantity = 1`, `TransactionNotes` clear, `await LoadAsync()` |
| | `MapServiceAsync` | `nameof(MapServiceAsync)` | `NewMappingServiceName` clear, `NewMappingQuantityPerService = 1`, `await LoadAsync()` |
| | `UnmapServiceAsync` | `nameof(UnmapServiceAsync)` | `await LoadAsync()` (after the `mapping is null` early-return, which stays outside the `try`) |

- **`catch (Exception)` with no exception variable** in all 6 → `Exception.Message` / backend body / SKU / cost / supplier / stock values structurally unreachable on screen and in the log.
- **Inventory consistency:** every `InventoryProfileViewModel` write is followed by the authoritative `await LoadAsync()` **inside** the guarded block. On failure that reload never runs, so `Stock` / `RecentTransactions` / `ServiceMappings` keep their last-known-good values (test-asserted: `Stock.QuantityOnHand == 42` unchanged, `RecentTransactions` still 1). `AddCategory` / `AddSupplier` append the returned DTO only after the await succeeds → a failure adds nothing (test-asserted). **No manual inventory state recovery** was added — nothing to recover.
- **Logging:** each catch reuses the ViewModel's **existing** instance-form `[LoggerMessage(EventId = 1, Level = Error, "… Operation={Operation}")] private partial void LogOperationFailed(string operation)` — once, operation-name-only. Both classes keep their single `ILogger` field → no `SYSLIB1020`.

---

## C. ACCOUNTING GUARD — `AccountingPageViewModel.CancelInvoiceAsync`

```csharp
private async Task CancelInvoiceAsync()
{
    if (SelectedInvoice is null) { return; }          // unchanged — outside the try

    var invoiceId = SelectedInvoice.Id;               // unchanged — outside the try

    try
    {
        await _invoiceCommandService.CancelInvoiceAsync(invoiceId).ConfigureAwait(true);   // unchanged
        ActionErrorMessage = null; HasActionError = false;

        await LoadAsync().ConfigureAwait(true);                                             // unchanged
        SelectedInvoice = Invoices.FirstOrDefault(invoice => invoice.Id == invoiceId);     // unchanged
    }
#pragma warning disable CA1031 // Command boundary: a failed invoice cancel must surface inline, not via the global dialog — same justified broad catch as Services.ServicePageViewModel.CreateServiceAsync (Wave A). No payment logic and no retry loop is introduced.
    catch (Exception)
#pragma warning restore CA1031
    {
        ActionErrorMessage = Strings.Common_ActionFailedMessage;
        HasActionError = true;
        LogOperationFailed(_logger, nameof(CancelInvoiceAsync));   // static form — ILogger passed explicitly
    }
}
```

- **Invoice-cancellation logic:** unchanged — the same single `await _invoiceCommandService.CancelInvoiceAsync(invoiceId)` call, the same `await LoadAsync()` refetch, the same re-select-by-id. The `try` only wraps them.
- **Payment behaviour:** none in this method — `CancelInvoiceAsync` is not a payment operation. `IPaymentCommandService` / `PosCheckoutViewModel` / `ChargeAsync` are untouched. **No idempotency assumption and no retry loop** is introduced; the command stays gated by `CanExecute` (`SelectedInvoice is not null && Status != Cancelled`).
- **Rollback / state rules:** none existed and none added — the method has no local state to roll back; a failure only surfaces `ActionErrorMessage`.
- **Static-form logging preserved:** `LogOperationFailed(_logger, nameof(CancelInvoiceAsync))` — the class holds two `ILogger` fields (`_logger`, `_posCheckoutLogger`), so the existing static-form `[LoggerMessage] private static partial void LogOperationFailed(ILogger logger, string operation)` is reused exactly as `LoadAsync` / `SearchAsync` already call it. No new logger, no signature change, no `SYSLIB1020`.

---

## D. BEHAVIOR PRESERVATION

| Concern | Status |
|---|---|
| **Validation** | preserved — `CreateProductAsync`'s `SelectedNewProductCategory/Supplier is null` early-return and `UnmapServiceAsync`'s `mapping is null` early-return stay outside the `try`, byte-identical. `CancelInvoiceAsync`'s `SelectedInvoice is null` early-return likewise. |
| **`CanExecute` gating** | preserved — every command predicate (`!string.IsNullOrWhiteSpace(...)`, `SelectedNewProduct* is not null`, `TransactionQuantity != 0`, `NewMappingQuantityPerService > 0`, `SelectedInvoice.Status != Cancelled`) is unchanged and unmoved. |
| **Service calls** | preserved — same method, same arguments, same order for all 7. |
| **Reload flow / authoritative `LoadAsync`** | preserved — kept inside the guarded block for `CreateProductAsync`, all 3 `InventoryProfileViewModel` methods, and `CancelInvoiceAsync`; `LoadAsync` is self-guarded so a reload failure cannot propagate into the command catch (Wave A/B `CustomerProfileViewModel.SaveChangesAsync` precedent). |
| **Inventory consistency** | preserved — no local stock/mapping mutation happens without a subsequent authoritative reload; page adds append the returned DTO only post-await. A failed command leaves the view unchanged (page adds) or forcibly re-synced to backend truth (profile reloads). No manual recovery path added. |
| **Accounting / payment / rollback logic** | unchanged — `CancelInvoiceAsync` is not a payment op; `PosCheckoutViewModel` untouched. |
| **`State` / destructive error** | never set by these guards — a failed Inventory/Accounting command does not blank the page. `DashboardState` keeps its `Loaded` / `Empty` value. |
| **`LoadAsync` / `SearchAsync`** | untouched — including the pre-existing `ErrorMessage = exception.Message` Load-boundary surfacing (separate deferred P2). |

---

## E. SECURITY REVIEW

| Domain | Sensitive data on a failed command | Wave C exposure |
|---|---|---|
| **Inventory** | SKU, supplier id/name, unit price / cost, on-hand & reorder stock values, product description | **None.** `catch (Exception)` binds no variable → on-screen text is the constant `Strings.Common_ActionFailedMessage`; the log gets only `Operation=<Method>` via the operation-name-only `[LoggerMessage]`. Form values (SKU, price, names) stay in the bound `NewProduct*` / `NewMapping*` / `Transaction*` properties for retry — in-memory, never logged, never placed in `ActionErrorMessage`. |
| **Accounting** | invoice amounts (subtotal/tax/total), payment details, customer billing name/id, receipt text | **None.** Same two mechanisms. `CancelInvoiceAsync` logs `Operation=CancelInvoiceAsync` only; `invoiceId` is a local — not logged. Never reads/forwards any invoice/payment field into `ActionErrorMessage` or the logger. |

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | unreachable (no exception variable; constant string only) |
| `Exception.Message` / `.ToString()` → log file | unreachable (`LogOperationFailed` has no `Exception` parameter) |
| Backend response payload | unreachable on both surfaces |
| Internal identifiers (product / category / supplier / mapping / invoice GUIDs) | not logged (operation name only), not shown (generic string only) |

**Test-enforced:** Inventory failure tests seed `InventoryBackendSecret = "backend 500: SKU=WIDGET-9 cost=42.50 supplier=Acme Corp on-hand=7"` (profile: the existing `Secret = "SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"`); Accounting seeds `InvoiceBackendSecret = "backend 500: invoice INV-8 total=1,850,000 customer=Amelia Hart card=****4242"`. Each asserts `Assert.DoesNotContain(secret, …)` against both `logger.Entries` and `ActionErrorMessage`.

---

## F. TESTS

**+13 tests** (2,641 → 2,654). No existing test modified. Reuses `RecordingLogger<T>` and the existing stubs; `StubInventoryCommandService` gained additive `Exception?` seams only (null path byte-identical — all pre-existing Inventory tests still pass unchanged). **No Accounting stub change** — the `CancelInvoiceAsync` failure tests use the pre-existing `cancelInvoice` constructor delegate.

### F.1 `InventoryPageViewModelTests.cs` — +5

| Test | Asserts |
|---|---|
| `CreateProductCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesForm` | no throw; `HasActionError`; message `== Strings.Common_ActionFailedMessage`; `State != Error`; `NewProductSku`/`Name` preserved; command attempted |
| `AddCategoryCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAppend` | no throw; error set + message; `Categories` does not contain the new name; `NewCategoryName` preserved |
| `AddSupplierCommand_Failure_DoesNotThrow_SetsActionErrorAndDoesNotAppend` | no throw; error set; `Suppliers` unchanged; `NewSupplierName` preserved |
| `CreateProductCommand_Failure_LogsOperationNameOnly_NoSkuOrCostLeak` | `Error` entry + `Operation=CreateProductAsync`; `DoesNotContain(InventoryBackendSecret)` in entries **and** `ActionErrorMessage` |
| `CreateProductCommand_SuccessAfterFailure_ClearsActionError` | fail → `HasActionError` true; clear seam + resubmit → false, `ActionErrorMessage` null, form cleared |

### F.2 `InventoryProfileViewModelTests.cs` — +5

| Test | Asserts |
|---|---|
| `RecordTransactionCommand_Failure_DoesNotThrow_SetsActionError_PreservesStockAndInput` | no throw; error set + message; `State == Loaded`; `Stock.QuantityOnHand == 42` unchanged; `RecentTransactions` still 1; `TransactionQuantity`/`Notes` preserved; command attempted |
| `MapServiceCommand_Failure_DoesNotThrow_SetsActionError` | no throw; error set + message; `NewMappingServiceName` preserved |
| `UnmapServiceCommand_Failure_DoesNotThrow_SetsActionError` | no throw; error set + message; `ServiceMappings` still 1 |
| `RecordTransactionCommand_Failure_LogsOperationNameOnly_NoLeak` | `Error` entry + `Operation=RecordTransactionAsync`; `DoesNotContain(Secret)` in entries **and** `ActionErrorMessage` |
| `MapServiceCommand_SuccessAfterFailure_ClearsActionError` | fail → true; clear seam + resubmit → false, `ActionErrorMessage` null |

### F.3 `AccountingPageViewModelTests.cs` — +3

| Test | Asserts |
|---|---|
| `CancelInvoiceCommand_Failure_DoesNotThrow_SetsActionErrorAndPreservesInvoiceListAndSelection` | no throw; `HasActionError`; message; `State != Error`; `Invoices` still 1; `SelectedInvoice.Id == "invoice-1"` unchanged; command attempted (`CancelledInvoiceIds` has 1) |
| `CancelInvoiceCommand_Failure_LogsOperationNameOnly_NoFinancialLeak` | `Error` entry + `Operation=CancelInvoiceAsync`; `DoesNotContain(InvoiceBackendSecret)` in entries **and** `ActionErrorMessage` |
| `CancelInvoiceCommand_SuccessAfterFailure_ClearsActionError` | fail → true; flip delegate + re-execute → `HasActionError` false, `ActionErrorMessage` null |

---

## G. VALIDATION

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020 / CA1031 / CA1848)
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

| Expected (Phase 8.74) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,655 PASS | 2,654 / 2,654 | ✅ (13 added; ~2,655 was a conservative upper bound) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## H. COMMIT READINESS

**Not committed** (per Phase 8.74 STRICT SCOPE). Ready for Phase 8.75 commit scope review.

- **Exactly 7 modified tracked files** (3 prod + 1 stub + 3 test):
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
- No new file. No `Strings.cs` / `.resx` change. No service / DI / interface / DTO / RBAC / navigation / `[LoggerMessage]`-signature / `PosCheckoutViewModel` / accounting-or-payment-logic change.
- Recommended commit (single, per scope review §G): `fix(desktop): guard inventory and invoice-cancel command failures`.
- Untracked `ROJAN_*.md` reports remain unstaged.

---

## STOP

Phase 8.74 implementation complete. 7 command guards — `InventoryPageViewModel` ×3, `InventoryProfileViewModel` ×3, `AccountingPageViewModel.CancelInvoiceAsync` ×1 — each reusing the Wave A/B pattern + the existing `[LoggerMessage]` + the existing `Common_ActionFailedMessage`; one additive `ActionErrorMessage`/`HasActionError` pair per VM. No service / DI / RBAC / accounting-or-payment-logic / localization-file / `PosCheckoutViewModel` change. Build 0/0, **2,654/2,654** tests, architecture 7/7.
**Next: Phase 8.75 — Wave C Commit Scope Review.** Awaiting authorization.
