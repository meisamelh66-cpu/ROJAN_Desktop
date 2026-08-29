# ROJAN AI — TEAM 3 — PHASE 8.62 — LEGACY `[LoggerMessage]` HARMONIZATION — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No source change. No test change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `6a1bced659ae129da48d2453c5636868c1455701` — `fix(desktop): add ViewModel diagnostic logging (specialist page)` (Phase 8.56, committed 8.58)
**Scope under review:** Phase 8.61 (legacy `[LoggerMessage]` harmonization) working-tree changes, pending commit.
**Verdict:** ✅ **READY TO COMMIT.** No blocking findings. No behaviour change.

---

## A. GIT STATE

| Check | Expected | Actual | Status |
|---|---|---|---|
| HEAD | `6a1bced` | `6a1bced659ae129da48d2453c5636868c1455701` | ✅ |
| Branch | `feature/team3-desktop-completion` | same | ✅ |
| Staged files | none | none (`git diff --cached` empty) | ✅ |
| Tracked code changes | 14 modified | 14 modified, 0 new, 0 deleted | ✅ |
| Pushed / merged / rebased / amended | none | none | ✅ |
| Unrelated modifications | none | none | ✅ |

`git diff --stat`: **14 files changed, 84 insertions(+), 57 deletions(-)**. All remaining `??` entries are `ROJAN_*.md` reports.

### A.1 Tracked changes (code)

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

---

## B. SCOPE VERIFICATION

### B.1 Production — exactly the 7 authorized files

| File | Change (from full diff) | Verdict |
|---|---|---|
| `AccountingPageViewModel.cs` | static `LogOperationFailed(ILogger, string, Exception)` → `(ILogger, string)`; 2 calls drop `, exception`; comment updated | ✅ in scope |
| `PosCheckoutViewModel.cs` | instance `LogOperationFailed(string, Exception)` → `(string)`; 3 calls drop `, exception`; comment added | ✅ |
| `BookingPageViewModel.cs` | instance `LogOperationFailed(string, Exception)` → `(string)`; 5 calls drop `, exception`; comment added | ✅ |
| `CalendarPageViewModel.cs` | instance `LogLoadFailed(string, Exception)` → `(string)`; 3 calls drop `, exception`; comment added | ✅ |
| `DashboardPageViewModel.cs` | instance `LogLoadFailed(Exception)` → `(string operation)`; message `"Dashboard overview load failed."` → `"… Operation={Operation}"`; 1 call `LogLoadFailed(exception)` → `LogLoadFailed(nameof(LoadAsync))` | ✅ |
| `SpecialistAvailabilityViewModel.cs` | instance `LogLoadFailed(string specialistId, Exception)` → `(string operation)`; message `SpecialistId={SpecialistId}` → `Operation={Operation}`; 1 call drops `_specialistId` + `exception` | ✅ |
| `SpecialistScheduleViewModel.cs` | 2 methods: `LogPermissionDenied(string specialistId, string operation)` → `(string operation)`, `LogOperationFailed(string specialistId, string operation, Exception)` → `(string operation)`; both messages drop `SpecialistId={SpecialistId} `; 4 calls drop `_specialistId` (and `, exception` on the 2 Error calls) | ✅ |

**8 `[LoggerMessage]` methods, 19 call sites.** Nothing outside the 7-file list.

### B.2 Tests — only the 7 corresponding files

| File | Change | Existing bodies |
|---|---|---|
| `AccountingPageViewModelTests.cs` | `LoadAsync_QueryServiceThrows_LogsErrorWithOperation` renamed `…_NoExceptionLeak`; seeds a backend-body secret, asserts `Operation=LoadAsync` present + secret absent + still in `ErrorMessage` | in-place edit |
| `PosCheckoutViewModelTests.cs` | `LoadCommand_QueryThrows_LogsTheFailure` renamed `…_OperationNameOnly_NoLeak`; `Operation=LoadOptionsAsync` present + secret absent | in-place |
| `BookingPageViewModelTests.cs` | `CreateBookingCommand_BackendThrows_…`: secret seeded, `Operation=CreateBookingAsync` present + absent | in-place |
| `CalendarPageViewModelTests.cs` | `InitializeAsync_SpecialistsQueryThrows_…_NoExceptionLeak`: `Operation=InitializeAsync` present + absent | in-place |
| `DashboardPageViewModelTests.cs` | `Constructor_QueryServiceThrows_LogsError_…_NoExceptionLeak`: `Operation=LoadAsync` present (new token) + secret absent | in-place |
| `SpecialistAvailabilityViewModelTests.cs` | **breaking assertion fixed** — `Message.Contains("specialist-1")` → `Operation=LoadAsync` present + `"specialist-1"` and body absent | in-place |
| `SpecialistScheduleViewModelTests.cs` | **breaking assertion fixed** — same shape | in-place |

**0 new test methods. 0 deleted test methods. No new helper. No shared-stub change.** Every test's user-facing behaviour assertion (`ErrorMessage` still equals the exception text) is preserved.

### B.3 Confirmed UNTOUCHED

| Area | Evidence |
|---|---|
| `Shell/App.xaml.cs` `LogUnhandledException` (keeps `Exception` — crash handler) | not in `git status` |
| `Infrastructure/Api/HttpApiClient.cs` `LogApiRequestFailed` (keeps `Exception` — Infra decision) | not in `git status` |
| DI — `Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs` | not in `git status` |
| Domain / Infrastructure / Shell / Application projects | not in `git status` |
| Backend contracts / DTOs / interfaces | not in `git status` |
| RBAC / permission gates | not in `git status` |
| Authentication | not in `git status` |
| Navigation / back-stack | not in `git status` |
| The 24 already-compliant Wave-2 VMs, `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, shared stubs | not in `git status` |

---

## C. `[LoggerMessage]` CONVERSION

| Check | Result |
|---|---|
| All legacy `(Exception)` / `(identifier, Exception)` forms → `(string operation)` (or `(ILogger, string operation)` static) | ✅ 8/8 methods |
| **No `Exception` parameter remains** in any ViewModel `[LoggerMessage]` | ✅ repo scan: `grep -rn "\[LoggerMessage" src/…/ViewModels -A1 \| grep Exception` → only a **comment** in `SpecialistPageViewModel.cs` ("No Exception parameter:"), no signature |
| No `Exception.Message` logged | ✅ every message template is a constant `"… Operation={Operation}"`; call sites pass `nameof(<Method>)` only. The pre-existing `ErrorMessage = exception.Message` (UI) is unchanged, never routed to the logger |
| No payload interpolation remains | ✅ the only `{…}` token in any template is now `{Operation}`; `{SpecialistId}` removed from all 3 methods that had it |
| `EventId` / `Level` preserved | ✅ Accounting/PosCheckout/Booking/Calendar/Dashboard/SpecAvail: `EventId=1, Error`; SpecSchedule: `EventId=1 Warning` (`LogPermissionDenied`) + `EventId=2 Error` (`LogOperationFailed`) |
| `DashboardPageViewModel` message gained `{Operation}` (the one genuine template change) | ✅ `"Dashboard overview load failed."` → `"Dashboard overview load failed. Operation={Operation}"`; call `LogLoadFailed(nameof(LoadAsync))` |

---

## D. SECURITY REVIEW

**After this commit, no `[LoggerMessage]` in any ViewModel passes an `Exception` or a record identifier.**

| Must NOT reach the log (after) | Result |
|---|---|
| Backend response bodies (`ApiException.Message` via `exception.ToString()`) | ✅ **removed** — no `[LoggerMessage]` takes an `Exception` param; `exception.ToString()` is never written for these 7 VMs |
| API exception payloads | ✅ removed (same mechanism) |
| **`SpecialistId`** payload | ✅ removed from `SpecialistAvailabilityViewModel` (1 method) and `SpecialistScheduleViewModel` (2 methods) — message tokens, method params, and call arguments all gone; `_specialistId` remains a private field used only by the query/command layer |
| User identifiers | ✅ none logged by any of the 7 |
| Financial values (amounts / prices / payments / salary) | ✅ never in a template; only ever reachable via the now-removed `Exception` channel |
| Tokens | ✅ not held by these 7 VMs |
| **Allowed** — `Operation=nameof(Method)` | ✅ the only variable content in every reachable log line |

**Test-enforced:** each of the 7 legacy VMs now has ≥1 failure test that seeds a recognizable backend-body / `specialist-1` string into the thrown exception and asserts `Assert.DoesNotContain(secret, entry.Message)` + `Assert.Contains("Operation=<method>", entry.Message)`, while still asserting the secret **is** surfaced in the user-facing `ErrorMessage`.

**Residual (intentional, disclosed):** `App.LogUnhandledException` (global crash handler) and `Infrastructure/Api/HttpApiClient.LogApiRequestFailed` still take an `Exception` — both are correct for their purpose and are outside the ViewModel track.

---

## E. ARCHITECTURE REVIEW

| Check | Result |
|---|---|
| Existing **static** form stays static | ✅ `AccountingPageViewModel.LogOperationFailed(ILogger logger, string operation)` — static preserved (required by its 2 `ILogger` fields) |
| Existing **instance** forms stay instance | ✅ the other 6 VMs / 7 methods — instance preserved |
| No `ILogger` field added | ✅ zero field changes in any of the 7 files |
| No constructor changes | ✅ zero ctor-signature changes; `_loggerFactory` (Accounting/Booking) untouched |
| No DI changes | ✅ no registration file in the diff |
| No `SYSLIB1020` | ✅ removing a parameter from a `[LoggerMessage]` partial cannot add an `ILogger` field; `dotnet build -c Debug` → **0 warnings / 0 errors** |
| No `CS0168` (unused `exception`) | ✅ every `catch (Exception exception)` still uses `exception.Message` for `ErrorMessage` before the log call — verified in the diff for all 7 |
| No `CA1848` | ✅ all logging still via source-generated `[LoggerMessage]` |
| `_specialistId` still a used field | ✅ 6 uses in `SpecialistAvailabilityViewModel`, 14 in `SpecialistScheduleViewModel` (query/command calls) |

---

## F. TEST REVIEW

| Check | Result |
|---|---|
| `SpecialistId` assertions updated | ✅ both `Message.Contains("specialist-1")` assertions (`SpecialistAvailabilityViewModelTests`, `SpecialistScheduleViewModelTests`) replaced with `Contains("Operation=LoadAsync")` + `DoesNotContain("specialist-1")` + `DoesNotContain("backend body")` |
| No-leak assertions valid | ✅ 7 legacy VMs each have a seeded-secret `DoesNotContain` on the log line + `Contains("Operation=<method>")` |
| Existing behaviour unchanged | ✅ every test still asserts `State == DashboardState.Error` (or `HasSaveError` / `IsPermissionDenied` / form-preservation) and that the exception text is surfaced in `ErrorMessage`; the Warning/Error separation in `SpecialistScheduleViewModelTests` and the command-name-derived `Operation` check (`:293`) still pass (template keeps `{Operation}`) |

### F.1 Fresh validation run (this phase, working tree)

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | 666 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,609** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,609 / 2,609 | 2,609 / 2,609 | ✅ |
| Architecture 7 / 7 | 7 / 7 | ✅ |

Delta vs `6a1bced` (2,609): **0** — assertion edits only; no behaviour change.

---

## G. COMMIT READINESS

| Gate | Status |
|---|---|
| HEAD `6a1bced`; nothing staged / pushed / merged / rebased / amended | ✅ |
| Exactly 14 code files, all Phase 8.61 authorized scope | ✅ |
| No `App.LogUnhandledException` / `HttpApiClient` / DI / Domain / backend contract / auth / RBAC / navigation / shared-infra change | ✅ |
| No `[LoggerMessage]` in any ViewModel passes an `Exception` (repo scan clean) | ✅ |
| No `[LoggerMessage]` in any ViewModel logs `SpecialistId` / any record identifier | ✅ |
| Static form stays static; instance forms stay instance; no `ILogger` field / ctor / DI change; no `SYSLIB1020` | ✅ (build 0/0) |
| `EventId` / `Level` / catch flow / `ErrorMessage` / `State` / user-facing strings unchanged | ✅ |
| 2 breaking test assertions fixed; 5 no-leak assertions added; 0 new test methods / helpers / stubs | ✅ |
| Build 0/0 · Tests 2,609/2,609 · Architecture 7/7 | ✅ |

### G.1 Recommendation

**READY.** Proceed to **Phase 8.63 — Commit Execution** on authorization. No remediation required.

Planned commit:
- Subject (exact, per the Phase 8.62 authorization): `fix(desktop): drop exception payload from diagnostic logging`
  *(the Phase 8.60 §G.3 draft used `refactor(desktop):`; the Phase 8.62 authorization specifies `fix(desktop):` — using the authorization's wording).*
- Staging: `git reset` → 14 explicit `git add <path>` (never `git add .` / `-A`).
- Trailers: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` + `Claude-Session: …`
- Commit-message gotcha: Bash tool does not interpret PowerShell `@'…'@` here-strings — use repeated `-m` or `git commit -F <file>`.
- No push / merge / rebase / amend.

After this commit, the ViewModel diagnostic-logging track is **rule-consistent** (no ViewModel `[LoggerMessage]` passes an exception or identifier) — fully closed. The remaining logging-adjacent items are the P1 missing-guard sweep and the `HttpApiClient` Infra-observability decision, both separately scoped.

---

## STOP

Commit scope review complete. 7 legacy VMs · 8 `[LoggerMessage]` methods · 19 call sites · no form/field/ctor/DI change · no `SYSLIB1020` · no behaviour change. No source or test change, no commit/push/merge/rebase/amend. HEAD remains `6a1bced`. **Awaiting Phase 8.63 commit authorization.**
