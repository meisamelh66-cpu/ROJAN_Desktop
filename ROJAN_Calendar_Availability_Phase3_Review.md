# ROJAN Calendar / Availability Integration — Phase 3 Review

**Priority:** P0
**Scope:** Review and plan validation only. No code, no refactoring, no architecture changes in this pass.
**Input reviewed:** `ROJAN_Calendar_Availability_Integration_Plan_v1.md` (Audit + Plan v1), validated against the repository at:
- `ROJAN_Backend` @ `6943986`
- `ROJAN_Desktop` (Owner App) @ `8526235`

---

## Executive Summary

The Plan v1 audit remains **accurate** — every file, interface, and behavior it describes was re-read directly and matches the current repository state exactly; nothing has drifted since it was written. All four previously completed integrations (Customer CRM, Service, Specialist, Booking API) were re-verified and remain intact: DI still wires the real `Backend*Repository` implementations, the full solution builds clean (0 errors, 0 warnings), and 65/65 targeted backend-repository tests pass.

The product decision in this order — **the manual "Toggle Slot → Booked" feature is removed; the real `Booking` record is the only source of truth for a slot's Booked state** — resolves Plan v1's Risk 1 ("standalone `CalendarPage`'s toggle has no backend equivalent") in favor of its third option: retire the toggle, make the page a read-only availability display. This is now confirmed as the direction. One sub-question the plan flagged but this order does not resolve is called out explicitly in §4 below, since it materially affects how big Phase 3 actually is.

Phase 3 implementation checklist is in §5. No code was written or changed for this review.

---

## 1. Plan Validation Against Current Repository State

### 1.1 Current Calendar implementation

Confirmed unchanged from the plan's description:

- `CalendarQueryService` (`src/Rojan.Desktop.Application/Calendar/CalendarQueryService.cs`) generates **fixed 30-minute slots** (`SlotDuration = TimeSpan.FromMinutes(30)`, line 17) from raw `WorkingSchedule` rows, independent of any service — confirmed by direct read, matches plan §1 exactly.
- `CalendarCommandService` (`.../CalendarCommandService.cs`) implements `ReserveSlotAsync`/`ReleaseSlotAsync` with a hand-rolled conflict re-check against `ICalendarRepository.GetBookedSlotsAsync` — matches plan §1/§4.
- `CalendarPageViewModel` (`src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs`) still exposes `ToggleSlotCommand` → `ToggleSlotAsync` (lines 255–276), calling `ReserveSlotAsync`/`ReleaseSlotAsync` directly with **no customer or service attached** — confirmed still present, matches plan exactly. `CalendarPage.xaml` (line 136) still binds a per-slot `Button` to this command.
- `ICalendarQueryService.GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync` still take only `(specialistId, date)` — **no `serviceId` parameter** — confirmed, the interface gap the plan calls the "one real blocker" is still present and unresolved.

### 1.2 Current Availability services

- `ICalendarQueryService` / `ICalendarCommandService` (Application layer) confirmed as the only Presentation-facing surface for calendar data, per their own doc comments.
- `BookingWorkflowService.GetAvailableSlotsAsync` (Wizard path) confirmed to call `ICalendarQueryService.GetDailyAvailabilityAsync` and filter to `Available` — the Wizard already resolves the selected service's `DurationMinutes` one step earlier (`WorkflowServiceOptionDto`) but has nowhere to pass it through, exactly as the plan describes.
- **New finding, not in Plan v1 (informational, does not change the plan's conclusions):** `BookingWorkflowService.CreateBookingAsync` calls `_calendarCommandService.ReserveSlotAsync` before `_bookingCommandService.CreateBookingAsync`, with a catch-block `ReleaseSlotAsync` rollback on failure. Since `BackendBookingRepository.CreateBookingAsync` currently `throw`s `NotSupportedException` (self-service `POST /api/v1/bookings` always attributes the booking to the calling user — there is no owner-initiated create path through it), this whole reserve→create→rollback path is presently dead against real backend data — an already-documented, pre-existing limitation (`ROJAN_Booking_Integration_Implementation_Report_v1.md` §5), not something Phase 3 introduces. Worth noting because it is directly relevant to Plan v1 §8 Phase 5: the backend *does* already have an owner-initiated booking endpoint the plan itself names (`POST /api/v1/salons/{salonId}/bookings`, `SalonBookingController.kt`, verified in the prior Team 1 verification pass) — `BackendBookingRepository`'s doc comment predates that endpoint's confirmation and should be revisited when Phase 5 is scoped, but that revisit is not part of Phase 3.

### 1.3 Existing interfaces

Confirmed via direct read:
- `ICalendarQueryService` — 3 methods, no `serviceId` param. Matches plan.
- `ICalendarCommandService` — `ReserveSlotAsync`/`ReleaseSlotAsync` only, explicitly scoped "not a full booking wizard" per its own doc comment. Matches plan.
- `ICalendarRepository` (Domain) — `GetWorkingSchedulesAsync`, `GetBookedSlotsAsync`, `ReserveSlotAsync`, `ReleaseSlotAsync`. **No create/update-schedule method** — confirmed, matches plan's "a fresh database has zero `WorkingSchedule` rows" finding.

### 1.4 Repository structure

DI registration (`ServiceCollectionExtensions.cs`) confirmed:

| Interface | Registered implementation |
|---|---|
| `ICustomerRepository` | `BackendCustomerRepository` |
| `IDashboardRepository` | `BackendDashboardRepository` |
| `IBookingRepository` | `BackendBookingRepository` |
| `ISpecialistRepository` | `BackendSpecialistRepository` |
| `IServiceRepository` | `BackendServiceRepository` |
| `ICalendarRepository` | **`EfCalendarRepository`** (still local — unchanged) |

Calendar is confirmed the one remaining local-only vertical slice, exactly as the plan states. `FakeCalendarRepository` remains in the codebase, unreferenced, consistent with the established Fake→Ef→Backend convention.

### 1.5 Backend availability API compatibility

Re-read directly against `ROJAN_Backend` @ `6943986`:

- `GET /api/v1/salons/{salonId}/specialists/{specialistId}/available-slots` (`AvailabilityController.kt`) — confirmed `serviceId` (required `@RequestParam`), `date` (required), `slotIntervalMinutes` (default `15`) → `List<TimeSlotResponse>`. Matches plan §2 exactly, field for field.
- `TimeSlotEngine`/`GetAvailableSlotsUseCase` five-input model (working hours, weekly availability, overrides, leaves, blocks) plus live-booking exclusion and past-time exclusion for "today" — file present and structurally consistent with the plan's described logic (not re-traced line-by-line in this pass; the plan's own audit already read it in full and nothing in the surrounding code suggests it changed).
- Schedule-authoring endpoints (`working-hours`, `weekly-availability`, `overrides`, `leaves`, `blocks`) all confirmed present under the specialist/salon routes the plan lists.

**Conclusion: Plan v1 is fully valid and current. No re-audit findings contradict it. Proceed on its basis.**

---

## 2. Previous Integration Verification

| Integration | Verdict | Evidence |
|---|---|---|
| Customer CRM | **PASS** | `ICustomerRepository` → `BackendCustomerRepository` (DI confirmed); repository tests pass |
| Service Integration | **PASS** | `IServiceRepository` → `BackendServiceRepository` (DI confirmed); repository tests pass |
| Specialist Integration | **PASS** | `ISpecialistRepository` → `BackendSpecialistRepository` (DI confirmed); repository tests pass |
| Booking API | **PASS** | `IBookingRepository` → `BackendBookingRepository` (DI confirmed); repository tests pass. Read/status-update paths work; `CreateBookingAsync`'s `NotSupportedException` is a known, already-documented limitation (§1.2 above), not a regression |

**Test evidence (executed, no code changed):**
- `dotnet build RojanDesktop.sln` — **Build succeeded, 0 errors, 0 warnings.**
- `dotnet test` filtered to `BackendCustomerRepositoryTests`, `BackendServiceRepositoryTests`, `BackendSpecialistRepositoryTests`, `BackendBookingRepositoryTests`, `BackendDashboardRepositoryTests` — **65/65 passed, 0 failed.**

All four integrations remain intact. Nothing in the current repository state contradicts or weakens the completed-milestone claims from the prior sessions.

---

## 3. Product Decision Confirmed

> **Manual "Toggle Slot → Booked" is removed. Source of truth: Real Booking only.**

This is now the confirmed direction, resolving Plan v1 §9 Risk 1 in favor of its third listed option (retire the toggle for backend data / read-only display), explicitly ruling out the other two options the plan raised (repurposing the toggle into a mini booking-creation flow, or keeping `CalendarPage` local/EF-only indefinitely as a permanent exception).

**Direct consequences, confirmed from the code as it stands today:**
- `CalendarPageViewModel.ToggleSlotCommand`/`ToggleSlotAsync` and its `ICalendarCommandService` dependency go away for the standalone Calendar page.
- `CalendarPage.xaml`'s per-slot clickable `Button` (line 136, bound to `ToggleSlotCommand`) needs to become non-interactive display, not a command target.
- A slot's Booked/Available state must come from real `Booking` data, not from `ICalendarCommandService.ReserveSlotAsync`/`ReleaseSlotAsync` calls against `EfCalendarRepository`.

**Sub-question this order does not resolve, flagged for explicit decision before/during Phase 3 (not decided by this review, per its review-only scope):** Plan v1 §3 already established that `available-slots` requires a `serviceId` and returns *only free* slots — it has no signal for "this slot is Booked," and Booked/Unavailable as distinct states have no backend equivalent (plan §3, "Status differences" row). A "real booking is the only source of truth" read-only calendar therefore has two materially different possible implementations:

1. **Availability-engine-backed:** call `available-slots` for the grid's Available cells (still needs *some* `serviceId` — the standalone page still has no service-selection concept), and separately cross-reference `GET .../bookings` to render Booked cells. Two data sources per render.
2. **Booking-backed only:** derive the whole grid from the specialist's working hours/weekly availability (for grid bounds) plus `GET .../bookings` (for what's occupied) — never calls `available-slots` at all, sidestepping the `serviceId` problem entirely for this screen, at the cost of the grid no longer reflecting service-duration-aware slot boundaries.

Both are consistent with "no manual toggle, real booking is truth" — they differ on whether the standalone page still shows an *availability* grid (option 1, needs a service decision) or becomes a *bookings* grid (option 2, doesn't). This is a design fork, not a plumbing detail, and should be settled explicitly before Phase 3 implementation starts on `CalendarPage` specifically. It does **not** block the Booking Wizard's date/time step, which already has a service in scope and needs option 1's mechanism regardless.

---

## 4. (reserved — see §3 for the product-decision section; numbering follows the order's own §1–§4 request, folded into §1–§3 above for readability)

---

## 5. Phase 3 Implementation Checklist

Scoped to Plan v1 §8 Phases 1–3 (the `serviceId` interface fix, the `TimeSlotResponse` contract, and `BackendCalendarRepository`), adjusted for the confirmed toggle-removal decision (§3 above). Phase 4 (the `CalendarPage` fate) is included per §3's still-open sub-question. Phase 5 (wiring the Wizard to real booking creation) remains explicitly out of scope, unchanged from the plan.

### 5.1 Required files/classes to change

| File | Change |
|---|---|
| `src/Rojan.Desktop.Application/Calendar/ICalendarQueryService.cs` | Add `serviceId` to `GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync` (additive — see §5.2) |
| `src/Rojan.Desktop.Application/Calendar/CalendarQueryService.cs` | Update signature to compile against the widened interface; local/EF generation keeps its fixed-30-minute behavior, `serviceId` unused there (or used only to validate presence) |
| `src/Rojan.Desktop.Application/Api/Contracts/TimeSlotResponse.cs` (new) | New wire DTO, mirrors `BookingResponse.cs`/`ServiceResponse.cs` precedent (`{ start, end }` — `LocalDateTime`-equivalent, no offset, same convention as `BookingResponse.StartTime`/`EndTime`) |
| `src/Rojan.Desktop.Infrastructure/Calendar/BackendCalendarRepository.cs` (new) | Implements `ICalendarQueryService` directly (not `ICalendarRepository` — deliberate deviation, per plan §3/§8.3). Calls `available-slots` once per (specialist, date), maps `TimeSlotResponse` → `AvailabilitySlotDto` as `Available`. `GetScheduledSpecialistsAsync` needs a new data source too — plan confirms `ISpecialistQueryService` already has real backend specialist ids (Phase 2), so this method should source from there instead of `ICalendarRepository.GetWorkingSchedulesAsync` |
| `src/Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` | Register `BackendCalendarRepository` against `ICalendarQueryService` (not `ICalendarRepository` — `ICalendarRepository`/`EfCalendarRepository` registration is untouched, since `ICalendarCommandService`'s local implementation still depends on it, per §3) |
| `src/Rojan.Desktop.Application/BookingWorkflow/BookingWorkflowService.cs` | `GetAvailableSlotsAsync` must now pass the selected service's id through to `ICalendarQueryService.GetDailyAvailabilityAsync` — it already has `WorkflowServiceOptionDto` in scope one step earlier |
| `src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs` | Remove `ToggleSlotCommand`/`ToggleSlotAsync` and the `ICalendarCommandService` dependency (per §3 decision); becomes read-only. Exact data-source shape (available-slots vs. bookings-backed) depends on the §3 open sub-question |
| `src/Rojan.Desktop.Presentation/Views/Calendar/CalendarPage.xaml` | Remove the per-slot `Button`'s `Command`/`CommandParameter` binding (line 136) — slot cells become non-interactive display elements |

### 5.2 Interface changes

- `ICalendarQueryService.GetDailyAvailabilityAsync(string specialistId, DateOnly scheduleDate, ...)` → add `string serviceId`. **Must be additive** (Plan v1 Risk 2: this is the one non-additive-if-done-carelessly step in the whole integration) — either an optional trailing parameter with a documented default, or an overload, so `CalendarQueryService`'s local/EF implementation keeps compiling without inventing a fake duration for data it was never given a service for.
- `GetWeeklyAvailabilityAsync` needs the same addition, for the same reason (it calls the daily method 7 times internally).
- `ICalendarCommandService` — **no interface change**. It is retained as-is for `BookingWorkflowService`'s reserve/release-around-create mechanic (still targeting local `EfCalendarRepository` until Phase 5). Do not delete or repurpose it in Phase 3, per §3's scoping — that would be an architecture change, out of this review's and this phase's authorization.

### 5.3 Repository changes

- **New:** `BackendCalendarRepository` implementing `ICalendarQueryService` (Application-layer interface, not the Domain-layer `ICalendarRepository` — this is the one place in the whole Backend-integration effort where Infrastructure implements an Application interface directly, and it should be documented as a deliberate, explained exception, matching every other `Backend*Repository`'s own-doc-comment convention already established in this codebase).
- **No change** to `ICalendarRepository`, `EfCalendarRepository`, or `FakeCalendarRepository` — they stay wired to `ICalendarCommandService`'s implementation, unreferenced by the new query path.
- **No backend schema/migration work** — every endpoint Phase 3 needs already exists and is owner-authorized/tenant-scoped (Plan v1 §7: "None on the backend").

### 5.4 ViewModel impacts

- `CalendarPageViewModel`: loses `ToggleSlotCommand`, `ICalendarCommandService` constructor dependency; `SelectedSpecialist`/`SelectedDate`/`ViewMode` load paths stay structurally the same but now resolve through `BackendCalendarRepository`. If §3's open sub-question resolves toward "still an availability-engine grid" (option 1), the ViewModel also needs a service-selection concept it does not have today — a real UI addition, not just plumbing.
- `BookingWizardViewModel`: no direct change — it already delegates to `BookingWorkflowService.GetAvailableSlotsAsync`, which is the one getting the `serviceId` threaded through.

### 5.5 Test requirements

- `CalendarQueryServiceTests.cs` — extend for the new `serviceId` parameter on the local implementation (default/unused-but-present behavior).
- New `BackendCalendarRepositoryTests.cs` (Infrastructure.Tests), following the exact pattern of `BackendCustomerRepositoryTests`/`BackendServiceRepositoryTests`/etc. — mock `IApiClient`, assert the `available-slots` URL/query params, assert `TimeSlotResponse` → `AvailabilitySlotDto` mapping.
- `CalendarPageViewModelTests.cs` — **6 existing references to `ToggleSlot`/`ReserveSlotAsync`/`ReleaseSlotAsync` need removal or rewrite** (confirmed by direct read of the test file); add coverage for the new read-only load path instead.
- `StubCalendarQueryService.cs` (both `BookingWorkflow` and `Presentation.Tests` copies) — update to the widened interface signature.
- Architecture tests (already part of the existing 6-test architecture suite per `ROJAN_Booking_Integration_Implementation_Report_v1.md`) should be re-run once `BackendCalendarRepository` exists, to confirm the Infrastructure→Application dependency direction it deliberately deviates on (implementing `ICalendarQueryService` rather than `ICalendarRepository`) doesn't trip a rule written before this exception was anticipated.

### 5.6 Migration/API requirements

- **None on the backend** — confirmed in §1.5, matches Plan v1 §7 exactly. No schema change, no new endpoint, no contract change needed on `ROJAN_Backend`.
- **No local database migration** on the Owner App side either — `EfCalendarRepository`/`WorkingSchedule` persistence is untouched; Phase 3 only adds a new read path that bypasses it for backend-sourced calendar data.

---

## 6. Recommendation

Plan v1 is validated and current — proceed on its basis. Sequence: (1) the additive `serviceId` interface change, (2) the `TimeSlotResponse` contract, (3) `BackendCalendarRepository` against `ICalendarQueryService`, exactly as Plan v1 §8/§10 already recommends. The one item this review adds beyond the plan: **get an explicit answer to §3's open sub-question (availability-engine-backed vs. bookings-backed read-only grid) before starting `CalendarPageViewModel`/`CalendarPage.xaml` changes specifically** — the Booking Wizard's date/time step can proceed on (1)–(3) without waiting for that answer, since it already has a service in scope and needs no redesign of what "Booked" means.

**No code was written for this review.**
