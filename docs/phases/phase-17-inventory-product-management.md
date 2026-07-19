# Phase 17 — Enterprise Inventory & Product Management

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a seventh real business module - Inventory - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by every prior module (Phases 10–16), replacing the
"inventory" placeholder sidebar entry one-for-one. No changes to the
Fluent 2 Design System (Phase 16) beyond automatic reuse of its shared
controls/tokens; no changes to Clean Architecture, navigation, or
dependency injection structure.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Inventory`): the widest single
      repository interface in this app, covering six related aggregate
      types in one vertical slice - `Product` (catalog entry, real
      `CategoryId`/`SupplierId` references within this same slice, per
      the Independence goal in docs/architecture/00-overview.md §2 -
      only cross-slice references stay free-text), `ProductCategory`,
      `Supplier`, `InventoryItem` (stock level - the "Inventory entity"
      deliverable), `StockTransaction` (the "Stock transactions"
      deliverable), and `ServiceProductMapping` (the "Service-to-product
      mapping" deliverable, free-text `ServiceId`/`ServiceName` - same
      cross-slice-independence reasoning as `Services.SpecialistService`).
      New `StockTransactionRules` (a genuine Domain rule: how each
      `StockTransactionType` moves an `InventoryItem`'s on-hand quantity,
      clamped at zero) - `IInventoryRepository` stays "dumb" (raw
      reads/writes only), consistent with the "return the read-set,
      compose in Application" convention every prior module follows.
- [x] **Application** (`Rojan.Desktop.Application/Inventory`):
      `IProductQueryService` (list + search + category/supplier option
      lists - the "Search and filtering" deliverable, composing over
      `GetProductsAsync` rather than a dedicated repository search
      method, same convention as every other module's search)/
      `IProductProfileQueryService` (product + stock + transaction
      history + service mappings, one aggregate fetch)/
      `IInventoryQueryService` (`GetLowStockItemsAsync` - the "Low-stock
      monitoring" deliverable, filtering on `InventoryItemDto.IsLowStock`)/
      `IInventoryCommandService` (create product/category/supplier,
      record a stock transaction - enforcing
      `StockTransactionRules.IsValidQuantity` and applying
      `StockTransactionRules.Apply` before writing, same validation-
      enforcement pattern Phase 15's `BookingCommandService` established
      - and map/unmap a product to a service). Registered in
      `AddApplication()`.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Inventory`):
      `FakeInventoryRepository` - 6 categories, 4 suppliers (one
      deliberately `Inactive`, showing a product still sourced from a
      now-inactive supplier), 10 products (one `Discontinued`), stock
      levels with 4 products deliberately at or below their reorder
      threshold (demonstrating low-stock monitoring live), seed stock
      transactions, and 8 service-to-product mappings cross-referencing
      the real service ids already seeded in `Services.FakeServiceRepository`
      ("service-1".."service-9") for a cohesive demo - not a real
      cross-slice link, just consistent naming, same reasoning as every
      other cross-slice reference in this app. Registered in
      `AddInfrastructure()`.
- [x] **Presentation**: `InventoryPageViewModel` (catalog/search, low-
      stock KPI, quick-add forms for product/category/supplier) +
      `InventoryProfileViewModel` (per-selection child ViewModel - stock
      level, record-a-transaction mini form, transaction history,
      service-mapping chips + map/unmap) - same master-detail shape as
      Customers/Specialists/Services. `InventoryPage.xaml` reuses
      DashboardCard/DashboardWidget/KPIValue and every Phase 16 Fluent
      form-control style (TextBox/ComboBox) automatically - no new
      Design System components, no Theme file changes. `InventoryModule`
      replaces the "inventory" `PlaceholderModule` one-for-one.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **No per-row low-stock badge in the product list.** The catalog list
  shows Product fields only (name/SKU/price/status); low-stock status
  lives on the separate `InventoryItem` aggregate. The aggregate low-
  stock count (KPI) and the per-product Stock card's warning text cover
  the "Low-stock monitoring" deliverable; a per-row visual flag would
  need the ViewModel to join Products with InventoryItems client-side -
  a reasonable follow-up, not built here to keep this phase's scope
  bounded.
- **Category/Supplier quick-add is name-only.** `CreateCategoryAsync`
  takes a description too (defaulted empty from the UI) and
  `CreateSupplierAsync` takes contact/email/phone (also defaulted
  empty) - the full command-layer capability exists, but the page's
  quick-add form only asks for a name, favoring a fast add-then-fill-in-
  later flow over three separate full forms cluttering the page.
- **No repository interface split.** `IInventoryRepository` has 16
  methods across six aggregate types - the widest interface in this
  app - because this codebase's convention is one repository interface
  per vertical slice, not per aggregate. Kept consistent with that
  convention rather than introducing a new pattern for this module
  alone.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 362/362 tests passed (84 new):
      Domain.Tests 60 (+21: record equality smoke coverage for all six
      aggregate types, `StockTransactionRules` quantity-validation and
      apply/clamp coverage), Application.Tests 120 (+25: query
      mapping/search/category/supplier coverage, profile aggregation,
      low-stock filtering, command-service creation/transaction-
      validation/mapping coverage), Infrastructure.Tests 66 (+19: seeded-
      data smoke tests plus create/update/record/map round-trips for
      every aggregate type), Presentation.Tests 112 (+19: page/profile
      ViewModel load-state, search, low-stock-count, and every command's
      CanExecute/execution coverage), ArchitectureTests 4 (unchanged -
      still passing, confirming Inventory follows the same dependency-
      direction and ViewModel-testability rules as every other slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the new "Inventory" sidebar entry (now the real
      module, no longer a placeholder), confirmed the KPI summary showed
      "Total Products: 10" and "Low Stock Items: 4" (both matching seed
      data exactly), confirmed the product catalog list rendered with
      SKU/price/status, selected a low-stock product (Colour Developer
      20 Vol) and confirmed its profile panel resolved category
      ("Colour Products") and supplier ("Radiant Professional Products")
      correctly, and confirmed every form control (TextBox/ComboBox)
      rendered with the Phase 16 Fluent styling automatically, with no
      per-page styling needed.
- [x] No changes to the Fluent 2 Design System - `Themes/` files
      untouched except `Views.xaml`'s DataTemplate registry (the same
      one-line addition every prior module made); every Inventory
      control reuses existing shared styles/tokens unchanged.
- [x] Clean Architecture boundaries unchanged -
      `Domain.Inventory` has no outward dependency, `Application.Inventory`
      depends only on `Domain.Inventory`, `Presentation` depends only on
      `Application.Inventory` - verified by the unmodified, still-passing
      `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
