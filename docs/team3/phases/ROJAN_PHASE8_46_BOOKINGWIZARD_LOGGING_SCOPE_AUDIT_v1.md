# ROJAN AI — TEAM 3 — PHASE 8.46 — BOOKINGWIZARD LOGGING (WAVE 2C-3b) — SCOPE AUDIT v1

**Type:** Audit only. **No source change. No test change. No logger added. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `7aa1d1b739b41a33f8b50f1319a7ff52318fb420` — `fix(desktop): add ViewModel diagnostic logging (profile panels)` (Phase 8.43, committed 8.45)
**Reference:** `ROJAN_PHASE8_42_DETAIL_PROFILE_BOOKINGWIZARD_LOGGING_SCOPE_AUDIT_v1.md` §B/§C, `ROJAN_PHASE8_45_PROFILE_LOGGING_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §G.

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `7aa1d1b739b41a33f8b50f1319a7ff52318fb420` |
| HEAD subject | `fix(desktop): add ViewModel diagnostic logging (profile panels)` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree is clean. Wave 2C-3a (`7aa1d1b`) is committed and validated (build 0/0, 2,587/2,587, arch 7/7). This audit adds no code.

---

## B. VIEWMODEL ANALYSIS — `BookingWizardViewModel`

**File:** `src/Rojan.Desktop.Presentation/ViewModels/BookingWorkflow/BookingWizardViewModel.cs` (646 lines).

### B.1 Structure

| Aspect | Current |
|---|---|
| Declaration | `public sealed class BookingWizardViewModel : ViewModelBase` — **not `partial`** |
| Constructor | `(IBookingWorkflowService workflowService, IDialogService dialogService, Action? onBookingCreated = null)` |
| Dependencies | `IBookingWorkflowService` (Application), `IDialogService` (Presentation), `Action?` callback. **No `ILogger` of any kind.** |
| Existing `ILogger` field | **none** |
| Existing `[LoggerMessage]` | **none** |
| `using`s | no `Microsoft.Extensions.Logging` / `.Abstractions` |
| Lifetime | **Not DI-registered.** Transient, created per-wizard-open by the parent (`new`), lives for the dialog's lifetime, GC'd on dialog close. |
| Parent creation path | `BookingPageViewModel.OpenWizard()` (`:405–409`): `var wizard = new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync()); _dialogService.ShowDialog(wizard);` |
| Constructor-time load | ctor ends with `_ = LoadOptionsAsync();` (safe fire-and-forget — catches internally) |
| Error-string helper | `ToFriendlyErrorMessage(Exception)` — a `switch` mapping `ApiTimeoutException`/`ApiConnectivityException` → network string, everything else → generic string. **The caught exception is consumed only to pick a fixed localized string; the raw `Exception`/`.Message` is never surfaced.** |

### B.2 Catch boundaries (5)

| # | Method | Catch site | Form | Current behaviour on failure | Error handling | Logging suitability |
|---|---|---|---|---|---|---|
| 1 | `LoadOptionsAsync` | `:291–297` | `catch (Exception exception)` | `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;` | Friendly string + Error state; dialog stays open | **INSTRUMENT** — top-level load boundary, same shape as every instrumented page/profile VM. Fires at construction. |
| 2 | `AddGuestCustomerAsync` | `:393–403` (`finally` at `:404`) | `catch (Exception exception)` | `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;` then `finally { IsAddingGuestCustomer = false; }` | Friendly string + Error state; picker not hidden | **INSTRUMENT** — command boundary. **Handles guest full name + phone** — security-critical (see §D). |
| 3 | `LoadAvailableSlotsAsync` | `:484–490` | `catch (Exception exception)` | `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;` | Friendly string + Error state | **INSTRUMENT** — load boundary (slot query for specialist/service/date). |
| 4 | `SearchNextAvailableDateAsync` | `:538–543` (`finally` at `:544`) | `catch (Exception)` — **no exception variable** | Comment-only body (`// Swallowed by design`); `finally` resets `IsSearchingNextAvailableDate` if this probe is still current | **None** — deliberately silent; never touches `ErrorMessage`/`State` | **DO NOT INSTRUMENT** — see §B.3. |
| 5 | `ConfirmBookingAsync` | `:617–623` | `catch (Exception exception)` | `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;` | Friendly string + Error state | **INSTRUMENT** — command boundary. Request carries customer/service/specialist ids + names + price + duration + slot time + **notes** — security-critical (see §D). |

Line numbers unchanged from the Phase 8.42 audit (`BookingWizardViewModel` was not touched by Wave 2C-3a).

### B.3 `SearchNextAvailableDateAsync` — recommendation: **REMAIN SILENT**

- **Best-effort by design.** Fired as `_ = SearchNextAvailableDateAsync()` from `LoadAvailableSlotsAsync` only when the picked date returns zero slots. It probes forward up to `NextAvailableDateSearchWindowDays` (7) days via the same Backend-authoritative `GetAvailableSlotsAsync`, stopping at the first day with a slot.
- **Cancellation-dominated.** Every new date pick or fresh probe calls `CancelNextAvailableDateSearch()` → `cts.Cancel()`, and the loop opens with `cts.Token.ThrowIfCancellationRequested()`. In normal use a probe is routinely superseded and throws `OperationCanceledException` into this catch. Logging every superseded probe = noise on the happy path.
- **Never user-visible.** The catch touches neither `ErrorMessage` nor `State`; the primary empty-slots message from `LoadAvailableSlotsAsync` already stands. A failed probe simply leaves `SuggestedNextAvailableDate == null`.
- **Precedent-consistent.** Matches the MobileOtp rule ("typed/expected failures log nothing") and the wave's `when (exception is not OperationCanceledException)` convention. Instrumenting it would also mean either a 5th noisy call or a filtered catch — added surface for negative value.
- **Guarded by test** (§E) so the skip is a deliberate, asserted decision, not an omission.

**→ Instrument 4 of 5. Authorizer sign-off requested on the `SearchNextAvailableDateAsync` skip.**

---

## C. PARENT PLUMBING DECISION — `BookingPageViewModel`

**File:** `src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs`.

### C.1 Current state

| Aspect | Value |
|---|---|
| Declaration | `public sealed partial class BookingPageViewModel : ViewModelBase` (already `partial`) |
| `ILogger` field | `private readonly ILogger<BookingPageViewModel> _logger;` (`:62`) — **already present** (from `da18c18`) |
| `[LoggerMessage]` | `:506` — `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Booking operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation, Exception exception);` — **instance-form, LEGACY `(string operation, Exception exception)` signature** — it passes the `Exception` object to the logger (5 call sites: `LoadAsync`, `CreateBookingAsync`, `ChangeStatusAsync`, `CancelSelectedBookingAsync`, `RescheduleSelectedBookingAsync`). |
| DI | `services.AddTransient<BookingPageViewModel>();` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:62`) |
| Wizard creation | `OpenWizard()` `:407` — `new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync())` |

### C.2 The constraint

`BookingPageViewModel` **already has one `ILogger<T>` field + an instance-form `[LoggerMessage]`**. Adding a second `ILogger<BookingWizardViewModel>` field would make the `[LoggerMessage]` source generator fail the build with **`SYSLIB1020` ("multiple ILogger fields")** — the exact constraint that governed Wave 2C-3a's three page parents.

### C.3 Options

| Option | Description | Assessment |
|---|---|---|
| **A — `ILoggerFactory` pass-through** | Parent ctor `+ ILoggerFactory? loggerFactory = null` (appended last, after `logger`); `private readonly ILoggerFactory? _loggerFactory;`; at `OpenWizard()`, `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` passed as the wizard's new last ctor arg. `ILoggerFactory` is **not** `ILogger` → no `SYSLIB1020`. Parent's own `_logger` + legacy `[LoggerMessage]` **completely untouched**. | ✅ **SELECT.** Identical to the proven Wave 2C-3a pattern (`Customer`/`Service`/`InventoryPageViewModel` in `7aa1d1b`). Smallest blast radius — zero edits to the parent's committed legacy logging or its 5 call sites. `ILoggerFactory` already registered by `AddLogging()`; all params optional → no DI change, no call-site breakage. |
| **B — static-form `[LoggerMessage]` + 2nd typed field** | Convert the parent's legacy `[LoggerMessage]` to static form so a 2nd `ILogger<TChild>` field is allowed (the `AccountingPageViewModel` precedent). | ❌ Rejected — forces editing the parent's committed legacy `(operation, exception)` signature and all 5 of its call sites; large review surface; touches booking-hardening code out of this wave's scope. |
| **C — plain `ILogger<TChild>?` pass-through** (the `AutomationPageViewModel` pattern) | Only valid when the parent has **no** logger of its own. `BookingPageViewModel` has one → would trip `SYSLIB1020`. | ❌ Not applicable. |

### C.4 Selected design — Option A

```
BookingPageViewModel:
  + ILoggerFactory? loggerFactory = null        // ctor param, appended after the existing `logger`
  + private readonly ILoggerFactory? _loggerFactory;
  OpenWizard():
      new BookingWizardViewModel(
          _workflowService, _dialogService, () => _ = LoadAsync(),
          _loggerFactory?.CreateLogger<BookingWizardViewModel>())   // new last arg

BookingWizardViewModel:
  sealed  -> sealed partial
  + using Microsoft.Extensions.Logging;  + using Microsoft.Extensions.Logging.Abstractions;
  + private readonly ILogger<BookingWizardViewModel> _logger;
  ctor + ILogger<BookingWizardViewModel>? logger = null    // appended AFTER `Action? onBookingCreated = null`
  _logger = logger ?? NullLogger<BookingWizardViewModel>.Instance;
  + [LoggerMessage(EventId = 1, Level = LogLevel.Error,
       Message = "Booking wizard operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);          // (string operation) ONLY — no Exception param
  4 calls: LogOperationFailed(nameof(<Method>)) as the LAST statement of catches 1/2/3/5,
           AFTER the unchanged `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;`
```

- Child's `[LoggerMessage]` uses the **new-style `(string operation)`** form (like every Wave-2 VM), **not** the parent's legacy exception-passing form. Different classes, different signatures — no adjacency problem, the parent's is untouched.
- Child has exactly one `ILogger` field → instance-form `[LoggerMessage]` is `SYSLIB1020`-safe in the child.
- `NullLogger<BookingWizardViewModel>.Instance` fallback → every existing `new BookingWizardViewModel(...)` (production `OpenWizard` when `_loggerFactory` is null; all 30+ existing tests) compiles and runs unchanged.

---

## D. SECURITY RULES

`BookingWizardViewModel` is the **most PII-dense ViewModel in the logging track** — guest identity, phone, and free-text booking notes all pass through instrumented catches.

### D.1 Sensitive data reachable from the 4 instrumented boundaries

| Source | Field(s) | Boundary |
|---|---|---|
| Guest identity | `GuestFullName`, `GuestPhone` → `CreateGuestCustomerAsync(GuestFullName.Trim(), GuestPhone.Trim())` | `AddGuestCustomerAsync` |
| Selected customer | `SelectedCustomer.Id`, `SelectedCustomer.FullName`, `IsLinkedToAccount` | `ConfirmBookingAsync` |
| Selected service | `SelectedService.Id`, `.Name`, `.DurationMinutes`, `.Price` | `ConfirmBookingAsync` |
| Selected specialist | `SelectedSpecialist.Id`, `.FullName` | `ConfirmBookingAsync`, `LoadAvailableSlotsAsync` |
| Appointment | `SelectedSlot.Start` (slot time), `SelectedDate` | `ConfirmBookingAsync`, `LoadAvailableSlotsAsync` |
| **Booking notes** | `Notes` (free text) → `CreateBookingWorkflowRequest(..., Notes)` | `ConfirmBookingAsync` |
| Backend responses | embedded in `ApiException.Message` by `AuthBootstrapHttpClient` | all four |

### D.2 The rule (non-negotiable)

**ALLOWED in a log line:** `Operation=<nameof(Method)>` and nothing else.

**FORBIDDEN — must never appear in any log this change produces:**
- Guest full name, guest phone, any phone number
- Booking notes / appointment notes
- Appointment times / slot times / `SelectedDate`
- Selected service (name / price / duration / id), selected specialist (name / id)
- Customer identity — `SelectedCustomer.Id` / `.FullName`, guest id
- Tokens (bearer / session)
- Backend response bodies
- `Exception.Message`
- the `Exception` object itself

### D.3 How the design guarantees it

| Guarantee | Mechanism |
|---|---|
| `Exception` object never passed | child `[LoggerMessage]` signature is `(string operation)` — **no `Exception` parameter** (unlike the parent's legacy form) |
| `Exception.Message` never logged | call sites pass `nameof(<Method>)` only; `ToFriendlyErrorMessage(exception)` output is a fixed localized string assigned to `ErrorMessage` (UI), never logged |
| No field data logged | the message template is a constant `"Booking wizard operation failed. Operation={Operation}"` with one `string` arg |
| Test-enforced | each failure test seeds a recognizable secret (guest name/phone, notes) into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)` |

Level `Error` clears the `LocalFileLoggerProvider` `Warning` floor. `EventId = 1`.

---

## E. TEST PLAN

### E.1 Existing coverage

- `tests/Rojan.Desktop.Presentation.Tests/BookingWorkflow/BookingWizardViewModelTests.cs` (594 lines, ~35 tests) — covers all 5 catch paths behaviourally (state/message assertions) but **no logging assertions**.
- `tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs` — `MakeSut(...)` helper; covers `OpenWizardCommand` shows a dialog.
- Test doubles (all reusable as-is, **no stub change**):
  - `StubBookingWorkflowService` — per-operation delegate ctor params (`getOptions:`, `getSlots:`, `createBooking:`, `createGuestCustomer:`); each accepts a throwing `Task.FromException(...)`.
  - `StubDialogService` — `ShownDialogs` list captures the wizard VM instance.
  - `RecordingLogger<T>` (`tests/…/Specialists/RecordingLogger.cs`) — records `(Level, Message)`.
  - **`RecordingLoggerFactory`** (`tests/…/Specialists/RecordingLoggerFactory.cs`) — **already committed in `7aa1d1b`** (Wave 2C-3a). Records `(Category, Level, Message)`. **No new test helper needed for this wave.**

### E.2 New tests (~6–7)

| # | File | Test | Asserts |
|---|---|---|---|
| 1 | `BookingWizardViewModelTests` | `Constructor_OptionsQueryThrows_LogsErrorWithOperationNameOnly` | `RecordingLogger` has one `Error` entry, `Operation=LoadOptionsAsync`, seeded secret absent |
| 2 | `BookingWizardViewModelTests` | `AddGuestCustomerCommand_Failure_LogsErrorWithOperationNameOnly_NoGuestPiiLeak` | `Operation=AddGuestCustomerAsync`; seeded `"Walk-in Guest / 555-0100"` absent from message |
| 3 | `BookingWizardViewModelTests` | `NextCommand_FromDateStep_SlotsQueryThrows_LogsErrorWithOperationNameOnly` | `Operation=LoadAvailableSlotsAsync`; secret absent |
| 4 | `BookingWizardViewModelTests` | `ConfirmBookingCommand_Failure_LogsErrorWithOperationNameOnly_NoNotesOrCustomerLeak` | `Operation=ConfirmBookingAsync`; seeded notes + customer/service/specialist names absent |
| 5 | `BookingWizardViewModelTests` | `SearchNextAvailableDateAsync_ProbeFails_LogsNothing` | picked date returns `[]` (→ Empty, fires probe), candidate dates throw; `RecordingLogger.Entries` is **empty** — guards the §B.3 skip |
| 6 | `BookingWizardViewModelTests` | `Constructor_OptionsQueryThrows_WithoutLogger_UsesNullLogger_NeverThrows` | no logger arg → `State == Error`, no throw |
| 7 | `BookingPageViewModelTests` | `LoggerFactory_ForwardedToWizardChild_ChildLoadFailureIsLoggedViaTheFactory` | `MakeSut(..., workflowService: throwingOptions, dialogService: dlg, loggerFactory: recordingFactory)`; execute `OpenWizardCommand`; `dlg.ShownDialogs` single; `recordingFactory.Entries` single `Error`, category contains `BookingWizardViewModel`, `Operation=LoadOptionsAsync`, secret absent |

`MakeSut` in `BookingPageViewModelTests` gains one optional `ILoggerFactory? loggerFactory = null` param forwarded as the new last ctor arg (additive test-helper change; existing calls unaffected).

### E.3 No existing test body changes

All 4 instrumented boundaries already have behavioural tests that pass a throwing delegate; the new tests are additive. Existing `new BookingWizardViewModel(workflowService, new StubDialogService())` calls keep compiling (new param optional).

---

## F. IMPLEMENTATION RECOMMENDATION

### F.1 Scope — 4 files

| # | File | Change |
|---|---|---|
| 1 | `src/…/ViewModels/BookingWorkflow/BookingWizardViewModel.cs` | `sealed`→`sealed partial`; +2 `using`; `ILogger<BookingWizardViewModel> _logger` field; ctor `+ ILogger<BookingWizardViewModel>? logger = null` (appended after `Action? onBookingCreated = null`); `?? NullLogger<>.Instance`; 1 instance-form `[LoggerMessage(EventId=1, Level=Error, "Booking wizard operation failed. Operation={Operation}")]`; **4** `LogOperationFailed(nameof(...))` calls — `LoadOptionsAsync`, `AddGuestCustomerAsync`, `LoadAvailableSlotsAsync`, `ConfirmBookingAsync` — each the last statement of the existing catch, after the unchanged `ErrorMessage`/`State`. `SearchNextAvailableDateAsync` catch/`finally`/`_nextAvailableDateSearchCts` **unchanged**. |
| 2 | `src/…/ViewModels/Bookings/BookingPageViewModel.cs` | `+ ILoggerFactory? loggerFactory = null` ctor param (after existing `logger`); `+ private readonly ILoggerFactory? _loggerFactory;`; `OpenWizard()` passes `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` as the wizard's new last arg. **Existing `_logger` field + legacy `[LoggerMessage]` + all 5 of its call sites: UNTOUCHED.** |
| 3 | `tests/…/BookingWorkflow/BookingWizardViewModelTests.cs` | +6 tests (E.2 #1–6) |
| 4 | `tests/…/Bookings/BookingPageViewModelTests.cs` | +1 test (E.2 #7) + additive `loggerFactory` param on `MakeSut` |

**No new files.** `RecordingLoggerFactory` already exists (`7aa1d1b`).

### F.2 Not touched

`BookingWizardStep.cs`, `IBookingWorkflowService` / any interface, any DTO, `IDialogService`, `StubBookingWorkflowService`, `StubDialogService`, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, DI registrations, Domain, Infrastructure, Shell, Application, backend contracts, RBAC, auth, navigation, the other detail/profile VMs (Wave 2C-3c).

### F.3 Validation gates (before and after commit)

```
dotnet build -c Debug     → 0 warnings / 0 errors   (watch SYSLIB1020 — the ILoggerFactory design prevents it)
dotnet test  -c Debug     → 2,587 + ~7 = ~2,594 / all pass
architecture tests        → 7 / 7
```

Expected test-count delta: **+7** (2,587 → ~2,594).

---

## G. ARCHITECTURE REVIEW

| Area | Impact | Why |
|---|---|---|
| DI | **none** | No registration added/changed. `ILoggerFactory` already registered by `AddLogging()` (`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs:91`). All new ctor params optional. `BookingWizardViewModel` stays `new`-by-parent, not DI-registered. |
| Domain | **none** | Presentation-only edit. |
| Backend contracts | **none** | No API client, DTO, or request/response type touched. `ToFriendlyErrorMessage` mapping unchanged. Backend remains sole authority for eligibility/availability/booking creation. |
| RBAC | **none** | No permission gate touched. |
| Authentication | **none** | Not referenced. |
| Navigation | **none** | Wizard is a dialog via `IDialogService`, not the nav stack; `OpenWizard()` control flow unchanged bar the one added ctor arg. |
| Presentation→Infrastructure/Domain dependency rule | **safe** | `Microsoft.Extensions.Logging` + `.Abstractions` are already Presentation `PackageReference`s (used by `BookingPageViewModel` today); `DependencyDirectionTests` explicitly allows `Microsoft.Extensions.Logging.Abstractions`. |
| `ViewModelTestabilityTests` | **safe** | No `System.Windows.Threading` / `System.Windows.Controls` introduced. |
| Architecture tests | **7 / 7 hold** | |

---

## H. COMMIT STRATEGY

**One isolated commit.**

| TASK 7 factor | Assessment |
|---|---|
| Security sensitivity | High (guest PII + notes) — but a single-purpose diff of one child VM + one parent plumbing param is *easier* to security-review isolated, not harder. Splitting further (e.g. per-catch) would fragment the security review. |
| Parent plumbing complexity | Low — one optional `ILoggerFactory?` param on one parent, proven pattern from `7aa1d1b`. |
| Review surface | ~4 files, ~4 production catch calls + 1 plumbing hop + 7 tests. Well within a single reviewable unit. |
| Wave already isolated | Wave 2C-3a shipped separately in `7aa1d1b`; BookingWizard is its own wave (2C-3b). The Phase 8.42 "split into two commits" recommendation referred to separating BookingWizard *from the profile panels* — **already done**. |

**Recommended commit subject (exact):**
```
fix(desktop): add ViewModel diagnostic logging (booking wizard)
```
Trailers: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` + `Claude-Session: …` (Team 3 arc convention).
Staging: `git reset` → 4 explicit `git add <path>` — never `git add .` / `-A`.
No push / merge / rebase / amend.

---

## I. OPEN QUESTION FOR THE AUTHORIZER

1. **Confirm the `SearchNextAvailableDateAsync` skip** (§B.3) — instrument 4 of 5 catches, leave the best-effort cancellable probe silent, guarded by test E.2 #5. Recommendation: **skip**.

---

## STOP

Audit complete. No source or test change, no logger added, no commit/push/merge/rebase/amend.
HEAD remains `7aa1d1b`. **Awaiting Wave 2C-3b implementation authorization (Phase 8.47).**
