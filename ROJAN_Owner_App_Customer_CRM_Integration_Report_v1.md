# ROJAN Owner App Customer CRM Integration Report v1

**Scope:** Connect the Owner App's existing Customer screens to the backend Customer CRM API, following the `BackendBookingRepository` pattern, per `ROJAN_Owner_App_Customer_CRM_Integration_Plan_v1.md` §5 (implementation order) and `ROJAN_Customer_CRM_Integration_Preparation_Report_v1.md` (blockers already resolved).

**Status:** Complete. `dotnet build` - 0 warnings, 0 errors. `dotnet test` - **2156/2156**, 0 failures (2135 baseline + 21 new).

---

## 1. What was built

### `BackendCustomerRepository` (Infrastructure, new)
`src/Rojan.Desktop.Infrastructure/Customers/BackendCustomerRepository.cs` - a field-for-field mirror of `BackendBookingRepository`'s shape: `ISalonContextService` resolves the salon, list reads page through `PagedResponse<T>` until exhausted, `IEnterpriseContext` stamps Organization/Branch onto every mapped customer.

| `ICustomerRepository` member | Backend call |
|---|---|
| `GetCustomersAsync` | `GET /customers`, paged, concatenated |
| `GetCustomerByIdAsync` | `GET /customers/{id}` (404 → null) |
| `GetNotesAsync` | `GET /customers/{id}/notes`, re-sorted newest-first client-side |
| `GetTagsAsync` | `GET /customers/{id}/tags`, real server ids, oldest-first |
| `GetActivityAsync` (Timeline) | `GET /customers/{id}/timeline`, paged, synthesized per-entry id |
| `CreateCustomerAsync` | `POST /customers` |
| `UpdateCustomerAsync` | `PATCH /customers/{id}` (full field set, always) |
| `AddNoteAsync` / `AddTagAsync` | `POST .../notes` / `POST .../tags` |
| `RemoveTagAsync` | `DELETE .../tags/{tagId}` |
| `AddActivityAsync` | Throws `NotSupportedException` - no backend equivalent (see §3) |

### `IApiClient` (Application, extended)
Added one new overload: `PatchAsync<TRequest, TResponse>(path, body, ct)`. The existing `PatchAsync<TResponse>` (no body) only covers Booking's parameterless status-transition endpoints; `PATCH /customers/{id}` is a genuine partial update with a JSON body. Implemented in `HttpApiClient`; the 4 existing `IApiClient` test stubs (`BackendBookingRepositoryTests`, `BackendSalonContextServiceTests`, `SyncQueueServiceTests`, `BackendDashboardRepositoryTests`) were each given a one-line `NotSupportedException` implementation, since none of them exercise it.

### Wire contracts (`Application/Api/Contracts`, new)
`CustomerResponse.cs` (+ `CreateCustomerRequest`/`UpdateCustomerRequest`), `CustomerNoteResponse.cs` (+ `AddCustomerNoteRequest`), `CustomerTagResponse.cs` (+ `AddCustomerTagRequest`), `CustomerTimelineEntryResponse.cs` - each mirroring its ROJAN_Backend Kotlin DTO field-for-field, same convention as `BookingResponse.cs`. `Status` stays a raw string, mapped explicitly by the repository (same reasoning as `BookingResponse.Status`).

### DI swap
`ServiceCollectionExtensions.cs`: `ICustomerRepository` now resolves to `BackendCustomerRepository`. `EfCustomerRepository`/`FakeCustomerRepository` remain in the codebase, unreferenced - same convention as every earlier Fake/Ef→Backend swap.

## 2. The one real correctness fix: booking-history linkage

`Domain.Customers.Customer` gained one new field: `UserId` (nullable, trailing, default `null` - every existing positional call site keeps compiling unchanged).

**Why this was necessary, not optional:** ROJAN_Backend's `Booking.customerId` is typed `UserId` - it identifies the *account* that made the booking, not a Customer CRM record. The Customer CRM `Customer.id` is a separate id space entirely, linked to a `User` only via the optional `Customer.userId` field. `CustomerProfileQueryService.BuildBookingSummaryAsync` previously filtered bookings by `booking.CustomerId == customer.Id` - correct for local/EF data (where both happened to be the same synthetic id space) but silently wrong for backend data: every backend-linked customer would have shown an empty booking history regardless of real bookings.

**Fix:** `BackendCustomerRepository` maps `CustomerResponse.UserId` straight through onto `Customer.UserId`. `CustomerProfileQueryService.GetProfileAsync` now calls `BuildBookingSummaryAsync(customer.UserId ?? customerId, ...)` - matches by the linked account id when present, falls back to the customer's own id otherwise. Local/EF data has no `UserId` concept (always null), so the fallback preserves that path's existing behavior exactly unchanged. This is also what makes the walk-in case fall out naturally rather than needing a special case: an unlinked customer's `UserId` is null, so the fallback still matches by `Id`, which no real booking references - an honestly empty summary, matching ROJAN_Backend's own `GetCustomerBookingsUseCase` behavior for the same case.

## 3. Requirement-by-requirement

| Requirement | How it's handled |
|---|---|
| Customer List | `GetCustomersAsync`, paged/concatenated - no Presentation change |
| Customer Profile | `GetCustomerByIdAsync` - no Presentation change |
| Customer Timeline | `GetActivityAsync` via `GET .../timeline`, already newest-first server-side |
| Customer Notes | `GetNotesAsync`/`AddNoteAsync` via the notes endpoints added in the preparation pass |
| Customer Tags | `GetTagsAsync`/`AddTagAsync`/`RemoveTagAsync` via the tags endpoints added in the preparation pass - real server ids resolve correctly even across sessions |
| Customer Booking History | Composed via `CustomerProfileQueryService` over `IBookingQueryService` (unchanged composition site), now correctly linked - see §2 |
| Empty timeline | `GetActivityAsync` returns `[]` for a customer with no notes/tags/status-changes/bookings - covered by `GetActivityAsync_EmptyTimeline_ReturnsEmptyListNotAnError` |
| Walk-in customers without booking history | `UserId` null → booking-summary filter falls back to `Id`, matches nothing, `CustomerBookingSummaryDto.Empty` - covered by `GetProfileAsync_WalkInCustomerWithNoLinkedUserId_HasNoBookingHistory` |
| Lifetime value availability | `CustomerResponse.LifetimeValue` is always present (backend returns zero, never null/missing, for a customer with no completed bookings) - no "unavailable" case exists, only zero, formatted via the same `FormatToman` convention as Booking/Dashboard |

`AddActivityAsync` throws `NotSupportedException` rather than silently no-op-ing: ROJAN_Backend has no generic "log an arbitrary activity" endpoint, and `CustomerCommandService` no longer calls this method for any mutation (per the preparation pass) - a future caller that does reach it gets an honest signal instead of a write that looks successful but never reached the server.

## 4. What stayed unchanged

- **Presentation layer**: zero changes. `CustomerPageViewModel`/`CustomerProfileViewModel` depend only on `CustomerQueryService`/`CustomerProfileQueryService`/`CustomerCommandService`, whose contracts didn't change shape.
- **Clean Architecture boundaries**: `BackendCustomerRepository` depends on `Application.Api`/`Application.Api.Contracts`/`Application.Organizations`/`Application.Salons` and `Domain.Customers` only - the identical dependency shape `BackendBookingRepository` already established, confirmed by `Rojan.Desktop.ArchitectureTests` passing unchanged (6/6).
- **Existing UI behavior**: Notes stay newest-first, Tags stay oldest-first, Timeline stays newest-first - all re-sorted client-side to match `EfCustomerRepository`/`FakeCustomerRepository`'s existing ordering, regardless of what order the backend itself returns them in.

## 5. Tests

| Suite | New tests | Result |
|---|---|---|
| `BackendCustomerRepositoryTests` (Infrastructure) | 17 - list/pagination, status/lifetime-value/UserId mapping, walk-in (null UserId), notes/tags ordering and real ids, empty timeline, merged-timeline pagination, create/update field mapping, note/tag creation with server-authoritative ids, tag removal, `AddActivityAsync` always throws | ✅ |
| `CustomerProfileQueryServiceTests` (Application) | 2 - linked-`UserId` booking match, walk-in-with-no-`UserId` empty history | ✅ |
| `CustomerTests` (Domain) | 1 - `UserId` defaults to null when omitted | ✅ |
| `PersistenceDependencyInjectionTests` (Infrastructure) | 1 - `ICustomerRepository` resolves to `BackendCustomerRepository` through the real composition root | ✅ |

| Full suite | Result |
|---|---|
| `dotnet test RojanDesktop.sln` | **2156/2156**, 0 failures (Domain 453, Application 703, Infrastructure 493, Presentation 456, ArchitectureTests 6, Shell.Tests 45) |

## 6. Known, accepted limitations (unchanged from the approved plan)

- Owner-initiated customer creation via the Owner App writes only local fields the backend accepts (`fullName`/`phoneNumber`/`email`/`company`) - status always starts as `LEAD` server-side, matching `CreateCustomerAsync`'s own doc comment.
- `LastContactedAt` remains an approximation (`CustomerResponse.UpdatedAt`) - flagged, not silently presented as exact, same as the preparation-phase plan documented.
- The vestigial `Customer.Notes` single-string field has no backend equivalent and maps to `string.Empty` for backend-sourced customers - the real Notes feature is the separate `CustomerNote` list, unaffected.
