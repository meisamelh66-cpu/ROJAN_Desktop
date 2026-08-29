# ROJAN AI — TEAM 3 — PHASE 8.121 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 5 (BOOKING + CALENDAR + INVENTORY) — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.122 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `d10f9bc` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_120_P2_SUBWAVE5_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 8 (4 prod + 4 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs            | 21 +++++++++--------
 src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs           | 13 ++++++-----
 src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryPageViewModel.cs         |  8 +++----
 src/Rojan.Desktop.Presentation/ViewModels/Inventory/InventoryProfileViewModel.cs      |  4 ++--
 tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs          | 13 +++++++----
 tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs         |  7 +++++--
 tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryPageViewModelTests.cs       |  4 ++--
 tests/Rojan.Desktop.Presentation.Tests/Inventory/InventoryProfileViewModelTests.cs    |  7 +++++--
 8 files changed, 46 insertions(+), 32 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, other ViewModels, stubs/test doubles. No new files.

**`using` additions:** `+ using Rojan.Desktop.Presentation.Localization;` in **2 prod** (`BookingPageViewModel.cs`, `CalendarPageViewModel.cs`) + **2 test** (`BookingPageViewModelTests.cs`, `CalendarPageViewModelTests.cs`). The 2 Inventory VMs and their 2 test files already import it (Wave C `66c8490`) — no addition.

---

## B. SITES SANITIZED — 11

Every site: `catch (Exception exception)` → `catch (Exception)` (variable dropped — referenced only for `.Message`), `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`.

| # | VM · method | Surface | `State = Error` | Log call | Guard preserved |
|---|---|---|---|---|---|
| 1 | `BookingPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | `if (requestVersion == _filterVersion)` stale-response guard — byte-unchanged |
| 2 | `BookingPageViewModel.CreateBookingAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(CreateBookingAsync))` | form-field-retention comment + behaviour unchanged |
| 3 | `BookingPageViewModel.ChangeStatusAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(ChangeStatusAsync))` | — |
| 4 | `BookingPageViewModel.CancelSelectedBookingAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(CancelSelectedBookingAsync))` | — |
| 5 | `BookingPageViewModel.RescheduleSelectedBookingAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(RescheduleSelectedBookingAsync))` | "does not clear RescheduleDate" comment + behaviour unchanged |
| 6 | `CalendarPageViewModel.InitializeAsync` | `ErrorMessage` | ✅ | `LogLoadFailed(nameof(InitializeAsync))` | — |
| 7 | `CalendarPageViewModel.LoadDailyAvailabilityAsync` | `ErrorMessage` | ✅ | `LogLoadFailed(nameof(LoadDailyAvailabilityAsync))` | `SelectedSpecialist/SelectedService` null guard unchanged |
| 8 | `CalendarPageViewModel.LoadWeeklyAvailabilityAsync` | `ErrorMessage` | ✅ | `LogLoadFailed(nameof(LoadWeeklyAvailabilityAsync))` | null guard unchanged |
| 9 | `InventoryPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 10 | `InventoryPageViewModel.SearchAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(SearchAsync))` | `if (string.Equals(searchText, SearchText, StringComparison.Ordinal))` out-of-order guard — byte-unchanged |
| 11 | `InventoryProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |

**Byte-unchanged everywhere:** the `#pragma warning disable CA1031` / `#pragma warning restore CA1031` pair around every catch; every `State = DashboardState.Error`; every `LogOperationFailed` / `LogLoadFailed(nameof(<Method>))`; every `[LoggerMessage(EventId = 1, Level = Error, Message = "… Operation={Operation}")]` instance signature; the Booking `LoadAsync` stale-response `if`; the Inventory `SearchAsync` out-of-order `if`; the `await LoadAsync()` success-path reloads in the 4 Booking command methods; the Calendar null guards; the Booking form-field-retention comments and behaviour.

**No filtered / typed catches in scope** — all 11 are plain `catch (Exception exception)` (verified: no `when` clause, no `OperationCanceledException` / `UnauthorizedOperationException` branch, no `finally`, no `[CallerMemberName]` in any target method). The Inventory Wave-C command guards (`CreateProductAsync` etc., `catch (Exception) { ActionErrorMessage = Strings.Common_ActionFailedMessage; }`) were already sanitized and are untouched.

---

## C. SECURITY IMPACT

Every one of the 11 surfaces is a bound `ErrorMessage` `TextBlock`. Raw `exception.Message` / `.ToString()` / `.InnerException` is now structurally unreachable from all of them — the `exception` variable is no longer bound.

| Data class | Was reachable via | Now |
|---|---|---|
| **Backend response bodies** (double-booking / slot-conflict 409s naming another customer or specialist; validation 400s) | Booking `CreateBookingAsync` — **`BookingPageViewModelTests` asserted `Assert.Equal(backendBody, sut.ErrorMessage)` (live leak)** | **not reachable** — test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| Backend response bodies (calendar init 500s) | Calendar `InitializeAsync` — **`CalendarPageViewModelTests` asserted `Assert.Equal(backendBody, sut.ErrorMessage)` (live leak)** | **not reachable** — test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` |
| Customer names, appointment times, specialist assignments, service names, prices | Booking `LoadAsync` / `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync` | **not reachable** — generic constant |
| Staff roster (specialist names / IDs), service catalog + pricing | Calendar `InitializeAsync` | **not reachable** — generic constant |
| A specialist's working hours, booked-vs-free slot times | Calendar `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync` | **not reachable** — generic constant |
| **Cost prices, retail prices, supplier names + terms, stock / low-stock levels, category structure** | Inventory `InventoryPageViewModel.LoadAsync` / `SearchAsync` | **not reachable** — generic constant |
| **Per-product cost, supplier, full stock-transaction history, service mappings** | `InventoryProfileViewModel.LoadAsync` | **not reachable** — sentinel `"SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"` now asserted absent from `sut.ErrorMessage` |

**Logs unchanged** — operation-name-only at all 11 sites (the exception object is never passed to the logger). Every pre-existing log no-leak assertion (`BookingPageViewModelTests:594`, `CalendarPageViewModelTests:118`, `InventoryPageViewModelTests` LoadAsync log test, `InventoryProfileViewModelTests:26`) is retained and green.

**Two confirmed live test-documented leaks closed** — `BookingPageViewModel.CreateBookingAsync` and `CalendarPageViewModel.InitializeAsync` previously had tests *asserting* the raw backend body reached `ErrorMessage` as correct behaviour (same situation as the sub-wave 2 `AcceptInviteViewModel` invite-token leak). Both assertions are now flipped to the generic constant + a `DoesNotContain` sentinel.

---

## D. TESTS

**+0 net tests** (Presentation.Tests stays at **772**). All changes are assertion updates on existing tests + 2 strengthening `DoesNotContain` sentinel additions.

| File | Change |
|---|---|
| `BookingPageViewModelTests` | `+ using …Localization;`. 5 assertions `Assert.Equal("boom" / backendBody, sut.ErrorMessage)` → `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` in `Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage`, `CreateBookingCommand_BackendThrows_SetsErrorStateAndLogsWithoutClearingForm`, `ConfirmBookingCommand_BackendThrows_SetsErrorState`, `CancelBookingCommand_WorkflowThrows_SetsErrorState`, `RescheduleBookingCommand_WorkflowThrows_SetsErrorStateAndDoesNotClearRescheduleDate`. `+ Assert.DoesNotContain(backendBody, sut.ErrorMessage ?? "", Ordinal)` in the CreateBooking test. |
| `CalendarPageViewModelTests` | `+ using …Localization;`. `Constructor_SpecialistsQueryThrows_StateIsErrorAndSetsErrorMessage` + `InitializeAsync_SpecialistsQueryThrows_LogsErrorWithOperation_NoExceptionLeak` → `Strings.Common_ActionFailedMessage`; `+ Assert.DoesNotContain(backendBody, sut.ErrorMessage ?? "", Ordinal)` in the InitializeAsync test. |
| `InventoryPageViewModelTests` | 2 assertions in `Constructor_QueryServiceThrows_StateIsErrorAndSetsErrorMessage` + `LoadAsync_QueryServiceThrows_LogsError` → `Strings.Common_ActionFailedMessage`. (File already `using …Localization;`.) |
| `InventoryProfileViewModelTests` | 2 assertions in `LoadAsync_Failure_WithoutLogger_UsesNullLogger_NeverThrows` + `Constructor_ProfileQueryThrows_StateIsErrorAndSetsErrorMessage` → `Strings.Common_ActionFailedMessage`; `+ Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, sut.ErrorMessage ?? "", Ordinal)` in `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoLeak`. (File already `using …Localization;`.) |

No new stub, no new seam, no DI change. Every failure path was already exercised by an existing test.

**Subset run:** `Bookings` + `Calendar` + `Inventory` namespaces → **130 / 130 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `d10f9bc` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Full test suite | 2,715+ | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain | 456 | 456 ✅ |
| — **Presentation** | 772 | **772** (assertion updates on existing tests — no net-new) ✅ |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Booking/Calendar/Inventory subset | — | **130 / 130 PASS** ✅ |

Suite progression: 2,715 (`d10f9bc`) → **2,715** (P2 sub-wave 5 — assertion updates, no net-new tests).

`grep -rn "exception.Message" src/…/ViewModels/{Bookings,Calendar,Inventory}/` → **(none)**.

---

## STOP

Phase 8.121 implementation complete. Base HEAD `d10f9bc` unchanged (no commit). Build 0/0, **2,715 / 2,715** tests pass, Architecture 7/7, Booking/Calendar/Inventory subset 130/130.

**11 sites / 4 ViewModels sanitized** — `BookingPageViewModel` (`LoadAsync` / `CreateBookingAsync` / `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync`), `CalendarPageViewModel` (`InitializeAsync` / `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync`), `InventoryPageViewModel` (`LoadAsync` / `SearchAsync`), `InventoryProfileViewModel` (`LoadAsync`). Each: `catch (Exception exception)` → `catch (Exception)`, `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`. The `#pragma CA1031` pair, every `State = DashboardState.Error`, every operation-name-only log call, the Booking stale-response guard and the Inventory out-of-order guard are byte-unchanged. `+ using …Localization;` in 2 prod + 2 test files (the 2 Inventory VMs + tests already import it). **No `.resx` / DI / service / contract / stub change.** +0 net tests. **Two confirmed live test-documented backend-body leaks (`BookingPageViewModel.CreateBookingAsync`, `CalendarPageViewModel.InitializeAsync`) are closed;** cost data, supplier terms, stock levels, customer/appointment data, and staff schedules no longer reach any UI surface.

**Awaiting Phase 8.122 — Sub-Wave 5 Commit Scope Review.**
