# ROJAN AI — TEAM 3 — PHASE 8.61 — LEGACY `[LoggerMessage]` HARMONIZATION — IMPLEMENTATION REPORT v1

**Type:** Implementation only. **No commit. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion` — HEAD still `6a1bced` (working tree modified, uncommitted).
**Reference:** `ROJAN_PHASE8_60_LEGACY_LOGGERMESSAGE_SCOPE_REVIEW_v1.md`
**Scope:** the 7 pre-8.15 ViewModels whose `[LoggerMessage]` still passes an `Exception` (and, in 2, a `SpecialistId`).

---

## A. FILES CHANGED (14 — all modified, 0 new)

`git diff --stat`: **14 files changed, 84 insertions(+), 57 deletions(-)**

### A.1 Production (7)

| # | File | `[LoggerMessage]` change | Call-site edits |
|---|---|---|---|
| 1 | `Accounting/AccountingPageViewModel.cs` | `LogOperationFailed(ILogger logger, string operation, Exception exception)` → `(ILogger logger, string operation)` — **stays static** (2 `ILogger` fields); comment updated | 2 — `LoadAsync` (`:157`), `SearchAsync` (`:190`) — drop `, exception` |
| 2 | `Accounting/PosCheckoutViewModel.cs` | `LogOperationFailed(string operation, Exception exception)` → `(string operation)` — instance; comment added | 3 — `LoadOptionsAsync`, `ProceedToPaymentAsync`, `ChargeAsync` — drop `, exception` |
| 3 | `Bookings/BookingPageViewModel.cs` | `LogOperationFailed(string operation, Exception exception)` → `(string operation)` — instance; comment added | 5 — `LoadAsync`, `CreateBookingAsync`, `ChangeStatusAsync`, `CancelSelectedBookingAsync`, `RescheduleSelectedBookingAsync` — drop `, exception` |
| 4 | `Calendar/CalendarPageViewModel.cs` | `LogLoadFailed(string operation, Exception exception)` → `(string operation)` — instance; comment added | 3 — `InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` — drop `, exception` |
| 5 | `Dashboard/DashboardPageViewModel.cs` | `LogLoadFailed(Exception exception)` → `(string operation)`; **message `"Dashboard overview load failed."` → `"Dashboard overview load failed. Operation={Operation}"`** | 1 — `LogLoadFailed(exception)` → `LogLoadFailed(nameof(LoadAsync))` |
| 6 | `Specialists/SpecialistAvailabilityViewModel.cs` | `LogLoadFailed(string specialistId, Exception exception)` → `(string operation)`; **message `"… SpecialistId={SpecialistId}"` → `"… Operation={Operation}"`** | 1 — `LogLoadFailed(_specialistId, exception)` → `LogLoadFailed(nameof(LoadAsync))` |
| 7 | `Specialists/SpecialistScheduleViewModel.cs` | `LogPermissionDenied(string specialistId, string operation)` → `(string operation)`; `LogOperationFailed(string specialistId, string operation, Exception exception)` → `(string operation)`; **both messages drop `SpecialistId={SpecialistId} `**; `EventId 1`/`2` + `Warning`/`Error` levels kept | 4 — 2 in `LoadAsync` (`:278` denied, `:286` failed), 2 in `TryMutateAsync` (`:467` denied, `:475` failed) — drop `_specialistId,` (and `, exception` on the 2 Error calls) |

**Total: 8 `[LoggerMessage]` methods, 19 call sites.** Every catch body keeps its unchanged `ErrorMessage = exception.Message;` / `State = …;` / `IsPermissionDenied = …;` — the `exception` local stays referenced (used for the on-screen message), so **no unused-variable / `CS0168` warning**. `_specialistId` remains a live field in both Specialist VMs (used by every query/command call).

### A.2 Tests (7 modified, 0 new methods)

| # | File | Change |
|---|---|---|
| 8 | `Accounting/AccountingPageViewModelTests.cs` | `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` → `…_NoExceptionLeak`: seeds a backend-body secret, asserts `Operation=LoadAsync` present + secret **absent** from the log (and still surfaced in `ErrorMessage`) |
| 9 | `Accounting/PosCheckoutViewModelTests.cs` | `LoadCommand_QueryThrows_LogsTheFailure` → `…_OperationNameOnly_NoLeak`: same shape (`Operation=LoadOptionsAsync`) |
| 10 | `Bookings/BookingPageViewModelTests.cs` | `CreateBookingCommand_BackendThrows_…`: seeds backend-body secret, asserts `Operation=CreateBookingAsync` present + secret absent |
| 11 | `Calendar/CalendarPageViewModelTests.cs` | `InitializeAsync_SpecialistsQueryThrows_…_NoExceptionLeak`: `Operation=InitializeAsync` present + secret absent |
| 12 | `Dashboard/DashboardPageViewModelTests.cs` | `Constructor_QueryServiceThrows_LogsError_…_NoExceptionLeak`: `Operation=LoadAsync` present + secret absent (also verifies the message now carries the operation token it never had) |
| 13 | `Specialists/SpecialistAvailabilityViewModelTests.cs` | `LoadCommand_QueryThrows_LogsTheFailure` → `…_OperationNameOnly_NoLeak`: **was `Message.Contains("specialist-1")`** (would break) → now asserts `Operation=LoadAsync` present + `"specialist-1"` and backend-body **absent** |
| 14 | `Specialists/SpecialistScheduleViewModelTests.cs` | `LoadCommand_QueryThrows_LogsTheFailure` → `…_OperationNameOnly_NoLeak`: **was `Message.Contains("specialist-1")`** (would break) → same fix (`Operation=LoadAsync`; id + body absent) |

**Net test-count change: 0** — the 2 predicted breaking assertions were fixed in place and 5 existing failure tests were strengthened with an explicit no-leak assertion, rather than adding new test methods. **No new test helper. No shared-stub change.** `RecordingLogger<T>` reused throughout.

### A.3 NOT touched

`App.xaml.cs LogUnhandledException` (crash handler — must keep the exception), `Infrastructure/Api/HttpApiClient.cs LogApiRequestFailed` (Infra decision), the 24 already-compliant Wave-2 VMs, DI registration, Domain, Infrastructure, Shell, Application, backend contracts / DTOs / interfaces, RBAC, authentication, navigation. No behaviour change in any catch: `ErrorMessage` / `State` / `IsPermissionDenied` / `InputErrorMessage` and every user-facing string are byte-identical.

---

## B. `[LoggerMessage]` CONVERSION DETAILS

### B.1 Target shape

Every ViewModel-track `[LoggerMessage]` is now the unified operation-name-only form:

```csharp
[LoggerMessage(EventId = N, Level = LogLevel.<Error|Warning>, Message = "<domain> operation failed. Operation={Operation}")]
private [static] partial void LogOperationFailed([ILogger logger, ] string operation);
```

called as `LogOperationFailed([_logger, ] nameof(<Method>));`.

### B.2 Per-VM

| VM | Form kept | `EventId` | `Level` | Message template (after) |
|---|---|---|---|---|
| `AccountingPageViewModel` | **static** (2 `ILogger` fields — instance form would be `SYSLIB1020`) | 1 | Error | `"Accounting operation failed. Operation={Operation}"` |
| `PosCheckoutViewModel` | instance | 1 | Error | `"POS checkout operation failed. Operation={Operation}"` |
| `BookingPageViewModel` | instance | 1 | Error | `"Booking operation failed. Operation={Operation}"` |
| `CalendarPageViewModel` | instance | 1 | Error | `"Calendar availability load failed. Operation={Operation}"` |
| `DashboardPageViewModel` | instance | 1 | Error | `"Dashboard overview load failed. Operation={Operation}"` **(token added)** |
| `SpecialistAvailabilityViewModel` | instance | 1 | Error | `"Specialist availability load failed. Operation={Operation}"` **(was `SpecialistId={SpecialistId}`)** |
| `SpecialistScheduleViewModel` | instance ×2 | 1 / 2 | Warning / Error | `"Specialist schedule permission denied. Operation={Operation}"` / `"Specialist schedule operation failed. Operation={Operation}"` **(both dropped `SpecialistId={SpecialistId} `)** |

- **No form changes.** Static stays static, instance stays instance.
- **No `ILoggerFactory` introduced.** No new `ILogger` field. No ctor-signature change.
- **`SYSLIB1020`:** not reachable — removing a parameter from a `[LoggerMessage]` partial cannot add an `ILogger` field. `dotnet build -c Debug` → 0 warnings / 0 errors.

---

## C. SECURITY REVIEW

**After this change, no `[LoggerMessage]` in any ViewModel passes an `Exception` or a record identifier.** The only reachable ViewModel-track log lines are:

```
[Error]   …AccountingPageViewModel:          Accounting operation failed. Operation={LoadAsync|SearchAsync}
[Error]   …PosCheckoutViewModel:             POS checkout operation failed. Operation={LoadOptionsAsync|ProceedToPaymentAsync|ChargeAsync}
[Error]   …BookingPageViewModel:             Booking operation failed. Operation={LoadAsync|CreateBookingAsync|ChangeStatusAsync|CancelSelectedBookingAsync|RescheduleSelectedBookingAsync}
[Error]   …CalendarPageViewModel:            Calendar availability load failed. Operation={InitializeAsync|LoadDailyAvailabilityAsync|LoadWeeklyAvailabilityAsync}
[Error]   …DashboardPageViewModel:           Dashboard overview load failed. Operation=LoadAsync
[Error]   …SpecialistAvailabilityViewModel:  Specialist availability load failed. Operation=LoadAsync
[Warning] …SpecialistScheduleViewModel:     Specialist schedule permission denied. Operation={Method}
[Error]   …SpecialistScheduleViewModel:     Specialist schedule operation failed. Operation={Method}
```

| Channel | Before | After |
|---|---|---|
| `Exception` object → `exception.ToString()` written to the local log file (embeds `ApiException` **backend response bodies**) | reachable for all 7 | **removed** — no `[LoggerMessage]` takes an `Exception` |
| `Exception.Message` (interpolated) | never (safe templates) | never |
| Record identifiers (`SpecialistId`) | logged by 2 VMs (3 methods) | **removed** |
| PII / financial values / tokens | never | never |

**Test-enforced:** each of the 7 legacy VMs now has ≥1 failure test that seeds a recognizable backend-body / `specialist-1` string into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)`, while still asserting the secret **is** surfaced in the user-facing `ErrorMessage` (behaviour unchanged).

**Special handling — `SpecialistId`:** confirmed removed from all 3 `[LoggerMessage]` methods in `SpecialistAvailabilityViewModel` / `SpecialistScheduleViewModel` (message tokens + method parameters + call arguments). `_specialistId` remains a private field (used by the query/command layer), never logged.

---

## D. TEST CHANGES

### D.1 Broken assertions — fixed

| Test | Was | Now |
|---|---|---|
| `SpecialistAvailabilityViewModelTests.LoadCommand_QueryThrows_LogsTheFailure` | `entry.Message.Contains("specialist-1")` | `Contains("Operation=LoadAsync")` + `DoesNotContain("specialist-1")` + `DoesNotContain("backend body")` |
| `SpecialistScheduleViewModelTests.LoadCommand_QueryThrows_LogsTheFailure` | `entry.Message.Contains("specialist-1")` | same fix |

### D.2 Strengthened (existing failure tests + explicit no-leak assertion)

`AccountingPageViewModelTests` (`LoadAsync`), `PosCheckoutViewModelTests` (`LoadCommand`), `BookingPageViewModelTests` (`CreateBookingCommand`), `CalendarPageViewModelTests` (`InitializeAsync`), `DashboardPageViewModelTests` (`Constructor`) — each now seeds a backend-body secret and asserts it is **absent** from the log line while present in `ErrorMessage`.

### D.3 Unaffected (verified still green)

`AccountingPageViewModelTests` :50 `Operation=LoadAsync` (BookingWizard-forwarding test, `AccountingPageViewModelTests` `LoggerFactory_ForwarderToInvoice…`), `PosCheckoutViewModelTests` :250/:264 (`Level == Error` only), `BookingPageViewModelTests` :608/:624/:643, `CalendarPageViewModelTests` `LoadDaily…`/`LoadWeekly…` (template keeps `Operation={Operation}`), `SpecialistScheduleViewModelTests` :273/:274 (Warning present / Error absent), :293 (Warning message contains the command-derived operation name — template unchanged for the `{Operation}` token).

### D.4 Estimate vs actual

| | Scope review estimate | Actual |
|---|---|---|
| Production files | 7 | 7 |
| Test files | ~7 | 7 |
| Breaking-assertion fixes | 2 | 2 |
| Net test-count delta | ~0 to +7 | **0** (folded into existing tests, no new methods) |
| New helper / shared-stub change | none | none |

---

## E. VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020, no CA1848, no CS0168)
dotnet test  -c Debug --no-build  → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | 666 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,609** | **0** | **0** |

| Expected (authorization) | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests ~2,616 PASS | **2,609 / 2,609** | ✅ (net 0 new tests — strengthened existing ones instead of adding methods; the scope review's "~0 to +7" upper bound was not needed) |
| Architecture 7 / 7 | 7 / 7 | ✅ |

Delta from baseline `6a1bced` (2,609): **0** — assertion edits only; user-visible behaviour byte-identical.

---

## F. COMMIT READINESS

| Gate | Status |
|---|---|
| Scope = the 7 legacy VMs + their 7 test files only | ✅ |
| No DI / Domain / backend contract / auth / RBAC / navigation / new-VM / shared-infra change | ✅ (not in `git status`) |
| No `[LoggerMessage]` in any ViewModel passes an `Exception` | ✅ (repo scan — only `App.LogUnhandledException` + `HttpApiClient` remain, both intentional) |
| No `[LoggerMessage]` in any ViewModel logs `SpecialistId` / any record identifier | ✅ |
| No form change (static stays static, instance stays instance); no `ILoggerFactory` added; no ctor-signature change; no `SYSLIB1020` | ✅ (build 0/0) |
| `EventId` / `Level` / catch flow / `ErrorMessage` / `State` / user-facing strings unchanged | ✅ |
| `exception` local still referenced (no `CS0168`); `_specialistId` still a used field | ✅ |
| 2 breaking test assertions fixed; 5 no-leak assertions added; no new test method / helper / stub | ✅ |
| Build 0/0 · Tests 2,609/2,609 · Architecture 7/7 | ✅ |

Working tree: **14 files** — `git status --porcelain`:
```
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/PosCheckoutViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistAvailabilityViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs
```

Recommended commit subject (per Phase 8.60 §G.3): `refactor(desktop): drop exception payload from diagnostic logging`

---

## STOP

Implementation complete. Build 0/0, 2,609/2,609 tests, architecture 7/7. Working tree modified across
exactly 14 files (7 production + 7 test). No behaviour change. **Nothing committed, pushed, merged,
rebased, or amended.** HEAD remains `6a1bced`. Awaiting Phase 8.62 commit scope review.
