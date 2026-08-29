# ROJAN AI — TEAM 3 — PHASE 8.63 — LEGACY `[LoggerMessage]` HARMONIZATION — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `6a1bced659ae129da48d2453c5636868c1455701`
**New HEAD:** `5ba554ceb588e5780b87aebdf280538f6b25c485`
**Commit subject:** `fix(desktop): drop exception payload from diagnostic logging`

---

## A. COMMIT

```
commit 5ba554ceb588e5780b87aebdf280538f6b25c485
Author:  Meisam Elhaee <meisamelh66@gmail.com>
Date:    Fri Aug 28 09:06:51 2026 -0700

    fix(desktop): drop exception payload from diagnostic logging

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_01QKJ9GR9nPK5zfcYKD6kWZj
```

Subject is EXACT as authorized. Trailers match the Team 3 arc convention.

---

## B. STAGING (explicit-path only)

```
git reset
git add <14 explicit paths>          # never git add . / git add -A
git diff --cached --name-only        # 14
```

| # | Path | Type |
|---|---|---|
| 1 | `src/…/ViewModels/Accounting/AccountingPageViewModel.cs` | prod |
| 2 | `src/…/ViewModels/Accounting/PosCheckoutViewModel.cs` | prod |
| 3 | `src/…/ViewModels/Bookings/BookingPageViewModel.cs` | prod |
| 4 | `src/…/ViewModels/Calendar/CalendarPageViewModel.cs` | prod |
| 5 | `src/…/ViewModels/Dashboard/DashboardPageViewModel.cs` | prod |
| 6 | `src/…/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs` | prod |
| 7 | `src/…/ViewModels/Specialists/SpecialistScheduleViewModel.cs` | prod |
| 8–14 | the 7 corresponding `tests/…/*ViewModelTests.cs` | test |

`git show --stat 5ba554c`: **14 files changed, 84 insertions(+), 57 deletions(-)**. No new file. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

| Area | Status |
|---|---|
| `Shell/App.xaml.cs` `LogUnhandledException` (keeps `Exception` — crash handler) | ✅ untouched (not in commit) |
| `Infrastructure/Api/HttpApiClient.cs` `LogApiRequestFailed` (keeps `Exception` — Infra decision) | ✅ untouched |
| DI — `Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs` | ✅ untouched |
| Domain / Infrastructure / Shell / Application projects | ✅ untouched |
| Backend contracts / DTOs / interfaces | ✅ untouched |
| Authentication | ✅ untouched |
| RBAC / permission gates | ✅ untouched |
| Navigation / back-stack | ✅ untouched |
| Shared infrastructure — `RecordingLogger.cs`, `RecordingLoggerFactory.cs`, all shared stubs | ✅ untouched |
| The 24 already-compliant Wave-2 VMs (incl. `SpecialistPageViewModel` from Phase 8.56) | ✅ untouched |

---

## D. POST-COMMIT VALIDATION

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
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,609** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,609 / 2,609 PASS | 2,609 / 2,609 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test count unchanged: 2,609 (`6a1bced`) → **2,609** (`5ba554c`) — assertion edits only, **no behaviour change**.

---

## E. WHAT LANDED

The 7 pre-8.15 ViewModels whose `[LoggerMessage]` still forwarded a caught `Exception` (and, in 2, a `SpecialistId`) are now on the unified operation-name-only rule.

| VM | `[LoggerMessage]` (after) | Form | Call sites |
|---|---|---|---|
| `AccountingPageViewModel` | `LogOperationFailed(ILogger logger, string operation)` — `"Accounting operation failed. Operation={Operation}"` | **static** (kept — 2 `ILogger` fields) | 2 |
| `PosCheckoutViewModel` | `LogOperationFailed(string operation)` — `"POS checkout operation failed. Operation={Operation}"` | instance | 3 |
| `BookingPageViewModel` | `LogOperationFailed(string operation)` — `"Booking operation failed. Operation={Operation}"` | instance | 5 |
| `CalendarPageViewModel` | `LogLoadFailed(string operation)` — `"Calendar availability load failed. Operation={Operation}"` | instance | 3 |
| `DashboardPageViewModel` | `LogLoadFailed(string operation)` — `"Dashboard overview load failed. Operation={Operation}"` **(token added)** | instance | 1 |
| `SpecialistAvailabilityViewModel` | `LogLoadFailed(string operation)` — `"Specialist availability load failed. Operation={Operation}"` **(was `SpecialistId={SpecialistId}`)** | instance | 1 |
| `SpecialistScheduleViewModel` | `LogPermissionDenied(string operation)` (Warning, EventId 1) / `LogOperationFailed(string operation)` (Error, EventId 2) — both `"… Operation={Operation}"` **(both dropped `SpecialistId={SpecialistId} `)** | instance ×2 | 4 |

**8 `[LoggerMessage]` methods, 19 call sites.**

- **No form change** — static stays static, instance stays instance. **No `ILogger` field, ctor, or DI change. No `SYSLIB1020`.**
- **No behaviour change** — every `catch (Exception exception)` keeps its unchanged `ErrorMessage = exception.Message;` / `State = DashboardState.Error;` / `IsPermissionDenied = …;`; `exception` stays referenced (no `CS0168`); `_specialistId` stays a used private field (query/command calls).
- **Security:** after this commit, **no `[LoggerMessage]` in any ViewModel passes an `Exception` or a record identifier**. Backend response bodies (embedded in `ApiException.ToString()`) and `SpecialistId` no longer reach the local rotated log file for these 7 VMs. Test-enforced — each has ≥1 failure test seeding a backend-body / `specialist-1` secret and asserting `DoesNotContain(secret)` + `Contains("Operation=<method>")`, while still asserting the secret **is** surfaced in the user-facing `ErrorMessage`.
- **Tests:** 2 breaking `Message.Contains("specialist-1")` assertions fixed → operation-name; 5 existing failure tests strengthened with a seeded-secret no-leak assertion. **0 new test methods, 0 new helpers, 0 shared-stub changes.**
- **Intentionally retained:** `App.LogUnhandledException` (crash handler must capture the exception) and `Infrastructure/Api/HttpApiClient.LogApiRequestFailed` (Infra-observability decision) — both outside the ViewModel track.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 14 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `5ba554c`.

---

## G. LOGGING ARCHITECTURE — CLOSED

With `5ba554c`, the ROJAN Desktop ViewModel diagnostic-logging architecture is **complete and rule-consistent**:

> **Every ViewModel with a swallowing broad `catch (Exception)` that surfaces a user-facing error state is instrumented** with source-generated `[LoggerMessage]` diagnostic logging at `Error` (`MobileOtpLoginViewModel` at `Warning`; `SpecialistScheduleViewModel` also `Warning` for permission-denied). **Every ViewModel `[LoggerMessage]` is operation-name-only** — `Operation={Operation}` is the only variable token; the caught `Exception` and any record identifier are never passed to the logger. Self-logging coverage: **33 of 55 ViewModels**; the remaining 22 have no failure boundary. One deliberate, test-guarded skip: `BookingWizardViewModel.SearchNextAvailableDateAsync`. The only `[LoggerMessage]` methods that still take an `Exception` are `App.LogUnhandledException` (the global crash handler) and `Infrastructure/Api/HttpApiClient.LogApiRequestFailed` (Infra-layer HTTP observability) — both correct for their purpose and outside the ViewModel track. **The logging architecture is closed.**

**Not blocking / separately scoped:** the P1 missing-guard sweep (~17 uncaught command methods across 7 VMs — `ROJAN_PHASE8_59_*` §E.2 / `ROJAN_PHASE8_54_*` §F P1.1); the `HttpApiClient` Infra-observability payload decision.

---

## STOP

Phase 8.63 commit executed and validated. HEAD `5ba554c`. Build 0/0, 2,609/2,609 tests, architecture 7/7.
**Legacy `[LoggerMessage]` harmonization complete. The ViewModel diagnostic-logging architecture is CLOSED
and rule-consistent.** No behaviour change. Checkpoint updated
(`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`). Awaiting next authorization.
