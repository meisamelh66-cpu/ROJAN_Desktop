# Phase 18 — Enterprise POS & Accounting Foundation

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add an eighth real business module - Accounting - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by every prior module (Phases 10–17A), replacing the
"accounting" placeholder sidebar entry one-for-one. Integrate with the
existing Booking, Customer, and Inventory slices without modifying any
of their own code. No changes to Clean Architecture, navigation, or
dependency injection structure beyond the one-line registrations every
prior module makes.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Accounting`): `Invoice`/
      `InvoiceItem`/`Payment`/`Receipt`/`CashSession` (five aggregate
      types in one repository interface, consistent with the "one
      repository per slice" convention Phase 17 already stretched to six
      aggregate types), plus `PaymentMethod`/`InvoiceStatus`/
      `CashSessionStatus` enums. New `InvoiceCalculator` (line total,
      subtotal, tax, total) and `InvoicePaymentRules` (payment-amount
      validation, payment-driven status transitions: no payment ->
      `Issued`, partial -> `PartiallyPaid`, full or over -> `Paid`) -
      genuine Domain business rules, same pattern as Phase 15's
      `BookingRules` and Phase 17's `StockTransactionRules`.
      Deliberately uses `decimal` for every monetary field instead of
      the display-only string-money convention every prior module used
      (`Service.Price`, `Product.UnitPrice`, `Booking.Price`) - this is
      the first module doing genuine monetary arithmetic (summing line
      items, computing tax, tracking balances, computing change) rather
      than pure display, the same kind of justified type-vocabulary
      departure Phase 14 made when introducing `DateOnly`/`TimeSpan`.
      `IAccountingRepository` stays "dumb" (raw reads/writes only) -
      `InvoiceCalculator`/`InvoicePaymentRules` are composed in
      Application, consistent with the "return the read-set, compose in
      Application" convention every prior module follows.
- [x] **Application** (`Rojan.Desktop.Application/Accounting`):
      `IInvoiceQueryService` (list/search/profile, composing over
      `GetInvoicesAsync` for search rather than a dedicated repository
      search method, same convention as every other module) and
      `GetCheckoutOptionsAsync` - composes over Customers/Bookings/
      Services/Inventory's own query services to build the POS
      checkout's cart options (the "Integrate with Booking, Customer,
      Inventory" requirement), the same cross-slice orchestration
      `BookingWorkflow.BookingWorkflowService` established in Phase 15.
      `IInvoiceCommandService.CreateInvoiceAsync` computes totals via
      `InvoiceCalculator`, writes the invoice and its line items, and
      for every line item with a product id calls Inventory's own
      `IInventoryCommandService.RecordStockTransactionAsync` to
      decrement stock (the "Integrate with Inventory" requirement) -
      Inventory's own command service is used exactly as published,
      never modified. `IPaymentCommandService.RecordPaymentAsync`
      re-derives the invoice's status via `InvoicePaymentRules` after
      each payment and issues a receipt - the several writes that
      together are "recording a payment", same multi-write-as-one-use-
      case pattern as Phase 15's booking-confirmation flow.
      `IPaymentQueryService.GetRevenueSummaryAsync` composes the Revenue
      KPI numbers (total/today revenue, outstanding balance, paid/
      outstanding invoice counts) over `GetPaymentsAsync`/
      `GetInvoicesAsync`, same "compose in Application" convention.
      `AccountingMapper.ParseMoney` is the one place Accounting parses
      another slice's string-money DTOs (`ProductDto.UnitPrice`,
      `ServiceDto.Price`) into `decimal`, at the boundary where it reads
      those DTOs - it never modifies Inventory/Services' own code.
      Registered in `AddApplication()`.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Accounting`):
      `FakeAccountingRepository` - 8 seed invoices spanning every
      `InvoiceStatus` (4 `Paid`, 2 `PartiallyPaid`, 1 `Issued`, 1
      `Cancelled`), cross-referencing real Booking/Customer/Service/
      Product ids already seeded in `Bookings.FakeBookingRepository`/
      `Customers.FakeCustomerRepository`/`Services.FakeServiceRepository`/
      `Inventory.FakeInventoryRepository` for a cohesive demo (one
      invoice - a retail walk-in sale - deliberately has no booking,
      demonstrating a standalone POS sale); 16 invoice line items, 6
      payments (Cash and Card), 6 receipts, and 2 cash sessions (one
      closed historical session, one open session ready for the POS
      flow to charge against). Registered in `AddInfrastructure()`.
- [x] **Presentation**: `AccountingPageViewModel` (Revenue KPI cards,
      searchable invoice list, a "New Sale (POS)" button, and the
      selected invoice's read-only `InvoiceProfileViewModel` - line
      items, payments, receipts, plus a Cancel Invoice action) - same
      master-detail shape as every prior module's page. `PosCheckoutViewModel`
      - a linear Cart → Payment → Receipt wizard-dialog shown in Shell's
      dialog region via the existing `IDialogService`, same pattern as
      `BookingWorkflow.BookingWizardViewModel` - fulfilling both the
      "POS checkout page" and "Payment dialog" deliverables in one
      dialog surface, since Shell's dialog region only supports one
      active dialog at a time; Payment is realized as the wizard's
      Payment step, not a separately-launched nested dialog. `AccountingModule`
      replaces the "accounting" `PlaceholderModule` one-for-one. No new
      Design System components - every card/widget/control reuses
      Phase 16/17A's Fluent styles unchanged.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **POS checkout tax rate is a hardcoded constant (8%), not
  configurable.** `PosCheckoutViewModel.TaxRate` mirrors
  `InvoiceCalculator.ComputeTax`'s rounding for the cart-step preview
  only; the authoritative total always comes back from
  `IInvoiceCommandService.CreateInvoiceAsync`. A tax-configuration
  surface is reasonable future work, not built here to keep this
  phase's scope bounded (matches the "foundation now, wire up later"
  pattern used throughout this app).
- **Cart line items are immutable once added** - no in-cart quantity
  editing; removing and re-adding is the only way to change a
  quantity. A reasonable POS-foundation simplification, not a full
  point-of-sale editing experience.
- **No repository interface split.** `IAccountingRepository` has 14
  methods across five aggregate types, consistent with this codebase's
  "one repository interface per vertical slice" convention rather than
  splitting per aggregate.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 449/449 tests passed (87 new):
      Domain.Tests 80 (+20: record equality smoke coverage for all five
      aggregate types, `InvoiceCalculator` line/subtotal/tax/total and
      midpoint-rounding coverage, `InvoicePaymentRules` amount-validation
      and status-transition coverage), Application.Tests 144 (+24:
      invoice query/search/profile/checkout-options composition and
      filtering, invoice creation with Inventory stock-decrement
      assertions, payment recording with status-transition and receipt
      assertions, revenue-summary composition, cash-session open/close
      guard coverage), Infrastructure.Tests 85 (+19: seeded-data smoke
      tests plus create/update/record round-trips for every aggregate
      type), Presentation.Tests 136 (+24: page/profile/checkout-wizard
      ViewModel load-state, search, cart/totals, step-transition, and
      every command's CanExecute/execution coverage, including dialog-
      service interaction), ArchitectureTests 4 (unchanged - still
      passing, confirming Accounting follows the same dependency-
      direction and ViewModel-testability rules as every other slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the new "Accounting" sidebar entry (now the real
      module, no longer a placeholder); confirmed the Revenue KPI cards
      showed "Total Revenue $582.64", "Today $0.00", "Outstanding
      $719.84", "Paid Invoices 4" - all matching seed data exactly;
      confirmed the invoice list rendered all 8 seeded invoices with
      correct customer/date/total/status; confirmed the invoice detail
      panel rendered line items, payments, and receipts for the
      selected invoice. Ran a full POS sale end-to-end: opened "New
      Sale (POS)", selected a customer and a product (Hydrating Shampoo
      1L, $18) in the Cart step, added it, proceeded to Payment
      (correctly showed "Amount due: $19.44" - $18 + 8% tax), charged
      in full via Cash ("Change due: $0.00"), and confirmed the Receipt
      step ("Payment recorded", "Customer: Amelia Hart", "Paid: $19.44
      via Cash"). After closing the dialog, confirmed the Accounting
      page updated live: Total Revenue $602.08 (+$19.44), Today Revenue
      $19.44 (was $0.00), invoice list grew from 8 to 9 entries, and
      the new invoice showed status "Paid". Confirmed Inventory
      integration by navigating to the Inventory page and selecting
      Hydrating Shampoo 1L: "On Hand" read 41 (down from the seeded 42
      by exactly the 1 unit sold), and its transaction history showed a
      new "Sold · 1" entry with the exact note
      `"Sold via invoice {invoiceId}."` that `InvoiceCommandService`
      writes - confirming the cross-slice Inventory stock decrement
      fired correctly from a real POS sale.
- [x] No changes to the Fluent 2 Design System - `Themes/` files
      untouched except `Views.xaml`'s DataTemplate registry (the same
      one-line addition every prior module made); every Accounting
      control reuses existing shared styles/tokens unchanged.
- [x] Clean Architecture boundaries unchanged - `Domain.Accounting` has
      no outward dependency, `Application.Accounting` depends only on
      `Domain.Accounting` plus the other slices' own Application-layer
      interfaces (Customers/Bookings/Services/Inventory), `Presentation`
      depends only on `Application.Accounting` - verified by the
      unmodified, still-passing `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
