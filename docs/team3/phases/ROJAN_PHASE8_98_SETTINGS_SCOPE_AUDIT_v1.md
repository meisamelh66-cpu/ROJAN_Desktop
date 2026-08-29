# ROJAN AI — TEAM 3 — PHASE 8.98 — SETTINGS PAGE CARVE-OUT — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / guard / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `7c9c132`
**Reference:** `ROJAN_PHASE8_97_WAVE_G_P2_INFRA_SCOPE_AUDIT_v1.md` §G (this is the recommended low-cost carve-out)
**Recommendation: Option A — implement, LOW risk. 1 prod file (Presentation only), ~4 additive stub seams, ~8 tests, no DI change, 0–1 XAML lines.**

---

## A. GIT STATE

```
git rev-parse HEAD        → 7c9c13229c8fdebfea65744a1a80c300997efcbd
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty)
git status (tracked)      → clean
```

Baseline (checkpoint §E, `7c9c132`): **2,701 / 2,701** — Domain 456, Presentation 758, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. INVENTORY — `SettingsPageViewModel`

`src/Rojan.Desktop.Presentation/ViewModels/Settings/SettingsPageViewModel.cs`

| Attribute | Value |
|---|---|
| Declaration | `public sealed class SettingsPageViewModel : ViewModelBase` — **needs `sealed partial class`** for a `[LoggerMessage]` |
| Construction | **DI `AddTransient<SettingsPageViewModel>()`** (`ServiceCollectionExtensions.cs:69`) — unlike the other 3 Wave G VMs |
| `ILogger` field | ❌ **none** |
| `[LoggerMessage]` | ❌ **none** |
| Error surfaces | ✅ **3, section-scoped, all already XAML-bound, all `private set`:** `StatusMessage` (Language section, `SettingsPage.xaml:134`), `ThemeStatusMessage` (Theme section, `:229`), `ApiEnvironmentStatusMessage` (API section, `:386`). All currently written **on the success path only** (restart-required localized string, or `string.Empty`). |
| Existing `try`/`catch` | ⚠️ **partial** — `DownloadOrInstallAsync` and `RemovePackAsync` each `catch (NotSupportedException exception) { StatusMessage = exception.Message; }` (the Phase 19A "not available yet" path; the message is a static developer string) |
| `CancellationToken` | ❌ **none** — no method accepts or threads a token |
| ctor fire-and-forget | `_ = RefreshAvailablePacksAsync();` (last line of ctor) |

### B.1 Async user-triggered methods

| # | Method | Command | Backing call | Local / backend | Guarded? | Surface today |
|---|---|---|---|---|---|---|
| 1 | `ApplyLanguageAsync` | `ApplyLanguageCommand` (CanExecute: `SelectedLanguage is not null`) | `_localizationService.SetLanguageAsync(code)` | **local** (settings persistence) | ❌ | `StatusMessage` (success only) |
| 2 | `ApplyThemeAsync` | `ApplyThemeCommand` | `_themeService.SetThemeModeAsync(mode)` | **local** (settings / registry) | ❌ | `ThemeStatusMessage` (success only) |
| 3 | `ApplyApiEnvironmentAsync` | `ApplyApiEnvironmentCommand` | `_apiEnvironmentService.SetEnvironmentAsync(env, url)` | **local** (settings file) | ❌ | `ApiEnvironmentStatusMessage` (success only) |
| 4 | `RefreshAvailablePacksAsync` | `RefreshAvailablePacksCommand` **+ ctor `_ =`** | `_packRepository.GetAvailableLanguagePacksAsync()` | **local** (always-empty catalog, Phase 19A) | ❌ | none |
| 5 | `DownloadOrInstallAsync` | `DownloadOrInstallCommand` | `_packRepository.DownloadAndInstallAsync(code)` | **local** (throws `NotSupportedException`) | ⚠️ `NotSupportedException` only | `StatusMessage` |
| 6 | `RemovePackAsync` | `RemovePackCommand` | `_packRepository.RemovePackAsync(code)` | **local** (throws `NotSupportedException`) | ⚠️ `NotSupportedException` only | `StatusMessage` |
| 7 | `SignOutAsync` *(inline lambda `_ => _authenticationService.SignOutAsync()`)* | `SignOutCommand` | `_authenticationService.SignOutAsync()` | **backend / auth** — the only remote call | ❌ | **none** (`SettingsPage.xaml:421` Account card has no bound message) |

### B.2 Sync commands (not in scope)

| Command | Behaviour |
|---|---|
| `RestartCommand` → `Restart()` | `Process.Start(Environment.ProcessPath)` + `Application.Current.Shutdown()` — **deliberately unguarded terminal action**; `Process.Start` can throw `Win32Exception` but the app is shutting down anyway. Leave as-is. |
| `SelectThemeModeCommand`, `SelectApiEnvironmentCommand` | pure in-memory property set |

### B.3 Test scaffolding

`tests/Rojan.Desktop.Presentation.Tests/Settings/` — `SettingsPageViewModelTests.cs` (21 tests) + 5 stub doubles:

| Stub | Failure seam today | Needed for Wave G′ |
|---|---|---|
| `StubLocalizationService` | ✅ **`ThrowOnSetLanguage` already exists** (currently unused) | reuse as-is |
| `StubThemeService` | ❌ | `+ SetThemeModeException` |
| `StubApiEnvironmentService` | ❌ | `+ SetEnvironmentException` |
| `StubAuthenticationService` (Settings-local) | ❌ (`SignOutAsync` just bumps `SignOutCallCount`) | `+ SignOutException` |
| `StubLanguagePackRepository` | `DownloadAndInstallAsync` / `RemovePackAsync` already throw `NotSupportedException`; `GetAvailableLanguagePacksAsync` returns the catalog | `+ GetAvailableLanguagePacksException` (+ optionally a non-`NotSupportedException` mode for `DownloadAndInstallAsync`) |

No `RecordingLogger` SUT variant exists yet for this file — one `CreateLoggedSut` helper would be added (or the logger passed inline, as the Automation tests do).

---

## C. CLASSIFICATION

| Category | Members |
|---|---|
| **A — user-triggered action needing hardening** | `ApplyLanguageAsync` (1), `ApplyThemeAsync` (2), `ApplyApiEnvironmentAsync` (3), `SignOutAsync` (7); **broaden** `DownloadOrInstallAsync` (5) / `RemovePackAsync` (6) to also catch the non-`NotSupportedException` case |
| **B — read-only / local / deliberate skip** | `RefreshAvailablePacksAsync` (4) — read-only, surfaces nothing; a failure today = empty list + crash dialog. Low value, but it is **also a ctor fire-and-forget** (unobserved-task exception path), so a cheap log-only guard is worth including. `RestartCommand` — terminal, skip. Sync selectors — skip. |
| **C — already guarded (partial)** | `DownloadOrInstallAsync` / `RemovePackAsync` — the `catch (NotSupportedException)` branch stays (its message is a static, non-sensitive developer string); a second general branch is added |
| **D — cancellation-sensitive** | **none** — `SettingsPageViewModel` threads no `CancellationToken`; no polling / background loops. The filtered `when (exception is not OperationCanceledException)` shape is **recommended for consistency** with Waves A–F (and because the service methods accept a token a future caller could pass), **not mandatory**. |

---

## D. SECURITY

| Vector | Sensitivity | Mitigation |
|---|---|---|
| **API production URL** (`ProductionUrlInput` → `SetEnvironmentAsync`) | **infra-sensitive** — an internal endpoint | `catch (Exception)` **no variable bound**; `LogOperationFailed(nameof(ApplyApiEnvironmentAsync))` — URL never read into the log or the UI string |
| Sign-out / auth errors (`SignOutAsync`) | backend body / token-clear failure detail | no-variable `catch`; operation-name-only log; UI (if surfaced) = generic constant only |
| Language code / theme mode | low | operation-name-only regardless |
| Pack metadata | catalog is always empty (Phase 19A); `NotSupportedException.Message` is a static string already surfaced today | keep the existing `NotSupportedException` branch; the **general** branch must use `Common_ActionFailedMessage`, not `exception.Message` |
| User preferences generally | low | not logged |

**Safe-logging requirement (identical to Waves A–F):**
- `catch (Exception)` / `catch (Exception exception) when (…)` — **no exception object passed to the logger**.
- One instance-form `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Settings page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);`
- Call `LogOperationFailed(nameof(<Method>))` — never a URL, code, mode, token, or exception message.
- UI: `Localization.Strings.Common_ActionFailedMessage` (shipped Wave A `794648e`, all 3 locale files) — no `.resx` change.
- Tests seed a `SECRET` URL / message into the thrown exception and assert `Assert.DoesNotContain(SECRET, entry.Message)` **and** the surface equals `Common_ActionFailedMessage`.

---

## E. CANCELLATION

| Check | Finding |
|---|---|
| `CancellationToken` usage in the VM | **none** — no method signature takes one; all service calls pass `default` implicitly |
| Background / polling paths | **none** |
| Fire-and-forget | one: `_ = RefreshAvailablePacksAsync();` in the constructor |
| Filtered catch required? | **No** (no cancellation can occur). **Recommended** anyway — use `catch (Exception exception) when (exception is not OperationCanceledException)` on all new guards for shape-consistency with Waves A–F and future-safety (the `ILocalizationService` / `IThemeService` / `IApiEnvironmentService` / `IAuthenticationService` / `ILanguagePackRepository` methods all accept a `CancellationToken`). |

No `OperationCanceledException` may become a status message or a log line.

---

## F. ARCHITECTURE

| Concern | Finding | Cost |
|---|---|---|
| Reuse existing `ILogger`? | **no logger exists** — add `ILogger<SettingsPageViewModel>? logger = null` as an optional 6th ctor param; `_logger = logger ?? NullLogger<SettingsPageViewModel>.Instance;` — **exact `InventoryPageViewModel` / `HrPageViewModel` pattern** (Phase 8.19). Class → `sealed partial`. | LOW |
| Need `[LoggerMessage]`? | **yes** — one instance-form declaration (see §D) | LOW |
| Need DI change? | **NO.** `AddTransient<SettingsPageViewModel>()` is unchanged — the container auto-fills `ILogger<SettingsPageViewModel>` (logging is registered); the `= null` default is for tests only. | none |
| `SYSLIB1020` | not a risk — single `ILogger` field + instance-form `[LoggerMessage]` | — |
| `ILoggerFactory` | **not needed** — `SettingsPageViewModel` has no child ViewModels | — |
| Error surface | **reuse the 3 existing section-scoped `*StatusMessage` properties** for methods 1/2/3/5/6 → **no new property, no XAML change** for those. For `SignOutAsync` (method 7) there is **no existing surface** — see §G for the 3 options. |
| `using` additions | `using Microsoft.Extensions.Logging;` + `using Microsoft.Extensions.Logging.Abstractions;` |
| Test complexity | ~4 additive stub `Exception?` seams (1 already exists); ~8 new tests in the existing file; add one `CreateLoggedSut` helper (or pass a `RecordingLogger` inline) | LOW–MEDIUM |

---

## G. RECOMMENDATION

### Verdict — **Option A: implement as a LOW-risk carve-out (Phase 8.99).**

`SettingsPageViewModel` is the one Wave G target that is DI-registered, already has (section-scoped) error surfaces, and lives entirely in the `Presentation` project — so the disproportionate cost that made §8.97 defer the other 3 VMs does not apply here.

### Guard mapping

| Method | New guard shape | Surface on failure |
|---|---|---|
| `ApplyLanguageAsync` | `try { <body> } catch (Exception exception) when (exception is not OperationCanceledException) { StatusMessage = Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(ApplyLanguageAsync)); }` | existing `StatusMessage` |
| `ApplyThemeAsync` | same | existing `ThemeStatusMessage` |
| `ApplyApiEnvironmentAsync` | same | existing `ApiEnvironmentStatusMessage` |
| `RefreshAvailablePacksAsync` | same, **log-only** (no surface exists; keep it minimal) | none — log only |
| `DownloadOrInstallAsync` / `RemovePackAsync` | keep `catch (NotSupportedException exception) { StatusMessage = exception.Message; }`, **add** a following `catch (Exception exception) when (exception is not OperationCanceledException) { StatusMessage = Strings.Common_ActionFailedMessage; LogOperationFailed(nameof(<Method>)); }` | existing `StatusMessage` |
| `SignOutAsync` | convert the inline lambda `_ => _authenticationService.SignOutAsync()` to a named `private async Task SignOutAsync()` method with the guard | **see options below** |

**`SignOutAsync` surface — pick one:**
- **G-1 (recommended): add a minimal `AccountStatusMessage` string property (`private set`) + one `<TextBlock Text="{Binding AccountStatusMessage}" …>` in the Account card (`SettingsPage.xaml` ~line 431).** Consistent with the 3 existing section-scoped surfaces; ~1 XAML line + ~4 VM lines.
- **G-2 (zero-XAML): log-only** — guard `SignOutAsync`, `LogOperationFailed`, no UI feedback. Conservative; a failed sign-out silently no-ops (the button can be pressed again).
- **G-3 (not recommended): reuse the top `StatusMessage`** — wrong section, would flash a message under the Language card.

### Implementation plan (Phase 8.99)

| Item | Detail |
|---|---|
| **Prod files** | `src/Rojan.Desktop.Presentation/ViewModels/Settings/SettingsPageViewModel.cs` (1) — `sealed partial`, `+ ILogger<SettingsPageViewModel>? logger = null` ctor param, `+ [LoggerMessage]`, 6 method bodies wrapped, `SignOutCommand` lambda → named method. **+ `src/Rojan.Desktop.Presentation/Views/Settings/SettingsPage.xaml` (1 line)** only if option **G-1** is chosen. |
| **Stub files** | `StubThemeService.cs` (`+ SetThemeModeException`), `StubApiEnvironmentService.cs` (`+ SetEnvironmentException`), `StubAuthenticationService.cs` [Settings] (`+ SignOutException`), `StubLanguagePackRepository.cs` (`+ GetAvailableLanguagePacksException`, + optional non-`NotSupportedException` mode) — 4 files, all additive null-path-identical. `StubLocalizationService.ThrowOnSetLanguage` reused as-is. |
| **Test file** | `SettingsPageViewModelTests.cs` — **+~8 tests**: `ApplyLanguage` / `ApplyTheme` / `ApplyApiEnvironment` / `SignOut` / `RefreshAvailablePacks` failure → generic surface (where applicable) + single op-only `Error` log + `DoesNotContain(SECRET)`; `DownloadOrInstall` non-`NotSupported` failure → generic `StatusMessage`; a `SECRET`-in-production-URL leak test on `ApplyApiEnvironmentAsync`; success-clears (e.g. `ApplyLanguageAsync` failure then success). 21 existing tests unchanged. |
| **DI** | none |
| **Estimated methods guarded** | **6** (4 fully new + 2 broadened) |
| **Tests** | ~8 |
| **Risk** | **LOW** — 1–2 prod files (Presentation only), no DI change, reuses existing surfaces (0–1 XAML lines), stub seams additive, established `ILogger?`-optional-param + instance-`[LoggerMessage]` pattern (Phase 8.19), no `CancellationToken` complexity, no `SYSLIB1020` |
| **Expected suite delta** | 2,701 → **~2,709** (+8); Presentation 758 → ~766 |
| **Build** | expected 0 warnings / 0 errors |
| **Architecture** | 7 / 7 (no new type dependencies) |
| **Commit subject (Phase 8.9x)** | `fix(desktop): guard settings page command failures` |

### After this carve-out

The Missing-Guard Sweep can be **formally closed** (Waves A–F + Settings). `WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel` remain a documented **P3** item (local-only persistence, non-destructive, Shell-project cost). The "sanitize load-error surfacing" P2 (Reporting ×3, AiCenter ×2, ~10 Automation-tab `= exception.Message`) remains a separate backlog phase.

---

## STOP

Phase 8.98 audit complete. HEAD `7c9c132`, tracked tree clean, baseline 2,701 / 2,701.
`SettingsPageViewModel` (DI-registered, Presentation-only, 3 existing section-scoped status surfaces, no `ILogger`, no `CancellationToken`, 2 partial `NotSupportedException` guards) is a clean LOW-risk carve-out: **6 methods** (`ApplyLanguageAsync`, `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `SignOutAsync`, + broaden `DownloadOrInstallAsync` / `RemovePackAsync`), reuse the existing `*StatusMessage` surfaces (0–1 XAML lines for the `SignOutAsync` case), add the standard `ILogger<T>?`-optional-param + instance `[LoggerMessage]` pattern (no DI change), ~4 additive stub seams, **~8 tests**, suite ~2,701 → ~2,709.

**Recommendation: Option A — implement at Phase 8.99. Awaiting authorization.**
