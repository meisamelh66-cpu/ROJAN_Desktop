# ROJAN AI — TEAM 3 — PHASE 8.18 LOGGING WAVE 2 — SCOPE AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no behaviour change, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `31f4b63` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_9_LOGGING_COVERAGE_AUDIT_v1.md`

Every figure verified against source this turn.

---

## A. Current Coverage (Task 1)

### A.1 ViewModel population

**56 ViewModel classes** (55 Presentation + 1 Shell) — unchanged from Phase 8.9's measurement.

### A.2 Self-logging ViewModels — **8 of 56 (14.3%)**

| # | ViewModel | Landed | Level |
|---|---|---|---|
| 1 | `Bookings/BookingPageViewModel` | Phase 7.4 (`da18c18`) | Error |
| 2 | `Accounting/PosCheckoutViewModel` | Phase 7.4 (`da18c18`) | Error |
| 3 | `Specialists/SpecialistScheduleViewModel` | Phase 7.4 (`ea03d83`/`53090c1`) | Warning/Error |
| 4 | `Specialists/SpecialistAvailabilityViewModel` | Phase 7.4 | Error |
| 5 | `Dashboard/DashboardPageViewModel` | Phase 8.11 (`2453a7f`) | Error |
| 6 | `Calendar/CalendarPageViewModel` | Phase 8.11 (`2453a7f`) | Error |
| 7 | `Accounting/AccountingPageViewModel` | Phase 8.11 (`2453a7f`) | Error |
| 8 | `Security/MobileOtpLoginViewModel` | Phase 8.15 (`31f4b63`) | Warning |

`Specialists/SpecialistPageViewModel` and `Specialists/SpecialistProfileViewModel` reference `ILogger`
**only as pass-through carriers** for their schedule/availability children — they do not self-log, so
they are **not** counted in the 8. (They are also not Wave 2 targets — their own catch is a single
`nameof`-style guard, and their diagnostic value is already covered by the children.)

### A.3 Remaining broad-catch ViewModels — the Wave 2 pool

**23 ViewModel files** contain a broad `catch (Exception)` and do **not** self-log. Verified counts:

| File | broad-catch count | Construction | Sidebar module |
|---|---|---|---|
| `BookingWorkflow/BookingWizardViewModel` | 5 | `new` by `BookingPageViewModel` | (modal wizard) |
| `Automation/WorkflowsTabViewModel` | 5 | `new` by `AutomationPageViewModel` | Automation (tab) |
| `Services/ServicePageViewModel` | 3 | **`AddTransient`** | Services |
| `Services/ServiceProfileViewModel` | 3 | `new` by `ServicePageViewModel` | (detail panel) |
| `Reporting/ReportingPageViewModel` | 3 | **`AddTransient`** | Reporting |
| `Automation/ScheduledJobsTabViewModel` | 3 | `new` by `AutomationPageViewModel` | Automation (tab) |
| `Inventory/InventoryPageViewModel` | 2 | **`AddTransient`** | Inventory |
| `HR/HrPageViewModel` | 2 | **`AddTransient`** | Staff & HR |
| `AI/AiCenterPageViewModel` | 2 | **`AddTransient`** | AI Center |
| `Salons/SalonPageViewModel` | 2 | **`AddTransient`** | Salon |
| `QrCodes/QrCodesPageViewModel` | 2 | **`AddTransient`** | QR Codes |
| `Support/SupportPageViewModel` | 2 | **`AddTransient`** | Support |
| `Membership/AcceptInviteViewModel` | 2 | **`AddTransient`** | Accept Invite |
| `Automation/BusinessRulesTabViewModel` | 2 | `new` by `AutomationPageViewModel` | Automation (tab) |
| `Automation/ApprovalsTabViewModel` | 2 | `new` by `AutomationPageViewModel` | Automation (tab) |
| `Customers/CustomerPageViewModel` | 1 | **`AddTransient`** | Customers |
| `Analytics/AnalyticsPageViewModel` | 1 | **`AddTransient`** | Analytics |
| `Organizations/OrganizationPageViewModel` | 1 | **`AddTransient`** | Organization |
| `Automation/AutomationDashboardTabViewModel` | 1 | `new` by `AutomationPageViewModel` | Automation (tab) |
| `Customers/CustomerProfileViewModel` | 1 | `new` by `CustomerPageViewModel` | (detail panel) |
| `HR/EmployeeProfileViewModel` | 1 | `new` by `HrPageViewModel` | (detail panel) |
| `Inventory/InventoryProfileViewModel` | 1 | `new` by `InventoryPageViewModel` | (detail panel) |
| `Accounting/InvoiceProfileViewModel` | 1 | `new` by `AccountingPageViewModel` | (detail panel) |

All 23 use the identical pattern: `_ = LoadAsync()` fire-and-forget in the constructor, and
`catch (Exception exception) { ErrorMessage = exception.Message; State = DashboardState.Error; }` (or a
write-boundary variant → `Strings.*_SaveError`). **None logs.** A non-API fault is invisible beyond the
on-screen message.

---

## B. Candidate Matrix (Task 2)

Prioritisation basis: **frequency** (primary sidebar page opened in daily reception/management workflow >
occasional-admin page > detail sub-panel > tab in a low-traffic module), **catch boundaries** (more =
more diagnostic surface), **diagnostic value** (a silently-swallowed load is worth more than one that
already shows an Error state + Retry), **architecture risk** (`AddTransient` = free logger injection, zero
plumbing; `new`-by-parent = the parent must carry an `ILogger<Child>` param — the AccountingPageViewModel
/ SpecialistPageViewModel pass-through precedent — more surface, more risk).

| ViewModel | Freq | Catches | Logging | Injection | Risk | Value | Tier |
|---|---|---|---|---|---|---|---|
| `CustomerPageViewModel` | **High** (core daily) | 1 (Load) | none | `AddTransient` (free) | Low | High | **2A** |
| `ServicePageViewModel` | **High** | 3 (Load; **LoadCategories — silently swallowed**; CreateService save) | none | `AddTransient` | Low | **High** (one branch is silent) | **2A** |
| `InventoryPageViewModel` | **High** | 2 (Load; Search) | none | `AddTransient` | Low | High | **2A** |
| `HrPageViewModel` | Med-High | 2 (Load; Search) | none | `AddTransient` | Low | High | **2A** |
| `ReportingPageViewModel` | Med | 3 (Load; +2 in run/export — scope review to confirm) | none | `AddTransient` | Low–Med (`IDisposable`) | Med-High | **2A** |
| `OrganizationPageViewModel` | Med (admin) | 1 (Load) | none | `AddTransient` | Low | Med | 2B |
| `AnalyticsPageViewModel` | Med | 1 (Load) | none | `AddTransient` | Low | Med | 2B |
| `AiCenterPageViewModel` | Low-Med | 2 (Load; +1) | none | `AddTransient` | Low | Med | 2B |
| `SalonPageViewModel` | Low (post-onboarding) | 2 (Load; Create) | none | `AddTransient` | Low | Med | 2B |
| `QrCodesPageViewModel` | Low-Med | 2 (Load; GenerateInvite) | none | `AddTransient` | Low | Med | 2B |
| `SupportPageViewModel` | Low (rare) | 2 (Load; SubmitMessage) | none | `AddTransient` | Low | Low-Med | 2C |
| `AcceptInviteViewModel` | Low (one-time) | 2 | none | `AddTransient` | **Med — membership/auth-adjacent, needs a careful data-safety review** | Low-Med | 2C |
| `BookingWizardViewModel` | Med (booking creation) | **5** | none | `new` by `BookingPageViewModel` | Med (parent plumbing) | Med-High | 2C (or its own mini-wave) |
| Automation tabs ×5 (`Workflows` 5, `ScheduledJobs` 3, `BusinessRules` 2, `Approvals` 2, `Dashboard` 1) | Low (Automation module) | 13 total | none | `new` by `AutomationPageViewModel` | Med (parent plumbs 5 `ILogger<Tab>` params) | Med | 2C |
| Detail/profile VMs ×4 (`CustomerProfile`, `ServiceProfile` 3, `InventoryProfile`, `EmployeeProfile`, `InvoiceProfile`) | Low (opened only after a successful list load) | ~7 total | none | `new` by parent page | Med (parent plumbing) | Low-Med | 2C |

---

## C. Recommended Wave 2A Scope

### C.1 Files (5 production + 5 test)

| Production file | Test file | Boundaries to instrument |
|---|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs` | `tests/…/Customers/CustomerPageViewModelTests.cs` | `LoadAsync` catch (~:216) |
| `src/Rojan.Desktop.Presentation/ViewModels/Services/ServicePageViewModel.cs` | `tests/…/Services/ServicePageViewModelTests.cs` | `LoadAsync` (~:324), `LoadCategoriesAsync` (**silently swallowed** ~:354), `CreateServiceAsync` (~:399) |
| `src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs` | `tests/…/Inventory/InventoryPageViewModelTests.cs` | `LoadAsync` (~:237), search handler (~:267) |
| `src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs` | `tests/…/HR/HrPageViewModelTests.cs` | `LoadAsync` (~:380), search handler (~:401) |
| `src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs` | `tests/…/Reporting/ReportingPageViewModelTests.cs` | `LoadAsync` (~:205) + the 2 in run/export (~:273, ~:303) — scope review to confirm each is a swallow, not a re-throw |

**Total: 10 files, ~12 log call sites.**

### C.2 Why these 5 for 2A

- All 5 are **primary sidebar modules** in the daily reception/management workflow — the pages a user
  actually opens repeatedly per shift (Customers, Services, Inventory, HR, Reporting).
- All 5 are `AddTransient` → `AddLogging()`'s open-generic `ILogger<T>` is injected automatically; **zero
  DI registration edits, zero constructor plumbing beyond the one optional param.**
- All 5 use the **exact Wave 1 pattern** — `_ = LoadAsync()` in ctor, `catch (Exception exception)` →
  `ErrorMessage`/`State`. The change is mechanical and identical to `2453a7f`.
- `ServicePageViewModel.LoadCategoriesAsync` is a genuine **silent failure** today (swallowed with only
  a code comment) — highest single diagnostic gain in the batch.
- All 5 have existing dedicated test files.

### C.3 Design (per file — the established pattern)

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed partial class XxxPageViewModel : ViewModelBase
{
    private readonly ILogger<XxxPageViewModel> _logger;

    public XxxPageViewModel(/* existing deps */, ILogger<XxxPageViewModel>? logger = null)
    {
        _logger = logger ?? NullLogger<XxxPageViewModel>.Instance;   // appended last, optional
    }

    // in each broad catch, AFTER the unchanged ErrorMessage/State lines:
    //   LogOperationFailed(nameof(LoadAsync), exception);

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Xxx operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation, Exception exception);
}
```

- **Level `Error`** — consistent with Wave 1 (swallowed broad-`catch (Exception)` load boundaries).
- Single `[LoggerMessage]` with an `{Operation}` discriminator per file (the `BookingPageViewModel`
  shape). Instance form (one logger field each — no `SYSLIB1020` risk).
- Log call appended **after** the existing `ErrorMessage = exception.Message; State = ...;` — no
  behaviour change, no catch removed, no exception-flow change.
- **No sensitive-data concern** in this batch — these are non-auth pages; the templates carry only a
  `nameof` operation name. The `Exception` is passed (formatted as `{Type}: {Message}` by the sink),
  identical to Wave 1 and the 4 Phase-7.4 ViewModels.

### C.4 Explicitly NOT in Wave 2A

Detail/profile sub-VMs, Automation tabs, `BookingWizardViewModel`, `AcceptInviteViewModel`, and the
Wave 2B pages — all deferred to their own later, separately-authorized waves.

---

## D. Test Strategy

Per file, using the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`, reused
cross-namespace via `using`) and each file's existing stub query/command services:

| Test shape | Count per file |
|---|---|
| `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` — assert `State == Error` + `ErrorMessage` unchanged **and** an `Error` entry containing the operation name | 1 |
| one test per **additional** boundary (search / create / categories) — same shape | 0–2 |
| `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | 1 |

Special: `ServicePageViewModel` gets an explicit
`LoadCategoriesAsync_Throws_LogsError` test — that boundary is currently silent, so the test proves the
new trail exists.

**Estimated new tests: ~13** (2–3 per file). Expected suite after Wave 2A implementation:
**2,528 + ~13 ≈ 2,541**, 0 failures. Existing tests: helper signatures (`CreateSut`/`MakeSut`) gain an
optional trailing `RecordingLogger<T>?` param → all current call sites compile and pass unchanged
(`NullLogger` default).

**Regression:** build (0/0) + full suite + architecture tests (7/7) — matching the engagement rhythm.

---

## E. Commit Strategy

### E.1 Wave sequencing

| Wave | Files (prod) | Content | Commit |
|---|---|---|---|
| **2A** | 5 | Customer, Service, Inventory, HR, Reporting page VMs | 1 commit — `fix(desktop): add ViewModel diagnostic logging (wave 2a)` |
| **2B** | 5 | Organization, Analytics, AiCenter, Salon, QrCodes page VMs | 1 commit (`wave 2b`) — after 2A lands + its own scope review |
| **2C-1** | 2 | Support, AcceptInvite page VMs (**AcceptInvite needs a MobileOtp-style data-safety review** — membership/auth-adjacent) | 1 commit |
| **2C-2** | 5 + parent | Automation tabs + `AutomationPageViewModel` logger plumbing | 1 commit |
| **2C-3** | ~5 + parents | Detail/profile VMs + `BookingWizardViewModel` + parent plumbing | 1 commit (or split) |

### E.2 Recommendation

**One commit per wave**, in the order 2A → 2B → 2C-1 → 2C-2 → 2C-3. Each wave gets its own
audit-adjacent cycle: **this audit → per-wave scope review (readiness only) → implementation
authorization → implement + validate → commit scope review → commit execution**, explicit-path staging,
one isolated commit.

Reasoning:
- Wave 1 established that ~4–5 files of the identical mechanical `[LoggerMessage]` change is a
  reviewable, single-concern commit.
- Splitting by **injection style** (2A/2B = `AddTransient`, zero plumbing; 2C-2/2C-3 = `new`-by-parent,
  needs parent changes) keeps the risky ones isolated from the trivial ones.
- 2C-1 isolates `AcceptInviteViewModel` so its auth-adjacency gets a focused data-safety review like
  `MobileOtpLoginViewModel` did — not buried in a batch.
- Per-wave scope reviews stay small and specific.

### E.3 Architecture review (Task 4) — Wave 2A

| Check | Result |
|---|---|
| `ILogger<T>` injection possible | **Yes** — all 5 are `AddTransient` in `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` (lines 61/64/66/68/70). `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) registers the open-generic `ILogger<T>`. DI fills the new optional param automatically |
| No DI registration changes needed | **Confirmed** — the `AddTransient<XxxPageViewModel>()` lines are unchanged; adding an optional ctor param requires no registration edit |
| No interface changes | **Confirmed** — the change is entirely inside the concrete ViewModel classes; `ICustomerQueryService` / `IServiceQueryService` / `IInventoryQueryService` / `IHrQueryService` / `IReportingService` etc. untouched |
| No domain impact | **Confirmed** — Presentation-layer only; no business rule, permission decision, backend call, or data-authority change. `Booking` / `Calendar` authority / `Shift Engine` / `RBAC` / `Authentication` / `Navigation` all untouched |
| `DependencyDirectionTests` | `Microsoft.Extensions.Logging.Abstractions` not forbidden (only Infrastructure/Domain/Shell/EF); already a Presentation `PackageReference` |
| `ViewModelTestabilityTests` | no `System.Windows.Threading`/`Controls` dependency added |
| Architecture suite | **7/7 expected unchanged** |
| `SYSLIB1020` (multi-logger) | **Not a risk** — each of the 5 has exactly one logger field (unlike `AccountingPageViewModel`, which needed the static form) |

---

## STOP

Audit complete. No implementation performed.

**Recommendation: Wave 2A** — `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`,
`HrPageViewModel`, `ReportingPageViewModel` (5 production + 5 test files, ~12 log sites, `Error` level,
one isolated commit). Wave 2B/2C sequenced after, each separately authorized.
