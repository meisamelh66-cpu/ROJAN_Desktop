# ROJAN AI — TEAM 3 — PHASE 8.2 DESKTOP RELEASE CANDIDATE HARDENING — PLAN v1

**Type:** Preparation only. No source file modified, no commit, no architecture/backend-contract/
permission/domain-authority change. `HEAD` (`801cc65`) unchanged — confirmed before and after.

---

## A. Scope

Reference: `ROJAN_PHASE8_1_DESKTOP_PRODUCTION_EXCELLENCE_AUDIT_v1.md`, which found no P0/P1 and four
P2 hardening candidates. This plan turns those four into concrete, sequenced, non-blocking work —
**no code is written here.** Every file/line reference below was verified directly against current
source this turn, not carried forward from the prior audit's own citations.

---

## B. Logging Plan — Task 1

**Current coverage, re-confirmed precisely this turn:** 7 of 71 ViewModel files construct an
`ILogger<T>` — `AccountingPageViewModel` (partial, see below), `PosCheckoutViewModel`,
`BookingPageViewModel`, and the four Specialist/Shift Engine ViewModels
(`SpecialistPageViewModel`/`SpecialistProfileViewModel`/`SpecialistScheduleViewModel`/
`SpecialistAvailabilityViewModel`).

**One correction to how "Revenue" coverage should be read:** `AccountingPageViewModel` appears in a
naive `ILogger<` grep, but reading it directly shows it only holds
`ILogger<PosCheckoutViewModel>? _posCheckoutLogger` — a pass-through field to thread a logger into the
checkout child it manually constructs. **`AccountingPageViewModel` has no `ILogger<AccountingPageViewModel>`
of its own; its own `LoadAsync` and other methods log nothing.** Revenue/Accounting reporting is
effectively uncovered.

### Priority flows, assessed against current source

| Flow | ViewModel(s) | Current state | Priority |
|---|---|---|---|
| **Authentication** | `MobileOtpLoginViewModel` | **No `ILogger<T>`** — confirmed absent, despite being hardened for UX in `801cc65`. Every OTP request/verify failure branch sets `ErrorMessage` correctly but logs nothing | **High** — first-touch flow, every session starts here, silent failures here are the hardest to diagnose remotely |
| **Booking** | `BookingPageViewModel` (covered) | Already logs all 5 command methods (`da18c18`) | Done — no action |
| **Calendar** | `CalendarPageViewModel` | **No `ILogger<T>`** — confirmed absent | **High** — a backend-availability failure here (Calendar's sole remaining responsibility since `7103647`) has zero diagnostic trail today |
| **Shift Engine** | 4 Specialist/Schedule ViewModels (covered) | Already logs (`ea03d83`) | Done — no action |
| **Dashboard** | `DashboardPageViewModel` | **No `ILogger<T>`** — confirmed absent | **High** — the first page every user sees after login; a load failure here is maximally visible |
| **Revenue** | `AccountingPageViewModel`, `AnalyticsPageViewModel`, `ReportingPageViewModel` | **None have their own logging** — confirmed absent on all three | **Medium** — real gap, but these are lower-traffic than Dashboard/Calendar/Auth and still Fake/local-adjacent (Accounting) or read-only aggregation (Analytics/Reporting) |

### Recommended target list, in priority order (identification only — no code)

1. `MobileOtpLoginViewModel` — highest priority: first-touch, zero coverage today
2. `DashboardPageViewModel` — highest-traffic page in the app
3. `CalendarPageViewModel` — sole remaining Calendar responsibility (availability reads) currently
   silent on failure
4. `AccountingPageViewModel` — close the gap the naive grep masked; give it its own logger, not just
   the pass-through field
5. `AnalyticsPageViewModel` / `ReportingPageViewModel` — lower priority, same pattern once the above
   four are done

**Pattern to reuse, not invent:** the exact same optional `ILogger<T>? logger = null` →
`NullLogger<T>.Instance` fallback, `[LoggerMessage]` source-generated call, and real-logger DI-threading
convention already established in `da18c18`/`ea03d83` — no new logging pattern is needed, only its
extension.

---

## C. Event Cleanup Plan — Task 2

Re-inspected all 5 files flagged in Phase 8.1, this time reading each subscription's exact pairing
(not just counting `+=`/`-=` occurrences):

| File | Subscription | Unsubscribe present? | Classification | Reasoning |
|---|---|---|---|---|
| `NewsTicker.xaml.cs` | `notifier.CollectionChanged +=` (source re-bind) | Yes — `_subscribedSource.CollectionChanged -=` runs before every re-subscribe | **Safe** | Explicit unsubscribe-before-resubscribe guard, correctly paired |
| `DashboardPage.xaml.cs` | `viewModel.RecentActivity`/`KpiMetrics.CollectionChanged +=` (on `DataContextChanged`) | Yes — both explicitly unsubscribed from `_subscribedViewModel` before resubscribing to the new one | **Safe** | Same guarded pattern as `NewsTicker` |
| `ReportingPage.xaml.cs` | `newViewModel.PropertyChanged +=` (on `DataContextChanged`) | Yes — `oldViewModel.PropertyChanged -=` runs first | **Safe** | Same guarded pattern |
| `LoginWindowViewModel.cs` | `MobileLogin.SignedIn += OnMobileSignedIn` (constructor) | No | **Safe** (re-assessed, not just re-counted) | Both `MobileOtpLoginViewModel` and `LoginWindowViewModel` are `AddTransient` (confirmed in DI), created and released together — the resulting reference cycle is not a .NET GC concern once nothing external holds either |
| `DashboardKpiCollection.cs` | `notifier.CollectionChanged += OnSourceCollectionChanged` (constructor) | **No — zero unsubscribe or `Dispose` anywhere in the file** | **Needs Review** — the one real candidate in this sweep | Constructed inside `KpiMetricsWithDerivedConverter` (a WPF value converter). Value converters can be re-invoked by the binding engine on every source-property update, not just once — if that happens here, **each invocation creates a new `DashboardKpiCollection` subscribed to the same longer-lived `source` collection**, with no way to ever unsubscribe the earlier ones. This is the one item in this sweep that could be a real, growing leak rather than a matched-lifetime false positive — its actual severity depends on how often WPF re-invokes this specific converter binding, which needs a binding-mode read (`Mode=OneWay` vs `OneTime`, `UpdateSourceTrigger`) before it can be downgraded to Safe or confirmed as Risk |

**Net finding: 4 of 5 are Safe on closer inspection; 1 (`DashboardKpiCollection`) is a genuine, still-open
Needs Review — this is the single highest-priority item in this whole plan**, since it's the only one
with a plausible mechanism for unbounded growth, not just a theoretical one.

---

## D. Cancellation Plan — Task 3

**Current state, re-confirmed:** every Application-layer service method already accepts
`CancellationToken cancellationToken = default` — the contract surface is complete everywhere. The gap
is exclusively on the Presentation side: only 2 of 71 ViewModels construct their own
`CancellationTokenSource` (`BookingWizardViewModel`, `ReportingPageViewModel`); everywhere else, a
`default`/`CancellationToken.None` is passed through, so nothing already in flight is ever actually
cancelled.

### High-value paths, assessed by real risk of stale/out-of-order results, not just efficiency

| Path | Current behavior | Risk if not cancelled | Where propagation should start |
|---|---|---|---|
| **Search** (`CommandPaletteViewModel`) | Confirmed: every method accepts a `CancellationToken` parameter, but no field stores a live `CancellationTokenSource`; every internal call site passes `default`/`CancellationToken.None` | **Highest** — a fast typist can trigger several overlapping `RefreshResultsAsync` calls; without cancelling the stale ones, an older, slower response can overwrite a newer, faster one's results on screen — a real correctness bug users could actually see, not just wasted work | A `CancellationTokenSource` field, cancelled and replaced at the start of each new keystroke-triggered `RefreshResultsAsync`, mirroring the debounce-and-cancel-previous pattern already proven in `BookingWizardViewModel` |
| **Booking** (`BookingPageViewModel.LoadAsync`, filter-triggered) | No token source; a filter change re-triggers `LoadAsync` with `default` | **Medium** — same overlapping-response risk as Search, lower frequency (filter changes are less rapid than keystrokes) | Same pattern: cancel-and-replace on each filter change |
| **Calendar** (availability load, day/week navigation) | No token source; rapid day/week navigation can queue overlapping `available-slots` calls | **Medium** — same stale-overwrite risk, triggered by rapid navigation clicks | Cancel-and-replace on each navigation action |
| **Dashboard loading** (`DashboardPageViewModel.LoadAsync`) | No token source; typically triggered once per page-open, not rapidly repeated | **Low** — real but infrequent; the main value here is being able to cancel an in-flight load if the user navigates away before it finishes, not overlapping-response correctness | Lower priority than the three above; propagate primarily for navigate-away cleanliness, not correctness |

**Recommended propagation shape, reusing the existing proven pattern, not inventing one:**
`BookingWizardViewModel`'s own `CancellationTokenSource` field (cancel-old, create-new, pass `.Token`
into the async call, dispose the old source) is already the exact shape this plan proposes extending
to Search, Booking's filter reload, and Calendar's navigation reload — no new cancellation pattern
needs designing, only its application to three more places.

---

## E. Startup UX Plan — Task 4

**Current blocking sequence, re-confirmed by reading `App.OnStartup` in full this turn (13 blocking
calls, in this order):**

1. `_host.StartAsync()`
2. Database migration (`dbContext.Database.MigrateAsync()`)
3. Theme service init
4. Localization service init
5. Device registration (`EnsureRegisteredAsync`)
6. API environment init
7. Session service init
8. *(session-resolution retry loop, itself synchronous)*
9. Certificate service init + conditional issuance
10. Sync queue service init
11. Notification seeding (2 calls)
12. Current session init (again, at line 541 — a second, later call path)

Every one of these runs on the UI thread before the main window becomes interactive, with **no visible
progress indicator today** — confirmed no splash/progress window construction found anywhere in this
sequence.

### Recommendations (identification only, no implementation)

| Recommendation | What it addresses | Candidate operations |
|---|---|---|
| **Progress indicator** | The single highest-value, lowest-risk change — a splash/progress window shown before `Host.CreateDefaultBuilder()...Build()` even starts, updated with a short status string per stage, closed once the main window is ready | All 13 stages — this doesn't need to make anything async, only visible |
| **Deferred loading** | Move genuinely optional, non-blocking-for-first-paint work off the critical path | Notification seeding (stage 11) looks like the strongest candidate — seeding sample/demo notifications is unlikely to need to complete before the user sees a window |
| **Background initialization** | For operations that don't gate what the user needs first (e.g. sync queue priming) | `SyncQueueService.InitializeAsync()` — worth checking whether anything in the first-paint path actually depends on the queue being primed yet, or whether it could start priming in the background while the window is already shown |

**Explicitly not recommended:** converting `OnStartup` itself to `async void` and awaiting through it —
the existing code's own comment already documents why this was deliberately avoided (WPF's
`Application.Run()` would resume pumping the Dispatcher before `OnStartup` finished, if it yielded at
an `await`). A progress window shown synchronously, with the existing blocking sequence still running
underneath it, achieves the visible-progress goal without touching that constraint.

---

## F. Priority Matrix — Task 5

| Item | Effort | Risk if deferred | Bucket |
|---|---|---|---|
| Add `ILogger<T>` to `MobileOtpLoginViewModel` | Small (mirrors existing pattern exactly) | Silent auth failures stay undiagnosable | **Quick win** |
| Add `ILogger<T>` to `DashboardPageViewModel` | Small | Silent failures on the highest-traffic page | **Quick win** |
| Add `ILogger<T>` to `CalendarPageViewModel` | Small | Silent failures on Calendar's sole remaining (availability-read) responsibility | **Quick win** |
| Give `AccountingPageViewModel` its own `ILogger<AccountingPageViewModel>` (not just the pass-through field) | Small | Revenue-page failures stay silent | **Quick win** |
| Resolve `DashboardKpiCollection`'s unmatched subscription (confirm converter re-invocation frequency, then either add unsubscribe/`Dispose` or confirm it's a non-issue) | Small investigation + small fix once diagnosed | The one plausible real leak in this whole audit, left unbounded | **Quick win** |
| Add `ILogger<T>` to `AnalyticsPageViewModel`/`ReportingPageViewModel` | Small ×2 | Lower-traffic silent failures | **Medium change** |
| Cancellation for `CommandPaletteViewModel` (Search) | Medium (new field + wiring, reusing `BookingWizardViewModel`'s proven shape) | Stale search results can visibly overwrite newer ones — a real, user-visible correctness issue | **Medium change** |
| Cancellation for Booking filter-reload and Calendar navigation-reload | Medium ×2 | Same stale-overwrite risk, lower frequency | **Medium change** |
| Startup progress indicator | Medium (new splash/progress window + status wiring through the existing 13-stage sequence) | Users perceive a slow/unreachable backend as a frozen app, not a loading one | **Medium change** |
| Cancellation for `DashboardPageViewModel.LoadAsync` | Small | Low real-world impact — infrequent, mostly a navigate-away cleanliness concern | **Future improvement** |
| Deferred/background-initialization split (notification seeding, sync queue priming) | Medium–Large (requires confirming no first-paint dependency exists before moving anything off the critical path) | Startup stays fully synchronous, but this is a latency optimization, not a correctness gap | **Future improvement** |
| Logging expansion to the remaining ~57 still-uncovered ViewModels beyond the 4 named above | Large (broad, repetitive, low-risk-per-file work) | Diagnosability debt accumulates slowly, not urgently | **Future improvement** |

---

## G. Implementation Order

Recommended sequencing for a future authorized implementation pass, grouped to minimize context
switching and match this engagement's own established "one concern per commit" discipline:

1. **Logging — Authentication, Dashboard, Calendar, Accounting** (4 Quick Wins, same pattern, same
   commit shape as `da18c18`/`ea03d83`) — highest value, lowest risk, ships first
2. **`DashboardKpiCollection` investigation + fix** — small, isolated, resolves the one real open leak
   question in this plan
3. **Cancellation — Search (`CommandPaletteViewModel`)** — the one cancellation item with a genuine
   correctness (not just efficiency) angle
4. **Cancellation — Booking filter-reload, Calendar navigation-reload** — same pattern, lower urgency
5. **Startup progress indicator** — independent of everything above, can be sequenced any time after
   item 1, no dependency either direction
6. **Remaining logging expansion + deferred/background startup work** — future improvements, no fixed
   timeline

Each item above should go through this engagement's own established
audit → scope-review → commit-execution rhythm individually, not as one bundled change — consistent
with every prior phase in this arc.

---

## STOP

Plan complete. No code changes performed.
