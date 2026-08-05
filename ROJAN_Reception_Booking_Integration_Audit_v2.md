# ROJAN Reception Booking Flow — Integration Audit + Implementation Plan v2

**Priority:** P0
**Scope:** Audit only. No code, no architecture, no implementation in this pass.
**Context:** Backend Phase 0 is complete — `POST /api/v1/salons/{salonId}/bookings` (owner/reception-authorized booking creation) now exists, per `ROJAN_Reception_Booking_Backend_Implementation_Report_v1.md`. This audit re-verifies the Owner App side against that new capability and specifies exactly what remains before the target flow can run end-to-end.

**Target flow:**
```
Reception / Owner
  -> Select Customer or Create Customer
  -> Select Service
  -> Select Specialist
  -> Select Date & Time
  -> Create Booking
  -> Customer Timeline Update
  -> Confirmation
```

**Headline finding:** the backend blocker identified in v1 is resolved — a real endpoint now exists to create a booking for a specific customer, tenant-isolated and tested (244/244 backend tests passing). The Owner App side is **unchanged since v1** (confirmed by `git status` — zero source diffs since the last Owner App milestone commit `b7881c5`): the Booking Wizard still calls `BackendBookingRepository.CreateBookingAsync`, which still unconditionally throws, and Service/Specialist/Calendar selection still read local SQLite data. Closing that gap is now a **pure Owner App integration task** — no further backend work is required to reach a working linked-customer reception flow, only the Owner App needs to catch up to what the backend can now do.

---

## 1. Owner App Booking Flow Audit

| Component | Current implementation | PASS/FAIL | Connected to Backend | Local/Fake data |
|---|---|---|---|---|
| Booking Wizard (`BookingWizardViewModel`/`BookingWizardView`) | Full 7-step dialog: Customer → Service → Specialist → Date → TimeSlot → Review → Confirmation, orchestrated by `BookingWorkflowService` | **PASS** (shape matches target exactly) | Partially (see rows below) | No (UI itself) |
| Booking ViewModels | `BookingWizardViewModel` (creation) + `BookingPageViewModel` (list/quick-add/status actions) | PASS (built, unchanged since v1) | N/A | N/A |
| Booking Repository | `IBookingRepository` → `BackendBookingRepository`. `CreateBookingAsync` **still unconditionally throws `NotSupportedException`** — unchanged, this is the exact call `BookingWorkflowService.CreateBookingAsync` makes | **FAIL** | YES (but Create is a hard no-op) | No |
| Booking Screens | `BookingPage.xaml`, `BookingWizardView.xaml` | PASS (built) | N/A | N/A |
| Customer Selection | `ICustomerQueryService.GetCustomersAsync()` → `BackendCustomerRepository` (completed since v1) | **PASS** | **YES** | No |
| Service Selection | `IServiceQueryService.GetServicesAsync()` → `EfServiceRepository` | **FAIL** (data source) | **NO** | **YES** |
| Specialist Selection | `ISpecialistQueryService.GetSpecialistsAsync()` → `EfSpecialistRepository` | **FAIL** (data source) | **NO** | **YES** |
| Calendar / Availability | `ICalendarQueryService.GetDailyAvailabilityAsync()` → `EfCalendarRepository`, fixed 30-minute slots, throws for a specialist with zero seeded `WorkingSchedule` rows | **FAIL** (data source, and structurally simpler than the backend engine — see §5) | **NO** | **YES** |

No change from v1 in any of these rows — re-confirmed by direct inspection, not assumed from the prior audit. The one still-missing UI affordance also re-confirmed unchanged: the Wizard's Customer step remains pick-from-list only, no inline "Create New Customer" branch (the target flow's "Select Customer or Create Customer" fork is not represented in the wizard itself; customer creation is only reachable from the separate Customer page).

## 2. Customer Integration Audit

| Check | Finding |
|---|---|
| Customer selection source | `BackendCustomerRepository.GetCustomersAsync()` → real `GET /api/v1/salons/{salonId}/customers`, paged/concatenated |
| Existing customer loading | Real, backend-connected, confirmed working (`ROJAN_Owner_App_Customer_CRM_Integration_Report_v1.md`, 2156/2156 Owner App tests passing) |
| New customer creation | `CustomerCommandService.CreateCustomerAsync` → real `POST /customers` — works, but only from the standalone Customer page, not from inside the Wizard |
| Customer id mapping | `WorkflowCustomerOptionDto.Id` is the real backend CRM `Customer.id` (a `UUID` string) — no id-space mismatch here, unlike the Booking/Customer relationship (see §6) |

**Can Reception select a real CRM customer? YES.** The list is real, backend-sourced data today. What Reception cannot yet do is *complete a booking* for every customer they select — only ones already linked to an account (`Customer.userId != null`) will succeed against the new `POST /api/v1/salons/{salonId}/bookings` endpoint; an unlinked walk-in will get a `409 CUSTOMER_NOT_LINKED_TO_ACCOUNT` (see §7). This is a backend-enforced business rule the Owner App does not yet surface anywhere in the picker UI — today's Customer step has no way to indicate which customers are actually bookable.

## 3. Service Integration Audit

| | |
|---|---|
| Current source | **Local** — `EfServiceRepository` (SQLite), no `BackendServiceRepository` exists |
| Backend API | Complete and sufficient: `POST/GET/PUT/DELETE /api/v1/salons/{salonId}/categories` and `.../categories/{categoryId}/services` — no new backend endpoint needed |

**Required changes:**
- **API integration**: build `BackendServiceRepository` (new Infrastructure class), mirroring the now-3x-proven `BackendBookingRepository`/`BackendCustomerRepository` pattern — fetch categories, then each category's services, flatten.
- **Model mapping, non-trivial** (a real gap, not just plumbing):
  - Owner App `Service.Category` is a **closed C# enum** (`Hair, Colour, Nails, Skin, Spa, Consultation` — 6 fixed values). Backend `ServiceCategory` is a **real, per-salon, owner-named entity** (`ServiceCategoryResponse(id, salonId, name, description, active, ...)`) — an open set, arbitrary names. These are not compatible representations: a category named anything outside the fixed 6 has nowhere to map to. The Owner App's `ServiceCategory` type needs to become a real referenced entity (id + name), not an enum, before backend categories can be represented losslessly.
  - Owner App `Service.Status` is a 3-value enum (`Active, Seasonal, Discontinued`). Backend `Service.active` is a plain boolean. `Seasonal` has no backend equivalent — same category of status-mismatch already accepted for Booking (`InProgress`/`NoShow`) and handled there via a capability flag; the same pattern (flag "Seasonal" as unsupported for backend-sourced services, default to Active/Discontinued only) would apply here.
- **Missing fields**: none in the Service→backend direction beyond the two above; `Name`, `DurationMinutes`, `Price`, `Description` all map directly (same `FormatToman`-style price formatting already used for Booking/Dashboard/Customer).

## 4. Specialist Integration Audit

| | |
|---|---|
| Current source | **Local** — `EfSpecialistRepository` (SQLite), no `BackendSpecialistRepository` exists |
| Backend API | Complete and sufficient: `POST/GET/PUT/DELETE /api/v1/salons/{salonId}/specialists` — no new backend endpoint needed |

**Required changes:**
- **API integration**: build `BackendSpecialistRepository`, same proven pattern.
- **Model mapping**:
  - Owner App `Specialist` has `Title`, `Email`, `Phone` — **none of these exist on the backend `SpecialistResponse`** (backend has only `displayName`, `bio`, `photoUrl`, optional `userId`; email/phone would only be reachable indirectly via the linked `User`, and only for the specialist's own account, not queryable by the owner today). These three fields would need to map to empty/placeholder values for backend-sourced specialists, same "honest, not fabricated" approach already used for Customer's vestigial `Notes` field.
  - Backend `SpecialistResponse.photoUrl`/`userId` have no Owner App `Specialist` field to land in — additive fields would need to be added (same trailing-optional-parameter technique already used for `Customer.UserId`), or simply dropped if not needed by any current screen.
  - `Domain.Services.SpecialistService` (Owner App's own "which specialist can perform which service" assignment record) has **no backend equivalent at all** — the backend has no such relationship; `Service`/`Specialist` are fully independent there. This does not currently block the Wizard (its Specialist step already shows every active specialist regardless of the selected Service, unfiltered), but is worth naming as a concept that would not carry over if a future "only show qualified specialists" filter were added.

## 5. Availability / Calendar Audit

| Backend concept | Owner App local equivalent | Gap |
|---|---|---|
| Working Hours (`WorkingHoursController`, per-salon, per-day-of-week, multiple intervals) | `WorkingSchedule.StartTime`/`EndTime` (per specialist, per day - **note: local schedule is per-*specialist*, backend's Working Hours is per-*salon***) | Different ownership level entirely - backend separates salon-wide Working Hours from per-specialist Weekly Availability; the Owner App conflates both into one per-specialist `WorkingSchedule` |
| Specialist Weekly Availability (`SpecialistScheduleController`, per specialist, per day, multiple intervals) | `WorkingSchedule.StartTime`/`EndTime` - single interval only, no multi-interval support | Owner App supports exactly one start/end window per day; backend supports a list of intervals (e.g. morning + afternoon with a gap) |
| Schedule Overrides (one-off date-specific interval changes) | **None** | No local concept at all |
| Leaves (vacation date ranges) | **None** | No local concept at all |
| Blocks (ad-hoc single blocked windows) | `WorkingSchedule.Breaks: List<TimeSlot>` - recurring only (repeats every week on that day), not date-specific | Owner App's "Breaks" is the closest analog but is recurring-by-day-of-week, not a one-off block on a specific date - a materially different concept |
| Slot generation | Backend: real per-service duration, via `GetAvailableSlotsUseCase`, combining all five inputs above plus existing bookings | Owner App: fixed 30-minute slots (`CalendarQueryService.SlotDuration`), ignores selected service's actual duration entirely | The Wizard already resolves the selected service's `DurationMinutes` (`WorkflowServiceOptionDto`) but the local Calendar slot generator never uses it - slots are always 30 minutes regardless of what was picked |
| Empty/unseeded state | Backend: a specialist simply has no configured hours anywhere, availability correctly returns empty | Owner App: **throws `InvalidOperationException`** for a specialist with zero `WorkingSchedule` rows - a fresh database has none, and there is no authoring UI to create one (`ICalendarRepository` has no create/update-schedule method at all) | This is worse than "different data" - it's a crash path for any backend-sourced specialist, since nothing populates `WorkingSchedule` from a real `BackendCalendarRepository` today |

**Current behavior**: entirely local, structurally simpler than the backend (2 concepts vs. 5), and would actively throw for any real backend specialist today.

**Required integration path**: build `BackendCalendarRepository` calling `GET .../available-slots` (already service-duration-aware, already merges all five backend concepts and existing bookings server-side) directly, **replacing** `CalendarQueryService`'s local generation logic for backend-sourced specialists rather than trying to map backend Overrides/Leaves/Blocks into the local `WorkingSchedule.Breaks` shape (they don't fit). This is the same "swap the data source, keep the Application-layer contract (`ICalendarQueryService.GetDailyAvailabilityAsync`)" pattern as every prior `Backend*Repository`, but note `ICalendarQueryService`'s own slot-generation logic (currently in `CalendarQueryService`, Application layer) becomes largely redundant once the backend does this server-side — the new repository would need to translate `TimeSlotResponse` entries directly into `AvailabilitySlotDto`s rather than reusing the existing local generation algorithm at all.

## 6. Booking Creation Flow Audit (new endpoint, verified by direct inspection)

`POST /api/v1/salons/{salonId}/bookings` (`SalonBookingController.createForCustomer`):

| Aspect | Verified detail |
|---|---|
| Request DTO | `CreateBookingForCustomerRequest(customerId: UUID, serviceId: UUID, specialistId: UUID, startTime: LocalDateTime, notes: String?)` - all fields except `notes` required |
| Response DTO | `BookingResponse(id, salonId, serviceId, specialistId, customerId, startTime, endTime, status, notes, createdAt, updatedAt)` - identical shape to the existing self-service response; `customerId` in the response is the resolved **`User.id`**, not the CRM `Customer.id` sent in the request (see Customer mapping row) |
| Authentication | Bearer JWT required, any authenticated caller reaches the method, but... |
| Salon Context | ...`salon.ownerId == callerId` is checked inside the use case - non-owner gets `403 SalonAccessDeniedException`; the target customer's `Customer.salonId` must equal the path `salonId` - mismatch gets `404 CustomerNotFoundException` (cross-tenant access is not distinguished from "doesn't exist," consistent with every other controller in this codebase) |
| Customer mapping | `customerId` in the request is a **CRM `Customer.id`**. The use case resolves `Customer.userId`; if null, `409 CUSTOMER_NOT_LINKED_TO_ACCOUNT`; if present, that `UserId` becomes `Booking.customerId` via the unmodified `CreateBookingUseCase`. **The Owner App has no existing model field prepared to carry this id/status distinction today** - `WorkflowCustomerOptionDto(Id, FullName)` (the Wizard's own customer-option shape) does not carry a "has linked account" flag, so the Wizard cannot yet tell Reception in advance which selected customers are actually bookable. |

Fully confirmed working and tenant-isolated by `ReceptionBookingFlowIntegrationTest` (6/6 passing) and `CreateBookingForCustomerUseCaseTest` (5/5 passing) - re-verified present and unmodified since the prior report.

## 7. Walk-in Customer Capability

| Question | Answer | Why |
|---|---|---|
| Can be selected? | **YES** | The Wizard's Customer step already lists every CRM customer via `BackendCustomerRepository`, linked or not - no filtering by link status exists today. |
| Can create booking? | **NO, for an unlinked walk-in** / **YES, if already linked** | `POST /api/v1/salons/{salonId}/bookings` now exists and works for a linked customer (`Customer.userId != null`); an unlinked one gets `409 CUSTOMER_NOT_LINKED_TO_ACCOUNT` by design, not a crash or misattribution. This is a real, tested improvement over v1 (where *no* customer, linked or not, could be booked at all) but does not fully close the walk-in gap - most real walk-ins (someone who has never installed the mobile app or verified OTP) will still have no linked account. |
| Can appear in timeline? | **Partially, unchanged from v1** | Notes/tags/status-change entries work for any customer regardless of link status. A booking entry only ever appears for a linked customer with a real booking - unreachable for a still-unlinked walk-in, same limitation as before, now simply gated by a clear 409 instead of being unreachable for every customer. |

**Limitation to report plainly:** Phase 0 solved *authorization and tenant isolation* for owner-initiated booking, not the *walk-in identity* problem itself. A true walk-in (never registered, no phone verified via OTP) still cannot be booked through any path today. Closing this fully requires the larger `Booking.customerId` domain decision flagged in v1 §7/§8 (redesigning what a booking can reference) - still out of scope, still not attempted, and still the one open architectural question blocking a *complete* Reception flow for every kind of customer.

## 8. Current state summary

| Layer | Ready? |
|---|---|
| Backend: Booking creation for a linked customer | **YES** - endpoint built, tested, tenant-isolated |
| Backend: Service/Specialist/Availability APIs | **YES** - all already existed, unchanged, sufficient |
| Backend: Walk-in (unlinked) booking | **NO** - explicitly deferred, needs its own domain decision |
| Owner App: Customer selection | **YES** - real backend data |
| Owner App: Customer creation (inline, in-wizard) | **NO** - exists only on a separate page |
| Owner App: Service/Specialist selection | **NO** - local SQLite only |
| Owner App: Calendar/Availability | **NO** - local SQLite only, structurally simpler than backend, would crash for a real specialist |
| Owner App: Booking creation (wired to the new endpoint) | **NO** - `BackendBookingRepository.CreateBookingAsync` still throws unconditionally; nothing calls the new endpoint yet |

## 9. Missing APIs

**None on the backend.** Every endpoint the Owner App integration needs already exists: Customer CRM (done), the new owner-authorized booking endpoint (done), Service/Category CRUD, Specialist CRUD, Working Hours, Specialist Schedule (weekly availability/overrides/leaves/blocks), and `available-slots`. The only backend gap remaining is the walk-in domain decision (§7), which is a redesign of an existing concept, not a missing endpoint.

## 10. Required implementation phases (proposed, not started)

1. **`BackendServiceRepository`** - proven pattern; needs the `ServiceCategory` enum→entity model change first (§3) or a documented lossy interim mapping.
2. **`BackendSpecialistRepository`** - proven pattern; Title/Email/Phone map to empty for backend-sourced specialists (§4), same "honest, not fabricated" precedent as `Customer.Notes`.
3. **`BackendCalendarRepository`** - replaces local slot generation entirely with `GET .../available-slots`; do not attempt to map backend Overrides/Leaves/Blocks into the local `WorkingSchedule.Breaks` shape, they don't fit (§5).
4. **Wire `BookingWorkflowService.CreateBookingAsync` to the new endpoint** - replace the `IBookingCommandService.CreateBookingAsync` call (which hits `BackendBookingRepository`'s always-throwing method) with a call through the new `POST /api/v1/salons/{salonId}/bookings` path for owner/reception-initiated bookings specifically; the customer's own self-service creation path (if the Owner App ever exposes one) would still need the original endpoint.
5. **Surface link-status in the Customer picker** - add a "has linked account" indicator to `WorkflowCustomerOptionDto`/the Wizard's Customer step, so Reception sees which customers are actually bookable *before* reaching the final Create step and hitting a 409.
6. **Add the "Create New Customer" branch inside the Wizard** - small UI addition, no backend dependency, closes the target flow's explicit "Select Customer or Create Customer" fork.
7. **Verify Customer Timeline + Confirmation** - expected to need zero new code, same finding as v1 Phase 4: `GetCustomerTimelineUseCase` already merges booking events automatically; already proven end-to-end by `ReceptionBookingFlowIntegrationTest`'s own timeline assertion.

Phases 1-3 are independent of each other and of Phase 4, and could proceed in parallel; Phase 4 depends on at least Phase 1 and 2 existing (a booking needs a real service/specialist id) and ideally Phase 3 (so the date/time step shows real availability, not fixed 30-minute local slots); Phase 5 depends on Phase 4 landing first (nothing to flag as unbookable until the real endpoint is wired); Phase 6 is independent and can happen any time.

## 11. Risks

- **Service `ServiceCategory` enum→entity change** is the one Owner App model change in this pass that is not purely additive - existing screens/filters that switch on the 6 fixed enum values would need to be re-examined, not just a new repository.
- **Calendar swap is a behavior change**, same category already accepted for Customers/Specialists/Services in Sprint 6 - real backend availability will differ from whatever local `WorkingSchedule` seed data exists today (which itself throws for an empty schedule, so there may be little real behavior to compare against for backend-sourced specialists).
- **Walk-in gap remains user-visible**: without Phase 5 (link-status indicator), Reception can select any customer, fill out the whole wizard, and only discover at the final step that an unlinked walk-in cannot be booked - a late, avoidable failure once the picker itself could show this upfront.
- **No distinct Reception/Staff identity**, unchanged from v1 - "Reception" still means "authenticated as the owner."

## 12. Recommendation

Proceed with Phases 1-3 (Service/Specialist/Calendar backend repositories) in parallel - each is now a well-proven, low-risk pattern with zero backend dependency, and Phase 1 additionally requires a small, explicit decision on the `ServiceCategory` model change before or during implementation. Once at least Service and Specialist are backend-connected, proceed to Phase 4 (wire the Wizard to the new booking endpoint) - the highest-value step, since it is the one that actually makes Reception bookings real. Do Phase 5 (link-status indicator) immediately alongside or right after Phase 4, before this ships to real reception use — shipping Phase 4 without it means every walk-in selection silently wastes the receptionist's time through five wizard steps before failing. Phase 6 (inline customer creation) and full walk-in support (the deferred domain decision) can both follow as independent, lower-urgency follow-ups.

**No code was written for this audit.** Awaiting a decision on implementation order (and the `ServiceCategory` model question specifically) before any implementation proceeds.
