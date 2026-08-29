# ROJAN AI — TEAM 3 — PHASE 8.49 — BOOKINGWIZARD LOGGING (WAVE 2C-3b) — COMMIT REPORT v1

**Type:** Commit execution. **No source change. No test change. No new files. No push. No merge. No rebase. No amend.**
**Branch:** `feature/team3-desktop-completion`
**Parent:** `7aa1d1b739b41a33f8b50f1319a7ff52318fb420`
**New HEAD:** `884cec36a6bbedea4b723227abbacb6dd3224441`
**Commit subject:** `fix(desktop): add ViewModel diagnostic logging (booking wizard)`

---

## A. COMMIT

```
commit 884cec36a6bbedea4b723227abbacb6dd3224441
Author:  Meisam Elhaee <meisamelh66@gmail.com>
Date:    Fri Aug 28 07:02:36 2026 -0700

    fix(desktop): add ViewModel diagnostic logging (booking wizard)

    Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>

    Claude-Session: https://claude.ai/code/session_01QKJ9GR9nPK5zfcYKD6kWZj
```

Subject is EXACT as authorized. Trailers match the Team 3 arc convention (`7aa1d1b`, `c01d0ce`, …).

---

## B. STAGING (explicit-path only)

```
git reset
git add <4 explicit paths>            # never git add . / git add -A
git diff --cached --name-only         # 4
```

| # | Path | Type |
|---|---|---|
| 1 | `src/Rojan.Desktop.Presentation/ViewModels/BookingWorkflow/BookingWizardViewModel.cs` | prod |
| 2 | `src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs` | prod (plumbing) |
| 3 | `tests/Rojan.Desktop.Presentation.Tests/BookingWorkflow/BookingWizardViewModelTests.cs` | test |
| 4 | `tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs` | test |

`git show --stat 884cec3`: **4 files changed, 185 insertions(+), 8 deletions(-)**. No new file. The 8 deletions are all signature-line replacements (`sealed`→`sealed partial`, single-line → multi-line ctor, extra ctor arg) — no behavioural line removed. All untracked `ROJAN_*.md` reports remain unstaged.

---

## C. SCOPE CONFIRMATION — staged diff reviewed pre-commit

| Area | Status |
|---|---|
| Profile ViewModels (`Customer`/`Service`/`InventoryProfileViewModel`) | ✅ untouched (not in commit) |
| Customer / Service / Inventory page VMs | ✅ untouched |
| DI — `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` | ✅ untouched |
| DI — `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` (`AddLogging()`) | ✅ untouched |
| Domain / Infrastructure / Shell / Application projects | ✅ untouched |
| Backend contracts / DTOs / API clients / `IBookingWorkflowService` / `IDialogService` / any interface | ✅ untouched |
| RBAC / permission gates | ✅ untouched |
| Authentication | ✅ untouched |
| Navigation / back-stack | ✅ untouched |
| Shared stubs — `StubBookingWorkflowService`, `StubDialogService`, `StubBookingQueryService`, `StubBookingCommandService`, `RecordingLogger.cs`, `RecordingLoggerFactory.cs` | ✅ untouched |
| `BookingWizardStep.cs` | ✅ untouched |

---

## D. POST-COMMIT VALIDATION

```
dotnet build -c Debug             → Build succeeded.  0 Warning(s)  0 Error(s)   (no SYSLIB1020)
dotnet test  -c Debug --no-build  → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | 651 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,594** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,594 / 2,594 PASS | 2,594 / 2,594 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Test-count progression: 2,587 (`7aa1d1b`) → **2,594** (`884cec3`), delta **+7** (all `Presentation.Tests`, 644 → 651).

---

## E. WHAT LANDED

Self-logging diagnostic logging for `BookingWizardViewModel` + `ILoggerFactory` parent→child plumbing via `BookingPageViewModel`:

| VM | Instrumented catches | `[LoggerMessage]` |
|---|---|---|
| `BookingWizardViewModel` | `LoadOptionsAsync`, `AddGuestCustomerAsync`, `LoadAvailableSlotsAsync`, `ConfirmBookingAsync` | `EventId=1, Level=Error, "Booking wizard operation failed. Operation={Operation}"` — signature `(string operation)`, **no `Exception` parameter** |

- Child: `sealed`→`sealed partial`, one `ILogger<BookingWizardViewModel> _logger` field, optional ctor param appended **after** `Action? onBookingCreated = null`, `?? NullLogger<BookingWizardViewModel>.Instance`, instance-form `[LoggerMessage]` (single field → no `SYSLIB1020`).
- Parent `BookingPageViewModel`: `+ ILoggerFactory? loggerFactory = null` appended after the existing optional `logger`; `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` passed at `OpenWizard()`. `ILoggerFactory` is not `ILogger` → the parent's existing `_logger` + **legacy `(string operation, Exception exception)` `[LoggerMessage]`** + its 5 call sites are untouched, no `SYSLIB1020`.
- All 4 log calls are the **last statement** of the existing `#pragma warning disable CA1031` broad catch, appended after the unchanged `ErrorMessage = ToFriendlyErrorMessage(exception); State = DashboardState.Error;` (and, in `AddGuestCustomerAsync`, before the unchanged `finally`). Append-only.
- **`SearchNextAvailableDateAsync` NOT instrumented** — best-effort cancellable probe, swallowed by design, never mutates `ErrorMessage`/`State`; its `catch`/`finally`/`_nextAvailableDateSearchCts` handling is byte-for-byte unchanged. Guarded by test `SearchNextAvailableDateAsync_ProbeFails_LogsNothing`.
- **Security:** no `Exception` object/message, no guest name/phone, no booking notes, no slot times, no customer id, no service price/duration, no backend body reachable from a log line. Test-enforced with seeded secrets on all 4 boundaries + the parent-forwarding test.
- **+7 tests** (4 boundary failure-logs w/ PII non-leak, 1 `SearchNextAvailableDateAsync` no-log guard, 1 NullLogger safety, 1 parent `ILoggerFactory` forwarding); 0 existing test bodies modified. Reused `RecordingLogger<T>` + `RecordingLoggerFactory` (from `7aa1d1b`); no new test helper; no shared stub change.

---

## F. GIT DISCIPLINE

- Explicit-path staging only (`git reset` then 4 × `git add <path>`). No `git add .` / `-A`.
- Staged diff reviewed before commit.
- **Not pushed. Not merged. Not rebased. Not amended.**
- One commit: `884cec3`.

---

## STOP

Phase 8.49 commit executed and validated. HEAD `884cec3`. Build 0/0, 2,594/2,594 tests, architecture 7/7.
Wave 2C-3b (BookingWizard) complete. Self-logging ViewModel coverage: **28/56 → 29/56**.
Checkpoint updated (`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`). Awaiting next authorization
(Wave 2C-3c — `Employee`/`Invoice`/`Specialist` profile VMs).
