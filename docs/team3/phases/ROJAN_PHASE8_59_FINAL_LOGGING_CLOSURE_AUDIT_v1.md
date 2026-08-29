# ROJAN AI — TEAM 3 — PHASE 8.59 — FINAL LOGGING CLOSURE AUDIT + HARDENING ROADMAP REVIEW v1

**Type:** Audit only. **No source change. No test change. No logger / DI change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `6a1bced659ae129da48d2453c5636868c1455701` — `fix(desktop): add ViewModel diagnostic logging (specialist page)` (Phase 8.56, committed 8.58)
**Reference:** `ROJAN_PHASE8_58_SPECIALIST_PAGE_LOGGING_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F, `ROJAN_PHASE8_54_REMAINING_VIEWMODEL_GAP_AUDIT_v1.md`.

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `6a1bced659ae129da48d2453c5636868c1455701` |
| HEAD subject | `fix(desktop): add ViewModel diagnostic logging (specialist page)` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. This audit adds no code.

---

## B. LOGGING CLOSURE VERIFICATION

### B.1 Machine check — zero uninstrumented swallowing catches

```
for each ViewModels/**/*ViewModel.cs:
    if (count("catch (Exception") > 0 and count("[LoggerMessage") == 0): report
→ (no output)
```

**Every ViewModel with a broad `catch (Exception)` now carries a `[LoggerMessage]`.** The track is closed.

- `grep -rn "\.LogError(\|\.LogWarning(\|\.LogInformation(\|_logger\.Log("` across `src/` → **zero raw logger calls** — all diagnostic logging goes through source-generated `[LoggerMessage]` (CA1848 satisfied everywhere).

### B.2 Self-logging ViewModels — 33 of 55

| Wave | ViewModels |
|---|---|
| Wave 1 (`2453a7f`, `31f4b63`) | `DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel`, `MobileOtpLoginViewModel` |
| Booking/Checkout + Shift-Engine hardening (`da18c18`, `ea03d83`) | `PosCheckoutViewModel`, `BookingPageViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel` |
| Wave 2A (`75357e1`) | `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`, `ReportingPageViewModel` |
| Wave 2B (`2ed685a`) | `AnalyticsPageViewModel`, `AiCenterPageViewModel`, `SalonPageViewModel`, `QrCodesPageViewModel` |
| Wave 2B-2 (`cbc3a82`) | `OrganizationPageViewModel` |
| Wave 2C-1 (`0542041`, `38c24da`) | `SupportPageViewModel`, `AcceptInviteViewModel` |
| Wave 2C-2 (`c01d0ce`) | `AutomationDashboardTabViewModel`, `ApprovalsTabViewModel`, `BusinessRulesTabViewModel`, `ScheduledJobsTabViewModel`, `WorkflowsTabViewModel` |
| Wave 2C-3a (`7aa1d1b`) | `CustomerProfileViewModel`, `ServiceProfileViewModel`, `InventoryProfileViewModel` |
| Wave 2C-3b (`884cec3`) | `BookingWizardViewModel` |
| Wave 2C-3c (`5b7f6ca`) | `EmployeeProfileViewModel`, `InvoiceProfileViewModel`, `SpecialistProfileViewModel` |
| Wave 2D (`6a1bced`) | `SpecialistPageViewModel` |

**= 33.** *(`AutomationPageViewModel` is plumbing-only — 5 `ILogger<TChild>?` pass-through params, no `[LoggerMessage]`, no catch — not counted here.)*

### B.3 Remaining 22 ViewModels — why no logging is required

| Group | ViewModels | Reason |
|---|---|---|
| Pure state / layout holders — no service calls, no `catch` | `SearchResultRowViewModel`, `NotificationGroupViewModel`, `NotificationRowViewModel`, `ToastNotificationViewModel`, `FilterEntryViewModel`, `PlaceholderModuleViewModel`, `HelpDialogViewModel`, `Workspaces/DockedPanelViewModel`, `FloatingWindowHandleViewModel`, `PaneLeafViewModel`, `PaneSplitViewModel`, `TabViewModel`, `WorkspaceHostViewModel`, `WorkspaceOutlineViewModel` (14) | No failure boundary exists — nothing to instrument |
| Singleton UI hosts / overlays — subscribe to a store or manage a UI queue; no I/O `catch` | `CommandPaletteViewModel`, `NotificationCenterViewModel`, `ToastHostViewModel` (3) | No swallowing broad `catch`; `CommandPaletteViewModel`'s CancellationToken gap is a separate, tracked P1 (not a logging item) |
| Local/synchronous | `SettingsPageViewModel` (1) | Reads/writes a local settings object; no async backend boundary |
| Pass-through parent | `AutomationPageViewModel` (1) | Forwards `ILogger<TChild>?` to its 5 tabs; owns no load/command logic, no `catch` — already carries the correct plumbing (Wave 2C-2) |
| Thin wrapper | `LoginWindowViewModel` (1) | Exposes `MobileLogin` only — no logic |
| Retired implementation | `Security/LoginViewModel` (1) | Credentials login — **no `LoginView` exists**; Shell's `LoginWindow` binds `MobileOtpLoginViewModel` only. `AddTransient`-registered but unreachable (architecture §C.6: retired implementations kept, unreferenced). Its typed `catch (ApiException)` fallthrough logs nothing (vs MobileOtp which does) — see §E P2. |

**= 22.** None has a swallowing broad `catch (Exception)` that surfaces a user-facing recoverable failure.

### B.4 Deliberate skip

`BookingWizardViewModel.SearchNextAvailableDateAsync` (5th catch) — best-effort cancellable "next available date" probe, swallowed by design, cancellation-dominated, never mutates `ErrorMessage`/`State`. Authorizer-approved (Phase 8.46 §I / 8.47), test-guarded (`SearchNextAvailableDateAsync_ProbeFails_LogsNothing`).

**→ Logging closure verified. 33/55 self-logging; 22/55 correctly uninstrumented; 1 deliberate skip.**

---

## C. SECURITY FINAL REVIEW

### C.1 Global rules (Phase 8.15+)

| | Rule |
|---|---|
| **Allowed** | `Operation=nameof(Method)` — a constant message template with at most a `string operation` argument |
| **Forbidden** | `Exception` object; `Exception.Message`; PII (name/phone/email/company/VAT/receipt text/resume URLs); financial values (prices/amounts/payments/salary/commission); backend response bodies; tokens (bearer/invite/session); user identity / record identifiers; chat/message content; role data |

### C.2 Waves 1–2D compliance

| Wave-family | Form | `Exception` passed? | Identifiers logged? | Verdict |
|---|---|---|---|---|
| Waves 2A / 2B / 2B-2 / 2C-1 / 2C-2 / 2C-3a / 2C-3b / 2C-3c / 2D + MobileOtp (24 VMs) | instance-form `LogOperationFailed(string operation)` (Automation-tab plumbing typed; SpecialistPage static — both `(…, string operation)`) | **no** | **no** | ✅ **compliant** |
| Wave 1 + Booking/Checkout + Shift-Engine (7 VMs, pre-8.15) | legacy — `[LoggerMessage(…, Exception exception)]` | **yes** | `SpecialistId` in 2 of them | ⚠️ **legacy — see §D** |

Every ViewModel-track log line's **message template** is a safe constant. The rule gap is confined to the pre-8.15 VMs passing the `Exception` payload (and 2 logging `SpecialistId`).

### C.3 No new exceptions introduced

Nothing committed by Team 3 from Phase 8.15 onward (`31f4b63` and later, 26 commits) passes an exception or an identifier to any logger. The 7 legacy VMs predate the rule and are all pre-8.15 committed code.

---

## D. LEGACY LOGGER FINDINGS

### D.1 `[LoggerMessage]` methods that still take an `Exception` parameter

| # | File | `[LoggerMessage]` method | Message template | Extra structured arg | Exception-passing call sites |
|---|---|---|---|---|---|
| 1 | `Presentation/…/Accounting/AccountingPageViewModel.cs:199` | `LogOperationFailed(ILogger logger, string operation, Exception exception)` (static) | `"Accounting operation failed. Operation={Operation}"` | — | 2 (`LoadAsync`, `SearchAsync`) |
| 2 | `Presentation/…/Accounting/PosCheckoutViewModel.cs:408` | `LogOperationFailed(string operation, Exception exception)` | `"POS checkout operation failed. Operation={Operation}"` | — | 3 (`LoadOptionsAsync`, `ProceedToPaymentAsync`, `ChargeAsync`) |
| 3 | `Presentation/…/Bookings/BookingPageViewModel.cs:510` | `LogOperationFailed(string operation, Exception exception)` | `"Booking operation failed. Operation={Operation}"` | — | 5 (`LoadAsync`, `CreateBookingAsync`, `ChangeStatusAsync`, `CancelSelectedBookingAsync`, `RescheduleSelectedBookingAsync`) |
| 4 | `Presentation/…/Calendar/CalendarPageViewModel.cs:311` | `LogLoadFailed(string operation, Exception exception)` | `"Calendar availability load failed. Operation={Operation}"` | — | 3 (Initialize / Daily / Weekly) |
| 5 | `Presentation/…/Dashboard/DashboardPageViewModel.cs:298` | `LogLoadFailed(Exception exception)` | `"Dashboard overview load failed."` **(no `{Operation}`)** | — | 1 |
| 6 | `Presentation/…/Specialists/SpecialistAvailabilityViewModel.cs:116` | `LogLoadFailed(string specialistId, Exception exception)` | `"Specialist availability load failed. SpecialistId={SpecialistId}"` | **`SpecialistId`** | 1 (`LoadAsync`) |
| 7 | `Presentation/…/Specialists/SpecialistScheduleViewModel.cs:484` | `LogOperationFailed(string specialistId, string operation, Exception exception)` | `"Specialist schedule operation failed. SpecialistId={SpecialistId} Operation={Operation}"` | **`SpecialistId`** | 2 (`LoadAsync`, generic op wrapper) |

Plus, in the same 2 Specialist files, **`LogPermissionDenied(string specialistId, string operation)`** (Warning, `SpecialistAvailability` implicit / `SpecialistSchedule` `:481`) — **no `Exception`** but **still logs `SpecialistId`** (3 call sites).

**Total Presentation-VM legacy surface:** 7 files, 7 exception-passing `[LoggerMessage]` methods (17 exception-passing call sites), + `SpecialistId` in 3 methods.

### D.2 Outside the ViewModel track (NOT part of any cleanup)

| File | Method | Why it is correct as-is |
|---|---|---|
| `Shell/App.xaml.cs:503` | `LogUnhandledException(ILogger logger, string source, Exception exception)` | **The global crash handler.** Capturing the full unhandled exception is its entire purpose — it is the last-resort forensic record before recovery/termination. Must keep the exception. |
| `Infrastructure/Api/HttpApiClient.cs:404` | `LogApiRequestFailed(string category, HttpMethod?, Uri?, int? statusCode, string exceptionType, Exception exception)` | Infrastructure-layer HTTP observability. Logs method / path / status / exception type. A separate Infrastructure decision (Phase 8.14 §A.3 flagged `AuthBootstrapHttpClient`'s *absence* of logging; `HttpApiClient` deliberately has it). Whether it should trim the payload is an Infra call, not a ViewModel-track item. |

### D.3 What actually reaches the log

`LocalFileLoggerProvider` renders a logged `Exception` as `exception.ToString()` into the daily-rotated file (14-day retention). For an `ApiException` (thrown by `HttpApiClient` / `AuthBootstrapHttpClient`), that string **embeds the raw backend response body**. So for the 7 legacy VMs, **backend response bodies for failed operations do reach the local log file today.** Plus `SpecialistId` (an opaque backend record id) for 2 of them.

### D.4 Classification

| Severity | Finding | Rationale |
|---|---|---|
| **P0 — security risk** | **NONE** | Local file only; never transmitted, indexed, or shared; 14-day rotation; developer/support-facing. No credentials, tokens, or session data in any template or reachable via the exception (the OTP token path is `MobileOtpLoginViewModel`, which is already compliant). |
| **P1 — should clean** | **NONE** (borderline — see P2) | The exposure (backend body / opaque id in a local rotated file) does not rise to "must fix before release". |
| **P2 — optional modernization** | **Harmonize the 7 legacy VMs to operation-name-only** — drop the `Exception` parameter (and `SpecialistId` argument) from each `[LoggerMessage]` + every call site; give `DashboardPageViewModel` an `{Operation}` token. Closes the last rule gap so the logging track is *rule-consistent*, not merely *functionally complete*. | Mechanical, low-risk, ~7 files / ~21 call sites. Not blocking. |
| **P2 — Infra decision (separate owner-track)** | `HttpApiClient.LogApiRequestFailed` payload trimming | Infrastructure layer, not ViewModel track. Defer to an Infra-observability review. |
| **Correct — no change** | `App.LogUnhandledException` | The crash handler must keep the exception. |

---

## E. HARDENING ROADMAP

### E.1 P0

**None.** No P0 anywhere in this codebase (re-confirmed — Phase 7.5 / 8.1 / 8.54 / this audit). Build clean, 2,609/2,609 tests, architecture 7/7, no security leak, no crash path.

### E.2 P1 — reliability (ranked)

| # | Item | Scope | Notes |
|---|---|---|---|
| P1.1 | **Missing-guard sweep** — ~17 async command methods across 7 instrumented VMs have **no `try`/`catch` at all**, so a backend/network failure propagates through `AsyncRelayCommand.Execute`'s bare `try/finally` to the app's global `DispatcherUnhandledException` dialog (recovered, never a crash) instead of this app's established in-page `ErrorMessage`/`State` (or `SaveErrorMessage`) pattern. Affected: `CustomerProfileViewModel` (`AddNoteAsync`/`AddTagAsync`/`RemoveTagAsync`), `ServiceProfileViewModel` (`AssignSpecialistAsync`/`UnassignSpecialistAsync`), `InventoryProfileViewModel` (`RecordTransactionAsync`/`MapServiceAsync`/`UnmapServiceAsync`), `EmployeeProfileViewModel` (`ActivateAsync`/`DeactivateAsync`/`SuspendAsync`), `SpecialistProfileViewModel` (`AddSkillAsync`/`RemoveSkillAsync`), `SpecialistPageViewModel` (`CreateSpecialistAsync`), `AccountingPageViewModel` (`CancelInvoiceAsync`). | Medium-large — 7 VMs, ~17 methods, each: add a broad catch in the established pattern + surface a user-facing message + append `LogOperationFailed(nameof(...))` + tests. Likely **2–3 waves**. Needs its own scope audit. |
| P1.2 | `AccountingPageViewModel.CancelInvoiceAsync` — no `try`/`catch` (a throw becomes an unobserved task exception caught by `App`). | Tiny — a subset of P1.1; can be folded in or done standalone. |
| P1.3 | **`CancellationToken` propagation** — most page reloads / searches ignore cancellation. Highest value: `CommandPaletteViewModel` (Search); then Booking filter-reload, Calendar navigation-reload. | Medium — design-heavier (thread a `CancellationTokenSource` per triggering property, cancel on re-trigger). Own audit. |
| P1.4 | **Startup UX** — no progress indicator across `App.OnStartup`'s 13 blocking initialization stages. | Medium — Shell + a startup progress VM. Own audit. |

### E.3 P2 — modernization / consistency

| # | Item | Scope |
|---|---|---|
| P2.1 | **Legacy `[LoggerMessage]` harmonization** (§D.4) — 7 Presentation VMs → operation-name-only. | Small-medium: ~7 files, ~21 call sites, 7 attribute edits + `DashboardPageViewModel` `{Operation}` token. Isolated commit `refactor(desktop): drop exception payload from diagnostic logging`. |
| P2.2 | `Security/LoginViewModel` — its `catch (ApiException)` fallthrough logs nothing (vs `MobileOtpLoginViewModel` at `Warning`). | Tiny — but the VM has **no view** and is unreachable. **Recommendation: document as intentional (retired implementation), instrument only if credentials login is re-activated.** |
| P2.3 | Cleanup — Calendar's 3 dead EF migration tables; `RolePermissions`' `CustomerEdit`/`ServiceEdit`/`SpecialistEdit` dead enum members. | Small, disclosed tech debt. |

### E.4 Blocked (upstream — not Team 3 actionable)

| Item | Blocker |
|---|---|
| Inventory backend integration | Backend has **zero Inventory code** (re-confirmed Phase 8.0). `FakeInventoryRepository`. Desktop prep complete. |
| HR backend integration | `FakeHrRepository`, legacy gates. Blocked on Backend/Team 1. |
| Accounting backend integration | `FakeAccountingRepository`. Also gated by the `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry correctness risk (backend payment-idempotency unverified from this codebase — documented, blocks Accounting's eventual connection). |

### E.5 Recommended sequence

1. **8.60 — P2.1 Legacy `[LoggerMessage]` harmonization** — small, mechanical, seals the just-closed logging track *rule-consistently* and resolves the one real (minor) exposure this audit found. **Doing it before P1.1 means the new guards P1.1 adds will use the already-uniform operation-name-only form (no mixed forms within a file).**
2. **8.62+ — P1.1 Missing-guard sweep** — the natural reliability continuation (same VMs, same pattern), split into 2–3 waves after its own audit.
3. Then P1.3 (CancellationToken) → P1.4 (Startup UX) → P2.3 (dead-code cleanup).

---

## F. NEXT PHASE RECOMMENDATION — **Phase 8.60: Legacy `[LoggerMessage]` Harmonization — Scope Review** (readiness only, no commit)

| Field | Detail |
|---|---|
| **Goal** | Bring the 7 pre-8.15 ViewModels to the operation-name-only logging rule so the (now functionally closed) logging track is rule-consistent: **no `[LoggerMessage]` in any ViewModel passes an `Exception` or a record identifier.** |
| **Scope — production (7 files)** | `AccountingPageViewModel.cs`, `PosCheckoutViewModel.cs`, `BookingPageViewModel.cs`, `CalendarPageViewModel.cs`, `DashboardPageViewModel.cs`, `SpecialistAvailabilityViewModel.cs`, `SpecialistScheduleViewModel.cs`. Per file: change each `[LoggerMessage]` method signature to drop `Exception exception` (and, for the 2 Specialist files, `string specialistId`); update every call site (`LogOperationFailed(nameof(X))` / `LogOperationFailed(_logger, nameof(X))` for the static Accounting form); add `Operation={Operation}` to `DashboardPageViewModel`'s message. **Keep** each method's form otherwise (instance vs static), `EventId`, `Level` (incl. `SpecialistSchedule`'s `Warning` permission-denied variant — drop only its `SpecialistId` arg). |
| **Scope — tests** | The 7 corresponding `*ViewModelTests.cs` files. Existing failure tests assert `State`/`ErrorMessage`/`SaveErrorMessage` behaviour (unaffected) and, where present, `Assert.Contains("Operation=X", entry.Message)` (unaffected). Update any assertion that expects the exception or `SpecialistId` in the log line to assert its **absence** instead. Add a `DoesNotContain(exception-secret)` / `DoesNotContain(specialistId)` assertion to at least one failure test per file. Reuse `RecordingLogger<T>`. **No shared-stub change.** |
| **NOT touched** | `App.xaml.cs LogUnhandledException` (crash handler — must keep the exception), `HttpApiClient.LogApiRequestFailed` (Infra decision), the 24 already-compliant VMs, DI, Domain, backend contracts, RBAC, auth, navigation, interfaces, DTOs. |
| **Risk** | **LOW-MEDIUM.** Mechanical signature+call-site edits across 7 committed files (touched by ~5 prior commits). No behaviour change — only what reaches the logger. Build risk: none (removing params from a `[LoggerMessage]` partial cannot introduce `SYSLIB1020`; watch for `CA1848`/unused-`exception`-variable warnings at the now-simplified catch sites — the `exception` local is still used for `ErrorMessage = exception.Message` in the instance-form VMs, so it stays referenced). |
| **Test requirements** | Full suite green (~2,609, likely ±0 net — assertion edits, maybe +7 explicit no-leak assertions folded into existing tests or +7 new). Architecture 7/7. Build 0 warnings / 0 errors. |
| **Commit** | One isolated commit — `refactor(desktop): drop exception payload from diagnostic logging`. Standard rhythm: 8.60 scope review → 8.61 implementation (STOP before commit) → 8.62 commit scope review → 8.63 commit execution → checkpoint update. |
| **Alternative for 8.60** | If the authorizer would rather start the **P1.1 missing-guard sweep** first (higher reliability value, larger), begin with its scope audit instead; harmonization then slots in afterward (more call sites to touch, but still mechanical). Recommendation stands: **harmonization first** (smaller, seals the track, avoids mixed forms). |

---

## STOP

Audit complete. Logging closure **verified** (33/55 self-logging, 22/55 correctly uninstrumented, 1 deliberate skip, 0 uninstrumented swallowing catches). No P0. The one real finding is a **P2** (7 pre-8.15 VMs still pass the `Exception` payload — backend bodies reach the local rotated log). No source or test change, no commit/push/merge/rebase/amend. HEAD remains `6a1bced`. **Awaiting Phase 8.60 authorization** (recommended: Legacy `[LoggerMessage]` Harmonization — Scope Review).
