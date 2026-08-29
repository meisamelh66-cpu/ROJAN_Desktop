# ROJAN AI — TEAM 3 — PHASE 8.1 DESKTOP PRODUCTION EXCELLENCE AUDIT — REPORT v1

**Type:** Audit only. No source file modified, no refactor, no feature added, no architecture changed,
no commit. `HEAD` (`801cc65`) unchanged before and after — confirmed by `git status`, which shows only
this report and its siblings as untracked additions, zero tracked-file changes.

---

## A. Executive Summary

This audit goes one level deeper than Phase 7.5's own final audit — not re-stating its conclusions,
but independently probing performance, DI lifetimes, memory-leak surface, and deployment
configuration that report didn't cover. **Two genuinely new findings this pass, both concrete and
verified against real source, not inferred:**

1. **Global exception handling is more thorough than previously documented in this arc:** `App.xaml.cs`
   wires up all three .NET unhandled-exception surfaces — `AppDomain.UnhandledException`,
   `TaskScheduler.UnobservedTaskException`, and `DispatcherUnhandledException` — each logging through
   the real file-backed logger before the UI-thread one shows a recoverable dialog. This was
   understated in earlier phases, which only ever cited the Dispatcher handler.
2. **Logging *coverage* has a real, quantifiable gap the infrastructure itself doesn't have:** the
   production logging pipeline (`LocalFileLoggerProvider`, daily-rotated, 14-day retention, fail-safe)
   is solid — but only **7 of 71 ViewModel files (10%)** actually construct an `ILogger<T>` at all.
   The other 90%, including Calendar, Customers, Dashboard, Automation, AI, Analytics, and the entire
   Booking Wizard, have zero logging capability today.

No P0-severity finding emerged. Release configuration builds clean (0 warnings, 0 errors). No
`AddScoped` lifetime mismatch exists anywhere (this app correctly uses only Singleton/Transient — it
has no request-scope concept to misuse). Zero `TODO`/`FIXME`/`HACK` markers exist anywhere in `src/`.

---

## B. Performance — Task 1

| Check | Classification | Finding |
|---|---|---|
| Async usage | **PASS** | The one `.Result` usage found (`GlobalSearchIndexService`, 5 call sites) is safe — every task is `await Task.WhenAll(...)`-completed on the line immediately before, so `.Result` only reads an already-finished task, never blocks |
| UI thread blocking | **WARNING** | `App.xaml.cs`'s startup sequence uses `.GetAwaiter().GetResult()` 13 times (session init, certificate issuance, device registration, sync queue init, etc.) — a deliberate, documented choice ("this method must stay synchronous end-to-end... WPF's `Application.Run()`... if `OnStartup` were async and yielded at an `await`, control would return to `Application.Run()`"), not an oversight. Real consequence: a slow/unreachable backend at startup delays the app becoming interactive, with no visible progress indicator during that window — worth a future UX pass, not a correctness bug |
| Long-running operations | **PASS** | No synchronous file/network I/O found outside the documented startup sequence above |
| `CancellationToken` usage | **WARNING** | Every Application-layer service interface accepts `CancellationToken cancellationToken = default` consistently — but only 8 of 71 Presentation ViewModel files reference `CancellationToken` at all, and only 2 (`BookingWizardViewModel`, `ReportingPageViewModel`) construct their own `CancellationTokenSource`. In practice, most UI-triggered operations pass the implicit `default`, so cancellation exists in the API surface but isn't meaningfully wired end-to-end from most pages — navigating away mid-operation doesn't actually cancel the in-flight call anywhere except those two |
| Memory leak risks | **WARNING** | 5 files subscribe to an event (`+=`) without a matching `-=` in the same file: `DashboardKpiCollection` (source `CollectionChanged`), `LoginWindowViewModel` (`MobileLogin.SignedIn`), and 3 view code-behind files. In every case checked, the event source is owned/composed by the subscriber (matched lifetime, not an externally-held long-lived singleton), so real leak exposure looks low — but none of these have an explicit `Dispose`/unsubscribe pattern, so this is worth a deliberate look, not a proven leak |

---

## C. Reliability — Task 2

**Global exception handling: real and thorough, re-verified by reading the full handler chain, not
just its registration:**

| Surface | Handler | Behavior |
|---|---|---|
| UI thread | `DispatcherUnhandledException` | Logs via `LogUnhandledException` (real `[LoggerMessage]`), shows an error dialog to the user, sets `e.Handled = true` — recovers, does not crash |
| Non-UI thread (fatal) | `AppDomain.CurrentDomain.UnhandledException` | Logs before the CLR terminates the process — correctly documented as unable to prevent termination, only to ensure the failure is recorded first |
| Unobserved `Task` faults | `TaskScheduler.UnobservedTaskException` | Logs and calls `e.SetObserved()` — prevents the older .NET finalizer-thread crash behavior |

**ViewModel exception paths:** the `DashboardState`(`Loading`/`Loaded`/`Empty`/`Error`)/`DashboardWidget`
pattern remains the one consistent convention across every page ViewModel that has one, unchanged by
this audit's findings.

**Logging coverage — the most significant Task 2 finding, quantified precisely:**

```
$ grep -rl "ILogger<" ViewModels/  → 7 files
$ find ViewModels/ -name "*.cs"    → 71 files
```

The 7 covered files are exactly the ones hardened in Phase 7.4.1 (Shift Engine) and 7.4.4/7.4.6
(Booking/Checkout) — the other **64 ViewModels (90%) have no logging at all**, including
`CalendarPageViewModel`, `CustomerPageViewModel`/`CustomerProfileViewModel`, `DashboardPageViewModel`,
every `AI`/`Analytics`/`Automation` ViewModel, and the entire `BookingWorkflow` (Booking Wizard) slice.
A caught exception in any of these 64 today sets `ErrorMessage`/`State` correctly (the UX is fine) but
leaves zero diagnostic trace — the exact gap Phase 7.4.1 first identified and started closing, one
vertical slice at a time, still ~90% open.

**Missing production diagnostics, concretely:** the production logging pipeline itself
(`LocalFileLoggerProvider`) is solid (§F) — the gap is adoption, not infrastructure.

**User recovery paths:** every hardened ViewModel (the same 7) sets `ErrorMessage` without clearing
user input on failure, confirmed in earlier phases and unchanged; retry is generally "re-invoke the
same command," not a dedicated Retry button, consistent throughout the app.

---

## D. DI & Service Lifetime Review — Task 3

| Check | Finding |
|---|---|
| Singleton correctness | Every repository/Application service is `AddSingleton` — correct for a desktop app with no per-request concept; confirmed no domain-authoritative service is accidentally `Transient` (which would be harmless but wasteful) or shared incorrectly |
| Scoped dependencies | **Zero `AddScoped` calls exist anywhere in this codebase** — confirmed by grep. This app never introduced a scope concept it would then have to manage correctly, so there is no scope-mismatch risk to find |
| Transient usage | Page ViewModels are consistently `AddTransient` — a fresh instance per navigation, matching this app's own documented architecture convention (`docs/architecture/01-desktop-shell.md §4`, cited in the Presentation DI file's own doc comment) |
| Disposable services | 5 classes implement `IDisposable`: `AuthBootstrapHttpClient`, `WorkflowSchedulerService`, `DispatcherToastDismissScheduler`, `DispatcherDelayScheduler`, plus the `IConnectivityService` interface. **Confirmed properly disposed**: `App.OnExit` calls `_host.StopAsync(stopTimeout)` (bounded 5-second timeout) followed by `_host.Dispose()`, which disposes every registered `IDisposable` singleton through the standard Generic Host disposal chain — not left to process teardown by accident |
| No lifetime mismatch | Confirmed — no `Singleton` service was found depending on a shorter-lived `Transient` one in a way that would capture stale state (the common DI footgun); the one place a longer-lived object holds a reference to a per-navigation ViewModel (`LoginWindowViewModel` → `MobileOtpLoginViewModel`) is itself also `Transient`, so both are recreated together, not a mismatch |

---

## E. UX State Review — Task 4

Re-verified directly against current source, all four flows:

| Flow | Loading | Empty | Error | Retry |
|---|---|---|---|---|
| **Authentication** | `IsBusy` flag disables commands and (per `LoginWindow`'s XAML convention) shows a busy indicator during OTP request/verify | N/A — a login form has no "empty" concept | `ErrorMessage` set per-scenario (invalid input, rate-limited, network, generic, invalid/expired code, inactive user) — the most granular error surface in the app, hardened further in `801cc65` | Re-invoking `RequestCodeCommand`/`VerifyCodeCommand` is the retry path; `ResendCodeCommand` is a first-class, backend-distinct retry action with its own cooldown |
| **Booking** | `DashboardState.Loading` on `LoadAsync` | `DashboardState.Empty` when no bookings match the current filter | `DashboardState.Error` — now covering all 5 command methods since `da18c18`, not just the original load path | Re-invoking the same command; form fields deliberately preserved on failure so retry doesn't require re-typing |
| **Calendar** | `DashboardState.Loading` on availability fetch | `DashboardState.Empty` for a day/week with no slots | `DashboardState.Error` on a failed `available-slots` call | Re-invoking the load command; no local fallback exists any more (by design, per `7103647`) — a failed Calendar load has nothing local to fall back to, correctly, since Backend is its sole authority |
| **Shift Engine** | `DashboardState.Loading` on schedule fetch | `DashboardState.Empty` for a specialist with no configured shifts | `DashboardState.Error`, now reachable at all only since the DI fix (`53090c1`) — before that, the page couldn't even resolve | Re-invoking the load command; the one documented gap (Phase 7.3.2's own finding, still true) is that a **permission-denied** state is indistinguishable from a generic error in this flow specifically, since its gate's exception type isn't yet special-cased in the ViewModel the way `SpecialistScheduleCommandServicePermissionGate` throwing is elsewhere |

All four flows share the one consistent `DashboardState`/`DashboardWidget` pattern — no flow invented
its own bespoke state machine.

---

## F. Code Quality — Task 5

| Check | Finding |
|---|---|
| TODO/FIXME | **Zero** — confirmed via `grep -rniE "// *(TODO|FIXME|HACK|XXX)\b" src/`, no match anywhere |
| Dead code | The retained `Fake*`/`Ef*` implementations (e.g. `FakeCalendarRepository`, `FakeInventoryRepository`) are deliberate, documented, and either unreferenced-but-kept (matching this codebase's own stated "retire in place, never delete" convention) or actively serving test coverage — not accidental dead code |
| Duplicate services | None found — every `Fake`/`Backend` pair is a real interface substitution, not a parallel duplicate implementation being both live at once |
| Deprecated patterns | None found — `async void` is used only where C#/WPF requires it (the one `AsyncRelayCommand.Execute` implementation, one WPF event handler override `OnExit`, one WPF event handler `OnProfileSpecialistUpdated`); no obsolete API usage, no legacy `BackgroundWorker`/`Thread`-based async pattern anywhere |

---

## G. Deployment Readiness — Task 6

| Check | Finding |
|---|---|
| Configuration handling | `ApiEnvironmentService` (Infrastructure) owns backend base-URL resolution; no hardcoded environment-specific URL found scattered elsewhere |
| Environment separation | Confirmed via `ApiEnvironmentService`'s own role — a single, real seam for switching backend targets, not duplicated per-caller |
| Logging configuration | **Real and production-grade**, verified by reading the full implementation: `Host.CreateDefaultBuilder()` (Console/Debug/EventLog providers by default) **plus** an explicit, custom `LocalFileLoggerProvider` — writes to `%LocalAppData%\RojanDesktop\logs\rojandesktop-yyyy-MM-dd.log`, daily-rotated, 14-day retention, and every write (and the directory-prep/cleanup step) is wrapped so a disk/permissions failure is silently dropped rather than turning a logging failure into a workflow failure. Explicitly documented as write-only/diagnostic-only — never a second source of business truth |
| Release build readiness | **Confirmed this turn**: `dotnet build RojanDesktop.sln -c Release` → **0 Warnings, 0 Errors** |

---

## H. Scorecard — Task 7

| Dimension | Score | Basis |
|---|---|---|
| **Architecture** | **9/10** | Confirmed at Phase 7.5: zero local database authority, zero local permission authority for backend-connected domains, clean Singleton/Transient-only DI model, no scope misuse. −1 for the still-legacy `IPermissionGate` gates on 6 not-yet-backend-connected domains (correct for now, but a known, sequenced follow-up) |
| **Security** | **9/10** | Backend-sourced permissions, fail-closed `Unknown` role, no bypass paths, credential/token flow untouched and correct throughout this arc. −1 for the still-open Accounting double-charge-on-retry risk (Phase 7.4.4, unresolved) |
| **Reliability** | **7/10** | Genuinely thorough global exception handling (3 surfaces, all logging, none crash silently) and a solid production logging pipeline — but only 10% of ViewModels actually use it, and cancellation support is similarly thin outside 2 flows. Nothing found is broken; a meaningful amount of designed-in resilience simply isn't adopted everywhere yet |
| **Maintainability** | **9/10** | Zero TODO/FIXME, zero dead-code accumulation, one consistent DashboardState/DI/logging convention applied everywhere it's been applied, comprehensive and current doc-comment discipline throughout. −1 for the logging-adoption gap itself being a maintainability debt (future incidents in the 90% will be harder to diagnose) |
| **Production Readiness** | **8/10** | The backend-connected surface (Auth/Booking/Calendar/Shift Engine/RBAC) is genuinely ready — clean Release build, full test suite passing, no P0 anywhere. Held back from higher only by the same two already-known items: Inventory/Accounting/HR still pending backend contracts, and the Accounting payment-retry risk |

---

## I. Recommendations

1. **Extend `[LoggerMessage]` logging to the remaining 64 ViewModels**, prioritizing by traffic/risk —
   Customers and Dashboard first (highest-traffic pages), Booking Wizard next (multi-step flow with
   the most opportunities to fail silently today).
2. **Add a startup progress indicator** covering the `GetAwaiter().GetResult()` initialization sequence
   in `App.xaml.cs`, so a slow backend at launch is visibly "loading," not apparently frozen.
3. **Resolve the Accounting `ChargeAsync` double-charge-on-retry question** with the backend team
   before Accounting's own backend integration begins — already tracked, restated here as still open.
4. **Wire cancellation through at least the highest-traffic list/search views** (Customers, Bookings,
   Specialists) so navigating away mid-load actually cancels the in-flight call, not just abandons it.
5. **Add explicit unsubscribe/`Dispose` to the 5 flagged event-subscribing files**, even though current
   lifetime analysis suggests low real risk — cheap insurance against a future refactor changing one
   side's lifetime without the other.

None of the above is a blocker — all five are incremental hardening in the same spirit as the
Phase 7.4.x arc, not a course correction.

---

## STOP

Audit complete. No implementation performed.
