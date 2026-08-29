# ROJAN AI — TEAM 3 — PHASE 8.106 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 1 — COMMIT REPORT v1

**Type:** Commit execution. One commit created. No push / merge / rebase / amend. No source/test change beyond Phase 8.104.
**Authorization:** APPROVED (Phase 8.106 block).

---

## A. NEW HEAD

```
76d3f61  fix(desktop): sanitize reporting, AI center and accounting error surfacing
0260bc3  fix(desktop): guard settings page command failures   (parent)
7c9c132  fix(desktop): guard remaining automation tab command failures
```

- **Branch:** `feature/team3-desktop-completion`
- **New HEAD:** `76d3f61` — child of `0260bc3`
- **Not pushed.** Tracked tree after commit: **clean**; untracked = `ROJAN_*.md` reports only.

### Staged & committed — 10 files, exactly the approved set

```
 src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs        | 12 +++---
 src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs                |  8 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs      |  8 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs         | 13 +++---
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/InvoiceProfileViewModel.cs      |  5 ++-
 tests/.../Reporting/ReportingPageViewModelTests.cs                                   | 47 ++++++++++++++---
 tests/.../AI/AiCenterPageViewModelTests.cs                                           | 25 +++++++++-
 tests/.../Accounting/AccountingPageViewModelTests.cs                                 | 20 +++++---
 tests/.../Accounting/PosCheckoutViewModelTests.cs                                    | 26 +++++++---
 tests/.../Accounting/InvoiceProfileViewModelTests.cs                                 |  9 ++--
 10 files changed, 126 insertions(+), 47 deletions(-)
```

Staging: `git reset` then explicit per-path `git add` (no `git add .` / `-A`). No report `.md` staged.

### Commit message (as committed)

```
fix(desktop): sanitize reporting, AI center and accounting error surfacing

Swap the raw exception.Message in the pre-existing top-level broad
catches to the generic Strings.Common_ActionFailedMessage across the
highest-sensitivity P2 tranche, so a failed load/run/charge shows a
safe message instead of a backend body, an internal URL, a customer
name, revenue figures, or payment-gateway detail.

- ReportingPageViewModel: LoadAsync, RunReportAsync, RerunSnapshotAsync
- AiCenterPageViewModel: LoadAsync, SendMessageAsync
- AccountingPageViewModel: LoadAsync, SearchAsync
- PosCheckoutViewModel: LoadOptionsAsync, ProceedToPaymentAsync, ChargeAsync
- InvoiceProfileViewModel: LoadAsync

Each catch now binds no exception variable. State = Error, the
RunReportAsync OperationCanceledException branch, every finally block,
and every operation-name-only [LoggerMessage] call are unchanged. No
localization, DI, service or contract change. +3 tests (sentinel-
enforced no-leak assertions); the confirmed SendMessageAsync
customer-name leak is now closed.

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

---

## B. 11 / 11 SANITIZED SITES

| VM | Method | Surface | Before → After |
|---|---|---|---|
| `ReportingPageViewModel` | `LoadAsync` | `ErrorMessage` | `exception.Message` → `Localization.Strings.Common_ActionFailedMessage` |
| `ReportingPageViewModel` | `RunReportAsync` | `StatusMessage` | same |
| `ReportingPageViewModel` | `RerunSnapshotAsync` | `StatusMessage` | same |
| `AiCenterPageViewModel` | `LoadAsync` | `ErrorMessage` | `exception.Message` → `Strings.Common_ActionFailedMessage` |
| `AiCenterPageViewModel` | `SendMessageAsync` | `StatusMessage` | same |
| `AccountingPageViewModel` | `LoadAsync` | `ErrorMessage` | same |
| `AccountingPageViewModel` | `SearchAsync` | `ErrorMessage` | same (inside the unchanged out-of-order-completion `if`) |
| `PosCheckoutViewModel` | `LoadOptionsAsync` | `ErrorMessage` | same (`+ using …Localization;`) |
| `PosCheckoutViewModel` | `ProceedToPaymentAsync` | `ErrorMessage` | same |
| `PosCheckoutViewModel` | `ChargeAsync` | `ErrorMessage` | same |
| `InvoiceProfileViewModel` | `LoadAsync` | `ErrorMessage` | same (`+ using …Localization;`) |

Every catch now binds **no exception variable**. **Byte-unchanged:** every `State = DashboardState.Error` (9 sites), the `RunReportAsync` `catch (OperationCanceledException) → Reporting_RunCancelled` branch, every `finally` (`IsRunning` / `IsSending`), the `SearchAsync` out-of-order guard, the POS re-charge semantics, every `#pragma warning disable/restore CA1031` comment, every `LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(_logger, …)` call, every `[LoggerMessage]` signature.

---

## C. SECURITY IMPROVEMENT

Before `76d3f61`, a failed load / report run / AI chat / POS charge / invoice load bound `exception.Message` to a `TextBlock`. After: the fixed localized constant only; `.Message` / `.ToString()` / `.InnerException` structurally unreachable from every surface.

| Data no longer reachable via the UI | Sentinel test |
|---|---|
| Revenue figures, report filters, customer/employee metrics (Reporting) | `"revenue 1,850,000 for customer Sarah Johnson"` → `DoesNotContain("Sarah Johnson" / "1,850,000")` on `ErrorMessage` / `StatusMessage` |
| **AI prompt / customer name** — `AiCenterPageViewModel.SendMessageAsync`, a **confirmed live leak** (`StatusMessage` showed `"upstream failed for customer Sarah Johnson"`) | new assertion `DoesNotContain("Sarah Johnson", sut.StatusMessage)` |
| Payment-gateway decline text / merchant account / card codes (POS `ChargeAsync`) | `"gateway declined: merchant acct 4929-XXXX, code 51"` → `DoesNotContain("4929" / "gateway", sut.ErrorMessage)` |
| Invoice totals / line items / payments / receipts (`InvoiceProfileViewModel`, POS `ProceedToPaymentAsync`) | `FinancialSecret = "Amelia Hart / total 43.20 / …"` → `DoesNotContain(FinancialSecret, sut.ErrorMessage)` |
| Backend response bodies, internal hosts, EF/SQL fragments (all 11) | `DoesNotContain(backendBody, sut.ErrorMessage)` (Accounting / POS) |

**Logs unchanged** — operation-name-only in all 11; the pre-existing `DoesNotContain(backendBody, entry.Message)` log assertions still pass.

---

## D. TEST DELTA

| | `0260bc3` | `76d3f61` | Δ |
|---|---|---|---|
| Domain | 456 | 456 | — |
| **Presentation** | 767 | **770** | **+3** |
| Application | 791 | 791 | — |
| Infrastructure | 609 | 609 | — |
| Shell | 80 | 80 | — |
| Architecture | 7 | 7 | — |
| **Total** | **2,710** | **2,713** | **+3** |

**+3 net**, all via **pre-existing** failure-injection seams (`StubReportSnapshotQueryService.GetRecentSnapshotsException`, `StubAIRepository.GetSessionsException`, `StubReportExecutionQueryService.ResultFactory`):
- `ReportingPageViewModelTests.RerunSnapshotCommand_ExecutionThrows_SurfacesGenericMessage_NoLeak`
- `ReportingPageViewModelTests.Constructor_LoadFails_StateIsError_SurfacesGenericMessage_NoLeak`
- `AiCenterPageViewModelTests.LoadCommand_Failure_StateIsError_SurfacesGenericMessage_NoLeak`

Plus ~10 in-place assertion flips (`Assert.Equal("boom"/backendBody, surface)` → `Assert.Equal(Strings.Common_ActionFailedMessage, surface)`) and the `// user-facing behaviour unchanged` comment in `AccountingPageViewModelTests` replaced with a `+ DoesNotContain(backendBody, sut.ErrorMessage)` assertion. **No new test files, no stub changes.**

---

## E. POST-COMMIT VALIDATION

| Gate | Expected | Actual (at `76d3f61`) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | 2,713 / 2,713 | **2,713 / 2,713 PASS** ✅ (Domain 456, Application 791, Presentation 770, Architecture 7, Shell 80, Infrastructure 609) |
| Architecture tests | 7 / 7 | **7 / 7 PASS** ✅ |

Suite progression: 2,701 (`7c9c132`) → 2,710 (`0260bc3`, Settings) → **2,713** (`76d3f61`, P2 sub-wave 1, +3).

---

## F. CHECKPOINT

`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` updated: §A HEAD `76d3f61` + banner + audit-phase list (+8.105) + commit chain; §B commit table (+`76d3f61` row); §E `Debug verified at 76d3f61` / `2,713/2,713` / Presentation 770 / progression line `→ 2,713 (76d3f61, +3 "sanitize load-error surfacing" P2 sub-wave 1)`; §F new Phase 8.104 detail bullet (incl. the confirmed `SendMessageAsync` leak); §G — Missing-Guard Sweep now flatly "complete"; "sanitize load-error surfacing" P2 is the active track, sub-wave 1 ✅, sub-waves 2–6 listed with site counts; §H items 1/2/5/6(c). No code changed by the checkpoint update.

---

## G. REMAINING P2 SUB-WAVES

`ROJAN_PHASE8_102_*` §F, priority-ordered. Each: one audit → one commit-scope-review → one commit; drop the `catch` variable, swap to `Strings.Common_ActionFailedMessage`, keep `State = Error` + `LogOperationFailed`; **no localization / DI / service / logging change**. ~50–80 in-place test-assertion edits total, minimal new tests.

| # | Sub-wave | VMs (sites) |
|---|---|---|
| ~~1~~ | ~~Reporting + AI Center + Accounting/POS~~ | **✅ `76d3f61`** — 11 sites |
| 2 | Customers + HR + Membership | `CustomerPageViewModel` (1), `CustomerProfileViewModel` (1), `HrPageViewModel` (2), `EmployeeProfileViewModel` (1), `AcceptInviteViewModel` (2) = **7** |
| 3 | Organization + Specialists + Services | `OrganizationPageViewModel` (1), `SpecialistPageViewModel` (1), `SpecialistProfileViewModel` (1), `SpecialistScheduleViewModel` (2), `SpecialistAvailabilityViewModel` (1), `ServicePageViewModel` (1), `ServiceProfileViewModel` (1) = **8** |
| 4 | Automation tabs | `WorkflowsTabViewModel` (5), `ScheduledJobsTabViewModel` (3), `BusinessRulesTabViewModel` (2), `ApprovalsTabViewModel` (2), `AutomationDashboardTabViewModel` (1) = **13** — **keep the `when (exception is not OperationCanceledException)` filter** |
| 5 | Booking + Calendar + Inventory | `BookingPageViewModel` (5), `CalendarPageViewModel` (3), `InventoryPageViewModel` (2), `InventoryProfileViewModel` (1) = **11** |
| 6 | Dashboard + Analytics + Salon + QR + Support + Settings | `DashboardPageViewModel` (1), `AnalyticsPageViewModel` (1), `SalonPageViewModel` (2), `QrCodesPageViewModel` (2), `SupportPageViewModel` (2), `SettingsPageViewModel` (2 — Category D `NotSupportedException`, optional) = **8–10** |

**Recommended next: Phase 8.107 — sub-wave 2 scope audit** (Customers + HR + Membership — PII / invite-token sensitivity). Also still available: Phase 8.99.1 (Settings XAML visibility tweak, LOW risk).

---

## STOP

Phase 8.106 complete. HEAD `76d3f61` (`fix(desktop): sanitize reporting, AI center and accounting error surfacing`), not pushed. Build 0/0, **2,713/2,713** tests pass, Architecture 7/7.
**11/11 sub-wave-1 sites sanitized** — `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }` across `ReportingPageViewModel` ×3, `AiCenterPageViewModel` ×2, `AccountingPageViewModel` ×2, `PosCheckoutViewModel` ×3, `InvoiceProfileViewModel` ×1. `State = Error`, the cancellation branch, every `finally`, and every operation-name-only log call are byte-unchanged; no localization / DI / service / contract / stub change. The **confirmed live `SendMessageAsync` customer-name leak is closed.** +3 tests. Sub-waves 2–6 of the "sanitize load-error surfacing" P2 remain (47 sites).

**Awaiting next authorization.**
