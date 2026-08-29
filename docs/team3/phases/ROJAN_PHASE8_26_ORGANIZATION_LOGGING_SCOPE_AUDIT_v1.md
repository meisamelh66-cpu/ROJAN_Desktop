# ROJAN AI — TEAM 3 — PHASE 8.26 ORGANIZATION PAGE LOGGING — SCOPE AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no tests added, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `2ed685a` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_22_LOGGING_WAVE2B_SCOPE_AUDIT_v1.md` §B/§F

Every fact verified against source this turn.

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `2ed685ac73636e07a828d8b55dd1a5221dc09657` |
| Branch | `feature/team3-desktop-completion` |
| Working tree | **clean** — 0 modified/deleted tracked files |
| Existing modifications | none |
| Untracked | `.md` reports only |

`git status --porcelain` (tracked) → empty. **Tracked tree clean.** ✅

---

## B. ViewModel Analysis (Task 2)

### B.1 Current state — `src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs` (586 lines)

| Aspect | Verified |
|---|---|
| Class | `public sealed class OrganizationPageViewModel : ViewModelBase` (not `partial`) |
| Namespace | `Rojan.Desktop.Presentation.ViewModels.Organizations` |
| Existing logger | **None** — no `ILogger` reference anywhere in the file |
| Constructor (`:84`) | `OrganizationPageViewModel(IOrganizationQueryService queryService, IOrganizationCommandService commandService, IPermissionEngine permissionEngine, Rojan.Desktop.Presentation.Organizations.ICurrentSessionService currentSessionService)` — **4 dependencies** |
| Constructor behaviour | builds a static `WorkspaceRole → Permission` reference grid via `_permissionEngine.GetPermissions(role)`; reads `_currentSessionService.CurrentRole` once (for `_selectedRoleToSwitchTo`); wires 6 commands; `_ = LoadAsync()` fire-and-forget at `:110` |
| Auto-load | `_ = LoadAsync();` in the constructor (safe fire-and-forget — `LoadAsync` catches internally) |

### B.2 Catch boundaries

| Method | Line | `catch (Exception)`? | Current handling | In scope? |
|---|---|---|---|---|
| `LoadAsync` | `:411` | **YES** — `#pragma warning disable CA1031` broad catch | `ErrorMessage = exception.Message; State = DashboardState.Error;` | **YES — the one insertion point** |
| `LoadBranchesForSelectedOrganizationAsync` | `:428` | No | no try/catch — a throw propagates to its awaiter (`LoadAsync` — so covered by that catch), or (from a setter's `_ = ...`) becomes an unobserved task exception → `App`'s `TaskScheduler.UnobservedTaskException` surface | No |
| `LoadSettingsForSelectedBranchAsync` | `:450` | No | same | No |
| `CreateOrganizationAsync` | `:485` | No | no try/catch — a throw becomes an unobserved task exception (AsyncRelayCommand fire-and-forget) | No — *missing guard*, not a swallowed catch |
| `CreateBranchAsync` | `:498` | No | same | No |
| `SaveBranchSettingsAsync` | `:521` | No | same (→ `StatusMessage = Strings.Organizations_SettingsSaved` on success only) | No |
| `SwitchRoleAsync` | `:579` | No | same | No |

**Only `LoadAsync` (`:411`) is a swallowing `catch (Exception)` — exactly one log insertion point.** The
uncaught write/loader methods are a *missing-guard* concern (a separate future error-handling phase, same
as `AccountingPageViewModel.CancelInvoiceAsync` in §F of the checkpoint), not a logging gap.

### B.3 User-visible state handling (must stay unchanged)

- `State` (`DashboardState` — Loading/Empty/Loaded/Error), rendered by `DashboardWidget`.
- `ErrorMessage` (string, shown in the Error state).
- `StatusMessage` (set only on a successful `SaveBranchSettingsAsync`).
- `LastUpdated` timestamp.

Only `State` + `ErrorMessage` are touched in the `LoadAsync` catch. The change appends one log call
**after** those two lines.

### B.4 Exact logging insertion point

```csharp
// LoadAsync, line ~411-416 — AFTER the two unchanged lines:
    ErrorMessage = exception.Message;      // unchanged
    State = DashboardState.Error;          // unchanged
    LogOperationFailed(nameof(LoadAsync)); // ADD

// plus, once, in the class body:
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Organization page operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);
```

Plus: `sealed`→`sealed partial`; `+using Microsoft.Extensions.Logging;` `+using Microsoft.Extensions.Logging.Abstractions;`;
`+private readonly ILogger<OrganizationPageViewModel> _logger;`; ctor `+ILogger<OrganizationPageViewModel>? logger = null`
(5th, optional, last); `_logger = logger ?? NullLogger<OrganizationPageViewModel>.Instance;`.

**Total: 1 log call site.**

---

## C. Security Review (Task 3)

### C.1 What flows through the `LoadAsync` try block

`GetOrganizationsAsync` → `OrganizationDto` (name, **legal name**, **tax information**, code, phone,
email, address). Then `LoadBranchesForSelectedOrganizationAsync` → `BranchDto` + `GetBranchSettingsAsync`
→ `BranchSettingsDto` (business hours, working days, **VAT percentage**, **receipt header/footer text**,
appointment rules, notification settings).

### C.2 Safe logging approach — identical to Wave 2A/2B

**Operation name only. The `Exception` is NOT passed to the `[LoggerMessage]` method.**

The produced line is always exactly:
```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Organizations.OrganizationPageViewModel: Organization page operation failed. Operation=LoadAsync
```

| Prohibited item | In any log line? | Why not |
|---|---|---|
| **Organization private data** (name/legal name/code/phone/email/address) | **No** | `LogOperationFailed(string operation)` has no data parameter; the exception is never passed |
| **Tax information** (`OrganizationDto.TaxInformation`) | **No** | same — never referenced by any log call |
| **VAT data** (`BranchSettingsDto.VatPercentage`) | **No** | same |
| **Receipt content** (`ReceiptSettingsDto.HeaderText`/`FooterText`) | **No** | same |
| **Employee data** | **No** | this page has no employee data |
| **Customer data** | **No** | this page has no customer data |
| **Backend responses** | **No** | only carried by `Exception.Message`, which is never passed |
| **`Exception.Message`** | **No** | the `LogOperationFailed(...)` call takes only `nameof(LoadAsync)` |

`IPermissionEngine` (role→permission grid) is used only in the constructor, far from the catch, and is
RBAC reference data — not sensitive — and is not referenced by any log call regardless.

**No sensitive-data logging risk.** ✅

---

## D. Architecture Impact (Task 4)

| Check | Result |
|---|---|
| `ILogger<T>` injection possible | **Yes** — `services.AddTransient<OrganizationPageViewModel>()` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:73`); `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) registers open-generic `ILogger<T>`; DI fills the new optional 5th param automatically |
| No DI changes required | **Confirmed** — the `AddTransient` line is unchanged; an optional ctor param needs no registration edit |
| No interface changes | **Confirmed** — `IOrganizationQueryService` (5 members), `IOrganizationCommandService` (5 members), `IPermissionEngine`, `ICurrentSessionService` all untouched; the change is entirely inside the concrete class |
| No Domain changes | **Confirmed** — Presentation-layer only; no business rule, permission decision, backend call, or data-authority change. `Booking` / `Calendar` authority / `Shift Engine` / `RBAC` / `Authentication` / `Navigation` all untouched |
| No backend contract changes | **Confirmed** — no API client, DTO, or contract touched |
| `DependencyDirectionTests` | `Microsoft.Extensions.Logging.Abstractions` not forbidden (only Infrastructure/Domain/Shell/EF); already a Presentation `PackageReference` |
| `ViewModelTestabilityTests` | no `System.Windows.Threading`/`Controls` dependency added |
| Architecture suite | **7/7 expected unchanged** |
| `SYSLIB1020` (multi-logger) | Not a risk — one `ILogger` field |

---

## E. Test Strategy (Task 5)

### E.1 Existing coverage

**`OrganizationPageViewModelTests.cs` does not exist.** Verified: the only reference to
`OrganizationPageViewModel` anywhere under `tests/` is `Rojan.Desktop.Shell.Tests/Navigation/NavigationServiceTests.cs`,
which constructs it as a nav target (via `new OrganizationQueryService(new FakeOrganizationRepository())`,
a nested `StubOrganizationCommandService`, `new PermissionEngine()`, and `StubCurrentSessionService`) —
that test asserts navigation permission behaviour, **not** the ViewModel's own logic.

### E.2 New test file + stubs required

| Item | Detail |
|---|---|
| **NEW** `tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs` | 2 tests (§E.3) |
| **NEW stub** `StubOrganizationQueryService` | `IOrganizationQueryService` (5 members) — needs `GetOrganizationsAsync` configurable to throw; the other 4 members return empty/null. Small; **private nested class** in the new test file (matching the Wave 2B `ThrowingKpiEngineQueryService` precedent) |
| **NEW stub** `StubOrganizationCommandService` | `IOrganizationCommandService` (5 members) — a pure `throw new NotSupportedException()` stub; the load-path tests never invoke a command. **Private nested class** in the new test file. (An equivalent nested one already exists in `NavigationServiceTests.cs`, in the Shell.Tests assembly — cannot be reused across assemblies) |
| **REUSE** `FakeCurrentSessionService` | `Rojan.Desktop.Presentation.Tests.Automation.FakeCurrentSessionService` — `internal`, implements the exact `Rojan.Desktop.Presentation.Organizations.ICurrentSessionService`, returns `CurrentRole => PlatformOwner`, `CurrentOrganization => null`. Reuse via `using Rojan.Desktop.Presentation.Tests.Automation;` — **no new session stub needed** |
| **REUSE** `PermissionEngine` | real `new PermissionEngine()` from `Rojan.Desktop.Application.Organizations` (as `NavigationServiceTests` does) |

**No shared/production stub is touched.** All new stubs are private nested classes in the one new test
file.

### E.3 Tests (2)

| Test | Setup | Assertion |
|---|---|---|
| `LoadAsync_QueryThrows_LogsError` | `StubOrganizationQueryService` with `GetOrganizationsAsync` → `Task.FromException(new InvalidOperationException("boom"))`; `RecordingLogger<OrganizationPageViewModel>` | `State == DashboardState.Error`, `ErrorMessage == "boom"` (**unchanged**) **and** `logger.Entries` contains a `LogLevel.Error` entry with `Message` containing `"LoadAsync"` |
| `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | same throwing query, **no logger passed** | `Record.Exception(() => new OrganizationPageViewModel(...))` is `null` |

Uses the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`) via `using`.
**Estimated: 2 tests.** Expected suite after implementation: **2,548 + 2 = 2,550**.

### E.4 Regression

The new test file adds tests only (nothing existing changes). Full validation: build (0/0) + full suite
(2,548 + 2) + architecture tests (7/7).

---

## F. Commit Plan (Task 6)

### F.1 Options

| | Isolated Organization commit | Combine with a future wave (2C) |
|---|---|---|
| Files | 1 production + 1 **new** test file (with its nested stubs) | + those into a 2C batch |
| Precedent | Wave 2B **already split Organization out** (Phase 8.22 §F) *specifically because* it carries new test scaffolding the other 4 did not | — |
| Injection style match | Organization is `AddTransient` (trivial, zero plumbing) — **unlike** every Wave 2C member (`new`-by-parent, needs parent logger plumbing) | mismatched — 2C is a different risk profile |
| Review surface | one small self-contained commit: 1 catch + a new test file whose stubs are visible in the same diff | new test scaffolding buried in a larger, structurally-different batch |
| Cost | one extra commit cycle (the engagement's normal unit) | — |

### F.2 Recommendation

**Isolated Organization commit** — `fix(desktop): add ViewModel diagnostic logging (organization page)`.

Reasoning:
- Wave 2B already made this call (§8.22 §F): Organization was split from Wave 2B precisely because it
  needs a new test file + stubs, and that scaffolding deserves its own reviewable diff.
- Organization is `AddTransient` (free `ILogger<T>` injection, zero plumbing). Every Wave 2C member is
  `new`-by-parent and needs the parent ViewModel to carry an `ILogger<Child>` param — a materially
  different change. Bundling Organization into 2C would mix two risk profiles in one commit.
- The commit is small and single-concern (1 broad catch + its test file). One extra commit cycle is the
  engagement's standard unit of work.

### F.3 Proposed staging (for the eventual commit-execution phase)

```
src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs   (new)
```

Explicit paths only — never `git add -A` / `git add .`.

### F.4 Sequencing

1. **Phase 8.27 — Implementation** (on authorization): apply §B.4 to `LoadAsync`; create the new test
   file with 2 tests + 2 nested stub classes.
2. **Validate:** build (0/0) + full suite (2,548 + 2) + architecture (7/7).
3. **Phase 8.28 — Commit Scope Review** (readiness only) → **Phase 8.29 — Commit Execution**: isolated
   commit, explicit-path staging, then fresh post-commit validation + checkpoint update
   (§B new commit, §E test count 2,548 → 2,550, self-logging coverage 17 → 18 of 56, §F/§G → Wave 2C).

### F.5 Out of scope

- The uncaught write/loader methods (`CreateOrganizationAsync` etc.) — *missing-guard* concern, a
  separate error-handling phase.
- Wave 2C (Support/AcceptInvite, Automation tabs, detail/profile VMs + BookingWizard).
- Shared-stub throw hooks.

---

## STOP

Audit complete. No implementation performed.

**Recommendation: 1 production file (`OrganizationPageViewModel.cs`, 1 log call at `LoadAsync`) + 1 new
test file (2 tests, 2 private nested stubs, reusing `FakeCurrentSessionService` + real `PermissionEngine`).
`Error` level, operation-name-only, exception never passed. Isolated commit.**
