# ROJAN AI — TEAM 3 — PHASE 8.3 CARD KPI EVENT LIFETIME INVESTIGATION — REPORT v1

**Type:** Audit only. No code modified, no unsubscribe added, no converter refactored, no commit, no
push. `HEAD` (`801cc65`) unchanged before and after.

**Naming clarification, up front:** no class named `CardKpiCollection` exists anywhere in this
repository (confirmed by an exhaustive filename and content search). Given this task's own description
matches the "suspected WPF event subscription lifetime risk" flagged in
`ROJAN_PHASE8_1_DESKTOP_PRODUCTION_EXCELLENCE_AUDIT_v1.md`/`ROJAN_PHASE8_2_RELEASE_HARDENING_PLAN_v1.md`
exactly, this investigation targets that component — **`DashboardKpiCollection`** — on the assumption
that's the intended referent. This substitution is disclosed here, not silent.

---

## A. Component Analysis — Task 1

| | |
|---|---|
| **File location** | `src/Rojan.Desktop.Presentation/Controls/Dashboard/DashboardKpiCollection.cs` |
| **Class type** | `public sealed class DashboardKpiCollection : IEnumerable<object>, INotifyCollectionChanged` — a live, read-only wrapper view, not a control and not `IDisposable` |
| **Lifetime** | Not independently managed — created fresh on every `Convert()` call of `KpiMetricsWithDerivedConverter`, an `IValueConverter` declared as a page-level `StaticResource` in `DashboardPage.xaml` |
| **Construction pattern** | `KpiMetricsWithDerivedConverter.Convert(value, ...)` → `new DashboardKpiCollection(source, DerivedCards, SourceSortKey)`, where `source` is whatever the bound `KpiMetrics` property currently holds. The constructor subscribes to `source.CollectionChanged` if `source` implements `INotifyCollectionChanged` — `KpiMetrics` (an `ObservableCollection<KpiMetricDto>`) does |

---

## B. Event Flow — Task 2

Traced end to end, each link verified by direct source read, not assumed:

**1. Subscribe.** `DashboardKpiCollection`'s constructor: `notifier.CollectionChanged += OnSourceCollectionChanged;` — `notifier` is `DashboardPageViewModel.KpiMetrics`.

**2. Publisher lifetime.** `KpiMetrics` is declared `public ObservableCollection<KpiMetricDto> KpiMetrics { get; }` — **get-only**, assigned exactly once in the constructor (`KpiMetrics = new ObservableCollection<KpiMetricDto>();`), never reassigned. `LoadAsync` only calls `.Clear()`/`.Add()` on this same instance. **The publisher's lifetime is therefore identical to its owning `DashboardPageViewModel` instance's lifetime** — it cannot outlive it, and nothing reassigns or replaces it independently.

**3. Subscriber lifetime.** `DashboardKpiCollection` holds no reference back to `DashboardPageViewModel` directly, but it is only ever reachable via the binding chain rooted at that same ViewModel (`ItemsSource="{Binding KpiMetrics, Converter=...}"` — the binding's source object is the page's `DataContext`, i.e. the `DashboardPageViewModel` instance itself). **The subscriber's effective lifetime is therefore also tied to the same `DashboardPageViewModel` instance** — it is a child of the same object graph as its publisher, not an independent, differently-scoped object.

**4. Unsubscribe.** **None exists** — confirmed, no `-=`, no `IDisposable`, no `Dispose()` method anywhere in `DashboardKpiCollection.cs`.

**Determination: A — Safe.** The missing unsubscribe is real, but it is inert: publisher and subscriber
share one lifetime root (the owning `DashboardPageViewModel`). When that instance becomes eligible for
garbage collection, `KpiMetrics` and `DashboardKpiCollection` become eligible together, regardless of
the still-registered delegate between them — .NET's garbage collector reclaims unreferenced object
graphs including their internal cycles; a missing unsubscribe only matters when the **publisher**
outlives the **subscriber's need for it**, which is not the case here (see §C for the one caveat this
finding depends on).

---

## C. WPF Binding Analysis — Task 3

| Check | Finding |
|---|---|
| **Converter lifetime** | `KpiMetricsWithDerivedConverter` is a page-level `StaticResource` — one shared, stateless instance (no fields) reused for the one `ItemsSource` binding that references it. The converter itself holds nothing that could leak; each `Convert()` call returns a brand-new `DashboardKpiCollection` value, not a cached one |
| **Binding mode** | `ItemsSource="{Binding KpiMetrics, Converter={StaticResource KpiMetricsWithDerivedConverter}}"` — implicit `OneWay` (the default for a non-two-way-capable target property; `ConvertBack` explicitly `throw`s, confirming one-way-only by design) |
| **Re-evaluation frequency — the determining fact, resolved this pass, not previously verified** | A WPF binding's converter re-runs only when the **bound property itself** raises `PropertyChanged` — not when a collection it returns fires its own `CollectionChanged`. Since `KpiMetrics` is get-only and never reassigned (§B.2), it never raises `PropertyChanged("KpiMetrics")`. **`Convert()` therefore runs exactly once per `DashboardPageViewModel` instance** — when the binding is first activated (DataContext attachment) — not repeatedly. This is precisely why `DashboardKpiCollection` exists at all (per its own doc comment): to re-raise `CollectionChanged` itself for in-place `Clear()`/`Add()` mutations, since the converter path alone would never see them |
| **Source ownership** | `KpiMetrics` is owned exclusively by the `DashboardPageViewModel` instance that is the binding's `DataContext` — never shared across instances, never a Singleton-scoped or static collection |

**Verdict on repeated creation:** repeated creation of `DashboardKpiCollection` instances **does** happen
— once per `DashboardPageViewModel` instantiation (i.e., once per navigation to the Dashboard page,
since `DashboardPageViewModel` is `AddTransient`, confirmed in DI) — but each new instance subscribes
to a **new, distinct `KpiMetrics` instance** created alongside it, not the same long-lived source
repeatedly. There is no scenario found in which multiple `DashboardKpiCollection` instances accumulate
against **one shared, long-lived** source.

---

## D. Risk Level — Task 4

**Classification: No Risk**, for the specific mechanism this investigation targeted (subscription vs.
publisher/subscriber lifetime mismatch). Explanation:

- The publisher (`KpiMetrics`) and subscriber (`DashboardKpiCollection`) are always created and
  discarded together, as part of the same `DashboardPageViewModel` object graph — never independently
  scoped.
- Repeated navigation to the Dashboard page creates repeated, but **mutually independent**, pairs — not
  a growing chain against one shared collection.
- Nothing found gives the event subscription itself the power to keep anything alive that wouldn't
  already be kept alive by the ordinary object graph rooted at the page's own `DataContext`.

**One related, distinct finding surfaced during this trace, disclosed even though it is outside this
component's own scope:** `Rojan.Desktop.Shell.Navigation.NavigationService`'s `_backStack` retains
**every previously-visited page ViewModel instance indefinitely** — `Navigate(viewModel)`
unconditionally does `_backStack.Push(_current)` before switching content, and nothing found ever
removes an entry from `_backStack` except `GoBack()`, which *relocates* it to `_current`/
`_forwardStack` rather than releasing it. This means a `DashboardPageViewModel` instance (and
everything it owns, including its `KpiMetrics`/`DashboardKpiCollection`) is deliberately retained by
this back-stack design for as long as the app session runs, or until enough `GoBack()` calls pop it
back to the front — **this is what actually keeps a visited Dashboard instance alive, not the missing
event unsubscribe.** Even if `DashboardKpiCollection` unsubscribed perfectly, the whole
`DashboardPageViewModel` graph would still be retained by `_backStack` regardless — the unsubscribe
would free nothing. This is a **separate, broader architectural question about navigation-history
memory growth**, not a defect in `DashboardKpiCollection`, and is explicitly out of scope for this
investigation's own component-level question.

---

## E. Recommendation — Task 5

**No fix is required for `DashboardKpiCollection` itself.** The suspected leak mechanism (subscription
lifetime mismatch) does not exist for this component — publisher and subscriber are lifetime-matched by
construction, not by any code that would need to be added.

**Optional, low-priority hardening — not because a bug exists, but as cheap insurance against a future
change that could break the current lifetime-matching assumption:**
- **Required change (if ever pursued):** have `DashboardKpiCollection` implement `IDisposable`, calling
  `notifier.CollectionChanged -= OnSourceCollectionChanged` in `Dispose()`; the converter/binding
  infrastructure would need an explicit disposal trigger (e.g. the page's own `Unloaded` handler,
  matching `DashboardPage.xaml.cs`'s existing `Unloaded += (_, _) => _clockTimer.Stop()` pattern) since
  nothing currently calls `Dispose` on a converter-produced value.
- **Scope:** one file (`DashboardKpiCollection.cs`) plus a few lines in `DashboardPage.xaml.cs`'s
  existing `Unloaded` handler — no converter refactor needed, no XAML change needed.
- **Test strategy:** a unit test constructing a `DashboardKpiCollection` over a stub
  `INotifyCollectionChanged` source, disposing it, then raising the source's `CollectionChanged` and
  asserting the wrapper's own `CollectionChanged` does *not* fire — proving the unsubscribe actually
  took effect, not just that `Dispose()` didn't throw.

**Recommended, separate follow-up (not this task's scope, named for visibility):** investigate
`NavigationService`'s `_backStack`/`_forwardStack` retention policy — whether unbounded growth over a
long session is intentional (a deliberate, accepted trade-off for full back/forward history) or worth
capping. This is the actual, real memory-growth mechanism found during this trace; `DashboardKpiCollection`
was a red herring relative to it.

---

## STOP

Investigation complete. No code changes performed.
