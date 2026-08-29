# ROJAN AI — TEAM 3 — PHASE 8.47 — BOOKINGWIZARD LOGGING (WAVE 2C-3b) — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `7aa1d1b` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_46_BOOKINGWIZARD_LOGGING_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_45_PROFILE_LOGGING_COMMIT_REPORT_v1.md`
**Scope:** `BookingWizardViewModel` self-logging + `BookingPageViewModel` `ILoggerFactory` plumbing only.

---

## A. FILES CHANGED (4 — all modified, 0 new)

`git diff --stat`: **4 files changed, 185 insertions(+), 8 deletions(-)**

### A.1 Production (2)

| # | File | Change |
|---|---|---|
| 1 | `src/…/ViewModels/BookingWorkflow/BookingWizardViewModel.cs` | `sealed`→`sealed partial`; `+using Microsoft.Extensions.Logging;` `+using Microsoft.Extensions.Logging.Abstractions;`; `private readonly ILogger<BookingWizardViewModel> _logger;` field; ctor reformatted to 4 params with `+ ILogger<BookingWizardViewModel>? logger = null` appended **after** `Action? onBookingCreated = null`; `_logger = logger ?? NullLogger<BookingWizardViewModel>.Instance;`; **1** instance-form `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Booking wizard operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);`; **4** call sites |
| 2 | `src/…/ViewModels/Bookings/BookingPageViewModel.cs` | `+ private readonly ILoggerFactory? _loggerFactory;`; ctor `+ ILoggerFactory? loggerFactory = null` appended **after** the existing optional `ILogger<BookingPageViewModel>? logger`; `_loggerFactory = loggerFactory;`; `OpenWizard()` now passes `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` as the wizard's new last ctor arg. **Existing `_logger` field, the legacy `[LoggerMessage(string operation, Exception exception)`, and all 5 of its call sites: UNTOUCHED.** |

**8 deletions** = the pre-change one-line ctor signature (`BookingWizardViewModel`) and the two single-line ctor-param / `new BookingWizardViewModel(...)` lines being replaced by their multi-line / extra-arg forms. No behavioural line removed.

### A.2 Tests (2 modified, 0 new)

| # | File | Change |
|---|---|---|
| 3 | `tests/…/BookingWorkflow/BookingWizardViewModelTests.cs` | `+using Microsoft.Extensions.Logging;` `+using Rojan.Desktop.Presentation.Tests.Specialists;`; `MakeSutOnDateStep` gains an optional `ILogger<BookingWizardViewModel>? logger = null` param (forwarded as the new 4th ctor arg; existing callers pass nothing → `NullLogger`); **+6 tests** |
| 4 | `tests/…/Bookings/BookingPageViewModelTests.cs` | `+using Rojan.Desktop.Application.BookingWorkflow;` `+using Rojan.Desktop.Presentation.ViewModels.BookingWorkflow;`; `MakeSut` gains an optional `ILoggerFactory? loggerFactory = null` param (forwarded as the new last ctor arg); **+1 test** |

**+7 tests. No existing test body modified** (0 lines removed from any test). **No shared production stub modified** — `StubBookingWorkflowService` / `StubDialogService` are already delegate/recording-driven and accept throwing tasks as-is. **No new test helper** — `RecordingLogger<T>` and `RecordingLoggerFactory` (committed `7aa1d1b`) reused.

### A.3 NOT touched

`BookingWizardStep.cs`, `IBookingWorkflowService` / any interface / DTO, `IDialogService`, `StubBookingWorkflowService`, `StubDialogService`, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, DI registration (`Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs`), Domain, Infrastructure, Shell, Application, backend contracts, RBAC, authentication, navigation, profile ViewModels, Customer/Service/Inventory page VMs, the other detail/profile VMs (Wave 2C-3c).

---

## B. LoggerFactory PLUMBING

`BookingPageViewModel` already holds **one `ILogger<BookingPageViewModel> _logger` + an instance-form `[LoggerMessage]`** (from `da18c18`, the legacy `(string operation, Exception exception)` form). Adding a second `ILogger<BookingWizardViewModel>` field would fail the source generator with **`SYSLIB1020`**.

**Resolution — `ILoggerFactory` (not `ILogger`) pass-through** (identical to Wave 2C-3a's `Customer`/`Service`/`InventoryPageViewModel`):

- Parent ctor gains **`ILoggerFactory? loggerFactory = null`** — one optional param, appended **after** the existing optional `ILogger<BookingPageViewModel>? logger = null`, so it is last.
- Stored as `private readonly ILoggerFactory? _loggerFactory;`.
- `OpenWizard()`: `new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync(), _loggerFactory?.CreateLogger<BookingWizardViewModel>())`. When `_loggerFactory` is null (no DI, or an old caller), `null` flows to the wizard, which falls back to `NullLogger<BookingWizardViewModel>.Instance`.
- `ILoggerFactory` is **not** `ILogger` → does **not** count toward `SYSLIB1020`. **Zero change** to the parent's own `_logger` / legacy `[LoggerMessage]` / its 5 call sites.
- `ILoggerFactory` is registered by `AddLogging()` (`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:91`). All new params optional → **no DI registration change, no call-site breakage**.
- `CreateLogger<T>` — `LoggerFactoryExtensions.CreateLogger<T>`, `Microsoft.Extensions.Logging` (already `using`-ed in `BookingPageViewModel`), already-referenced `Microsoft.Extensions.Logging.Abstractions` assembly. No new package reference.

**Child (`BookingWizardViewModel`):** standard self-logging shape — `sealed partial`, exactly one `ILogger<BookingWizardViewModel> _logger` field → **instance-form** `[LoggerMessage]`, no `SYSLIB1020` in the child; `?? NullLogger<BookingWizardViewModel>.Instance`; optional ctor param appended last.

**`dotnet build -c Debug` → 0 warnings / 0 errors — no `SYSLIB1020`.**

---

## C. LOGGING BOUNDARIES

**4 of 5** catch boundaries instrumented. Each `LogOperationFailed(nameof(<Method>))` is the **last statement** of the existing `#pragma warning disable CA1031` broad catch, appended **after** the unchanged `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;`:

| Method | Catch | Call added | Existing behaviour |
|---|---|---|---|
| `LoadOptionsAsync` | `catch (Exception exception)` | `LogOperationFailed(nameof(LoadOptionsAsync));` | `ErrorMessage`/`State` unchanged; fires at construction via `_ = LoadOptionsAsync()` |
| `AddGuestCustomerAsync` | `catch (Exception exception)` (+ `finally`) | `LogOperationFailed(nameof(AddGuestCustomerAsync));` — **before** the unchanged `finally { IsAddingGuestCustomer = false; }` | `ErrorMessage`/`State` unchanged; `finally` unchanged |
| `LoadAvailableSlotsAsync` | `catch (Exception exception)` | `LogOperationFailed(nameof(LoadAvailableSlotsAsync));` | `ErrorMessage`/`State` unchanged |
| `ConfirmBookingAsync` | `catch (Exception exception)` | `LogOperationFailed(nameof(ConfirmBookingAsync));` | `ErrorMessage`/`State` unchanged; `_onBookingCreated?.Invoke()` (success path) untouched |

`[LoggerMessage]` signature is `(string operation)` — **no `Exception` parameter** (unlike the parent's legacy form). Level `Error` (clears the `LocalFileLoggerProvider` `Warning` floor). `EventId = 1`.

Only log lines this change can produce:
```
<ts> [Error] …BookingWizardViewModel: Booking wizard operation failed. Operation=LoadOptionsAsync
<ts> [Error] …BookingWizardViewModel: Booking wizard operation failed. Operation=AddGuestCustomerAsync
<ts> [Error] …BookingWizardViewModel: Booking wizard operation failed. Operation=LoadAvailableSlotsAsync
<ts> [Error] …BookingWizardViewModel: Booking wizard operation failed. Operation=ConfirmBookingAsync
```

---

## D. `SearchNextAvailableDateAsync` SKIP — CONFIRMED

**NOT instrumented.** Its `catch (Exception)` (no exception variable), `finally`, and `_nextAvailableDateSearchCts` handling are **byte-for-byte unchanged**.

Rationale (per Phase 8.46 §B.3, authorizer-approved):
- Best-effort forward probe (≤7 days), fired only as `_ = SearchNextAvailableDateAsync()` when the picked date has zero slots.
- Cancellation-dominated — every new date pick / fresh probe calls `cts.Cancel()`; the loop opens with `cts.Token.ThrowIfCancellationRequested()`. Superseded probes routinely throw `OperationCanceledException` into this catch on the happy path.
- Never user-visible — touches neither `ErrorMessage` nor `State`. A failed probe just leaves `SuggestedNextAvailableDate == null`.
- Logging every superseded/failed probe = diagnostic noise for zero value.

**Guarded by test** `SearchNextAvailableDateAsync_ProbeFails_LogsNothing` — picked date returns `[]` (→ `Empty`, fires the probe), every forward candidate throws; asserts `RecordingLogger.Entries` is **empty** and `State == Empty`. The skip is now an asserted decision, not an omission.

---

## E. SECURITY REVIEW

| Aspect | Confirmed |
|---|---|
| `Exception` object | **never passed** — child signature is `(string operation)`, no `Exception` param |
| `Exception.Message` | **never logged** — call sites pass `nameof(...)` only; `ToFriendlyErrorMessage(exception)` output is a fixed localized string assigned to `ErrorMessage` (UI), never to the logger |
| Backend response body | never logged (only ever inside `ApiException.Message`) |
| **Guest name / phone** (`GuestFullName`, `GuestPhone` → `CreateGuestCustomerAsync`) | never referenced by a log call |
| **Booking notes** (`Notes` → `CreateBookingWorkflowRequest`) | never referenced |
| Appointment times / `SelectedSlot.Start` / `SelectedDate` | never referenced |
| Selected customer identity / id / `FullName` / `IsLinkedToAccount` | never referenced |
| Selected service — name / **price** / **duration** / id | never referenced |
| Selected specialist — name / id | never referenced |
| Tokens (bearer / session) | not held by this VM |
| Level / EventId | `Error` / `1` |
| Behaviour | `#pragma` unchanged; `ErrorMessage` / `State` / `finally` / `_onBookingCreated` / `_nextAvailableDateSearchCts` all unchanged; log strictly appended last |

**Test-enforced no-leak** — each failure test seeds a recognizable secret into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)`:
- `LoadOptionsAsync`: `"Amelia Hart / 555-0100 / VIP corner chair please"`
- `AddGuestCustomerAsync`: `"Walk-in Guest"` + `"555-0100"` (both asserted absent)
- `LoadAvailableSlotsAsync`: `"specialist-1 / service-1 / booked slots for Amelia Hart"`
- `ConfirmBookingAsync`: notes `"VIP corner chair please"` + `"Amelia Hart"` + `"Jordan Lee"` + `"$65"` (all asserted absent; the stub's exception interpolates `CustomerName / ServiceName / SpecialistName / Notes / Price`)
- Parent forwarding: `"guest booking secret / 555-0100"` asserted absent

---

## F. TESTS

### F.1 Added (7)

| # | File | Test | Asserts |
|---|---|---|---|
| 1 | `BookingWizardViewModelTests` | `Constructor_OptionsQueryThrows_LogsErrorWithOperationNameOnly_NoLeak` | one `Error` entry, `Operation=LoadOptionsAsync`, secret absent |
| 2 | `BookingWizardViewModelTests` | `AddGuestCustomerCommand_Failure_LogsErrorWithOperationNameOnly_NoGuestPiiLeak` | `Operation=AddGuestCustomerAsync`; guest name + phone absent |
| 3 | `BookingWizardViewModelTests` | `NextCommand_FromDateStep_SlotsQueryThrows_LogsErrorWithOperationNameOnly_NoLeak` | `Operation=LoadAvailableSlotsAsync`; secret absent |
| 4 | `BookingWizardViewModelTests` | `ConfirmBookingCommand_Failure_LogsErrorWithOperationNameOnly_NoNotesOrIdentityLeak` | `Operation=ConfirmBookingAsync`; notes + customer/specialist names + price absent |
| 5 | `BookingWizardViewModelTests` | `SearchNextAvailableDateAsync_ProbeFails_LogsNothing` | `State == Empty`, `RecordingLogger.Entries` **empty** (skip guard) |
| 6 | `BookingWizardViewModelTests` | `Constructor_OptionsQueryThrows_WithoutLogger_UsesNullLogger_NeverThrows` | 2-arg ctor, `State == Error`, generic friendly message, no throw |
| 7 | `BookingPageViewModelTests` | `OpenWizardCommand_ForwardsLoggerFactoryToWizard_ChildLoadFailureIsLoggedViaTheFactory` | dialog shown; `RecordingLoggerFactory` single `Error` entry, category contains `BookingWizardViewModel`, `Operation=LoadOptionsAsync`, secret absent |

### F.2 Behaviour preservation — verified by test

- Every existing `BookingWizardViewModelTests` / `BookingPageViewModelTests` test passes unchanged (booking flow, guest creation, slot loading, confirmation, dialog behaviour, `onBookingCreated` callback, eligibility filter, smart ordering, next-available-date suggestion).
- `SearchNextAvailableDateAsync_ProbeFails_LogsNothing` also re-proves `State == Empty` on the empty-slots path.
- No-logger tests re-prove the friendly-message / Error-state surfacing is unchanged.

### F.3 Fresh full run (working tree, uncommitted)

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **651** | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,594** | **0** | **0** |

Delta from baseline `7aa1d1b` (2,587): **+7** (Presentation.Tests 644 → 651).

---

## G. VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → 2,594 / 2,594 passing   0 failed   0 skipped
Architecture tests                → 7 / 7 passing
```

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,594 / 2,594 | 2,594 / 2,594 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

---

## H. COMMIT READINESS

| Gate | Status |
|---|---|
| Scope = `BookingWizardViewModel` + `BookingPageViewModel` plumbing + their 2 test files | ✅ |
| No profile VM / Customer-Service-Inventory page / DI / Domain / backend / RBAC / auth / navigation change | ✅ (not in `git status`) |
| No `ILogger<BookingWizardViewModel>` field on the parent — `ILoggerFactory` used | ✅ (no `SYSLIB1020`) |
| Child `[LoggerMessage]` signature `(string operation)` — no `Exception` | ✅ |
| Only 4 boundaries instrumented; `SearchNextAvailableDateAsync` unchanged + test-guarded | ✅ |
| Every log call `nameof`-only; no guest PII / notes / slot times / customer id / service price / backend body | ✅ |
| Behaviour append-only after existing error handling; flow / callback / dialog / cts unchanged | ✅ |
| No shared production stub modified; no existing test body changed; no new file | ✅ |
| Build 0/0 · Tests 2,594/2,594 · Architecture 7/7 | ✅ |

Working tree: **4 files** — `git status --porcelain`:
```
 M src/Rojan.Desktop.Presentation/ViewModels/BookingWorkflow/BookingWizardViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/BookingWorkflow/BookingWizardViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs
```

Recommended commit subject (per Phase 8.46 §H): `fix(desktop): add ViewModel diagnostic logging (booking wizard)`

---

## STOP

Implementation complete. Build 0/0, 2,594/2,594 tests, architecture 7/7. Working tree modified across
exactly 4 files (2 production + 2 test). **Nothing committed, pushed, merged, rebased, or amended.**
HEAD remains `7aa1d1b`. Awaiting Phase 8.48 commit scope review.
