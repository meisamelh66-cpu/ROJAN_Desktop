# ROJAN AI — TEAM 3 — PHASE 8.5 NAVIGATION BACKSTACK HARDENING — SCOPE REVIEW v1

**Type:** Preparation only. `NavigationService` not modified, no stack limit added, no routing/
ViewModel-lifecycle change, no commit. `HEAD` (`801cc65`) unchanged before and after.

---

## A. Scope

Reference: `ROJAN_PHASE8_4_NAVIGATION_MEMORY_RETENTION_AUDIT_v1.md`, which classified unbounded
`_backStack`/`_forwardStack` growth as **P2** — a confirmed, currently-live retention pattern, not a
hypothetical one, but bounded in per-instance impact and not a crash/correctness risk. This document
prepares an exact, ready-to-execute implementation scope for the "cap the stack depth" mitigation that
report's own §E recommended first — **no code is written here.**

---

## B. File Impact Map — Task 1

**Correction to this task's own layer framing, disclosed rather than force-fit:** `INavigationService`
lives in **Presentation**, not Application — no Application-layer navigation interface or service
exists anywhere in this codebase (confirmed by search). The impact map below reflects the real layering.

| Layer | File | Change needed |
|---|---|---|
| **Presentation** | `src/Rojan.Desktop.Presentation/Navigation/INavigationService.cs` | **None.** Its 5 members (`CanGoBack`, `CanGoForward`, `NavigateTo<T>`, `NavigateTo(descriptor)`, `GoBack`, `GoForward`) fully cover what a bounded implementation still exposes — capping is an internal implementation detail of the concrete class, not a contract change. Confirmed by reading the interface in full this turn |
| **Presentation** | Every `INavigationService` consumer (ViewModels calling `NavigateTo`/`GoBack`/`GoForward`) | **None.** Zero call-site changes anywhere, since the interface is unchanged |
| **Shell** | `src/Rojan.Desktop.Shell/Navigation/NavigationService.cs` | **The only production file requiring a change.** `_backStack`/`_forwardStack` field declarations, `Navigate(viewModel)`, and (for symmetry, if forward-stack capping is also desired) `GoBack`/`GoForward` |
| **Shell.Tests** | `tests/Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs` | **New tests added** (§E) — existing tests in this file are expected to remain valid unchanged (§F) |

**Total production file impact: 1 file.** This is a narrowly-scoped, internally-contained change by
construction — the interface boundary this app already maintains between Presentation (abstraction)
and Shell (concrete implementation) is exactly what keeps this change from touching anything else.

---

## C. Design Proposal — Task 2

### Current: Unbounded BackStack

Plain `Stack<ViewModelBase>` ×2, no capacity limit, no eviction — re-confirmed from Phase 8.4's own
trace, unchanged since.

### Future proposal: Bounded BackStack

**Maximum depth recommendation: 20 entries** (`_backStack`; the same or a smaller value for
`_forwardStack` is a separate, secondary decision — see below). Rationale:
- Large enough that realistic back-navigation usage (a user stepping back a handful of pages) is never
  artificially truncated in practice — 20 sidebar-navigation steps is already a deep session for most
  workflows in this app.
- Small enough to bound worst-case retained memory to a fixed, small multiple of one page's own
  footprint, regardless of how long a session runs or how many total navigations occur.
- Not derived from a measured memory budget (no such budget exists in this codebase today) — offered as
  a reasonable starting point for product/UX sign-off, not a hard-computed number.

**Eviction strategy: FIFO (oldest-first) on push, only when at capacity.** When `Navigate()` would push
`_backStack` past the cap, the oldest entry (the bottom of the stack) is discarded before the new push.

**A real implementation-detail worth flagging now, before code is written:** `System.Collections.Generic.Stack<T>`
has no efficient "remove from the bottom" operation — evicting the oldest entry while still pushing/
popping the newest in O(1) will likely require replacing the bare `Stack<T>` with a structure that
supports both (e.g. a `LinkedList<T>` used as a deque, or a small custom bounded-stack wrapper). This
is a real design decision for the eventual implementation pass, not a blocker for this scope review, but
it's why "just add a cap" is not a one-line change to the existing `Stack<T>` fields as they stand today.

**`_forwardStack` capping — separate, secondary question:** since `_forwardStack` is always cleared
entirely on any fresh `Navigate()` call (unrelated to the cap), it can never grow beyond however many
consecutive `GoBack()` calls a user makes before navigating forward again — in practice already
self-bounding by ordinary usage, unlike `_backStack`. Capping it too is optional hardening, not the
primary fix.

---

## D. UX Impact — Task 3

**Worked scenario, illustrative cap = 3 for legibility** (the recommended production value is 20 — a
smaller number is used here only to make the eviction mechanism visible in a short trace):

```
Navigate A → backStack=[]                _current=A
Navigate B → backStack=[A]               _current=B
Navigate C → backStack=[A,B]             _current=C
Navigate D → backStack=[A,B,C]  (at cap) _current=D
Navigate E → EVICT A (oldest) first, then push D
             backStack=[B,C,D]           _current=E
```

**When the limit is reached:**
- **The oldest page (A)** is silently evicted from `_backStack` the moment a new push would exceed the
  cap — its `ViewModelBase` instance is no longer referenced by `NavigationService` and becomes eligible
  for garbage collection (subject to nothing else holding it, matching Phase 8.3's own lifetime
  reasoning for this app's ViewModels generally).
- **GoBack behavior:** from `E`, repeated `GoBack()` calls correctly walk `D → C → B`, then
  `CanGoBack` becomes `false` — **the user cannot return to `A` any more; it is permanently
  unreachable via Back**, not merely deferred. This is the real, user-facing trade-off this hardening
  introduces, and should be stated plainly, not glossed over: bounded memory is bought at the cost of
  bounded history depth.
- **Forward navigation:** unaffected in shape — `GoForward()` after any `GoBack()` in this scenario
  still walks back up through whatever is in `_forwardStack` exactly as it does today; capping
  `_backStack` doesn't change `GoForward`'s own correctness, only how far back `GoBack` can ever reach.

**Overall UX impact assessment:** low. A 20-entry cap (§C) is deep enough that ordinary users are very
unlikely to ever notice the eviction boundary; the illustrative cap-3 trace above exists only to make
the mechanism legible for this review, not to represent the recommended real-world experience.

---

## E. Test Strategy — Task 4

All tests below extend `NavigationServiceTests.cs`, matching its existing style (confirmed: the file
already tests `CanGoBack`/`CanGoForward`/permission-gated `NavigateTo<T>` behavior in the same shape
these would follow).

**Navigation:**
- *Stack limit respected* — `Navigate_ExceedsMaxDepth_BackStackNeverExceedsCap`: navigate past the cap
  repeatedly, assert the internal count (via a test-visible seam, e.g. an internal `BackStackDepth`
  property analogous to the existing `CanGoBack`) never exceeds the configured maximum.
- *Old entries released* — `Navigate_ExceedsMaxDepth_OldestViewModelBecomesCollectible`: capture a
  `WeakReference` to the ViewModel expected to be evicted, navigate past the cap, force a GC, assert
  the weak reference's `IsAlive` is `false` — proves eviction actually drops the reference, not just
  that the count stays bounded.
- *GoBack still works* — `GoBack_AfterEviction_WalksRemainingEntriesThenStops`: reproduce the §D trace
  exactly, assert `GoBack()` correctly reaches `D`, `C`, `B` in order and `CanGoBack` becomes `false`
  after — never throws, never reaches the evicted `A`.
- *Forward navigation works* — `GoForward_AfterGoBack_StillRestoresSkippedEntry`: confirm `GoForward`'s
  existing, already-tested behavior is unaffected by capping (a regression-shaped test, not new
  behavior).

**Memory:**
- *No unbounded growth* — `Navigate_ManyTimesPastCap_BackStackSizeStaysBounded`: navigate a large
  number of times (e.g. 200, well past any realistic session and far past the cap), assert the stack's
  count stays at or below the cap throughout, not just at the end.

**Regression:**
- Every existing `NavigationServiceTests` test (permission-gated `NavigateTo<T>` denial/allow,
  `CanGoBack`/`CanGoForward` basic true/false transitions, the existing pre-cap `GoBack`/`GoForward`
  round-trip) is expected to **pass unchanged** — none of them navigate anywhere near the proposed
  20-entry cap, so none should be sensitive to its introduction. Explicitly re-run these, not just
  assumed, once implementation happens.

---

## F. Implementation Checklist (for the future authorized pass — not executed here)

1. Confirm the 20-entry default with product/UX (or adjust) before implementing.
2. Replace `_backStack`'s underlying `Stack<ViewModelBase>` with a structure supporting O(1) push,
   O(1) pop-from-top, and O(1) evict-from-bottom (§C's flagged design detail).
3. Implement FIFO eviction in `Navigate()`, guarded by the cap.
4. Add the 6 tests in §E to `NavigationServiceTests.cs`.
5. Re-run the full existing `NavigationServiceTests` suite to confirm zero regressions (§E,
   Regression).
6. Run the full solution build + test suite + architecture tests, matching this engagement's own
   established validation rhythm, before any commit is authorized.
7. Route through this engagement's own audit → scope-review → commit-execution sequence, as a single,
   isolated commit touching only `NavigationService.cs` + its test file — no bundling with anything
   else.

---

## Risk Review — Task 5

**Confirmed: no impact on Authentication, Booking Authority, Calendar Authority, Shift Engine, or
RBAC.** `NavigationService` is a pure Shell-layer UI-navigation concern — it holds `ViewModelBase`
references for display purposes only and contains no business logic, no permission decision beyond the
pre-existing `RequiredPermissionsByViewModelType` check (itself untouched by this proposal), no backend
call, and no data-authority role of any kind. The one and only user-visible consequence of this
hardening is bounded Back-navigation depth (§D) — a UX property, not a security, authority, or
correctness property of any domain this engagement has hardened. `LoginWindowViewModel`/
`MobileOtpLoginViewModel` are confirmed (Phase 8.4 §D) entirely outside this system, so Authentication
is untouched by construction, not merely by assertion.

---

## STOP

Scope review complete. No implementation performed.
