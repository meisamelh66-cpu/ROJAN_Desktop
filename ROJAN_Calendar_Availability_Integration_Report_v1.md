# ROJAN Calendar / Availability Integration — Implementation Report v1

**Priority:** P0
**Scope:** Implementation, per "ROJAN Calendar / Availability Integration — Phase 3 Implementation" approval. Builds directly on `ROJAN_Calendar_Availability_Integration_Plan_v1.md` and `ROJAN_Calendar_Availability_Phase3_Review.md`.
**Repositories:** `ROJAN_Backend` @ `6943986` (unchanged - no backend code touched), `ROJAN_Desktop` (Owner App).

---

## Executive Summary

Calendar/Availability is now backend-connected. `ICalendarQueryService` is served by a new `BackendCalendarAvailabilityRepository` that calls ROJAN_Backend's `available-slots` engine directly, replacing local fixed-30-minute generation. The Reception Booking Wizard's full flow - Customer → Service → Specialist → Availability → Time Slot → Booking - now runs end-to-end against real backend data, including booking creation, which previously threw `NotSupportedException`. The manual "Toggle Slot → Booked" feature is removed; `CalendarPage` is now a read-only, service-driven availability display, with real `Booking` data as the only source of truth for a slot's Booked state.

No backend code, database schema, or API contract changed - every endpoint this phase needed already existed and was verified in the prior review pass. `dotnet build` succeeds with 0 warnings/errors; the full test suite passes at **2,199/2,199** across all six test projects.

---

## 1. What Changed

### 1.1 Calendar availability contract (`serviceId` support)

- `ICalendarQueryService.GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync` gained a required `serviceId` parameter, positioned per the approved context order: `specialistId, serviceId, date`.
- `Application.Calendar.CalendarQueryService` (the local/EF implementation) accepts and ignores it - it's no longer the registered implementation (see §1.3), kept alive only so its own unit tests keep exercising real local-generation logic.

### 1.2 `BackendCalendarAvailabilityRepository` (new)

`src/Rojan.Desktop.Infrastructure/Calendar/BackendCalendarAvailabilityRepository.cs` - follows the established `Backend*Repository` pattern (`IApiClient` + `ISalonContextService`, same shape as `BackendBookingRepository`/`BackendServiceRepository`/`BackendSpecialistRepository`), with one deliberate deviation flagged in the prior review and confirmed correct here: it implements `ICalendarQueryService` (Application) directly rather than `ICalendarRepository` (Domain), because ROJAN_Backend's `available-slots` endpoint already performs the entire slot-generation computation server-side - reusing the raw-schedule-rows-in/generate-in-Application shape would mean re-fetching five schedule endpoints per day and reimplementing the backend's own `TimeSlotEngine` client-side.

- `GetScheduledSpecialistsAsync` → `GET /api/v1/salons/{salonId}/specialists`, filtered to Active, ordered by name.
- `GetDailyAvailabilityAsync` → `GET /api/v1/salons/{salonId}/specialists/{specialistId}/available-slots?serviceId={serviceId}&date={date}`, mapped to `AvailabilitySlotDto` entries, all `Available` (see honesty notes below).
- `GetWeeklyAvailabilityAsync` → 7 sequential daily calls, matching the plan's already-flagged week-view cost (§5 of the integration plan).

**Honesty notes (documented in the class's own doc comment, same convention every other `Backend*Repository` uses):**
- Every returned slot is `AvailabilityStatus.Available` - `available-slots` returns only free windows, with no signal for *why* a moment is absent. This is the direct, intended consequence of the product decision: Booked state now comes from real `Booking` data, not a value this read path fabricates.
- `DailyAvailabilityDto.WorkingStart`/`WorkingEnd` are derived from the first/last returned slot, not a real "does this specialist work today" signal - a fully-booked working day and a genuine non-working/leave day both show as empty/"not scheduled." Getting this fully accurate would require querying the schedule-authoring endpoints separately, which this class deliberately avoids (per the single-call design above).
- `GetScheduledSpecialistsAsync` now means "every active specialist," not "has a working-hours entry" - there's no cheap way to ask the narrower question without querying every specialist's schedule individually.

### 1.3 DI wiring

- **Removed** from `Application.DependencyInjection.ServiceCollectionExtensions.AddApplication()`: `services.AddSingleton<ICalendarQueryService, CalendarQueryService>()`.
- **Added** to `Infrastructure.DependencyInjection.ServiceCollectionExtensions.AddInfrastructure()`: `services.AddSingleton<ICalendarQueryService, BackendCalendarAvailabilityRepository>()`.
- `ICalendarRepository` → `EfCalendarRepository` registration is **untouched** - `ICalendarCommandService`'s implementation still depends on it (see §1.6).

### 1.4 Reception Booking Wizard — full flow connected

`BookingWizardViewModel` already implemented the entire Customer → Service → Specialist → Date → TimeSlot → Review → Confirmation flow structurally; only two things stood between it and running against real data:

1. `LoadAvailableSlotsAsync` now passes `SelectedService.Id` through `IBookingWorkflowService.GetAvailableSlotsAsync(specialistId, serviceId, date)` (interface, service, and permission-gate signatures all threaded consistently).
2. `BackendBookingRepository.CreateBookingAsync` - previously `throw new NotSupportedException(...)` because the self-service `POST /api/v1/bookings` has no owner-initiated path - now calls the real owner-authorized `POST /api/v1/salons/{salonId}/bookings` endpoint (`SalonBookingController.createForCustomer`, confirmed to exist during the prior review's backend verification pass). `Booking.CustomerId` is sent as-is as `customerId`; this is already a Customer CRM id (not a User id) because `BackendCustomerRepository` populates it that way, exactly matching what the backend endpoint expects. The backend's own 409 (customer has no linked account yet) surfaces as an `ApiException`, unhandled specially - a real, known business rule, not a bug.

New wire contract added: `CreateBookingForCustomerRequest(CustomerId, ServiceId, SpecialistId, StartTime, Notes)` in `Api.Contracts`, matching ROJAN_Backend's DTO field-for-field.

### 1.5 Manual toggle removed; `CalendarPage` is now service-driven and read-only

Per the approved product decision:

- `CalendarPageViewModel.ToggleSlotCommand`/`ToggleSlotAsync` and its `ICalendarCommandService` dependency are gone.
- The page gained a **Service** picker (`Services`/`SelectedService`, sourced from `IServiceQueryService`, Active-only) alongside the existing Specialist/Date pickers - required because the backend's availability engine needs a service to compute slot length. The load sequence is now three-stage: specialists → services → (once both are selected) availability.
- `CalendarPage.xaml`'s per-slot tile changed from an interactive `Button` bound to the removed command into a plain, non-interactive `Border` - visually unchanged (same StatusPill/severity styling), just no longer clickable.

### 1.6 What was deliberately left unchanged

`BookingWorkflowService.CreateBookingAsync`/`CancelBookingAsync`/`RescheduleBookingAsync` still reserve/release a slot through `ICalendarCommandService` (→ `EfCalendarRepository`, local) as their own internal write-side safety net and rollback mechanism. This was a real design question during implementation: since `ICalendarQueryService` no longer reads local reserved state (availability is backend-sourced now), this local bookkeeping is decoupled from anything the calendar *displays*. It was kept **unchanged** rather than removed, because:

- It's a different concern from the manual toggle the product decision targeted (an internal, automatic rollback mechanism vs. a user-facing, detached "reserve with no booking" feature).
- Removing it would mean rewriting the ten tests in `BookingWorkflowServiceTests.cs` that specifically assert its reserve/release/rollback call sequencing across Create/Cancel/Reschedule and a full lifecycle scenario - a materially larger change than this order's explicit scope, and outside "Keep: Existing Booking API."
- It's harmless: the backend's own `BookingRepository.reserve()` is the real, atomic conflict guarantee now (already tested via `BookingConflictConcurrencyIntegrationTest` on the backend side); this local reservation only ever fails against its own prior local writes, never a false positive against real data.

**Flagging this explicitly rather than silently leaving it**: it is now dead-weight bookkeeping (write-only, nothing reads it back), not incorrect. If a future phase wants to simplify it, that's a separate, explicitly-scoped decision.

---

## 2. Files Changed

**New:**
- `src/Rojan.Desktop.Infrastructure/Calendar/BackendCalendarAvailabilityRepository.cs`
- `src/Rojan.Desktop.Application/Api/Contracts/TimeSlotResponse.cs`
- `tests/Rojan.Desktop.Infrastructure.Tests/Calendar/BackendCalendarAvailabilityRepositoryTests.cs`

**Modified (production code):**
- `src/Rojan.Desktop.Application/Calendar/ICalendarQueryService.cs`, `CalendarQueryService.cs`
- `src/Rojan.Desktop.Application/Api/Contracts/BookingResponse.cs` (added `CreateBookingForCustomerRequest`)
- `src/Rojan.Desktop.Application/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `src/Rojan.Desktop.Infrastructure/Bookings/BackendBookingRepository.cs`
- `src/Rojan.Desktop.Application/BookingWorkflow/IBookingWorkflowService.cs`, `BookingWorkflowService.cs`, `BookingWorkflowServicePermissionGate.cs`
- `src/Rojan.Desktop.Presentation/ViewModels/BookingWorkflow/BookingWizardViewModel.cs`
- `src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs`
- `src/Rojan.Desktop.Presentation/Views/Calendar/CalendarPage.xaml`

**Modified (tests):**
- `tests/Rojan.Desktop.Application.Tests/Calendar/CalendarQueryServiceTests.cs`
- `tests/Rojan.Desktop.Application.Tests/BookingWorkflow/StubCalendarQueryService.cs`, `BookingWorkflowServiceTests.cs`
- `tests/Rojan.Desktop.Presentation.Tests/Calendar/StubCalendarQueryService.cs`, `CalendarPageViewModelTests.cs`
- `tests/Rojan.Desktop.Presentation.Tests/BookingWorkflow/StubBookingWorkflowService.cs`, `BookingWizardViewModelTests.cs`
- `tests/Rojan.Desktop.Infrastructure.Tests/Bookings/BackendBookingRepositoryTests.cs`

**Untouched by design:** `ICalendarCommandService`, `CalendarCommandService`, `CalendarCommandServicePermissionGate`, `ICalendarRepository`, `EfCalendarRepository`, `FakeCalendarRepository` (see §1.6).

---

## 3. Confirmation of Kept Integrations

Verified with real tests, not just DI inspection - `Backend*RepositoryTests` for Customer, Service, Specialist, Booking, and Dashboard all still pass unchanged (they were not modified except `BackendBookingRepositoryTests.cs`'s `CreateBookingAsync` tests, which were rewritten for the new real behavior, not broken by it).

| Integration | Status |
|---|---|
| Customer CRM | Unchanged, still `BackendCustomerRepository` |
| Service Integration | Unchanged, still `BackendServiceRepository` |
| Specialist Integration | Unchanged, still `BackendSpecialistRepository` |
| Booking API | Extended (not replaced) - `CreateBookingAsync` now implemented; all previously-passing read/status-update/reschedule behavior unchanged |

---

## 4. Test Evidence

```
dotnet build RojanDesktop.sln
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet test RojanDesktop.sln
Rojan.Desktop.Domain.Tests............. 454/454 passed
Rojan.Desktop.ArchitectureTests........   6/6   passed
Rojan.Desktop.Presentation.Tests....... 456/456 passed
Rojan.Desktop.Shell.Tests..............  45/45  passed
Rojan.Desktop.Application.Tests........ 705/705 passed
Rojan.Desktop.Infrastructure.Tests..... 533/533 passed
-----------------------------------------------------
Total: 2,199/2,199 passed, 0 failed
```

The architecture test suite passing confirms the one deliberate layering deviation (`BackendCalendarAvailabilityRepository` implementing an Application-layer interface directly) doesn't violate the codebase's dependency-direction rules.

**New/rewritten test coverage added this phase:**
- **Repository tests:** `BackendCalendarAvailabilityRepositoryTests` (11 tests) - active-specialist filtering, Available-only mapping, WorkingStart/End derivation, exact `serviceId`/`date` query string, specialist-name-lookup fallback, error propagation, and the week view's 7 per-day calls. `BackendBookingRepositoryTests` - 4 new/rewritten tests replacing the old "always throws" test: request-field mapping, empty-notes-to-null, no-salon, and backend-rejection (409) propagation.
- **Mapping tests:** covered within the repository tests above (`TimeSlotResponse` → `AvailabilitySlotDto`, `Booking` → `CreateBookingForCustomerRequest`).
- **ViewModel tests:** `CalendarPageViewModelTests` rewritten - removed the three `ToggleSlotCommand_*` tests, added `Constructor_NoActiveServices_StateIsEmpty`, `SelectedService_Changed_ReloadsAvailability`, and `GetDailyAvailabilityAsync_CalledWithSelectedSpecialistAndServiceIds` (verifies the specialist/service ids actually flow through). `BookingWizardViewModelTests` unaffected in behavior, updated only for the widened stub signature.
- **Integration-shaped coverage:** `BookingWorkflowServiceTests.GetAvailableSlotsAsync_FiltersToAvailableOnly` now asserts through the `serviceId`-aware path; the existing Create/Cancel/Reschedule lifecycle tests continue to pass unchanged, confirming the kept local safety net (§1.6) still behaves correctly alongside the new real booking-creation call.

---

## 5. Migration / API Contract Notes

- **No database migration** - neither ROJAN_Backend's schema nor the Owner App's local SQLite schema changed. `EfCalendarRepository`/`WorkingSchedule` persistence is untouched.
- **No backend API contract changes** - every endpoint consumed (`available-slots`, `GET .../specialists`, `POST /api/v1/salons/{salonId}/bookings`) already existed and was verified against the live backend source in the prior review pass (`ROJAN_Calendar_Availability_Phase3_Review.md` §1.5, `ROJAN_Team1_Integration_Verification_Report_v1.md`).
- **New Owner App-side wire contracts only** (additive, not breaking): `TimeSlotResponse`, `CreateBookingForCustomerRequest` - both mirror existing ROJAN_Backend DTOs field-for-field.
