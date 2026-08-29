# ROJAN AI — TEAM 3 — PHASE 8.9 LOGGING COVERAGE HARDENING AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no DI change, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `94fca6a` (`fix(desktop): bound navigation back-stack depth` — confirmed via `git rev-parse HEAD`)
**Reference:** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §E/§F
**Objective:** Assess production logging coverage across ViewModels/services after Navigation BackStack
Hardening (Phase 8.6/8.8) landed.

Every claim below was verified against source this turn. Where a prior report's figure did not match
what the code shows, this document states the discrepancy and uses the measured value.

---

## A. Current Logging Architecture

### A.1 Sink — `LocalFileLoggerProvider`

`src/Rojan.Desktop.Infrastructure/Observability/LocalFileLoggerProvider.cs` — a single `ILoggerProvider`:

| Property | Value (from source) |
|---|---|
| Output | `%LocalAppData%\RojanDesktop\logs\rojandesktop-yyyy-MM-dd.log` |
| Rotation | One file per UTC day (filename carries the date) |
| Retention | 14 days; stale files pruned on construction (`TryPrepareDirectoryAndCleanUp`) |
| **Level filter** | **`IsEnabled(logLevel) => logLevel >= LogLevel.Warning`** — `Information`/`Debug`/`Trace` are dropped by this sink |
| Format | `{timestamp:O} [{Level}] {category}: {message}` (+ ` | {ExceptionType}: {ExceptionMessage}` when an exception is passed) |
| Failure mode | Every write and the retention sweep are wrapped; `IOException`/`UnauthorizedAccessException` are swallowed — logging can never escalate into a workflow failure |
| Read path | None. Write-only from the app's perspective — cannot become a second source of business truth |

### A.2 Registration & factory configuration

- **`src/Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:91`** —
  `services.AddLogging();` — the standard `Microsoft.Extensions.Logging` bootstrap. Registers
  `ILoggerFactory` and open-generic `ILogger<T>`, so **any** constructor parameter typed `ILogger<T>` is
  satisfied automatically by DI for every DI-resolved type.
- **`src/Rojan.Desktop.Shell/App.xaml.cs:63`** —
  `.ConfigureLogging(logging => logging.AddProvider(new LocalFileLoggerProvider()))` on the Generic Host
  builder. **Additive** to the Console/Debug providers `Host.CreateDefaultBuilder()` already wires — not
  a replacement.
- No `SetMinimumLevel`, no `appsettings`-based `Logging` section, no per-category filters anywhere in the
  codebase. Effective floor for the file sink is `Warning` (A.1); Console/Debug keep their host defaults
  (`Information`), but those sinks are dev-only (no console window in the packaged WPF app).

### A.3 Unhandled-exception surfaces (already complete — checkpoint §E, re-confirmed)

`App.xaml.cs` owns all three .NET last-resort surfaces, each routed through
`LogUnhandledException` (a `[LoggerMessage(Level = Error)]` partial) before recovering or accepting
termination:

| Surface | Handler | Behaviour |
|---|---|---|
| `DispatcherUnhandledException` | `OnDispatcherUnhandledException` | log Error → dialog → `e.Handled = true` (keep running) |
| `AppDomain.CurrentDomain.UnhandledException` | `OnUnhandledException` | log Error (process will terminate regardless) |
| `TaskScheduler.UnobservedTaskException` | `OnUnobservedTaskException` | log Error → `SetObserved()` |

### A.4 Established per-type logging pattern (the convention to follow)

Confirmed identical across all 4 self-logging ViewModels (`BookingPageViewModel`,
`PosCheckoutViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`):

```csharp
public sealed partial class XxxViewModel : ViewModelBase          // 'partial' for the source generator
{
    private readonly ILogger<XxxViewModel> _logger;

    public XxxViewModel(/* real deps */, ILogger<XxxViewModel>? logger = null)   // optional, last param
    {
        _logger = logger ?? NullLogger<XxxViewModel>.Instance;    // NullLogger fallback for direct-`new` tests
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Xxx operation failed. Operation={Operation}")]
    private partial void LogXxxFailed(Exception exception, string operation);   // allocation-free (CA1848)
}
```

Key points of the convention:
- **Optional `ILogger<T>? = null` + `NullLogger<T>.Instance` fallback** — DI injects the real logger in
  production; existing unit tests that `new` the ViewModel directly need no change.
- **`[LoggerMessage]` source-generated partials**, not `_logger.LogError(...)` — satisfies CA1848
  (allocation-free), matches `HttpApiClient` and `App`.
- **Levels used: `Error` for a swallowed failure, `Warning` for a permission denial.** Both clear the
  `Warning` floor, so both reach the file.
- Structured named parameters (`{Operation}`, `{SpecialistId}`) — never string interpolation into the
  template.

### A.5 `NullLogger<T>` convention

`Microsoft.Extensions.Logging.Abstractions` is a direct `PackageReference` in
`Rojan.Desktop.Presentation.csproj` (added Phase 7.4.1). `NullLogger<T>.Instance` is the universal
fallback — used by every current logger-bearing ViewModel, and is the reason adding a logger parameter is
a **non-breaking** change to every existing test.

---

## B. Coverage Measurement

### B.1 ViewModel population

| Measure | Value | Method |
|---|---|---|
| ViewModel classes total | **56** | `grep -rnE "public (sealed\|abstract\|partial )*(class\|record) [A-Za-z0-9_]+ViewModel"` over `src/**/*.cs` (excl. `obj`/`bin`) |
| — in `Rojan.Desktop.Presentation` | 55 | same |
| — in `Rojan.Desktop.Shell` | 1 (`MainWindowViewModel`) | same |

> **Discrepancy with checkpoint §E ("7 of 71 ViewModel files").** The population is **56 classes**, not
> 71. The "71" is not reproducible from the current tree by any counting method tried (class decls,
> `*ViewModel*.cs` filenames, `: ViewModelBase` implementers). Treat 56 as the live denominator; the "71"
> is stale. The **numerator "7"** is closer but also imprecise — see B.2.

### B.2 Logging adoption — measured

| Category | Count | Files |
|---|---|---|
| ViewModels that **emit their own log records** | **4** | `Bookings/BookingPageViewModel`, `Accounting/PosCheckoutViewModel`, `Specialists/SpecialistScheduleViewModel`, `Specialists/SpecialistAvailabilityViewModel` |
| ViewModels that **reference `ILogger<T>` only to pass a logger to a child ViewModel** (no self-logging) | 3 | `Accounting/AccountingPageViewModel` (→ `PosCheckoutViewModel`), `Specialists/SpecialistPageViewModel` (→ schedule/availability children), `Specialists/SpecialistProfileViewModel` (→ same children) |
| **Total files referencing `ILogger`** | **7** | (the checkpoint's "7") |
| ViewModels with **no `ILogger` reference at all** | **49** | everything else |

**Effective self-logging coverage: 4 / 56 ≈ 7%.** All 4 are outputs of the Phase 7.4 hardening arc
(`da18c18` booking/checkout, `ea03d83`/`53090c1` shift engine). No ViewModel outside that arc logs.

### B.3 Services / Infrastructure

| Component | Logging |
|---|---|
| `HttpApiClient` (`Infrastructure/Api`) | **Yes** — `ILogger<HttpApiClient>`, `[LoggerMessage(Level = Warning)]` on API request failures. Every HTTP-shaped failure already leaves a file-log trail |
| `App` (composition root) | **Yes** — `ILogger<App>`, unhandled-exception surfaces (A.3) |
| All other Application/Infrastructure services (`CurrentSessionService`, `SessionService`, `SyncQueueService`, `CertificateService`, `WorkflowSchedulerService`, `NotificationService`, `ApiEnvironmentService`, repositories, query/command services, …) | **No** `ILogger` reference. Most have their own narrower diagnostics or are pure/deterministic; none was flagged in any prior audit as a logging gap |

Scope note: the checkpoint's §F item is explicitly *ViewModel* logging coverage. Service-layer logging
is not in the Phase 8.2 candidate set and is not assessed as a gap here beyond this inventory.

---

## C. Missing Areas

### C.1 The real gap: swallowed broad-catch boundaries with no log

**32 ViewModels contain a broad `catch (Exception)`** (the deliberate, `#pragma warning disable CA1031`
"top-level load boundary" pattern used app-wide). Of those, **only 4 log before swallowing** → **28
ViewModels swallow an arbitrary exception into `ErrorMessage = exception.Message` + an `Error`/`Empty`
state with zero diagnostic record.**

For a non-`ApiException` failure (a mapping bug, a null reference, an unexpected enum, a serialization
fault), the **only** surviving evidence is `exception.Message` rendered on screen — no type, no stack, no
file entry. For an `ApiException`, `HttpApiClient` has already logged the HTTP failure at `Warning`, so
there is a partial trail, but without the ViewModel-level "which workflow / what was the user doing"
context.

### C.2 The four Phase 8.2 candidates — verified from source

| # | ViewModel | Has logger? | Failure surface (verified) | Existing trail |
|---|---|---|---|---|
| 1 | `Security/MobileOtpLoginViewModel` | **No** — ctor takes only `IAuthenticationService`, `IDelayScheduler` | 3 async network flows (`RequestCodeAsync`, `ResendCodeAsync`, `VerifyCodeAsync`), each with typed `catch`es ending in a generic `catch (ApiException) → Strings.Login_Error_Generic`. No `catch (Exception)` — a non-`ApiException` fault propagates to the Dispatcher surface (which does log) | Partial: `HttpApiClient` logs the HTTP failure at `Warning`; a hard fault is caught by `OnDispatcherUnhandledException`. The **auth-flow context** (which of request/resend/verify, phone in E.164) is not logged anywhere |
| 2 | `Dashboard/DashboardPageViewModel` | **No** | One `catch (Exception exception)` at `LoadAsync` (`DashboardPageViewModel.cs:284`) → `ErrorMessage = exception.Message; State = Error`. Auto-runs on construction (`_ = LoadAsync()` at :177) | None for a non-API fault. First screen after login |
| 3 | `Calendar/CalendarPageViewModel` | **No** | **Three** `catch (Exception exception)` boundaries — `InitializeAsync` (:213), `LoadDailyAvailabilityAsync` (:256), `LoadWeeklyAvailabilityAsync` (:296) — each → `ErrorMessage = exception.Message`. Re-triggered on every specialist/service/date/view-mode change | None for a non-API fault |
| 4 | `Accounting/AccountingPageViewModel` | **Carries** `ILogger<PosCheckoutViewModel>? _posCheckoutLogger` to hand to its child `PosCheckoutViewModel` (:215) — **does not log itself** | Two `catch (Exception exception)` boundaries — `LoadAsync` (:145) and the search handler (:175) — each → `ErrorMessage = exception.Message; State = Error` | None for its own boundary; the child `PosCheckoutViewModel` is fully covered |

**Confirmed:** the checkpoint's candidate list is directionally correct. Note the nuance on #4 —
`AccountingPageViewModel` already *has* an `ILogger<PosCheckoutViewModel>` parameter (pass-through only),
so the change there is "add its own `ILogger<AccountingPageViewModel>` and use it", not "introduce
logging from nothing".

### C.3 Beyond the four candidates

The same unlogged-broad-catch pattern is present in ~24 more ViewModels. Highest-traffic among them
(each a primary sidebar destination that auto-loads and reloads on filter changes):
`CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`,
`ReportingPageViewModel`, `AnalyticsPageViewModel`, `OrganizationPageViewModel`, `SalonPageViewModel`,
`AiCenterPageViewModel`, plus the 5 Automation tab ViewModels. These are a natural follow-on wave, not
part of this phase's recommendation (see E).

---

## D. Risk Matrix

Classification basis: **User impact** (does the user lose work / see a wrong result / get stuck?),
**Production-debugging difficulty** (can support diagnose a field report without this log?), **Frequency**
(how often does the unlogged path actually execute?).

| Area | User impact | Debug difficulty w/o log | Frequency | **Risk** |
|---|---|---|---|---|
| **Unhandled-exception surfaces** (App) | — | — | — | **Already covered** — not a gap |
| **`HttpApiClient` API failures** | — | — | — | **Already covered** — `Warning` trail exists |
| `MobileOtpLoginViewModel` — auth-flow context on OTP failure | Low (clear on-screen error; user retries) | Medium (HTTP failure *is* logged by `HttpApiClient`; missing piece is flow/step correlation) | Low–Medium (every failed login attempt) | **P2** |
| `DashboardPageViewModel` — swallowed load fault | Low (page shows Error state + Retry) | **High** for a non-API fault (nothing survives but `.Message` on screen) | Low (load succeeds in the normal case; first screen every session) | **P2** |
| `CalendarPageViewModel` — 3 swallowed load faults | Low (Error state + Retry) | **High** for a non-API fault | Low–Medium (reloads on every filter change) | **P2** |
| `AccountingPageViewModel` — 2 swallowed boundaries (own) | Low (Error state) | **High** for a non-API fault | Low | **P2** |
| ~24 other ViewModels with unlogged broad-catch (§C.3) | Low each | High for a non-API fault | Varies (some rarely opened) | **P3** (aggregate P2 — see E) |
| Application/Infrastructure services without `ILogger` (§B.3) | — | Low–Medium (most have narrower diagnostics or are deterministic) | — | **P3**, out of this phase's scope |

**No P0. No P1.** Every item is a *diagnostic-quality* gap: no crash, no data loss, no security exposure,
no incorrect business result is caused or hidden by the missing logs. The unhandled-exception safety net
(A.3) and `HttpApiClient`'s `Warning` trail (B.3) mean nothing fails *silently to the point of being
undiagnosable* today — the gap is **time-to-diagnose**, not **detectability**.

### D.1 Why not higher

- A swallowed exception still surfaces to the user as an `Error` state with a `Retry` — it is not a
  silent wrong-answer.
- Any exception that is *not* caught by a ViewModel boundary is caught and logged by the Dispatcher
  surface.
- API failures — by far the most common real-world failure class for this app — already have a `Warning`
  file trail from `HttpApiClient`.

### D.2 Why not lower (why P2, not P3, for the candidates)

- The broad `catch (Exception)` is specifically designed to catch the *unexpected* — exactly the class of
  fault where a stack trace is most valuable and where `.Message` alone is least sufficient.
- `Dashboard` and `Calendar` are among the most-loaded pages; a recurring field fault there with no log
  is a realistic support-escalation scenario.

---

## E. Recommended Next Hardening Phase

### E.1 Architecture confirmation (Task 4)

| Check | Result |
|---|---|
| Established `ILogger` pattern exists and is consistent | **Yes** — A.4, 4 ViewModels, identical shape |
| `NullLogger<T>` convention | **Yes** — A.5; makes the change non-breaking for existing tests |
| DI compatibility | **Yes** — `AddLogging()` already registers open-generic `ILogger<T>`; page ViewModels are `AddTransient` and will receive the real logger with no DI edit. `NullLogger` covers direct-`new` tests |
| Layer violation risk | **None** — `Microsoft.Extensions.Logging.Abstractions` is already a Presentation `PackageReference`; `DependencyDirectionTests` forbids only Infrastructure/Domain/Shell/EF Core, not logging abstractions. `ViewModelTestabilityTests` forbids only `System.Windows.Threading`/`Controls` — unaffected |
| Architecture tests impact | **Zero expected** — no rule touches logging |
| `[LoggerMessage]` / CA1848 | Pattern already compliant; new code follows it |

**No DI change and no architecture change is required to close this gap** — it is purely additive
per-ViewModel constructor + `[LoggerMessage]` partials.

### E.2 Recommended scope for the next phase (proposed "Phase 8.10 — ViewModel Logging Coverage, Wave 1")

**Priority order (highest value / lowest risk first):**

1. **`DashboardPageViewModel`** — single boundary, highest-traffic (loads every session), currently zero
   trail. Smallest change, clearest payoff.
2. **`CalendarPageViewModel`** — three boundaries, high-traffic, reloads constantly. Largest single-file
   diagnostic gain.
3. **`AccountingPageViewModel`** — two own boundaries; already imports the logging namespace (pass-through
   logger present), so the smallest incremental diff of the three page VMs.
4. **`MobileOtpLoginViewModel`** — auth entry point; add `Warning`-level logging of the flow/step on the
   generic `catch (ApiException)` fallthrough (not the typed, expected branches). Lower urgency because
   `HttpApiClient` already logs the HTTP failure — this only adds flow correlation.

**Estimated files:**

| File | Prod files | Test files | Notes |
|---|---|---|---|
| `Dashboard/DashboardPageViewModel.cs` | 1 | `DashboardPageViewModelTests.cs` (add assertions or leave; `NullLogger` keeps existing green) | class → `sealed partial`, +ctor param, +1 `[LoggerMessage]`, +1 log call |
| `Calendar/CalendarPageViewModel.cs` | 1 | `CalendarPageViewModelTests.cs` | +ctor param, +1–3 `[LoggerMessage]`, +3 log calls |
| `Accounting/AccountingPageViewModel.cs` | 1 | `AccountingPageViewModelTests.cs` | +own `ILogger<AccountingPageViewModel>` param alongside the existing pass-through one, +2 log calls |
| `Security/MobileOtpLoginViewModel.cs` | 1 | `MobileOtpLoginViewModelTests.cs` | +ctor param, +1 `[LoggerMessage]` (Warning), +1 log call in the `catch (ApiException)` branch |
| **Total** | **4 production files** | up to 4 test files | No DI file. No interface. No shared file |

**Implementation sequence (matches this engagement's rhythm):**

1. **Phase 8.10a — Scope Review** (readiness only): exact per-file diff plan, confirm each ViewModel's
   test currently `new`s it directly (so `NullLogger` fallback keeps tests green), pick `EventId`s,
   confirm one commit per file *or* one bundled commit (recommend **one bundled commit** — all four are
   the same mechanical change, single concern "ViewModel logging coverage wave 1").
2. **Phase 8.10b — Implementation**: apply the pattern to all 4, in the priority order above.
3. **Validate**: `dotnet build` (expect 0/0) + full suite (expect 2,512 + any new logging assertions) +
   architecture tests (expect 7/7 unchanged).
4. **Phase 8.10c — Commit Scope Review** → **Phase 8.10d — Commit Execution**: explicit-path staging of
   the ≤8 files, single isolated commit, `fix(desktop): add ViewModel logging coverage (wave 1)` or
   similar.
5. **Later waves** (separate phases, not now): the ~24 remaining unlogged broad-catch ViewModels (§C.3),
   grouped by module, 1 wave per commit.

### E.3 Explicitly out of scope for the next phase

- Service-layer logging (§B.3) — no prior audit flagged it; deterministic/pure services do not benefit.
- Lowering the `LocalFileLoggerProvider` `Warning` floor — deliberate (keeps the file small, keeps
  `Information` noise out); the recommended additions all log at `Warning`/`Error` and clear it.
- Any DI or `ConfigureLogging` change — not needed (E.1).

---

## STOP

Audit complete. No source modified, no logger added, no DI change, no commit, no push. Recommendation:
**Phase 8.10 — ViewModel Logging Coverage Wave 1** (4 production files, DI-free, priority order
Dashboard → Calendar → Accounting → MobileOtpLogin).
