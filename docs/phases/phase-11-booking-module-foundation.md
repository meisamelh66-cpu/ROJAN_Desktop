# Phase 11 — Enterprise Booking Module Foundation

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a third real business module - Bookings - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by Dashboard (Phase 06B) and Customer CRM
(Phase 09/10), and wire it into Shell navigation, replacing the
"appointments" `PlaceholderModule` one-for-one per the swap the Phase 07
module system was designed for. Foundation scope: a real, working list +
detail + create + status-transition slice, not a full calendar/scheduling
experience - no conflict detection, no time-of-day picker, no cross-slice
link to a real Customer record.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Bookings`): `Booking` record
      (`CustomerId` deliberately free-form/unvalidated - linking to a real
      Customer record is a future integration point, not built here, per
      the Independence goal in `docs/architecture/00-overview.md` §2);
      `BookingStatus` (`Pending`/`Confirmed`/`Completed`/`Cancelled`);
      `IBookingRepository` (`GetBookingsAsync`, `GetBookingByIdAsync`,
      `CreateBookingAsync`, `UpdateBookingStatusAsync`).
- [x] **Application** (`Rojan.Desktop.Application/Bookings`):
      `BookingDto`, `IBookingQueryService`/`BookingQueryService`,
      `IBookingCommandService`/`BookingCommandService`
      (create + status-transition commands), `BookingMapper`
      (Domain&lt;-&gt;Application mapping, same pattern as `Customers.CustomerMapper`),
      `CreateBookingRequest`. Registered in `AddApplication()`.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/Bookings`):
      `FakeBookingRepository` - mutable instance state (DI singleton, so
      writes persist for the app's lifetime), 8 seed bookings referencing
      the existing Customer CRM seed names for a cohesive demo, spanning
      all 4 status values across past/future dates. Registered in
      `AddInfrastructure()`.
- [x] **Presentation**: `BookingPageViewModel` (list, new-booking form,
      selected-booking detail, `ConfirmBookingCommand`/
      `CompleteBookingCommand`/`CancelBookingCommand` each gated by
      `CanExecute` on the selected booking's current status);
      `BookingPage.xaml` (same `DashboardCard`/`DashboardWidget` shape as
      every other module, no new Design System components); `BookingModule`
      (mirrors `DashboardModule`/`CustomerModule`).
- [x] **Shell wiring**: `App.xaml.cs` - the `"appointments"`
      `PlaceholderModule` registration replaced with
      `services.AddSingleton<IModule, BookingModule>()`; sidebar entry
      renamed id/title from `appointments`/"Appointments" to
      `bookings`/"Bookings" (matching the domain language already used
      elsewhere - "Total Bookings" KPI, "Booking completed"/"Booking
      created" activity-log entries from the Customer CRM slice), same
      order (20) and glyph (◷). `Views.xaml` DataTemplate mapping added.
      `MainWindow`/`MainWindowViewModel`/`NavigationService`/`ModuleRegistry`
      all unchanged - confirms the Phase 07 module system needed exactly
      the one-line swap it was designed for.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **No time-of-day picker.** New bookings default to a fixed 10:00 AM
  slot; only the date is user-selectable (`DatePicker`, built into WPF,
  no new dependency). A real time picker is a deferred enhancement, not
  an oversight.
- **`CustomerId` is not validated or linked.** Booking's `CustomerName` is
  free text, not a foreign key into the Customer CRM slice - intentional,
  per Vertical Slice independence, but means nothing currently prevents
  a booking referencing a customer name that doesn't exist in Customers.
- **No search/filter on the booking list**, unlike Customers - a
  deliberate foundation-scope simplification (8 seed bookings don't need
  it yet); revisit once the list grows.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 121/121 tests passed (39 new):
      Domain.Tests 12 (+2), Application.Tests 36 (+9: BookingQueryService,
      BookingCommandService), Infrastructure.Tests 24 (+7:
      FakeBookingRepository), Presentation.Tests 45 (+21:
      BookingPageViewModel, including `Theory`-driven CanExecute coverage
      for all three status-transition commands), ArchitectureTests 4
      (unchanged - still passing, confirming Bookings follows the same
      dependency-direction and ViewModel-testability rules as every other
      slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the renamed "Bookings" sidebar entry, confirmed the
      seeded list/detail rendered correctly (including a pre-Confirmed
      booking showing Confirm disabled), selected a Pending booking and
      confirmed Confirm was enabled/Complete disabled, clicked Confirm and
      verified the status and button states updated live, then created a
      new booking via the form (defaulted to Pending, auto-selected, form
      cleared).
- [x] No database, no API, no external integrations - `FakeBookingRepository`
      remains in-memory only.
- [x] Clean Architecture boundaries unchanged - `Domain.Bookings` has no
      outward dependency, `Application.Bookings` depends only on
      `Domain.Bookings`, `Presentation` depends only on
      `Application.Bookings` - verified by the unmodified, still-passing
      `ArchitectureTests`.

## Approval

Approved by: <pending> — <date>
