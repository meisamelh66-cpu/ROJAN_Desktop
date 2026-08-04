# ROJAN Owner App — Customer CRM Integration Plan v1

**Scope:** Wire the Owner App's existing Customer screens to the just-completed backend Customer CRM API, following the exact `BackendBookingRepository` pattern already proven in this codebase. Audit + plan only — no code written yet.

---

## 1. Current state (confirmed by direct reading, not assumed)

| | Owner App | Backend |
|---|---|---|
| Data source | `EfCustomerRepository` (local SQLite) — active in DI today | `CustomerController`, real, complete (this session's prior work) |
| Screens | `CustomerPageViewModel` (list/search/create) + `CustomerProfileViewModel` (full 360: notes, tags, timeline, booking history, computed insights) — **all already built, all already consuming exactly the shape a real backend should provide** | — |
| Pattern to reuse | — | `ISalonContextService`/`BackendSalonContextService` (already built for Booking) resolves the salon directly, no new plumbing needed |

Good news up front: `CustomerPageViewModel`/`CustomerProfileViewModel` need **no changes at all** for list/profile/status/booking-history display — same as Booking, they depend only on Application-layer services, and those services' contracts don't need to change shape. The changes are entirely in `CustomerCommandService`/`CustomerProfileQueryService`/`CustomerMapper` (Application layer) and a new `BackendCustomerRepository` (Infrastructure) — plus, per this audit, one place needs a real behavior decision, not just a mapping.

## 2. Clean, direct mappings (no issue)

| Owner App concept | Backend source | Notes |
|---|---|---|
| `GetCustomersAsync()` (no pagination at the port) | `GET /customers`, paged internally, all pages concatenated | Identical pattern to `BackendBookingRepository.GetBookingsAsync()` - already proven |
| `GetCustomerByIdAsync` | `GET /customers/{id}` | - |
| `CreateCustomerAsync` | `POST /customers` | Backend returns 201 with the real id immediately - clean |
| `LifetimeValue: string` | `CustomerResponse.lifetimeValue: BigDecimal` | Format with the same `FormatToman` 1-liner already used in `BackendDashboardRepository`/`BackendBookingRepository` |
| `OrganizationId`/`BranchId` | No backend equivalent (only `salonId`) | Same fix as Booking: stamp from the current `IEnterpriseContext` so the existing Organization/Branch Scoping filter in `CustomerProfileQueryService`/`CustomerQueryService` still passes for backend-sourced records |
| `Customer.Notes: string` (a single vestigial field, separate from the real `CustomerNote` list feature - confirmed unused by the actual Notes UI, which binds `ObservableCollection<CustomerNoteDto>` from `GetNotesAsync`, not this field) | No backend equivalent at all | Map to `string.Empty` for backend-sourced customers - honest, not fabricated, and matches how little this field is actually used today |
| `LastContactedAt: DateTimeOffset` | No backend equivalent | Map from `CustomerResponse.updatedAt` - the closest honest proxy ("last time this record changed"), not literally "last contacted." Flagging this as an approximation, not silently presenting it as exact. |
| Timeline (`GetActivityAsync`) | `GET /customers/{id}/timeline` | Backend already returns a **merged** feed (activities + notes + booking events) - maps directly onto what `GetActivityAsync` is expected to return |

## 3. Three real blockers found - not cosmetic, need a decision before coding

### 3.1 No backend endpoint to re-fetch a customer's notes list

`ICustomerRepository.GetNotesAsync(customerId)` returns real `CustomerNote` objects with real per-note ids - and `CustomerProfileViewModel` calls it fresh after every note is added (`AddNoteAsync` → `LoadAsync()` → `GetNotesAsync` again). **The backend has `POST .../notes` (write) but no `GET .../notes` (list)** - confirmed absent, flagged as a known gap in `ROJAN_Customer_CRM_Implementation_Report_v1.md` §6.4 already. A backend-connected repository has no way to answer "what are this customer's notes" after the first load.

### 3.2 No backend endpoint to re-fetch a customer's tags with their real ids

Same shape, worse consequence: `CustomerResponse` embeds `tags: List<String>` (labels only), but `RemoveTagCommand`/`DELETE .../tags/{tagId}` needs a real tag **id**, which only exists in the response of `POST .../tags` at creation time. **A tag added in an earlier session (or by anyone else) can be displayed but never removed** - there is no way to resolve its id from a fresh profile load.

### 3.3 `CustomerCommandService`'s activity-logging model doesn't match the backend's

This is the one that would silently misbehave rather than obviously fail, which is why it matters most. Today, `CustomerCommandService` (Application layer, unchanged since before this integration) explicitly calls `_repository.AddActivityAsync(...)` after **every** mutation - create ("Customer created"), update ("Customer profile updated"), add-note ("Note added"), add-tag, remove-tag - treating the repository as a dumb store and building the activity trail itself.

The backend does the opposite: `AddCustomerTagUseCase`/`RemoveCustomerTagUseCase`/`UpdateCustomerUseCase` (on a real status change) each write their **own** activity row as a server-side side effect - there is no generic "log an arbitrary activity" endpoint at all. Plugging in a naive `BackendCustomerRepository.AddActivityAsync()` has no correct backend call to make, and if `CustomerCommandService`'s own explicit calls are left in place unchanged:
- Tag-add/tag-remove/status-change would double-log (once server-side automatically, once from the client's now-orphaned call).
- "Customer created"/"Customer profile updated"/"Note added" generic entries have no server-side equivalent at all and would simply vanish or error.

**This can't be fixed in the repository alone - `CustomerCommandService` itself needs to stop calling `AddActivityAsync` for the cases the backend already logs automatically** (tag add/remove, status change), and accept that "Customer created"/"Customer profile updated" as distinct timeline entries won't exist for backend-sourced customers (arguably fine - the backend's design already treats a note appearing in the timeline as sufficient, without a redundant "Note added" entry next to it).

## 4. Recommended resolution

For §3.1/§3.2, the cleanest fix is two small, purely additive backend endpoints:
- `GET /api/v1/salons/{salonId}/customers/{customerId}/notes` → `List<CustomerNoteResponse>`
- `GET /api/v1/salons/{salonId}/customers/{customerId}/tags` → `List<CustomerTagResponse>`

Both are trivial given the existing `CustomerNoteRepository`/`CustomerTagRepository` ports already have `findByCustomerId` - this is a controller-and-nothing-else addition, not new domain/persistence work. **This needs your explicit approval separately, since it's a backend change and this ticket's scope (per the diagram) is the Owner App side** - I have not made this change and will not without confirmation.

If backend changes are out of scope for this pass, the fallback is degrading the UI honestly rather than silently: derive Notes/Tags display-only from the Timeline (which already returns `NOTE`/`TAG_ADDED`/`TAG_REMOVED` entries), accept that tags added before the current session can't be removed via this screen, and disable `RemoveTagCommand` for any tag whose id wasn't captured in this session's own `AddTagAsync` response. I'd rather ship the two small endpoints than ship that degradation, but it's your call.

For §3.3, no backend change is needed - this is purely an `CustomerCommandService` (Owner App Application layer) adjustment: remove its own `AddActivityAsync` calls for tag/status operations (the backend already logs those), and accept that generic create/update/note-added entries won't appear as separate timeline rows for backend-sourced customers.

## 5. Implementation order (once approved)

1. Confirm whether the two small backend endpoints (§4) are approved, or the degraded fallback is preferred.
2. `BackendCustomerRepository` (Infrastructure) - list/get/create/update, reusing `ISalonContextService`, `IEnterpriseContext` stamping, and `FormatToman`, exactly mirroring `BackendBookingRepository`'s shape.
3. Notes/Tags wiring - depends on the §4 decision.
4. Timeline wiring - direct, `GET /timeline` → `GetActivityAsync`.
5. Adjust `CustomerCommandService` to stop double-logging activities the backend already logs (§3.3).
6. DI swap (`EfCustomerRepository` → `BackendCustomerRepository`, left unreferenced, same convention as every prior swap).
7. Tests - unit tests for the repository mapping (mirroring `BackendBookingRepositoryTests`), and an update to any existing `CustomerCommandService` tests affected by the activity-logging change.

**No code has been written for this phase.** Awaiting your decision on §4 before implementation starts.
