# ROJAN Reception Booking Flow — Audit + Plan v1

**Priority:** P0
**Scope:** Audit only. No code, no architecture, no implementation in this pass.

**Target flow:**
```
Reception / Owner
  -> Select Existing Customer or Create New Customer
  -> Select Service
  -> Select Specialist
  -> Select Date & Time
  -> Create Booking
  -> Update Customer Timeline
  -> Show Confirmation
```

**Headline finding, stated up front:** the Owner App already has a UI that matches this flow almost exactly — `BookingWizardViewModel`/`BookingWizardView`, a 7-step dialog (Customer → Service → Specialist → Date → TimeSlot → Review → Confirmation). It is fully built and wired. **It cannot complete a single real booking today**, for one specific reason: ROJAN_Backend's `POST /api/v1/bookings` always attributes the new booking to the caller's own JWT identity — there is no endpoint that lets an owner/reception create a booking *for* a customer. This is not a missing screen; it's a missing backend capability the whole flow depends on.

---

## 1. Owner App current state

| Area | Implementation found | vs. target flow | Local/fake data | Backend connected |
|---|---|---|---|---|
| Booking creation UI | `BookingWizardViewModel` (Presentation) + `BookingWorkflowService` (Application, `Application/BookingWorkflow/`) — 7-step dialog, orchestrates Customers/Services/Specialists/Calendar/Bookings in one place | **PASS** (shape matches target almost exactly) | No | Partially (see rows below) |
| Booking creation - actual write | `BookingCommandService.CreateBookingAsync` → `IBookingRepository.CreateBookingAsync` → `BackendBookingRepository.CreateBookingAsync` | **FAIL** - `BackendBookingRepository.CreateBookingAsync` unconditionally throws `NotSupportedException` (`src/Rojan.Desktop.Infrastructure/Bookings/BackendBookingRepository.cs`) | No | YES, but create is a hard no-op |
| Customer selection | `_customerQueryService.GetCustomersAsync()` → `ICustomerRepository` → `BackendCustomerRepository` (completed in the prior ticket) | **PASS** | No | **YES** |
| Customer creation | `CustomerPageViewModel.CreateCustomerCommand` exists and works end-to-end against the backend - but it is a **separate page**, not reachable from inside the Wizard | **PARTIAL** - capability exists, not integrated into the flow | No | YES |
| Service selection | `_serviceQueryService.GetServicesAsync()` → `IServiceRepository` → **`EfServiceRepository`** (local SQLite) | **FAIL** (data source) | **YES** | **NO** |
| Specialist selection | `_specialistQueryService.GetSpecialistsAsync()` → `ISpecialistRepository` → **`EfSpecialistRepository`** (local SQLite) | **FAIL** (data source) | **YES** | **NO** |
| Date & time / Calendar | `_calendarQueryService.GetDailyAvailabilityAsync()` → `ICalendarRepository` → **`EfCalendarRepository`** (local SQLite), fixed 30-minute slots, no seed/authoring path (throws `InvalidOperationException` for a specialist with zero `WorkingSchedule` rows - a known pre-existing gap per that repository's own DI comment) | **FAIL** (data source, and disconnected from the real `AvailabilityController`) | **YES** | **NO** |
| Booking-slot reservation | `ICalendarCommandService.ReserveSlotAsync`/`ReleaseSlotAsync`, local only, with hand-rolled compensation (reserve-then-create, release-on-failure) since there's no cross-repository transaction | **N/A locally** - correct logic, wrong data source | YES | NO |
| Customer Timeline update | Not a separate write anywhere in the Owner App - ROJAN_Backend's `GetCustomerTimelineUseCase` merges `BOOKING_CREATED`/`BOOKING_CONFIRMED`/etc. automatically, server-side, from `Customer.userId`-linked bookings | **Already correct, once bookings exist** - no Owner App work needed here | No | YES (server-side, automatic) |
| Confirmation screen | `BookingWizardStep.Confirmation` + `BookingConfirmationDto`, fully built | **PASS** (UI), blocked only by the create step above | No | N/A |

**One design gap independent of backend connectivity:** the Wizard's Customer step is pick-from-list only — there is no "Create New Customer" affordance inside the wizard itself, despite the target flow explicitly requiring it as a branch. A receptionist must exit the wizard, use the separate Customer page to create the record, then re-open the wizard and find them in the list.

## 2. Backend capability audit

Every endpoint below is `Authentication: Bearer JWT (required)` unless noted; `Tenant scope` describes how cross-salon access is prevented.

### Booking

| Endpoint | Method | Request | Response | Auth | Tenant scope |
|---|---|---|---|---|---|
| `/api/v1/bookings` | POST | `CreateBookingRequest(salonId, serviceId, specialistId, startTime, notes?)` | `BookingResponse` (201) | Bearer, any authenticated user | **customerId is always the caller's own JWT `sub` - no field to specify a different customer.** Idempotency-Key header supported. |
| `/api/v1/bookings/mine` | GET | paged | `PagedResponse<BookingResponse>` | Bearer | Caller's own bookings only |
| `/api/v1/bookings/{id}` | GET | - | `BookingResponse` | Bearer | Booking's own customer or the owning salon's owner |
| `/api/v1/bookings/{id}/confirm` | PATCH | - | `BookingResponse` | Bearer | Salon owner only |
| `/api/v1/bookings/{id}/cancel` | PATCH | - | `BookingResponse` | Bearer | Booking's own customer or salon owner |
| `/api/v1/bookings/{id}/complete` | PATCH | - | `BookingResponse` | Bearer | Salon owner only |
| `/api/v1/bookings/{id}/reschedule` | PUT | `RescheduleBookingRequest(newStartTime)` | `BookingResponse` | Bearer | Booking's own customer or salon owner |
| `/api/v1/salons/{salonId}/bookings` | GET | paged, `status?` | `PagedResponse<BookingResponse>` | Bearer | Salon owner only (`salon.ownerId == callerId`) |
| `/api/v1/salons/{salonId}/specialists/{specialistId}/available-slots` | GET | `serviceId`, `date`, `slotIntervalMinutes?` | `List<TimeSlotResponse>` | Bearer | Not owner-restricted - any authenticated caller can query availability |

### Customer CRM (already integrated - listed for completeness)

`/api/v1/salons/{salonId}/customers` (+ `/{id}`, `/{id}/notes`, `/{id}/tags`, `/{id}/timeline`, `/{id}/bookings`) - full CRUD + notes/tags/timeline, owner-only, salon-scoped. See `ROJAN_Customer_CRM_Implementation_Report_v1.md` and `ROJAN_Owner_App_Customer_CRM_Integration_Report_v1.md` for the full contract - unchanged by this audit.

### Service / Service Category

| Endpoint | Method | Request | Response | Auth | Tenant scope |
|---|---|---|---|---|---|
| `/api/v1/salons/{salonId}/categories` | POST/GET | `CreateServiceCategoryRequest` | `ServiceCategoryResponse` / `List<...>` | POST: owner only. GET: any authenticated caller | Path-scoped |
| `/api/v1/salons/{salonId}/categories/{id}` | GET/PUT/DELETE | `UpdateServiceCategoryRequest` | `ServiceCategoryResponse` | PUT/DELETE: owner only | Path-scoped |
| `/api/v1/salons/{salonId}/categories/{categoryId}/services` | POST/GET | `CreateServiceRequest` | `ServiceResponse` / `List<...>` | POST: owner only. GET: any authenticated caller | Path-scoped |
| `.../services/{serviceId}` | GET/PUT/DELETE | `UpdateServiceRequest` | `ServiceResponse` | PUT/DELETE: owner only | Path-scoped |

### Specialist

| Endpoint | Method | Request | Response | Auth | Tenant scope |
|---|---|---|---|---|---|
| `/api/v1/salons/{salonId}/specialists` | POST/GET | `CreateSpecialistRequest(userId?, displayName, bio?, photoUrl?)` | `SpecialistResponse` / `List<...>` | POST: owner only. GET: any authenticated caller | Path-scoped |
| `.../specialists/{id}` | GET/PUT/DELETE | `UpdateSpecialistRequest` | `SpecialistResponse` | PUT/DELETE: owner only | Path-scoped |

### Availability / Schedule

| Endpoint | Method | Request | Response | Auth | Tenant scope |
|---|---|---|---|---|---|
| `/api/v1/salons/{salonId}/working-hours/{dayOfWeek}` | PUT/GET/DELETE | `SetWorkingHoursRequest(intervals)` | `WorkingHoursResponse` | PUT/DELETE: owner only. GET: any | Path-scoped |
| `/api/v1/salons/{salonId}/working-hours` | GET | - | `List<WorkingHoursResponse>` | any | Path-scoped |
| `.../specialists/{id}/schedule/weekly-availability[/{dayOfWeek}]` | PUT/GET/DELETE | `SetWeeklyAvailabilityRequest(intervals)` | `WeeklyAvailabilityResponse` | write: owner only | Path-scoped |
| `.../specialists/{id}/schedule/overrides[/{date}\|/{overrideId}]` | PUT/GET/DELETE | `SetScheduleOverrideRequest` | `ScheduleOverrideResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `.../specialists/{id}/schedule/leaves[/{leaveId}]` | POST/GET/DELETE | `CreateLeaveRequest` | `LeaveResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `.../specialists/{id}/schedule/blocks[/{blockId}]` | POST/GET/DELETE | `CreateBlockRequest` | `BlockResponse` | write: owner only; `reason` redacted for non-owners | Path-scoped |
| `/api/v1/salons/{salonId}/specialists/{id}/available-slots` | GET | `serviceId`, `date`, `slotIntervalMinutes?` | `List<TimeSlotResponse>` | any authenticated | Path-scoped |

The backend already computes real bookable slots server-side (`GetAvailableSlotsUseCase`), combining working hours, weekly availability, overrides, leaves, blocks, and existing bookings — a materially more complete availability engine than the Owner App's local, fixed-30-minute-slot `CalendarQueryService`.

## 3. Domain compatibility check

| Concern | Owner App (`Domain.Bookings.Booking`) | ROJAN_Backend (`Booking`) | Issue |
|---|---|---|---|
| Customer reference | `CustomerId: string` — set from a picked `CustomerDto.Id` (CRM id) in the Wizard, or free text in the quick-add form | `customerId: UserId` — strictly the account that made the booking | **ID mapping issue.** These are different id spaces. A CRM `Customer.Id` is never a valid backend `customerId`, and there is no backend field to carry it even if it were. |
| Status | 6 values: `Pending, Confirmed, InProgress, Completed, NoShow, Cancelled` | 4 values: `PENDING, CONFIRMED, COMPLETED, CANCELLED` | **Status mismatch**, already known and handled: `IBookingRepository.SupportsInProgressAndNoShowStatuses` is `false` for `BackendBookingRepository`; `BookingPageViewModel` disables the corresponding actions. No new work needed, just re-confirmed still accurate. |
| Salon reference | None — uses `OrganizationId`/`BranchId` (local-only concept) | `salonId: SalonId` (required) | **Missing field**, already worked around: `BackendBookingRepository` stamps `OrganizationId`/`BranchId` from the current session so the existing scoping filter is a harmless no-op; the real `salonId` is resolved separately via `ISalonContextService` for every call. No new issue. |
| End time | Not stored — `DurationMinutes: int` only, end computed at the UI/mapping layer | `endTime: LocalDateTime`, computed server-side from `Service.durationMinutes` at creation | Compatible once Service data is real: the Wizard already resolves `DurationMinutes` from the selected `WorkflowServiceOptionDto`, so no gap once Service selection is backend-connected. |
| Notes | `Notes: string` | `notes: String?` | Compatible, direct mapping. |
| Creation identity | N/A - Owner App has no concept of "who is booking on whose behalf" | N/A - the backend assumes customer == caller, always | **Missing capability, not just a missing field.** See §4 — this is the actual blocker. |

## 4. Walk-in customer flow

| Question | Answer | Why |
|---|---|---|
| Can create booking? | **NO** | `POST /api/v1/bookings` always sets `customerId = currentUserResolver.resolve(principal)` — the caller's own identity. There is no request field, no alternate endpoint, and no owner-authorized "create for customer X" path anywhere in `BookingController`/`SalonBookingController`. This is true for *every* customer, not just unlinked walk-ins — the Reception Booking Flow as a whole has no backend endpoint to call at the "Create Booking" step, full stop. |
| Can appear in timeline? | **Partially** | `GetCustomerTimelineUseCase` merges booking events (`BOOKING_CREATED`, etc.) only via `Customer.userId` when linked (see `ROJAN_Customer_CRM_Implementation_Report_v1.md` §6.1/§6.2). A walk-in with no linked account can still accumulate notes/tags/status-change entries (those work today), but never a booking entry, because no booking can ever be created for them in the first place. |
| Can be selected by receptionist? | **YES, but misleadingly** | The Wizard's Customer step already lists every CRM customer (linked or not) via `BackendCustomerRepository.GetCustomersAsync()`. Selecting a walk-in and completing the wizard today either throws (once Create is wired to actually call the backend and it 404s/misbehaves) or — if naively "fixed" by silently substituting the logged-in owner's own identity as the booking's customer — would **misattribute the booking to the owner**, a correctness bug worse than a visible failure. |

This confirms the finding already flagged (but left unresolved) in `ROJAN_Customer_CRM_Implementation_Report_v1.md` §6.1 ("Walk-in customers still can't have real booking history") and `ROJAN_Owner_App_Customer_CRM_Integration_Plan_v1.md`: it was deferred there as out of scope for Customer CRM; this audit confirms it is now the hard blocker for Reception Booking Flow specifically, not a cosmetic gap.

## 5. Multi-tenant rules

Verified directly against every controller read in §2:

```
Owner (JWT sub, no salon claim)
  -> Salon.ownerId == callerId          (checked per request, every write endpoint)
  -> Customer.salonId == {path salonId} (CustomerController, confirmed no leak - integration-tested)
  -> Booking.salonId == {path salonId}  (SalonBookingController.list)
  -> Booking reachable from Customer only via Customer.userId (GetCustomerBookingsUseCase)
```

- No cross-salon leak found in any endpoint audited — every owner-only write checks `salon.ownerId == callerId`; every path-scoped read filters or 404s on a mismatched salon in the path (`findServiceOrThrow`/`findSpecialistOrThrow`/`findCategoryOrThrow` pattern, consistent everywhere).
- **Role gap, worth flagging even though out of scope to fix now:** ROJAN_Backend's `UserRole` enum has exactly three values — `CUSTOMER`, `MANAGER`, `SPECIALIST`. There is no `RECEPTION`/`STAFF` role, and every authorization check in this codebase is **ownership-based** (`salon.ownerId == callerId`), never role-based. Today, "Reception" in the target flow's own actor diagram can only mean *the owner's own login, used at a front desk* — there is no way to give a receptionist a distinct, audit-attributable identity without either (a) accepting shared-credential use of the owner account, or (b) a real backend role/permission model that does not exist yet. This does not block building the flow (the owner can be Reception), but it is a real gap between the ticket's own actor model and what the backend can authorize.
- **No salon-switcher UI**: `ISalonContextService.GetSalonIdAsync()` always resolves to "the first salon the backend returns" for an owner with multiple salons — a pre-existing, documented limitation (`BackendBookingRepository`'s own doc comment) that would apply identically to a multi-salon Reception flow.

## 6. Architecture gaps summary

1. **No backend endpoint to create a booking on behalf of a different customer.** (Blocker — see §4.) This is a Booking-domain change, not additive plumbing: `Booking.customerId` is a non-null `UserId` today; a walk-in has no `UserId` to put there.
2. **No `BackendCalendarRepository`** — Owner App availability is entirely local/fake (`EfCalendarRepository`), disconnected from the backend's real, more complete `AvailabilityController`/`WorkingHoursController`/`SpecialistScheduleController` stack.
3. **No `BackendServiceRepository`/`BackendSpecialistRepository`** — Service and Specialist selection both read local SQLite data (`EfServiceRepository`/`EfSpecialistRepository`), never the real backend catalog. Two salons' catalogs could drift arbitrarily from what a customer sees via mobile OTP or (future) Website Booking.
4. **No "Create New Customer" step inside the Wizard** — a UI-only gap, cheap to close once the rest is real.
5. **No Reception/Staff identity model** — a backend role/authorization gap, explicitly out of scope to fix here but load-bearing for the ticket's own actor diagram.

## 7. Required APIs (net-new or changed, backend)

| Need | Suggested shape | Notes |
|---|---|---|
| Owner-initiated booking creation for a specific customer | New endpoint, e.g. `POST /api/v1/salons/{salonId}/bookings`, owner-only, body includes `customerId` (CRM `Customer.id`) instead of trusting the caller's own identity | The single highest-risk item — touches `CreateBookingUseCase`, `Booking` domain (customerId typing), migrations, and every downstream consumer of `Booking.customerId` (`GetCustomerBookingsUseCase`, `GetCustomerTimelineUseCase`, `CalculateCustomerLifetimeValueUseCase`). Needs its own architecture decision before implementation — same "plan before code" precedent as Customer CRM. |
| Booking-for-walk-in (no linked `User`) | Depends entirely on the decision above — either the new endpoint accepts a CRM `customerId` directly (and `Booking` gains a way to reference a `Customer` without a `UserId`), or walk-ins remain unbookable via Reception until linked | Architecture decision, not an endpoint spec, belongs in a dedicated plan doc. |
| None of Service/Specialist/Availability need new backend endpoints | Existing `ServiceController`/`SpecialistController`/`AvailabilityController`/`WorkingHoursController`/`SpecialistScheduleController` are already complete and sufficient | Only the Owner App side (new `BackendServiceRepository`/`BackendSpecialistRepository`/`BackendCalendarRepository`) needs building — same proven pattern as `BackendBookingRepository`/`BackendCustomerRepository`. |

## 8. Implementation phases (proposed, not started)

1. **Phase 0 — Backend architecture decision + implementation: owner-initiated booking creation.** The one true blocker. Deserves its own short architecture plan (mirroring `ROJAN_Customer_CRM_Architecture_Plan_v1.md`'s precedent) before any code, given it changes `Booking.customerId`'s meaning/typing.
2. **Phase 1 — `BackendServiceRepository` + `BackendSpecialistRepository`.** Lowest risk, proven pattern (3rd/4th repeat of the same `BackendXxxRepository` shape), can start immediately and in parallel with Phase 0.
3. **Phase 2 — `BackendCalendarRepository`.** Replaces local slot generation with real `GET .../available-slots` calls; retire `CalendarQueryService`'s local 30-minute-slot generation for backend-sourced specialists (kept for any offline/local-only path if one is still wanted).
4. **Phase 3 — Wire `BookingWorkflowService.CreateBookingAsync` to the new Phase 0 endpoint**, once it exists; add the "Create New Customer" inline affordance to `BookingWizardViewModel`'s Customer step.
5. **Phase 4 — Re-verify Customer Timeline and Confirmation.** Expected to need zero new code — `GetCustomerTimelineUseCase` already merges booking events automatically once real bookings exist for a linked customer; the Confirmation screen is already fully built. This step is verification, not implementation.

Phases 1-2 do not depend on Phase 0 and could ship first as pure data-source swaps with zero UI change, exactly like the Customer CRM integration; Phase 3 is the only one gated on the backend decision in Phase 0.

## 9. Risks

- **Booking domain change (Phase 0) is invasive by nature** — `Booking.customerId` retyping/redesign touches every existing Booking use case and the schema; needs careful migration planning, not just a new endpoint.
- **Calendar behavior change**: switching from fixed 30-minute local slots to the backend's real, service-duration-aware slot engine is a genuine behavior change for anyone already relying on local Calendar data — same category of "real persistence replaces demo data" change already accepted for Customers/Specialists/Services in Sprint 6.
- **Shared-credential Reception**: without a role model, "Reception" bookings are indistinguishable from "Owner" bookings in any audit trail, until a real staff-identity feature is built (explicitly out of scope here).
- **Multi-salon owners**: Reception flow inherits the existing single-salon assumption; a receptionist at Salon B for an owner who also owns Salon A would silently work against Salon A until a salon switcher exists.

## 10. Recommendation

Do not attempt Phase 3 (wiring the existing Wizard to real writes) before Phase 0 lands — it would either fail loudly (acceptable) or, if rushed, silently misattribute walk-in bookings to the owner (unacceptable). Recommended order: start Phases 1 and 2 immediately in parallel (proven, low-risk, unblock nothing but themselves) while a short, dedicated architecture-decision document is written and approved for Phase 0's `Booking.customerId` change — the same "plan first, get sign-off, then implement" sequence already used successfully for Customer CRM. Once Phase 0's endpoint exists, Phase 3 is a small, mostly-mechanical wiring change against a UI that is already fully built.

**No code was written for this audit.** Awaiting a decision on Phase 0's approach before any implementation planning proceeds further.
