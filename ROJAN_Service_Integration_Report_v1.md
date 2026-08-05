# ROJAN Service Integration Report v1

**Scope:** Reception Booking Integration — Phase 1, Service Integration only, per `ROJAN_Reception_Booking_Integration_Audit_v2.md` §3/§10. No Booking creation changes, no Specialist/Calendar work — those remain separate phases.
**Status:** Complete. `dotnet build` — 0 warnings, 0 errors. `dotnet test` — **2175/2175**, 0 failures (2156 baseline + 19 new).

---

## 1. What was built

### `BackendServiceRepository` (Infrastructure, new)
`src/Rojan.Desktop.Infrastructure/Services/BackendServiceRepository.cs` — follows the now-3x-proven `Backend*Repository` pattern (`ISalonContextService` resolves the salon, `IApiClient` for HTTP). Reuses the existing `ServiceResponse`/`ServiceCategoryResponse` wire contracts unchanged — both already existed (added during Booking Integration for `BackendBookingRepository`'s service-name lookup), so no new contract types were needed.

| `IServiceRepository` member | Backend call |
|---|---|
| `GetServicesAsync` | `GET /categories` then `GET /categories/{id}/services` for each — flattened. Unlike `BackendBookingRepository.BuildServiceLookupAsync`'s best-effort lookup (which degrades to an empty map on failure, since it's only cosmetic name resolution), a failure here **throws** — this is the primary catalog read, not an auxiliary lookup. |
| `GetServiceByIdAsync` | Same fetch, filtered by id — there is no flat "get service by id" endpoint on the backend |
| `GetAssignedSpecialistsAsync` | Always returns empty — see §2 |
| `AssignSpecialistAsync` / `UnassignSpecialistAsync` | Always throw `NotSupportedException` — see §2 |

### DI swap
`ServiceCollectionExtensions.cs`: `IServiceRepository` now resolves to `BackendServiceRepository`. `EfServiceRepository`/`FakeServiceRepository` remain in the codebase, unreferenced — same convention as every earlier swap.

## 2. Scope boundary: specialist-assignment writes are not backend-supported (stated explicitly)

`Domain.Services.SpecialistService` (Owner App's own "which specialist can perform which service" record) has **no backend equivalent at all** — `Service` and `Specialist` are fully independent entities on ROJAN_Backend, with no assignment relationship anywhere in its API. Rather than fabricate one:
- `GetAssignedSpecialistsAsync` always returns an empty list — honest, not an error, matching the "empty is a valid result" convention already established for an unlinked Customer's booking history.
- `AssignSpecialistAsync`/`UnassignSpecialistAsync` always throw `NotSupportedException` with a clear message — matching `BackendBookingRepository.CreateBookingAsync`'s exact precedent for "there is no backend call this could ever make." This does not currently block the Reception Booking Flow: the Wizard's Specialist-selection step already shows every active specialist unfiltered, never consulting this assignment relationship.

## 3. Resolving the `ServiceCategory` model mismatch

**The problem** (from `ROJAN_Reception_Booking_Integration_Audit_v2.md` §3): the Owner App's `ServiceCategory` was a closed 6-value C# enum (`Hair, Colour, Nails, Skin, Spa, Consultation`); ROJAN_Backend's category is a real, per-salon, owner-named entity with arbitrary text — an open set. A category named anything outside those six had nowhere to map to.

**Resolution chosen — additive, not a redesign:**
1. Added a 7th enum value, **`ServiceCategory.Other`**, to both `Domain.Services.ServiceCategory` and `Application.Services.ServiceCategory` — the honest fallback bucket for any backend category name that doesn't match one of the five real ones (case-insensitive: `"HAIR"`, `"color"`, `"  Spa  "` all match correctly; `"Barbering"` falls back to `Other`).
2. Added a trailing, optional **`Service.CategoryName: string?`** field (Domain) and its `ServiceDto.CategoryName` mirror (Application) — the backend's real, authoritative category text, carried alongside the best-effort enum classification so **nothing is ever lost**, even when the enum itself had to fall back to `Other`. Always `null` for local/EF-backed data (which has no such concept); always populated for backend-sourced data.

**Why not redesign `ServiceCategory` into a real entity** (the audit's other option): that would have touched every one of the 14 files that already reference the enum — including `ServicePageViewModel`'s category-filter `ComboBox` (which already derives its options from `Enum.GetValues<ServiceCategory>()`) — for a Phase explicitly scoped to "keep UI changes minimal." The chosen approach required **zero Presentation-layer changes**: the new `Other` value automatically appears in the existing filter `ComboBox` with no code change, since that binding already enumerates every value of the type at runtime.

**`ServiceStatus` mismatch** (3-value enum vs. backend's boolean `active`): resolved the same way already accepted for Booking's `InProgress`/`NoShow` gap — `Seasonal` simply never appears for backend-sourced data (`BackendServiceRepository` only ever produces `Active` or `Discontinued`), not a crash, not a new capability flag (nothing in the current UI attempts to *set* a service to `Seasonal`, unlike Booking's status-transition commands).

## 4. What stayed unchanged

- **Presentation layer**: zero changes, confirmed by the build (`ServicePageViewModel` and its XAML untouched).
- **`IServiceRepository`/`IServiceQueryService`/`IServiceCommandService` contracts**: unchanged — `BackendServiceRepository` is a drop-in implementation of the existing port, same as every prior swap.
- **Clean Architecture boundaries**: `BackendServiceRepository` depends on `Application.Api`/`Application.Api.Contracts`/`Application.Salons` and `Domain.Services` only — identical dependency shape to `BackendBookingRepository`/`BackendCustomerRepository`, confirmed by `Rojan.Desktop.ArchitectureTests` passing unchanged (6/6).
- **Every existing positional `Service(...)`/`ServiceDto(...)` call site** — both new fields are trailing optional parameters, so all ~14 pre-existing files that construct these types (including test fixtures across Accounting, Reporting, Intelligence, Search, BookingWorkflow) kept compiling with zero edits.

## 5. Tests

| Suite | New tests | Result |
|---|---|---|
| `BackendServiceRepositoryTests` (Infrastructure) | 15 — category/service fetch-and-flatten, category-name matching (case-insensitive, alternate spelling, `Other` fallback), status mapping, multi-category flattening, salon/categories/services failure paths (all throw, unlike the best-effort Booking lookup), get-by-id found/missing, always-empty assignment read, both assignment writes always throw | ✅ |
| `ServiceQueryServiceTests` (Application) | 2 — `Other` added to the existing category-mapping theory, `CategoryName` null-for-local/present-for-backend pass-through | ✅ |
| `ServiceTests` (Domain) | 1 — `CategoryName` defaults to null when omitted | ✅ |
| `PersistenceDependencyInjectionTests` (Infrastructure) | 1 — `IServiceRepository` resolves to `BackendServiceRepository` through the real composition root | ✅ |

| Full suite | Result |
|---|---|
| `dotnet test RojanDesktop.sln` | **2175/2175**, 0 failures (Domain 454, Application 705, Infrastructure 509, Presentation 456, ArchitectureTests 6, Shell.Tests 45) |

## 6. What this unblocks / what remains

Service selection in the Reception Booking Wizard now reads the real backend catalog — the Wizard's `Services` picker (`BookingWorkflowService.GetBookingOptionsAsync` → `IServiceQueryService.GetServicesAsync`) will show real, backend-sourced services once exercised, with correct duration/price already flowing into `WorkflowServiceOptionDto` unchanged.

Per `ROJAN_Reception_Booking_Integration_Audit_v2.md` §10, still not done, unaffected by this phase:
1. **`BackendSpecialistRepository`** — Specialist selection remains local SQLite.
2. **`BackendCalendarRepository`** — Availability remains local SQLite, structurally simpler than the backend engine.
3. **Wiring `BookingWorkflowService.CreateBookingAsync`** to the new owner-authorized booking endpoint — `BackendBookingRepository.CreateBookingAsync` still throws unconditionally; explicitly out of scope for this ticket ("No Booking creation changes yet").
4. **Catalog authoring** (create/update a service or category from the Owner App) remains unbuilt — `IServiceRepository` never had create/update methods, unchanged by this phase, same pre-existing gap noted in the DI file before this swap.
