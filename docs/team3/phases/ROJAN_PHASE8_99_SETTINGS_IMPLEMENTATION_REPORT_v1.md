# ROJAN AI — TEAM 3 — PHASE 8.99 — SETTINGS PAGE COMMAND HARDENING — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.100 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `7c9c132` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_98_SETTINGS_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 7

```
 src/Rojan.Desktop.Presentation/ViewModels/Settings/SettingsPageViewModel.cs        | 106 ++++++++++---
 src/Rojan.Desktop.Presentation/Views/Settings/SettingsPage.xaml                    |   8 ++
 tests/Rojan.Desktop.Presentation.Tests/Settings/SettingsPageViewModelTests.cs      | 159 +++++++++++++++++++
 tests/Rojan.Desktop.Presentation.Tests/Settings/StubApiEnvironmentService.cs       |   8 ++
 tests/Rojan.Desktop.Presentation.Tests/Settings/StubAuthenticationService.cs       |   8 ++
 tests/Rojan.Desktop.Presentation.Tests/Settings/StubLanguagePackRepository.cs      |  18 ++-
 tests/Rojan.Desktop.Presentation.Tests/Settings/StubThemeService.cs                |   8 ++
 7 files changed, 291 insertions(+), 24 deletions(-)
```

All within the STRICT SCOPE allowance: 1 prod VM + optional `SettingsPage.xaml` (SignOut surface) + `SettingsPageViewModelTests.cs` + 4 Settings-local test doubles. **Not touched:** `WorkspaceHostViewModel`, `NotificationCenterViewModel`, `CommandPaletteViewModel`, `MainWindowViewModel`, Shell infra, DI registration, auth/backend contracts, shared localization (`Strings.cs` / `.resx`), any other ViewModel.

### A.1 `SettingsPageViewModel.cs`

- `public sealed class` → **`public sealed partial class`**.
- `+ using Microsoft.Extensions.Logging;` / `+ using Microsoft.Extensions.Logging.Abstractions;`
- `+ private readonly ILogger<SettingsPageViewModel> _logger;`
- ctor: `+ ILogger<SettingsPageViewModel>? logger = null` (6th param, optional — **no breaking change, no DI change**); `_logger = logger ?? NullLogger<SettingsPageViewModel>.Instance;` — the `InventoryPageViewModel` / `HrPageViewModel` pattern (Phase 8.19).
- `+ private string _accountStatusMessage = string.Empty;` + `public string AccountStatusMessage { get; private set; }` (new section-scoped surface for `SignOutAsync`).
- `SignOutCommand` lambda `_ => _authenticationService.SignOutAsync()` → `_ => SignOutAsync()` (new named method).
- `+ [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Settings page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);`
- 6 method bodies guarded (§B).

### A.2 `SettingsPage.xaml` (SignOut surface only, per audit option G-1)

- `+ <converters:CollectionToVisibilityConverter x:Key="CollectionToVisibilityConverter" />` in `UserControl.Resources` (the converter already exists — `Converters/CollectionToVisibilityConverter.cs`; `string.Length == 0 → Collapsed`).
- `+` one `<TextBlock Text="{Binding AccountStatusMessage}" … Visibility="{Binding AccountStatusMessage, Converter=…}" />` in the Account `DashboardCard`, styled identically to the 3 existing `*StatusMessage` TextBlocks (warning brush, caption style, wrap). Shows only when non-empty.

### A.3 Test doubles — 4, all additive `Exception?` seams (null-path byte-identical)

| Stub | Added |
|---|---|
| `StubThemeService` | `Exception? SetThemeModeException` → `SetThemeModeAsync` faults, no state change |
| `StubApiEnvironmentService` | `Exception? SetEnvironmentException` → `SetEnvironmentAsync` faults, no state change |
| `StubAuthenticationService` (Settings) | `Exception? SignOutException` → `SignOutAsync` faults **after** `SignOutCallCount++`, auth state unchanged |
| `StubLanguagePackRepository` | `Exception? GetAvailableLanguagePacksException`; `Exception? PackMutationException` → `DownloadAndInstallAsync` / `RemovePackAsync` fault with a **non-`NotSupportedException`** to exercise the new general catch |

`StubLocalizationService.ThrowOnSetLanguage` (pre-existing) — not needed this phase (`ApplyLanguageAsync` is out of scope).

---

## B. GUARD COVERAGE

**6 methods guarded — exactly the phase METHODS list** (`ApplyLanguageAsync` is *not* in this phase's list and was **not** touched):

| # | Method | Command | Shape | Surface on failure |
|---|---|---|---|---|
| 1 | `ApplyThemeAsync` | `ApplyThemeCommand` | whole body wrapped: `try { … } catch (Exception exception) when (exception is not OperationCanceledException) { ThemeStatusMessage = Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(ApplyThemeAsync)); }` | `ThemeStatusMessage` |
| 2 | `ApplyApiEnvironmentAsync` | `ApplyApiEnvironmentCommand` | whole body wrapped (incl. the `productionUrl` local — **scoped inside the `try` so the URL is unreachable from the catch**) | `ApiEnvironmentStatusMessage` |
| 3 | `RefreshAvailablePacksAsync` | `RefreshAvailablePacksCommand` (+ ctor `_ =`) | whole body wrapped | `StatusMessage` |
| 4 | `DownloadOrInstallAsync` | `DownloadOrInstallCommand` | **`catch (NotSupportedException)` kept** (static "coming soon" message, no log) + **new** `catch (Exception exception) when (…)` after it → generic message + log | `StatusMessage` |
| 5 | `RemovePackAsync` | `RemovePackCommand` | same as #4 | `StatusMessage` |
| 6 | `SignOutAsync` | `SignOutCommand` (lambda → named method) | `try { await _authenticationService.SignOutAsync(); AccountStatusMessage = string.Empty; } catch (…) { AccountStatusMessage = Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(SignOutAsync)); }` | **`AccountStatusMessage`** (new) |

`RestartCommand` (`Process.Start` + `Application.Current.Shutdown`) — deliberately unguarded (terminal), unchanged. Sync selectors — unchanged.

### State safety (verified against the diff)

| Concern | Result |
|---|---|
| Settings partially committed on failure | **No** — every guarded method throws at the single `await _service.X(…)` call; the follow-on `OnPropertyChanged` calls and the success-path status assignment do not run. |
| Theme selection preserved | `SelectedThemeMode` is a separate user-set property never touched by `ApplyThemeAsync`; on failure it keeps the user's pick (test-asserted). |
| API env / URL selection preserved | `SelectedApiEnvironment` / `ProductionUrlInput` untouched on failure (test-asserted). |
| Sign-out failure leaves auth state consistent | `SignOutAsync` never mutates auth state itself — the service owns `CurrentState`. On a faulted `SignOutAsync()` the stub leaves `CurrentState == Authenticated`; the VM only shows `AccountStatusMessage` (test-asserted). The button stays enabled → the user can retry. |
| `RefreshAvailablePacksAsync` (ctor fire-and-forget) | on failure `AvailableLanguagePacks` is untouched (the throw is at `GetAvailableLanguagePacksAsync`, before `.Clear()`); the ctor still completes. |

---

## C. SECURITY

| Vector | Result |
|---|---|
| **API production URL** (`ProductionUrlInput` / `SetEnvironmentAsync`) → log | **unreachable** — `productionUrl` is a local declared *inside* the `try`; the `catch` binds no exception variable to the logger and calls `LogOperationFailed(nameof(ApplyApiEnvironmentAsync))` only. |
| API URL → UI | **prevented** — `ApiEnvironmentStatusMessage = Strings.Common_ActionFailedMessage` (fixed constant), never `exception.Message`. Test `ApplyApiEnvironmentCommand_Failure_ShowsGenericError_DoesNotLeakUrl_PreservesSelection` seeds the URL as both `ProductionUrlInput` and the exception message and asserts `DoesNotContain` in the surface + the single log entry. |
| Sign-out / auth backend detail → log or UI | **unreachable** — no exception variable to the logger; UI = generic constant. |
| Language code / theme mode / pack metadata → log | **unreachable** — `{Operation}` is a compile-time `nameof(...)` string; no argument carries a code/mode. |
| `NotSupportedException.Message` (Download/Remove) | still surfaced to `StatusMessage` on the **`NotSupportedException` branch only** — it is a **static developer string** ("… not available yet - Phase 19A ships the framework only."), non-sensitive, and unchanged from prior behaviour. The **new general branch** uses the generic constant. |

Every failure test seeds the `Secret` sentinel (`"https://internal-vpn.rojan.local/api-SECRET-token"`) and asserts `Assert.DoesNotContain(Secret, entry.Message)` and the surface equals `Strings.Common_ActionFailedMessage`.

---

## D. LOGGING

| Check | Result |
|---|---|
| Pattern | single `ILogger<SettingsPageViewModel>` field + **instance-form** `[LoggerMessage]` — the `InventoryPageViewModel` idiom |
| `[LoggerMessage]` | `EventId = 1`, `Level = LogLevel.Error`, `Message = "Settings page operation failed. Operation={Operation}"` |
| Operation values | `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `DownloadOrInstallAsync`, `RemovePackAsync`, `SignOutAsync` — all `nameof(...)` |
| `ILoggerFactory` | not introduced (no child ViewModels) |
| DI change | **none** — `AddTransient<SettingsPageViewModel>()` unchanged; the container auto-supplies `ILogger<SettingsPageViewModel>`; the `= null` default serves tests |
| `SYSLIB1020` | not triggered (one `ILogger` + instance-form) — **build 0 warnings / 0 errors** |
| ctor break | none — the new param is optional and last |

---

## E. TESTS

**+9** in `SettingsPageViewModelTests.cs` (`Presentation.Tests` 758 → 767; the file 17 → 26). All 17 pre-existing tests unchanged and green. New `LoggedSut` record + `CreateLoggedSut()` + `AssertSingleErrorFor()` helpers added (mirrors the Automation-tab test style).

| Test | Dimension |
|---|---|
| `ApplyThemeCommand_Failure_ShowsGenericError_PreservesSelection_LogsOperationOnly` | failure no-throw · `ThemeStatusMessage == Common_ActionFailedMessage` · `SelectedThemeMode` preserved · single `Error` log `Operation=ApplyThemeAsync` · no `Secret` |
| `ApplyThemeCommand_SuccessAfterFailure_ClearsGenericError` | fail → generic; clear seam; retry → `ThemeStatusMessage == Settings_Theme_RestartRequired` (success overwrites) |
| `ApplyApiEnvironmentCommand_Failure_ShowsGenericError_DoesNotLeakUrl_PreservesSelection` | **URL non-leak** (surface + log) · `SelectedApiEnvironment` / `ProductionUrlInput` preserved · single log `Operation=ApplyApiEnvironmentAsync` |
| `RefreshAvailablePacksCommand_Failure_ShowsGenericError_LogsOperationOnly` | `StatusMessage == Common_ActionFailedMessage` · single log |
| `DownloadOrInstallCommand_UnexpectedFailure_ShowsGenericError_LogsOperationOnly` | non-`NotSupportedException` → generic + log; not the secret |
| `DownloadOrInstallCommand_NotSupported_KeepsStaticMessage_DoesNotLog` | `NotSupportedException` branch → non-empty `StatusMessage`, **not** the generic constant, **zero** log entries |
| `RemovePackCommand_UnexpectedFailure_ShowsGenericError_LogsOperationOnly` | as Download |
| `SignOutCommand_Failure_ShowsAccountError_LeavesAuthStateConsistent_LogsOperationOnly` | no-throw · `AccountStatusMessage == Common_ActionFailedMessage` · `SignOutCallCount == 1` · `CurrentState == Authenticated` (unchanged) · single log `Operation=SignOutAsync` · no `Secret` |
| `SignOutCommand_Success_LeavesAccountStatusEmpty` | `AccountStatusMessage == ""` · `CurrentState == SignedOut` · zero log entries |

**Cancellation:** no dedicated test — `SettingsPageViewModel` threads no `CancellationToken`, so no `OperationCanceledException` can be produced; the filtered `when (exception is not OperationCanceledException)` clause is present on all 6 guards for consistency/future-safety (verifiable in the diff).

---

## F. VALIDATION

| Gate | Expected | Actual (working tree = `7c9c132` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,709 | **2,710 / 2,710 PASS** ✅ |
| — Domain | 456 | 456 |
| — Application | 791 | 791 |
| — **Presentation** | +9 → 767 | **767** ✅ |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| `SettingsPageViewModelTests` subset | — | **26 / 26 PASS** ✅ |

Suite progression: 2,701 (`7c9c132`) → **2,710** (+9, Settings page carve-out).

---

## G. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 7 files, all within the STRICT SCOPE allowance (1 prod VM + 1 XAML for the SignOut surface + 1 test file + 4 Settings-local stubs) |
| Base HEAD | `7c9c132` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,710 / 2,710; Architecture 7 / 7 |
| Guards | ✅ 6/6 (phase list) — filtered catch, generic constant, operation-name-only log, existing method bodies verbatim, `NotSupportedException` branches preserved |
| Security | ✅ API URL / auth detail unreachable from log and UI; sentinel-enforced |
| Logging | ✅ single `ILogger` + instance `[LoggerMessage]`; no `ILoggerFactory` / DI / ctor-break; no `SYSLIB1020` |
| DI | ✅ unchanged |
| Line endings | working-copy files are CRLF; `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only |
| **Known limitation (documented for the commit review)** | the 3 pre-existing surfaces (`StatusMessage` / `ThemeStatusMessage` / `ApiEnvironmentStatusMessage`) have XAML visibility triggers gated on `Is*RestartRequired == True`, so on **failure** those TextBlocks stay `Collapsed` even though the VM property now holds the generic message — the value is set (test-verified) and the crash dialog + log leak are prevented, but there is **no visual feedback** for Theme / API / pack-refresh failures until those 3 triggers are broadened to "non-empty string" (behaviour-equivalent for the success path, and would also fix the **pre-existing** latent invisibility of the Download/Remove "coming soon" message). `AccountStatusMessage` (new) uses a non-empty converter and **does** display. Recommend a tiny XAML follow-up (or fold into 8.100 review). |
| Proposed commit subject | `fix(desktop): guard settings page command failures` |
| Proposed staged files | the 7 above — **no `git add -A` / `git add .`** |

---

## STOP

Phase 8.99 implementation complete. Base HEAD `7c9c132` unchanged (no commit). Build 0/0, **2,710 / 2,710** tests pass, Architecture 7/7.
6 `SettingsPageViewModel` command methods guarded (`ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `DownloadOrInstallAsync`, `RemovePackAsync`, `SignOutAsync`) with the filtered-cancellation shape, generic `Common_ActionFailedMessage` surface (reusing the 3 existing `*StatusMessage` properties + one new `AccountStatusMessage` for SignOut), and operation-name-only logging via a new single-`ILogger` + instance `[LoggerMessage]` — **no DI change, no ctor break**. `ApplyLanguageAsync` was outside this phase's method list and was not touched. +9 tests, +4 additive stub seams.

**Awaiting Phase 8.100 — Settings Page Commit Scope Review.**
