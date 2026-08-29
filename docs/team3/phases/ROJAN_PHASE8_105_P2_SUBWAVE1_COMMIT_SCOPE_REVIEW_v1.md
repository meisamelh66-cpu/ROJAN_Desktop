# ROJAN AI — TEAM 3 — PHASE 8.105 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 1 — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No** source / test / fix / new-file / commit / push / merge / rebase / amend. Nothing staged.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `0260bc3` (unchanged)
**Reference:** `ROJAN_PHASE8_103_P2_SUBWAVE1_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_104_P2_SUBWAVE1_IMPLEMENTATION_REPORT_v1.md`
**Verdict: READY TO COMMIT** at Phase 8.106.

---

## A. GIT STATE

```
git rev-parse HEAD        → 0260bc38aabdb51af32e40bc90d22d00504e5211
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
```

### Modified tracked files — 10, all Phase 8.104

```
 M src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Accounting/InvoiceProfileViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/AI/AiCenterPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/PosCheckoutViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Accounting/InvoiceProfileViewModelTests.cs
```

Diffstat: `10 files changed, 126 insertions(+), 47 deletions(-)`. Untracked: only `ROJAN_*.md`. **Confirmed: only Phase 8.104 changes present; staging empty.**

The 47 deletions inspected — all are: (a) `catch (Exception exception)` → `catch (Exception)` and `= exception.Message` lines, (b) comment updates, (c) test-method renames (signature line changes), (d) seeded exception-message strings lengthened (`"search boom"` → `"search boom for Amelia Hart"`, etc.). **No test was removed.**

---

## B. SCOPE

| Required prod file | Modified? | Notes |
|---|---|---|
| `ReportingPageViewModel.cs` | ✅ | 3 catches; `= Localization.Strings.Common_ActionFailedMessage` (matches the file's existing `Localization.Strings.` style — no `using` added) |
| `AiCenterPageViewModel.cs` | ✅ | 2 catches; `= Strings.Common_ActionFailedMessage` (file already `using …Localization;`) |
| `AccountingPageViewModel.cs` | ✅ | 2 catches; `= Strings.Common_ActionFailedMessage`; static-form `LogOperationFailed(_logger, …)` and the `SearchAsync` out-of-order-completion `if` guard untouched |
| `PosCheckoutViewModel.cs` | ✅ | `+ using Rojan.Desktop.Presentation.Localization;` (1 line); 3 catches; `= Strings.Common_ActionFailedMessage` |
| `InvoiceProfileViewModel.cs` | ✅ | `+ using Rojan.Desktop.Presentation.Localization;` (1 line); 1 catch; `= Strings.Common_ActionFailedMessage` |

| Test files | 5 — `ReportingPageViewModelTests`, `AiCenterPageViewModelTests`, `AccountingPageViewModelTests`, `PosCheckoutViewModelTests`, `InvoiceProfileViewModelTests`. All existing, all directly related. `PosCheckoutViewModelTests` + `InvoiceProfileViewModelTests` gained `+ using …Localization;`. |

| Must stay untouched | Status |
|---|---|
| Services / query & command services | ✅ not in diff |
| Backend contracts / DTOs | ✅ not in diff |
| DI registration | ✅ not in diff |
| Localization files (`Strings.resx` / `.en` / `.ar`) | ✅ not in diff — `Common_ActionFailedMessage` reused as-is (Wave A `794648e`) |
| Test stubs (`StubReportingServices`, `StubAIRepository`, `StubInvoiceQueryService`, …) | ✅ not in diff — every failure path uses a pre-existing seam |
| Shell / `MainWindowViewModel` / navigation / authentication | ✅ not in diff |
| Any other ViewModel | ✅ not in diff — sub-waves 2–6 untouched |

**10 files, 100% within the STRICT SCOPE allowance.**

---

## C. SANITIZATION — 11/11 verified against the diff

| # | VM · method | Before | After | `State = Error` | `finally` / cancel branch | `Log…(nameof())` | `[LoggerMessage]` |
|---|---|---|---|---|---|---|---|
| 1 | `ReportingPageViewModel.LoadAsync` | `catch (Exception exception) { ErrorMessage = exception.Message; …` | `catch (Exception) { ErrorMessage = Localization.Strings.Common_ActionFailedMessage; …` | ✅ kept | n/a | ✅ `LogOperationFailed(nameof(LoadAsync))` | ✅ unchanged |
| 2 | `ReportingPageViewModel.RunReportAsync` | `… StatusMessage = exception.Message` | `… StatusMessage = Localization.Strings.Common_ActionFailedMessage` | n/a | ✅ `catch (OperationCanceledException) → Reporting_RunCancelled` **and** `finally { IsRunning = false; }` byte-unchanged | ✅ | ✅ |
| 3 | `ReportingPageViewModel.RerunSnapshotAsync` | `… StatusMessage = exception.Message` | `… = Localization.Strings.Common_ActionFailedMessage` | n/a | ✅ `finally { IsRunning = false; }` unchanged | ✅ | ✅ |
| 4 | `AiCenterPageViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `ErrorMessage = Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 5 | `AiCenterPageViewModel.SendMessageAsync` | `StatusMessage = exception.Message` | `StatusMessage = Strings.Common_ActionFailedMessage` | n/a | ✅ `finally { IsSending = false; }` unchanged | ✅ | ✅ |
| 6 | `AccountingPageViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `ErrorMessage = Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ `LogOperationFailed(_logger, …)` (static form) | ✅ |
| 7 | `AccountingPageViewModel.SearchAsync` | `ErrorMessage = exception.Message` (inside `if (searchText == SearchText)`) | `= Strings.Common_ActionFailedMessage` (same `if`) | ✅ kept | ✅ out-of-order guard `if` unchanged | ✅ static form | ✅ |
| 8 | `PosCheckoutViewModel.LoadOptionsAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 9 | `PosCheckoutViewModel.ProceedToPaymentAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 10 | `PosCheckoutViewModel.ChargeAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 11 | `InvoiceProfileViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |

Every catch now binds **no exception variable**. Every `#pragma warning disable/restore CA1031` boundary comment is byte-unchanged.

### Business behaviour — unchanged

| Concern | Verified |
|---|---|
| Page/dialog still recovers to the Error state (not a crash) | ✅ — `State = DashboardState.Error` retained at all 9 load/command sites; the 2 status-message sites keep their `finally` flag reset |
| Report run cancellation | ✅ — `RunReportAsync`'s `catch (OperationCanceledException) { StatusMessage = Reporting_RunCancelled; }` precedes the general catch, unchanged; a cancelled run still shows the cancelled copy, not the generic message |
| Search out-of-order completions | ✅ — `AccountingPageViewModel.SearchAsync` still only surfaces if `searchText == SearchText` |
| POS re-charge after a failed charge | ✅ — `ChargeAsync` leaves `CreatedInvoice` / `AmountTendered` untouched; the test still asserts `Assert.Same(invoiceBeforeCharge, sut.CreatedInvoice)` and `ChargeCommand.CanExecute(null)` |
| `IsRunning` / `IsSending` flags | ✅ — reset in `finally`, unchanged |

---

## D. SECURITY

Every one of the 11 surfaces now assigns the fixed localized constant `Strings.Common_ActionFailedMessage` (or `Localization.Strings.…` in Reporting) — the caught exception is **not bound to a variable**, so `.Message` / `.ToString()` / `.InnerException` are structurally unreachable from the surface.

| Data class | Was reachable via | Now |
|---|---|---|
| Revenue figures / customer metrics / employee-performance data | `ReportingPageViewModel` `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` (`ApiException.Message` quoting filters or row values) | **not reachable** — generic constant; tests seed `"revenue 1,850,000 for customer Sarah Johnson"` and assert `DoesNotContain("Sarah Johnson" / "1,850,000")` on the surface |
| AI prompts / responses / transcripts / customer names in a prompt | `AiCenterPageViewModel` `SendMessageAsync` — **previously a live leak** (`StatusMessage` showed `"upstream failed for customer Sarah Johnson"`) | **not reachable** — new assertion `DoesNotContain("Sarah Johnson", sut.StatusMessage)` |
| Payment-gateway / processor detail, merchant-account, card-network codes | `PosCheckoutViewModel.ChargeAsync` | **not reachable** — test seeds `"gateway declined: merchant acct 4929-XXXX, code 51"`, asserts `DoesNotContain("4929" / "gateway", sut.ErrorMessage)` |
| Invoice totals / line items / payments / receipts | `PosCheckoutViewModel.ProceedToPaymentAsync`, `InvoiceProfileViewModel.LoadAsync` | **not reachable** — `InvoiceProfileViewModelTests` asserts `DoesNotContain(FinancialSecret, sut.ErrorMessage)` where `FinancialSecret = "Amelia Hart / total 43.20 / Cash payment 43.20 / receipt"` |
| Backend response bodies / internal hosts / DB fragments | all 11 (`HttpRequestException`, EF text echoed in a 500) | **not reachable** — `AccountingPageViewModelTests` / `PosCheckoutViewModelTests` assert `DoesNotContain(backendBody, sut.ErrorMessage)` |

### Logs — unchanged, still operation-name-only

All 11 catches keep the identical `LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(_logger, nameof(<Method>))` call. `[LoggerMessage]` message templates (`"Reporting page operation failed. Operation={Operation}"`, etc.) are byte-unchanged. The pre-existing `DoesNotContain(backendBody, entry.Message)` log assertions still pass — re-verified in the subset run.

---

## E. TESTS

| Gate | Expected | Actual (working tree = `0260bc3` + Phase 8.104) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full suite | 2,713 / 2,713 | **2,713 / 2,713 PASS** ✅ |
| — Domain / Application / Infrastructure / Shell | 456 / 791 / 609 / 80 | unchanged ✅ |
| — **Presentation** | 767 → 770 | **770** (+3) ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-1 subset (Reporting + AI Center + Accounting + POS + Invoice) | — | **98 / 98 PASS** ✅ |

Suite progression: 2,701 (`7c9c132`) → 2,710 (`0260bc3`, Settings) → **2,713** (P2 sub-wave 1, +3).

### Test additivity

- **+3 net tests**, all via **pre-existing** failure-injection seams (`StubReportSnapshotQueryService.GetRecentSnapshotsException`, `StubAIRepository.GetSessionsException`, `StubReportExecutionQueryService.ResultFactory`): `ReportingPageViewModelTests.RerunSnapshotCommand_ExecutionThrows_…` + `.Constructor_LoadFails_…`; `AiCenterPageViewModelTests.LoadCommand_Failure_…`.
- ~10 in-place assertion flips (`Assert.Equal("boom"/backendBody, surface)` → `Assert.Equal(Strings.Common_ActionFailedMessage, surface)`) + the `// user-facing behaviour unchanged` comment in `AccountingPageViewModelTests` replaced with a `+ DoesNotContain(backendBody, …)` assertion.
- No new test files, **no stub changes**.
- All other Reporting / AI Center / Accounting / POS / Invoice tests unchanged and green.

---

## F. COMMIT READINESS

| Gate | State |
|---|---|
| Scope | ✅ 10 files (5 prod + 5 test), all authorised |
| Base HEAD | `0260bc3` — unchanged; staging empty |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,713 / 2,713; Architecture 7 / 7; subset 98 / 98 |
| Sanitization | ✅ 11/11 — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / `finally` / cancellation branch / log calls byte-unchanged |
| Security | ✅ exception message structurally unreachable from every surface; sentinel-enforced across customer names, revenue figures, gateway detail, financial detail, backend bodies |
| Behaviour | ✅ unchanged — error-state recovery, cancellation copy, out-of-order guard, re-charge semantics, busy flags all preserved |
| Localization | ✅ no `.resx` change |
| DI / services / contracts / stubs | ✅ none |
| Line endings | working-copy CRLF; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |

### Proposed commit

**Subject:**
```
fix(desktop): sanitize reporting, AI center and accounting error surfacing
```

**Body (suggested):**
```
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
```

**Trailers (required):**
```
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

### Proposed staging (Phase 8.106 — explicit paths, NO `git add -A` / `git add .`)

```
git add \
  src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Accounting/InvoiceProfileViewModel.cs \
  tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/AI/AiCenterPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Accounting/PosCheckoutViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Accounting/InvoiceProfileViewModelTests.cs
```

Expected post-commit: new HEAD child of `0260bc3`; `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update (§B commit table, §E 2,710 → 2,713, §G P2 sub-wave-1 ✅ + sub-waves 2–6 remaining).

---

## STOP

Phase 8.105 review complete. **Verdict: READY.** HEAD `0260bc3`, staging empty, 10 sub-wave-1 files modified and nothing else, build 0/0, 2,713/2,713, Architecture 7/7, subset 98/98. All 11 sites drop the `catch` variable and swap `exception.Message` → `Strings.Common_ActionFailedMessage`; `State = Error`, the cancellation branch, every `finally`, and every operation-name-only log call are byte-unchanged; no localization / DI / service / contract / stub change. The confirmed `SendMessageAsync` customer-name leak is closed.

**Awaiting Phase 8.106 — Sub-Wave 1 Commit Authorization.**
