# ROJAN AI — TEAM 3 — PHASE 8.102 — P2 HARDENING: SANITIZE LOAD-ERROR SURFACING — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / localization / service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `0260bc3` (`fix(desktop): guard settings page command failures`)
**Reference:** `ROJAN_PHASE8_101_SETTINGS_COMMIT_REPORT_v1.md` §I item 2, `ROJAN_PHASE8_65_*` §D, `ROJAN_PHASE8_81_*` §D.2, `ROJAN_PHASE8_89_*` §D.2
**Recommendation: implement as ~6 domain sub-waves (priority-ordered by content sensitivity), each its own audit → review → implement → commit cycle. LOW risk per sub-wave; purely a UI-string swap — no behaviour, logging, service, DI or localization change.**

---

## A. GIT STATE

```
git rev-parse HEAD        → 0260bc38aabdb51af32e40bc90d22d00504e5211
git branch --show-current → feature/team3-desktop-completion
git status (tracked)      → clean
git diff --cached         → (empty)
```

Untracked: only `ROJAN_*.md`. Baseline (checkpoint §E, `0260bc3`): **2,710 / 2,710** — Domain 456, Presentation 767, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. ERROR-SURFACE INVENTORY

### B.0 The pattern

Every affected site is the **UI half** of a top-level broad-catch boundary:

```csharp
#pragma warning disable CA1031 // Top-level load/command boundary: any failure must surface as ..., not crash the page.
catch (Exception exception)
#pragma warning restore CA1031
{
    <Surface> = exception.Message;          // ← the leak (this audit's target)
    State = DashboardState.Error;           // load boundaries only — intentional, keep
    LogOperationFailed(nameof(<Method>));   // already operation-name-only — keep, unchanged
}
```

The **log** side is already safe everywhere — every one of these methods calls an operation-name-only source-generated `LogOperationFailed(string)` / `LogLoadFailed(string)` that never receives the exception (legacy harmonization `5ba554c`; per-VM `[LoggerMessage]` doc comments explicitly say "never the exception, its message, or any backend response detail"). **Only the `<Surface> = exception.Message` UI assignment contradicts that discipline** — the bound `TextBlock` can show a raw `HttpRequestException` / `JsonException` / backend `ApiException` body / SQL text / file path / cancelled-task message to the end user.

### B.1 Full inventory — **60 sites across 32 ViewModels** (all in `Rojan.Desktop.Presentation/ViewModels`; **no Shell surfaces**)

| Domain / VM | Sites | Methods (surface) |
|---|---|---|
| **Reporting** `ReportingPageViewModel` | 3 | `LoadAsync` (`ErrorMessage`), `RunReportAsync` (`StatusMessage`), `RerunSnapshotAsync` (`StatusMessage`) |
| **AI Center** `AiCenterPageViewModel` | 2 | `LoadAsync` (`ErrorMessage`), `SendMessageAsync` (`StatusMessage`) |
| **Accounting** `AccountingPageViewModel` | 2 | `LoadInvoicesAsync` / secondary load (`ErrorMessage` ×2) |
| `PosCheckoutViewModel` | 3 | `LoadOptionsAsync`, `ProceedToPaymentAsync`, **`ChargeAsync`** (`ErrorMessage` ×3) |
| `InvoiceProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Customers** `CustomerPageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| `CustomerProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **HR** `HrPageViewModel` | 2 | `LoadAsync` + secondary (`ErrorMessage` ×2) |
| `EmployeeProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Membership** `AcceptInviteViewModel` | 2 | `LookupAsync` (`LookupErrorMessage`), `AcceptAsync` (`AcceptErrorMessage`) — **token / identity context** |
| **Organization** `OrganizationPageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Specialists** `SpecialistPageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| `SpecialistProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| `SpecialistScheduleViewModel` | 2 | `LoadAsync` + secondary (`ErrorMessage` ×2) |
| `SpecialistAvailabilityViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Services** `ServicePageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| `ServiceProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Automation** `WorkflowsTabViewModel` | 5 | `LoadAsync`, `CreateDraftAsync`, `PublishAsync`, `RunNowAsync`, `RollbackAsync` (`ErrorMessage` ×5) |
| `ScheduledJobsTabViewModel` | 3 | `LoadAsync`, `CreateAsync`, `RunNowAsync` (`ErrorMessage` ×3) |
| `BusinessRulesTabViewModel` | 2 | `LoadAsync`, `CreateAsync` (`ErrorMessage` ×2) |
| `ApprovalsTabViewModel` | 2 | `LoadAsync`, `DecideAsync` (`ErrorMessage` ×2) |
| `AutomationDashboardTabViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Booking** `BookingPageViewModel` | 5 | `LoadAsync` + 4 command boundaries (`ErrorMessage` ×5) |
| **Calendar** `CalendarPageViewModel` | 3 | `InitializeAsync`, `LoadDailyAvailabilityAsync`, `LoadWeeklyAvailabilityAsync` (`ErrorMessage` ×3) |
| **Inventory** `InventoryPageViewModel` | 2 | `LoadAsync` + secondary (`ErrorMessage` ×2) |
| `InventoryProfileViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Dashboard** `DashboardPageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Analytics** `AnalyticsPageViewModel` | 1 | `LoadAsync` (`ErrorMessage`) |
| **Salon** `SalonPageViewModel` | 2 | `LoadAsync` (`ErrorMessage`), `CreateSalonAsync` (`CreateErrorMessage`) |
| **QR Codes** `QrCodesPageViewModel` | 2 | `LoadAsync` (`ErrorMessage`), `GenerateReceptionInviteAsync` (`GenerateInviteErrorMessage`) |
| **Support** `SupportPageViewModel` | 2 | `SubmitMessageAsync` (`MessageError`), `SubmitApplicationAsync` (`ApplicationError`) |
| **Settings** `SettingsPageViewModel` | 2 | `DownloadOrInstallAsync`, `RemovePackAsync` — **`catch (NotSupportedException exception) { StatusMessage = exception.Message; }`** (see §C Category D) |

**Grep basis:** `^\s+(ErrorMessage|StatusMessage|CreateErrorMessage|LookupErrorMessage|AcceptErrorMessage|GenerateInviteErrorMessage|MessageError|ApplicationError|…)\s*=\s*exception\.Message;` → 60 hits / 32 files. Doc-comment `<see cref="Exception.Message"/>` hits and `notification.Message` / `result.Message` (DTO fields, not exceptions) excluded.

### B.2 Not in scope — already correct

| VM | Why it is fine |
|---|---|
| `LoginViewModel`, `MobileOtpLoginViewModel` | catch **typed** `ApiException` / `ApiAuthenticationException` / `ApiRateLimitException` and map `StatusCode` → **localized `Strings.Login_*` constants**; never `exception.Message`. This is the model of "done right". |
| Every Missing-Guard Sweep guard (Waves A–F + Settings carve-out) | `catch (Exception)` **no variable** → `Strings.Common_ActionFailedMessage` |
| `ReportingPageViewModel` `catch (OperationCanceledException) → Strings.Reporting_RunCancelled` | intentional cancellation copy, not an exception-message surface |
| `ExportDialogViewModel` `StatusMessage … : result.Message` | `result.Message` is an `IReportExportService` DTO field (curated), not an exception |
| Toast / notification row `Message => Notification.Message` | domain notification content, not an error surface |

---

## C. CLASSIFICATION

| Category | Definition | Members | Action |
|---|---|---|---|
| **A — user-visible sensitive leak** | bare `catch (Exception exception)` → `<Surface> = exception.Message` reaches a bound `TextBlock` | **58 sites / 30 VMs** (the B.1 table minus the 2 `SettingsPageViewModel` `NotSupportedException` sites) | **sanitize — the P2 work** |
| **B — internal-only logging** | exception reaches only a logger | **none** — every affected method's `LogOperationFailed` / `LogLoadFailed` is already operation-name-only | n/a |
| **C — already sanitized** | typed catch → localized constant, or no-variable catch → `Common_ActionFailedMessage` | `LoginViewModel`, `MobileOtpLoginViewModel`, all Wave A–F guards, `Reporting` cancellation branch | **do not touch** |
| **D — intentional technical message** | the message is a fixed, non-sensitive, author-written string | `SettingsPageViewModel` ×2 — `catch (NotSupportedException exception) { StatusMessage = exception.Message; }` where the message is the static Phase-19A string *"… not available yet — Phase 19A ships the framework only."* | **low priority** — no leak risk today, but recommend swapping to a localized "coming soon" constant for consistency; can be a 2-line addendum to whichever sub-wave touches Settings, or skipped |

**All 58 Category-A sites are the same shape** and the same fix. None is a partial/typed catch — every one catches bare `Exception`, so even where the happy-path failure is a curated `ApiException`, the same clause also surfaces `HttpRequestException` / `JsonException` / `IOException` / `TaskCanceledException` raw.

---

## D. SECURITY

What a Category-A `TextBlock` can currently display to an end user, by domain:

| Exposure | Where it can surface |
|---|---|
| **Backend exception bodies / stack-y messages** | every site — `ApiException.Message` often carries the server's error string; `HttpRequestException` carries connection detail |
| **Internal URLs / host names** | `HttpRequestException` (`"No such host is known (api.internal.rojan:8443)"`), `SocketException` |
| **File paths** | `IOException` / `UnauthorizedAccessException` from any local-store fallback (`LocalWorkspaceStore`, settings, QR image write) → `"Access to the path 'C:\Users\…\AppData\…' is denied"` |
| **Database errors** | if a backend 500 echoes an EF/SQL fragment, it rides through `ApiException.Message` |
| **Customer / PII data** | `CustomerProfileViewModel`, `EmployeeProfileViewModel`, `HrPageViewModel`, `AcceptInviteViewModel` (invite token, invitee email, salon id/role), `SupportPageViewModel` |
| **Revenue / financial data** | `ReportingPageViewModel` (report rows / filters in a validation message), `AnalyticsPageViewModel`, `AccountingPageViewModel`, `InvoiceProfileViewModel`, **`PosCheckoutViewModel.ChargeAsync`** (payment-gateway / processor error text) |
| **AI responses / prompts** | `AiCenterPageViewModel.SendMessageAsync` (`StatusMessage = exception.Message` — a model/provider error may quote the prompt or a partial completion) |
| **Automation content** | `WorkflowsTabViewModel` / `BusinessRulesTabViewModel` / `ScheduledJobsTabViewModel` — a rule/step/cron validation message |

### Required sanitization pattern (identical to the Missing-Guard Sweep)

```csharp
// before
catch (Exception exception)
{
    ErrorMessage = exception.Message;
    State = DashboardState.Error;              // load boundary — UNCHANGED
    LogOperationFailed(nameof(LoadAsync));     // UNCHANGED
}

// after
catch (Exception)                             // drop the variable → leak structurally impossible
{
    ErrorMessage = Strings.Common_ActionFailedMessage;   // fixed localized constant
    State = DashboardState.Error;              // UNCHANGED
    LogOperationFailed(nameof(LoadAsync));     // UNCHANGED
}
```

- **Drop the `exception` variable** from the `catch` clause (matches the Wave A–F no-variable idiom — makes `.Message` / `.ToString()` / inner unreachable).
- **Keep `State = DashboardState.Error`** on load boundaries — that "replace the page with an error + retry view" behaviour is deliberate and orthogonal to the message content. **No business-behaviour change.**
- **Keep the `LogOperationFailed` / `LogLoadFailed` call** exactly.
- **Keep** the `catch (OperationCanceledException) → <cancelled copy>` branches where they exist (Reporting).
- Where the surface is a `catch (…) when (exception is not OperationCanceledException)` filtered clause (the 13 Automation-tab pre-8.39 guards), keep the filter, drop the variable, swap the string.

---

## E. ARCHITECTURE

| Question | Answer |
|---|---|
| **Reuse existing `[LoggerMessage]`?** | **Yes — no logging change at all.** Every one of the 32 VMs already has an `ILogger<T>` field + an operation-name-only source-generated `LogOperationFailed` / `LogLoadFailed` invoked in the same catch. This audit's change never touches the logger. |
| **New localization?** | **Not required.** `Strings.Common_ActionFailedMessage` exists in all 3 locale files (`Strings.resx` / `.en` / `.ar`, Wave A `794648e`) and reads correctly for both load and action contexts (fa: *"انجام این عملیات ممکن نشد. لطفاً دوباره تلاش کنید."*). **Optional nicety:** add **one** `Common_LoadFailedMessage` ("Couldn't load this content. Please try again.") for load-state boundaries and keep `Common_ActionFailedMessage` for the command/create sites — 1 key × 3 `.resx`. Impl-phase decision; the audit's STRICT MODE forbids adding it here. |
| **DI impact?** | **None.** No ctor change, no field added, no registration touched. |
| **Service / contract impact?** | **None.** |
| **Test additions?** | No new test **files**. Each affected VM has a `LoadAsync_Failure_*` / `*_Failure_*` test; the assertions that check the surface value need updating. Two kinds:<br>• already `Assert.DoesNotContain(<secret>, sut.ErrorMessage)` → stays green (often *becomes* a real guarantee).<br>• `Assert.Equal(<thrown message>, sut.ErrorMessage)` or `Assert.Contains(<fragment>, …)` → change to `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)`.<br>Estimate **~50–80 assertion edits across ~32 test files**; optionally add an explicit `== Common_ActionFailedMessage` assertion where the failure test currently only checks `State == Error`. |
| **`SYSLIB1020` / partial-class?** | Not relevant — no `[LoggerMessage]` added; the classes are already `partial` where they have a generated logger. |
| **`CA1031`** | The `#pragma warning disable CA1031` boundary comments stay; dropping the exception variable does not affect them. |

---

## F. RECOMMENDATION

### This is P2 hardening, fully separate from the (completed) Missing-Guard Sweep

- The **Missing-Guard Sweep** (Waves A–F + Settings carve-out, `794648e` … `0260bc3`) added *new* `try`/`catch` around *previously unguarded* commands so failures stop hitting `App.DispatcherUnhandledException`. **Complete.**
- **This P2** changes the *message string* in *already-existing* catches from `exception.Message` to a generic constant. No new guard, no new boundary, no behaviour change — the pages already recover; they just currently recover with an unsafe message.

### Split into ~6 domain sub-waves, priority-ordered by content sensitivity

Each sub-wave: one audit → one commit-scope-review → one commit, explicit-path staging, `fix(desktop): sanitize <domain> error surfacing` (or fold the whole thing under `fix(desktop): sanitize load-error surfacing` per sub-wave suffix).

| # | Sub-wave | VMs (sites) | Why this priority |
|---|---|---|---|
| **1** | **Reporting + AI Center + Accounting/POS** | `ReportingPageViewModel` (3), `AiCenterPageViewModel` (2), `AccountingPageViewModel` (2), `PosCheckoutViewModel` (3 — incl. `ChargeAsync`), `InvoiceProfileViewModel` (1) = **11** | revenue data, AI responses, payment-processor errors, invoice detail — highest exposure; the two flagged top-priority VMs are here |
| **2** | **Customers + HR + Membership** | `CustomerPageViewModel` (1), `CustomerProfileViewModel` (1), `HrPageViewModel` (2), `EmployeeProfileViewModel` (1), `AcceptInviteViewModel` (2) = **7** | PII, invite token / invitee identity |
| **3** | **Organization + Specialists + Services** | `OrganizationPageViewModel` (1), `SpecialistPageViewModel` (1), `SpecialistProfileViewModel` (1), `SpecialistScheduleViewModel` (2), `SpecialistAvailabilityViewModel` (1), `ServicePageViewModel` (1), `ServiceProfileViewModel` (1) = **8** | staff/org data; `SpecialistAvailabilityViewModel` uses `LogLoadFailed` (differently-named, same shape) |
| **4** | **Automation tabs** | `WorkflowsTabViewModel` (5), `ScheduledJobsTabViewModel` (3), `BusinessRulesTabViewModel` (2), `ApprovalsTabViewModel` (2), `AutomationDashboardTabViewModel` (1) = **13** | workflow/rule/cron content; these are the pre-8.39 filtered guards — keep the `when (exception is not OperationCanceledException)` filter |
| **5** | **Booking + Calendar + Inventory** | `BookingPageViewModel` (5), `CalendarPageViewModel` (3 — `LogLoadFailed`), `InventoryPageViewModel` (2), `InventoryProfileViewModel` (1) = **11** | customer/appointment/stock data |
| **6** | **Dashboard + Analytics + Salon + QR + Support + Settings** | `DashboardPageViewModel` (1 — `LogLoadFailed`), `AnalyticsPageViewModel` (1), `SalonPageViewModel` (2), `QrCodesPageViewModel` (2), `SupportPageViewModel` (2), `SettingsPageViewModel` (2 — Category D `NotSupportedException`, optional) = **8–10** | lower sensitivity; wrap-up sub-wave |

(Alternatively: **sub-wave 1 alone** as an authorised first tranche, then re-assess appetite for 2–6.)

### Per-sub-wave implementation shape

- **Files:** the sub-wave's VMs (Presentation only) + their test files. Sub-wave 1 = 5 prod + 5 test ≈ 10 files.
- **Change:** per site — drop the `exception` identifier from the `catch`, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage` (or `Common_LoadFailedMessage` if that key is added). Nothing else in the catch moves.
- **Tests:** update the surface-value assertions; add `Assert.Equal(Strings.Common_ActionFailedMessage, …)` where only `State`/no-throw was checked. **No new test files, no new stubs** (the existing failure-injection seams already throw a seeded message — the test now asserts the generic constant instead of that message, and `DoesNotContain(seededSecret)` becomes a genuine guarantee).
- **Risk: LOW** per sub-wave — mechanical, single-shape, fully test-guarded, no behaviour/logging/DI/service/localization change (if reusing the existing key). Aggregate is large, hence the split.
- **Suite delta:** ≈ 0 net new tests (assertions change in place); possibly +5–15 across all sub-waves for added explicit "surface == generic" checks.
- **Build:** 0/0 expected (dropping an unused `catch` variable is clean; `Strings.Common_ActionFailedMessage` already resolves in every one of these VMs — most already `using Rojan.Desktop.Presentation.Localization;` or use the `Localization.Strings.` prefix).

### Also recommended alongside (separate, tiny)

- **Phase 8.99.1** — `SettingsPage.xaml` visibility-trigger tweak (from Phase 8.101 §E). Independent of this sweep; do it first or bundle with sub-wave 6.

---

## STOP

Phase 8.102 audit complete. HEAD `0260bc3`, tracked tree clean, baseline 2,710 / 2,710.
**58 Category-A sites across 30 ViewModels** surface `exception.Message` to a bound `TextBlock` from a top-level broad catch — a systemic P2 leak (backend bodies, URLs, file paths, PII, revenue data, AI responses) that contradicts the already-safe operation-name-only logging in the same catch. The fix is uniform and behaviour-neutral: **drop the `catch` variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**, keep `State = Error` and the `LogOperationFailed` call. **No logging, localization, DI, service, or contract change** (localization key already ships). 2 additional `SettingsPageViewModel` `NotSupportedException` sites are Category D (non-sensitive static string) — optional.
**Recommendation: ~6 domain sub-waves, priority-ordered (Reporting/AI/Accounting first), LOW risk each; or authorise sub-wave 1 as a first tranche.** This is P2 hardening, fully separate from the completed Missing-Guard Sweep.

**Awaiting Phase 8.103 authorization.**
