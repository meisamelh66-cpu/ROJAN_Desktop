# ROJAN AI — TEAM 3 — PHASE 8.60 — LEGACY `[LoggerMessage]` HARMONIZATION — SCOPE REVIEW v1

**Type:** Readiness review only. **No source change. No test change. No logger / DI change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `6a1bced659ae129da48d2453c5636868c1455701` — `fix(desktop): add ViewModel diagnostic logging (specialist page)` (Phase 8.56, committed 8.58)
**Reference:** `ROJAN_PHASE8_59_FINAL_LOGGING_CLOSURE_AUDIT_v1.md` §D (the P2 finding this phase closes), `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F.
**Verdict:** ✅ **READY TO IMPLEMENT.** LOW-MEDIUM risk, purely mechanical, no behaviour change.

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

Working tree clean. This review adds no code.

---

## B. LEGACY INVENTORY

All 7 are `sealed partial`, Presentation-layer page/child ViewModels. All 7 already build clean; none is on the critical path.

| # | ViewModel | `[LoggerMessage]` method(s) — current | Form | `Exception` param? | Extra structured arg | Call sites | `ILogger` **fields** | `_loggerFactory`? |
|---|---|---|---|---|---|---|---|---|
| 1 | `Accounting/AccountingPageViewModel` (`:198`) | `LogOperationFailed(ILogger logger, string operation, Exception exception)` — `"Accounting operation failed. Operation={Operation}"` | **static** | yes | — | **2** — `LoadAsync` (`:157`), `SearchAsync` (`:190`) | **2** (`_posCheckoutLogger`, `_logger`) | yes |
| 2 | `Accounting/PosCheckoutViewModel` (`:407`) | `LogOperationFailed(string operation, Exception exception)` — `"POS checkout operation failed. Operation={Operation}"` | instance | yes | — | **3** — `LoadOptionsAsync` (`:279`), `ProceedToPaymentAsync` (`:375`), `ChargeAsync` (`:403`) | **1** (`_logger`) | no |
| 3 | `Bookings/BookingPageViewModel` (`:509`) | `LogOperationFailed(string operation, Exception exception)` — `"Booking operation failed. Operation={Operation}"` | instance | yes | — | **5** — `LoadAsync` (`:359`), `CreateBookingAsync` (`:404`), `ChangeStatusAsync` (`:435`), `CancelSelectedBookingAsync` (`:469`), `RescheduleSelectedBookingAsync` (`:505`) | **1** (`_logger`) | yes |
| 4 | `Calendar/CalendarPageViewModel` (`:310`) | `LogLoadFailed(string operation, Exception exception)` — `"Calendar availability load failed. Operation={Operation}"` | instance | yes | — | **3** — `InitializeAsync` (`:222`), `LoadDailyAvailabilityAsync` (`:266`), `LoadWeeklyAvailabilityAsync` (`:306`) | **1** (`_logger`) | no |
| 5 | `Dashboard/DashboardPageViewModel` (`:297`) | `LogLoadFailed(Exception exception)` — `"Dashboard overview load failed."` **(no `{Operation}` token)** | instance | yes | — | **1** — `LoadAsync` (`:293`) | **1** (`_logger`) | no |
| 6 | `Specialists/SpecialistAvailabilityViewModel` (`:115`) | `LogLoadFailed(string specialistId, Exception exception)` — `"Specialist availability load failed. SpecialistId={SpecialistId}"` | instance | yes | **`SpecialistId`** | **1** — `LoadAsync` (`:111`) | **1** (`_logger`) | no |
| 7 | `Specialists/SpecialistScheduleViewModel` (`:480`, `:483`) | `LogPermissionDenied(string specialistId, string operation)` — `"…permission denied. SpecialistId={SpecialistId} Operation={Operation}"` (Warning, **no `Exception`**) · `LogOperationFailed(string specialistId, string operation, Exception exception)` — `"…operation failed. SpecialistId={SpecialistId} Operation={Operation}"` (Error) | instance ×2 | Error one: yes | **`SpecialistId`** (both) | **4** — in `LoadAsync` (`:278` denied, `:286` failed) and `TryMutateAsync` (`:467` denied, `:475` failed) — `TryMutateAsync`'s `operationName` is `[CallerMemberName]`-supplied, so its 8 command-method callers are **not** touched | **1** (`_logger`) | no |

**Totals:** 7 files · 8 `[LoggerMessage]` methods · **19 call sites** · every catch body already assigns `ErrorMessage = exception.Message` (or a `Strings.*` constant) **before** the log call, so the `exception` local stays referenced after the log arg is dropped — **no unused-variable / CA-warning risk**.

### B.1 Dependencies / risk per VM

| VM | Dependency / behaviour note | Risk |
|---|---|---|
| `AccountingPageViewModel` | Static form is **required** (2 `ILogger` fields). Also holds `_loggerFactory` (Phase 8.51, → `InvoiceProfileViewModel`) — untouched. | LOW — drop 1 param + 2 call args. |
| `PosCheckoutViewModel` | `ChargeAsync` has the disclosed double-charge-on-retry P2 (correctness, unrelated). Logging change does not touch that logic. | LOW |
| `BookingPageViewModel` | Holds `_loggerFactory` (Phase 8.51/2C-3b, → `BookingWizardViewModel`) — untouched. 5 command paths (`da18c18` hardening) each keep their `ErrorMessage`/`State` behaviour. | LOW |
| `CalendarPageViewModel` | Wave 1 VM; 3 load paths. Pure read/API layer (no local authority). | LOW |
| `DashboardPageViewModel` | Message gains an `{Operation}` token it never had → the ONE genuine template change (all others keep their template, just drop the arg). | LOW |
| `SpecialistAvailabilityViewModel` | `_specialistId` stays a field (used by the query). Message swaps `SpecialistId={SpecialistId}` → `Operation={Operation}`. | LOW — 1 breaking test assertion (§F). |
| `SpecialistScheduleViewModel` | Grandchild of `SpecialistProfileViewModel` (constructed with `scheduleLogger`). `_specialistId` stays a field. Both message templates drop `SpecialistId={SpecialistId}`. `LogPermissionDenied` (Warning, no exception) also loses `specialistId`. | LOW-MEDIUM — 2 `[LoggerMessage]` + 4 call sites in one file; 1 breaking test assertion (§F). |

---

## C. SECURITY FINDINGS

### C.1 What reaches the log today (the 7 legacy VMs)

| Channel | Present? | Detail |
|---|---|---|
| `Exception` object → `exception.ToString()` written by `LocalFileLoggerProvider` | **YES** (all 7 Error methods) | For an `ApiException` (from `HttpApiClient` / `AuthBootstrapHttpClient`) the string **embeds the raw backend response body** for the failed operation |
| `Exception.Message` (as the message-template text) | **NO** | Every message template is a safe constant — the exception is a structured *argument*, not interpolated into `{…}` |
| Record identifiers | **YES** (2 VMs) | `SpecialistId` — an opaque backend record id (e.g. `"specialist-1"`) — in `SpecialistAvailabilityViewModel` and `SpecialistScheduleViewModel` (3 methods) |
| PII (name / phone / email / etc.) | **NO** | No template or call site references a `Dto` field |
| Financial payload (amounts / prices / payments) | **NO** | Not in any template; only reachable inside `ApiException.Message` (i.e. via the exception channel above) |
| Tokens (bearer / session / OTP / invite) | **NO** | Not held by any of these 7 VMs (the OTP path is `MobileOtpLoginViewModel`, already compliant) |

### C.2 Severity — unchanged from the Phase 8.59 audit

| Severity | Verdict |
|---|---|
| **P0 — security risk** | **NONE.** Exposure is **local-only** — `LocalFileLoggerProvider` writes a daily-rotated file with 14-day retention on the operator's own machine; nothing is transmitted, indexed, uploaded, or shared. No credentials/tokens. |
| **P1** | **NONE.** A backend response body / opaque id in a local rotated dev log does not gate a release. |
| **P2 — optional modernization** | **This phase.** Bring the 7 to operation-name-only so the (functionally closed) logging track is *rule-consistent*: no `[LoggerMessage]` in any ViewModel passes an `Exception` or a record identifier. |

### C.3 Explicitly OUT of scope (correct as-is / different owner)

| File | Why kept |
|---|---|
| `Shell/App.xaml.cs:503` `LogUnhandledException(ILogger, string source, Exception exception)` | The global crash handler — capturing the full unhandled fault is its purpose. **Keeps the exception.** |
| `Infrastructure/Api/HttpApiClient.cs:404` `LogApiRequestFailed(…, Exception exception)` | Infrastructure-layer HTTP observability (method / path / status / exception type). A separate Infra-observability decision — not the ViewModel track. |

---

## D. ARCHITECTURE STRATEGY

`SYSLIB1020` fires only on **multiple `ILogger` fields + an instance-form `[LoggerMessage]`**. **Removing parameters from a `[LoggerMessage]` partial can never introduce it** — no `ILogger` field is added anywhere in this phase. So each VM keeps its current form:

| VM | `ILogger` fields | Current form | **Target form** | Signature change |
|---|---|---|---|---|
| `AccountingPageViewModel` | 2 | static | **static (unchanged)** — required by the 2-field constraint | `(ILogger logger, string operation, Exception exception)` → `(ILogger logger, string operation)` |
| `PosCheckoutViewModel` | 1 | instance | **instance (unchanged)** | `(string operation, Exception exception)` → `(string operation)` |
| `BookingPageViewModel` | 1 | instance | **instance (unchanged)** | `(string operation, Exception exception)` → `(string operation)` |
| `CalendarPageViewModel` | 1 | instance | **instance (unchanged)** | `(string operation, Exception exception)` → `(string operation)` |
| `DashboardPageViewModel` | 1 | instance | **instance (unchanged)** | `(Exception exception)` → `(string operation)` **+ message gains `Operation={Operation}`** |
| `SpecialistAvailabilityViewModel` | 1 | instance | **instance (unchanged)** | `(string specialistId, Exception exception)` → `(string operation)` **+ message: `SpecialistId={SpecialistId}` → `Operation={Operation}`** |
| `SpecialistScheduleViewModel` | 1 | instance ×2 | **instance ×2 (unchanged)** | `LogPermissionDenied(string specialistId, string operation)` → `(string operation)`; `LogOperationFailed(string specialistId, string operation, Exception exception)` → `(string operation)` **+ both messages drop `SpecialistId={SpecialistId}`** |

- **No VM needs `ILoggerFactory`** — none needs a *new* logger, only a slimmer `[LoggerMessage]` signature.
- **No VM needs to change form** — Accounting stays static (its precedent reason still holds), the other 6 stay instance-form.
- `EventId` / `Level` / instance-vs-static / `[CallerMemberName]` on `TryMutateAsync` — all preserved. Only the `Exception` parameter (and `SpecialistId`) is removed.
- Committed-pattern alignment: the target is the **exact** shape used by every Wave-2A→2D VM (`LogOperationFailed(string operation)`).

---

## E. COMMIT PLAN

### E.1 Recommendation — **Option 1: one harmonization commit**

```
refactor(desktop): drop exception payload from diagnostic logging
```

**7 production files + ~7 test files, ~19 call-site edits + 8 attribute edits + 2 breaking-assertion fixes + ~7 no-leak assertions.**

### E.2 Option 1 vs Option 2

| | Option 1 — one commit | Option 2 — split by domain (Accounting / Booking / Calendar / Dashboard / Specialist = 5 commits) |
|---|---|---|
| Review surface | One diff, one security narrative ("stop passing the exception; drop `SpecialistId`"). Every hunk is the identical mechanical shape. | 5 diffs, 5 scope-review → commit cycles. Each hunk still trivially reviewable. |
| Risk of a bad merge / partial state | Nil — the change is atomic and behaviour-neutral (only what reaches the logger changes). | A split leaves the tree in a "half-harmonized" state between commits — harmless but pointless. |
| Bisect granularity | One revert reverts everything. | Finer — but there is no plausible regression to bisect for (no behaviour change). |
| Precedent | Wave 2A bundled 5 VMs; Wave 2C-3c bundled 6 VMs + 3 parents. A single conceptual refactor across 7 files fits the same mold. | — |
| **Verdict** | **RECOMMENDED** — smallest process cost, atomic, one clean security story. | Not recommended — 4 extra review/commit cycles for zero risk reduction. |

**One reviewer note within the single commit:** the `SpecialistAvailabilityViewModel` / `SpecialistScheduleViewModel` hunks do slightly more than "drop the exception" — they also drop the `SpecialistId` token/arg. Call it out in the commit body so the reviewer looks there closely.

### E.3 Staging

`git reset` → explicit `git add <path>` for the 7 production + the touched test files (final list in §G). Never `git add .` / `-A`.

---

## F. TEST PLAN

### F.1 Existing assertions — impact

| Test | Assertion | Impact |
|---|---|---|
| `SpecialistAvailabilityViewModelTests:85` | `entry.Message.Contains("specialist-1", …)` | **BREAKS** — message no longer contains the id. → change to `entry.Message.Contains("LoadAsync")` / `"Operation=LoadAsync"`. |
| `SpecialistScheduleViewModelTests:257` | `entry.Message.Contains("specialist-1", …)` | **BREAKS** — same. → change to assert `"Operation="` / the method name. |
| `AccountingPageViewModelTests` :50/:119/:136 | `Operation=LoadAsync` / `Message.Contains("LoadAsync"/"SearchAsync")` | ✅ green — templates keep `Operation={Operation}` |
| `CalendarPageViewModelTests` :116/:131/:150 | `Message.Contains("InitializeAsync"/"LoadDaily…"/"LoadWeekly…")` | ✅ green — template keeps `Operation={Operation}` |
| `PosCheckoutViewModelTests` :234/:250/:264 · `BookingPageViewModelTests` :592/:608/:624/:643 · `DashboardPageViewModelTests` :85 | only `entry.Level == LogLevel.Error` | ✅ green |
| `SpecialistScheduleViewModelTests` :273/:274/:293 | Warning present / Error absent / Warning message contains the command-derived operation name | ✅ green — `Operation={Operation}` retained |

**Only 2 assertions break.** Both are `Contains("specialist-1")` → must become the operation name.

### F.2 New / strengthened assertions (the security guarantee)

Add to **one failure test per legacy VM** (7 total — fold into existing tests, or add 7 tiny dedicated `…_NoExceptionOrIdentifierLeak` tests):
- seed a recognizable secret into the thrown exception (e.g. `"backend body / specialist-1"`), then
- `Assert.DoesNotContain(secret, entry.Message)` and (for the 2 Specialist VMs) `Assert.DoesNotContain("specialist-1", entry.Message)`
- keep / add `Assert.Contains("Operation=<method>", entry.Message)`

Optional: strengthen `DashboardPageViewModelTests:85` to also assert `Operation=LoadAsync` now that the template carries it.

### F.3 NullLogger safety

Unaffected — no ctor/field/DI change. Existing "no logger" tests (where present) keep passing.

### F.4 Estimate

| | Count |
|---|---|
| Production files | **7** |
| Test files | **~7** (`Accounting/AccountingPageViewModelTests`, `Accounting/PosCheckoutViewModelTests`, `Bookings/BookingPageViewModelTests`, `Calendar/CalendarPageViewModelTests`, `Dashboard/DashboardPageViewModelTests`, `Specialists/SpecialistAvailabilityViewModelTests`, `Specialists/SpecialistScheduleViewModelTests`) |
| Breaking-assertion fixes | 2 |
| New/strengthened no-leak assertions | ~7 (folded in) or ~7 tiny new tests |
| Net test-count delta | **~0 to +7** (2,609 → 2,609–2,616) |
| New test helper / shared-stub change | **none** — `RecordingLogger<T>` / `RecordingLoggerFactory` reused |

---

## G. PHASE 8.61 RECOMMENDATION — **Legacy `[LoggerMessage]` Harmonization — Implementation**

### G.1 Exact files

**Production (7):**
```
src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Bookings/BookingPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Calendar/CalendarPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs
```

**Tests (7):**
```
tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Accounting/PosCheckoutViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Bookings/BookingPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Calendar/CalendarPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Dashboard/DashboardPageViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistAvailabilityViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs
```

### G.2 Per-file change

| File | `[LoggerMessage]` edit | Call-site edits |
|---|---|---|
| `AccountingPageViewModel` | `(ILogger logger, string operation, Exception exception)` → `(ILogger logger, string operation)` | 2 — drop `, exception` |
| `PosCheckoutViewModel` | `(string operation, Exception exception)` → `(string operation)` | 3 — drop `, exception` |
| `BookingPageViewModel` | `(string operation, Exception exception)` → `(string operation)` | 5 — drop `, exception` |
| `CalendarPageViewModel` | `(string operation, Exception exception)` → `(string operation)` | 3 — drop `, exception` |
| `DashboardPageViewModel` | `(Exception exception)` → `(string operation)`; message `"Dashboard overview load failed."` → `"Dashboard overview load failed. Operation={Operation}"` | 1 — `LogLoadFailed(exception)` → `LogLoadFailed(nameof(LoadAsync))` |
| `SpecialistAvailabilityViewModel` | `(string specialistId, Exception exception)` → `(string operation)`; message `"… SpecialistId={SpecialistId}"` → `"… Operation={Operation}"` | 1 — `LogLoadFailed(_specialistId, exception)` → `LogLoadFailed(nameof(LoadAsync))` |
| `SpecialistScheduleViewModel` | `LogPermissionDenied(string specialistId, string operation)` → `(string operation)`; `LogOperationFailed(string specialistId, string operation, Exception exception)` → `(string operation)`; both messages drop `SpecialistId={SpecialistId} ` prefix | 4 — drop `_specialistId,` (and `, exception` on the 2 Error calls) |

Every catch keeps its unchanged `ErrorMessage = exception.Message;` / `State = …;` / `IsPermissionDenied = …;` — the log call is the only thing edited. `_specialistId` / `_loggerFactory` remain live fields (used elsewhere).

### G.3 Commit strategy

**One isolated commit** — `refactor(desktop): drop exception payload from diagnostic logging`. Body notes: (a) brings the 7 pre-8.15 VMs to the operation-name-only rule (no `[LoggerMessage]` in any ViewModel now passes an `Exception` or a record id); (b) the 2 Specialist files additionally drop the `SpecialistId` token; (c) `App.LogUnhandledException` and `HttpApiClient` intentionally keep their exception (crash handler / Infra). Trailers `Co-Authored-By` + `Claude-Session`. Explicit-path staging. No push/merge/rebase/amend.

Standard rhythm: **8.61 implementation (STOP before commit) → 8.62 commit scope review → 8.63 commit execution → checkpoint update → the logging track is rule-consistent (fully closed).**

### G.4 Risk

**LOW-MEDIUM.** Mechanical signature + call-arg removal across 7 committed files (touched by ~5 prior commits). **No behaviour change** — `ErrorMessage`/`State`/`IsPermissionDenied` and every user-facing string are untouched; only what reaches the logger changes. No `SYSLIB1020` (no `ILogger` field added). No unused-variable warning (`exception` stays referenced for `ErrorMessage`). The one non-mechanical bit is `DashboardPageViewModel`'s message gaining an `{Operation}` token — additive, safe.

### G.5 Validation expectations

```
dotnet build -c Debug   → 0 warnings / 0 errors   (no SYSLIB1020; no CA1848; no CS0168/unused-variable)
dotnet test  -c Debug   → 2,609 → 2,609–2,616 / all pass  (2 assertion fixes + ~7 no-leak assertions)
architecture tests      → 7 / 7
```

---

## STOP

Scope review complete. 7 legacy VMs · 8 `[LoggerMessage]` methods · 19 call sites · no `SYSLIB1020` / form change · no behaviour change. Recommend **one commit**. No P0 — the underlying exposure is local-only; this phase is P2 rule-consistency. No source or test change, no commit/push/merge/rebase/amend. HEAD remains `6a1bced`. **Awaiting Phase 8.61 implementation authorization.**
