# ROJAN AI — TEAM 3 — PHASE 8.42 — WAVE 2C-3 DETAIL/PROFILE + BOOKINGWIZARD LOGGING — SCOPE AUDIT v1

**Type:** Scope audit only. **No source modified. No logger added. No tests added. No commit.**
**Branch:** `feature/team3-desktop-completion`
**Reference:** `ROJAN_PHASE8_41_AUTOMATION_LOGGING_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`.

---

## A. Git State

| Item | Value |
|---|---|
| HEAD | `c01d0ce17f964ceca235291dff3123b580088101` (`c01d0ce` — *fix(desktop): add ViewModel diagnostic logging (automation tabs)*, Phase 8.39 / Wave 2C-2) |
| Branch | `feature/team3-desktop-completion` |
| Tracked working tree | **clean** — `git status` shows only untracked `.md` reports |
| Tests at HEAD | 2,576 / 2,576 pass (Presentation.Tests 633) |
| Architecture | 7 / 7 |
| Self-logging ViewModel coverage | 25 of 56 |

---

## B. ViewModel Inventory

### B.1 In-scope (per authorization TASK 2)

All four are `public sealed class : ViewModelBase`, **none holds any `ILogger` field**, all are
**`new`-by-parent** (never DI-resolved — each needs an id/selection known only at runtime), and all
**fire-and-forget their load in the constructor** (`_ = LoadAsync()` / `_ = LoadOptionsAsync()`).

| VM | Lines | Ctor dependencies | Broad `catch` sites | Constructed by |
|---|---|---|---|---|
| `Customers/CustomerProfileViewModel` | 314 | `string customerId`, `ICustomerProfileQueryService`, `ICustomerCommandService` | **1** — `LoadAsync` (:239, `catch (Exception exception)`) | `CustomerPageViewModel.SelectedCustomer` setter (`:159`) |
| `Services/ServiceProfileViewModel` | 345 | `string serviceId`, `IServiceProfileQueryService`, `IServiceCommandService`, `IIntelligenceEngine` | **3** — `LoadAsync` (:237, `catch (Exception exception)`), `SaveChangesAsync` (:292, `catch (Exception)` — no var), `DeactivateAsync` (:338, `catch (Exception)` — no var) | `ServicePageViewModel.SelectedService` setter (`:244`) |
| `Inventory/InventoryProfileViewModel` | 187 | `string productId`, `IProductProfileQueryService`, `IInventoryCommandService` | **1** — `LoadAsync` (:153, `catch (Exception exception)`) | `InventoryPageViewModel.SelectedProduct` setter (`:138`) |
| `BookingWorkflow/BookingWizardViewModel` | 646 | `IBookingWorkflowService`, `IDialogService`, `Action? onBookingCreated = null` | **5** — `LoadOptionsAsync` (:292), `AddGuestCustomerAsync` (:394), `LoadAvailableSlotsAsync` (:485), `SearchNextAvailableDateAsync` (:539, `catch (Exception)` — best-effort, swallowed by design, has `finally`), `ConfirmBookingAsync` (:618) | `BookingPageViewModel.OpenWizard()` (`:407`) |

### B.2 Error-handling pattern (verified)

| Site type | Shape |
|---|---|
| Profile-VM `LoadAsync` (×3) | `catch (Exception exception) { ErrorMessage = exception.Message; State = DashboardState.Error; }` — under `#pragma warning disable CA1031`, identical to every page/profile VM |
| `ServiceProfileViewModel.SaveChangesAsync` / `DeactivateAsync` | `catch (Exception) { SaveErrorMessage = Strings.Services_SaveError; HasSaveError = true; /* revert edit buffers */ }` — **no exception variable**, friendly localized string only |
| BookingWizard `LoadOptionsAsync` / `AddGuestCustomerAsync` / `LoadAvailableSlotsAsync` / `ConfirmBookingAsync` | `catch (Exception exception) { ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error; }` — the exception is consumed **only** to build a friendly user string |
| BookingWizard `SearchNextAvailableDateAsync` | `catch (Exception) { /* swallowed by design */ } finally { … }` — a cancellable best-effort "next available date" probe; cancellation is the dominant path |

**Every catch swallows (no rethrow).** A `[LoggerMessage]` call appended as the last statement — after
the unchanged state/error assignment — is the same append-only instrumentation used in Waves 1 / 2A /
2B / 2C-1 / 2C-2.

### B.3 Instrumentable catch count

| VM | Instrument | Skip | Rationale for skip |
|---|---|---|---|
| `CustomerProfileViewModel` | 1 | 0 | — |
| `ServiceProfileViewModel` | 3 | 0 | — |
| `InventoryProfileViewModel` | 1 | 0 | — |
| `BookingWizardViewModel` | **4** | 1 (`SearchNextAvailableDateAsync`) | Best-effort probe, swallowed by design, dominated by expected `OperationCanceledException` from `_nextAvailableDateSearchCts`; never surfaces to the user. Logging every cancelled probe = noise. Consistent with the MobileOtp precedent ("typed/expected failures log nothing") and the `when (exception is not OperationCanceledException)` convention. **Flagged for the authorizer** — instrument only if explicitly wanted. |
| **Total** | **9** | 1 | |

### B.4 Out of named scope (related `new`-by-parent VMs — NOT this wave)

| VM | Catches | Constructed by | Note |
|---|---|---|---|
| `HR/EmployeeProfileViewModel` | 1 (`LoadAsync`) | `HrPageViewModel.SelectedEmployee` setter (`:250`) | Same shape as the 3 profile VMs; deferrable to a 2C-3 follow-up |
| `Accounting/InvoiceProfileViewModel` | 1 (`LoadAsync`) | `AccountingPageViewModel` (`:113`) | Parent already uses the static-form `[LoggerMessage]` + `_posCheckoutLogger` pass-through — a 3rd `ILogger` is trivial there; still, out of named scope |
| `Specialists/SpecialistProfileViewModel` | 4 | `SpecialistPageViewModel` (`:181`) | Already receives `_scheduleLogger` / `_availabilityLogger` **for its own grandchildren** but has no `ILogger<SpecialistProfileViewModel>` of its own; deferrable |

**Recommendation:** keep this wave to the 4 authorized VMs; sweep Employee/Invoice/SpecialistProfile
in a short "Wave 2C-3c" afterward if desired.

### B.5 Existing tests

| VM | Test file | Lines | Local stub(s) — delegate-driven, already support injecting a throwing task |
|---|---|---|---|
| `CustomerProfileViewModel` | `tests/…/Customers/CustomerProfileViewModelTests.cs` | 336 | `StubCustomerProfileQueryService` (`(customerId, ct) => Task<CustomerProfileDto>` factory), `StubCustomerCommandService` |
| `ServiceProfileViewModel` | `tests/…/Services/ServiceProfileViewModelTests.cs` | 199 | `StubServiceProfileQueryService` (delegate factory), `StubServiceCommandService`, `StubIntelligenceEngine` |
| `InventoryProfileViewModel` | `tests/…/Inventory/InventoryProfileViewModelTests.cs` | 143 | `StubProductProfileQueryService` (delegate factory), `StubInventoryCommandService` |
| `BookingWizardViewModel` | `tests/…/BookingWorkflow/BookingWizardViewModelTests.cs` | 594 | `StubBookingWorkflowService` (per-operation delegate params: `getOptions:`, `getSlots:`, `createBooking:`, `createGuest:` …), `StubDialogService` |

`RecordingLogger<T>` — `tests/Rojan.Desktop.Presentation.Tests/Specialists/RecordingLogger.cs`
(namespace `…Tests.Specialists`) — reusable via `using`.

---

## C. Dependency / Lifecycle & Plumbing Analysis

### C.1 Classification

| VM | Type | DI? |
|---|---|---|
| All 4 in-scope | **(B) child ViewModels requiring parent logger pass-through** | No — `new`-by-parent |

| Parent (DI `AddTransient`) | Owns / creates | Creation path |
|---|---|---|
| `CustomerPageViewModel` | `CustomerProfileViewModel` | `SelectedCustomer` setter → `new CustomerProfileViewModel(value.Id, _profileQueryService, _commandService)` |
| `ServicePageViewModel` | `ServiceProfileViewModel` | `SelectedService` setter → `new ServiceProfileViewModel(value.Id, _profileQueryService, _commandService, _intelligenceEngine)` |
| `InventoryPageViewModel` | `InventoryProfileViewModel` | `SelectedProduct` setter → `new InventoryProfileViewModel(value.Id, _profileQueryService, _commandService)` |
| `BookingPageViewModel` | `BookingWizardViewModel` | `OpenWizard()` → `new BookingWizardViewModel(_workflowService, _dialogService, () => _ = LoadAsync())` |

### C.2 THE PLUMBING CONSTRAINT — SYSLIB1020

**All four parents already carry exactly one `ILogger<TSelf> _logger` field plus an instance-form
`[LoggerMessage]`** (Wave 2A for Customer/Service/Inventory `75357e1`; `da18c18` for Booking):

| Parent | `[LoggerMessage]` form | field count today |
|---|---|---|
| `CustomerPageViewModel` | instance — `private partial void LogOperationFailed(string operation)` | 1 |
| `ServicePageViewModel` | instance — same | 1 |
| `InventoryPageViewModel` | instance — same | 1 |
| `BookingPageViewModel` | instance — `private partial void LogOperationFailed(string operation, Exception exception)` (legacy, still passes the exception) | 1 |

Adding a **second** `ILogger<TChild>` pass-through **field** to any of these makes the `[LoggerMessage]`
source generator fail with **`SYSLIB1020` "Found multiple fields of type … ILogger"** (the
`AccountingPageViewModel` precedent — it went to the static form precisely for this).

### C.3 Resolution — `ILoggerFactory` pass-through (RECOMMENDED)

`ILoggerFactory` is **not** `ILogger`, so a stored `ILoggerFactory` field does **not** count toward the
`SYSLIB1020` "multiple ILogger fields" rule.

- Parent ctor gains **`ILoggerFactory? loggerFactory = null`** (one optional param, appended last).
- Stored as `private readonly ILoggerFactory? _loggerFactory;`.
- At the `new` site: `loggerFactory?.CreateLogger<CustomerProfileViewModel>()` passed to the child.
  (`LoggerFactoryExtensions.CreateLogger<T>` — namespace `Microsoft.Extensions.Logging`, in the
  already-referenced `Microsoft.Extensions.Logging.Abstractions` assembly.)
- **Zero change** to the parent's existing `_logger` field or `[LoggerMessage]` — including
  `BookingPageViewModel`'s legacy exception-passing one (untouched, out of scope).
- `ILoggerFactory` is registered by `AddLogging()` (`Infrastructure/DependencyInjection/…:86` comment
  confirms it). All params optional → no DI registration change, no call-site breakage.

**Alternative (NOT recommended):** convert each parent's instance-form `[LoggerMessage]` to the
static form (`AccountingPageViewModel` precedent). Works, but edits four already-committed files and
every one of their call sites, and would force a decision on `BookingPageViewModel`'s legacy
exception-passing signature. `ILoggerFactory` keeps this wave's blast radius to the child VMs +
one appended parameter per parent.

### C.4 Child VM shape (each of the 4)

Standard self-logging shape, unchanged from Waves 1 / 2A / 2B / 2C-1 / 2C-2:
- `sealed class` → `sealed partial class`
- `+ using Microsoft.Extensions.Logging;` `+ using Microsoft.Extensions.Logging.Abstractions;`
- `private readonly ILogger<TSelf> _logger;`
- ctor `+ ILogger<TSelf>? logger = null` (optional, appended last) → existing tests still compile
- `_logger = logger ?? NullLogger<TSelf>.Instance;`
- one **instance-form** partial (each child has exactly 1 `ILogger` field → no `SYSLIB1020`):
  ```csharp
  [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "<area> profile operation failed. Operation={Operation}")]
  private partial void LogOperationFailed(string operation);
  ```
  BookingWizard message: `"Booking wizard operation failed. Operation={Operation}"`.
- in each instrumented catch, **after** the unchanged state/error assignment:
  `LogOperationFailed(nameof(<Method>));`

### C.5 Architecture-test impact (TASK 5)

**None expected.**

| Layer | Change |
|---|---|
| DI architecture | none — no registration added/changed; `ILoggerFactory` already registered; all new params optional |
| `ServiceCollectionExtensions.cs` | none |
| Domain | none |
| Backend contracts (API clients, DTOs, endpoints) | none |
| RBAC / permissions | none |
| Authentication / session | none |
| Navigation | none |
| Interfaces (`I*.cs`) | none |

`Microsoft.Extensions.Logging` / `.Abstractions` is already a Presentation `PackageReference` and is
**not** on the `DependencyDirectionTests` forbidden list. No `System.Windows.Threading` /
`System.Windows.Controls` type introduced. **Architecture tests: 7 / 7 unchanged.**

---

## D. Security Constraints

**Design rule (unchanged since Phase 8.15): operation name only. The `Exception` is NEVER passed to
the logger.** `[LoggerMessage]` signature `(string operation)`; every call site passes `nameof(...)`.

The only log lines this wave can produce (4 distinct messages × operation name, all `Error`):

```
<ts> [Error] …CustomerProfileViewModel:  Customer profile operation failed. Operation=LoadAsync
<ts> [Error] …ServiceProfileViewModel:   Service profile operation failed. Operation=SaveChangesAsync
<ts> [Error] …InventoryProfileViewModel: Inventory profile operation failed. Operation=LoadAsync
<ts> [Error] …BookingWizardViewModel:    Booking wizard operation failed. Operation=ConfirmBookingAsync
```

### D.1 FORBIDDEN — must never reach the log

| Category | Concrete fields in these VMs |
|---|---|
| `Exception` object / `Exception.Message` | never passed — `ToFriendlyErrorMessage(exception)` consumes it for the UI string only; the logger never sees it |
| Backend response body | only ever inside `Exception.Message` — never passed |
| **Customer PII** | `CustomerDto` full name, company, email, phone, lifetime value, notes; activity timeline text |
| **Guest PII** (`BookingWizardViewModel.AddGuestCustomerAsync`) | `GuestFullName`, `GuestPhone` (passed to `CreateGuestCustomerAsync`) |
| **Booking details** (`ConfirmBookingAsync` request) | selected customer id/name, service name/price/duration, specialist id/name, slot start time, **`Notes`** |
| Service data | service name, description, **price** (`EditablePrice` / `PriceValue`), duration |
| Product / inventory data | product name, SKU, category, supplier, **cost/price**, stock levels, transaction notes |
| Identifiers | `_customerId`, `_serviceId`, `_productId`, org/branch ids |
| Tokens (bearer / session) | not held by these VMs |

### D.2 ALLOWED

- `nameof(LoadAsync)` / `nameof(SaveChangesAsync)` / `nameof(DeactivateAsync)` /
  `nameof(LoadOptionsAsync)` / `nameof(AddGuestCustomerAsync)` / `nameof(LoadAvailableSlotsAsync)` /
  `nameof(ConfirmBookingAsync)`
- `LogLevel.Error` (clears the `LocalFileLoggerProvider` `Warning` floor)
- `EventId = 1` per class

### D.3 Behaviour-preservation checklist (per site)

- `#pragma warning disable/restore CA1031` unchanged
- `ErrorMessage = exception.Message;` / `ErrorMessage = ToFriendlyErrorMessage(exception);` /
  `SaveErrorMessage = Strings.Services_SaveError; HasSaveError = true;` + edit-buffer revert — all unchanged
- `State = DashboardState.Error;` unchanged where present
- BookingWizard `finally` block and `_nextAvailableDateSearchCts` handling unchanged
- log call appended as the **last** statement of the catch
- parents: `SelectedX` setter logic / `OpenWizard()` unchanged except the one added constructor argument

---

## E. Test Strategy

### E.1 Reuse, don't rebuild

- **`RecordingLogger<T>`** (`…Tests.Specialists`) — reuse via `using`.
- All four existing local query/workflow stubs are **delegate-driven** and already accept a factory —
  a failing case needs only `(_, _) => Task.FromException<TDto>(new InvalidOperationException(secret))`;
  **no stub file modification** for the child-VM failure tests.
- Existing test constructors (`new CustomerProfileViewModel("customer-1", profileQuery, commandService)`
  etc.) compile **unchanged** — the child `logger` param and the parent `loggerFactory` param are optional.

### E.2 One small new test-only helper

The parent pass-through tests need an `ILoggerFactory` that hands back a `RecordingLogger<T>`. Add a
**private nested** `sealed class RecordingLoggerFactory : ILoggerFactory` (or one shared internal helper
in `tests/…/` — **not** a production stub) that returns a caller-supplied `RecordingLogger<T>` from
`CreateLogger`. ~15 lines. No shared production stub touched.

### E.3 Test matrix

| Target | Tests |
|---|---|
| `CustomerProfileViewModel` | (1) `LoadAsync` failure → 1 `Error` entry, `Operation=LoadAsync`, seeded PII (`"Amelia Hart"` / `"555-0100"`) absent; (2) no logger → `NullLogger`, `State=Error`, no throw |
| `ServiceProfileViewModel` | (1) `LoadAsync` failure logs `Operation=LoadAsync`, price/name absent; (2) `SaveChangesAsync` failure logs `Operation=SaveChangesAsync` + edit buffers still revert; (3) `DeactivateAsync` failure logs `Operation=DeactivateAsync`; (4) no logger → `NullLogger` safety |
| `InventoryProfileViewModel` | (1) `LoadAsync` failure logs `Operation=LoadAsync`, SKU/supplier/cost absent; (2) no-logger safety |
| `BookingWizardViewModel` | (1) `LoadOptionsAsync` failure logs `Operation=LoadOptionsAsync`; (2) `AddGuestCustomerAsync` failure logs `Operation=AddGuestCustomerAsync`, seeded guest name/phone absent; (3) `LoadAvailableSlotsAsync` failure logs `Operation=LoadAvailableSlotsAsync`; (4) `ConfirmBookingAsync` failure logs `Operation=ConfirmBookingAsync`, booking notes absent; (5) `SearchNextAvailableDateAsync` cancellation still logs **nothing** (guards the skip decision); (6) no-logger safety |
| Parent pass-through (×4) | one test per parent: construct parent with a `RecordingLoggerFactory`, drive the child into its `LoadAsync` failure, assert the recorder captured the child's `Operation=LoadAsync` entry (proves `CreateLogger<TChild>()` forwarding) |

**Estimated delta: +19 to +22** tests. Projected total ≈ **2,595–2,598**. Exact number reported at
implementation.

### E.4 Avoided

- No shared production stub modified.
- No change to any existing test body (all-additive).
- No `StubCurrentSessionService` / `FakeCurrentSessionService` / `RecordingLogger.cs` change.

---

## F. Recommended Implementation Sequence

**Split into two commits** — TASK 7 triggers **do fire** for BookingWizard:

| TASK 7 trigger | Assessment |
|---|---|
| BookingWizard has higher risk | **Yes** — 646 lines, 5 catches, guest PII + booking notes + selected-customer/service/specialist data, `ToFriendlyErrorMessage` exception consumption, dialog lifecycle, and its parent `BookingPageViewModel` carries the **legacy exception-passing `[LoggerMessage]`** (adjacency risk) |
| Customer data creates additional security review | **Yes** — CustomerProfile + BookingWizard both touch name/phone/email; a PII-focused diff is easier to review isolated |
| Parent plumbing increases scope | **Moderate** — 4 parents each get one `ILoggerFactory?` param; the `SYSLIB1020` constraint (§C.2) means the plumbing choice must be reviewed, not rubber-stamped |

### Commit A — Wave 2C-3a: Profile ViewModels

| # | File | Change |
|---|---|---|
| 1 | `…/ViewModels/Inventory/InventoryProfileViewModel.cs` | self-logging shape, 1 call site (`LoadAsync`) |
| 2 | `…/ViewModels/Customers/CustomerProfileViewModel.cs` | self-logging shape, 1 call site (`LoadAsync`) |
| 3 | `…/ViewModels/Services/ServiceProfileViewModel.cs` | self-logging shape, 3 call sites (`LoadAsync`, `SaveChangesAsync`, `DeactivateAsync`) |
| 4 | `…/ViewModels/Inventory/InventoryPageViewModel.cs` | `+ILoggerFactory? loggerFactory = null` ctor param + field; `loggerFactory?.CreateLogger<InventoryProfileViewModel>()` at `:138` |
| 5 | `…/ViewModels/Customers/CustomerPageViewModel.cs` | same, `CreateLogger<CustomerProfileViewModel>()` at `:159` |
| 6 | `…/ViewModels/Services/ServicePageViewModel.cs` | same, `CreateLogger<ServiceProfileViewModel>()` at `:244` |
| 7 | `tests/…/Inventory/InventoryProfileViewModelTests.cs` | +2 |
| 8 | `tests/…/Customers/CustomerProfileViewModelTests.cs` | +2 |
| 9 | `tests/…/Services/ServiceProfileViewModelTests.cs` | +4 |
| 10 | `tests/…/{Customers,Services,Inventory}PageViewModelTests.cs` | +1 pass-through test each (3) + shared `RecordingLoggerFactory` helper |

Subject: `fix(desktop): add ViewModel diagnostic logging (profile panels)`

### Commit B — Wave 2C-3b: Booking Wizard

| # | File | Change |
|---|---|---|
| 1 | `…/ViewModels/BookingWorkflow/BookingWizardViewModel.cs` | self-logging shape, 4 call sites (`LoadOptionsAsync`, `AddGuestCustomerAsync`, `LoadAvailableSlotsAsync`, `ConfirmBookingAsync`) — `SearchNextAvailableDateAsync` deliberately **not** instrumented (§B.3) |
| 2 | `…/ViewModels/Bookings/BookingPageViewModel.cs` | `+ILoggerFactory? loggerFactory = null` ctor param + field; `loggerFactory?.CreateLogger<BookingWizardViewModel>()` at `OpenWizard()` `:407`. **Existing `_logger` / legacy `[LoggerMessage]` untouched.** |
| 3 | `tests/…/BookingWorkflow/BookingWizardViewModelTests.cs` | +5–6 (four catch sites + cancellation-logs-nothing guard + no-logger) |
| 4 | `tests/…/Bookings/BookingPageViewModelTests.cs` | +1 pass-through test |

Subject: `fix(desktop): add ViewModel diagnostic logging (booking wizard)`

### Validation (each commit)

`dotnet build` (0/0 — **watch for `SYSLIB1020`**; the `ILoggerFactory` design should prevent it),
`dotnet test --no-build` (all pass), architecture 7 / 7.

### Not touched (either commit)

DI registration, any interface, any DTO, Domain / Infrastructure / Shell / Application, any API client,
RBAC, navigation, authentication, `RecordingLogger.cs`, any shared production stub, any existing test body.

---

## STOP

Audit complete. No source / tests / DI / stubs modified. No commit, no push.
`ROJAN_PHASE8_42_DETAIL_PROFILE_BOOKINGWIZARD_LOGGING_SCOPE_AUDIT_v1.md` written. Awaiting Phase 8.43
(implementation authorization for Wave 2C-3a) or scope adjustment.
