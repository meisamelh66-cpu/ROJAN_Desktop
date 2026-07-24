# 0002. Sprint 6 Persistence Foundation — Seed Strategy

**Status:** Accepted (documentation only — no seeding implemented)
**Date:** 2026-07-24

## Context

Sprint 6 Commits 2 through 6 replaced five Domain modules' repositories
(`ICustomerRepository`, `ISpecialistRepository`, `IServiceRepository`,
`IBookingRepository`, `ICalendarRepository`) with real EF Core/SQLite
implementations, each registered in `Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure`
in place of the corresponding `Fake*Repository` (which remain in the
codebase, unreferenced, per every commit's explicit "do not remove yet"
instruction).

Every `Fake*Repository` seeded realistic Persian-language demo data in
its constructor. None of the five `Ef*Repository` implementations do -
a fresh SQLite database is genuinely empty on first run. Whether that
empty state is *recoverable* through the running app depends entirely on
whether the module's own repository contract exposes a create/write
operation - and that turned out to differ by module in a way that was
only discovered while implementing each one (Commits 4 and 6 each
required a user decision mid-commit for exactly this reason).

## Current behavior (as of Sprint 6 Commit 6)

| Module | Create/write path exists? | Fresh-database behavior |
|---|---|---|
| Customers | Yes - `CustomerCommandService.CreateCustomerAsync` | Starts empty, grows normally through the Customer page's "Add Customer" flow. |
| Specialists | Yes - `SpecialistCommandService.CreateSpecialistAsync` | Starts empty, grows normally through the Specialist page's "Add Specialist" flow. |
| Bookings | Yes - `BookingCommandService`/`BookingWorkflowService.CreateBookingAsync` | Starts empty, grows normally through the booking wizard - but a booking's `CustomerId`/`SpecialistId`/`ServiceId` are free-form, unvalidated text (see `Domain.Bookings.Booking`'s own doc comment), so a *meaningful* booking still depends on the Customers/Specialists/Services catalogs actually having data. |
| Services | **No** - `IServiceRepository`/`IServiceCommandService` only ever supported browse-catalog-plus-specialist-assignment, never catalog authoring (pre-existing scope limit, not introduced by Sprint 6) | Catalog stays permanently empty - no UI path exists to add a service. Confirmed with the user before switching `AddInfrastructure`'s registration (Commit 4). |
| Calendar (`WorkingSchedule`) | **No** - `ICalendarRepository`/`ICalendarCommandService` only ever supported reading schedules plus reserving/releasing a slot, never schedule authoring (pre-existing scope limit) | Schedules stay permanently empty. Worse than the Services gap: `Application.Calendar.CalendarQueryService.GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync` **throw** `InvalidOperationException` for any specialist with zero schedules, rather than returning empty - so the Calendar page and the booking wizard's time-picker step are non-functional, not merely empty. Confirmed with the user before switching `AddInfrastructure`'s registration (Commit 6). |
| Calendar (`ReservedSlot`) | Yes - `CalendarCommandService.ReserveSlotAsync`/`ReleaseSlotAsync` | Technically self-healing, but practically unreachable while `WorkingSchedule` stays empty (see above) - the UI flow that leads to reserving a slot goes through the throwing availability read first. |

No seed data was written for any of the five modules in Sprint 6. This
was a deliberate scope boundary, not an oversight - every commit's task
explicitly limited it to replacing the repository behind the existing
`I*Repository` contract, never to inventing new write operations that
contract doesn't have.

## Decision

- **No seeding is implemented as part of Sprint 6.** This document
  records the gap; it does not close it.
- Going forward, this is understood as two separate, already-distinguishable
  categories of future work, not one:
  1. **Demo/onboarding seed data** for Customers/Specialists/Bookings -
     straightforward, since real create commands already exist; a future
     commit can simply call them once (e.g. on first run, guarded the
     same way `Shell.App.OnStartup`'s `SeedDemoNotificationsIfEmpty`
     already guards notification seeding) or via an EF Core migration's
     `HasData`/seed step.
  2. **Catalog-authoring commands** for Services (`CreateServiceAsync`/
     `UpdateServiceAsync` on `IServiceRepository`/`IServiceCommandService`)
     and **schedule-authoring commands** for Calendar (a
     `CreateWorkingScheduleAsync`-shaped addition to
     `ICalendarRepository`/`ICalendarCommandService`) - these are real
     Domain contract *additions*, not seeding, and were explicitly out of
     scope for every Sprint 6 commit ("do not change repository interface
     shape"). Only once these exist does seeding those two modules become
     possible at all.

## Consequences

- On a fresh install today, the Customer/Specialist/Booking pages will
  start empty and are fully usable from an empty state. The Service
  catalog will start empty and stay empty. The Calendar page and the
  booking wizard's time-picker step will throw for every specialist,
  because no specialist has a working schedule.
- Sprint 7 (or whichever sprint takes up product-facing work next) should
  treat "add catalog-authoring commands to Services" and "add
  schedule-authoring commands to Calendar" as prerequisites before the
  app is genuinely usable end-to-end on a fresh database - not
  optional polish.
- Demo/onboarding seeding (category 1 above) is a separate, smaller,
  lower-risk follow-up that does not require any Domain contract change
  and can be scheduled independently.
