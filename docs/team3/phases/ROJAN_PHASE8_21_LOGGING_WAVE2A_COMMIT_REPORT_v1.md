# ROJAN AI — TEAM 3 — PHASE 8.21 LOGGING WAVE 2A — COMMIT REPORT v1

**Type:** Commit executed + fresh post-commit validation. **Not pushed, not merged, not rebased, not amended.**
**Branch:** `feature/team3-desktop-completion`

---

## A. Commit Hash

**`75357e13cb1c243dbf4788cfd394711577893bb1`** (`75357e1`)

- Parent: `31f4b63` (`fix(desktop): log unexpected OTP API failures`)
- Author: Meisam Elhaee — Thu Aug 27 2026 21:56:32 -0700
- Subject: `fix(desktop): add ViewModel diagnostic logging (wave 2a)` (exactly as authorized)
- Trailers: `Co-Authored-By: Claude Sonnet 5`, `Claude-Session: …`

```
git log --oneline -4
75357e1 fix(desktop): add ViewModel diagnostic logging (wave 2a)
31f4b63 fix(desktop): log unexpected OTP API failures
2453a7f fix(desktop): add ViewModel diagnostic logging (wave 1)
94fca6a fix(desktop): bound navigation back-stack depth
```

---

## B. Files Committed

```
git show --stat 75357e1
 src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs         | 13 ++++++-
 src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs                      | 14 ++++++-
 src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs        | 14 ++++++-
 src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs        | 15 ++++++-
 src/Rojan.Desktop.Presentation/ViewModels/Services/ServicePageViewModel.cs           | 17 ++++++--
 tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerPageViewModelTests.cs       | 31 +++++++++++++++
 tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs                    | 33 ++++++++++++++--
 tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs      | 34 ++++++++++++++--
 tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs      | 34 ++++++++++++++--
 tests/Rojan.Desktop.Presentation.Tests/Services/ServicePageViewModelTests.cs         | 31 +++++++++++++++
 10 files changed, 236 insertions(+), 18 deletions(-)
```

**Exactly the 10 authorized files — 5 production + 5 test. Nothing else.**

| File | Log call sites |
|---|---|
| `Customers/CustomerPageViewModel.cs` | 1 — `LoadAsync` |
| `Services/ServicePageViewModel.cs` | 3 — `LoadAsync`, `LoadCategoriesAsync` (previously silent), `CreateServiceAsync` |
| `Inventory/InventoryPageViewModel.cs` | 2 — `LoadAsync`, `SearchAsync` |
| `HR/HrPageViewModel.cs` | 2 — `LoadAsync`, `SearchAsync` |
| `Reporting/ReportingPageViewModel.cs` | 3 — `LoadAsync`, `RunReportAsync`, `RerunSnapshotAsync` |

Each production file: `sealed`→`sealed partial`, +2 `using`s, +`ILogger<T> _logger` field, +optional last
ctor param `ILogger<T>? logger = null` + `NullLogger` fallback, +1 `[LoggerMessage(Level = Error)]`
partial. Each test file: +2 `using`s, +2 tests; three helpers (`MakeSut` ×2, `CreateSut`) gained an
optional trailing `RecordingLogger<T>?` param. **No existing test body modified.**

---

## C. Scope Verification

Performed on the **staged** diff before committing (`git diff --cached`), re-confirmed from
`git show 75357e1`:

| Check | Result |
|---|---|
| Staging method | `git reset` to clear the index, then **10 explicit `git add <path>`**. **No `git add .`, no `git add -A`.** |
| Staged file count | Exactly 10, all authorized |
| Unstaged tracked changes at commit time | none (`git diff --name-only` empty) |
| `.md` reports staged | none — all remain untracked |
| Working tree after commit | **clean** (0 modified/deleted tracked); untracked = `.md` reports only |
| **DI changes** | **None** — `ServiceCollectionExtensions.cs` not in the diff |
| **Interface changes** | **None** — no `I*.cs` in the diff |
| **Domain changes** | **None** — no `Rojan.Desktop.Domain` file in the diff |
| **Backend contract changes** | **None** |
| **Authentication changes** | **None** |
| **Navigation changes** | **None** |
| **Shared stub changes** | **None** — `RecordingLogger.cs`, `Stub*QueryService.cs`, `Stub*CommandService.cs` all unmodified (referenced via `using` only) |
| Push / merge / rebase / amend | **none performed** — single fresh commit on `31f4b63` |

---

## D. Logging Architecture Confirmation

| Aspect | Confirmed |
|---|---|
| `ILogger<T>` field | instance field `private readonly ILogger<XxxPageViewModel> _logger;` in all 5, constructor-injected via the optional param |
| `NullLogger<T>` fallback | `_logger = logger ?? NullLogger<XxxPageViewModel>.Instance;` — proven by the 5 `NoLoggerSupplied_UsesNullLogger_…` tests |
| `[LoggerMessage]` source generation | all 5 use source-generated partials, not raw `_logger.LogError` — required (CA1848 under `TreatWarningsAsErrors`); instance form (one logger field each → no `SYSLIB1020`) |
| Level | **`Error`** for every boundary — clears the `LocalFileLoggerProvider` `Warning` floor, reaches `%LocalAppData%\RojanDesktop\logs\` |
| Content | **operation name only** — `Message = "Xxx page operation failed. Operation={Operation}"`, `{Operation}` = compile-time `nameof(<method>)`. **The `Exception` is never passed to the logger** (per the SECURITY rule: no `Exception.Message`, no backend response, no customer/phone/token data can reach the log) |
| Behaviour preservation | every `State` / `ErrorMessage` / `StatusMessage` / `CreateErrorMessage` line unchanged; the log call is appended after. Reporting's `catch (OperationCanceledException)` branch not logged. `ServicePageViewModel.LoadCategoriesAsync` stays deliberately swallowed — the log is the only new signal for that otherwise-silent degradation |
| Architecture tests | `Microsoft.Extensions.Logging.Abstractions` not forbidden by `DependencyDirectionTests`; no `System.Windows.Threading`/`Controls` added → `ViewModelTestabilityTests` unaffected. **7/7 pass** |

Self-logging ViewModel coverage after this commit: **13 of 56** (the 8 prior + Customer, Service,
Inventory, HR, Reporting page ViewModels).

---

## E. Validation Results — Fresh, Post-Commit (HEAD = `75357e1`)

### E.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### E.2 Full test suite

```
dotnet test --no-build
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **595** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,538** | **0** | **0** |

### E.3 Test count delta

| | Total | Presentation.Tests |
|---|---|---|
| Baseline `31f4b63` | 2,528 | 585 |
| **New HEAD `75357e1`** | **2,538** | **595** |
| Delta | **+10** | +10 |

All +10 are the new Wave 2A tests. No pre-existing test changed result.

### E.4 Architecture tests

**7 / 7 passing** — unchanged.

### E.5 Expected vs actual (per authorization)

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,538 / 2,538, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## F. Remaining Backlog

### F.1 Logging coverage — remaining

| Wave | ViewModels | Status |
|---|---|---|
| **2B** | `OrganizationPageViewModel`, `AnalyticsPageViewModel`, `AiCenterPageViewModel`, `SalonPageViewModel`, `QrCodesPageViewModel` (all `AddTransient` page VMs, medium daily frequency, 1–2 broad catches each) | **Recommended next** — same additive pattern, `Error` level, one isolated commit |
| **2C-1** | `SupportPageViewModel`, `AcceptInviteViewModel` | Deferred — `AcceptInviteViewModel` is membership/auth-adjacent and needs a `MobileOtpLoginViewModel`-style data-safety review |
| **2C-2** | 5 Automation tab VMs + `AutomationPageViewModel` logger plumbing | Deferred — `new`-by-parent, needs parent to carry `ILogger<Tab>` params |
| **2C-3** | detail/profile VMs (`CustomerProfile`, `ServiceProfile`, `InventoryProfile`, `EmployeeProfile`, `InvoiceProfile`) + `BookingWizardViewModel` (5 catches) + parent plumbing | Deferred — `new`-by-parent |
| test-infra | shared-stub throw hooks for fuller per-boundary coverage of the 3 untested Wave 2A log sites (Service `LoadCategories`/`CreateService`, extra search/rerun boundaries) | Follow-up — not a correctness risk (they share the tested `LogOperationFailed` method) |
| `AuthBootstrapHttpClient` has no logging of its own | Phase 8.14 §A.3 — separate Infrastructure decision |

Self-logging ViewModel coverage: **13 of 56 (~23%)**.

### F.2 Non-logging backlog (unchanged)

| Item | Status |
|---|---|
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry risk | Documented, unresolved — blocks Accounting's eventual backend connection |
| `AccountingPageViewModel.CancelInvoiceAsync` — missing try/catch | Deferred to a dedicated error-handling phase |
| `CancellationToken` propagation — `CommandPaletteViewModel` (Search) highest value | Planned, not started |
| Startup UX — no progress indicator across `App.OnStartup`'s 13 blocking stages | Planned, not started |
| RBAC migration for the 6 still-local domains | Sequenced future work, per-domain backend-contract-blocked |
| Calendar's dead EF migration/tables (3) | Disclosed tech debt, deferred |
| `RolePermissions` dead enum members | Cleanup opportunity, low urgency |

**Upstream-blocked (not Team 3 actionable):** Inventory, HR, Accounting backend integration — blocked on
Backend/Team 1; Desktop-side prep complete since Phase 8.0.

**No P0. No P1.** Recommended next action: **Logging Wave 2B**.

---

## STOP

Commit executed (`75357e1`), fresh validation green (build 0/0, 2,538/2,538 tests, architecture 7/7),
report written, checkpoint updated. No push, no merge, no rebase, no amend. Awaiting next authorization.
