# ROJAN Owner App — Booking Integration Implementation Report v1

**Scope:** Task 1 (Booking) only, per "ROJAN Booking Integration Approval v1" — Customer CRM formally separated into its own backend-design task (see `ROJAN_Booking_CRM_Integration_Plan_v1.md`).
**Status:** Complete. `dotnet build` — **BUILD SUCCESSFUL**, 0 warnings, 0 errors. Full solution test suite: **2,135/2,135 passing**, including all 6 architecture tests (no dependency-direction or ViewModel-testability regressions).

---

## 1. The three design decisions, resolved

### 1.1 Salon context strategy
New `ISalonContextService` (Application) / `BackendSalonContextService` (Infrastructure) — calls `GET /api/v1/salons/mine` once, caches the result for the instance's lifetime (a singleton, same lifetime as `BackendBookingRepository`). If the owner manages more than one salon, the **first one returned is used** — there is no salon-switcher UI in this phase. This is an explicit, documented Phase 1 limitation, not a silent decision: a multi-salon owner will only ever see/manage their first salon's bookings until a switcher is built. Deliberately a small, narrowly-scoped new port rather than extending `IEnterpriseContext` — this is Booking-integration-specific plumbing, not a redesign of the existing Organization/Branch model.

### 1.2 Booking status mapping
| Backend (`ROJAN_Backend`) | Owner App (`Domain.Bookings.BookingStatus`) |
|---|---|
| `PENDING` | `Pending` |
| `CONFIRMED` | `Confirmed` |
| `CANCELLED` | `Cancelled` |
| `COMPLETED` | `Completed` |
| *(none)* | `InProgress` — **unsupported** |
| *(none)* | `NoShow` — **unsupported** |

**Owner App adaptation, not a backend request**: a new `IBookingRepository.SupportsInProgressAndNoShowStatuses` capability flag (`false` for `BackendBookingRepository`, `true` for the local `Fake`/`Ef` implementations) flows through `IBookingCommandService` → `BookingCommandServicePermissionGate` → `BookingPageViewModel`, whose Start/No-Show buttons are now disabled (`CanExecute`) rather than left to fail at the repository call. `BackendBookingRepository.UpdateBookingStatusAsync` also throws a clear `NotSupportedException` for these two (and for `Pending`, since there is no "set back to pending" endpoint either) as a defense-in-depth safety net, independent of the UI guard.

### 1.3 Missing fields: Price and Duration
**Better outcome than the original plan anticipated** — both are resolvable, no backend extension needed:
- **Duration**: computed from `endTime - startTime` on `BookingResponse` itself (both already on the wire) — reflects the *actual reserved duration at booking time*, which is more correct than re-reading the service's *current* configured duration (which could have changed since).
- **Price**: `ROJAN_Backend`'s `ServiceResponse` (fetched anyway for the service name, see §1.4) includes `price: BigDecimal` — resolved as a side effect of name resolution, formatted the same way `BackendDashboardRepository.FormatToman` already does elsewhere in this codebase.

Both are **Owner App adaptation only** (mapping logic in `BackendBookingRepository`) — zero `ROJAN_Backend` changes.

### 1.4 Discovered during implementation, beyond the three named decisions
- **Specialist and Service name resolution** (confirmed with you mid-implementation): Specialist resolves via a direct `GET /api/v1/salons/{salonId}/specialists/{specialistId}`. Service turned out to need a 2-level traversal — there is no flat "get service by id" endpoint on the backend, only `GET /categories` → `GET /categories/{categoryId}/services` — solved with a per-load in-memory salon-wide service map, still zero new backend endpoints. Customer name stays a raw id, as agreed (CRM phase).
- **`POST /api/v1/bookings` is customer-self-service only.** The backend always attributes a new booking to *the calling user* as its customer, with no request field or separate endpoint for an owner to create a booking on behalf of someone else. Calling it from the Owner App's own session would silently make the salon owner the "customer" of every booking created through it — wrong data, not a missing feature. `BackendBookingRepository.CreateBookingAsync` throws a clear, descriptive `NotSupportedException` explaining exactly this, rather than doing that. **This does not block any of the four explicitly-required flows** (view bookings, booking details, status update, cancel/confirm) — Create was never one of them.
- **`IBookingRepository.GetBookingsAsync()` has no pagination parameter at the Domain-port level** — it returns *everything*, and all client-side filtering (`BookingPageViewModel`'s 6 combinable filters) already happens over that full list. `BackendBookingRepository` pages through every backend page internally and returns the concatenated result, which means **zero UX regression** on filtering/search — better than the original integration plan's flagged risk, which assumed server-side filtering limits would force a client-side-over-one-page compromise.
- **`Booking.OrganizationId`/`BranchId` have no backend equivalent** (`BookingResponse` only carries `salonId`), but `BookingQueryService.ScopeToCurrentSession` filters on them. `BackendBookingRepository` stamps every mapped booking with the *current* `IEnterpriseContext.CurrentOrganizationId`/`CurrentBranchId` so that filter becomes a harmless no-op for backend data rather than silently discarding every booking. The real tenant boundary is already enforced correctly by `ISalonContextService` resolving the right `salonId` for every call.
- **`IApiClient` had no PATCH support** — the backend's confirm/cancel/complete endpoints are `PATCH`. Added `PatchAsync<TResponse>(path, cancellationToken)` to `IApiClient`/`HttpApiClient`, the same kind of additive CRUD-completion change that added `PutAsync`/`DeleteAsync` before it (per that interface's own "Sprint 7 Commit 1" doc comment).

## 2. Files changed

### Application (new)
- `Api/Contracts/PagedResponse.cs`, `BookingResponse.cs` (+ `CreateBookingRequest`, `RescheduleBookingRequest`), `SalonResponse.cs`, `SpecialistResponse.cs`, `ServiceResponse.cs` (+ `ServiceCategoryResponse`) — all matching `ROJAN_Backend`'s Kotlin DTOs field-for-field
- `Salons/ISalonContextService.cs`

### Application (modified)
- `Api/IApiClient.cs` — added `PatchAsync`
- `Bookings/IBookingCommandService.cs`, `BookingCommandService.cs`, `BookingCommandServicePermissionGate.cs` — added `SupportsInProgressAndNoShowStatuses` pass-through

### Domain (modified)
- `Bookings/IBookingRepository.cs` — added `SupportsInProgressAndNoShowStatuses`

### Infrastructure (new)
- `Salons/BackendSalonContextService.cs`
- `Bookings/BackendBookingRepository.cs`

### Infrastructure (modified)
- `Api/HttpApiClient.cs` — implemented `PatchAsync`
- `Bookings/FakeBookingRepository.cs`, `Persistence/Bookings/EfBookingRepository.cs` — `SupportsInProgressAndNoShowStatuses => true`
- `DependencyInjection/ServiceCollectionExtensions.cs` — registers `ISalonContextService`, swaps `IBookingRepository` to `BackendBookingRepository` (`EfBookingRepository`/`FakeBookingRepository` stay in the codebase, unreferenced, matching every earlier Fake/Ef→Backend swap)

### Presentation (modified)
- `ViewModels/Bookings/BookingPageViewModel.cs` — Start/No-Show `CanExecute` now also checks `SupportsInProgressAndNoShowStatuses`

### Tests (new)
- `Infrastructure.Tests/Bookings/BackendBookingRepositoryTests.cs` — 13 tests: name/price/duration resolution, graceful fallback on unresolvable service/specialist, full pagination, no-salon failure, get-by-id + 404, all 4 status-transition mappings + the 3 unsupported ones, reschedule, create-always-throws, capability flag
- `Infrastructure.Tests/Salons/BackendSalonContextServiceTests.cs` — 5 tests: single/multi/zero salon, caching (one backend call across repeated resolutions), failure propagation
- `Infrastructure.Tests/Api/HttpApiClientTests.cs` — +1 test for `PatchAsync` (method, no body, deserialized response)

### Tests (modified — fixed to compile/pass against the widened interfaces)
- `Application.Tests/BookingWorkflow/StubBookingCommandService.cs`, `Application.Tests/Bookings/StubBookingRepository.cs`, `Presentation.Tests/Bookings/StubBookingCommandService.cs` — added `SupportsInProgressAndNoShowStatuses`
- `Infrastructure.Tests/Dashboard/BackendDashboardRepositoryTests.cs`, `Infrastructure.Tests/Sync/SyncQueueServiceTests.cs` — added `PatchAsync` stub methods (other `IApiClient` implementers)
- `Infrastructure.Tests/Persistence/PersistenceDependencyInjectionTests.cs` — its `AddInfrastructure()`-only DI test needed `AddApplication()` added too (a real, expected consequence: `BackendBookingRepository` now resolves down to `HttpApiClient`, which needs `IRetryPolicy` — registered by `AddApplication()`, not `AddInfrastructure()` alone) plus a stub `IEnterpriseContext` (registered by Shell in the real app, not by either DI extension method)

*(The working tree also has unrelated uncommitted changes from earlier tickets this session — Mobile OTP Login, Dashboard integration, Settings — none touched by this task; out of scope for this report.)*

## 3. Tests

| Suite | Result |
|---|---|
| `BackendBookingRepositoryTests` (new) | ✅ 13/13 |
| `BackendSalonContextServiceTests` (new) | ✅ 5/5 |
| `HttpApiClientTests` (+1 for PatchAsync) | ✅ |
| Full solution | ✅ **2,135/2,135**, 0 failures — includes `Rojan.Desktop.ArchitectureTests` (6/6): no `Presentation`→`Domain`/`Infrastructure` violations (confirms "No UI layer API calls" held throughout — `BookingPageViewModel` still only ever touches `IBookingQueryService`/`IBookingCommandService`/`IBookingWorkflowService`), and no ViewModel took a forbidden `System.Windows.Threading`/`Controls` dependency |

`dotnet build RojanDesktop.sln` — clean, 0 warnings, 0 errors.

## 4. Required flows — final status

| Flow | Status |
|---|---|
| 1. View bookings | ✅ Real backend data, full pagination handled internally, all 6 existing client-side filters still work unchanged |
| 2. Booking details | ✅ `GET /api/v1/bookings/{id}`, 404 → `null` |
| 3. Booking status update | ✅ Confirm/Cancel/Complete via `PATCH`; Start/No-Show correctly disabled (no backend equivalent) rather than silently broken |
| 4. Cancel/confirm booking | ✅ Both via `PATCH`, correct owner/customer authorization enforced server-side |
| *(Create — not one of the four required flows)* | Implemented, but throws by design — see §1.4 |
| *(Reschedule — not one of the four required flows)* | ✅ Implemented and working (`PUT .../reschedule`) |

## 5. Remaining limitations (explicit, not hidden)

1. **No salon-switcher UI.** A multi-salon owner only ever sees their first salon (§1.1). Needs a follow-up if any real owner actually manages more than one salon.
2. **Start/No-Show are now unreachable via the real backend**, by design (§1.2) — a real product decision may eventually want ROJAN_Backend to grow these two states; not requested or assumed here per "do not change Backend contracts without approval."
3. **Customer name is an unresolved raw id** everywhere a booking is shown — unchanged from the original plan, explicitly deferred to the separated CRM task.
4. **Booking creation is non-functional via the real backend** (§1.4) — both existing creation paths in the Owner App (the free-text quick-add form and the Wizard) would also need real backend-recognized Service/Specialist UUIDs to ever succeed even if the owner-attribution problem were solved, since neither Service nor Specialist is backend-integrated yet either. A genuinely working "owner creates a booking" flow needs backend work (an owner-initiated creation endpoint) that is out of this task's scope.
5. **Service/category data is re-fetched on every single booking-list load**, not cached across loads (a deliberate simplicity choice over premature caching — see `BackendBookingRepository.BuildServiceLookupAsync`'s own doc comment). Worth revisiting if load times become noticeable on a salon with many categories/services.
6. **Timezone handling assumes the Owner App and the salon share one timezone** — `ROJAN_Backend`'s `startTime`/`endTime` are timezone-less (`LocalDateTime`); this mapping treats them as the Owner App's own local wall-clock time, the same assumption `BookingPageViewModel.CreateBookingAsync` already makes elsewhere in this codebase. Not a new gap, but worth knowing if a salon and its owner's desktop ever run in different timezones.
