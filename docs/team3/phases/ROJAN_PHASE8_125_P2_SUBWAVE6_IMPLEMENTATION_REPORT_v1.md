# ROJAN AI — TEAM 3 — PHASE 8.125 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 6 (FINAL) — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.126 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `71fb472` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_124_P2_SUBWAVE6_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 12 (6 prod + 6 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Dashboard/DashboardPageViewModel.cs         |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Analytics/AnalyticsPageViewModel.cs         |  5 +++--
 src/Rojan.Desktop.Presentation/ViewModels/Salons/SalonPageViewModel.cs                |  9 +++++----
 src/Rojan.Desktop.Presentation/ViewModels/QrCodes/QrCodesPageViewModel.cs             |  9 +++++----
 src/Rojan.Desktop.Presentation/ViewModels/Support/SupportPageViewModel.cs             |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerProfileViewModel.cs       |  4 ++--
 tests/…/Dashboard/DashboardPageViewModelTests.cs                                      |  8 +++++---
 tests/…/Analytics/AnalyticsPageViewModelTests.cs                                      |  3 ++-
 tests/…/Salons/SalonPageViewModelTests.cs                                             | 11 +++++++----
 tests/…/QrCodes/QrCodesPageViewModelTests.cs                                          |  9 ++++++---
 tests/…/Support/SupportPageViewModelTests.cs                                          | 14 +++++++++-----
 tests/…/Customers/CustomerProfileViewModelTests.cs                                    |  7 +++++--
 12 files changed, 53 insertions(+), 34 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, other ViewModels (incl. `SettingsPageViewModel` — its 2 `NotSupportedException` Category-D branches were audited as excluded), stubs/test doubles. No new files.

**`using` additions:** `+ using Rojan.Desktop.Presentation.Localization;` in **3 prod** (`AnalyticsPageViewModel.cs`, `SalonPageViewModel.cs`, `QrCodesPageViewModel.cs`) + **4 test** (`DashboardPageViewModelTests.cs`, `AnalyticsPageViewModelTests.cs`, `SalonPageViewModelTests.cs`, `QrCodesPageViewModelTests.cs`, `SupportPageViewModelTests.cs`). `DashboardPageViewModel` / `CustomerProfileViewModel` already imported it; `SupportPageViewModel` uses the fully-qualified `Localization.Strings.` form (prod — no addition; test file got the `using` for readability).

---

## B. SITES SANITIZED — 9 / 6 VMs

### Plain `catch (Exception exception)` → `catch (Exception)` (7 sites, variable dropped)

| # | VM · method | Surface | `State = Error` | Log call | Preserved |
|---|---|---|---|---|---|
| 1 | `DashboardPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogLoadFailed(nameof(LoadAsync))` | `canViewFinancials` KPI-filter (in `try`) |
| 2 | `AnalyticsPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | `Task.WhenAll` fan-out |
| 3 | `SalonPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 4 | `SalonPageViewModel.CreateSalonAsync` | `CreateErrorMessage` | n/a | `LogOperationFailed(nameof(CreateSalonAsync))` | **`finally { IsCreating = false; }`**; `HasCreateError` notify; form retention |
| 5 | `QrCodesPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 6 | `QrCodesPageViewModel.GenerateReceptionInviteAsync` | `GenerateInviteErrorMessage` | n/a | `LogOperationFailed(nameof(GenerateReceptionInviteAsync))` | **`finally { IsGeneratingReceptionInvite = false; }`**; `HasGenerateInviteError` notify; `if (Salon is null) return;` |
| 7 | `CustomerProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | (carried over from sub-wave 2) |

Each: `catch (Exception exception)` → `catch (Exception)`, `= exception.Message;` → `= Strings.Common_ActionFailedMessage;`.

### Filtered `catch (Exception exception) when (exception is not OperationCanceledException)` (2 sites, variable kept)

| # | VM · method | Surface | Log call | Preserved |
|---|---|---|---|---|
| 8 | `SupportPageViewModel.SubmitMessageAsync` | `MessageError` | `LogOperationFailed(nameof(SubmitMessageAsync))` | `when` clause **byte-unchanged** (`exception` bound, unused in body, no warning); `MessageStatus = null` reset |
| 9 | `SupportPageViewModel.SubmitApplicationAsync` | `ApplicationError` | `LogOperationFailed(nameof(SubmitApplicationAsync))` | `when` clause **byte-unchanged**; the 11-field clear-on-success block |

Each: **only** `= exception.Message;` → `= Localization.Strings.Common_ActionFailedMessage;` (fully-qualified, matching the file's existing `Localization.Strings.Support_Message_Sent` / `_Application_Sent`). Catch clause identical.

**Byte-unchanged everywhere:** every `#pragma warning disable CA1031` / `restore CA1031` pair; every `State = DashboardState.Error` (5 sites); every `Log…(nameof(<Method>))`; every `[LoggerMessage]` instance signature; the 2 `finally` blocks; the QR `Salon is null` guard; the Support success-path form-clears and status resets; the `Dashboard` financial-KPI gating; the `HasCreateError` / `HasGenerateInviteError` change notifications.

**Excluded (Category-D, per audit):** `SettingsPageViewModel.DownloadOrInstallAsync` / `RemovePackAsync` — `catch (NotSupportedException) { StatusMessage = exception.Message; }`. The message is the hard-coded local developer string from `LocalOnlyLanguagePackRepository` ("…not available yet - Phase 19A ships the framework only"), not untrusted data. `grep -rn "= exception.Message" src/…/ViewModels/` now returns **only** these 2 sites.

---

## C. SECURITY

With the `exception` variable removed (7 sites) / no longer read (2 Support sites), `exception.Message` is structurally unreachable from every one of the 9 bound error `TextBlock`s.

| Domain | VM · method | Data no longer reachable | Enforcement |
|---|---|---|---|
| **Dashboard** — KPI / revenue / metrics | `LoadAsync` | KPI overview incl. **revenue figures / financial KPIs** (the data gated behind `AccountingView`), staff names, activity feed | test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(backendBody, sut.ErrorMessage)` (**live leak closed**) |
| **Analytics** — reports / insights | `LoadAsync` | KPI values, analytics summary (revenue trends, retention, spend), chart series | test now asserts `Strings.Common_ActionFailedMessage` |
| **Salon** — configuration | `LoadAsync` | salon name / phone / email / address (owner PII), org·branch ids | test now asserts `Strings.Common_ActionFailedMessage` |
| | `CreateSalonAsync` | backend validation bodies echoing name/phone/email/address, uniqueness-conflict detail | test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain("Validation failed", …)` (**live leak closed**) |
| **QR** — invite data / access links | `LoadAsync` | salon record, customer-facing salon QR payload / download URL | test now asserts `Strings.Common_ActionFailedMessage` |
| | `GenerateReceptionInviteAsync` | **invite tokens / invite ids**, authz bodies | tests now assert `Strings.Common_ActionFailedMessage` + `DoesNotContain("Forbidden", …)` (**live leak closed**) |
| **Support** — ticket details | `SubmitMessageAsync` | sender name / email, subject / body echoed in a validation 400 | test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain("failed validation", …)` |
| | `SubmitApplicationAsync` | **applicant PII** — name, mobile, email, city, GitHub / LinkedIn / portfolio / resume URLs | test now asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain("failed validation", …)` |
| **CustomerProfile** — PII / notes / history | `LoadAsync` | **customer PII** (name / email / phone), notes, tags, full appointment history, loyalty / engagement insights | `LoadAsync_Failure_…_NoPiiLeak` now also asserts `Strings.Common_ActionFailedMessage` + `DoesNotContain(PiiSecret, sut.ErrorMessage)` |

**Logs unchanged** — operation-name-only at all 9 sites; the exception object is never passed to any logger. Every pre-existing log no-leak assertion (`DashboardPageViewModelTests`, `SupportPageViewModelTests` ×2, `CustomerProfileViewModelTests`, the Salon/QR/Analytics operation-name log tests) is retained and green.

**Three confirmed live test-documented leaks closed:** Dashboard `LoadAsync`, Salon `CreateSalonAsync`, QR `GenerateReceptionInviteAsync` (same class as sub-wave 2 `AcceptInviteViewModel` and sub-wave 5 `CreateBookingAsync` / `InitializeAsync`).

---

## D. TESTS

**+0 net tests** (Presentation.Tests stays at **772**). All changes are assertion updates on existing tests + `DoesNotContain` sentinel additions.

| File | Change |
|---|---|
| `DashboardPageViewModelTests` | `+ using …Localization;`. `Constructor_QueryServiceThrows_…` + `Constructor_QueryServiceThrows_LogsError_…NoExceptionLeak` → `Strings.Common_ActionFailedMessage`; `+ DoesNotContain(backendBody, sut.ErrorMessage)` in the leak test. |
| `AnalyticsPageViewModelTests` | `+ using …Localization;`. `LoadAsync_QueryThrows_LogsError` → `Strings.Common_ActionFailedMessage`. |
| `SalonPageViewModelTests` | `+ using …Localization;`. `Constructor_QueryThrows_…` + `LoadAsync_QueryThrows_LogsError` + `CreateSalonAsync_CommandThrows_LogsError` → `Strings.Common_ActionFailedMessage`; `CreateSalonCommand_Failure_SetsCreateErrorMessageAndLeavesFormVisible` → generic + `DoesNotContain("Validation failed", …)`. |
| `QrCodesPageViewModelTests` | `+ using …Localization;`. `Constructor_SalonQueryFails_StateIsError` + `LoadAsync_QueryThrows_LogsError` + `GenerateReceptionInviteCommand_BackendRejects_LogsError` + `GenerateReceptionInviteCommand_BackendRejects_SetsGenerateInviteErrorMessage` → `Strings.Common_ActionFailedMessage`; `+ DoesNotContain("Forbidden", …)` in the LogsError test. |
| `SupportPageViewModelTests` | `+ using …Localization;`. 5 assertions (`SubmitMessageCommand_ServiceThrows_SetsErrorAndKeepsFields`, `…_LogsErrorWithoutLeakingFormData`, `SubmitApplicationCommand_…LogsErrorWithoutLeakingApplicantData`, `NoLoggerSupplied_…SubmitFailureNeverThrows`, `SubmitApplicationCommand_ServiceThrows_SetsErrorAndKeepsFields`) `Assert.NotNull(...)` → `Assert.Equal(Strings.Common_ActionFailedMessage, …)`; `+ DoesNotContain("failed validation", …)` in the 2 `…SetsErrorAndKeepsFields` tests. |
| `CustomerProfileViewModelTests` | (already `using …Localization;`). `LoadAsync_Failure_WithoutLogger_…` + `Constructor_ProfileQueryThrows_…` → `Strings.Common_ActionFailedMessage`; `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` → `+ Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(PiiSecret, sut.ErrorMessage)`. |

No new test file, no new stub, no DI change. Every failure path was already exercised by an existing test.

**Subset run:** `Dashboard` + `Analytics` + `Salons` + `QrCodes` + `Support` + `CustomerProfile` → **78 / 78 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `71fb472` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Full test suite | 2,715+ | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain | 456 | 456 ✅ |
| — **Presentation** | 772 | **772** (assertion updates — no net-new) ✅ |
| — Application | 791 | 791 ✅ |
| — Infrastructure | 609 | 609 ✅ |
| — Shell | 80 | 80 ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-6 subset | — | **78 / 78 PASS** ✅ |

Suite progression: 2,715 (`71fb472`) → **2,715** (P2 sub-wave 6 — assertion updates, no net-new tests).

`grep -rn "= exception.Message" src/…/ViewModels/` → **only** the 2 excluded `SettingsPageViewModel` Category-D `NotSupportedException` sites remain. **All 58 Category-A `= exception.Message` UI surfaces across the app are now sanitized — the P2 track's Category-A scope is complete pending commit.**

---

## STOP

Phase 8.125 implementation complete. Base HEAD `71fb472` unchanged (no commit). Build 0/0, **2,715 / 2,715** tests pass, Architecture 7/7, sub-wave-6 subset 78/78.

**9 Category-A sites / 6 VMs sanitized** — `DashboardPageViewModel.LoadAsync`, `AnalyticsPageViewModel.LoadAsync`, `SalonPageViewModel` (`LoadAsync` / `CreateSalonAsync`), `QrCodesPageViewModel` (`LoadAsync` / `GenerateReceptionInviteAsync`), `SupportPageViewModel` (`SubmitMessageAsync` / `SubmitApplicationAsync`), `CustomerProfileViewModel.LoadAsync`. 7 plain catches (variable dropped) + 2 filtered Support catches (`when` clause byte-unchanged, only the assignment swapped, FQ `Localization.Strings.` form). The `#pragma CA1031` pairs, every `State = Error`, every operation-name-only log call, the 2 `finally` blocks, the QR `Salon is null` guard, and the Support success-path form-clears are byte-unchanged. `+ using …Localization;` in 3 prod + 4 test files. **No `.resx` / DI / service / contract / stub change.** +0 net tests. **Three confirmed live test-documented leaks closed** (Dashboard backend body, Salon "Validation failed", QR "Forbidden"); revenue / analytics / salon config / invite tokens / applicant PII / customer PII no longer reach any UI surface.

**The 2 `SettingsPageViewModel` `NotSupportedException` Category-D branches were excluded per the audit (local fixed developer string — no untrusted data).**

**With sub-wave 6, all 58 Category-A `= exception.Message` UI surfaces are sanitized — the P2 track completes on commit.**

**Awaiting Phase 8.126 — Sub-Wave 6 Commit Scope Review.**
