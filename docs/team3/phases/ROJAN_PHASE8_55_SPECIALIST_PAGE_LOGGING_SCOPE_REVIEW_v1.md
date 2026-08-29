# ROJAN AI — TEAM 3 — PHASE 8.55 — SPECIALIST PAGE LOGGING (WAVE 2D / P1) — SCOPE REVIEW v1

**Type:** Readiness review only. **No source change. No test change. No logger / stub / DI change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `5b7f6ca157bf32906c2bfccfc29c7fcba39fd901` — `fix(desktop): add ViewModel diagnostic logging (detail panels)` (Phase 8.51, committed 8.53)
**Reference:** `ROJAN_PHASE8_54_REMAINING_VIEWMODEL_GAP_AUDIT_v1.md` §E/§F (this is the single P1 item), `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`.
**Verdict:** ✅ **READY TO IMPLEMENT.** Low risk. Static-form `[LoggerMessage]` mandatory (SYSLIB1020).

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `5b7f6ca157bf32906c2bfccfc29c7fcba39fd901` |
| HEAD subject | `fix(desktop): add ViewModel diagnostic logging (detail panels)` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. This review adds no code.

---

## B. ARCHITECTURE ANALYSIS — `SpecialistPageViewModel`

**File:** `src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs`

### B.1 Structure & lifetime

| Aspect | Value |
|---|---|
| Declaration | `public sealed class SpecialistPageViewModel : ViewModelBase` — **not `partial`** |
| `using` | `Microsoft.Extensions.Logging` present; **`Microsoft.Extensions.Logging.Abstractions` absent** (no `NullLogger` use yet) |
| Lifetime | Shell-registered `services.AddTransient<SpecialistPageViewModel>()`; one instance per navigation to the Specialists page |
| Constructor-time load | ctor ends with `_ = LoadAsync();` (safe fire-and-forget — self-catching) |
| Constructs a child | `SelectedSpecialist` setter (`:172–186`): `new SpecialistProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine, _serviceQueryService, _scheduleQueryService, _scheduleCommandService, _scheduleLogger, _availabilityLogger, _loggerFactory?.CreateLogger<SpecialistProfileViewModel>())` — Phase 8.51 plumbing |

### B.2 Constructor dependencies (order)

```
SpecialistPageViewModel(
    ISpecialistQueryService queryService,
    ISpecialistProfileQueryService profileQueryService,
    ISpecialistCommandService commandService,
    IIntelligenceEngine intelligenceEngine,
    IServiceQueryService serviceQueryService,
    ISpecialistScheduleQueryService scheduleQueryService,
    ISpecialistScheduleCommandService scheduleCommandService,
    ILogger<SpecialistScheduleViewModel>? scheduleLogger = null,        // grandchild logger — forwarded verbatim
    ILogger<SpecialistAvailabilityViewModel>? availabilityLogger = null,// grandchild logger — forwarded verbatim
    ILoggerFactory? loggerFactory = null)                               // added Phase 8.51 — used for SpecialistProfileViewModel
```

### B.3 Existing logger fields / `[LoggerMessage]`

| Item | Value |
|---|---|
| `_scheduleLogger` | `private readonly ILogger<SpecialistScheduleViewModel>? _scheduleLogger;` (`:46`) — passed to `new SpecialistProfileViewModel(...)` → grandchild `SpecialistScheduleViewModel` |
| `_availabilityLogger` | `private readonly ILogger<SpecialistAvailabilityViewModel>? _availabilityLogger;` (`:47`) — same path → `SpecialistAvailabilityViewModel` |
| `_loggerFactory` | `private readonly ILoggerFactory? _loggerFactory;` (`:48`, Phase 8.51) — used as `_loggerFactory?.CreateLogger<SpecialistProfileViewModel>()` at the child `new` |
| Own `ILogger<SpecialistPageViewModel>` | **none** |
| `[LoggerMessage]` methods | **none** |

**→ 2 typed `ILogger<T>` fields + 1 `ILoggerFactory` field. No `[LoggerMessage]`.**

### B.4 Catch boundaries — exactly 1

| Method | Site | Form | Current error handling | Instrument? |
|---|---|---|---|---|
| `LoadAsync` | `:250–259` | `#pragma warning disable CA1031` → `catch (Exception exception)` | Inside `if (requestVersion == _filterVersion)`: `ErrorMessage = exception.Message; State = DashboardState.Error;` (stale-result-guarded — a superseded load applies nothing) | **YES** — the single Category A boundary |
| `CreateSpecialistAsync` | `:282–294` | — | **no `try`/`catch`** — a failure propagates to `AsyncRelayCommand.Execute`'s `try/finally` → the app's global handler (recovered, never a crash) | **NO** — missing-guard, out of the logging track (same as other pages' create methods that predate their own hardening) |
| `OnProfileSpecialistUpdated` | `:312–321` | `async void` | calls `await LoadAsync().ConfigureAwait(true)` (self-catching); no own `catch` | **NO** — nothing to instrument |
| `ClearFilters` | `:267–280` | — | no I/O | **NO** |

---

## C. SYSLIB1020 RESOLUTION

### C.1 Current build risk

**None today** — `SpecialistPageViewModel` holds 2 `ILogger` fields but has **no `[LoggerMessage]`**, and `SYSLIB1020` only fires when a class contains **both** multiple `ILogger` fields **and** an **instance-form** `[LoggerMessage]` partial method. Build is currently clean (`5b7f6ca`, 0 warnings).

### C.2 Why adding `ILogger<SpecialistPageViewModel>` directly (instance form) is NOT acceptable

If instrumentation followed the standard child-VM shape used everywhere else (`sealed partial` + `private readonly ILogger<SpecialistPageViewModel> _logger;` + an **instance-form** `[LoggerMessage] private partial void LogOperationFailed(string operation);`):

- the class would then hold **3 `ILogger` fields** (`_scheduleLogger`, `_availabilityLogger`, `_logger`)
- **and** an instance-form `[LoggerMessage]`
- → the source generator emits **`SYSLIB1020` "Multiple logger fields are not permitted"**
- → `Directory.Build.props` sets `TreatWarningsAsErrors=true` → **the build fails.**

This is the exact constraint `AccountingPageViewModel` already hit (2 `ILogger` fields — `_logger` + `_posCheckoutLogger`), and it is why that class uses the **static form**.

### C.3 Recommended final pattern — **static-form `[LoggerMessage]`**

```
public sealed partial class SpecialistPageViewModel : ViewModelBase   // sealed -> sealed partial
{
    ...
    + private readonly ILogger<SpecialistPageViewModel> _logger;
    ...
    // in the ctor (NO new ctor parameter — derive from the ILoggerFactory it already takes):
    + _logger = loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance;
    ...
    // LoadAsync catch, last statement, inside the existing `if (requestVersion == _filterVersion)`:
            ErrorMessage = exception.Message;
            State = DashboardState.Error;
    +       LogOperationFailed(_logger, nameof(LoadAsync));
    ...
    + [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Specialist page operation failed. Operation={Operation}")]
    + private static partial void LogOperationFailed(ILogger logger, string operation);
}
```

| Property | Static form | Instance form |
|---|---|---|
| `SYSLIB1020` (multiple `ILogger` fields) | **not triggered** — the logger is a **method parameter**, field count is irrelevant | triggered → build fails |
| Precedent | `AccountingPageViewModel.LogOperationFailed(ILogger, string, Exception)` (`:195`); `App.LogUnhandledException` | every other child VM |
| Signature here | `(ILogger logger, string operation)` — **no `Exception` parameter** (operation-name-only rule, Phase 8.15+) — this **diverges from Accounting's legacy `(ILogger, string, Exception)`** on purpose | n/a |

`+ using Microsoft.Extensions.Logging.Abstractions;` for `NullLogger`.

### C.4 Why "derive `_logger` from `loggerFactory`" and not a new ctor param

- `SpecialistPageViewModel` **already takes `ILoggerFactory? loggerFactory = null`** (Phase 8.51). Deriving `_logger` from it adds **zero ctor-signature change**, no DI change, and is consistent with how it already obtains the child's logger.
- `AddLogging()` registers `ILoggerFactory`; DI injects the real factory in production → `_logger` is a real `ILogger<SpecialistPageViewModel>`. Tests that pass no factory → `NullLogger` (safe).
- **Documented alternative** (if the authorizer prefers explicit injection for test clarity): append one optional `ILogger<SpecialistPageViewModel>? logger = null` **after** `loggerFactory`, `_logger = logger ?? NullLogger<…>.Instance;`. Equally SYSLIB1020-safe with the static form. The factory-derived form is the recommendation.

---

## D. SECURITY REVIEW

### D.1 Sensitive data reachable at `LoadAsync`

| Source | Data |
|---|---|
| `_queryService.SearchSpecialistsAsync(BuildFilter())` result / the `SpecialistDto[]` being loaded | specialist `FullName`, `Title`, **`Email`**, **`Phone`**, `Bio`, `Status` |
| `BuildFilter()` (`SpecialistSearchFilter`) | the operator's typed `SearchText`, `SelectedSkill`, status filter |
| the caught `exception` | for an `ApiException` (from `AuthBootstrapHttpClient`), the **raw backend response body** in `.Message` |

### D.2 The rule

**ALLOWED in the log line:** `Operation=nameof(LoadAsync)` and nothing else.

**FORBIDDEN — must never appear:** specialist name / email / phone / bio / status, the search / skill / status filter text, backend response bodies, `Exception.Message`, the `Exception` object.

### D.3 How the recommended design guarantees it

| Guarantee | Mechanism |
|---|---|
| `Exception` object never passed | static `[LoggerMessage]` signature is `(ILogger logger, string operation)` — **no `Exception` parameter** |
| `Exception.Message` never logged | call site is `LogOperationFailed(_logger, nameof(LoadAsync))` — `nameof` only; the pre-existing `ErrorMessage = exception.Message` is unchanged UI behaviour, never routed to the logger |
| No `SpecialistDto` / filter data logged | message template is a constant with one `string` argument |
| Test-enforced | seed a recognizable secret (specialist email + phone + bio + a fake search term) into the thrown exception; assert `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=LoadAsync", entry.Message)` |

Level `Error` (clears the `LocalFileLoggerProvider` `Warning` floor). `EventId = 1`.

---

## E. TEST PLAN

### E.1 Existing coverage & infra (no changes needed)

- `tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs` — exists, ~25 tests, all construct `new SpecialistPageViewModel(...)` inline (no `MakeSut` helper), namespace `Rojan.Desktop.Presentation.Tests.Specialists` → **`RecordingLogger<T>` and `RecordingLoggerFactory` are directly in scope**.
- `StubSpecialistQueryService` — its `SearchSpecialistsAsync(SpecialistSearchFilter)` overload delegates to `_searchSpecialistsByFilter`, which **defaults to `(_, ct) => _getSpecialists(ct)`**. So `new StubSpecialistQueryService(_ => Task.FromException<IReadOnlyList<SpecialistDto>>(...))` makes `LoadAsync`'s filter query throw. **No stub change.**
- The existing `LoggerFactory_ForwardedToSpecialistProfileChild_…` test (Phase 8.51) uses a passing `queryService` → `SpecialistPageViewModel.LoadAsync` succeeds → it will emit **no** page-level log → its `Assert.Single(loggerFactory.Entries)` **stays valid** (only the child's entry). No regression.

### E.2 New tests (~3)

| # | Test | Asserts |
|---|---|---|
| 1 | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` | construct with a throwing filter query (seeded secret `"Jordan Lee / jordan.lee@rojan.example / 555-0100 / balayage bio"`) and a `RecordingLoggerFactory`; assert `State == DashboardState.Error`; assert one `Error` entry whose category contains `SpecialistPageViewModel`, message contains `Operation=LoadAsync`, and `DoesNotContain` the secret. *(If the implementation uses the optional-`ILogger` alternative, use `RecordingLogger<SpecialistPageViewModel>` and `Assert.Single(logger.Entries)` instead.)* |
| 2 | `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` | construct with a throwing filter query and **no** logger factory; assert `State == DashboardState.Error`, `ErrorMessage == "boom"`, no exception thrown |
| 3 | `LoadAsync_StaleResult_LogsNothing` *(optional — guards the `requestVersion` check)* | drive two overlapping loads where the first (slow, failing) completes after `_filterVersion` moved on; assert the stale failure emits **no** log entry (the log call sits inside the `if (requestVersion == _filterVersion)` block) |

### E.3 Behaviour preservation — verified by test

- Every existing `SpecialistPageViewModelTests` test passes unchanged (directory load/empty/error states, filter/search, auto-selection, `SelectedSpecialist` setter constructing the child + wiring `SpecialistUpdated`, `CreateSpecialistAsync`, `ClearFilters`, `OnProfileSpecialistUpdated` reload).
- `_scheduleLogger` / `_availabilityLogger` / `_loggerFactory` forwarding to `SpecialistProfileViewModel` unchanged — the Phase 8.51 `LoggerFactory_ForwardedToSpecialistProfileChild_…` test still passes.

### E.4 Architecture tests

7 / 7 unaffected — Presentation-only edit; `Microsoft.Extensions.Logging(.Abstractions)` already Presentation `PackageReference`s (allowed by `DependencyDirectionTests`); no `System.Windows.Threading` / `System.Windows.Controls`.

---

## F. COMMIT READINESS

### F.1 Scope — 2 files

| # | File | Change |
|---|---|---|
| 1 | `src/…/ViewModels/Specialists/SpecialistPageViewModel.cs` | `sealed class` → `sealed partial class`; `+ using Microsoft.Extensions.Logging.Abstractions;`; `+ private readonly ILogger<SpecialistPageViewModel> _logger;`; in ctor `+ _logger = loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance;` (no new ctor param); 1 **static-form** `[LoggerMessage]` `private static partial void LogOperationFailed(ILogger logger, string operation)`; 1 call `LogOperationFailed(_logger, nameof(LoadAsync));` as the last statement of the `LoadAsync` catch (inside the existing `if (requestVersion == _filterVersion)`). **Untouched:** `_scheduleLogger`, `_availabilityLogger`, `_loggerFactory`, the `SelectedSpecialist` setter's `new SpecialistProfileViewModel(...)` call, `CreateSpecialistAsync`, `OnProfileSpecialistUpdated`, `ClearFilters`. |
| 2 | `tests/…/Specialists/SpecialistPageViewModelTests.cs` | +1 `using Microsoft.Extensions.Logging;` (for `LogLevel`); +~3 tests (E.2). No existing test body changed. |

**No new files. No DI / interface / DTO / shared-stub change. No `SpecialistProfileViewModel` / grandchild change.**

### F.2 Not touched

`SpecialistProfileViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`, `StubSpecialistQueryService`, `StubSpecialistCommandService`, `StubSpecialistProfileQueryService`, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, DI registration, Domain, Infrastructure, Shell, Application, backend contracts, RBAC, authentication, navigation, the P2 legacy-`[LoggerMessage]` VMs (§D.3 of the Phase 8.54 audit — separate future phase).

### F.3 Validation gates (before and after commit)

```
dotnet build -c Debug   → 0 warnings / 0 errors   (watch SYSLIB1020 — the static form prevents it)
dotnet test  -c Debug   → 2,606 + ~3 = ~2,609 / all pass
architecture tests      → 7 / 7
```

Expected test-count delta: **+3** (≈2,606 → ≈2,609). Coverage: self-logging **32/55 → 33/55**.

### F.4 Risk assessment

| Risk | Level | Mitigation |
|---|---|---|
| `SYSLIB1020` on instrumentation | **fully avoided** | static-form `[LoggerMessage]` (§C.3) — build gate confirms |
| Regressing the Phase 8.51 child-forwarding | **none** | `_loggerFactory` / grandchild-logger forwarding untouched; existing forwarding test unaffected |
| Ctor-signature churn breaking call sites / tests | **none** | factory-derived `_logger` — **no ctor param added**; ≥25 existing `new SpecialistPageViewModel(...)` sites compile unchanged |
| Logging a stale/superseded load | **avoided** | call sits inside the existing `if (requestVersion == _filterVersion)` guard; test E.2 #3 asserts it |
| Behaviour change in `LoadAsync` error surfacing | **none** | log call appended strictly after the unchanged `ErrorMessage`/`State` assignment |
| Constructor-time load fires the log during `new` | **by design** | identical to every prior page-VM wave; tests construct with a recorder and assert one entry |

### F.5 Commit plan

**One isolated commit.**
- Subject (exact): `fix(desktop): add ViewModel diagnostic logging (specialist page)`
- Staging: `git reset` → 2 explicit `git add <path>` (never `git add .` / `-A`).
- Trailers: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` + `Claude-Session: …`
- No push / merge / rebase / amend.

This is the **last P1 item**; after it lands, the checkpoint's §F "logging coverage: final" statement (Phase 8.54 §F.1) can be recorded and the logging track closed. The P2 legacy-`[LoggerMessage]`-harmonization remains a separately-scoped future option, not blocking.

---

## STOP

Scope review complete. No source or test change, no logger/stub/DI change, no commit/push/merge/rebase/amend.
HEAD remains `5b7f6ca`. **Awaiting Phase 8.56 implementation authorization.**
