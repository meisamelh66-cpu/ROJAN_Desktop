# ROJAN AI — TEAM 3 — PHASE 8.4 NAVIGATION MEMORY RETENTION AUDIT — REPORT v1

**Type:** Audit only. `NavigationService` not modified, no ViewModel lifecycle changed, no disposal
logic added, no routing changed, no commit. `HEAD` (`801cc65`) unchanged before and after — the finding
this report investigates was itself surfaced (not fixed) during Phase 8.3's own component-level trace.

---

## A. Navigation Architecture — Task 1

| | |
|---|---|
| **Implementation** | `Rojan.Desktop.Shell.Navigation.NavigationService : INavigationService`, `sealed`, registered `AddSingleton` (confirmed: `services.AddSingleton<NavigationService>(); services.AddSingleton<INavigationService>(sp => sp.GetRequiredService<NavigationService>());`) — **one instance for the entire application process lifetime**, not per-window or per-session |
| **BackStack data structure** | Two plain `Stack<ViewModelBase>` fields: `_backStack`, `_forwardStack`. No capacity limit, no eviction policy, no external configuration — a bare `System.Collections.Generic.Stack<T>`, which grows unbounded by design (no `Stack<T>` constructor overload limiting size is used) |
| **Page/ViewModel creation pattern** | `NavigateTo<TViewModel>()` calls `_serviceProvider.GetRequiredService<TViewModel>()` — every page ViewModel is registered `AddTransient` (confirmed for `DashboardPageViewModel` and the general Presentation DI convention), so **every navigation call, including repeat visits to the same page type, constructs a brand-new instance** |
| **Ownership model** | The class's own doc comment states the design explicitly: *"History is standard browser-style: navigating to something new pushes the current entry onto the back-stack and clears the forward-stack... exactly like a web browser abandons forward history once you click a new link after going back."* This is a **deliberate, documented design choice**, not an accidental retention |

---

## B. Lifetime Flow — Task 2

Traced through every method in the file, not summarized from memory:

```
NavigateTo<T>() / NavigateTo(descriptor)
        │
        ▼
Navigate(viewModel)
   ├─ if _current is not null: _backStack.Push(_current)   ← OLD page retained here
   ├─ _forwardStack.Clear()                                 ← only release point on fresh nav
   └─ SetContent(viewModel)  →  _current = viewModel (NEW instance)
        │
        ▼
   [page renders, is used, user navigates away again → repeat from top]
        │
        ▼  (only if the user explicitly invokes Back)
GoBack()
   ├─ _forwardStack.Push(_current)   ← current moves to forward stack, not released
   └─ SetContent(_backStack.Pop())   ← popped entry becomes _current again
```

**Object release — the precise mechanism, and its one real gap:**
- An entry in `_backStack` is removed **only** by `GoBack()` popping it — which does not discard it, it
  relocates it to `_current` (and, on the *next* fresh `Navigate()` call, into `_forwardStack`, from
  which it can finally be released when *that* stack is `.Clear()`-ed by yet another fresh navigation).
- **An entry pushed onto `_backStack` by ordinary forward navigation is never removed by any amount of
  further forward navigation.** Navigating Dashboard → Bookings → Calendar → Dashboard → Bookings →
  Calendar (repeating indefinitely, never clicking Back) pushes a new entry onto `_backStack` on every
  single transition, forever, for the life of the process.

**Can old ViewModels be collected?** Only in two cases: (1) the user clicks Back enough times to pop a
given entry back to `_current`, then navigates somewhere new (clearing `_forwardStack`, which — if that
entry had just been pushed onto it by the Back click and not clicked-forward again — releases it), or
(2) the entire process exits. **Ordinary forward-only navigation, which is the primary way this app is
used (a persistent sidebar for direct navigation exists; Back/Forward are secondary, confirmed wired to
real toolbar commands and the command palette, but not the primary navigation path for most users),
never releases anything.**

---

## C. Memory Assessment — Task 3

**This is a confirmed retention pattern, not a hypothetical one** — traced through actual code, not
inferred. Whether to call it a "leak" depends on framing:

- **The caching *behavior* itself (A) is expected** — explicitly documented, browser-style
  back/forward history is a legitimate, deliberate UX feature.
- **The *absence of any bound or eviction policy* on that cache is not obviously a deliberate choice** —
  nothing in the doc comment or code suggests anyone decided "and let it grow without limit for an
  entire session." This is the actual gap.

**Classification: P2.**

Reasoning, weighing the four factors this task asked for:

| Factor | Assessment |
|---|---|
| **User behavior** | Sidebar-first navigation (this app's primary pattern, confirmed by `MainWindowViewModel.BuildVisibleNavigationItems` existing as the main nav-item source) means most real sessions accumulate `_backStack` entries steadily; Back/Forward usage — while real and wired — is very unlikely to keep pace with forward navigation for most users |
| **Stack growth** | Unbounded and monotonic under sidebar-first usage; one entry per navigation, no cap, no periodic pruning |
| **Object retention** | Each retained entry is a full page ViewModel plus whatever it loaded (e.g. a Bookings page's currently-filtered list) — **confirmed passive, not active**: `DashboardPage.xaml.cs`'s own `Unloaded += (_, _) => _clockTimer.Stop()` (and the equivalent for any other page-level timer) fires when the *view* leaves the visual tree on navigation-away, so a retained-in-backstack ViewModel is not still polling, ticking, or consuming CPU — it is inert memory, not an active leak of ongoing work |
| **Production impact** | A long-running session (a full workday or longer, common for a desktop line-of-business app left open) with steady sidebar navigation and infrequent Back usage will show **slow, monotonic memory growth that never recovers without a restart** — real, measurable, but not a crash risk on any realistic single-session timescale, and each retained instance's own footprint is bounded by what one page normally loads (not unbounded per-instance) |

**Not P1/P0:** nothing here corrupts data, blocks a workflow, or risks an application crash on any
timescale actually observed or reasoned through in this audit. **Not "No Risk":** this is a real,
confirmed, currently-live pattern in production code today, not a theoretical concern.

---

## D. Domain Impact — Task 4

| Flow | Affected? | Assessment |
|---|---|---|
| **Authentication** | **No** | `LoginWindowViewModel`/`MobileOtpLoginViewModel` live in a separate WPF `Window`, entirely outside `NavigationService`'s content-swapping system — they are never pushed onto `_backStack`; both are `Transient` and released together when the login window closes (Phase 8.3's own reasoning applies directly) |
| **Dashboard** | **Yes — likely highest exposure** | The natural "home" page a user returns to repeatedly throughout a session; each return via the sidebar (not Back) creates and retains a new `DashboardPageViewModel` (and, per Phase 8.3, its own `KpiMetrics`/`DashboardKpiCollection` pair) |
| **Booking** | **Yes** | Same mechanism, likely high-frequency given this is a primary daily-use page |
| **Calendar** | **Yes** | Same mechanism, similar frequency to Booking |
| **Shift Engine** | **Yes, but lower exposure** | `SpecialistScheduleViewModel`/`SpecialistAvailabilityViewModel` are composed *inside* `SpecialistProfileViewModel`, itself reached from `SpecialistPageViewModel` — switching between specialists/tabs within that page is an internal state change, not a fresh `NavigationService.NavigateTo` call, so it does not itself grow `_backStack`. Exposure here is bounded by how often the user navigates to/from the Specialists page as a whole, likely less frequent than Dashboard/Booking/Calendar |

**Is this critical for any of them?** No — this is a uniform, mechanism-level pattern affecting every
`NavigateTo`-reached page equally; it is not a domain-specific defect in Authentication, Booking,
Calendar, or Shift Engine's own logic, and none of Phase 7.4's RBAC/Calendar/Auth authority work is
implicated in any way.

---

## E. Recommendation — Task 5

**A mitigation is worth pursuing, at low urgency, given the P2 classification.** Recommended options,
identification only:

**Possible mitigations, in order of increasing intrusiveness:**
1. **Cap `_backStack`/`_forwardStack` depth** — e.g. retain only the most recent N entries, discarding
   the oldest when a push would exceed it. Smallest possible change (a bound check in `Navigate`/
   `GoBack`/`GoForward`), preserves the existing browser-style UX for realistic back-navigation depths
   (few users go back more than a handful of steps), bounds worst-case memory to a fixed multiple of
   one page's footprint.
2. **Deduplicate consecutive same-type entries** — if `NavigateTo<T>()` is called while `_current` is
   already a `T` (unlikely given sidebar-first UX, but possible for deep-link/quick-action scenarios),
   avoid pushing a redundant entry. Smaller effect than (1), addresses a narrower case.
3. **Clear `_backStack`/`_forwardStack` on a natural session boundary** (logout, if that path resets
   the app in-process rather than exiting the process — **this audit did not fully verify whether logout
   restarts the process or reuses it**, worth confirming before relying on this as a mitigation).

**Scope, if (1) is pursued:** `NavigationService.cs` only — `Navigate`, `GoBack`, `GoForward` — no
`INavigationService` interface change, no caller changes anywhere else in the app.

**Testing approach:** extend `NavigationServiceTests` (the existing test file already covering this
class, confirmed from its own doc comment reference) with a test that navigates past the chosen cap and
asserts the oldest retained `ViewModelBase` reference is dropped (e.g. via a `WeakReference` check that
it becomes collectible after a forced GC, or simply asserting stack `Count` never exceeds the cap) —
the same verification shape already proven adequate for `CanGoBack`/`CanGoForward` behavior in that
file today.

**Not recommended:** adding `IDisposable` to every page ViewModel and disposing on stack eviction —
Phase 8.3 already established that no component-level subscription actually leaks *beyond* what this
retention itself causes; the retention is the root cause, not a symptom requiring per-ViewModel cleanup
machinery.

---

## STOP

Audit complete. No code changes performed.
