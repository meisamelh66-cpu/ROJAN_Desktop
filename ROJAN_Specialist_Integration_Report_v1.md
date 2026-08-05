# ROJAN Specialist Integration Report v1

**Scope:** Reception Booking Integration — Phase 2, Specialist Integration only, per approved ticket. No Booking creation, Calendar/Availability, Service integration, or Customer CRM changes.
**Status:** Complete. `dotnet build` — 0 warnings, 0 errors. `dotnet test` — **2188/2188**, 0 failures (2175 baseline + 13 new).

---

## 1. Audit: current Specialist implementation (before this change)

| Component | Finding |
|---|---|
| `Domain.Specialists.Specialist` | `record(Id, FullName, Title, Email, Phone, Status, Bio)` — no Organization/Branch fields at all (confirmed: neither `SpecialistQueryService` nor `SpecialistCommandService` reference `IEnterpriseContext`), unlike Customer/Booking |
| `ISpecialistRepository` | `GetSpecialistsAsync`, `GetSpecialistByIdAsync`, `GetSkillsAsync`, `CreateSpecialistAsync`, `UpdateSpecialistAsync`, `AddSkillAsync`, `RemoveSkillAsync` — a fuller write surface than `IServiceRepository` had (Service had no create/update at all) |
| ViewModels/Screens | `SpecialistPageViewModel`/`SpecialistProfileViewModel` and their views — unaffected by this phase, consume only the existing Application-layer contracts |
| Data source (before) | `EfSpecialistRepository` (local SQLite), registered via `services.AddSingleton<ISpecialistRepository, EfSpecialistRepository>()` |
| Service–Specialist relationship | `Domain.Services.SpecialistService(Id, ServiceId, SpecialistId, SpecialistName)` — an Owner App–only assignment record with free-form, unvalidated ids; `Domain.Services` deliberately does not depend on `Domain.Specialists` (Independence goal, confirmed in that record's own doc comment) |

## 2. Backend API verification (re-confirmed by direct inspection)

| Endpoint | Method | Request | Response | Auth | Tenant scope |
|---|---|---|---|---|---|
| `/api/v1/salons/{salonId}/specialists` | POST | `CreateSpecialistRequest(userId?, displayName, bio?, photoUrl?)` | `SpecialistResponse` (201) | Owner only | Path-scoped |
| `/api/v1/salons/{salonId}/specialists` | GET | — | `List<SpecialistResponse>` (not paginated) | Any authenticated | Path-scoped |
| `/api/v1/salons/{salonId}/specialists/{id}` | GET | — | `SpecialistResponse` | Any authenticated | Path-scoped |
| `/api/v1/salons/{salonId}/specialists/{id}` | PUT | `UpdateSpecialistRequest(displayName, bio?, photoUrl?)` — **no status/active field** | `SpecialistResponse` | Owner only | Path-scoped |
| `/api/v1/salons/{salonId}/specialists/{id}` | DELETE | — | 204 (deactivates) | Owner only | Path-scoped — **not called**, `ISpecialistRepository` has no delete/deactivate method |

`SpecialistResponse(id, salonId, userId?, displayName, bio?, photoUrl?, active, createdAt, updatedAt)`.

## 3. What was built

### `BackendSpecialistRepository` (Infrastructure, new)
`src/Rojan.Desktop.Infrastructure/Specialists/BackendSpecialistRepository.cs` — same shape as `BackendServiceRepository`/`BackendBookingRepository`: `ISalonContextService` resolves the salon, `IApiClient` for HTTP, reuses the already-existing `SpecialistResponse` wire contract unchanged (added during Booking Integration). No Presentation-layer code calls `IApiClient` directly — Presentation only ever reaches `ISpecialistQueryService`/`ISpecialistCommandService`, unchanged by this phase.

### New wire contracts (`Api/Contracts/SpecialistResponse.cs`, extended)
Added `CreateSpecialistRequest(UserId?, DisplayName, Bio?, PhotoUrl?)` and `UpdateSpecialistRequest(DisplayName, Bio?, PhotoUrl?)`, both mirroring ROJAN_Backend's Kotlin DTOs field-for-field. Named identically to the pre-existing `Application.Specialists.CreateSpecialistRequest`/`UpdateSpecialistRequest` (different shape, different namespace) — same safe-by-construction precedent already established for Booking/Customer (`Infrastructure` never imports `Application.Specialists`, so there is no ambiguity).

### DI swap
`ServiceCollectionExtensions.cs`: `ISpecialistRepository` now resolves to `BackendSpecialistRepository`. `EfSpecialistRepository`/`FakeSpecialistRepository` remain in the codebase, unreferenced.

## 4. Model differences — resolved

| Difference | Resolution |
|---|---|
| **Missing fields**: Owner App `Title`/`Email`/`Phone` have no backend equivalent | Map to `string.Empty` for backend-sourced specialists — honest, not fabricated, same precedent as `Customer.Notes` |
| **Missing fields (reverse)**: backend `photoUrl`/`userId` have no Owner App field | **Not added** — nothing in this phase needs them (no UI displays a photo; no feature needs a specialist's linked account). Not invented; if a future phase needs either, they can be added the same additive, trailing-optional-parameter way `Customer.UserId`/`Service.CategoryName` were |
| **ID mapping** | None needed — `Specialist.Id` is already the real backend UUID directly; `Booking.specialistId` already references it identically on both sides (unlike Booking/Customer's `UserId` vs. CRM-`Customer.id` mismatch, there is no second id space here) |
| **Status mapping**: Owner App 3-value enum (`Active, OnLeave, Inactive`) vs. backend boolean `active` | `Active`/`Inactive` map directly; `OnLeave` is never produced for backend-sourced data — same "the gap is a value that's never produced, not a crash" resolution already used for `ServiceStatus.Seasonal` |
| **Status mapping, the write side**: `UpdateSpecialistAsync` receives a full `Specialist` including a possibly-changed `Status`, but `PUT /specialists/{id}` has **no field to change it at all** | See §5 |

## 5. The one real limitation: status changes cannot be fulfilled through `UpdateSpecialistAsync`

ROJAN_Backend's `PUT /specialists/{id}` accepts only `displayName`/`bio`/`photoUrl` — there is no way to change `active` through this endpoint (only `DELETE` deactivates, and `ISpecialistRepository` has no delete method to call it from). Per the instruction not to invent data and to keep behavior explicit:

- `BackendSpecialistRepository.UpdateSpecialistAsync` still sends and applies name/bio changes (an honest partial application — the fields that *can* be updated *are* updated).
- If the caller's requested `Status` genuinely differs from what the backend actually reports back, the method throws `NotSupportedException` with a message naming exactly what couldn't be fulfilled, **after** the name/bio update has already been applied.
- Most calls never hit this: `SpecialistCommandService.UpdateSpecialistAsync` carries the specialist's current, unchanged status through on every edit that isn't itself a status change (confirmed by reading that method — same pattern `CustomerCommandService` already established).

This is verified by `UpdateSpecialistAsync_RequestedStatusChange_ThrowsNotSupportedException` and `UpdateSpecialistAsync_SameStatus_SendsUpdateAndReturnsMappedResponse`.

## 6. Service relationship — audited, not invented

Per the requirement to check the Specialist↔Service relationship: **ROJAN_Backend has no specialist-skill concept**, and (confirmed again in this pass, first found during Service Integration) **no specialist-to-service assignment concept** either — `Specialist` and `Service` are fully independent entities there, with no linking table or field anywhere in the API.

- `GetSkillsAsync` always returns an empty list — honest, not an error.
- `AddSkillAsync`/`RemoveSkillAsync` always throw `NotSupportedException` — there is no backend call either could ever make, same treatment `BackendServiceRepository.AssignSpecialistAsync`/`UnassignSpecialistAsync` already established for the symmetric gap on the Service side.
- This does not block the Reception Booking Wizard: its Specialist-selection step already shows every active specialist unfiltered by any assignment relationship.

## 7. Tests

| Suite | New tests | Result |
|---|---|---|
| `BackendSpecialistRepositoryTests` (Infrastructure) | 12 — field mapping, status mapping (Active/Inactive, no OnLeave), salon/fetch failure paths, get-by-id found/missing, create (null userId/photoUrl), update with unchanged status, update with a requested status change (throws), always-empty skills read, both skill writes always throw | ✅ |
| `PersistenceDependencyInjectionTests` (Infrastructure) | 1 — `ISpecialistRepository` resolves to `BackendSpecialistRepository` through the real composition root | ✅ |

No Domain/Application-layer test additions were needed — unlike Service Integration (which required a new `ServiceCategory.Other` enum value and `CategoryName` field, each with their own mapping tests), **no Domain or Application model change was required for Specialist Integration at all**: `Specialist`/`SpecialistDto`/`SpecialistMapper` are unchanged, byte-for-byte.

| Full suite | Result |
|---|---|
| `dotnet test RojanDesktop.sln` | **2188/2188**, 0 failures (Domain 454, Application 705, Infrastructure 522, Presentation 456, ArchitectureTests 6, Shell.Tests 45) |

## 8. Files changed

**New:**
- `src/Rojan.Desktop.Infrastructure/Specialists/BackendSpecialistRepository.cs`
- `tests/Rojan.Desktop.Infrastructure.Tests/Specialists/BackendSpecialistRepositoryTests.cs`

**Modified (additive only):**
- `src/Rojan.Desktop.Application/Api/Contracts/SpecialistResponse.cs` — added `CreateSpecialistRequest`/`UpdateSpecialistRequest`; existing `SpecialistResponse` untouched.
- `src/Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` — one registration line swapped, `using` adjusted.
- `tests/Rojan.Desktop.Infrastructure.Tests/Persistence/PersistenceDependencyInjectionTests.cs` — one new test added.

**Untouched, confirmed by the build/test run:** `Domain.Specialists.*`, `Application.Specialists.*` (all of it — `SpecialistDto`, `SpecialistMapper`, `SpecialistCommandService`, `SpecialistQueryService`, `SpecialistProfileQueryService`, `SpecialistSearchFilter`), `EfSpecialistRepository`, `FakeSpecialistRepository`, every Presentation ViewModel/View, Booking creation flow, Calendar/Availability, Service integration, Customer CRM.

## 9. Remaining limitations

1. **Specialist status cannot be changed against backend data** through `ISpecialistCommandService.UpdateSpecialistAsync` — see §5. Closing this would require a new backend capability (an `active`/status field on `PUT /specialists/{id}`, or exposing `ISpecialistRepository` to the existing `DELETE` deactivation endpoint) — out of scope for this phase.
2. **No specialist-skill support against backend data** — see §6. Same category of gap as the Service↔Specialist assignment relationship; would need a new backend concept to close.
3. **`photoUrl`/linked-account (`userId`) are read from the backend but not surfaced anywhere** — no Owner App field carries them yet, since nothing currently needs them.

## 10. Readiness for Calendar integration

Specialist selection in the Reception Booking Wizard (`BookingWorkflowService.GetBookingOptionsAsync` → `ISpecialistQueryService.GetSpecialistsAsync`) now reads the real backend catalog, with real ids that are already directly valid `SpecialistId` references on the backend for the `GET .../available-slots` endpoint the next phase needs. Calendar/Availability integration (Phase 3, not started, explicitly out of scope here) can now proceed knowing:
- Specialist ids flowing into a future `BackendCalendarRepository` will already be real backend ids — no id-mapping work needed at that boundary, unlike the Booking/Customer relationship.
- `Domain.Specialists.Specialist` carries no schedule/working-hours data itself (never did) — Calendar integration is a fully separate concern, already correctly modeled as such by `ICalendarRepository`'s own independent port.
