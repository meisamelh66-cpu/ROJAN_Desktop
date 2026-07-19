# Phase 13 — Enterprise Service Catalog & Availability Foundation

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a fifth real business module - Services - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by Dashboard (Phase 06B), Customer CRM (Phase 09/10),
Bookings (Phase 11), and Specialists (Phase 12), and wire it into Shell
navigation, replacing the "services" `PlaceholderModule` one-for-one (the
placeholder's `id`/title already matched, no rename needed this time).
Foundation scope: browse/search the catalog, view category/duration/
price/description, and manage which specialists are assigned to each
service - deliberately no catalog-authoring (create/update-service)
commands, no Calendar, no Payment.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Services`): `Service` record;
      `ServiceCategory` (`Hair`/`Colour`/`Nails`/`Skin`/`Spa`/`Consultation`);
      `ServiceStatus` (`Active`/`Seasonal`/`Discontinued`);
      `SpecialistService` - the specialist-to-service assignment mapping,
      with `SpecialistId`/`SpecialistName` deliberately free-form/
      unvalidated (same reasoning as `Bookings.Booking.SpecialistName`):
      this vertical slice does not depend on `Domain.Specialists`, per the
      Independence goal in `docs/architecture/00-overview.md` §2, though
      seed data uses matching names for a cohesive demo; `IServiceRepository`
      (`GetServicesAsync`, `GetServiceByIdAsync`, `GetAssignedSpecialistsAsync`,
      `AssignSpecialistAsync`, `UnassignSpecialistAsync` - deliberately no
      create/update-service methods, since nothing in Application calls
      them: catalog authoring wasn't requested this phase).
- [x] **Application** (`Rojan.Desktop.Application/Services`):
      `IServiceQueryService`/`ServiceQueryService` (list +
      `SearchServicesAsync` by name/category/description, composing over
      `GetServicesAsync` same as `Customers.CustomerQueryService.SearchCustomersAsync`);
      `IServiceProfileQueryService`/`ServiceProfileQueryService` (service
      + assigned specialists as one aggregate fetch);
      `IServiceCommandService`/`ServiceCommandService` - the one write
      capability this phase requested, specialist assignment only
      (`AssignSpecialistAsync`/`UnassignSpecialistAsync`); `ServiceMapper`.
      Registered in `AddApplication()` (aliased `AppServices` in the DI
      file to avoid any visual confusion with `IServiceCollection`/
      `ServiceCollectionExtensions` in the same file - same names,
      unrelated concepts).
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Services`):
      `FakeServiceRepository` - mutable instance state (DI singleton), 9
      seed services covering every category and status (7 Active, 1
      Seasonal, 1 Discontinued), with specialist assignments referencing
      the specialist names already seeded in
      `Specialists.FakeSpecialistRepository` for a cohesive demo.
      Registered in `AddInfrastructure()` (also aliased for the same
      `IServiceCollection` naming-clarity reason).
- [x] **Presentation**: `ServicePageViewModel` (catalog, search - no "Add
      Service" form, matching the scoped-out create/update-service
      commands) plus `ServiceProfileViewModel` (category/status,
      duration/price via the existing `KPIValue` control, description,
      assigned specialists display/assign/unassign) - same
      list-plus-per-selection-profile split `Customers.CustomerPageViewModel`/
      `CustomerProfileViewModel` established in Phase 10, minus editable
      status (only display was requested); `ServicePage.xaml` (same
      `DashboardCard`/`DashboardWidget`/`KPIValue` shape as every other
      module, no new Design System components); `ServiceModule`.
- [x] **Shell wiring**: `App.xaml.cs` - the `"services"` `PlaceholderModule`
      registration replaced with `services.AddSingleton<IModule, ServiceModule>()`
      (same id/title/order/glyph, no rename needed since the placeholder
      was already named "Services"). `Views.xaml` DataTemplate mapping
      added. `MainWindow`, `MainWindowViewModel`, `NavigationService`, and
      `ModuleRegistry` all unchanged - the one-line swap the Phase 07
      module system was designed for, same as Bookings/Specialists.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **No catalog authoring.** There is no way to create, edit, or retire a
  service through the UI - the 9 seed services are the entire catalog for
  this phase. A deliberate scope decision (only "Specialist-service
  assignment commands" were requested at the Application layer), not an
  oversight; revisit if catalog management becomes a real requirement.
- **No cross-slice link to Specialists.** `SpecialistService.SpecialistName`
  (free text) and `Specialist.FullName` are not connected by anything
  beyond matching seed-data strings - a real link is a future integration
  point, out of scope here (and orthogonal to the explicit "Do not
  implement Calendar" constraint, which would be the more likely place
  such a link eventually matters).
- **`Price` is a formatted string, not a decimal** - consistent with how
  every other vertical slice in this app represents money
  (`Customer.LifetimeValue`, `Booking` has no price field yet), but means
  no arithmetic (totals, discounts) can be done on it without a future
  type change. Explicitly out of scope ("Do not implement Payment").

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 207/207 tests passed (42 new):
      Domain.Tests 16 (+2), Application.Tests 72 (+20: ServiceQueryService
      including search and full category/status Theory coverage,
      ServiceProfileQueryService, ServiceCommandService),
      Infrastructure.Tests 41 (+7: FakeServiceRepository),
      Presentation.Tests 74 (+13: ServicePageViewModel,
      ServiceProfileViewModel), ArchitectureTests 4 (unchanged - still
      passing, confirming Services follows the same dependency-direction
      and ViewModel-testability rules as every other slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to Services, confirmed the seeded catalog/profile
      rendered correctly (category, status, duration/price via KPIValue,
      assigned-specialist chips), selected a Discontinued service with no
      assignments and confirmed the empty state, searched "spa" and
      confirmed the list filtered to and reselected the one match
      (Massage), assigned a new specialist ("Riley Chen") to Manicure and
      watched the chip appear live alongside the existing assignment.
- [x] No database, no API, no external integrations, no Calendar, no
      Payment - `FakeServiceRepository` remains in-memory only, `Price`
      stays a display string with no payment-processing logic anywhere.
- [x] Clean Architecture boundaries unchanged - `Domain.Services` has no
      outward dependency, `Application.Services` depends only on
      `Domain.Services`, `Presentation` depends only on
      `Application.Services` - verified by the unmodified, still-passing
      `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
