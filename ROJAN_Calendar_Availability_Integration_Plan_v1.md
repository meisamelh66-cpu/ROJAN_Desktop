# ROJAN Calendar / Availability Integration — Audit + Plan v1

**Priority:** P0
**Scope:** Audit only. No code, no architecture, no implementation in this pass.
**Context:** Customer CRM, the owner/reception booking endpoint, Service Integration, and Specialist Integration are all complete. This is the last data-source gap before the Reception Booking Flow can run entirely against real backend data.

**Target flow:**
```
Reception / Owner -> Select Specialist -> Load Real Availability -> Select Date -> Select Available Time Slot -> Create Booking
```

**Headline finding:** the backend's availability engine is materially *more capable* than the Owner App's local one — it is already service-duration-aware, already excludes past times on the current day, and already accounts for five distinct schedule concepts (working hours, weekly availability, overrides, leaves, blocks) plus live booking conflicts. The integration work is real but low-risk. The one finding that actually blocks a clean swap is an **interface gap, not a data gap**: `ICalendarQueryService.GetDailyAvailabilityAsync(specialistId, date)` has no `serviceId` parameter, but the backend's slot engine requires one to compute slot length. This must be resolved before a `BackendCalendarRepository` can be written correctly.

---

## 1. Owner App Calendar Audit

| Component | Finding |
|---|---|
| Calendar screens | Two distinct consumers: (1) the standalone `CalendarPage`/`CalendarPageViewModel` — a specialist/date picker plus a Day or Week availability grid, with a toggle command that reserves an Available slot or releases a Booked one directly, **with no customer/service attached at all**; (2) the Booking Wizard's Date/TimeSlot steps (`BookingWizardViewModel`), which load slots for a specialist+date and let Reception pick one to carry into booking creation |
| Availability ViewModels | `CalendarPageViewModel` (Day/Week toggle via `CalendarViewMode`, two-stage load: scheduled-specialist list, then that specialist's availability) and `BookingWizardViewModel` (calls through `BookingWorkflowService.GetAvailableSlotsAsync`, not `ICalendarQueryService` directly) |
| Time slot generation logic | `Application.Calendar.CalendarQueryService` — **local, fixed 30-minute slots** (`SlotDuration = TimeSpan.FromMinutes(30)`), generated across one `WorkingSchedule` row's single start/end window per day, marked `Booked` if overlapping a locally-stored booked range, `Unavailable` if overlapping a recurring (weekly, not date-specific) `Breaks` entry. **Never consults the selected service's actual duration** - every slot is 30 minutes regardless of what service was picked |
| Booking Wizard date/time step | `BookingWizardViewModel.LoadAvailableSlotsAsync` → `BookingWorkflowService.GetAvailableSlotsAsync(specialistId, date)` → `ICalendarQueryService.GetDailyAvailabilityAsync` - **the Wizard already knows the selected service's duration** (`WorkflowServiceOptionDto.DurationMinutes`, resolved one step earlier) but never passes it through to slot generation, because the interface has nowhere to put it |
| Local SQLite calendar data | `EfCalendarRepository` — confirmed unchanged since the v1/v2 audits: no create/update-schedule method exists at all (`ICalendarRepository` has none), so a fresh database has zero `WorkingSchedule` rows and `GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync` **throw `InvalidOperationException`** for any specialist with none - a real, pre-existing crash path for any backend-sourced specialist today |
| Existing repositories/services | `ICalendarRepository` → `EfCalendarRepository` (unchanged); `ICalendarQueryService`/`ICalendarCommandService` (Application layer, own the slot-generation and reserve/release logic respectively) - both would need real changes, not just a repository swap, since today's slot generation happens in `CalendarQueryService` itself rather than being delegated to the repository |

**Current source: Local.** **Current slot generation rules:**
- **Duration handling:** fixed 30 minutes, ignores the selected service entirely.
- **Working hours:** one `WorkingSchedule.StartTime`/`EndTime` window per specialist per day-of-week (note: this is *per-specialist*, not salon-wide - conflates what the backend keeps as two separate concepts, see §3).
- **Blocked times:** `WorkingSchedule.Breaks` - recurring, weekly, not date-specific. No concept of a one-off override, a leave date-range, or an ad-hoc single-date block.
- **Conflicts:** checked against `ICalendarRepository.GetBookedSlotsAsync` (locally reserved ranges) inside `CalendarCommandService.ReserveSlotAsync`, re-checked at write time; separately, `BookingCommandService.CreateBookingAsync` also runs its own independent conflict check (`EnsureNoConflictAsync`) against locally-stored bookings - two separate, redundant local safety nets, neither backed by real atomicity (`Ef`/`Fake` in-memory checks, not a database-level guarantee).

## 2. Backend Availability Audit (re-verified by direct inspection, including the use case internals)

| Endpoint | Method | Request | Response | Auth | Salon context |
|---|---|---|---|---|---|
| `/api/v1/salons/{salonId}/working-hours/{dayOfWeek}` | PUT/GET/DELETE | `SetWorkingHoursRequest(intervals)` | `WorkingHoursResponse` | write: owner only; read: any authenticated | Path-scoped |
| `/api/v1/salons/{salonId}/working-hours` | GET | — | `List<WorkingHoursResponse>` | any authenticated | Path-scoped |
| `.../specialists/{id}/schedule/weekly-availability[/{dayOfWeek}]` | PUT/GET/DELETE | `SetWeeklyAvailabilityRequest(intervals)` | `WeeklyAvailabilityResponse` | write: owner only | Path-scoped |
| `.../specialists/{id}/schedule/overrides[/{date}\|/{overrideId}]` | PUT/GET/DELETE | `SetScheduleOverrideRequest(date, intervals, reason?)` | `ScheduleOverrideResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `.../specialists/{id}/schedule/leaves[/{leaveId}]` | POST/GET/DELETE | `CreateLeaveRequest(startDate, endDate, reason?)` | `LeaveResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `.../specialists/{id}/schedule/blocks[/{blockId}]` | POST/GET/DELETE | `CreateBlockRequest(date, start, end, reason?)` | `BlockResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `/api/v1/salons/{salonId}/specialists/{id}/available-slots` | GET | `serviceId` (required), `date` (required), `slotIntervalMinutes` (default 15) | `List<TimeSlotResponse>` (`{start, end}`, both `LocalDateTime`) | any authenticated | Path-scoped |

**How `available-slots` actually computes a day's slots** (`GetAvailableSlotsUseCase` + `TimeSlotEngine`, read in full for this audit):
1. Specialist and service must exist, be active, and belong to the salon in the path - else 404.
2. If the specialist has an approved leave covering the date → **empty list**, no error.
3. Salon `WorkingHours` for that day-of-week is the outer bound - if none configured → **empty list**.
4. A same-date `Override` takes priority over the specialist's recurring `WeeklyAvailability` for that day-of-week; if neither exists → **empty list**.
5. Salon hours ∩ (override-or-weekly-availability), minus any `Block` for that date → the day's free windows.
6. Existing **active** bookings for that specialist/day are subtracted as busy time.
7. If the requested date is *today*, slots starting before the current wall-clock time are excluded - the Owner App's local generator has no equivalent (it would show a past 9am slot as Available at 3pm).
8. Slots are generated at the *service's real duration*, stepped every `slotIntervalMinutes` (caller-configurable, defaults to 15) - not a fixed 30 minutes.

**Conflict prevention beyond availability display:** `BookingRepository.reserve()` (used by both the self-service and the new owner/reception booking creation paths) performs the actual double-booking check atomically at write time, independent of whatever `available-slots` showed a moment earlier - this is the backend's real safety net, already exercised by 244/244 passing backend tests including `BookingConflictConcurrencyIntegrationTest`.

## 3. Domain Model Comparison

| Concern | Owner App | Backend | Finding |
|---|---|---|---|
| Slot generation | `CalendarQueryService` (Application layer) does it, from raw `WorkingSchedule` rows `ICalendarRepository` returns | `GetAvailableSlotsUseCase`/`TimeSlotEngine` (backend, server-side) does it entirely; the Owner App would only ever consume already-generated `TimeSlotResponse` values | A `BackendCalendarRepository` implementing today's `ICalendarRepository` contract (raw schedule data in, generate locally) **cannot represent the backend's five-input model losslessly** - the correct integration point is `ICalendarQueryService`, not `ICalendarRepository`, mirrored directly onto `available-slots` rather than reusing the existing local generation algorithm at all. (Same conclusion already reached in `ROJAN_Reception_Booking_Integration_Audit_v2.md` §5, re-confirmed here with the use case's actual logic now read in full.) |
| **Missing required parameter** | `ICalendarQueryService.GetDailyAvailabilityAsync(specialistId, date)` / `GetWeeklyAvailabilityAsync(specialistId, weekStart)` - **no `serviceId` parameter anywhere in the interface** | `available-slots` **requires** `serviceId` - slot length is derived from it | **This is the one real blocker.** Not a data-mapping problem - an interface signature gap. The Booking Wizard already has the selected service in scope one step earlier and could supply it; the standalone `CalendarPage` has **no service concept at all** today and would need either a new service-selection control or an explicit policy decision (see §6, Risk 1) before it could call this endpoint meaningfully. |
| Working hours ownership | Per-specialist (`WorkingSchedule` has no salon-level twin) | Two separate levels: salon-wide `WorkingHours` (outer bound, applies to every specialist) + per-specialist `WeeklyAvailability` (narrower, within it) | The Owner App has no field to hold "salon operating hours" independent of a specialist - not consumed by `available-slots` mapping directly (the backend already intersects both server-side), so this is informational, not a blocking gap, but confirms the local model is a simplification, not a subset. |
| Breaks vs. Overrides/Leaves/Blocks | `WorkingSchedule.Breaks: List<TimeSlot>` - recurring by day-of-week only | Overrides (date-specific interval replacement), Leaves (date-range, whole-day), Blocks (date-specific single window) - three distinct, date-specific concepts | Not a mapping target at all once `ICalendarQueryService` calls `available-slots` directly (server pre-computes everything) - the local `Breaks` concept becomes entirely unused for backend-sourced data, same as `CalendarQueryService`'s own generation algorithm. |
| Status differences | `AvailabilityStatus.Available / Booked / Unavailable` | No equivalent enum - `available-slots` returns *only* free slots (a slot that doesn't appear is implicitly unavailable, for any reason) | `Booked`/`Unavailable` as distinct, explained states have no direct backend signal to map from - a `BackendCalendarRepository` can only ever produce `Available` entries from this endpoint's response. Showing a specialist's *taken* slots (e.g. a Week-view grid with visibly Booked cells) would need `GET .../bookings` cross-referenced separately, not something `available-slots` provides. |
| Timezone handling | `TimeSlot`/`AvailabilitySlot` use `DateTimeOffset`; `CalendarQueryService.ToDateTimeOffset` stamps the **machine's current local UTC offset** (`DateTimeOffset.Now.Offset`) onto every generated slot, not a stored salon timezone | `TimeSlotResponse{start, end}` are Kotlin `LocalDateTime` - no offset at all, same as `BookingResponse.startTime`/`endTime` | Same convention already established and working for Booking (`BackendBookingRepository`'s own doc comment: treat the backend's offset-less `LocalDateTime` as the app's own local wall-clock time via `DateTimeOffset.Now.Offset`). No new timezone problem - this is a direct continuation of an already-solved pattern, not a fresh one. |
| Slot interval granularity | Fixed 30 minutes, not configurable | `slotIntervalMinutes`, defaults to 15, caller-supplied | The Owner App currently has no UI/setting for this at all; a `BackendCalendarRepository` would need to pick a default (15, matching the backend's own default, is the obvious choice) since nothing today lets a user choose. |

## 4. Booking Compatibility Check

`Selected availability slot -> POST /api/v1/salons/{salonId}/bookings`:

| Check | Finding |
|---|---|
| Service duration | Already compatible once `serviceId` flows into slot generation (§3) - a slot selected from `available-slots` is, by construction, exactly as long as the service being booked, so `CreateBookingForCustomerRequest.startTime` needs no separate duration field; the backend recomputes `endTime` server-side from the service anyway (unchanged, already true for the existing self-service flow). |
| Specialist availability | A slot returned by `available-slots` was, at query time, provably free per the backend's own five-input model - stronger than what local generation could ever guarantee, since it already incorporates live bookings from every source (mobile OTP customers, the new reception endpoint, anything else), not just what the Owner App happens to know locally. |
| Conflict handling | Fully delegated to `BookingRepository.reserve()` at booking-creation time (already true and tested, per §2) - a `BackendCalendarRepository` does not need its own conflict-checking logic at all, unlike the current local `CalendarCommandService.ReserveSlotAsync`'s hand-rolled re-check. |
| Double booking prevention | Real, atomic, server-side, already covered by `BookingConflictConcurrencyIntegrationTest` (backend) - once the Wizard is wired to the new endpoint (a separate, not-yet-done step - explicitly excluded from every phase completed so far), the Owner App's local `EnsureNoConflictAsync`/reserve-then-create-then-rollback dance becomes unnecessary, not just redundant. |

**This confirms:** once `serviceId` reaches slot generation (§3) and the Wizard is wired to `POST /api/v1/salons/{salonId}/bookings` (still not done - out of scope for every phase completed to date, including this audit), the full target flow is mechanically sound with no further compatibility gaps.

## 5. Performance Considerations

- **Number of API calls, Day view / Wizard date step:** 1 call per (specialist, date) selection - identical cost to every other `Backend*Repository` read already built (Customer/Booking/Service/Specialist), no increase.
- **Number of API calls, Week view:** `GetWeeklyAvailabilityAsync` currently loops the local generator 7 times in-process; a backend-connected equivalent would need **7 separate `available-slots` calls** (no bulk/week endpoint exists on the backend) - the one place in this integration where the call count is materially higher than today's local cost. Not necessarily a problem (7 small GETs is still fast), but worth naming explicitly since every other integration phase so far has been closer to 1:1.
- **Caching requirements:** none required, and none should be added — this codebase's own established convention (`BackendBookingRepository`'s own doc comment: "resolved fresh on every call... a known, documented tradeoff") already accepts re-fetching on every read rather than caching, specifically *because* availability is the most time-sensitive data in the app (a cached slot list could show a slot as free after someone else just booked it). Introducing caching here would be a regression in correctness, not an optimization.
- **Calendar loading strategy:** the existing two-stage load (`CalendarPageViewModel`: specialist list first, then that specialist's availability once selected; `BookingWizardViewModel`: options first, availability only after Date step is reached) already matches an on-demand, not-prefetched strategy - no change needed to the loading *strategy* itself, only to what each load actually fetches from.

## 6. Current state summary

| Layer | Ready? |
|---|---|
| Backend: `available-slots` engine (working hours, weekly availability, overrides, leaves, blocks, live conflicts, past-time exclusion) | **YES** - complete, already more capable than the local equivalent |
| Backend: schedule authoring APIs (working hours, weekly availability, overrides, leaves, blocks) | **YES** - all exist, all owner-authorized, all tenant-scoped |
| Owner App: `ISpecialistQueryService`/backend specialist ids feeding into a future `BackendCalendarRepository` | **YES** - Phase 2 already guarantees real, directly-usable backend specialist ids |
| Owner App: `ICalendarQueryService` interface shape | **NO** - missing the `serviceId` parameter `available-slots` requires; today's contract cannot express the request the backend needs |
| Owner App: `TimeSlotResponse` wire contract | **NO** - does not exist yet in `Api.Contracts` (small, mechanical addition, same pattern as every prior phase) |
| Owner App: standalone `CalendarPage`'s "reserve a slot with no booking" toggle feature | **NO equivalent on the backend** - a real design question, not a plumbing gap (see Risk 1) |
| Owner App: Wizard wired to the new booking-creation endpoint | **NO** - unchanged, still not done, a separate and still-outstanding piece of work from every phase completed so far |

## 7. Missing APIs

**None on the backend.** Every schedule-authoring and availability-computation endpoint the Owner App needs already exists, confirmed by reading the use case internals in full. The only "missing" surface is on the Owner App side: the `serviceId` parameter on `ICalendarQueryService`, and the `TimeSlotResponse` wire contract - both interface/plumbing additions, not new backend work.

## 8. Required implementation phases (proposed, not started)

1. **Add `serviceId` to `ICalendarQueryService.GetDailyAvailabilityAsync`/`GetWeeklyAvailabilityAsync`** (Application-layer interface change) and thread it from the Booking Wizard (already has the selected service in scope). This is the one genuine interface change this integration needs, and it should land *before* `BackendCalendarRepository` exists, not alongside it - `CalendarQueryService`'s current local implementation would need a compatible (if arbitrary) default duration to keep compiling for local/EF data in the meantime, or the change should be additive (optional parameter) to avoid a hard break.
2. **Add the `TimeSlotResponse` wire contract** to `Api.Contracts` - mechanical, mirrors `BookingResponse`/`ServiceResponse`/`SpecialistResponse`'s existing precedent exactly.
3. **`BackendCalendarRepository`, targeting `ICalendarQueryService` directly (not `ICalendarRepository`)** - a deliberate deviation from the swap-the-repository pattern every prior phase used, because (per §3) the backend already does slot generation server-side; reusing `CalendarQueryService`'s local generation algorithm on top of raw schedule data would either require re-fetching five separate schedule endpoints per day (working hours, weekly availability, override, leave, blocks) and reimplementing `TimeSlotEngine`'s logic client-side (duplicated, risk of drift), or simply calling `available-slots` once and mapping the response directly - the latter is correct and far simpler. `ICalendarCommandService.ReserveSlotAsync`/`ReleaseSlotAsync` have no backend equivalent to call (see Risk 1) and would need their own explicit decision, not a mechanical port.
4. **Decide the standalone `CalendarPage`'s fate** - see Risk 1. Not a "phase" so much as a prerequisite decision before Phase 3 can fully land for that screen specifically (the Booking Wizard's date/time step has no such ambiguity and could proceed independently).
5. **Wire the Wizard to `POST /api/v1/salons/{salonId}/bookings`** - unchanged scope from every prior audit, still the final step, still not part of this phase.

Phases 1-2 are small and independent of each other; Phase 3 depends on both. Phase 4 is a decision, not effort, and can happen in parallel with 1-3. Phase 5 remains explicitly out of scope for this ticket and every one before it.

## 9. Risks

1. **The standalone `CalendarPage`'s "toggle a slot to Booked with no customer/service attached" feature has no backend equivalent.** The backend has no concept of a reservation that isn't a real `Booking` (with a real customer, service, and specialist). This is a genuine product/design question - options include: repurpose the toggle into a mini booking-creation flow (scope creep for this integration), keep the page local/EF-only indefinitely (an intentional exception to "everything is backend-connected now"), or retire the toggle feature for backend data and make the page read-only availability display. **Not resolvable by this audit** - needs an explicit decision before Phase 3/4 implementation.
2. **`GetDailyAvailabilityAsync`'s interface change (Phase 1) is the one non-additive-if-done-carelessly step in this entire integration effort so far.** Every other phase (Customer, Booking, Service, Specialist) only ever added trailing optional parameters or new types; this is the first case where an *existing, required* method signature needs a new parameter that has no sensible default for local data (what duration would `EfCalendarRepository`'s slot generation use for a `serviceId` it's never been given?). Needs a specific design choice, not just "add a parameter."
3. **Week view's 7x API-call cost** (§5) is real but almost certainly acceptable - flagged so it doesn't surprise anyone during implementation, not because it's a blocker.
4. **A fresh backend salon's specialists have no configured `WorkingHours`/`WeeklyAvailability` by default** - `available-slots` correctly returns an empty list for this case (no crash, unlike today's local `EfCalendarRepository` throwing `InvalidOperationException`), which is strictly better, but still means a newly onboarded salon would see "no availability anywhere" until an owner configures schedules through the (already-existing, already-tested) schedule-authoring endpoints - an onboarding/UX consideration, not a code defect.

## 10. Recommendation

Proceed in the order listed in §8: resolve the `serviceId` interface gap first (Phase 1, additive to avoid breaking local/EF data), add the `TimeSlotResponse` contract (Phase 2, trivial), then build `BackendCalendarRepository` against `ICalendarQueryService` directly rather than `ICalendarRepository` (Phase 3, per §3's finding that the backend already does the work `CalendarQueryService` does locally, and duplicating that logic client-side would be strictly worse than calling the one endpoint that already returns the answer). Get an explicit decision on the standalone `CalendarPage`'s toggle-to-reserve feature (Risk 1) before or alongside Phase 3-4, since it changes the shape of `ICalendarCommandService`'s backend story, not just `ICalendarQueryService`'s. Defer Phase 5 (wiring the Wizard to real booking creation) as its own follow-up ticket, exactly as every prior audit has recommended - Calendar integration does not need to wait for it, and it does not need to wait for Calendar integration either, but shipping Phase 5 before Phase 3 would mean Reception picks a real customer/service/specialist against a still-fake availability grid, which is a worse failure mode than the reverse ordering.

**No code was written for this audit.** Awaiting a decision on the `serviceId` interface change (Phase 1) and the `CalendarPage` toggle-feature question (Risk 1) before implementation planning proceeds further.
