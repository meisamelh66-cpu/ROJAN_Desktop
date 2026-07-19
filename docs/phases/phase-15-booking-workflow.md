# Phase 15 — Enterprise Booking Workflow & Reservation Flow

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Build the first genuinely cross-slice Application-layer use case in this
codebase - a guided Booking Wizard that coordinates Customers, Services,
Specialists, Calendar, and Bookings into one reservation flow - without
weakening the Domain-layer vertical-slice independence every prior phase
has maintained. The existing Bookings page's free-text quick-add form
stays untouched as a separate, simpler path; the wizard is additive.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/Bookings`): `Booking` expanded
      with `ServiceId`/`SpecialistId`/`Price` (previously only free-text
      `ServiceName`/`SpecialistName`, no price at all); `BookingStatus`
      expanded from 4 to 6 values (`Pending`/`Confirmed`/`InProgress`/
      `Completed`/`Cancelled`/`NoShow`); new `BookingRules` static class -
      `IsValidTransition` (a lifecycle state machine) and
      `IsValidDuration` (1-480 minutes) - a deliberate, explicitly
      requested deviation from this codebase's usual "Domain is just
      data + repository contract" minimalism. `IBookingRepository` is
      unchanged - `GetBookingByIdAsync` already existed.
- [x] **Application**: new `Rojan.Desktop.Application/BookingWorkflow`
      slice - `IBookingWorkflowService`/`BookingWorkflowService`, the
      only Application service in this app that depends on five sibling
      Application interfaces at once (`ICustomerQueryService`,
      `IServiceQueryService`, `ISpecialistQueryService`,
      `ICalendarQueryService`, `ICalendarCommandService`,
      `IBookingQueryService`, `IBookingCommandService`) - normal Clean
      Architecture use-case orchestration, not a Domain dependency
      change. `GetBookingOptionsAsync` (Customers/Active-only-Services/
      Active-only-Specialists, the "booking options query"),
      `GetAvailableSlotsAsync` (delegates to Calendar's slot generation
      and conflict detection, filtered to Available-only, the "available
      slot query"), `CreateBookingAsync` (reserves the calendar slot,
      then creates the booking; if booking creation fails, releases the
      slot it just reserved - no database transaction spans both writes,
      so the rollback is explicit), `CancelBookingAsync` (updates status
      to Cancelled, releases the calendar slot if the booking has a real
      specialist id). `Bookings.BookingCommandService` now enforces
      `BookingRules` on every write (invalid duration/illegal transition
      throws) and `Bookings.IBookingQueryService` gained
      `GetBookingByIdAsync` (needed for cancel's calendar-release path).
      `CreateBookingRequest` gained four trailing optional fields
      (`CustomerId`/`ServiceId`/`SpecialistId`/`Price`, defaulted) so the
      existing quick-add form's call site keeps compiling unchanged.
      Registered in `AddApplication()`.
- [x] **Infrastructure**: `FakeBookingRepository`'s 8 seed bookings now
      carry real `ServiceId`/`SpecialistId` cross-references to the
      actual Service/Specialist catalogs (Phases 12/13) where the
      existing free-text name cleanly matches a real entry - one seed
      row (`booking-3`, "Corporate Group Styling") predates the Service
      catalog and isn't one of the nine seeded services, so it
      deliberately keeps an empty `ServiceId` rather than fabricating a
      false link. No repository interface changes.
- [x] **Presentation**: new `IDialogService` (Presentation) /
      `MainWindowViewModel` implements it (Shell) - the first producer
      of `MainWindowViewModel.ActiveDialog`, an extension point that
      property's own doc comment has named since Phase 07
      ("a future IDialogService sets this"); registered the same
      alias-registration way `INavigationService` already is. New
      `BookingWizardViewModel`/`BookingWizardView` - a 7-step linear
      wizard (Customer → Service → Specialist → Date → TimeSlot →
      Review → Confirmation) shown in Shell's dialog region, not a page.
      Customer selection is not one of the six steps the phase brief
      enumerates, but "coordinate Customer" only means something if the
      wizard picks a real customer rather than free text, so it's the
      natural first step. `BookingPageViewModel` gained
      `OpenWizardCommand` (constructs the wizard with its own
      already-injected `IBookingWorkflowService`/`IDialogService` and a
      reload callback, same "parent constructs child ViewModel" pattern
      as `CustomerPageViewModel`/`CustomerProfileViewModel`) and a
      "New Booking (Wizard)" button, alongside the untouched quick-add
      form. `Views.xaml` DataTemplate mapping added.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **Two booking-creation paths now coexist.** The quick-add form
  (free-text, no calendar reservation, foundation scope since Phase 11)
  and the Wizard (real ids, reserves a calendar slot) both write through
  `IBookingCommandService.CreateBookingAsync` via different
  `CreateBookingRequest` shapes. This is intentional - retrofitting the
  quick-add form to require real ids was out of scope; not replacing it
  keeps the phase's blast radius to what was actually requested.
- **No InProgress/NoShow buttons on the existing Bookings page.** The
  simple page's Confirm/Complete/Cancel buttons still only exercise the
  transitions they already did (all still legal under the new
  `BookingRules` table); reaching `InProgress`/`NoShow` today requires
  going through `BookingWorkflowService` or a future UI addition - not
  built here, out of this phase's explicit scope.
- **No database transaction across the calendar-reserve + booking-create
  write pair.** `BookingWorkflowService.CreateBookingAsync` handles this
  with an explicit try/catch rollback (release the slot on booking
  failure) rather than a real transaction, consistent with this app
  having no database anywhere.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors (including
      fixing one `CA1716` reserved-keyword-parameter-name violation on
      `IBookingWorkflowService.GetAvailableSlotsAsync`'s `date`
      parameter, same known analyzer rule as Phase 14).
- [x] `dotnet test RojanDesktop.sln` - 278/278 tests passed (46 new):
      Domain.Tests 39 (+7: `BookingRules` transition/duration coverage),
      Application.Tests 95 (+16: `BookingWorkflowService` options
      filtering, slot filtering, create-with-rollback, cancel-with-
      release, plus `BookingCommandService` validation enforcement),
      Infrastructure.Tests 47 (unchanged count, existing tests updated
      for the expanded `Booking` constructor), Presentation.Tests 93
      (+16: `BookingWizardViewModel` step navigation/CanExecute/confirm
      flow, `BookingPageViewModel`'s new `OpenWizardCommand`),
      ArchitectureTests 4 (unchanged - still passing, confirming
      `BookingWorkflow` depends only on other Application interfaces and
      `Domain.Bookings`, never on another slice's Domain types).
- [x] Runtime verified via UI Automation against the real running app:
      opened the Bookings page, clicked "New Booking (Wizard)", stepped
      through Customer → Service → Specialist → Date → TimeSlot,
      confirmed the Review step correctly summarized the selections
      ("Service: Haircut & Style ($65)", "Specialist: Jordan Lee"),
      clicked Confirm Booking, confirmed the Confirmation step showed
      "Booking confirmed" with matching details, clicked Done and
      confirmed the dialog closed back to the Bookings page.
- [x] No database, no API, no payments, no notifications - every write
      stays in the existing in-memory fake repositories.
- [x] Clean Architecture boundaries unchanged -
      `BookingWorkflowService` depends only on Application interfaces
      plus `Domain.Bookings`; no Domain slice references another
      Domain slice - verified by the unmodified, still-passing
      `ArchitectureTests`.
- [x] Shell architecture unchanged - `MainWindow.xaml`'s dialog region,
      `MainWindowViewModel`'s `ActiveDialog` property, `NavigationService`,
      and `ModuleRegistry` are all structurally the same as Phase 07;
      `MainWindowViewModel` gained an `IDialogService` implementation
      (two one-line methods) using the property that was already there.

## Approval

Approved by: <pending> — <date>
