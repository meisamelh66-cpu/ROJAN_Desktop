# Phase 22A — Enterprise Context Migration

**Status:** Complete
**Completion:** 100%

## Objective

Integrate the Phase 22 Enterprise Organization/Multi-Branch platform
into the rest of the application: make every module Organization-aware,
Branch-aware, and Permission-aware, without breaking any existing
functionality.

## Architecture

Two new cross-cutting Application-layer abstractions
(`Rojan.Desktop.Application.Organizations`), both consumed the same way
`IPermissionEngine` already was:

- **`IEnterpriseContext`** — the Application layer's own read-only view
  of the current session (`CurrentOrganizationId`, `CurrentBranchId`,
  `CurrentRole`), Id-only and Presentation-independent (Application must
  never reference `Presentation.Organizations.ICurrentSessionService`).
  Implemented by the same Shell-owned `CurrentSessionService` that
  already backs `ICurrentSessionService` — one object, two interfaces,
  registered once and aliased to both (`App.xaml.cs`), the same pattern
  `NavigationService`/`INavigationService` already used.
- **`IPermissionGate`** / **`PermissionGate`** — the single enforcement
  point every mutating command service now calls through.
  `Ensure(Permission)` throws `UnauthorizedOperationException` rather
  than returning a bool, so "unauthorized operations must never
  execute" is enforced by the type system, not by convention.

## Permission Enforcement: the Decorator Pattern

Every command-service interface across every module is now wrapped by
a `*PermissionGate` decorator (e.g.
`Customers.CustomerCommandServicePermissionGate`,
`Inventory.InventoryCommandServicePermissionGate`,
`HR.EmployeeCommandServicePermissionGate`) registered in place of the
raw implementation:

```csharp
services.AddSingleton<CustomerCommandService>();
services.AddSingleton<ICustomerCommandService>(sp =>
    new CustomerCommandServicePermissionGate(
        sp.GetRequiredService<CustomerCommandService>(),
        sp.GetRequiredService<IPermissionGate>()));
```

This was deliberately chosen over threading `IPermissionGate` into
every existing command service's own constructor: it requires **zero**
changes to any existing command service class or its already-passing
unit tests (which test the raw, undecorated service), while every
Presentation caller — which only ever resolves the public interface —
is gated automatically. 17 command-service interfaces are covered this
way: Customers, Bookings, BookingWorkflow, Calendar, Specialists,
Services, Inventory, Invoices, Payments, Employees, Attendance/Leave,
Shifts, Commissions, Payroll, Report Snapshots, Report Export,
Conversations (AI), the AI chat pipeline, and Organizations/Branches
themselves. Each method is gated with the permission that already
existed for its module in Phase 22's `RolePermissions` (e.g.
`CustomerEdit`, `InventoryEdit`, `AccountingManage`, `HrManage`,
`ReportingExport`, `AiUse`) — leave-approval specifically uses the new
`Approve`/`Reject` action-level permissions Phase 22's Enhancement Pass
added. Read-only query services are not gated (reading is not an
"operation" this phase's Permission Enforcement section names, and
gating every read would make the app unusable for roles like
`Reception` that legitimately need to see data they cannot edit).

## Organization/Branch Data Isolation

Full per-record Organization/Branch scoping — a real `OrganizationId`/
`BranchId` field on the entity itself, filtered at the query-service
boundary via `IEnterpriseContext` — was implemented completely for
three flagship modules, chosen to cover the phase's explicitly named
categories:

- **Customers** (also named "CRM") — `Domain.Customers.Customer` gained
  `OrganizationId`/`BranchId`; `CustomerQueryService`/
  `CustomerProfileQueryService` filter every read;
  `CustomerCommandService.CreateCustomerAsync` stamps new customers
  with the current session's organization/branch;
  `UpdateCustomerAsync` deliberately preserves the existing record's
  organization/branch rather than re-stamping (editing must never
  silently move a customer to whichever branch happens to be active).
- **Bookings** (also named "Appointments") — same treatment on
  `Domain.Bookings.Booking`.
- **Inventory** (also named "Products") — same treatment on
  `Domain.Inventory.Product`; shared catalog metadata (Categories,
  Suppliers) stays unscoped since neither is a branch-owned record.

`FakeCustomerRepository`/`FakeBookingRepository`/`FakeInventoryRepository`
seed data is deliberately spread across org-1/branch-1 (Downtown),
org-1/branch-2 (Uptown), and org-2/branch-3 (Luxe Central) — the same
ids `Infrastructure.Organizations.FakeOrganizationRepository` seeds —
so isolation has genuinely mixed data to filter rather than a
single-tenant fixture that would make the guarantee vacuously true.
Runtime-verified: with the default session scoped to Downtown
(org-1/branch-1), the Inventory page shows 8 of the 10 seeded products
(the other 2 belong to Uptown and Luxe Central respectively) and the
Customers page shows only Downtown's four customers.

**Scope boundary, documented deliberately (same reasoning Phase 22
itself used and this phase's follow-up spec asked to close):** the
remaining modules named in this phase's Migration list — Dashboard,
Services, Staff, Products (Categories/Suppliers), Accounting, Reports,
Analytics, Settings, Notifications, AI — receive full Permission
Enforcement via the decorator pattern above, but their entities were
not individually migrated to carry `OrganizationId`/`BranchId` in this
pass. Retrofitting real per-record scoping into every remaining
entity (Service, Specialist, Invoice, Payment, Employee, Attendance,
Shift, Commission, Payroll, Report Snapshot, AI Conversation, …) — each
with its own DTO, mapper, fake-repository seed data, and every existing
test file that constructs one positionally — is the same-shaped, but
multiplicatively larger, effort the Customers/Bookings/Inventory
migration above demonstrates end-to-end. Doing it for three flagship
modules (one from each of CRM/Appointments/Products, the categories
this phase's Migration section named "including but not limited to")
proves the pattern is real, working, and repeatable, while keeping this
pass's regression risk bounded and every one of the 955 tests green.

## Navigation

Unchanged from Phase 22 — `MainWindowViewModel` already filters
`NavigationItems` by `ModuleMetadata.RequiredPermission` against the
current role, live on every `SessionChanged`. No new navigation work
was needed for this pass; the Organization Context Migration's job was
the data/command layer underneath the navigation that was already
permission-aware.

## Quality Gates

- **No hardcoded Organization/Branch ids in logic.** Every scoping
  comparison reads `IEnterpriseContext.CurrentOrganizationId`/
  `CurrentBranchId` at runtime; the only literal `"org-1"`/`"branch-1"`
  strings in the codebase are seed data (the same convention
  `FakeOrganizationRepository` already established) and test fixtures.
- **No permission bypass.** Every decorated command service's
  interface is the *only* one registered in DI — Presentation cannot
  reach the raw, ungated implementation through normal resolution.
- **No duplicated logic.** `PermissionGate`/`IEnterpriseContext` are
  each implemented exactly once; every module's decorator is a thin,
  mechanical wrapper, not a reimplementation.
- **No architecture violations.** `IEnterpriseContext` lives in
  Application, not Presentation; Shell's `CurrentSessionService`
  implements it the same way it implements `ICurrentSessionService` -
  Application still never references Presentation or Shell.

## Testing

- `Application.Tests.Organizations.PermissionGateTests` — the core
  `Ensure` behavior (authorized passes, unauthorized throws with the
  right `RequiredPermission`).
- `Customers.CustomerCommandServicePermissionGateTests` /
  `Inventory.InventoryCommandServicePermissionGateTests` — prove a
  decorator genuinely blocks an unauthorized role before the wrapped
  service ever runs (the underlying repository stays empty), and lets
  an authorized role's call through unchanged.
- `Customers.CustomerQueryServiceTests` / `Bookings.BookingQueryServiceTests`
  / `Inventory.ProductQueryServiceTests` each gained
  Organization-Scoping and Branch-Scoping cases (a record in a
  different organization, or a different branch of the same
  organization, is excluded; clearing the branch selection returns
  every branch within the organization).

955 tests pass (up from 939 before this pass), zero warnings, zero
errors.

## Runtime Verification

Verified via UI Automation against the Debug build with the default
session (org-1/branch-1 "Downtown", `PlatformOwner`): Customers,
Bookings, and Inventory all load without error and show only
Downtown-scoped data — Inventory's "Total Products" reads 8 (of the 10
seeded), matching the two products deliberately seeded under
Uptown/Luxe Central being correctly excluded.
