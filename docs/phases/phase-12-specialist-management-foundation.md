# Phase 12 — Enterprise Specialist Management Foundation

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a fourth real business module - Specialists - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by Dashboard (Phase 06B), Customer CRM (Phase 09/10),
and Bookings (Phase 11), and wire it into Shell navigation, replacing the
"employees" `PlaceholderModule` one-for-one. Foundation scope: a staff
directory with search, status display/edit, and skills management - no
calendar/scheduling integration, no payroll/compensation features.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Specialists`): `Specialist` record
      (deliberately not linked to `Domain.Bookings.Booking.SpecialistName`
      - this vertical slice does not depend on `Domain.Bookings`, per the
      Independence goal in `docs/architecture/00-overview.md` §2, though
      seed data uses matching names for a cohesive demo); `SpecialistStatus`
      (`Active`/`OnLeave`/`Inactive`); `SpecialistSkill`; `ISpecialistRepository`
      (`GetSpecialistsAsync`, `GetSpecialistByIdAsync`, `GetSkillsAsync`,
      `CreateSpecialistAsync`, `UpdateSpecialistAsync`, `AddSkillAsync`,
      `RemoveSkillAsync`).
- [x] **Application** (`Rojan.Desktop.Application/Specialists`):
      `ISpecialistQueryService`/`SpecialistQueryService` (list +
      `SearchSpecialistsAsync`, composing over `GetSpecialistsAsync` same
      as `Customers.CustomerQueryService.SearchCustomersAsync`);
      `ISpecialistProfileQueryService`/`SpecialistProfileQueryService`
      (specialist + skills as one aggregate fetch);
      `ISpecialistCommandService`/`SpecialistCommandService` (create,
      update, add/remove skill); `SpecialistMapper`. Registered in
      `AddApplication()`.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Specialists`):
      `FakeSpecialistRepository` - mutable instance state (DI singleton),
      5 seed specialists (3 matching the specialist names already
      referenced in `Bookings.FakeBookingRepository`'s seed data, plus one
      `OnLeave` and one `Inactive` for full status coverage) with skills
      per specialist. Registered in `AddInfrastructure()`.
- [x] **Presentation**: `SpecialistPageViewModel` (directory, search,
      new-specialist form) + `SpecialistProfileViewModel` (status
      display/edit via `EditableStatus`/`SaveChangesCommand`, skills
      display via `AddSkillCommand`/`RemoveSkillCommand`) - same
      list-plus-per-selection-profile-ViewModel split
      `Customers.CustomerPageViewModel`/`CustomerProfileViewModel`
      established in Phase 10, minus notes/timeline (not requested here);
      `SpecialistPage.xaml` (same `DashboardCard`/`DashboardWidget` shape
      as every other module, no new Design System components);
      `SpecialistModule`.
- [x] **Shell wiring**: `App.xaml.cs` - the `"employees"` `PlaceholderModule`
      registration replaced with `services.AddSingleton<IModule, SpecialistModule>()`;
      sidebar entry renamed id/title from `employees`/"Employees" to
      `specialists`/"Specialists", same order (60) and glyph (◉).
      `Views.xaml` DataTemplate mapping added. `MainWindow`,
      `MainWindowViewModel`, `NavigationService`, and `ModuleRegistry` all
      unchanged - the one-line swap the Phase 07 module system was
      designed for, same as Bookings in Phase 11.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **No cross-slice link to Bookings.** `Booking.SpecialistName` (free
  text) and `Specialist.FullName` are not connected by anything beyond
  matching seed-data strings - a real link (and the conflict-detection/
  scheduling logic it would enable) is explicitly out of scope
  ("Do not implement Calendar").
- **No compensation/payroll fields** on `Specialist` - explicitly out of
  scope ("Do not implement Payroll"); status and skills only.
- **No search/filter beyond name/title/email** and no skill-level search
  - proportionate to 5 seed specialists; revisit if the roster grows.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 165/165 tests passed (44 new):
      Domain.Tests 14 (+2), Application.Tests 52 (+16:
      SpecialistQueryService including search, SpecialistProfileQueryService,
      SpecialistCommandService), Infrastructure.Tests 34 (+10:
      FakeSpecialistRepository), Presentation.Tests 61 (+16:
      SpecialistPageViewModel, SpecialistProfileViewModel), ArchitectureTests
      4 (unchanged - still passing, confirming Specialists follows the same
      dependency-direction and ViewModel-testability rules as every other
      slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the renamed "Specialists" sidebar entry, confirmed the
      seeded directory/profile rendered correctly (status, skills as
      chips), selected an OnLeave specialist, searched "therapist" and
      confirmed the list filtered to and reselected the one match (Casey
      Morgan) with her own skills, added a skill to Jordan Lee and watched
      it appear live, then created a new specialist via the form
      (defaulted to Active, auto-selected, form cleared).
- [x] No database, no API, no external integrations, no Calendar, no
      Payroll - `FakeSpecialistRepository` remains in-memory only, and
      `Specialist`/`SpecialistDto` carry no scheduling or compensation
      fields.
- [x] Clean Architecture boundaries unchanged - `Domain.Specialists` has
      no outward dependency, `Application.Specialists` depends only on
      `Domain.Specialists`, `Presentation` depends only on
      `Application.Specialists` - verified by the unmodified,
      still-passing `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
