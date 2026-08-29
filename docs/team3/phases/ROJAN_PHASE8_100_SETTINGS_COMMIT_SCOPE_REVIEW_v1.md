# ROJAN AI — TEAM 3 — PHASE 8.100 — SETTINGS PAGE COMMAND HARDENING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No** source / test / new-file / XAML-visibility-fix / commit / push / merge / rebase / amend. Nothing staged.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `7c9c132` (unchanged)
**Reference:** `ROJAN_PHASE8_98_SETTINGS_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_99_SETTINGS_IMPLEMENTATION_REPORT_v1.md`
**Verdict: READY TO COMMIT** at Phase 8.101. The `Is*RestartRequired` visibility gate is a **non-blocking** follow-up.

---

## A. GIT STATE

```
git rev-parse HEAD        → 7c9c13229c8fdebfea65744a1a80c300997efcbd
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
```

### Modified tracked files — 7, all Phase 8.99 / Settings

```
 M src/Rojan.Desktop.Presentation/ViewModels/Settings/SettingsPageViewModel.cs
 M src/Rojan.Desktop.Presentation/Views/Settings/SettingsPage.xaml
 M tests/Rojan.Desktop.Presentation.Tests/Settings/SettingsPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Settings/StubApiEnvironmentService.cs
 M tests/Rojan.Desktop.Presentation.Tests/Settings/StubAuthenticationService.cs
 M tests/Rojan.Desktop.Presentation.Tests/Settings/StubLanguagePackRepository.cs
 M tests/Rojan.Desktop.Presentation.Tests/Settings/StubThemeService.cs
```

Diffstat: `7 files changed, 291 insertions(+), 24 deletions(-)`. The 24 deletions are all reformat-in-place (method bodies moved into a `try`; two `StubLanguagePackRepository` expression bodies turned into `X is not null ? … : <original>` ternaries). Untracked: only `ROJAN_*.md`. **Confirmed: only Phase 8.99 files modified; staging empty.**

---

## B. SCOPE

| Required file | Modified? | Notes |
|---|---|---|
| `SettingsPageViewModel.cs` | ✅ | `sealed partial`, `+ ILogger<T>? logger = null` (6th, optional), `+ AccountStatusMessage`, 6 guards, `SignOutCommand` lambda → named method, `+ [LoggerMessage]` |
| `SettingsPage.xaml` | ✅ | `+ CollectionToVisibilityConverter` resource (converter pre-exists) + one `<TextBlock Text="{Binding AccountStatusMessage}" …>` in the Account card — the SignOut surface authorised by audit option G-1 |
| `SettingsPageViewModelTests.cs` | ✅ | `+ LoggedSut`/`CreateLoggedSut`/`AssertSingleErrorFor` helpers + 9 tests; 17 existing tests unchanged |
| Settings-local test stubs | ✅ | `StubThemeService` (`+ SetThemeModeException`), `StubApiEnvironmentService` (`+ SetEnvironmentException`), `StubAuthenticationService` (`+ SignOutException`), `StubLanguagePackRepository` (`+ GetAvailableLanguagePacksException`, `+ PackMutationException`) — 4 files |

| Must stay untouched | Status |
|---|---|
| Shell infrastructure / `MainWindowViewModel` | ✅ not in diff |
| DI registration (`ServiceCollectionExtensions.cs`) | ✅ not in diff — `AddTransient<SettingsPageViewModel>()` unchanged |
| Authentication contracts (`IAuthenticationService`) | ✅ not in diff — the stub is a test double, the interface is untouched |
| Backend contracts / DTOs | ✅ not in diff |
| `WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel` | ✅ not in diff |
| Any other ViewModel | ✅ not in diff |
| Shared localization (`Strings.cs` / `.resx`) | ✅ not in diff — `Common_ActionFailedMessage` reused as-is |
| `CollectionToVisibilityConverter.cs` | ✅ not modified — only referenced |

**7 files, 100% within the STRICT SCOPE allowance.**

---

## C. GUARDS — 6 reviewed against the diff

| # | Method | Existing body preserved | Failure path safe | Cancellation | Global `State`/`Error` change |
|---|---|---|---|---|---|
| 1 | `ApplyThemeAsync` | ✅ 5 lines verbatim inside `try` | ✅ throw at `SetThemeModeAsync` → `OnPropertyChanged` trio + success `ThemeStatusMessage` skipped; `SelectedThemeMode` (separate property) untouched | ✅ `when (exception is not OperationCanceledException)` | ❌ none — writes only `ThemeStatusMessage` |
| 2 | `ApplyApiEnvironmentAsync` | ✅ verbatim inside `try`, **incl. the `productionUrl` local — now scoped inside the `try` so it is unreachable from the `catch`** | ✅ throw at `SetEnvironmentAsync` → nothing after runs; `SelectedApiEnvironment` / `ProductionUrlInput` untouched | ✅ filtered | ❌ none — `ApiEnvironmentStatusMessage` only |
| 3 | `RefreshAvailablePacksAsync` | ✅ verbatim inside `try` | ✅ throw at `GetAvailableLanguagePacksAsync` (before `.Clear()`) → `AvailableLanguagePacks` untouched; ctor `_ =` call still completes | ✅ filtered | ❌ none — `StatusMessage` only |
| 4 | `DownloadOrInstallAsync` | ✅ `try` body + `catch (NotSupportedException) { StatusMessage = exception.Message; }` **kept verbatim**; new general `catch` **after** it | ✅ `NotSupportedException` still → static message, **no log**; any other exception → generic + log | ✅ filtered on the new catch | ❌ none — `StatusMessage` only |
| 5 | `RemovePackAsync` | ✅ same as #4 | ✅ same | ✅ filtered | ❌ none |
| 6 | `SignOutAsync` | lambda `_ => _authenticationService.SignOutAsync()` → `_ => SignOutAsync()`; new method is `try { await …SignOutAsync(); AccountStatusMessage = ""; } catch { AccountStatusMessage = generic; Log; }` | ✅ no local auth-state mutation; on failure the service owns `CurrentState` (test: stays `Authenticated`); button stays enabled → retry possible | ✅ filtered | ❌ none — new `AccountStatusMessage` only, **not** a global/error state |

Catch body identical across all 6 (modulo method name + surface property):
```csharp
catch (Exception exception) when (exception is not OperationCanceledException)
{
    <SectionStatusMessage> = Strings.Common_ActionFailedMessage;
    LogOperationFailed(nameof(<Method>));
}
```

`RestartCommand` (`Process.Start` + `Application.Current.Shutdown`) — **unchanged**, deliberately unguarded (terminal). `ApplyLanguageAsync` — **not in this phase's method list, not touched** (confirmed: no diff hunk).

**No `DashboardState` / `State = Error` anywhere — `SettingsPageViewModel` has no such concept, and none was added.**

---

## D. LOGGING

| Check | Result |
|---|---|
| `sealed` → `sealed partial` | ✅ correct — required for the source-generated `LogOperationFailed` |
| `ILogger<SettingsPageViewModel>? logger = null` optional injection | ✅ correct — 6th param, optional, last; **no ctor break** (all 21+ existing call sites and the DI registration compile unchanged) |
| `NullLogger` fallback | ✅ `_logger = logger ?? NullLogger<SettingsPageViewModel>.Instance;` — identical to `InventoryPageViewModel` (Phase 8.19) |
| `[LoggerMessage]` operation-only | ✅ `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Settings page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` — the only parameter is a `nameof(...)` string; the caught `exception` is **never passed** |
| `ILoggerFactory` | ✅ **not introduced** — no child ViewModels |
| DI change | ✅ **none** — `AddTransient<SettingsPageViewModel>()` unchanged; container auto-supplies `ILogger<SettingsPageViewModel>` (same as every other DI-registered page VM); the `= null` default is test-only |
| `SYSLIB1020` | ✅ **not triggered** — exactly one `ILogger` field + instance-form `[LoggerMessage]`; **build is 0 warnings / 0 errors** |

---

## E. SECURITY

| Vector | Result |
|---|---|
| **API production URL** (`ProductionUrlInput` → `SetEnvironmentAsync`) → log | **unreachable** — the `productionUrl` local is declared **inside** the `try`; the `catch` binds no exception variable; `LogOperationFailed(nameof(ApplyApiEnvironmentAsync))` carries only the method name |
| API URL → UI | **prevented** — `ApiEnvironmentStatusMessage = Strings.Common_ActionFailedMessage` (fixed constant), never `exception.Message` |
| Authentication / sign-out backend detail → log or UI | **unreachable** — no exception variable to the logger; UI = generic constant (`AccountStatusMessage`) |
| Pack metadata / language code / theme mode → log | **unreachable** — `{Operation}` is a compile-time literal; no DTO/field argument |
| Tokens / secrets | none handled by this VM; the `SignOutAsync` guard never reads the exception |
| `NotSupportedException.Message` (Download/Remove, existing branch) | still surfaced — but it is a **static non-sensitive developer string** ("… not available yet — Phase 19A ships the framework only."), unchanged from prior behaviour; the **new** general branch uses the generic constant |

Requirement satisfied: **`Operation=nameof(Method)`** on every guard. Test-enforced — each failure test seeds `Secret = "https://internal-vpn.rojan.local/api-SECRET-token"` (used as both the exception message and, in the API test, the `ProductionUrlInput`) and asserts `Assert.DoesNotContain(Secret, entry.Message)` + surface `== Strings.Common_ActionFailedMessage`.

---

## F. UX FINDING — `Is*RestartRequired` visibility gate

**Finding:** the 3 pre-existing surface TextBlocks (`StatusMessage` `SettingsPage.xaml:135`, `ThemeStatusMessage` `:230`, `ApiEnvironmentStatusMessage` `:387`) each carry
```xml
<Setter Property="Visibility" Value="Collapsed" />
<DataTrigger Binding="{Binding Is<X>RestartRequired}" Value="True">
    <Setter Property="Visibility" Value="Visible" />
</DataTrigger>
```
so on a **guarded failure** (which sets the `*StatusMessage` property but leaves `Is<X>RestartRequired == false`) the TextBlock stays **`Collapsed`** — the generic message is set on the VM (test-verified) but **not visually shown** for the Theme / API / pack-refresh sections.

**Classification: NON-BLOCKING.**
- The commit's core value is fully delivered: the guard **prevents the global `App.DispatcherUnhandledException` crash dialog**, **prevents the URL/exception leak into the log**, and records an operation-name-only `Error` entry. None of that depends on visibility.
- The `SignOutAsync` path — the one genuine backend/auth call and the highest-value case — **does** display, via the new `AccountStatusMessage` TextBlock with `CollectionToVisibilityConverter` (non-empty → Visible).
- The gap is a pre-existing structural quirk of these 3 TextBlocks, not introduced by this change. It also already hides the Download/Remove `NotSupportedException` "coming soon" message today.
- Per this phase's STRICT MODE ("Do NOT fix XAML visibility"), it is **not** touched here.

**Recommendation: Phase 8.99.1 (tiny XAML follow-up) or fold into the P2 "sanitize load-error surfacing" phase.** Change each of the 3 triggers from `Is<X>RestartRequired == True` to a non-empty-string test on the bound `*StatusMessage` (e.g. the same `CollectionToVisibilityConverter` used for `AccountStatusMessage`). This is **behaviour-equivalent on the success path** (the success code sets the message to the restart string exactly when a restart is required, else `string.Empty`) and additionally surfaces failures **and** fixes the latent Download/Remove invisibility. ~3 TextBlock edits, no VM change, LOW risk.

---

## G. TESTS

| Gate | Expected | Actual (working tree = `7c9c132` + Phase 8.99) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full suite | 2,710 / 2,710 | **2,710 / 2,710 PASS** ✅ |
| — Domain / Application / Infrastructure / Shell | 456 / 791 / 609 / 80 | unchanged ✅ |
| — **Presentation** | 758 → 767 | **767** (+9) ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| `SettingsPageViewModelTests` subset | 26 / 26 | **26 / 26 PASS** ✅ |

Suite progression: 2,691 (`4b1afca`) → 2,701 (`7c9c132`, Wave F) → **2,710** (Settings carve-out, +9).

### Test additivity

9 new `[Fact]`s; **all 17 pre-existing `SettingsPageViewModelTests` unchanged**. The 2 pre-existing `…RepositoryNotSupported…` tests still pass — the `StubLanguagePackRepository` reformat is null-path byte-identical (both seams null → `Task.FromResult(_catalog)` / synchronous `throw new NotSupportedException(...)` exactly as before). 4 stub `Exception?` seams, all default-`null` → original behaviour.

Coverage vs the phase's TASK checklist: failure-does-not-throw ✅ · generic error surface ✅ · success clears error ✅ (`ApplyThemeCommand_SuccessAfterFailure…`, `SignOutCommand_Success…`) · no exception leak ✅ (sentinel) · operation-only logging ✅ · cancellation ✅ (structural — no token exists to cancel; filter present in the diff) · SignOut failure safety ✅ (`…LeavesAuthStateConsistent…`).

---

## H. COMMIT READINESS

| Gate | State |
|---|---|
| Scope | ✅ 7 files, all authorised |
| Base HEAD | `7c9c132` — unchanged; staging empty |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,710 / 2,710; Architecture 7 / 7; Settings 26 / 26 |
| Guards | ✅ 6/6 — existing bodies verbatim, filtered catch, generic constant, no global state |
| Logging | ✅ single `ILogger` + instance `[LoggerMessage]`; no `ILoggerFactory` / DI / ctor-break / `SYSLIB1020` |
| Security | ✅ API URL + auth detail unreachable from log and UI; sentinel-enforced |
| UX finding | ✅ classified non-blocking; follow-up recorded (Phase 8.99.1 / P2) |
| Line endings | working-copy CRLF; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |

### Proposed commit

**Subject:**
```
fix(desktop): guard settings page command failures
```

**Body (suggested):**
```
Wrap the SettingsPage command methods in the established filtered
try/catch so a failing theme / API-environment / language-pack /
sign-out operation surfaces in-page instead of the global crash
dialog.

- ApplyThemeAsync, ApplyApiEnvironmentAsync, RefreshAvailablePacksAsync
- DownloadOrInstallAsync, RemovePackAsync (general branch added after
  the existing NotSupportedException branch)
- SignOutAsync (command lambda promoted to a named method)

Adds an optional ILogger<SettingsPageViewModel> (NullLogger fallback,
no DI change) and a single operation-name-only [LoggerMessage]; failure
sets the section's existing *StatusMessage - or the new
AccountStatusMessage for sign-out - to Strings.Common_ActionFailedMessage
(no exception.Message, no API URL, no State=Error). Additive Exception?
seams on the Settings test doubles; +9 tests.

The 3 pre-existing *StatusMessage TextBlocks are visibility-gated on
Is*RestartRequired; surfacing failure text there is a follow-up.
```

**Trailers (required):**
```
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

### Proposed staging (Phase 8.101 — explicit paths, NO `git add -A` / `git add .`)

```
git add \
  src/Rojan.Desktop.Presentation/ViewModels/Settings/SettingsPageViewModel.cs \
  src/Rojan.Desktop.Presentation/Views/Settings/SettingsPage.xaml \
  tests/Rojan.Desktop.Presentation.Tests/Settings/SettingsPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Settings/StubApiEnvironmentService.cs \
  tests/Rojan.Desktop.Presentation.Tests/Settings/StubAuthenticationService.cs \
  tests/Rojan.Desktop.Presentation.Tests/Settings/StubLanguagePackRepository.cs \
  tests/Rojan.Desktop.Presentation.Tests/Settings/StubThemeService.cs
```

Expected post-commit: new HEAD child of `7c9c132`; `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update (§B commit table, §E 2,701 → 2,710, §G — Missing-Guard Sweep can be declared closed for the backend-connected surface, Settings carve-out done, `WorkspaceHost`/`NotificationCenter`/`CommandPalette` remain P3, + the Phase 8.99.1 XAML follow-up note).

---

## STOP

Phase 8.100 review complete. **Verdict: READY.** HEAD `7c9c132`, staging empty, 7 Settings files modified and nothing else, build 0/0, 2,710/2,710, Architecture 7/7, Settings 26/26. All 6 guards use the filtered-cancellation shape, reuse the section-scoped `*StatusMessage` surfaces (+ new `AccountStatusMessage` for SignOut), surface only the generic constant, preserve existing bodies and the `NotSupportedException` branches, and add no `ILoggerFactory` / DI / contract change. The `Is*RestartRequired` visibility gate is a **non-blocking** follow-up (Phase 8.99.1 / P2).

**Awaiting Phase 8.101 — Settings Page Commit Authorization.**
