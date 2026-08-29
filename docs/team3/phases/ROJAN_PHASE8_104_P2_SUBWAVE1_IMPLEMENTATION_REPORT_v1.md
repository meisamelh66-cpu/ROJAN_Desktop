# ROJAN AI — TEAM 3 — PHASE 8.104 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 1 — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.105 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `0260bc3` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_103_P2_SUBWAVE1_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 10 (5 prod + 5 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Reporting/ReportingPageViewModel.cs        | 12 +++---
 src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs                |  8 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/AccountingPageViewModel.cs      |  8 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/PosCheckoutViewModel.cs         | 13 +++---
 src/Rojan.Desktop.Presentation/ViewModels/Accounting/InvoiceProfileViewModel.cs      |  5 ++-
 tests/Rojan.Desktop.Presentation.Tests/Reporting/ReportingPageViewModelTests.cs      | 47 ++++++++++++++---
 tests/Rojan.Desktop.Presentation.Tests/AI/AiCenterPageViewModelTests.cs              | 25 +++++++++-
 tests/Rojan.Desktop.Presentation.Tests/Accounting/AccountingPageViewModelTests.cs    | 20 +++++---
 tests/Rojan.Desktop.Presentation.Tests/Accounting/PosCheckoutViewModelTests.cs       | 26 +++++++---
 tests/Rojan.Desktop.Presentation.Tests/Accounting/InvoiceProfileViewModelTests.cs    |  9 ++--
 10 files changed, 126 insertions(+), 47 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, other ViewModels, Shell, navigation, authentication. No new files, no new stubs.

### Per-prod-file

| File | Change |
|---|---|
| `ReportingPageViewModel.cs` | 3 catches (`LoadAsync`, `RunReportAsync`, `RerunSnapshotAsync`): `catch (Exception exception)` → `catch (Exception)`; `= exception.Message` → `= Localization.Strings.Common_ActionFailedMessage`. `catch (OperationCanceledException) → Reporting_RunCancelled` and both `finally { IsRunning = false; }` untouched. |
| `AiCenterPageViewModel.cs` | 2 catches (`LoadAsync`, `SendMessageAsync`): same swap → `Strings.Common_ActionFailedMessage`. `finally { IsSending = false; }` untouched. |
| `AccountingPageViewModel.cs` | 2 catches (`LoadAsync`, `SearchAsync`): same swap → `Strings.Common_ActionFailedMessage`. `SearchAsync`'s out-of-order-completion `if (searchText == SearchText)` guard and the static-form `LogOperationFailed(_logger, …)` untouched. |
| `PosCheckoutViewModel.cs` | `+ using Rojan.Desktop.Presentation.Localization;`. 3 catches (`LoadOptionsAsync`, `ProceedToPaymentAsync`, `ChargeAsync`): same swap → `Strings.Common_ActionFailedMessage`. |
| `InvoiceProfileViewModel.cs` | `+ using Rojan.Desktop.Presentation.Localization;`. 1 catch (`LoadAsync`): same swap → `Strings.Common_ActionFailedMessage`. |

**Every `State = DashboardState.Error`, every `finally`, every `#pragma warning disable/restore CA1031` comment, every `Log…(nameof(<Method>))` call, and every `[LoggerMessage]` signature is byte-unchanged.**

---

## B. SITES SANITIZED — 11 / 11

| # | VM | Method | Surface | `State = Error` kept | Ref used |
|---|---|---|---|---|---|
| 1 | `ReportingPageViewModel` | `LoadAsync` | `ErrorMessage` | ✅ | `Localization.Strings.Common_ActionFailedMessage` |
| 2 | `ReportingPageViewModel` | `RunReportAsync` | `StatusMessage` | n/a (`finally`) | `Localization.Strings.…` |
| 3 | `ReportingPageViewModel` | `RerunSnapshotAsync` | `StatusMessage` | n/a (`finally`) | `Localization.Strings.…` |
| 4 | `AiCenterPageViewModel` | `LoadAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 5 | `AiCenterPageViewModel` | `SendMessageAsync` | `StatusMessage` | n/a (`finally`) | `Strings.…` |
| 6 | `AccountingPageViewModel` | `LoadAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 7 | `AccountingPageViewModel` | `SearchAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 8 | `PosCheckoutViewModel` | `LoadOptionsAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 9 | `PosCheckoutViewModel` | `ProceedToPaymentAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 10 | `PosCheckoutViewModel` | `ChargeAsync` | `ErrorMessage` | ✅ | `Strings.…` |
| 11 | `InvoiceProfileViewModel` | `LoadAsync` | `ErrorMessage` | ✅ | `Strings.…` |

Cancellation: `ReportingPageViewModel.RunReportAsync` still has its explicit `catch (OperationCanceledException) { StatusMessage = Reporting_RunCancelled; }` **before** the general catch — unchanged. No other method in this cluster threads a token. No `OperationCanceledException` becomes the generic message.

---

## C. SECURITY IMPACT

Every one of the 11 catches now binds **no exception variable** — `exception.Message` / `.ToString()` / `.InnerException` is structurally unreachable from the surface assignment. The bound `TextBlock` receives only the fixed localized constant `Strings.Common_ActionFailedMessage`.

| Flow | Before → After |
|---|---|
| **Reporting** `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` | a backend validation message quoting **report filters / row values / revenue figures**, or `HttpRequestException` host detail → **generic constant** |
| **AI Center** `LoadAsync` | health-score / insight / config backend error, token detail → **generic constant** |
| **AI Center** `SendMessageAsync` | **model-provider error quoting the user's prompt or a customer name** — *confirmed live_: the existing test seeds `"upstream failed for customer Sarah Johnson"` and it previously reached `StatusMessage` → **generic constant** (new test asserts `DoesNotContain("Sarah Johnson", sut.StatusMessage)`) |
| **POS** `LoadOptionsAsync` / `ProceedToPaymentAsync` | customer list, invoice line items / tax / totals in a validation message → **generic constant** |
| **POS** `ChargeAsync` | **payment-gateway decline text / merchant-account detail / card-network codes** → **generic constant** (new test seeds `"gateway declined: merchant acct 4929-XXXX, code 51"` and asserts `DoesNotContain("4929" / "gateway", sut.ErrorMessage)`) |
| **Invoice profile** `LoadAsync` | full invoice + payments + receipts detail in a backend error → **generic constant** (test seeds `"Amelia Hart / total 43.20 / …"` and asserts `DoesNotContain(FinancialSecret, sut.ErrorMessage)`) |

The log side was already operation-name-only in all 11 — unchanged and re-verified by the existing `DoesNotContain(backendBody, entry.Message)` assertions.

---

## D. TESTS

**+3 net** (Presentation.Tests 767 → **770**). ~10 assertions updated in place; 5 tests renamed to reflect the strengthened contract; 3 genuinely new tests. No new files, no new stubs — every failure path uses a **pre-existing** failure-injection seam.

| File | Δtests | Detail |
|---|---|---|
| `ReportingPageViewModelTests` | **+2** | `RunReportCommand_ExecutionThrows_LogsError` → renamed `…_AndSurfacesGenericMessage`; seeds `"…for customer Sarah Johnson"`, asserts `StatusMessage == Common_ActionFailedMessage` + `DoesNotContain("Sarah Johnson")`. `NoLoggerSupplied_…RunReportFailureNeverThrows` — surface assertion flipped to the constant. **New:** `RerunSnapshotCommand_ExecutionThrows_SurfacesGenericMessage_NoLeak`; **New:** `Constructor_LoadFails_StateIsError_SurfacesGenericMessage_NoLeak` (via the pre-existing `StubReportSnapshotQueryService.GetRecentSnapshotsException` seam). |
| `AiCenterPageViewModelTests` | **+1** | `SendMessageCommand_ServiceThrows_LogsErrorWithoutLeakingChatText` → renamed `…LogsErrorAndSurfacesGenericMessage_NoChatTextLeak`; **added** `StatusMessage == Common_ActionFailedMessage` + `DoesNotContain("Sarah Johnson", sut.StatusMessage)`. **New:** `LoadCommand_Failure_StateIsError_SurfacesGenericMessage_NoLeak` (via the pre-existing `StubAIRepository.GetSessionsException` seam + `sut.LoadCommand.Execute(null)`). |
| `AccountingPageViewModelTests` | 0 | `Constructor_QueryServiceThrows_…` → `…SetsGenericErrorMessage` (assert flipped). `LoadAsync_QueryServiceThrows_…` — the assertion `Assert.Equal(backendBody, sut.ErrorMessage); // user-facing behaviour unchanged` **replaced** with `Assert.Equal(Common_ActionFailedMessage, …)` + `DoesNotContain(backendBody, sut.ErrorMessage)` + comment updated. `SearchAsync_QueryServiceThrows_…` → `…AndSurfacesGenericMessage`; seeds `"…for Amelia Hart"`, asserts the constant + `DoesNotContain("Amelia Hart")`. |
| `PosCheckoutViewModelTests` | 0 | `+ using …Localization;`. `Constructor_OptionsQueryThrows_…` → `…SetsGenericErrorMessage` (+ `DoesNotContain("Amelia Hart")`). `LoadCommand_QueryThrows_…` → `…_InLogOrUi` (+ surface constant + `DoesNotContain(backendBody)`). `ProceedToPaymentCommand_BackendThrows_LogsTheFailure` → `…_AndSurfacesGenericMessage` (+ constant + `DoesNotContain("Amelia Hart")`). `ChargeCommand_BackendThrows_…ReChargeable` → `…_AndSurfacesGenericMessage`; seeds `"gateway declined: merchant acct 4929-XXXX, code 51"`, asserts constant + `DoesNotContain("4929" / "gateway")`. The re-chargeable / `CanExecute` assertions kept. |
| `InvoiceProfileViewModelTests` | 0 | `+ using …Localization;`. `LoadAsync_Failure_…NoFinancialLeak` — **added** `ErrorMessage == Common_ActionFailedMessage` + `DoesNotContain(FinancialSecret, sut.ErrorMessage)` (was log-only). Both `Assert.Equal("boom", sut.ErrorMessage)` (WithoutLogger + `Constructor_ProfileQueryThrows`) → the constant; the latter renamed `…SetsGenericErrorMessage`. |

**Subset run:** Reporting + AI Center + Accounting + POS + Invoice profile → **98 / 98 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `0260bc3` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,713 | **2,713 / 2,713 PASS** ✅ |
| — Domain | 456 | 456 |
| — **Presentation** | +3 → 770 | **770** ✅ |
| — Application | 791 | 791 |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-1 subset | — | **98 / 98 PASS** ✅ |

Suite progression: 2,710 (`0260bc3`) → **2,713** (+3, P2 sub-wave 1).

---

## F. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 10 files (5 prod + 5 test), all within the STRICT SCOPE allowance |
| Base HEAD | `0260bc3` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,713 / 2,713; Architecture 7 / 7; subset 98 / 98 |
| Sites | ✅ 11 / 11 — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / `finally` / cancellation branch / log calls all byte-unchanged |
| Security | ✅ exception message structurally unreachable from every surface; sentinel-enforced (customer name, financial figures, gateway detail) |
| Behaviour | ✅ unchanged — `State`, re-run/re-charge semantics, out-of-order-completion guard, `IsRunning`/`IsSending` flags all preserved |
| Localization | ✅ no `.resx` change — `Common_ActionFailedMessage` reused (all 3 locales, Wave A) |
| DI / services / contracts | ✅ none |
| Line endings | working-copy CRLF; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |
| Proposed commit subject | `fix(desktop): sanitize reporting, AI center and accounting error surfacing` |
| Proposed staged files | the 10 above — **no `git add -A` / `git add .`** |

### Separate from Missing-Guard work

This changes the *message string* in *pre-existing* catches. It adds no guard, no boundary, no behaviour. The Missing-Guard Sweep (`794648e` … `0260bc3`) is complete and untouched.

---

## STOP

Phase 8.104 implementation complete. Base HEAD `0260bc3` unchanged (no commit). Build 0/0, **2,713 / 2,713** tests pass, Architecture 7/7, sub-wave-1 subset 98/98.
**11 Category-A sites across 5 ViewModels sanitized** — `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = DashboardState.Error`, every `finally`, the Reporting `OperationCanceledException` branch, and every operation-name-only log call are byte-unchanged. `PosCheckoutViewModel` + `InvoiceProfileViewModel` each gained one `using` line. **No localization / DI / service / contract change.** +3 net tests (sentinel-enforced no-leak assertions for customer names, revenue figures, and payment-gateway detail); the confirmed `SendMessageAsync` customer-name leak is now closed.

**Awaiting Phase 8.105 — Sub-Wave 1 Commit Scope Review.**
