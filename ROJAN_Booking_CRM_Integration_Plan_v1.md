# ROJAN Owner App — Real Booking & Customer CRM Integration Plan v1

**Priority:** P0
**Status:** Audit complete. No code has been modified. Awaiting approval before implementation.

---

## Executive summary

**Task 1 (Booking) is feasible and low-technical-risk** — `ROJAN_Backend` already has a complete, production-shaped booking engine (Postgres, concurrency-safe, paginated). The Owner App's Booking module is already cleanly layered (Domain → Application → Infrastructure → Presentation, zero UI-layer API calls) and there's a proven swap pattern already in production use (`BackendDashboardRepository`). The work is real but mechanical, with a handful of genuine model mismatches to resolve (below).

**Task 2 (Customer CRM) cannot proceed as scoped.** `ROJAN_Backend` has **no Customer/CRM domain at all** — no entity, no table, no repository, no endpoint. The only related concept is `User` with `role = CUSTOMER`: a bare auth identity (id, email?, phone?, fullName, role) with none of the Owner App's CRM fields (Company, LifetimeValue, Status lifecycle, Notes, Tags, Activity timeline). This is not a "connect to real data" task for Customer — it's new backend scope that doesn't exist yet. **This needs your decision before any Customer implementation work is planned in detail** — see §2 and §6.

---

## 1. Current state

### 1.1 Booking

| | Owner App (`ROJAN_Desktop`) | Backend (`ROJAN_Backend`) |
|---|---|---|
| Domain model | `Booking` record: free-text `CustomerId`/`ServiceId`/`SpecialistId` (no FK, no validation), 6-state `BookingStatus` (`Pending, Confirmed, InProgress, Completed, Cancelled, NoShow`), `Price` as a formatted string, `DurationMinutes` | `Booking` aggregate: real `UUID` value classes for every reference, 4-state `BookingStatus` (`PENDING, CONFIRMED, CANCELLED, COMPLETED`), `startTime`/`endTime` (no duration/price field on the wire) |
| Repository | `IBookingRepository` — 2 implementations exist: `FakeBookingRepository` (unreferenced) and `EfBookingRepository` (**active**, SQLite via EF Core) | `BookingRepository` port — `BookingRepositoryAdapter` over Postgres, advisory-lock-protected against double-booking |
| Persistence | SQLite, `%LocalAppData%\RojanDesktop\database\rojan.db`, `DbSet<BookingEntity>`, migration `20260724142355_AddBookingPersistence` — genuinely persists real user input across restarts | Postgres `bookings` table (Flyway `V3__booking_engine_schema.sql`), FK to `salons`/`services`/`specialists`/`users`, `CHECK (start_time < end_time)`, indexes on specialist+time, salon, customer, status |
| API surface | None — `EfBookingRepository` never calls the network | `BookingController` (`/api/v1/bookings/*`), `SalonBookingController` (`/api/v1/salons/{salonId}/bookings`), `AvailabilityController` (`/api/v1/salons/{salonId}/specialists/{specialistId}/available-slots`) — all functional, paginated, Bearer-JWT-authenticated |
| Screens | `BookingPageViewModel` (list/filter/quick-add/status actions/cancel/reschedule), `BookingWizardViewModel` (guided create wizard) — both depend only on Application-layer query/command/workflow services, zero direct API/Infrastructure/Domain reference | — |

**Current data source for Booking: Local (SQLite via `EfBookingRepository`).** Zero backend calls today, despite the backend API being fully functional and ready.

### 1.2 Customer

| | Owner App (`ROJAN_Desktop`) | Backend (`ROJAN_Backend`) |
|---|---|---|
| Domain model | `Customer` record — CRM-shaped: `Company`, `LifetimeValue` (formatted string), `CustomerStatus` (`Lead, Prospect, Active, Vip, Inactive, Churned`), `Notes`, plus related `CustomerNote`/`CustomerTag`/`CustomerActivity` aggregates | **Does not exist.** Only `User` with `role = CUSTOMER`: `id, email?, passwordHash?, phoneNumber?, fullName, role, active` — no company, no lifetime value, no status lifecycle, no notes, no tags, no activity |
| Repository | `ICustomerRepository` — 2 implementations: `FakeCustomerRepository` (unreferenced), `EfCustomerRepository` (**active**, SQLite) | N/A |
| Persistence | SQLite, `DbSet<CustomerEntity>`/`CustomerNoteEntity`/`CustomerTagEntity`/`CustomerActivityEntity`, migration `20260724133056_AddCustomerPersistence` | N/A |
| API surface | A partial, existing sync-queue mechanism (`CustomerCommandServiceSyncProducer` → `ISyncQueueService` → best-effort `POST sync/operations`) attempts to push Create/Update writes toward *some* backend endpoint, but this predates and is orthogonal to a real CRM API — `sync/operations` is a generic sync envelope, not a Customer-shaped endpoint | **No Customer endpoint exists.** `GET /api/v1/users/me` returns only the caller's own profile — there is no `GET /api/v1/users/{id}` or batch lookup, so even a booking's `customerId` (a raw UUID) cannot be resolved to a name/phone/email today |
| Screens | `CustomerPageViewModel` (list/search/create) + `CustomerProfileViewModel` — a full "Customer 360": notes, tags, **activity timeline** (already exists, backed by `CustomerActivity`), **booking history** (already exists — composed by filtering `IBookingQueryService.GetBookingsAsync()` where `CustomerId` matches, split into upcoming/past), customer scoring/loyalty/engagement (`CustomerInsightsCalculator`) | — |

**Current data source for Customer: Local (SQLite via `EfCustomerRepository`).** The "booking history" and "timeline" flows this task asks about **already exist in the Owner App today** — they are not new UI to build, only currently powered by local fake/local data instead of a real backend that doesn't yet have anywhere to source them from.

### 1.3 A note on which backend is the actual target

This workspace contains two backend-shaped projects. `ROJAN_Backend` (Kotlin/Spring Boot, this session's other working directory) is the confirmed real integration target — the Owner App's existing Auth and Dashboard integrations both have doc comments explicitly stating they match `ROJAN_Backend`'s DTOs field-for-field, and `IApiEnvironmentService`'s dev default port (`8080`) matches `ROJAN_Backend`'s Spring Boot default. There is a second, unrelated ASP.NET Core project (`Rojan.Server`) living inside the `ROJAN_Desktop` repo with its own Customer/Auth domain — nothing in the current integration touches it, and this plan assumes it is out of scope (flagging this explicitly so it isn't accidentally targeted).

---

## 2. Existing blockers

### Blocking Task 2 (Customer) entirely
1. **No backend Customer/CRM domain exists.** `Company`, `LifetimeValue`, `Status` lifecycle, `Notes`, `Tags`, `Activity` have nowhere to live server-side.
2. **No way to resolve a booking's `customerId` to a name/phone/email.** No `GET /api/v1/users/{id}`, no batch lookup — blocks even a read-only "customer name" column anywhere without new backend work.
3. **No owner-facing "bookings for customer X" endpoint.** `findByCustomerId` exists at the backend's repository layer but only powers the self-service `/bookings/mine` (the *customer's own* view), not an owner/CRM view.

### Blocking Task 1 (Booking) write-path, and partially the read-path
4. **The Owner App has no session-level `salonId` today.** `IEnterpriseContext.CurrentOrganizationId`/`CurrentBranchId` currently come entirely from `FakeOrganizationRepository` (local fake data), not the backend. `SalonBookingController` (the endpoint an owner actually needs) requires `{salonId}` in the path. `GET /api/v1/salons/mine` exists and returns `List<SalonResponse{id, ownerId, name, description?, phone, email?, address, active}>` — this closes the gap, but the Owner App needs new plumbing to call it after login, store the resolved `salonId`, and handle the zero-salons and multi-salon cases (no picker UI exists for this today).
5. **Booking status model mismatch.** Owner App has 6 states (adds `InProgress`, `NoShow`); backend has 4 (`PENDING/CONFIRMED/CANCELLED/COMPLETED`). Per this task's "do not change Backend contracts without approval" instruction, this plan does **not** assume the backend will grow new statuses — `InProgress`/`NoShow` need an explicit decision (see §6).
6. **No price or duration on the backend's wire shape.** `BookingResponse` has `startTime`/`endTime`, no `durationMinutes`, no price field at all. The Owner App's `Booking.Price` (a formatted string) has no backend source today — likely needs to come from a separate `Service` lookup, not confirmed in this audit.
7. **Backend booking-list filtering is minimal**: status + sort-direction only (hardcoded sort by `startTime`). No date-range, specialist, service, or customer filter server-side. The Owner App's `BookingPageViewModel` today supports 6 combinable local filters (search text, customer name, service name, status, date-from, date-to) — most would need to become client-side post-filtering over fetched pages, a real UX/scale tradeoff, not a drop-in.
8. **No pagination contract exists yet on the Owner App side.** `IApiClient` has no query-string helper (`path` is a literal string); a `Contracts.PagedResponse<T>` record matching the backend's `{content, page, size, totalElements, totalPages}` envelope doesn't exist yet and needs to be added.

### Worth flagging, not blocking
9. Owner App's `OrganizationId`/`BranchId` (free-text strings on `Booking`) have no backend equivalent surfaced on `BookingResponse` (only `salonId`) — needs a mapping decision.
10. `CustomerCommandServiceSyncProducer`'s existing sync-queue may become redundant if a real backend-connected `ICustomerRepository` is ever built (direct writes vs. local-then-sync-later) — needs an explicit decision, not silent removal, whenever Customer work is eventually scoped.
11. No architecture test currently enforces "Presentation must go through Application services, not `IApiClient`, per module" — today it's convention only (every existing ViewModel already follows it). Worth adding as a fourth `DependencyDirectionTests` rule alongside this work, optional.
12. **Unconfirmed: is there any real (non-fake, non-test) booking/customer data already sitting in an installed Owner App's local SQLite database that would be lost from view once the repository DI registration swaps to the backend?** If any real users have already used this app pre-integration, a one-time local→backend data migration may be needed before cutover. This audit could not determine that from the codebase alone — needs your input.

---

## 3. API requirements (Task 1 — Booking)

All endpoints below already exist and are functional in `ROJAN_Backend` today (audited directly from source, not assumed).

| Endpoint | Method | Request DTO | Response DTO | Auth | Error handling |
|---|---|---|---|---|---|
| `/api/v1/salons/mine` | GET | — | `List<SalonResponse{id, ownerId, name, description?, phone, email?, address, active}>` | Bearer JWT | — (empty list if the caller owns no salon) |
| `/api/v1/salons/{salonId}/bookings` | GET | query: `page=0, size=20, status?, sortDirection=DESC` | `PagedResponse<BookingResponse>` | Bearer JWT, must be the salon's owner | 404 `SALON_NOT_FOUND`; 403 `ACCESS_DENIED`; 400 `INVALID_ARGUMENT` (bad enum) |
| `/api/v1/bookings/{bookingId}` | GET | — | `BookingResponse` | Bearer JWT, booking's customer or owning salon's owner | 404 `BOOKING_NOT_FOUND`; 403 `ACCESS_DENIED` |
| `/api/v1/bookings/{bookingId}/confirm` | PATCH | — | `BookingResponse` | Bearer JWT, owner only | 404; 403 `ACCESS_DENIED`; 409 `INVALID_BOOKING_STATE` |
| `/api/v1/bookings/{bookingId}/cancel` | PATCH | — | `BookingResponse` | Bearer JWT, customer or owner | 404; 403; 409 `INVALID_BOOKING_STATE` |
| `/api/v1/bookings/{bookingId}/complete` | PATCH | — | `BookingResponse` | Bearer JWT, owner only | 404; 403; 409 `INVALID_BOOKING_STATE` |
| `/api/v1/bookings/{bookingId}/reschedule` | PUT | `RescheduleBookingRequest{newStartTime}` | `BookingResponse` | Bearer JWT, customer or owner | 404; 409 `BOOKING_CONFLICT`; 400 `VALIDATION_FAILED` |
| `/api/v1/bookings` | POST | `CreateBookingRequest{salonId, serviceId, specialistId, startTime, notes?}` (+ optional `Idempotency-Key` header) | 201 `BookingResponse` | Bearer JWT | 404 `SALON_NOT_FOUND`/`SERVICE_NOT_FOUND`/`SPECIALIST_NOT_FOUND`; 409 `BOOKING_CONFLICT`/`IDEMPOTENCY_KEY_CONFLICT`; 400 `VALIDATION_FAILED` |
| `/api/v1/salons/{salonId}/specialists/{specialistId}/available-slots` | GET | query: `serviceId, date, slotIntervalMinutes=15` | `List<TimeSlotResponse{start, end}>` | Bearer JWT | 404 `SPECIALIST_NOT_FOUND`/`SERVICE_NOT_FOUND` |

`BookingResponse` shape: `{id, salonId, serviceId, specialistId, customerId, startTime, endTime, status, notes, createdAt, updatedAt}` — all UUIDs/timestamps, no embedded customer/service/specialist names (matches §2 blocker #6 and the Customer-resolution gap).

`PagedResponse<T>` envelope: `{content, page, size, totalElements, totalPages}` — needs a new matching Owner App `Contracts.PagedResponse<T>` record (doesn't exist yet).

Every 401 (missing/invalid/expired token) is handled uniformly at the Spring Security filter level, before reaching any controller: fixed body `{"errorCode":"AUTH_UNAUTHORIZED","message":"Authentication required"}` — this already round-trips correctly through the Owner App's existing `HttpApiClient` 401-refresh-and-retry pipeline with zero new code needed.

## API requirements (Task 2 — Customer)

**No table to produce.** No Customer endpoint exists in `ROJAN_Backend` to map against. The only adjacent endpoint is `GET /api/v1/users/me` (caller's own profile only — cannot look up any other user), which is insufficient for any of Task 2's required flows.

---

## Required flows — current state vs. target

### Task 1 — Booking

| Flow | Today (Owner App) | Target |
|---|---|---|
| 1. View bookings | `BookingPageViewModel.LoadAsync` reads `EfBookingRepository` via `IBookingQueryService`, fully local, 6 combinable client-side filters | `BackendBookingRepository.GetBookingsAsync` → `GET /api/v1/salons/{salonId}/bookings` (paged); status+sort filter server-side, remaining filters (search text, customer/service name, date range) applied client-side over fetched page(s) — explicit UX tradeoff vs. today's instant full-local search |
| 2. Booking details | Read from the already-loaded local list | `GET /api/v1/bookings/{bookingId}` — note the response carries no customer/service/specialist display names, only UUIDs (§2 blocker #6/#2) |
| 3. Booking status update | `IBookingCommandService.UpdateBookingStatusAsync` against `EfBookingRepository`, validated client-side via `BookingRules` before the write | `PATCH .../confirm` / `.../complete` (owner-only) — server is now the authoritative validator; a 409 `INVALID_BOOKING_STATE` can occur even when local `BookingRules` allowed the attempt (e.g. a concurrent change), which needs new error-surface UX that doesn't exist today |
| 4. Cancel / confirm booking | `IBookingWorkflowService.CancelBookingAsync` (also releases a local Calendar reservation) / `IBookingCommandService` confirm | `PATCH .../cancel` / `.../confirm` — the local Calendar-release side-effect in `BookingWorkflowService` needs a decision: keep it as a local-only UI concern, or is Calendar itself a separate future backend integration (out of scope here, flagging only) |

### Task 2 — Customer

| Flow | Today (Owner App) | Target |
|---|---|---|
| 1. Customer list | `CustomerPageViewModel`, `EfCustomerRepository`, 4 local filters | **Blocked** — no backend Customer list endpoint exists |
| 2. Customer profile | `CustomerProfileViewModel`, full local CRM record | **Blocked** — no backend Customer entity exists to fetch |
| 3. Booking history (per customer) | Already exists — `CustomerProfileQueryService` filters `IBookingQueryService.GetBookingsAsync()` by `CustomerId` client-side | Would work **once Booking itself is backend-connected**, IF an owner-facing "bookings by customerId" capability exists — today it doesn't (§2 blocker #3); the self-service `/bookings/mine` isn't usable here since it returns the *caller's* bookings, not an arbitrary customer's |
| 4. Service history | Not a distinct concept found separately from booking history in either codebase — likely the same underlying data as #3, filtered/labeled differently | Same blockers as #3 |
| 5. Customer timeline | Already exists — `CustomerActivity` records, local only | **Blocked** — no backend concept of a "customer activity" to source this from |

---

## 4. Database considerations

- **Backend (Postgres):** no schema changes needed for Task 1 — the existing `V3__booking_engine_schema.sql` already supports every read/write flow in scope. Task 2 would require an entirely new schema (a `customers` table or CRM-extension-of-`users` design) that does not exist and is not part of this plan until scoped separately.
- **Owner App (SQLite):** once Booking's `IBookingRepository` DI registration swaps from `EfBookingRepository` to a new `BackendBookingRepository`, the local `Bookings` table becomes orphaned (no longer read or written) — recommend explicitly keeping it in the codebase unreferenced, matching this app's own established convention for superseded implementations (`FakeBookingRepository` already sits unreferenced today), rather than deleting it or silently building an offline-cache layer on top of it (a materially larger scope than "connect to real data").
- **No new Owner App EF Core migration is needed for Task 1** — this integration removes a local dependency, it doesn't add one.
- If Customer CRM fields end up staying local in some future hybrid design (identity from backend `User`, CRM fields still local), the existing Customer SQLite schema would likely still be needed, plus a new backend-`UserId` ↔ local-CRM-record link — this is exploratory, not decided, and depends entirely on how Task 2 gets rescoped.

---

## 5. Migration strategy

**Booking:**
1. Read-path first (view list + details) — lowest risk, no write conflicts possible, immediately validates the salonId-resolution and DTO-mapping work.
2. Then status-transition writes (confirm/cancel/complete) — needs new 409-conflict error-surface UX.
3. Then create + reschedule — needs the available-slots endpoint wired in too, since `CreateBookingRequest.startTime` must land on a backend-valid slot.
4. Cutover is a one-line DI change (`AddSingleton<IBookingRepository, EfBookingRepository>()` → `..., BackendBookingRepository>()`), identical to how `BackendDashboardRepository` was introduced — trivially reversible if something goes wrong, since `EfBookingRepository`/`FakeBookingRepository` stay in the codebase unreferenced rather than being deleted.

**Customer:** no migration strategy can be written yet — blocked on the backend-scope decision in §6. Recommend treating Customer as a separate, later phase gated on either (a) new `ROJAN_Backend` Customer/CRM domain work being scoped, approved, and built first, or (b) an explicitly-approved hybrid design (backend identity + locally-persisted CRM fields) that this plan has not evaluated in depth because it wasn't asked to design new backend architecture without approval.

---

## 6. Implementation order (proposed, pending your approval)

1. **Decision needed from you:** descope Customer (Task 2) out of this "Phase 1," since the backend has nothing to integrate against — recommend a separate ticket to design the backend Customer/CRM domain first. This plan does not assume that decision for you.
2. Salon-id resolution: new `GET /api/v1/salons/mine` call after login, new session-state home for the resolved `salonId` (today's `IEnterpriseContext` is entirely fake-data-backed), plus UX for the zero-salon and multi-salon cases.
3. Booking read path: `BackendBookingRepository.GetBookingsAsync`/`GetBookingByIdAsync`, new `Contracts.BookingResponse`/`Contracts.PagedResponse<T>` DTOs, DI swap, verify `BookingPageViewModel` against real data.
4. Booking status-transition writes: confirm/cancel/complete, new 409-conflict UX.
5. Booking create + reschedule, wired to the available-slots endpoint.
6. Explicit decision + documentation on the unsupported-server-side-filters gap (§2 blocker #7) and the `InProgress`/`NoShow` status gap (§2 blocker #5) — both need a product decision, not just an engineering one.
7. (Separate, later ticket) Customer/CRM — once backend scope exists.

## 7. Estimated risks

| Risk | Level | Notes |
|---|---|---|
| Customer/CRM backend gap | **High** | Blocks Task 2 as literally scoped; needs your decision before any further planning |
| Booking status/model mismatches (4 vs 6 states, no price/duration on the wire, salonId plumbing) | Medium | Real design work, not a pure repository swap, but well-understood and scoped above |
| Filter/search UX regression | Medium | Today's fully-local instant search across 6 filters becomes partly client-side-over-a-page once backend-connected; needs a product decision on acceptable UX |
| Conflict/concurrency UX | Medium | Backend enforces booking state/conflict rules authoritatively (409s) in ways the Owner App's local-only `BookingRules` never had to surface to a user before |
| HTTP/auth/error-mapping plumbing | Low | Proven pattern already in production (`BackendDashboardRepository`) - low technical risk to replicate for Booking |
| Local-data-loss risk on cutover | Low–Medium | Unconfirmed whether any installed Owner App already has real (non-seed) local booking/customer data that would need one-time migration before the DI swap - flagged in §2 blocker #12, needs your input |

---

**No code was written or modified during this audit.** Awaiting your review of §2 (blockers) and the Task 2 descoping question in §6 before implementation begins.
