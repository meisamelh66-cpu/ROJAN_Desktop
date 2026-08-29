# ROJAN AI — TEAM 3 — PHASE 8.97 — MISSING-GUARD SWEEP — WAVE G (P2 INFRA) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / guard / service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `7c9c132` (`fix(desktop): guard remaining automation tab command failures`)
**Reference:** `ROJAN_PHASE8_96_AUTOMATION_COMMIT_REPORT_v1.md`, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`, `ROJAN_PHASE8_64_*` §E.2 (Wave G was scoped here as P2)
**Recommendation: Option B — DEFER (with a small optional carve-out for `SettingsPageViewModel`).**

---

## A. GIT STATE

```
git rev-parse HEAD        → 7c9c13229c8fdebfea65744a1a80c300997efcbd
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
git status (tracked)      → clean
```

Untracked: only `ROJAN_*.md` reports. Post-Wave-F baseline (checkpoint §E, `7c9c132`): **2,701 / 2,701** — Domain 456, Presentation 758, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. INVENTORY

The Wave G targets (`ROJAN_PHASE8_64_*` §E.2 "Wave G — P2 infra"):

| VM | File | Constructed by | Has `ILogger`? | Has `[LoggerMessage]`? | Error surface? | Any `try`/`catch`? |
|---|---|---|---|---|---|---|
| `WorkspaceHostViewModel` | `ViewModels/Workspaces/WorkspaceHostViewModel.cs` | `new` in `Rojan.Desktop.Shell/MainWindowViewModel.cs:136` (not DI) | ❌ **none** | ❌ | ❌ **none** | ❌ **none** |
| `NotificationCenterViewModel` | `ViewModels/Notifications/NotificationCenterViewModel.cs` | `new` in `MainWindowViewModel.cs:124` (not DI) | ❌ **none** | ❌ | ❌ **none** | ❌ **none** |
| `CommandPaletteViewModel` | `ViewModels/Search/CommandPaletteViewModel.cs` | `new` per-open in `MainWindowViewModel.cs:609` (not DI) | ❌ **none** | ❌ | ❌ **none** | ❌ **none** |
| `SettingsPageViewModel` | `ViewModels/Settings/SettingsPageViewModel.cs` | DI `AddTransient` (`ServiceCollectionExtensions.cs:69`) | ❌ **none** | ❌ | ✅ `StatusMessage` / `ThemeStatusMessage` / `ApiEnvironmentStatusMessage` (private-set) | ⚠️ **partial** — `catch (NotSupportedException)` in `DownloadOrInstallAsync` / `RemovePackAsync` only |

> The phase brief names a fourth VM "`SettingsCommandPaletteViewModel`" — no such class exists. Interpreted as the two separate VMs `SettingsPageViewModel` + `CommandPaletteViewModel`, matching `ROJAN_PHASE8_64_*` §E.2.

### B.0 Backing stores — all **local**, none backend/HTTP

| Service | Concrete impl | Nature |
|---|---|---|
| `IWorkspaceService` → `IWorkspaceRepository` | `Infrastructure/Workspaces/LocalWorkspaceStore.cs` | local file persistence |
| `INotificationService` → `INotificationRepository` | `Infrastructure/Notifications/LocalNotificationRepository.cs` | local |
| `ISearchHistoryStore` / `ISearchFavoritesStore` | `Infrastructure/Search/LocalSearchHistoryStore.cs` / `LocalSearchFavoritesStore.cs` | local |
| `IThemeService` / `ILocalizationService` / `IApiEnvironmentService` | local settings persistence | local |
| `ILanguagePackRepository` | always-empty catalog (Phase 19A "do not connect to servers") — every op throws `NotSupportedException` | local stub |
| **`IAuthenticationService.SignOutAsync()`** | **the one genuine remote/auth call** in the whole Wave G surface | backend |

Failure modes for Wave G are therefore **disk I/O errors, serialization faults, file locks** — not backend 4xx/5xx bodies. Exception: `SignOutCommand`.

### B.1 `WorkspaceHostViewModel` — async user-triggered methods

| Command / trigger | Method | Ends with | Token? |
|---|---|---|---|
| `SplitRightCommand` / `SplitDownCommand` (+ per-leaf `PaneLeafViewModel.SplitRight/DownCommand`) | `SplitAsync` | `await SaveAsync()` | ❌ |
| `CloseFocusedTabCommand` | `CloseFocusedTabAsync` → `CloseTabAsync` | `await SaveAsync()` | ❌ |
| per-tab `closeCommand` / Outline close | `CloseTabAsync`, `CloseOutlineEntryAsync` | `await SaveAsync()` | ❌ |
| per-leaf `closePaneCommand` | `ClosePaneAsync` | `await SaveAsync()` | ❌ |
| `FloatOutFocusedTabCommand` / per-tab `floatOutCommand` | `FloatOutFocusedTabAsync`, `FloatOutAsync` | `await SaveAsync()` | ❌ |
| per-leaf `addTabCommand` | `OpenModuleInPaneAsync` | `await SaveAsync()` | ❌ |
| split `resize` command | `ResizeSplitAsync` | `await SaveAsync()` | ❌ |
| `ToggleDockPanelCommand` / `OutlinePanel.toggleVisibilityCommand` | `ToggleDockPanelAsync` | `await SaveAsync()` | ❌ |
| `ConfirmNameCommand` | `ConfirmNameAsync` → `RenameWorkspaceAsync` / `CreateWorkspaceAsync` | `await RefreshWorkspaceListsAsync()` | ❌ |
| `SwitchWorkspaceItemCommand` | `SwitchWorkspaceAsync` → `SwitchWorkspaceAsync` (service) | `PrimaryModuleChangeRequested` | ❌ |
| `DuplicateWorkspaceCommand` | `DuplicateWorkspaceAsync` | `RefreshWorkspaceListsAsync` | ❌ |
| `DeleteWorkspaceCommand` | `DeleteWorkspaceAsync` → `DeleteWorkspaceAsync` + `GetActiveWorkspaceAsync` | `RefreshWorkspaceListsAsync` | ❌ |
| `ResetWorkspaceCommand` | `ResetWorkspaceAsync` | `RefreshWorkspaceListsAsync` | ❌ |
| (startup) | `InitializeAsync(string)` — `EnsureInitializedAsync` + `RefreshWorkspaceListsAsync` | — | ❌ |
| fire-and-forget | `_ = SaveAsync()` (in `SetPrimaryModuleId`, `FocusTab`, `OnFloatingWindowClosed`); `_ = CloseOutlineEntryAsync(...)` (Outline ctor) | — | ❌ |
| helper | `SaveAsync` (`SaveLayoutAsync`), `RefreshWorkspaceListsAsync` (`GetWorkspacesAsync` + `GetRecentWorkspacesAsync`) | — | ❌ |

Sync RelayCommands (`CycleTabNext/Previous`, `ToggleSwitcher`, `StartCreate`, `StartRename`, `CancelNameEdit`) are in-memory — not in scope. **~14 distinct async user-triggered methods; no `CancellationToken` anywhere.**

### B.2 `NotificationCenterViewModel`

| Command / trigger | Method | Backing call | Token? |
|---|---|---|---|
| `MarkAllReadCommand` | `MarkAllReadAsync` | `_notificationService.MarkAllAsReadAsync()` | ❌ |
| `ClearAllCommand` | `ClearAllAsync` | `_notificationService.ClearAllAsync()` | ❌ |
| row `MarkAsRead` callback | `MarkAsReadAsync` | `_notificationService.MarkAsReadAsync(id)` | ❌ |
| row `Dismiss` callback | `DismissAsync` | `_notificationService.DismissAsync(id)` | ❌ |
| `IsSilentModeEnabled` setter | `_ = _notificationService.SetSilentModeEnabledAsync(value)` (fire-and-forget) | ✅ | ❌ |
| `SelectedSeverityFilter` / `SearchText` / `IsShowingUnreadOnly` setters; `OnServiceStateChanged` / `OnNotificationRaised` events | `_ = RefreshAsync()` (fire-and-forget) | `GetAllAsync` | ✅ (`RefreshAsync(CancellationToken)`, but callers pass none) |
| (startup) | `InitializeAsync(CancellationToken)` | `GetIsSilentModeEnabledAsync` + `RefreshAsync` | ✅ |

**~6 async user-triggered methods. `CancellationToken` is threaded through `InitializeAsync` / `RefreshAsync`.**

### B.3 `CommandPaletteViewModel`

| Command / trigger | Method | Backing call | Token? |
|---|---|---|---|
| `ExecuteSelectedCommand` | `ExecuteSelectedAsync` → `ExecuteAsync` | `_historyStore.RecordSearchAsync` + `GetRecentSearchesAsync`; then `NavigateTo` **or** `command.Execute(null)` (a `MainWindowViewModel` command) | ❌ (but `ExecuteAsync` itself no token) |
| `ExecuteResultCommand` | `ExecuteAsync` | as above | ❌ |
| `ClearHistoryCommand` | `ClearHistoryAsync` | `_historyStore.ClearAsync()` | ❌ |
| row `ToggleFavorite` callback | `ToggleFavoriteAsync` | `_favoritesStore.ToggleFavoriteAsync` + `GetFavoriteIdsAsync` + `RefreshFavoriteResultsAsync` | uses `CancellationToken.None` |
| `SearchText` setter | `_ = RefreshResultsAsync()` (fire-and-forget) | `GetAllCandidatesAsync` (`_searchIndexService.GetCandidatesAsync`) | ✅ (`RefreshResultsAsync(CancellationToken)`) |
| (startup) | `InitializeAsync(CancellationToken)` | `GetFavoriteIdsAsync` + `GetRecentSearchesAsync` + `RefreshFavoriteResultsAsync` | ✅ |
| helper | `GetAllCandidatesAsync(CancellationToken)`, `RefreshFavoriteResultsAsync(CancellationToken)` | ✅ | ✅ |

**~7 async user-triggered methods. `CancellationToken` threaded through Initialize / Refresh / candidate loads.**

### B.4 `SettingsPageViewModel`

| Command | Method | Backing call | Guarded? | Surface |
|---|---|---|---|---|
| `ApplyLanguageCommand` | `ApplyLanguageAsync` | `_localizationService.SetLanguageAsync` (local) | ❌ | `StatusMessage` (success only) |
| `ApplyThemeCommand` | `ApplyThemeAsync` | `_themeService.SetThemeModeAsync` (local) | ❌ | `ThemeStatusMessage` (success only) |
| `ApplyApiEnvironmentCommand` | `ApplyApiEnvironmentAsync` | `_apiEnvironmentService.SetEnvironmentAsync` (local) | ❌ | `ApiEnvironmentStatusMessage` (success only) |
| `SignOutCommand` | `_authenticationService.SignOutAsync()` (direct lambda) | **auth / backend** | ❌ | none |
| `RefreshAvailablePacksCommand` (+ ctor `_ = RefreshAvailablePacksAsync()`) | `RefreshAvailablePacksAsync` | `_packRepository.GetAvailableLanguagePacksAsync` (empty catalog) | ❌ | none |
| `DownloadOrInstallCommand` | `DownloadOrInstallAsync` | `_packRepository.DownloadAndInstallAsync` | ⚠️ `catch (NotSupportedException)` only → `StatusMessage = exception.Message` (static "coming soon" string) | `StatusMessage` |
| `RemovePackCommand` | `RemovePackAsync` | `_packRepository.RemovePackAsync` | ⚠️ `catch (NotSupportedException)` only → `StatusMessage = exception.Message` | `StatusMessage` |
| `RestartCommand` | `Restart()` (sync) | `Process.Start` + `Application.Current.Shutdown()` | ❌ (deliberately — terminal action) | — |

**~7 async user-triggered methods; 2 partially guarded (`NotSupportedException` only). No `CancellationToken`.**

### B.5 Test scaffolding today

All 4 VMs have a Presentation test file (`WorkspaceHostViewModelTests`, `NotificationCenterViewModelTests`, `CommandPaletteViewModelTests`, `SettingsPageViewModelTests`). Their stub doubles (`FakeWorkspaceRepository`, `StubNotificationService`, `StubSearchHistoryStore`, `StubSearchFavoritesStore`, `StubGlobalSearchIndexService`, + Settings stubs) have **no failure-injection seams** (`grep Exception|Throw` → 0) and **no `RecordingLogger` SUT variant** (the VMs have no logger). Wave G would add all of that.

---

## C. CLASSIFICATION

| Category | Members | Notes |
|---|---|---|
| **A — user-triggered mutation that would benefit from a guard** | `WorkspaceHostViewModel`: `Split/CloseTab/ClosePane/FloatOut/OpenModuleInPane/Resize/ToggleDockPanel` (all end in `SaveAsync`), `ConfirmName`, `SwitchWorkspace`, `DuplicateWorkspace`, `DeleteWorkspace`, `ResetWorkspace`, `SaveAsync` itself · `NotificationCenterViewModel`: `MarkAllReadAsync`, `ClearAllAsync`, `MarkAsReadAsync`, `DismissAsync`, `SetSilentModeEnabledAsync` (setter) · `CommandPaletteViewModel`: `ExecuteAsync`, `ClearHistoryAsync`, `ToggleFavoriteAsync` · `SettingsPageViewModel`: `ApplyLanguageAsync`, `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `SignOutAsync`, `DownloadOrInstallAsync` (broaden), `RemovePackAsync` (broaden) | **~22 methods** — but **all write to local stores** except `SettingsPageViewModel.SignOutAsync` (auth/backend) |
| **B — read-only / background** | all `InitializeAsync` (×4), `RefreshAsync` / `RefreshResultsAsync` / `RefreshFavoriteResultsAsync` / `RefreshWorkspaceListsAsync` / `RefreshAvailablePacksAsync`, `GetAllCandidatesAsync`, and every event-handler-driven `_ = RefreshAsync()` | failures here just leave a stale list; lowest value to guard |
| **C — already guarded** | `SettingsPageViewModel.DownloadOrInstallAsync` / `RemovePackAsync` — **partial** (`catch (NotSupportedException)` only; a non-`NotSupportedException` still propagates) | not a general guard |
| **D — cancellation-sensitive** | `NotificationCenterViewModel` (`InitializeAsync` / `RefreshAsync`) and `CommandPaletteViewModel` (`InitializeAsync` / `RefreshResultsAsync` / `GetAllCandidatesAsync` / `RefreshFavoriteResultsAsync`) **thread `CancellationToken`**. `WorkspaceHostViewModel` and `SettingsPageViewModel` do **not**. | any Wave G guard on the token-using VMs MUST use the filtered `when (exception is not OperationCanceledException)` shape |

---

## D. SECURITY

Content that *could* be exposed if Wave G is implemented carelessly:

| Surface | Sensitive content |
|---|---|
| Notification payloads | notification `Title` / `Message` / `Body` — may carry customer names, booking references, invoice numbers, business events |
| Workspace metadata | user-authored workspace names; module ids; pane-tree structure |
| User preferences | language code, theme mode, and **the API production URL** (`ProductionUrlInput` → `SetEnvironmentAsync`) — infra-sensitive |
| Command / search arguments | free-text `SearchText` the user typed into the palette — may contain a customer name, phone fragment, etc. |
| Backend exception details | `SignOutAsync` failures; any disk-I/O exception path/message from the Local* stores |

**Current leak risk: zero** — none of these 4 VMs logs anything today, and 3/4 surface nothing to the UI.

**Safe-logging requirements for any Wave G implementation** (same discipline as Waves A–F):
- `catch (Exception)` **with no exception variable bound** — `Exception.Message` / `.ToString()` / stack / inner must be structurally unreachable.
- One operation-name-only `[LoggerMessage(EventId = 1, Level = Error, Message = "… Operation={Operation}")]` per VM; call `LogOperationFailed(nameof(Method))` — never pass a notification DTO, workspace name, search query, URL, or id.
- UI (where a surface is added): a fixed localized constant (`Strings.Common_ActionFailedMessage`) — never `exception.Message`. Also broaden `SettingsPageViewModel`'s two `catch (NotSupportedException) { StatusMessage = exception.Message; }` — the `NotSupportedException.Message` there is a **static, non-sensitive "coming soon" string** so it is acceptable *today*, but a general catch must not widen that to arbitrary exception messages.
- Sentinel-enforced tests: seed a `SECRET` notification body / workspace name / search query / URL and assert `DoesNotContain`.

---

## E. CANCELLATION

| VM | `CancellationToken` today | Fire-and-forget paths | Needs `when (exception is not OperationCanceledException)`? |
|---|---|---|---|
| `WorkspaceHostViewModel` | **none** (no method accepts a token) | `_ = SaveAsync()` ×3, `_ = CloseOutlineEntryAsync(...)` | not strictly required (no token can be cancelled), but the **filtered shape is still recommended** for consistency with Waves A–F and future-safety |
| `NotificationCenterViewModel` | `InitializeAsync(CancellationToken)`, `RefreshAsync(CancellationToken)` (callers currently pass `default`) | `_ = RefreshAsync()` ×5, `_ = SetSilentModeEnabledAsync(value)` | **YES** — filtered catch required |
| `CommandPaletteViewModel` | `InitializeAsync` / `RefreshResultsAsync` / `GetAllCandidatesAsync` / `RefreshFavoriteResultsAsync` all take a token | `_ = RefreshResultsAsync()` | **YES** — filtered catch required |
| `SettingsPageViewModel` | **none** | `_ = RefreshAvailablePacksAsync()` (ctor) | not strictly required; filtered shape still recommended |

**Conclusion:** Wave G guards must default to the filtered `catch (Exception exception) when (exception is not OperationCanceledException)` shape everywhere (mandatory for the two token-using VMs, recommended for the other two). No `OperationCanceledException` may become a UI error or a log line — identical to Wave F's requirement.

The fire-and-forget setter/event paths (`_ = RefreshAsync()`, `_ = SaveAsync()`) are the **highest-value** guard targets: an exception on a discarded `Task` there is an unobserved-task exception that currently escapes to `App.DispatcherUnhandledException`.

---

## F. ARCHITECTURE

| Concern | Finding | Cost |
|---|---|---|
| `ILogger` availability | **none of the 4 has one.** `SettingsPageViewModel` is DI `AddTransient` → `ILogger<SettingsPageViewModel>` auto-resolves with just a ctor-param add. The other 3 are `new`'d in `Rojan.Desktop.Shell/MainWindowViewModel` (lines 124 / 136 / 609), which **itself has no `ILogger`** — so giving them a real logger requires **injecting `ILoggerFactory` into `MainWindowViewModel` (a Shell-project ctor change) and forwarding** at each `new` site, or accepting `ILogger<T>? = null` params that production never populates (→ `NullLogger` always → the log is dead). | **HIGH for 3/4** (crosses into the Shell project + construction-site plumbing); LOW for `SettingsPageViewModel` |
| `[LoggerMessage]` | each VM would need `partial` + one instance-form declaration | LOW |
| `ILoggerFactory` requirement | **yes**, for the 3 Shell-constructed VMs (no parent logger to hand down) unless DI-registering them | MEDIUM–HIGH |
| Error surface | 3/4 have **none** — need a new bindable `ErrorMessage`/`HasError` (or `ActionErrorMessage`) property **+ XAML binding** in `WorkspaceHostPanelView` / `NotificationCenterPanelView` / `CommandPaletteView`. `SettingsPageViewModel` already has the `*StatusMessage` family. | MEDIUM for 3/4; LOW for Settings |
| DI impact | `SettingsPageViewModel`: 1 ctor param (auto-resolved). The other 3: none if kept `new`'d — but then no real logger. Registering them in DI is a larger refactor of the deliberate "constructed by its opener" pattern (documented in each class's XML doc). | MEDIUM |
| `SYSLIB1020` | not a risk (all single-`ILogger` + instance-form) — only relevant once a logger exists | — |
| Test complexity | ~6 stub doubles need new `Exception?` seams; 3/4 test files need a new `RecordingLogger`-based SUT variant built from scratch (no `CreateLoggedSut` exists for them). ~25–35 new tests. | MEDIUM–HIGH |

**This is materially more expensive than any of Waves A–F**, every one of which reused an existing injected `ILogger` + an existing inline error surface and touched only the `Presentation` project.

---

## G. RECOMMENDATION

### Priority verdict — **P2 infrastructure cleanup, NOT a P1 command-failure-UX gap**

- Every Wave G operation except `SettingsPageViewModel.SignOutCommand` writes to a **local** store (`LocalWorkspaceStore`, `LocalNotificationRepository`, `LocalSearchHistoryStore`, `LocalSearchFavoritesStore`, local settings). Failure modes are disk I/O / serialization / file-lock — low-frequency, **non-destructive** (in-memory structural state survives; the op retries on the next trigger), already recovered by `App.DispatcherUnhandledException`. **No P0, no data loss.**
- The Missing-Guard Sweep's stated goal — guard user-triggered **backend-connected** command failures so they surface in-page — is **already met by Waves A–F**: every backend-talking domain page (Customers, Services, Specialists, HR, Inventory, Accounting, Organization, Reporting, Export, AI Center, Automation ×3) is closed.
- The only genuine backend/auth call in the Wave G surface is `SettingsPageViewModel.SignOutCommand`, and sign-out failure is itself low-stakes (the user retries; local session-clear paths exist).

### Recommended path — **Option B: DEFER `WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel` to the P2 hardening backlog**, with an **optional small carve-out**:

**Optional Wave G′ (micro) — `SettingsPageViewModel` only**, if the engagement wants the sweep formally closed on the low-cost half:
- **Files:** `SettingsPageViewModel.cs` (1 prod) + 1–2 Settings stub doubles (`Exception?` seams) + `SettingsPageViewModelTests.cs` (1 test) = **~3–4 files, Presentation project only.**
- **Methods (~6):** guard `ApplyLanguageAsync`, `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `SignOutAsync`; broaden the two `catch (NotSupportedException)` in `DownloadOrInstallAsync` / `RemovePackAsync` to also catch the general case with the generic string (keep the `NotSupportedException` → static-message branch).
- **Logger:** add `ILogger<SettingsPageViewModel>` (DI auto-resolves) + `partial` + one instance `[LoggerMessage]`.
- **Surface:** reuse the existing `StatusMessage` / `ThemeStatusMessage` / `ApiEnvironmentStatusMessage` (per section) with `Strings.Common_ActionFailedMessage` on failure — **no new property, no XAML change.**
- **Shape:** `catch (Exception exception) when (exception is not OperationCanceledException)` (defensive; no token today).
- **Risk: LOW.** **Test estimate: ~8.** Suite ~2,701 → ~2,709.
- Explicitly **accept `WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel` as P3** — the Shell-project construction-site cost + new-error-surface + XAML + ~6 stub-seam work is disproportionate to guarding non-destructive local-persistence failures.

### If Option A (full Wave G) is nonetheless authorised — estimates

| | |
|---|---|
| **Prod files** | 4 VMs + `Rojan.Desktop.Shell/MainWindowViewModel.cs` (inject `ILoggerFactory`, forward at 3 `new` sites) + 3 XAML views (new error-surface binding) — **~8 files across 2 projects** |
| **Test files** | 4 VM test files + ~6 stub doubles (`FakeWorkspaceRepository`, `StubNotificationService`, `StubSearchHistoryStore`, `StubSearchFavoritesStore`, `StubGlobalSearchIndexService`, Settings stubs) + new `RecordingLogger` SUT builders — **~10 files** |
| **Methods guarded** | ~22 Category-A (of ~34 async user-triggered total) |
| **Tests** | ~25–35 |
| **Suite** | ~2,701 → ~2,730 |
| **Risk** | **MEDIUM** — first sweep wave to touch the Shell project; new bindable error surfaces + XAML; construction-site logger plumbing; deliberate "constructed by opener" pattern is perturbed. Would want its own audit → scope-review → implement → commit-review → commit cycle, likely split (Workspace / Notification+Palette / Settings). |

### Suggested next phase

**Phase 8.98 — Wave G′ (SettingsPageViewModel micro) implementation** (Option B carve-out), OR formally close the Missing-Guard Sweep at Wave F and move `WorkspaceHost` / `NotificationCenter` / `CommandPalette` + the "sanitize load-error surfacing" P2 (Reporting ×3, AiCenter ×2, ~10 Automation-tab `= exception.Message`) into a named P2/P3 backlog document.

---

## STOP

Phase 8.97 audit complete. HEAD `7c9c132`, tracked tree clean, baseline 2,701 / 2,701.
Wave G targets — `WorkspaceHostViewModel`, `NotificationCenterViewModel`, `CommandPaletteViewModel`, `SettingsPageViewModel` — carry **~34 async user-triggered methods (~22 Category-A)**, but **all backing stores are local** (only `SettingsPageViewModel.SignOutCommand` is backend/auth), failures are non-destructive and already recovered, and **none of the 4 VMs has an `ILogger` or (3/4) an error surface** — 3 are `new`'d in the Shell project's `MainWindowViewModel`, which itself has no logger. This is **P2 infra, not a P1 gap**.
**Recommendation: Option B — defer.** Optional low-cost carve-out: a `SettingsPageViewModel`-only micro-wave (~3–4 Presentation files, ~6 methods, ~8 tests, LOW risk, reuses the existing `*StatusMessage` surfaces). Accept the other 3 VMs as P3.

**Awaiting Phase 8.98 authorization.**
