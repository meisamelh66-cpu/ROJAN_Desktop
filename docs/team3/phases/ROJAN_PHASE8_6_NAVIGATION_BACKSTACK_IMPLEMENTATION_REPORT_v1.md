# ROJAN AI — TEAM 3 — PHASE 8.6 NAVIGATION BACKSTACK HARDENING — IMPLEMENTATION REPORT v1

**Type:** Implementation complete. Build + full test suite + architecture tests all green.
**No commit performed.** `HEAD` is still `801cc65` — this report is the gate before commit authorization.

**Branch:** `feature/team3-desktop-completion`
**Authorization:** `PHASE 8.6 — NAVIGATION BACKSTACK HARDENING — IMPLEMENTATION AUTHORIZATION v1` (APPROVED)
**Scope reference:** `ROJAN_PHASE8_5_NAVIGATION_BACKSTACK_HARDENING_SCOPE_REVIEW_v1.md`

---

## A. Files Changed

Exactly the 2 files the authorization allowed — no others touched.

| File | Layer | +/− | Nature of change |
|---|---|---|---|
| `src/Rojan.Desktop.Shell/Navigation/NavigationService.cs` | Shell | +72 / −4 | Bounded back-stack: `LinkedList<T>` deque, `MaxBackStackDepth` const, FIFO eviction, 2 internal test seams |
| `tests/Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs` | Shell.Tests | +135 / −0 | 5 new tests + 2 private helpers + 1 `using` |

`git diff --stat`: `2 files changed, 207 insertions(+), 4 deletions(-)`

**Confirmed NOT modified** (per authorization's DO-NOT list):
`INavigationService` (untouched — no contract change), any ViewModel, any Page, any routing/module descriptor,
Domain layer, Booking, Calendar, Shift Engine, RBAC, Authentication. Verified by the diff above being the
entire change set.

---

## B. Design Implementation

### B.1 Structural change

`_backStack` changed from `Stack<ViewModelBase>` to `LinkedList<ViewModelBase>` used as a deque. Reason
(flagged in the scope review §C): `Stack<T>` has no O(1) remove-from-bottom, which FIFO eviction requires.
`LinkedList<T>` gives all three needed operations in O(1):

| Logical op | `LinkedList<T>` call | Site |
|---|---|---|
| push (top of stack) | `AddLast` | `PushBackStack` |
| pop (top of stack) | `Last.Value` + `RemoveLast` | `PopBackStack` |
| evict oldest (bottom) | `RemoveFirst` | `PushBackStack`, only when at cap |

`_forwardStack` is **unchanged** — still a plain `Stack<ViewModelBase>`. It is cleared on every fresh
`Navigate()` and so is already self-bounding by ordinary usage (scope review §C, "separate secondary
question"). Not capped — capping it was explicitly optional, not the primary fix.

### B.2 Configuration

```csharp
internal const int MaxBackStackDepth = 20;
```

- Value **20**, exactly as authorized.
- `internal` (not `private`) so tests assert against the same constant rather than a magic number —
  `InternalsVisibleTo("Rojan.Desktop.Shell.Tests")` already exists in the Shell `.csproj`.
- Single point of change if product/UX later wants a different depth.

### B.3 Eviction algorithm (FIFO, oldest-first)

```csharp
private void PushBackStack(ViewModelBase viewModel)
{
    if (_backStack.Count >= MaxBackStackDepth)
    {
        _backStack.RemoveFirst();   // evict oldest (bottom) FIRST
    }

    _backStack.AddLast(viewModel);  // THEN push new entry on top
}
```

Order is exactly as the authorization specified: *"If BackStack reaches limit: Remove oldest entry, Then
push new entry."* The count can therefore never exceed `MaxBackStackDepth`, and the evicted
`ViewModelBase` is no longer referenced by `NavigationService` (→ collectible, subject to nothing else
holding it — see test C.3).

### B.4 Call sites routed through the cap

All three places that add to the back-stack now go through `PushBackStack`:

| Method | Before | After |
|---|---|---|
| `Navigate(viewModel)` (private; behind both `NavigateTo` overloads) | `_backStack.Push(_current)` | `PushBackStack(_current)` |
| `GoForward()` | `_backStack.Push(_current)` | `PushBackStack(_current)` |
| `GoBack()` | `_backStack.Pop()` | `PopBackStack()` |

Routing `GoForward` through the cap too keeps the invariant `_backStack.Count <= MaxBackStackDepth`
unconditionally true. In practice `GoForward` can only move an entry that a prior `GoBack` took *out* of
the back-stack, so it cannot by itself drive growth past the cap — but going through the same helper is
simpler and leaves no unbounded path.

### B.5 Test seams added (internal, Shell only)

```csharp
internal int BackStackDepth => _backStack.Count;   // assert the cap directly
internal ViewModelBase? Current => _current;        // assert which entry GoBack/GoForward land on
```

Both mirror the role `CanGoBack`/`CanGoForward` already play for the existing tests. No public surface
change; `INavigationService` consumers see nothing new.

### B.6 UX consequence (disclosed, unchanged from scope review §D)

Once 20 back-entries exist, the 21st navigation permanently drops the oldest — it is **unreachable via
Back thereafter, not deferred**. Bounded memory is bought at the cost of bounded history depth. 20 is deep
enough that ordinary use is very unlikely to hit the boundary.

---

## C. Test Evidence

`NavigationServiceTests` now has **8 tests (3 pre-existing + 5 new)**. All 8 pass:

```
dotnet test ... --filter "FullyQualifiedName~NavigationServiceTests"
Passed!  - Failed: 0, Passed: 8, Skipped: 0, Total: 8
```

| # | Authorization requirement | Test | What it proves |
|---|---|---|---|
| 1 | Stack limit respected | `Navigate_ExceedsMaxDepth_BackStackDepthNeverExceedsCap` | Navigates cap+10 times; asserts `BackStackDepth <= 20` **after every single navigation** (not just at the end), and `== 20` at the end |
| 2 | Oldest entry eviction | `Navigate_ExceedsMaxDepth_EvictsOldestEntryFirst` | Navigates `page-0..page-21`; walks `GoBack` to exhaustion; asserts exactly 20 entries reachable, first popped is `page-20`, last reachable is `page-1`, and `page-0` is **not** among them (evicted from the bottom) |
| 3 | WeakReference confirms released object | `Navigate_ExceedsMaxDepth_EvictedViewModelIsReleasedForCollection` | Captures a `WeakReference` to the first-navigated VM via a `[MethodImpl(NoInlining)]` helper (no lingering stack root), pushes past the cap, forces GC; asserts `IsAlive == false` — eviction actually drops the reference, not just the count |
| 4 | GoBack after eviction | `GoBack_AfterEviction_WalksEveryRetainedEntryThenStopsWithoutThrowing` | Navigates cap+3; drains `GoBack`; asserts no exception, exactly 20 back-steps, `CanGoBack == false` after — never throws, never touches an evicted entry |
| 5 | Forward navigation regression | `GoForward_AfterGoBack_RestoresThePageSteppedBackFrom` | `page-0→1→2`, `GoBack` lands on `page-1` with `CanGoForward` true, `GoForward` restores `page-2` with `CanGoForward` false — forward semantics unchanged by the new structure |
| 6 | Existing Navigation tests remain passing | (the 3 pre-existing tests) | `NavigateTo_TargetRequiresUngrantedPermission_DoesNotResolveOrNavigate`, `NavigateTo_TargetRequiresGrantedPermission_NavigatesNormally`, `NavigateTo_TargetHasNoRequiredPermission_NavigatesRegardlessOfRole` — all pass unchanged, no edits |

Helpers added: `CreateSutWithSequentialPlaceholderPages()` (registers a fresh
`PlaceholderModuleViewModel` per resolution, `Title = "page-N"`, so eviction order is observable),
`CurrentTitle(sut)`, and the `NoInlining` weak-reference helper.

---

## D. Validation Results

All run against the full solution, matching this engagement's established rhythm.

### D.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### D.2 Full test suite

```
dotnet test --no-build
```

| Project | Result |
|---|---|
| Rojan.Desktop.Domain.Tests | Passed — 456 |
| Rojan.Desktop.Presentation.Tests | Passed — 569 |
| Rojan.Desktop.Application.Tests | Passed — 791 |
| Rojan.Desktop.Infrastructure.Tests | Passed — 609 |
| Rojan.Desktop.Shell.Tests | Passed — 80 |
| Rojan.Desktop.ArchitectureTests | Passed — 7 |
| **TOTAL** | **Passed — 2,512 / 2,512, 0 failed, 0 skipped** |

- Baseline at `801cc65` (checkpoint §E): **2,507** total (that figure includes the 7 architecture tests).
- Now: **2,512** = 2,507 + **5 new Phase 8.6 tests**. No pre-existing test changed result.
- Shell.Tests: 75 → 80 (+5).

### D.3 Architecture tests

```
Passed!  - Failed: 0, Passed: 7, Skipped: 0, Total: 7 - Rojan.Desktop.ArchitectureTests.dll
```

**7 / 7** — unchanged. `NavigationService` remains a pure Shell-layer concern; no layering rule affected.

### D.4 Expected vs actual

| Authorization expectation | Actual | Status |
|---|---|---|
| Build PASS | 0 warnings, 0 errors | ✅ |
| Tests: 2507 + new tests | 2,512 (2,507 + 5) | ✅ |
| Architecture: 7/7 | 7/7 | ✅ |

---

## E. Commit Readiness

**Ready. Not committed — stopping here per the authorization's COMMIT RULE.**

- **Working tree:** only the 2 authorized files are modified (plus this report + pre-existing untracked
  `.md` files). No tracked file outside scope changed.
- **Proposed staging (explicit paths only — never `git add -A` / `git add .`):**
  - `src/Rojan.Desktop.Shell/Navigation/NavigationService.cs`
  - `tests/Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs`
  - `ROJAN_PHASE8_6_NAVIGATION_BACKSTACK_IMPLEMENTATION_REPORT_v1.md` (this file — include or hold per your call; consistent with prior phases it is typically left untracked)
- **Proposed commit message (single, isolated commit — no bundling):**

  ```
  fix(desktop): bound navigation back-stack depth

  Cap NavigationService._backStack at 20 entries with FIFO (oldest-first)
  eviction to bound retained ViewModel memory over long sessions. Replace
  the bare Stack<T> with a LinkedList<T> deque for O(1) evict-from-bottom.
  INavigationService and all call sites are unchanged; forward-stack is
  left as-is (already self-bounding). Trade-off: the oldest back entry
  becomes unreachable via Back once the cap is exceeded.
  ```

- **Downstream impact:** none on Authentication, Booking, Calendar, Shift Engine, or RBAC — confirmed by
  the diff being fully contained to `NavigationService` internals + its test file.
- **Checkpoint update owed after commit:** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §B (new commit
  row), §F (Navigation BackStack item → resolved), §G (new next action → §F logging-coverage item).

---

## STOP

Implementation and validation complete. **No commit performed.** Awaiting commit authorization.
