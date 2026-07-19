# Phase 14 — Enterprise Calendar & Availability Engine Foundation

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a sixth real business module - Calendar - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by every prior module, and wire it into Shell
navigation as a genuinely new sidebar entry (no existing placeholder was
named "Calendar"). Foundation scope: generate a specialist's daily
availability from a recurring weekly schedule, detect conflicts against
existing booked ranges, and let a user reserve/release a single slot -
deliberately no full booking wizard (no customer/service/notes capture),
no external calendar sync, no database, no API.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Calendar`): `WorkingSchedule`
      (a specialist's recurring hours for one `DayOfWeek`, with
      `SpecialistId`/`SpecialistName` deliberately free-form/unvalidated -
      this vertical slice does not depend on `Domain.Specialists`, per the
      Independence goal in `docs/architecture/00-overview.md` §2); `TimeSlot`
      (a pure Start/End value, used both for existing booked ranges and
      generated availability); `AvailabilitySlot` (a `TimeSlot` plus who it
      belongs to and its status - constructed by Application's generation
      logic, not repository-returned, but modeled in Domain since it's a
      real business concept regardless of where it's produced);
      `AvailabilityStatus` (`Available`/`Booked`/`Unavailable` - the last
      deliberately unused by this phase's generator, reserved for a future
      refinement like blocked/break time); `ICalendarRepository`
      (deliberately returns only raw schedule/booked-slot data - generation
      and conflict detection are Application's job, the "return the
      read-set, compose in Application" convention already established by
      Customer/Specialist/Service search).
- [x] **Application** (`Rojan.Desktop.Application/Calendar`):
      `ICalendarQueryService`/`CalendarQueryService` - owns both explicitly
      requested business rules: generating fixed 30-minute slots across a
      specialist's working hours, and marking each Available/Booked by
      checking it against existing booked ranges (conflict detection);
      `ICalendarCommandService`/`CalendarCommandService` - reserve/release
      a single slot, with `ReserveSlotAsync` re-checking for a conflict
      immediately before writing (a write-time guarantee, not just a
      display-time convenience, since a shown-Available snapshot can go
      stale); `CalendarMapper`; `ScheduledSpecialistDto`/
      `AvailabilitySlotDto`/`DailyAvailabilityDto`. Registered in
      `AddApplication()`.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Calendar`):
      `FakeCalendarRepository` - mutable instance state for booked slots
      (DI singleton), working schedules for the three Active specialists
      already seeded in `Specialists.FakeSpecialistRepository` (Jordan
      Lee, Priya Nair, Casey Morgan - each with a different weekly
      pattern), and existing booked-slot "conflicts" computed relative to
      "today" (next occurrence of a given weekday) so the demo always
      shows a real conflict on an upcoming working day regardless of when
      the app is run. Registered in `AddInfrastructure()`.
- [x] **Presentation**: `CalendarPageViewModel` - unlike every other
      module, not a list-plus-detail split; a day's availability grid *is*
      the page. Specialist/date picker, resolved working-hours caption,
      and a single `ToggleSlotCommand` that reserves an Available slot or
      releases a Booked one, reloading afterward. `CalendarPage.xaml`
      reuses the existing `GlassButton` style (via `BasedOn`, overriding
      only `Background` per slot status through a `DataTrigger`) for the
      slot grid - no new Design System component. `CalendarModule`.
- [x] **Shell wiring**: `App.xaml.cs` - `services.AddSingleton<IModule, CalendarModule>()`
      added after `BookingModule` (order 25); this is not a placeholder
      swap (no existing placeholder was named "Calendar") but the same
      composition-root mechanism every other module registration uses -
      confirmed not a Shell architecture change since `MainWindow`,
      `MainWindowViewModel`, `NavigationService`, and `ModuleRegistry` are
      all unmodified. `Views.xaml` DataTemplate mapping added.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **No full booking wizard.** Reserving a slot captures only
  specialist/start/end - no customer, service, or notes. This is the
  availability *engine*, not a booking creation flow; that distinction
  was explicit in this phase's constraints.
- **No cross-slice link to Bookings or Specialists.** Seed schedule/
  conflict data uses matching specialist names for narrative consistency
  only, same as every prior cross-slice reference in this app (`Booking.SpecialistName`,
  `SpecialistService.SpecialistName`) - reserving a Calendar slot does not
  create or affect a real `Booking` record, and vice versa. A real
  integration is a future decision, not built here.
- **`AvailabilityStatus.Unavailable` is defined but unused** by this
  phase's slot generator (every generated slot falls within working
  hours, so it's always Available or Booked) - documented in the enum's
  own doc comment as intentional, not an oversight.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors (including
      fixing three `CA1716` reserved-keyword-parameter-name violations -
      `date`/`end` conflict with VB.NET keywords - caught by the existing
      analyzer configuration, not a new rule).
- [x] `dotnet test RojanDesktop.sln` - 232/232 tests passed (25 new):
      Domain.Tests 19 (+3), Application.Tests 80 (+8: slot generation
      across working hours, conflict marking, reserve-time conflict
      re-check), Infrastructure.Tests 47 (+6: seeded schedule/conflict
      data, reserve/release round-trip), Presentation.Tests 82 (+8:
      two-stage load, empty-day handling, toggle command routing),
      ArchitectureTests 4 (unchanged - still passing, confirming Calendar
      follows the same dependency-direction and ViewModel-testability
      rules as every other slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the new "Calendar" sidebar entry, confirmed the
      default specialist/date/working-hours/16-slot grid rendered
      correctly (all Available, correctly spaced 30 minutes apart),
      switched to a specialist who doesn't work that weekday and
      confirmed the "Not scheduled to work this day."/empty-state
      handling, then clicked a slot twice and watched it toggle live
      Available → Booked → Available.
- [x] No database, no API, no external calendar sync, no full booking
      wizard - `FakeCalendarRepository` remains in-memory only, and
      `ICalendarCommandService` captures only specialist/start/end.
- [x] Clean Architecture boundaries unchanged - `Domain.Calendar` has no
      outward dependency, `Application.Calendar` depends only on
      `Domain.Calendar`, `Presentation` depends only on
      `Application.Calendar` - verified by the unmodified, still-passing
      `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
