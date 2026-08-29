# ROJAN AI — TEAM 3 — PHASE 8.43 — PROFILE PANELS LOGGING (WAVE 2C-3a) — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `c01d0ce` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_42_DETAIL_PROFILE_BOOKINGWIZARD_LOGGING_SCOPE_AUDIT_v1.md`
**Scope:** Profile Panel ViewModels only. **`BookingWizardViewModel` / `BookingPageViewModel` untouched.**

---

## A. Files Changed (13 — 12 modified + 1 new)

### A.1 Production (6)

| # | File | Change | Instrumented catches |
|---|---|---|---|
| 1 | `…/ViewModels/Customers/CustomerProfileViewModel.cs` | `sealed`→`sealed partial`; +2 `using`; `ILogger<T> _logger` field; ctor `+ILogger<T>? logger = null` (4th, optional); `?? NullLogger<T>.Instance`; 1 instance-form `[LoggerMessage(EventId=1, Level=Error, "Customer profile operation failed. Operation={Operation}")]`; 1 call | `LoadAsync` |
| 2 | `…/ViewModels/Services/ServiceProfileViewModel.cs` | same shape; message `"Service profile operation failed. Operation={Operation}"`; 3 calls | `LoadAsync`, `SaveChangesAsync`, `DeactivateAsync` |
| 3 | `…/ViewModels/Inventory/InventoryProfileViewModel.cs` | same shape; message `"Inventory profile operation failed. Operation={Operation}"`; 1 call | `LoadAsync` |
| 4 | `…/ViewModels/Customers/CustomerPageViewModel.cs` | `+ILoggerFactory? loggerFactory = null` ctor param (appended after the existing optional `logger`) + `private readonly ILoggerFactory? _loggerFactory;`; child `new` at `:159` now passes `_loggerFactory?.CreateLogger<CustomerProfileViewModel>()` | — (plumbing) |
| 5 | `…/ViewModels/Services/ServicePageViewModel.cs` | same; child `new` at `:244` passes `_loggerFactory?.CreateLogger<ServiceProfileViewModel>()` | — |
| 6 | `…/ViewModels/Inventory/InventoryPageViewModel.cs` | same; child `new` at `:138` passes `_loggerFactory?.CreateLogger<InventoryProfileViewModel>()` | — |

**5 instrumented catch sites.** Each `LogOperationFailed(nameof(<Method>))` appended as the **last**
statement of the existing `#pragma warning disable CA1031` broad catch — **after** the unchanged
`ErrorMessage = exception.Message;` / `State = DashboardState.Error;` (LoadAsync), and after the
unchanged `SaveErrorMessage = Strings.Services_SaveError; HasSaveError = true;` + edit-buffer revert
(`SaveChangesAsync` / `DeactivateAsync` — both `catch (Exception)` with no exception variable).

### A.2 Tests (6 modified + 1 new)

| # | File | Change |
|---|---|---|
| 7 | `tests/…/Specialists/RecordingLoggerFactory.cs` | **NEW** — `public sealed class RecordingLoggerFactory : ILoggerFactory` (~50 lines). Records every log call routed through any logger it hands out, tagged with the category name. Used only by the 3 parent pass-through tests. Sits next to `RecordingLogger.cs`, namespace `…Tests.Specialists`, reused via `using`. |
| 8 | `tests/…/Customers/CustomerProfileViewModelTests.cs` | +2 (`LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak`, `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows`) |
| 9 | `tests/…/Services/ServiceProfileViewModelTests.cs` | +4 (`LoadAsync` failure-logs, `SaveChangesCommand` failure-logs **+ still reverts buffers**, `DeactivateCommand` failure-logs, no-logger safety) |
| 10 | `tests/…/Inventory/InventoryProfileViewModelTests.cs` | +2 (failure-logs no-leak, no-logger safety) |
| 11 | `tests/…/Customers/CustomerPageViewModelTests.cs` | +1 (`LoggerFactory_ForwardedToProfileChild_ChildLoadFailureIsLoggedViaTheFactory`) |
| 12 | `tests/…/Services/ServicePageViewModelTests.cs` | +1 (same) |
| 13 | `tests/…/Inventory/InventoryPageViewModelTests.cs` | +1 (same) |

**+11 tests.** All reuse `RecordingLogger<T>` / the new `RecordingLoggerFactory`. **No existing test
body modified** (0 lines removed). **No shared production stub modified** — the 3 profile query stubs
(`StubCustomerProfileQueryService`, `StubServiceProfileQueryService`, `StubProductProfileQueryService`)
are already delegate-driven and accept a throwing task as-is; `StubServiceCommandService` already
carried a `UpdateServiceException` hook.

### A.3 NOT touched

`BookingWizardViewModel`, `BookingPageViewModel`, DI registration
(`Presentation/DependencyInjection/ServiceCollectionExtensions.cs`), any interface, any DTO, any
Domain/Infrastructure/Shell/Application file, any API client, RBAC, navigation, authentication,
`RecordingLogger.cs`, `EmployeeProfileViewModel`, `InvoiceProfileViewModel`, `SpecialistProfileViewModel`.

### A.4 Authorization discrepancy note

The authorization's BOUNDARIES section lists `InventoryProfile: LoadAsync, UpdateAsync`.
`InventoryProfileViewModel` has **no `UpdateAsync` method and no second catch** — its only broad catch
is `LoadAsync` (`RecordTransactionAsync` / `MapServiceAsync` / `UnmapServiceAsync` have no `try`/`catch`;
adding one would be a missing-guard change, explicitly excluded by "Do not change … Inventory
updates"). **Only the one real catch (`LoadAsync`) was instrumented.**

---

## B. `ILoggerFactory` Plumbing

The plumbing constraint identified in the Phase 8.42 audit (§C.2): all three parent page ViewModels
(`CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`) already hold **one
`ILogger<TSelf> _logger` field + an instance-form `[LoggerMessage]`** (Wave 2A, `75357e1`). Adding a
second `ILogger<TChild>` field would fail the `[LoggerMessage]` source generator with **`SYSLIB1020`
"multiple ILogger fields"**.

**Resolution — `ILoggerFactory` (not `ILogger`) pass-through:**

- Parent ctor gains **`ILoggerFactory? loggerFactory = null`** — one optional param, appended **after**
  the existing optional `ILogger<TSelf>? logger = null`, so it is last.
- Stored as `private readonly ILoggerFactory? _loggerFactory;`.
- At the child `new` site (inside the `SelectedX` property setter):
  `_loggerFactory?.CreateLogger<TChild>()` — passed as the child's last ctor arg. When
  `_loggerFactory` is null (no DI, or an old caller), `null` flows to the child, which falls back to
  `NullLogger<TChild>.Instance`.
- `ILoggerFactory` is **not** `ILogger`, so it does **not** count toward `SYSLIB1020`. **Zero change**
  to any parent's existing `_logger` field or `[LoggerMessage]`.
- `ILoggerFactory` is registered by `AddLogging()` (Infrastructure DI). All new params optional →
  **no DI registration change, no call-site breakage** (verified: full suite green, every pre-existing
  parent test compiles and passes unchanged).
- `CreateLogger<T>` — `LoggerFactoryExtensions.CreateLogger<T>`, namespace `Microsoft.Extensions.Logging`
  (already `using`-ed in all 3 parents), in the already-referenced
  `Microsoft.Extensions.Logging.Abstractions` assembly. No new package reference.

**Build confirms no `SYSLIB1020`** — `dotnet build` = 0 warnings / 0 errors.

---

## C. Logging Implementation

Each child profile VM: standard self-logging shape unchanged from Waves 1 / 2A / 2B / 2C-1 / 2C-2 —
`sealed partial`, `ILogger<TSelf> _logger` (exactly one field → **instance-form** `[LoggerMessage]`,
no `SYSLIB1020` in the child), `?? NullLogger<TSelf>.Instance`, optional ctor param appended last.

The 5 log calls:

```
CustomerProfileViewModel.LoadAsync       → LogOperationFailed(nameof(LoadAsync))
ServiceProfileViewModel.LoadAsync        → LogOperationFailed(nameof(LoadAsync))
ServiceProfileViewModel.SaveChangesAsync → LogOperationFailed(nameof(SaveChangesAsync))
ServiceProfileViewModel.DeactivateAsync  → LogOperationFailed(nameof(DeactivateAsync))
InventoryProfileViewModel.LoadAsync      → LogOperationFailed(nameof(LoadAsync))
```

`[LoggerMessage]` signature is `(string operation)` in all three classes — **no `Exception`
parameter**. Level `Error` (clears the `LocalFileLoggerProvider` `Warning` floor). `EventId = 1` per class.

---

## D. Security Review

The only log lines this change can produce:

```
<ts> [Error] …CustomerProfileViewModel:  Customer profile operation failed. Operation=LoadAsync
<ts> [Error] …ServiceProfileViewModel:   Service profile operation failed. Operation=SaveChangesAsync
<ts> [Error] …InventoryProfileViewModel: Inventory profile operation failed. Operation=LoadAsync
```

| Aspect | Confirmed |
|---|---|
| `Exception` object | **never passed** — signature `(string operation)`, no `Exception` param |
| `Exception.Message` | **never logged** — call sites pass `nameof(...)` only |
| Backend response body | never logged (only ever in `Exception.Message`) |
| **Customer PII** — name / phone / email / company / lifetime value / notes / activity text | never referenced by a log call |
| **Service data** — name / description / **price** (`EditablePrice` / `PriceValue`) / duration | never referenced |
| **Inventory data** — product name / **SKU** / category / **supplier** / **cost** / stock levels / transaction notes | never referenced |
| Identifiers — `_customerId` / `_serviceId` / `_productId` / org / branch | never logged |
| Tokens (bearer / session) | not held by these VMs |
| Level / EventId | `Error` / `1` |
| Behaviour | `#pragma` unchanged; `ErrorMessage` / `State` / `SaveErrorMessage` / `HasSaveError` / edit-buffer revert all unchanged; log strictly appended last |

**Test-enforced:** each failure test seeds a recognisable secret into the exception message and asserts
`Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", …)`. Secrets:
`"Amelia Hart / amelia.hart@example.com / 555-0100"` (Customer),
`"Haircut & Style / $65 / Classic cut and blow-dry finish."` (Service),
`"SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"` (Inventory). The 3 parent pass-through tests seed
`"child boom"` and assert it is absent.

---

## E. Tests

### E.1 Behaviour preservation — verified by test

- `ServiceProfileViewModel.SaveChangesAsync` failure test asserts `HasSaveError == true` **and**
  `EditableName` reverted to `"Haircut & Style"` — the edit-buffer revert still happens; the log call
  is purely additive.
- All `LoadAsync` failure tests assert `State == DashboardState.Error` and (no-logger variant)
  `ErrorMessage == "boom"` — unchanged error surfacing.
- 3 parent pass-through tests assert `sut.Profile is not null` (child still constructed) and the child's
  `LoadAsync` failure reached the factory with `Operation=LoadAsync`, category containing the child type
  name, no secret.

### E.2 Fresh full run (working tree, uncommitted)

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **644** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,587** | **0** | **0** |

Delta from baseline `c01d0ce`: **+11** (Presentation.Tests 633 → 644).

---

## F. Validation

```
dotnet build          → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test --no-build → 2,587 / 2,587 passing
Architecture tests     → 7 / 7 passing
```

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |
| ~+10 tests | +11 | ✅ |

---

## G. Commit Readiness

| Gate | Status |
|---|---|
| Scope = 3 profile VMs + 3 parent plumbing + corresponding tests + 1 new test helper | ✅ |
| `BookingWizardViewModel` / `BookingPageViewModel` untouched | ✅ (not in `git status`) |
| No DI / `ServiceCollectionExtensions` / Domain / backend-contract / RBAC / auth / navigation / interface change | ✅ |
| No multiple `ILogger<TChild>` fields — `ILoggerFactory` used | ✅ (no `SYSLIB1020`) |
| Every log call `nameof`-only; `Exception` never passed; no PII / price / SKU / cost / supplier | ✅ |
| Behaviour append-only after existing error handling (incl. Service save-buffer revert) | ✅ |
| No shared production stub modified; no existing test body changed | ✅ |
| Build 0/0 · Tests 2,587/2,587 · Architecture 7/7 | ✅ |

Working tree: **13 files** — `git status --porcelain`:
```
 M src/…/ViewModels/Customers/CustomerPageViewModel.cs
 M src/…/ViewModels/Customers/CustomerProfileViewModel.cs
 M src/…/ViewModels/Inventory/InventoryPageViewModel.cs
 M src/…/ViewModels/Inventory/InventoryProfileViewModel.cs
 M src/…/ViewModels/Services/ServicePageViewModel.cs
 M src/…/ViewModels/Services/ServiceProfileViewModel.cs
 M tests/…/Customers/CustomerPageViewModelTests.cs
 M tests/…/Customers/CustomerProfileViewModelTests.cs
 M tests/…/Inventory/InventoryPageViewModelTests.cs
 M tests/…/Inventory/InventoryProfileViewModelTests.cs
 M tests/…/Services/ServicePageViewModelTests.cs
 M tests/…/Services/ServiceProfileViewModelTests.cs
?? tests/…/Specialists/RecordingLoggerFactory.cs
```

Recommended commit subject (per audit §F): `fix(desktop): add ViewModel diagnostic logging (profile panels)`

---

## STOP

Implementation complete. Build 0/0, 2,587/2,587 tests, architecture 7/7. Working tree modified across
exactly 13 files (6 production + 6 test + 1 new test helper). **Nothing committed, pushed, merged,
rebased, or amended.** HEAD remains `c01d0ce`. Awaiting Phase 8.44 commit scope review.
