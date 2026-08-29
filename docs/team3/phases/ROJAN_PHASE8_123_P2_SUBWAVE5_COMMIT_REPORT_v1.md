# ROJAN AI — TEAM 3 — PHASE 8.123 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 5 (BOOKING + CALENDAR + INVENTORY) — COMMIT REPORT v1

**Type:** Commit execution. One commit performed. No source/test change beyond what Phase 8.121 produced; no push / merge / rebase / amend.
**Authorization:** Phase 8.123 — APPROVED (reference `ROJAN_PHASE8_122_P2_SUBWAVE5_COMMIT_SCOPE_REVIEW_v1.md`).
**Branch:** `feature/team3-desktop-completion`

---

## A. GIT STATE

| | Before | After |
|---|---|---|
| HEAD | `d10f9bc` | **`71fb472d6306ec609a5a6ba5b46c775a19f7e40e`** |
| Parent | — | `d10f9bc` |
| Branch | `feature/team3-desktop-completion` | unchanged |
| Tracked working tree | 8 modified | **clean** |
| Staged | none | none (committed) |
| Pushed? | — | **No** — local only |

**Staging:** `git reset` → 8 explicit per-path `git add` (4 prod + 4 test) → staged diff reviewed → `git commit`. **No `git add .` / `git add -A`.**

Staged diff reviewed before commit — exactly: 11 × `catch (Exception exception)` → `catch (Exception)`, 11 × `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`, 4 × `+ using Rojan.Desktop.Presentation.Localization;` (2 prod + 2 test), 11 × failure-test assertion updated to the generic constant, 3 × `Assert.DoesNotContain(<secret>, sut.ErrorMessage …)`. Nothing else.

### Commit `71fb472`

```
fix(desktop): sanitize booking calendar inventory error surfacing

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

`8 files changed, 44 insertions(+), 33 deletions(-)`

| File | Δ |
|---|---|
| `src/…/ViewModels/Bookings/BookingPageViewModel.cs` | 21 (`+using` + 5 sites) |
| `src/…/ViewModels/Calendar/CalendarPageViewModel.cs` | 13 (`+using` + 3 sites) |
| `src/…/ViewModels/Inventory/InventoryPageViewModel.cs` | 8 (2 sites) |
| `src/…/ViewModels/Inventory/InventoryProfileViewModel.cs` | 4 (1 site) |
| `tests/…/Bookings/BookingPageViewModelTests.cs` | 13 (`+using` + 5 asserts + 1 sentinel) |
| `tests/…/Calendar/CalendarPageViewModelTests.cs` | 7 (`+using` + 2 asserts + 1 sentinel) |
| `tests/…/Inventory/InventoryPageViewModelTests.cs` | 4 (2 asserts) |
| `tests/…/Inventory/InventoryProfileViewModelTests.cs` | 7 (2 asserts + 1 sentinel) |

---

## B. BOOKING / CALENDAR / INVENTORY CLOSURE

`grep -rn "exception.Message" src/…/ViewModels/{Bookings,Calendar,Inventory}/` → **(none)** at `71fb472`.

| # | VM · method | `State = Error` | Log call | Guard preserved |
|---|---|---|---|---|
| 1–5 | `BookingPageViewModel` · `LoadAsync` / `CreateBookingAsync` / `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync` | ✅ all 5 | `LogOperationFailed(nameof(...))` | `LoadAsync` stale-response `if (requestVersion == _filterVersion)` |
| 6–8 | `CalendarPageViewModel` · `InitializeAsync` / `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync` | ✅ all 3 | `LogLoadFailed(nameof(...))` | `SelectedSpecialist/SelectedService` null guards |
| 9–10 | `InventoryPageViewModel` · `LoadAsync` / `SearchAsync` | ✅ both | `LogOperationFailed(nameof(...))` | `SearchAsync` out-of-order `if (string.Equals(searchText, SearchText, Ordinal))` |
| 11 | `InventoryProfileViewModel` · `LoadAsync` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |

**11 / 11 sanitized.** Each: `catch (Exception exception)` → `catch (Exception)` (variable dropped — was referenced only for `.Message`), `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`. **Unchanged (verified in staged diff):** the `#pragma warning disable CA1031` / `restore CA1031` pair at every site; every `State = DashboardState.Error`; every operation-name-only log call; both guards; the 4 Booking-command `await LoadAsync()` reloads; the Calendar null guards; the Booking form-field-retention comments and behaviour. No filtered/typed catch, no `finally`, no `[CallerMemberName]` in scope. The 6 Inventory Wave-C command guards (`ActionErrorMessage = Strings.Common_ActionFailedMessage`) were untouched.

---

## C. SECURITY IMPROVEMENT

With the `exception` variable removed at all 11 sites, `exception.Message` / `.ToString()` / `.InnerException` is structurally unreachable from every bound `ErrorMessage` `TextBlock` in the Booking, Calendar, and Inventory pages.

| Domain | Data no longer reachable | Enforcement |
|---|---|---|
| **Booking** | customer names, appointment times, specialist assignments, service names, prices; **double-booking / slot-conflict 409 bodies** naming another customer/specialist; cancellation-policy / penalty text | generic constant; `CreateBookingCommand_BackendThrows_…` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| **Calendar** | staff roster (specialist names / IDs), service catalog + pricing, a specialist's working hours, booked-vs-free slot times | generic constant; `InitializeAsync_…_NoExceptionLeak` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| **Inventory** | **cost prices**, retail prices, **supplier names + terms**, **stock / low-stock levels**, category structure, per-product **transaction history**, service mappings | generic constant; `InventoryProfileViewModelTests.LoadAsync_Failure_…_NoLeak` now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(Secret, sut.ErrorMessage)` (`Secret = "SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"`) |

**Two confirmed live test-documented backend-body leaks closed** — `BookingPageViewModel.CreateBookingAsync` and `CalendarPageViewModel.InitializeAsync` previously had tests *asserting* `Assert.Equal(backendBody, sut.ErrorMessage)` as correct behaviour (same class as the sub-wave 2 `AcceptInviteViewModel` invite-token leak). Both assertions flipped to the generic constant + a `DoesNotContain` sentinel.

**Logs remain operation-name-only** — the exception object is never passed to any logger. Every pre-existing log no-leak assertion is retained and green.

---

## D. TEST RESULTS — post-commit at `71fb472`

| Gate | Expected | Actual |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Full suite | 2,715 / 2,715 | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain | 456 | 456 ✅ |
| — Presentation | 772 | **772** (assertion updates on existing tests — no net-new) ✅ |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Booking/Calendar/Inventory subset | — | **130 / 130 PASS** ✅ |

Suite progression: 2,715 (`d10f9bc`) → **2,715** (`71fb472`, +0 — assertion updates, no net-new tests).

---

## E. REMAINING P2 WAVES

| Sub-wave | Scope | Status |
|---|---|---|
| 1 | Reporting + AI Center + Accounting/POS (11 sites) | ✅ `76d3f61` (8.104 / 8.106) |
| 2 | Customers + HR + Membership (6 of 7 sites) | ✅ `1260d4e` (8.108 / 8.110) — `CustomerProfileViewModel.LoadAsync` (site 7) deferred |
| 3 | Organization + Specialists + Services (8 sites / 7 VMs) | ✅ `b509054` (8.112 / 8.114) |
| 4 | Automation tabs (13 / 13 sites / 5 tab VMs) | ✅ `d10f9bc` (8.116 + 8.117.1 / 8.119) |
| **5** | **Booking + Calendar + Inventory (11 / 11 sites / 4 VMs)** | ✅ **`71fb472` (Phase 8.121, committed 8.123)** |
| 6 | Dashboard + Analytics + Salon + QR + Support + Settings | **remaining** — ~8–10 sites, incl. `SettingsPageViewModel`'s 2 `NotSupportedException`→`StatusMessage` branches (Category D, optional) **+ `CustomerProfileViewModel.LoadAsync`** carried over from sub-wave 2 |

**Sub-wave 6 is the last P2 sub-wave.** After it, the "sanitize load-error surfacing" P2 track (58 Category-A sites / 30 VMs) is complete.

**Also outstanding (documented, not authorized):** the 3 local-only infra VMs (`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`) as **P3**; the Phase 8.99.1 `SettingsPage.xaml` visibility-trigger tweak.

`LoginViewModel` / `MobileOtpLoginViewModel` are already correct (typed `ApiException` catches → `Strings.Login_*`).

---

## F. CHECKPOINT

`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` updated: §A HEAD `d10f9bc` → `71fb472` + banner + audit-phase list (+8.122) + commit chain; §B commit table (+`71fb472` row); §E build/test (`71fb472`, 2,715 → 2,715 +0) + progression line; §F Phase 8.121 detail bullet; §G P2 track (sub-wave 5 ✅ 11/11; sub-wave 6 + `CustomerProfileViewModel` remain); §H items 1/2/5/6; STOP update-history (Phase 8.122 review note + Phase 8.123 commit entry). No code changed in performing the checkpoint update.

---

## STOP

Phase 8.123 commit execution complete. **HEAD `71fb472`** (`fix(desktop): sanitize booking calendar inventory error surfacing`), parent `d10f9bc`, branch `feature/team3-desktop-completion`, **not pushed**. Tracked working tree clean.

**Sub-wave 5 complete — 11 / 11 Booking/Calendar/Inventory error surfaces sanitized.** `catch (Exception exception)` → `catch (Exception)` + `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;` at each; `#pragma CA1031`, `State = Error`, every operation-name-only log call, the Booking stale-response guard, the Inventory out-of-order guard byte-unchanged; `+ using …Localization;` in 2 prod + 2 test files; no `.resx` / DI / service / contract / stub change. Build 0/0, 2,715 / 2,715 tests pass, Architecture 7/7, subset 130/130. Two live test-documented backend-body leaks closed; cost data, supplier terms, stock levels, customer/appointment data and staff schedules no longer reach any UI surface. +0 net tests.

**P2 remaining: sub-wave 6 (Dashboard + Analytics + Salon + QR + Support + Settings + `CustomerProfileViewModel.LoadAsync`) — the final P2 sub-wave.**

**Awaiting next authorization block.**
