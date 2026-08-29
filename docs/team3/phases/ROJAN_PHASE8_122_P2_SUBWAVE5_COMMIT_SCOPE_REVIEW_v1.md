# ROJAN AI — TEAM 3 — PHASE 8.122 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 5 (BOOKING + CALENDAR + INVENTORY) — COMMIT SCOPE REVIEW v1

**Type:** Commit scope review. **STRICT MODE — no source/test change, no fix/refactor, no commit/push/merge/rebase.** Read-only verification.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `d10f9bc` (unchanged)
**Reference:** `ROJAN_PHASE8_120_P2_SUBWAVE5_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_121_P2_SUBWAVE5_IMPLEMENTATION_REPORT_v1.md`

**Verdict: ✅ READY TO COMMIT.** 11/11 sites sanitized, scope clean, build 0/0, 2,715/2,715 tests pass, Architecture 7/7, two live backend-body leaks closed.

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `d10f9bc2ff0dd4460dcd75bf41f9e246a6b8d300` (`fix(desktop): sanitize automation tab error surfacing` — Phase 8.116 + 8.117.1, committed 8.119) |
| Branch | `feature/team3-desktop-completion` |
| Staged | **none** (`git diff --cached` empty) |
| Working tree — tracked modified | **8 files** (4 prod + 4 test), all under `ViewModels/{Bookings,Calendar,Inventory}/` and `Tests/{Bookings,Calendar,Inventory}/` |
| New / deleted tracked files | none |
| Untracked | `.md` reports only |

```
 src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs            | 21 +++++++++++----------
 src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs           | 13 +++++++------
 src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs         |  8 ++++----
 src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs      |  4 ++--
 tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs          | 13 ++++++++-----
 tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs         |  7 +++++--
 tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs       |  4 ++--
 tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryProfileViewModelTests.cs    |  7 +++++--
 8 files changed, 44 insertions(+), 33 deletions(-)
```

**Confirmed:** only Phase 8.121 changes exist. No unrelated files, no stray edits.

---

## B. SCOPE REVIEW

| Layer | Files | Nature |
|---|---|---|
| Production | 4 — `BookingPageViewModel.cs`, `CalendarPageViewModel.cs`, `InventoryPageViewModel.cs`, `InventoryProfileViewModel.cs` | `catch (Exception exception)` → `catch (Exception)`; `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;` (11 sites); `+ using …Localization;` in 2 of them |
| Test | 4 — matching `*Tests.cs` | assertion updates on existing failure tests; `+ using …Localization;` in 2; 3 `DoesNotContain` sentinel additions |

**Confirmed ABSENT from the diff:**

| Must not be touched | Status |
|---|---|
| Services (`IBookingQueryService` / `IBookingCommandService` / `IBookingWorkflowService` / `ICalendarQueryService` / `IProductQueryService` / `IInventoryQueryService` / `IProductProfileQueryService` impls) | ✅ not in diff |
| Backend contracts / DTOs | ✅ not in diff |
| DI registration | ✅ not in diff |
| `Strings.resx` / `Strings.en.resx` / `Strings.ar.resx` | ✅ not in diff |
| Shell / navigation / auth | ✅ not in diff |
| Any other ViewModel | ✅ not in diff |
| Stubs / test doubles | ✅ not in diff |
| New files | ✅ none |

**`using` additions:** `+ using Rojan.Desktop.Presentation.Localization;` in `BookingPageViewModel.cs` + `CalendarPageViewModel.cs` (prod) and `BookingPageViewModelTests.cs` + `CalendarPageViewModelTests.cs` (test). The 2 Inventory VMs and their test files already imported it (Wave C `66c8490`). No FQ-vs-`using` inconsistency — every file references `Strings.Common_ActionFailedMessage` unqualified.

---

## C. SANITIZATION REVIEW

`git diff` on the 4 prod files = **exactly 11 changed catch bodies + 2 `using` lines**, nothing else. Full changed-line set:

```
+using Rojan.Desktop.Presentation.Localization;    (×2)
-        catch (Exception exception)                (×11)
+        catch (Exception)                          (×11)
-            ErrorMessage = exception.Message;       (×11, 2 of them extra-indented inside a guard `if`)
+            ErrorMessage = Strings.Common_ActionFailedMessage;   (×11)
```

`grep -rn "exception.Message" src/…/ViewModels/{Bookings,Calendar,Inventory}/` → **(none)**.
`grep -rn "= exception\b" …` → **(none)** — the `exception` variable is fully removed at all 11 sites (it was referenced only for `.Message`).

| # | VM · method | `Strings.Common_ActionFailedMessage` | `State = DashboardState.Error` | Log call (unchanged) | Guard (unchanged) |
|---|---|---|---|---|---|
| 1 | `BookingPageViewModel.LoadAsync` | ✅ | ✅ | `LogOperationFailed(nameof(LoadAsync))` | `if (requestVersion == _filterVersion)` **stale-response** |
| 2 | `BookingPageViewModel.CreateBookingAsync` | ✅ | ✅ | `LogOperationFailed(nameof(CreateBookingAsync))` | form-field-retention comment + behaviour |
| 3 | `BookingPageViewModel.ChangeStatusAsync` | ✅ | ✅ | `LogOperationFailed(nameof(ChangeStatusAsync))` | — |
| 4 | `BookingPageViewModel.CancelSelectedBookingAsync` | ✅ | ✅ | `LogOperationFailed(nameof(CancelSelectedBookingAsync))` | — |
| 5 | `BookingPageViewModel.RescheduleSelectedBookingAsync` | ✅ | ✅ | `LogOperationFailed(nameof(RescheduleSelectedBookingAsync))` | "does not clear RescheduleDate" comment + behaviour |
| 6 | `CalendarPageViewModel.InitializeAsync` | ✅ | ✅ | `LogLoadFailed(nameof(InitializeAsync))` | — |
| 7 | `CalendarPageViewModel.LoadDailyAvailabilityAsync` | ✅ | ✅ | `LogLoadFailed(nameof(LoadDailyAvailabilityAsync))` | `SelectedSpecialist/SelectedService` null guard |
| 8 | `CalendarPageViewModel.LoadWeeklyAvailabilityAsync` | ✅ | ✅ | `LogLoadFailed(nameof(LoadWeeklyAvailabilityAsync))` | null guard |
| 9 | `InventoryPageViewModel.LoadAsync` | ✅ | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 10 | `InventoryPageViewModel.SearchAsync` | ✅ | ✅ | `LogOperationFailed(nameof(SearchAsync))` | `if (string.Equals(searchText, SearchText, Ordinal))` **out-of-order** |
| 11 | `InventoryProfileViewModel.LoadAsync` | ✅ | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |

**Confirmed unchanged:** the `#pragma warning disable CA1031` / `restore CA1031` pair around every catch; every `State = DashboardState.Error`; every `Log…(nameof(<Method>))`; every `[LoggerMessage(EventId = 1, Level = Error, Message = "… Operation={Operation}")]` instance signature; the Booking stale-response `if` and the Inventory out-of-order `if`; the `await LoadAsync()` success reloads in the 4 Booking command methods; the Calendar null guards; the Booking form-retention comments and behaviour. No filtered/typed catch, no `finally`, no `[CallerMemberName]` anywhere in scope (all 11 are plain `catch (Exception exception)`). The 6 Inventory Wave-C command guards (`ActionErrorMessage = Strings.Common_ActionFailedMessage`) are untouched.

---

## D. SECURITY REVIEW

All 11 surfaces are bound `ErrorMessage` `TextBlock`s. With the `exception` variable removed, `.Message` / `.ToString()` / `.InnerException` is structurally unreachable from every one.

| Domain | Data no longer reachable | Enforcement |
|---|---|---|
| **Booking** | customer names, appointment times, **specialist assignments**, service names, prices; **slot-conflict / double-booking 409 bodies** naming another customer or specialist; cancellation-policy / penalty text | generic constant; `CreateBookingCommand_BackendThrows_…` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| **Calendar** | staff roster (**specialist names / IDs**), **service catalog + pricing**, a specialist's **working hours**, booked-vs-free **slot times** | generic constant; `InitializeAsync_…_NoExceptionLeak` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| **Inventory** | **cost prices**, retail prices, **supplier names + terms**, **stock / low-stock levels**, category structure, per-product **transaction history**, service mappings | generic constant; `InventoryProfileViewModelTests.LoadAsync_Failure_…_NoLeak` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(Secret, sut.ErrorMessage)` (`Secret = "SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"`) |

**Two confirmed live test-documented leaks closed** — `BookingPageViewModel.CreateBookingAsync` (`BookingPageViewModelTests` previously `Assert.Equal(backendBody, sut.ErrorMessage)`) and `CalendarPageViewModel.InitializeAsync` (`CalendarPageViewModelTests` previously `Assert.Equal(backendBody, sut.ErrorMessage)`). Both assertions now flipped to the generic constant + a `DoesNotContain` sentinel.

**Logs — operation-name-only, unchanged.** The exception object is never passed to any logger. Existing log no-leak assertions (`BookingPageViewModelTests` CreateBooking log check, `CalendarPageViewModelTests:118`, `InventoryPageViewModelTests` LoadAsync log check, `InventoryProfileViewModelTests:26`) retained and green.

---

## E. TEST REVIEW

| Gate | Expected | Actual |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Full suite | 2,715 / 2,715 | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain | 456 | 456 ✅ |
| — **Presentation** | 772 | **772** (assertion updates on existing tests — no net-new) ✅ |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Booking/Calendar/Inventory subset | — | **130 / 130 PASS** ✅ |

Test diff = 11 assertion updates (`"boom"` / `backendBody` → `Strings.Common_ActionFailedMessage`) + 3 `DoesNotContain` sentinel lines + 2 test-file `using` additions. No new test, no new stub, no DI change. Suite progression: 2,715 (`d10f9bc`) → **2,715** (P2 sub-wave 5, +0).

---

## F. COMMIT READINESS

| Item | State |
|---|---|
| Scope | ✅ 8 files (4 prod + 4 test), all within Phase 8.121's STRICT SCOPE |
| Base HEAD | `d10f9bc` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; subset 130 / 130 |
| Sites | ✅ 11 / 11 — only the catch clause + `ErrorMessage =` line changed; `#pragma CA1031`, `State = Error`, log calls, both guards byte-unchanged |
| Security | ✅ customer/appointment/specialist data, staff schedules & availability, stock/supplier/cost data all structurally unreachable; 2 live backend-body leaks closed; logs operation-name-only |
| Behaviour | ✅ unchanged — error-state recovery, stale-response discard, out-of-order discard, form-field retention, `await LoadAsync()` reloads all preserved |
| Localization | ✅ no `.resx` change; `+ using …Localization;` in 2 prod + 2 test files (Inventory pair already had it) |
| DI / services / contracts / stubs | ✅ none |
| Deferred | none — all 11 audited sites done |
| Line endings | tool-edited files may show benign LF/CRLF `git diff` warnings; `core.autocrlf=true` normalises to LF in the committed blob — cosmetic |

### Proposed commit (Phase 8.123 — on authorization)

**Subject:**
```
fix(desktop): sanitize booking calendar inventory error surfacing
```

**Body (suggested):**
```
Replace raw exception.Message on the Booking, Calendar and Inventory top-level
error surfaces with the generic localized Strings.Common_ActionFailedMessage.
Covers all 11 plain broad-catch sites across BookingPageViewModel
(Load/CreateBooking/ChangeStatus/CancelSelectedBooking/RescheduleSelectedBooking),
CalendarPageViewModel (Initialize/LoadDailyAvailability/LoadWeeklyAvailability),
InventoryPageViewModel (Load/Search) and InventoryProfileViewModel (Load).

Each catch drops its now-unused exception variable; the #pragma CA1031 pair,
State = DashboardState.Error, the operation-name-only LogOperationFailed /
LogLoadFailed calls, the BookingPageViewModel stale-response guard and the
InventoryPageViewModel out-of-order guard are byte-unchanged. No service,
contract, DI or .resx change.

Customer / appointment / specialist data, staff schedules and availability, and
stock / supplier / cost data no longer reach any UI surface. Two live
test-documented backend-body leaks (BookingPageViewModel.CreateBookingAsync,
CalendarPageViewModel.InitializeAsync) are closed. Logs remain
operation-name-only. Existing failure-test assertions updated (+0 net tests).

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

**Staging procedure (Phase 8.123):** `git reset` → 8 explicit `git add` paths (never `git add .` / `-A`):
```
git add src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs
git add src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs
git add tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryProfileViewModelTests.cs
```
Then `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update, then STOP.

---

## STOP

Phase 8.122 commit scope review complete. **Verdict: READY.**

Working tree = `d10f9bc` + 8 uncommitted sub-wave-5 files (4 prod + 4 test). HEAD unchanged, nothing staged. **11 / 11** Booking/Calendar/Inventory error surfaces sanitized — the only production change is `catch (Exception exception)` → `catch (Exception)` + `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;` at each. `#pragma CA1031`, `State = Error`, every operation-name-only log call, the stale-response guard and the out-of-order guard are byte-unchanged. `+ using …Localization;` in 2 prod + 2 test files; no `.resx` / DI / service / contract / stub change. Build 0/0, 2,715 / 2,715 tests pass, Architecture 7/7, subset 130/130. Two live test-documented backend-body leaks closed. +0 net tests.

**Awaiting Phase 8.123 — Sub-Wave 5 Commit Authorization.**
