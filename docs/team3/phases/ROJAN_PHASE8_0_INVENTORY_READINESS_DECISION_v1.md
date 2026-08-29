# ROJAN AI — TEAM 3 — PHASE 8.0 INVENTORY DOMAIN ACTIVATION ARCHITECTURE REVALIDATION — REPORT v1

**Type:** Audit only. No code modified, no repository created, no DTO created, no UI changed, no
`FakeInventoryRepository` replacement, no permission added, no API connected. `HEAD` (`801cc65` in
`ROJAN_Desktop_team3`) unchanged before and after this task.

**Reference note, disclosed up front, not silently worked around:** two of the four referenced
documents — `ROJAN_INVENTORY_DOMAIN_ARCHITECTURE_DECISION_v1.md` and
`ROJAN_INVENTORY_BACKEND_CONTRACT_v1.md` — **do not exist anywhere in the filesystem**, searched
across every `ROJAN_*` project directory under `C:\AndroidProjects`, not just this repo. The other
two — `ROJAN_PHASE7_3_1_INVENTORY_IMPLEMENTATION_READINESS_AUDIT_v1.md` and
`ROJAN_PHASE7_5_DESKTOP_PRODUCTION_READINESS_FINAL_AUDIT_v1.md` — exist and were read in full. This
report is built on the latter two plus a fresh, independent re-verification against real source
(`ROJAN_Backend`, this repo's own Inventory files), not on the two missing documents' assumed content.

---

## A. Current State

Nothing about Inventory has changed on either side since the referenced Phase 7.3.1 audit — confirmed
by re-checking every one of its load-bearing claims directly this turn, not carried forward unread.
No commit across the entire Phase 7.4 remediation arc (`53090c1`→`801cc65`) touched any Inventory
file — consistent with every scope review in that arc, none of which ever listed an Inventory path as
pending.

---

## B. Backend Gap — Task 1

**Classification: MISSING.**

Re-verified fresh this turn, exhaustively, against `C:\AndroidProjects\ROJAN_Backend` directly (not
inferred from the referenced audit's own conclusion):

```
$ grep -rli "inventory" --include="*.kt" .              (excl. test/build)  → 0 results
$ grep -rli "stocktransaction|productcategory|stocklocation|stockmovement" --include="*.kt" .  → 0 results
$ git log --all --oneline -i --grep="inventory"                             → 0 results, any branch
```

| Layer | Status |
|---|---|
| **Entities** | None — no domain type anywhere in `ROJAN_Backend/domain` |
| **Controllers** | None — no `api` module file references Inventory |
| **Use Cases** | None — no `application` module file references Inventory |
| **API Contracts** | None — no endpoint, no DTO, and (per the reference-note above) no separate contract document exists either |
| **Permissions** | None — `domain/salon/Permission.kt` read in full this turn: 12 members (`MANAGE_SALON`, `MANAGE_MEMBERSHIP`, `MANAGE_CATALOG`, `MANAGE_STAFF`, `MANAGE_SCHEDULE_ALL`, `MANAGE_SCHEDULE_OWN`, `VIEW_CRM`, `MANAGE_CRM`, `MANAGE_BOOKINGS`, `MANAGE_OWN_BOOKINGS`, `VIEW_CUSTOMER_IDENTITY`, `CREATE_CUSTOMER_IDENTITY`, `VIEW_CUSTOMER_BOOKING_HISTORY`) — none named `INVENTORY`/`STOCK`/`PRODUCT` |

**This is not a partial-progress finding — it is a re-confirmed zero.** The backend's current branch
(`feature/manager-rbac-dashboard-fix`) and its 5 most recent commits are all unrelated to Inventory
(public specialist services, booking idempotency, RBAC dashboard fix, reception customer permissions,
OTP serialization).

---

## C. Desktop Readiness — Task 2

Re-confirmed directly against current source this turn:

| Layer | State |
|---|---|
| **Domain** (`Domain/Inventory/`) | Unchanged — 6 aggregate types (`Product`, `ProductCategory`, `Supplier`, `InventoryItem`, `StockTransaction`, `ServiceProductMapping`), `IInventoryRepository` (15 methods), `StockTransactionRules` |
| **Application** (`Application/Inventory/`) | Unchanged — `IProductQueryService`, `IInventoryQueryService`, `IInventoryCommandService`, `InventoryCommandServicePermissionGate` |
| **`FakeInventoryRepository`** | **Confirmed the sole registered implementation**, re-verified this turn: `services.AddSingleton<IInventoryRepository, FakeInventoryRepository>();` is the only registration for that interface anywhere in `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` |
| **ViewModels** | Unchanged — `InventoryPageViewModel`, `InventoryProfileViewModel`, both depending only on the Application interfaces above, never on `FakeInventoryRepository` directly |
| **UI surfaces** | Unchanged — `InventoryPage.xaml(.cs)`, a real, complete Products/Stock/Recent-Transactions surface |

**Confirmed: FakeInventory remains demo only.** No file anywhere in this call chain makes a real HTTP
call, reads a real API base address, or references any `Backend*` type — every read/write terminates
in in-memory state. This was true at Phase 7.3.1 and remains true now.

**One live cross-domain dependency, re-confirmed this turn:** `Accounting.InvoiceCommandService`
constructor-injects `IInventoryCommandService` directly (to decrement stock on invoice creation) —
still a real, working Fake-to-Fake call today (Accounting is itself still `FakeAccountingRepository`-
backed, per the Phase 7.5 domain matrix).

---

## D. Migration Plan — Task 3

Nothing required-before-production has changed since the referenced Phase 7.3.2 plan. Restated,
re-verified, not re-derived:

| Required item | Owner | Status |
|---|---|---|
| Backend entities (`Product`, `InventoryItem`, `StockTransaction`/`StockMovement`, `Supplier`, and the open `StockLocation` question) | Backend/Team 1 | **Missing** |
| API endpoints | Backend/Team 1 | **Missing** |
| DTO contract | Backend/Team 1 | **Missing** |
| Permission matrix (a real `INVENTORY`/`STOCK`/`PRODUCT`-named `Permission` value) | Backend/Team 1 | **Missing** |
| Repository adapter (`BackendInventoryRepository`) | Desktop/Team 3 | **Not started, correctly** — cannot be written correctly against a contract that doesn't exist |

**Two migrations, not one, both blocked on Backend:** (1) the repository swap itself, and (2) a
separate RBAC Backend Authority migration for `InventoryCommandServicePermissionGate` (still on the
legacy local `IPermissionGate`/`Permission.InventoryEdit`, re-confirmed this turn — see Phase 7.5's own
permission-gate sweep, which found this file among the 6 domains still on the legacy mechanism,
consistently with its own still-`Fake` data authority). Migration (2) cannot start until Backend
defines the permission it would migrate *to* — which, per §B, doesn't exist either.

**Open structural question, unresolved, worth restating:** if the real backend Inventory domain tracks
stock per location (`StockLocation`), that is a structural change to `InventoryItem` and the Stock UI,
not a field-mapping exercise — this should be raised with Backend before their contract is finalized,
not discovered mid-adapter-implementation.

---

## E. Authority Review — Task 4

**Confirmed: Inventory ownership is correctly assigned, in both design and code, today.**

- **Backend = Business Authority (once it exists).** `IInventoryRepository`'s own doc comment
  (re-read this turn, unchanged) explicitly states the interface is "dumb" — quantity arithmetic lives
  in Application (`StockTransactionRules`), not Domain, precisely because that logic is expected to
  retire once a real backend computes stock-quantity truth itself. This is the same principle already
  applied to Calendar's now-fully-removed local conflict logic.
- **Desktop = Experience Layer.** Every Inventory ViewModel, page, and Application service exists
  today purely to present and locally simulate a domain Desktop does not, and will not, own the truth
  of. Nothing in this call chain makes an authoritative business decision — `FakeInventoryRepository`
  is explicitly a demo data source, not a decision-maker standing in for one.

This is architecturally identical to how Calendar, Booking, Customer, Specialist, and Service were
each already treated before their own Backend migrations — Desktop was never the authority for any of
them either, even while their Fake/EF-backed implementations were still in place.

---

## F. Risk Register (carried forward, re-verified, not newly discovered)

| # | Risk | Class |
|---|---|---|
| 1 | No backend contract exists at any layer | **P0** — blocks all further Inventory implementation |
| 2 | No backend permission exists for Inventory management | **P0** — blocks the RBAC migration specifically, independent of the data contract |
| 3 | Inventory RBAC still on the legacy local permission system | **P1** — a real, working stopgap today; becomes a real gap the moment Inventory data goes Backend-real without this also migrating |
| 4 | `StockLocation`/multi-location stock may be a structural gap, not a field-mapping exercise | **P1** — should be raised with Backend before their contract is finalized |
| 5 | Accounting's live `InvoiceCommandService` → `IInventoryCommandService` dependency needs re-verification once Inventory goes Backend-real while Accounting may still be Fake | **P2** |
| 6 | `StockTransactionRules.cs` will need retirement once Backend computes stock-quantity truth itself | **P2** |
| 7 | Permission-denied ViewModel state does not exist in either Inventory ViewModel today — cannot be added correctly until the RBAC migration happens (today's legacy `IPermissionGate.Ensure` doesn't throw the distinguishable exception type the pattern needs) | **P2** |

**Nothing new was found this pass** — every risk above was already identified in Phase 7.3.1/7.3.2 and
is restated here, re-verified against current source, not rediscovered.

---

## Roadmap Decision — Task 5

**Recommend: B — Continue another domain.**

- **(A) Start Backend Inventory Implementation** is not Team 3's to begin — Desktop cannot author a
  backend contract, and per §B, nothing exists yet for Desktop to build against. This work belongs to
  Backend/Team 1.
- **(B) Continue another domain** — the correct recommendation. Desktop's own Inventory preparation is
  already complete and was already assessed as the most thorough "Pending" domain in this app at
  Phase 7.3.1; there is no further Desktop-side architecture work that would change once the backend
  contract eventually appears (the migration sequence in §D is already fully specified and ready to
  execute the moment the two blockers clear).
- **(C) Additional architecture work** is not warranted — nothing this pass found changes or extends
  the Phase 7.3.1/7.3.2 plan; re-running that planning work now would not produce new information.

---

## STOP

Audit complete. No implementation performed. Waiting for Backend Contract approval (Team 1/Backend),
same waiting condition as the referenced Phase 7.3.1 audit.
