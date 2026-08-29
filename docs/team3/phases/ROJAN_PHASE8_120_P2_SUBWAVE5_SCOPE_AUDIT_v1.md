# ROJAN AI — TEAM 3 — PHASE 8.120 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 5 (BOOKING + CALENDAR + INVENTORY) — SCOPE AUDIT v1

**Type:** Scope audit. **AUDIT ONLY — no source/test/localization/service/DI change, no commit.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `d10f9bc` (unchanged)
**Reference:** `ROJAN_PHASE8_119_P2_SUBWAVE4_COMMIT_REPORT_v1.md`, `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md`

**Bottom line:** **11 sites / 4 ViewModels.** All are the plain `catch (Exception exception)` top-level-boundary shape (identical to sub-waves 2 & 3 — **drop the exception variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**). No filtered catches, no typed catches, no `finally`, no `[CallerMemberName]`, no cancellation handling in any target method. **Two of the 11 are live, test-documented backend-body leaks** (`BookingPageViewModel.CreateBookingAsync`, `CalendarPageViewModel.InitializeAsync` — existing tests assert `Assert.Equal(backendBody, sut.ErrorMessage)`). LOW risk. One commit. `~2,715` (test count net-neutral to small +).

---

## A. GIT STATE

| | |
|---|---|
| HEAD | `d10f9bc2ff0dd4460dcd75bf41f9e246a6b8d300` (`fix(desktop): sanitize automation tab error surfacing` — Phase 8.116 + 8.117.1, committed 8.119) |
| Branch | `feature/team3-desktop-completion` |
| Tracked working tree | clean (0 modified/deleted) |
| Staged | none |
| Untracked | `.md` reports only |

Sub-waves 1–4 of the P2 track are landed (`76d3f61`, `1260d4e`, `b509054`, `d10f9bc`). This audit covers **sub-wave 5**.

---

## B. INVENTORY — 11 sites / 4 ViewModels

All under `src/Rojan.Desktop.Presentation/ViewModels/`. Every catch is `catch (Exception exception)` wrapped in `#pragma warning disable CA1031` … `#pragma warning restore CA1031`, body = `ErrorMessage = exception.Message; State = DashboardState.Error; Log…(nameof(<Method>));` (one variant nests the assignment inside a stale-response `if`). The `exception` variable is referenced **only** for `.Message` at every site.

### B.1 `Bookings/BookingPageViewModel.cs` — 5 sites

| # | Method | Line | Surface | `State` | Log call | Notes |
|---|---|---|---|---|---|---|
| 1 | `LoadAsync` | 357 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(LoadAsync))` | assignment inside `if (requestVersion == _filterVersion)` **stale-response guard** — keep the `if` |
| 2 | `CreateBookingAsync` | 402 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(CreateBookingAsync))` | **live test-documented leak** (`BookingPageViewModelTests:589` asserts `Assert.Equal(backendBody, sut.ErrorMessage)`); comment "does not clear the New Booking form fields" is unrelated — keep |
| 3 | `ChangeStatusAsync` | 433 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(ChangeStatusAsync))` | plain |
| 4 | `CancelSelectedBookingAsync` | 467 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(CancelSelectedBookingAsync))` | plain |
| 5 | `RescheduleSelectedBookingAsync` | 503 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(RescheduleSelectedBookingAsync))` | plain |

- `sealed partial class`; single `ILogger<BookingPageViewModel>` field + optional `ILoggerFactory?` (child-panel pass-through). `[LoggerMessage(EventId = 1, Level = Error, Message = "Booking operation failed. Operation={Operation}")]` — instance form, operation-name-only. **No `using Rojan.Desktop.Presentation.Localization;`** → needs `+ using` (or FQ form).

### B.2 `Calendar/CalendarPageViewModel.cs` — 3 sites

| # | Method | Line | Surface | `State` | Log call |
|---|---|---|---|---|---|
| 6 | `InitializeAsync` | 220 | `ErrorMessage` | `Error` | `LogLoadFailed(nameof(InitializeAsync))` — **live test-documented leak** (`CalendarPageViewModelTests:116` asserts `Assert.Equal(backendBody, sut.ErrorMessage)`) |
| 7 | `LoadDailyAvailabilityAsync` | 264 | `ErrorMessage` | `Error` | `LogLoadFailed(nameof(LoadDailyAvailabilityAsync))` |
| 8 | `LoadWeeklyAvailabilityAsync` | 304 | `ErrorMessage` | `Error` | `LogLoadFailed(nameof(LoadWeeklyAvailabilityAsync))` |

- `sealed partial class`; single `ILogger<CalendarPageViewModel>?` field. `[LoggerMessage(EventId = 1, Level = Error, Message = "Calendar availability load failed. Operation={Operation}")]` — instance form, operation-name-only (comment already states "the caught exception is never passed to the logger"). **No `using …Localization;`** → needs `+ using` (or FQ form).

### B.3 `Inventory/InventoryPageViewModel.cs` — 2 sites

| # | Method | Line | Surface | `State` | Log call | Notes |
|---|---|---|---|---|---|---|
| 9 | `LoadAsync` | 270 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(LoadAsync))` | plain |
| 10 | `SearchAsync` | 308 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(SearchAsync))` | assignment inside `if (string.Equals(searchText, SearchText, …))` **out-of-order guard** — keep the `if` |

- `sealed partial class`; `ILogger<InventoryPageViewModel>` + `ILoggerFactory?`. `[LoggerMessage(EventId = 1, Level = Error, Message = "Inventory page operation failed. Operation={Operation}")]` — instance form, operation-name-only.
- **Already `using Rojan.Desktop.Presentation.Localization;`** — the Wave-C Missing-Guard command guards (`CreateProductAsync` / `CreateCategoryAsync` / `CreateSupplierAsync`, lines 365/386/407) already use `catch (Exception) { ActionErrorMessage = Strings.Common_ActionFailedMessage; … }`. **No `using` addition.** These 2 remaining `ErrorMessage = exception.Message` sites are the *top-level load/search boundary* (a different surface — `ErrorMessage`, gated on `State`), untouched by Wave C.

### B.4 `Inventory/InventoryProfileViewModel.cs` — 1 site

| # | Method | Line | Surface | `State` | Log call |
|---|---|---|---|---|---|
| 11 | `LoadAsync` | 182 | `ErrorMessage` | `Error` | `LogOperationFailed(nameof(LoadAsync))` |

- `sealed partial class`; single `ILogger<InventoryProfileViewModel>?`. `[LoggerMessage(EventId = 1, Level = Error, Message = "Inventory profile operation failed. Operation={Operation}")]` — instance form, operation-name-only.
- **Already `using …Localization;`** (Wave-C guards `RecordTransactionAsync` / `MapProductToServiceAsync` / `UnmapProductFromServiceAsync` at 204/226/251 use `ActionErrorMessage = Strings.Common_ActionFailedMessage`). **No `using` addition.**

### Not targets (verified)

- `BookingWorkflow/BookingWizardViewModel.cs` — **already safe.** Booking Intelligence Phase 1 maps every caught exception through a `switch` expression to a fixed localized message ("never the raw `Exception.Message`" — its own doc comment). No `= exception.Message` anywhere.
- `Calendar/CalendarViewMode.cs` — enum, no logic.
- Inventory Wave-C command guards (6 sites) — already `Strings.Common_ActionFailedMessage`.

---

## C. CLASSIFICATION

| Aspect | Finding |
|---|---|
| Catch shape | **11 / 11 plain `catch (Exception exception)`** — the sub-wave 2 / 3 shape, NOT the Automation `when (… is not OperationCanceledException)` filtered shape. |
| Exception variable | Referenced **only** for `.Message` at all 11 → **drop the variable**: `catch (Exception exception)` → `catch (Exception)`. |
| Fix | `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;` (Calendar + Booking) / `= Strings.Common_ActionFailedMessage;` unqualified (Inventory ×3, already imports). |
| Preserve byte-unchanged | `#pragma warning disable/restore CA1031`; `State = DashboardState.Error`; every `LogOperationFailed` / `LogLoadFailed(nameof(<Method>))`; both `[LoggerMessage]` instance signatures; the `BookingPageViewModel.LoadAsync` `if (requestVersion == _filterVersion)` stale guard; the `InventoryPageViewModel.SearchAsync` `if (string.Equals(searchText, SearchText, …))` out-of-order guard; the `CreateBookingAsync` form-field-retention comment + behaviour; the `await LoadAsync()` success reloads. |
| Category (per Phase 8.102) | All **Category A** — `= exception.Message` to a bound `TextBlock` from a top-level broad catch. |
| New surfaces / flags | none — reuse the existing `ErrorMessage` property in every VM. |

---

## D. SECURITY

Every one of the 11 surfaces is a bound `ErrorMessage` `TextBlock`. The raw `exception.Message` currently reaches the user; it can carry backend response bodies, internal URLs, stack frames, DB fragments, and the domain-specific data below.

### D.1 Booking (5 sites)

| Method | What a raw message can expose |
|---|---|
| `LoadAsync` (booking search) | Other customers' **names**, appointment date/times, **specialist assignments**, **service names**, **prices**, booking IDs — a 500 echoing the query or a row |
| `CreateBookingAsync` | **Double-booking / slot-conflict bodies** naming the customer or specialist already in that slot; validation detail; pricing rules; **`BookingPageViewModelTests:589` proves a backend body reaches `ErrorMessage` today** |
| `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync` | Cancellation-policy text, penalty/refund figures, workflow-state internals, the affected booking's customer/specialist/service |

### D.2 Calendar (3 sites)

| Method | What a raw message can expose |
|---|---|
| `InitializeAsync` | Staff roster (**specialist names / IDs**), **service catalog + pricing** (loads `GetSpecialistsAsync` + `GetServicesAsync`); **`CalendarPageViewModelTests:116` proves a backend body reaches `ErrorMessage` today** |
| `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync` | A specific specialist's **working hours**, booked-vs-free **slot times** (implied customer bookings), specialist ID in the request echo |

### D.3 Inventory (3 sites)

| Method | What a raw message can expose |
|---|---|
| `InventoryPageViewModel.LoadAsync` | **Cost prices**, retail prices, **supplier names + terms**, category structure, **stock / low-stock levels** (loads products + categories + suppliers + low-stock) |
| `InventoryPageViewModel.SearchAsync` | Product names, SKUs, cost data in a query-echo 500 |
| `InventoryProfileViewModel.LoadAsync` | Per-product **cost**, **supplier**, full **stock-transaction history**, service mappings — the existing test sentinel is literally `"SKU-SECRET-9931 / Glow Beauty Supply Co. / $18"` and `"backend 500: SKU=WIDGET-9 cost=42.50 supplier=Acme Corp on-hand=7"` |

### D.4 Logs — already clean

All 11 sites call `Log…(nameof(<Method>))` — the exception object is **never** passed to the logger. Both existing `[LoggerMessage]` templates are `Operation={Operation}` only. The existing log-no-leak assertions (`CalendarPageViewModelTests:118`, `BookingPageViewModelTests:594`, `InventoryPageViewModelTests:302`, `InventoryProfileViewModelTests:255`, `InventoryProfileViewModelTests:26`) stay green and are retained.

**Two confirmed live leaks** (Booking `CreateBookingAsync`, Calendar `InitializeAsync`) are currently *asserted* by tests as correct behaviour — same situation as the sub-wave 2 `AcceptInviteViewModel` invite-token leak. Sub-wave 5 closes them and flips those assertions to `Strings.Common_ActionFailedMessage`.

---

## E. ARCHITECTURE

| Concern | Finding |
|---|---|
| **`[LoggerMessage]` availability** | All 4 VMs are already `sealed partial` with an instance-form operation-name-only `[LoggerMessage]`. **No logger change.** Each has exactly one `ILogger<T>` field → no `SYSLIB1020` risk (that only bites with ≥2 `ILogger` fields + instance form). |
| **Localization usage** | `Strings.Common_ActionFailedMessage` ships fa/en/ar since Wave A `794648e`. **`InventoryPageViewModel` + `InventoryProfileViewModel` already `using …Localization;`** (Wave C). **`CalendarPageViewModel` + `BookingPageViewModel` do NOT** → 2 prod files need `+ using Rojan.Desktop.Presentation.Localization;` (matches the sub-wave 1 `PosCheckoutViewModel` / `InvoiceProfileViewModel` precedent). No `.resx` change. |
| **Test impact** | ~10 existing assertions to update from the raw message to `Strings.Common_ActionFailedMessage`: `BookingPageViewModelTests` L72, **L589**, L609, L625, L643; `CalendarPageViewModelTests` L96, **L116**; `InventoryPageViewModelTests` L72, L88; `InventoryProfileViewModelTests` L37, L83. `CalendarPageViewModelTests` + `BookingPageViewModelTests` need `+ using …Localization;` (Inventory test files already have it). Recommend also adding a `DoesNotContain(<secret>, ErrorMessage)` sentinel assertion at the 2 confirmed-leak sites. Expect **+0 to +2 net tests** (mostly edits, not additions). |
| **Stub impact** | **None.** Every failure path is already exercised by an existing test via an existing stub seam (`Task.FromException`, `*Exception` stub properties). No new stub, no new seam. |
| **DI impact** | **None.** No constructor signature change, no registration change. |
| **Risk** | **LOW** — lowest-complexity sub-wave alongside sub-wave 2: uniform plain-catch shape, no filter/typed-catch/finally/CallerMemberName to preserve, 2 of 4 files already wired for localization. |

---

## F. RECOMMENDATION

**Proceed to a single implementation phase (8.121)** covering all 11 sites / 4 VMs in one commit.

1. **Prod (4 files):**
   - `BookingPageViewModel.cs` — `+ using Rojan.Desktop.Presentation.Localization;`; 5 catches `catch (Exception exception)` → `catch (Exception)`, `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`. Keep the `LoadAsync` stale-guard `if`, the `CreateBookingAsync` form-retention comment, every `State = Error`, every `LogOperationFailed(nameof(...))`.
   - `CalendarPageViewModel.cs` — `+ using …Localization;`; 3 catches, same swap; keep every `State = Error` + `LogLoadFailed(nameof(...))`.
   - `InventoryPageViewModel.cs` — **no `using` change**; 2 catches, same swap; keep the `SearchAsync` out-of-order `if`.
   - `InventoryProfileViewModel.cs` — **no `using` change**; 1 catch, same swap.
2. **Tests (4 files):** update the ~10 raw-message assertions to `Strings.Common_ActionFailedMessage`; `+ using …Localization;` in `BookingPageViewModelTests.cs` + `CalendarPageViewModelTests.cs`; add a `DoesNotContain` sentinel assertion at `CreateBookingAsync` + `InitializeAsync` (the 2 confirmed leaks). No stub/DI/`.resx` change.
3. **Validation:** `dotnet build` 0/0; full suite (expect `~2,715`, ±2); Architecture 7/7; Booking/Calendar/Inventory subsets green.
4. **Commit subject:** `fix(desktop): sanitize booking, calendar and inventory error surfacing`.
5. **STOP** after implementation → Phase 8.122 commit scope review → Phase 8.123 commit execution.

After sub-wave 5, only **sub-wave 6** remains (Dashboard + Analytics + Salon + QR + Support + Settings + the carried-over `CustomerProfileViewModel.LoadAsync`).

---

## STOP

Phase 8.120 scope audit complete. **AUDIT ONLY — nothing modified.** HEAD `d10f9bc`, tracked tree clean.

**Sub-wave 5 = 11 sites / 4 ViewModels:** `BookingPageViewModel` (`LoadAsync` / `CreateBookingAsync` / `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync`), `CalendarPageViewModel` (`InitializeAsync` / `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync`), `InventoryPageViewModel` (`LoadAsync` / `SearchAsync`), `InventoryProfileViewModel` (`LoadAsync`). All plain `catch (Exception exception)` — **drop the variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**, keeping `#pragma CA1031`, `State = Error`, the operation-name-only log call, and the two stale/out-of-order guards. 2 prod files (`BookingPageViewModel`, `CalendarPageViewModel`) + 2 test files (`BookingPageViewModelTests`, `CalendarPageViewModelTests`) need `+ using …Localization;`; the 2 Inventory VMs + tests already have it. **No `.resx` / DI / service / contract / stub change.** **2 confirmed live test-documented backend-body leaks** (`BookingPageViewModel.CreateBookingAsync`, `CalendarPageViewModel.InitializeAsync`). LOW risk, one commit.

**Awaiting Phase 8.121 — Sub-Wave 5 Implementation Authorization.**
