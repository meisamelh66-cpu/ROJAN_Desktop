# Phase 10 — Enterprise Customer CRM Expansion

> **Naming note:** referred to as "Phase 10" throughout, per
> `docs/adr/0001-phase-09-naming-collision.md` - the original Customer CRM
> module is committed/filed as "Phase 09" but is understood as Phase 10
> going forward; this expansion continues that numbering.

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Expand the read-only Phase 09/10 Customer CRM into a full Customer 360
profile, on the same Domain/Application/Infrastructure/Presentation
vertical-slice pattern - no database, no API, fake repository only, no
Shell/module/navigation changes. This is also the first write (command)
use case anywhere in the codebase; Phase 08's Testing Strategy plan
explicitly flagged that no CQRS command pattern existed yet.

## Deliverables

- [x] **Domain**: `CustomerTag`, `CustomerNote`, `CustomerActivity`
      records; `CustomerStatus` expanded from `{Lead, Active, Inactive}`
      to `{Lead, Prospect, Active, Vip, Inactive, Churned}`;
      `ICustomerRepository` expanded with `GetCustomerByIdAsync`,
      `GetNotesAsync`, `GetTagsAsync`, `GetActivityAsync`,
      `CreateCustomerAsync`, `UpdateCustomerAsync`, `AddNoteAsync`,
      `AddTagAsync`, `RemoveTagAsync`, `AddActivityAsync`.
- [x] **Application**: `CustomerProfileDto`/`ICustomerProfileQueryService`/
      `CustomerProfileQueryService` (the profile query - customer + notes
      + tags + activity + computed statistics, one aggregate fetch);
      `ICustomerQueryService.SearchCustomersAsync` (the search query -
      composes over the repository's existing `GetCustomersAsync` rather
      than a new repository method, keeping Domain's contract minimal);
      `ICustomerCommandService`/`CustomerCommandService` (create/update
      customer, add note, add/remove tag - the codebase's first command
      service, every mutation also logs a `CustomerActivity` so the
      Timeline reflects real actions, not just seed data); `CustomerMapper`
      (Domain&lt;-&gt;Application mapping extracted from `CustomerQueryService`
      once a second and third consumer needed the same logic).
- [x] **Infrastructure**: `FakeCustomerRepository` converted from static
      read-only seed data to instance mutable state (a DI singleton, so
      writes persist for the running app's lifetime) implementing every
      new repository method; seed data expanded to 7 customers covering
      all 6 status values, plus notes/tags/activity per customer.
- [x] **Presentation**: `CustomerProfileViewModel` (new - statistics
      cards, tags, notes, timeline, editable status + Save, all for one
      selected customer); `CustomerPageViewModel` extended (search now
      routes through `SearchCustomersAsync` instead of client-side
      filtering, `Profile` property rebuilt on selection change, new
      "Add Customer" form + command); `CustomerPage.xaml` rewritten with
      the full Customer 360 right panel - Statistics/Tags/Notes/Timeline,
      every card/widget reusing the existing `DashboardCard`/
      `DashboardWidget`/`KPIValue` components, no new Design System
      components needed.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **Search now re-queries per keystroke.** `SearchCustomersAsync` composes
  over the repository's `GetCustomersAsync` (which has Infrastructure's
  simulated latency), so rapid typing issues several requests; a
  staleness guard (compare the in-flight search's text against the
  current `SearchText` when it completes) discards out-of-order results,
  but no debounce exists. Acceptable at this data volume; revisit if it
  ever feels laggy.
- **No note/activity deletion.** Only tag removal was in scope
  ("Notes and tags commands" - tags explicitly support remove, notes were
  asked for as add-only). Notes and the Timeline are append-only by
  design for this phase.
- **`CustomerCommandService` always reloads the full profile after a
  mutation** (add note/tag, remove tag, save status) rather than patching
  the ViewModel's collections in place - simpler and guaranteed
  consistent, at the cost of one extra ~200-400ms round trip per action.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 82/82 tests passed: Domain.Tests 10
      (+6 new: CustomerTag/CustomerNote/CustomerActivity equality),
      Application.Tests 27 (+17 new: CustomerProfileQueryService,
      CustomerCommandService, CustomerQueryService.SearchCustomersAsync,
      expanded status Theory), Infrastructure.Tests 17 (+12 new: every
      expanded FakeCustomerRepository method), Presentation.Tests 24
      (+11 new: CustomerProfileViewModel, CustomerPageViewModel search/
      Profile/CreateCustomer behavior), ArchitectureTests 4 (unchanged -
      still passing, confirming the expansion didn't violate the
      dependency-direction or ViewModel-testability rules).
- [x] Runtime verified via UI Automation against the real running app
      (not just unit tests): navigated to Customers, selected a VIP
      customer and confirmed the profile/statistics/tags/notes/timeline
      rendered with correct seeded data, added a tag, added a note,
      created a new customer via the form (correctly defaulted to Lead
      status, $0 lifetime value, auto-selected afterward, form cleared).
- [x] No Shell/module/navigation changes - `CustomerModule.cs`,
      `App.xaml.cs`, `Views.xaml`'s DataTemplate mapping all untouched;
      `CustomerProfileViewModel` is a nested object bound within
      `CustomerPageViewModel`'s own DataContext, never
      NavigationService-resolved.
- [x] No database, no API - `FakeCustomerRepository` remains in-memory
      only, per the constraint.

## Approval

Approved by: <pending> — <date>
