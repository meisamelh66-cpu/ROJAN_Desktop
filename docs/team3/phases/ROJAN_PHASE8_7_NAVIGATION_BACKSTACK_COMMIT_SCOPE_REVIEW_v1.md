# ROJAN AI — TEAM 3 — PHASE 8.7 NAVIGATION BACKSTACK HARDENING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No commit performed.** No code changed in producing this document.
**Mode:** READINESS ONLY — this gate confirms the exact diff, staging list, and message before Phase 8.8
(commit execution) is authorized.

**Branch:** `feature/team3-desktop-completion`
**HEAD:** `801cc65` (`git rev-parse HEAD` this turn — unchanged, no drift since the checkpoint)
**Predecessors:** Phase 8.6 implementation (`ROJAN_PHASE8_6_NAVIGATION_BACKSTACK_IMPLEMENTATION_REPORT_v1.md`),
Phase 8.5 scope review, Phase 8.4 audit.

---

## A. Working Tree State — Verified This Turn

```
git status --porcelain (tracked only):
 M src/Rojan.Desktop.Shell/Navigation/NavigationService.cs
 M tests/Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs
```

- **Exactly 2 tracked files modified** — both on the Phase 8.6 authorization's allow-list, nothing else.
- No tracked file deleted, renamed, or added.
- Untracked: only `.md` reports (this engagement's audit trail), including this phase's own two report
  files. No untracked code.
- `git diff --stat`: `2 files changed, 207 insertions(+), 4 deletions(-)`.

**No scope leakage.** The diff does not touch `INavigationService`, any ViewModel, any Page, any module/
routing descriptor, Domain, Booking, Calendar, Shift Engine, RBAC, or Authentication.

---

## B. Diff Review — `NavigationService.cs` (+72 / −4)

| Hunk | Change | Assessment |
|---|---|---|
| Class XML doc | Added a Phase 8.6 paragraph explaining the bound, FIFO eviction, the `LinkedList<T>`-as-deque choice, and why `_forwardStack` is left alone | Documentation only |
| Field | `private readonly Stack<ViewModelBase> _backStack` → `private readonly LinkedList<ViewModelBase> _backStack` | Required: `Stack<T>` has no O(1) remove-from-bottom. `LinkedList<T>` gives O(1) `AddLast`/`RemoveLast`/`RemoveFirst`. `_forwardStack` unchanged (`Stack<T>`) |
| Const | `internal const int MaxBackStackDepth = 20` | Value exactly as authorized. `internal` so tests bind to the same symbol (`InternalsVisibleTo` already present) |
| Seam | `internal int BackStackDepth => _backStack.Count` | Test-only observability, mirrors `CanGoBack`'s existing role. No public surface change |
| Seam | `internal ViewModelBase? Current => _current` | Test-only observability of the displayed VM. No public surface change |
| `GoBack()` | `SetContent(_backStack.Pop())` → `SetContent(PopBackStack())` | Behaviour-preserving: `PopBackStack` returns `_backStack.Last.Value` then `RemoveLast()` — same LIFO top-of-stack pop. Still guarded by the pre-existing `if (!CanGoBack) return;` |
| `GoForward()` | `_backStack.Push(_current)` → `PushBackStack(_current)` | Routes the one other back-stack add through the cap. See §D for why this can never actually evict in practice — kept for invariant safety |
| `Navigate()` | `_backStack.Push(_current)` → `PushBackStack(_current)` | The primary fix path. FIFO eviction now applies to normal forward navigation |
| New `PushBackStack(vm)` | `if (Count >= cap) RemoveFirst(); AddLast(vm);` | Eviction order is exactly "remove oldest, **then** push new" as authorized. Post-condition: `Count <= cap` always |
| New `PopBackStack()` | `var top = Last!.Value; RemoveLast(); return top;` | `!` is safe — every caller checks `CanGoBack` (Count > 0) first |

**Correctness conclusion:** the change is behaviour-preserving for every navigation sequence that stays
within 20 back-entries (i.e. all existing tests and essentially all real sessions), and adds bounded FIFO
eviction beyond that point. No other behaviour of `NavigationService` (permission gate, host attach,
deferred `ApplyContent` dispatch, forward-stack clear-on-navigate) is touched.

---

## C. Diff Review — `NavigationServiceTests.cs` (+135 / −0)

- **Zero deletions, zero edits to existing tests.** The 3 pre-existing tests
  (`NavigateTo_TargetRequiresUngrantedPermission_DoesNotResolveOrNavigate`,
  `NavigateTo_TargetRequiresGrantedPermission_NavigatesNormally`,
  `NavigateTo_TargetHasNoRequiredPermission_NavigatesRegardlessOfRole`) are unchanged and still pass.
- **1 new `using`:** `System.Runtime.CompilerServices` (for `[MethodImpl(MethodImplOptions.NoInlining)]`
  on the weak-reference helper).
- **2 new private helpers:** `CreateSutWithSequentialPlaceholderPages()` (fresh `PlaceholderModuleViewModel`
  per resolve, `Title = "page-N"`), `CurrentTitle(sut)`, plus the `NoInlining` weak-ref helper.
- **5 new `[Fact]` tests**, mapping 1:1 to the authorization's test requirements:

| Requirement | Test |
|---|---|
| 1. Stack limit respected | `Navigate_ExceedsMaxDepth_BackStackDepthNeverExceedsCap` |
| 2. Oldest entry eviction | `Navigate_ExceedsMaxDepth_EvictsOldestEntryFirst` |
| 3. WeakReference confirms released object | `Navigate_ExceedsMaxDepth_EvictedViewModelIsReleasedForCollection` |
| 4. GoBack after eviction | `GoBack_AfterEviction_WalksEveryRetainedEntryThenStopsWithoutThrowing` |
| 5. Forward navigation regression | `GoForward_AfterGoBack_RestoresThePageSteppedBackFrom` |
| 6. Existing tests remain passing | (the 3 above, unchanged) |

Test style matches the file's existing conventions (xUnit `[Fact]`, `Record.Exception`, `ServiceCollection`
SUT wiring, `PlaceholderModuleViewModel` as the navigation payload).

---

## D. Risk Review

| Area | Finding |
|---|---|
| **`GoForward` eviction** | Cannot occur in practice. `_forwardStack` only accumulates entries that a prior `GoBack` removed from `_backStack`; since `_backStack` is capped at 20, no more than 20 consecutive `GoBack` calls are ever possible, so `GoForward` can push `_backStack` back to at most 20 — never to 21, never triggering `RemoveFirst`. Routing it through `PushBackStack` anyway keeps the `Count <= 20` invariant unconditionally true with no downside |
| **Other consumers of the concrete class** | `App.xaml.cs` (DI registration) and `MainWindow.xaml.cs` (calls `Attach`, sets `DashboardNavigationBridge.Current`) take `NavigationService` concretely but touch none of the changed internals. Every other consumer — `MainWindowViewModel`, `CommandPaletteViewModel`, `DashboardNavigationBridge`, `WorkspaceHostViewModel` — uses `INavigationService` only. Confirmed by grep this turn |
| **Test doubles** | `StubNavigationService` (used by `MainWindowViewModelNavigationTests`, `CommandPaletteViewModelTests`, etc.) is a separate hand-written `INavigationService` implementation — completely unaffected by a change to the concrete `NavigationService` |
| **`MainWindowViewModel` CanGoBack/CanGoForward republish** (`MainWindowViewModel.cs:388`) | Still correct — both properties keep their exact prior semantics (`_backStack.Count > 0` / `_forwardStack.Count > 0`) |
| **Thread-safety** | Unchanged. `NavigationService` was never thread-safe and isn't now; all navigation is UI-thread only. `LinkedList<T>` and `Stack<T>` have identical (none) thread-safety guarantees |
| **Architecture layering** | `NavigationService` stays a pure Shell-layer concern. Architecture tests 7/7 unchanged |
| **Domain impact** | None on Authentication, Booking, Calendar, Shift Engine, RBAC — `NavigationService` holds `ViewModelBase` references for display only, no business logic, no backend call, no data authority |

**UX trade-off (disclosed, accepted at Phase 8.5):** once 20 back-entries exist, the oldest is
permanently unreachable via Back. Not a regression of any hardened domain — a deliberate bounded-history
property.

---

## E. Fresh Validation — Re-run This Turn

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded. 0 Warning(s), 0 Error(s).** |
| Full test suite | **2,512 / 2,512 passing, 0 failed, 0 skipped** (Domain 456, Presentation 569, Application 791, Infrastructure 609, Shell 80, Architecture 7) |
| Architecture tests | **7 / 7 passing** |
| Delta vs `801cc65` baseline (2,507 total incl. architecture) | **+5** — exactly the 5 new Phase 8.6 tests; no pre-existing test changed result |

---

## F. Proposed Commit — For Phase 8.8 Authorization

### F.1 Staging (explicit paths only — never `git add -A` / `git add .`)

```
git add src/Rojan.Desktop.Shell/Navigation/NavigationService.cs
git add tests/Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs
```

Both files are single-concern (Navigation BackStack Hardening only) — no interactive staging or
`git apply --cached` patch isolation is needed this time, unlike the shared-file commits earlier in this
engagement.

The `.md` reports (`ROJAN_PHASE8_6_*`, `ROJAN_PHASE8_7_*`) are **not** staged — consistent with every prior
phase in this engagement, they remain untracked audit-trail artifacts.

### F.2 Commit message (single isolated commit — no bundling)

```
fix(desktop): bound navigation back-stack depth

Cap NavigationService._backStack at 20 entries with FIFO (oldest-first)
eviction to bound retained ViewModel memory over long sessions. Replace
the bare Stack<T> with a LinkedList<T> deque for O(1) evict-from-bottom.
INavigationService and all call sites are unchanged; the forward-stack is
left as-is (already self-bounding via clear-on-navigate). Trade-off: the
oldest back entry becomes unreachable via Back once the cap is exceeded.

Adds 5 NavigationServiceTests covering the cap, FIFO eviction order,
WeakReference-proven release of the evicted ViewModel, GoBack correctness
after eviction, and a GoForward regression guard.
```

### F.3 Post-commit follow-up (Phase 8.8, after the commit)

1. Fresh validation on the new HEAD (build + full suite + architecture tests).
2. Update `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`:
   - §B — add the new commit row.
   - §F — mark "Navigation BackStack unbounded growth" as **Resolved** (with the commit SHA).
   - §G — new next action → §F's next-ranked item: logging coverage for the 4 named ViewModels.

---

## G. Readiness Verdict

**READY TO COMMIT.**

- Diff is complete, minimal, single-concern, and matches the Phase 8.6 authorization exactly.
- Build clean, 2,512/2,512 tests green, architecture 7/7 — re-verified this turn.
- No scope leakage, no risk to any hardened domain, no interface change.
- Staging list and commit message are specified above and ready to execute on Phase 8.8 authorization.

---

## STOP

Commit scope review complete. No commit performed. Awaiting Phase 8.8 (commit execution) authorization.
