# ROJAN AI — TEAM 3 — PHASE 8.89 — MISSING-GUARD SWEEP — WAVE E (AI CENTER) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source change. No test change. No guard added. No service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `6f64ffa95a99cd1cdea7acbbf37afb0f63dd04b8`
**Objective:** Audit the AI Center ViewModels and identify the remaining unguarded user-triggered command-failure boundaries, using the Wave A–D pattern (`794648e`, `a5be831`, `66c8490`, `525fd4b`, + `5640123` / `6f64ffa`).

---

## A. GIT STATE

```
git rev-parse HEAD        → 6f64ffa95a99cd1cdea7acbbf37afb0f63dd04b8
git branch --show-current → feature/team3-desktop-completion
git status --porcelain | grep -vE '^\?\? ROJAN_.*\.md$'   → (empty)
```

| Check | Result |
|---|---|
| HEAD | `6f64ffa` (Export Dialog micro-phase commit) ✅ |
| Branch | `feature/team3-desktop-completion` ✅ |
| Tracked working tree | **clean** ✅ |
| Untracked | only `ROJAN_*.md` reports |
| Last 3 commits | `6f64ffa` guard report export · `5640123` guard reporting · `525fd4b` guard organization |

Baseline test suite (checkpoint §E, `6f64ffa`): **2,678 / 2,678** — Domain 456, Application 791, Presentation 735, Infrastructure 609, Shell 80, Architecture 7.

---

## B. AI CENTER INVENTORY

The AI Center domain has exactly **one ViewModel**: `src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs`.
(`AI/AiCenterSection.cs` is a plain enum; there is no profile / dialog / child ViewModel.)

- Already `sealed partial`; a **single** `ILogger<AiCenterPageViewModel>` field; an instance-form operation-name-only `[LoggerMessage(EventId = 1, Level = Error, Message = "AI Center page operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` (Phase 8.23 Wave 2B) with a doc comment that explicitly forbids logging "the exception, its message, the user's chat text, AI responses, or any backend/token detail." **No logging-infrastructure change is needed** — Wave E only adds call sites.
- Composes 13 Application services (`IAIService`, `IBusinessHealthService`, `ISummaryEngine`, `INotificationInsightService`, `IInsightEngine`, `IRecommendationEngine`, `IConversationManager`, `IAIHistoryService`, `IPromptTemplateRepository`, `IAIConfigurationService`, `IAISettingsService`, `ITokenUsageTracker`, `ILocalizationService`). **Fake-backed** — the whole AI feature is `Mock`-provider / in-memory (`Fake*` / `Mock*`); backend has no AI code. **P1 — UX consistency**, not P0.

### B.1 Every user-triggered command

| # | Command → method | Kind | Current exception handling | Error/Status surface used | User impact today on failure |
|---|---|---|---|---|---|
| 1 | `LoadCommand` → `LoadAsync` | `AsyncRelayCommand` (also ctor-time `_ = LoadAsync()`) | **guarded** — top-level `catch (Exception exception)` → `ErrorMessage = exception.Message` + `State = Error` + `LogOperationFailed(nameof(LoadAsync))` | `State` / `ErrorMessage` (destructive) | already recovered; `ErrorMessage` shows raw `exception.Message` (P2 leak — §D) |
| 2 | `SelectSectionCommand` → (lambda) | `RelayCommand`, sync — `SelectedSection = (AiCenterSection)param` | n/a — cannot fail | — | none |
| 3 | `SendMessageCommand` → `SendMessageAsync` | `AsyncRelayCommand` (chat send; `IsSending` gate) | **guarded** — `catch (Exception exception)` → `StatusMessage = exception.Message` + `LogOperationFailed(nameof(SendMessageAsync))`; `finally { IsSending = false; }`. The Phase 8.23 chat-text-non-leak test is bound to this method. | `StatusMessage` | already recovered; `StatusMessage` shows raw `exception.Message` (P2 leak — §D) |
| 4 | `NewConversationCommand` → `NewConversationAsync` | `AsyncRelayCommand` | **NONE** — `await _conversationManager.CreateSessionAsync("New conversation")` → set `CurrentSessionId`/`CurrentSessionTitle` → `Messages.Clear()` → `await ReloadSessionsAsync()` → `SelectedSection = Chat` | none | **generic `App.DispatcherUnhandledException` dialog** on a failed "New conversation" |
| 5 | `OpenConversationCommand` → `OpenConversationAsync(session)` | `AsyncRelayCommand` (param `ConversationSessionDto`) | **NONE** — set `CurrentSessionId`/`Title` → `await LoadMessagesAsync()` (reads `_conversationManager.GetMessagesAsync`) → `SelectedSection = Chat` | none | generic dialog on a failed open (message load) |
| 6 | `TogglePinCommand` → `TogglePinAsync(session)` | `AsyncRelayCommand` | **NONE** — `await _conversationManager.TogglePinAsync(session.Id)` → `await ReloadSessionsAsync()` | none | generic dialog on a failed pin/unpin |
| 7 | `DeleteSessionCommand` → `DeleteSessionAsync(session)` | `AsyncRelayCommand` | **NONE** — `await _conversationManager.DeleteSessionAsync(session.Id)` → conditional `CurrentSessionId = null` / `Messages.Clear()` → `await ReloadSessionsAsync()` → conditional `await EnsureActiveSessionAsync()` | none | generic dialog on a failed session delete |
| 8 | `SearchHistoryCommand` → `SearchHistoryAsync` | `AsyncRelayCommand` (expression-bodied) | **NONE** — `ReplaceCollection(SearchResults, await _aiHistoryService.SearchAsync(SearchText))` | none | generic dialog on a failed history search |
| 9 | `ClearHistoryCommand` → `ClearHistoryAsync` | `AsyncRelayCommand` | **NONE** — `await _conversationManager.ClearHistoryAsync()` → `CurrentSessionId = null` / `Messages.Clear()` → `await ReloadSessionsAsync()` → `await EnsureActiveSessionAsync()` | none | generic dialog on a failed **destructive** clear-all-history |
| 10 | `ExportSessionCommand` → `ExportSessionAsync(session)` | `AsyncRelayCommand` (expression-bodied) | **NONE** — `ExportPreviewText = await _conversationManager.ExportSessionAsync(session.Id)` | `ExportPreviewText` (holds the exported conversation text) | generic dialog on a failed export |
| 11 | `SaveSettingsCommand` → `SaveSettingsAsync` | `AsyncRelayCommand` | **NONE** — build `AISettingsDto` → `ApplySettings(await _settingsService.UpdateSettingsAsync(updated))` → `StatusMessage = "Settings saved."` | `StatusMessage` (success only) | generic dialog on a failed settings save |
| 12 | `SaveConfigurationCommand` → `SaveConfigurationAsync` | `AsyncRelayCommand` | **NONE** — `Configuration = await _configurationService.SetConfigurationAsync(SelectedProviderType, ModelIdInput, IsProviderEnabled)` → `StatusMessage = "Model configuration saved."` | `StatusMessage` (success only) | generic dialog on a failed model/provider config save |

### B.2 Private helpers (covered transitively — no separate guard)

| Helper | Unguarded? | Unguarded callers | Covered by guarding… |
|---|---|---|---|
| `ReloadSessionsAsync` | yes | `NewConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `ClearHistoryAsync` (all Category A) — plus `LoadAsync` / `SendMessageAsync` (already guarded) | commands #4/6/7/9 |
| `EnsureActiveSessionAsync` | yes | `DeleteSessionAsync`, `ClearHistoryAsync` — plus `LoadAsync` (guarded) | commands #7/9 |
| `LoadMessagesAsync` | yes | `OpenConversationAsync` — plus `EnsureActiveSessionAsync` (covered above) | command #5 |

Guarding the **9 Category-A commands** leaves no unguarded call path into any of these three helpers.

### B.3 The "~12" reconciliation

`ROJAN_PHASE8_64_*` §D listed AI Center as "~12" and enumerated `ReloadSessionsAsync` / `EnsureActiveSessionAsync` / `LoadMessagesAsync` alongside the commands. The true count of **command methods to guard is 9** (#4–#12 above); the three private helpers are covered transitively (§B.2). `LoadAsync` (#1) and `SendMessageAsync` (#3) are already guarded and out of scope.

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — backend-connected mutation/action needing a guard** | `NewConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `ClearHistoryAsync`, `SaveSettingsAsync`, `SaveConfigurationAsync` (**mutations**); `OpenConversationAsync`, `SearchHistoryAsync`, `ExportSessionAsync` (**reads triggered by a user click that currently crash on failure**) | **guard in Phase 8.90 — 9 methods** |
| **B — read-only** | — (the three "read" commands in A are *user-triggered actions* that need the guard; there is no pure background read command) | — |
| **C — already guarded** | `LoadAsync` (top-level `State = Error` + log), `SendMessageAsync` (`catch (Exception)` → `StatusMessage` + log; `finally IsSending = false`) | **do not modify** |
| **D — global-handler acceptable** | `SelectSectionCommand` (sync, cannot fail) | — |

No Category D among the 9 — each is a P1 UX-consistency gap (a failed pin / delete / clear-history / save-settings / save-config / export / open / search / new-conversation click should surface inline, not throw a modal system dialog).

---

## D. SECURITY

AI Center is a **high-sensitivity** surface: **the user's chat prompts (`ChatInputText`), the AI's generated responses (`Messages`), the exported conversation transcript (`ExportPreviewText`), business insights / recommendations / health score computed from customer-revenue-employee data, prompt templates, and token-usage figures.**

### D.1 The 9 new guards — no exposure

| Vector | Finding |
|---|---|
| `Exception.Message` → UI | **not exposed** — `catch (Exception)` binds **no variable** in all 9; `ActionErrorMessage` is only ever `null` or the compile-time constant `Strings.Common_ActionFailedMessage` |
| `Exception.Message` / `.ToString()` → log file | **not exposed** — `LogOperationFailed(string operation)` has **no `Exception` parameter`; `LocalFileLoggerProvider` renders no backend body |
| **Prompt leakage** | **prevented** — no guarded method reads `ChatInputText` / any message text into `ActionErrorMessage` or the logger |
| **Generated-content leakage** (`Messages`, `ExportPreviewText`) | **prevented** — same. `ExportSessionAsync` on failure does **not** set `ExportPreviewText` (the assignment *is* the awaited expression) — no partial transcript is written anywhere |
| Backend exception bodies | **prevented** — no-variable catch |
| Sensitive identifiers (`session.Id`, `CurrentSessionId`, model id) | **not logged** (operation name only), **not shown** (generic string only) |

**Logger receives only:** `Operation=<MethodName>` (`NewConversationAsync` / `OpenConversationAsync` / `TogglePinAsync` / `DeleteSessionAsync` / `SearchHistoryAsync` / `ClearHistoryAsync` / `ExportSessionAsync` / `SaveSettingsAsync` / `SaveConfigurationAsync`) via the existing operation-name-only template.

### D.2 Out of scope (unchanged — "sanitize load-error surfacing" P2)

`LoadAsync` → `ErrorMessage = exception.Message` and `SendMessageAsync` → `StatusMessage = exception.Message` are pre-existing (Phase 8.19 / 8.23). They render an `ApiException` / backend body straight into the UI. This is the same "sanitize load-error surfacing" P2 flagged for `ReportingPageViewModel` — **not touched by Wave E** (Category C — already guarded, do not modify). Given AI Center's chat-content sensitivity, the P2 phase should include these two alongside the Reporting ones.

---

## E. ARCHITECTURE

| Check | Value |
|---|---|
| **Logger availability** | a single `ILogger<AiCenterPageViewModel> _logger` field + instance-form `[LoggerMessage] LogOperationFailed(string operation)` (Phase 8.23). **Reusable as-is** — Wave E adds 9 call sites. |
| **`ILoggerFactory` needs** | **none** — `AiCenterPageViewModel` has no child ViewModel to plumb a factory to; it is a self-logging `AddTransient` page VM |
| **`SYSLIB1020` risk** | **none** — one `ILogger` field + instance-form `[LoggerMessage]` (already compiles clean at `6f64ffa` and every prior wave) |
| **DI impact** | **none** — no constructor change; `ActionErrorMessage` / `HasActionError` are additive private-set properties |
| **Localization** | **no change** — `Strings.Common_ActionFailedMessage` already ships (Wave A `794648e`). There is **no `Ai_SaveError` string**; `SaveSettingsAsync` / `SaveConfigurationAsync` currently use hardcoded English literals (`"Settings saved."` / `"Model configuration saved."`) — a pre-existing localization gap, **out of Wave E scope**. `Common_ActionFailedMessage` is the correct reuse for the failure surface (Wave B/C/D precedent). |

---

## F. TEST PLAN

### F.1 Test-stub seams

`AiCenterPageViewModelTests` drives the VM with the **real** Application services (`ConversationManager`, `AIHistoryService`, `AIConfigurationService`, `AISettingsService`, `TokenUsageTracker`, `PromptTemplateRepository`) over the **Presentation.Tests-local `internal sealed class StubAIRepository : Domain.AI.IAIRepository`** (`tests/…/AI/StubAIRepository.cs` — in-memory, used **only** by `AiCenterPageViewModelTests`). The seams go on that repo (one layer below the VM's service dependencies; the real services propagate the throw):

| Seam (additive `Exception?`) | Covers commands |
|---|---|
| `CreateSessionException` | `NewConversationAsync` |
| `GetMessagesException` | `OpenConversationAsync`, `ExportSessionAsync` |
| `UpdateSessionException` | `TogglePinAsync` |
| `DeleteSessionException` | `DeleteSessionAsync`, `ClearHistoryAsync` |
| `GetSessionsException` | `SearchHistoryAsync`, `ClearHistoryAsync` (reload), `ReloadSessionsAsync`-path assertions |
| `SetSettingsException` | `SaveSettingsAsync` |
| `SetProviderConfigurationException` | `SaveConfigurationAsync` |

**~7 additive `Exception?` seams**, each `X is not null ? Task.FromException : <original>` — null-path byte-identical (the ~20 existing AI Center tests unaffected). No cross-namespace shared stub touched. (The implementation phase will confirm the exact repo method each command hits — `ExportSessionAsync` / `SearchHistoryAsync` / `ClearHistoryAsync` may route through `GetSessionByIdAsync` too, in which case a `GetSessionByIdException` seam replaces / augments the above.)

### F.2 New tests (`AiCenterPageViewModelTests.cs`)

| Category | Tests | Count |
|---|---|---|
| **Failure does not throw + error surfaced** — one per Category-A command: `Record.Exception(() => Cmd.Execute(param))` is `null`; `HasActionError == true`; `ActionErrorMessage == Strings.Common_ActionFailedMessage` | ×9 | 9 |
| **State preservation** | `SaveSettings` failure → `StatusMessage != "Settings saved."` + local settings toggles unchanged; `SaveConfiguration` failure → `StatusMessage != "Model configuration saved."` + `Configuration` unchanged; `DeleteSession` failure → session still in `RecentSessions`; `ClearHistory` failure → history not cleared; `ExportSession` failure → `ExportPreviewText` unchanged (no partial transcript); `State != DashboardState.Error` for all | ~5 |
| **Success clears error** | `SaveSettings` fail → clear seam → succeed → `HasActionError == false` | 1 |
| **No sensitive-data leak** | seed the guarded failure with a sentinel embedding a fake prompt + AI response + customer name; assert `Operation=<Method>` in an `Error` entry and `DoesNotContain(sentinel)` in `logger.Entries` **and** `ActionErrorMessage` (mirrors the Phase 8.23 `SendMessageAsync` chat-text-non-leak test) | 2 |
| **Regression** | the ~20 existing AI Center tests (incl. the `SendMessageAsync` chat-text-non-leak, `LoadAsync` error-state) pass unchanged | (0 new) |

**Estimated new tests: ~17** (9 + ~5 + 1 + 2). Conservative suite projection: **2,678 → ~2,695**.

### F.3 Files changed (Phase 8.90 implementation)

| Group | Files | Count |
|---|---|---|
| Production | `ViewModels/AI/AiCenterPageViewModel.cs` | 1 |
| Test stub | `tests/…/AI/StubAIRepository.cs` | 1 |
| Test | `tests/…/AI/AiCenterPageViewModelTests.cs` | 1 |
| **Total** | | **3** |

No new file, no `Strings.cs` / `.resx` change, no shared-stub change, no new test helper, no ctor / DI / service change.

### F.4 Risk

**LOW.** 9 additive `try`/`catch` around existing awaits + one bindable property pair (no ctor, no DI). Fake-backed / mock-provider domain. `LoadAsync` and `SendMessageAsync` (the two already-guarded, chat-sensitive paths) are not touched. The one design point: `OpenConversationAsync` / `NewConversationAsync` set `CurrentSessionId` (and, at the end, `SelectedSection = Chat`) — see §H.

---

## G. COMMIT STRATEGY

**Recommendation: a single Wave E commit.**

```
fix(desktop): guard AI Center command failures
```

Rationale:
- **One ViewModel** (`AiCenterPageViewModel`), 9 methods, one additive property pair, one identical mechanical `try`/`catch` shape — a single cohesive change.
- Matches `ROJAN_PHASE8_64_*` §D ("**one commit** — `fix(desktop): guard AI Center command failures`") and the Wave A–D cadence.
- A split (e.g. "session ops" vs "settings/config" vs "history") would fragment one file's changes across three commits with no isolation or bisection benefit and shared `ActionErrorMessage` plumbing repeated.

Standard rhythm: 8.90 implementation (STOP before commit) → 8.91 commit scope review → 8.92 commit execution → checkpoint update.

---

## H. PHASE 8.90 RECOMMENDATION

**PHASE 8.90 — MISSING-GUARD SWEEP — WAVE E (AI CENTER) — IMPLEMENTATION v1**

**Exact scope — modify ONLY:**
- `src/Rojan.Desktop.Presentation/ViewModels/AI/AiCenterPageViewModel.cs`:
  - add `ActionErrorMessage` / `HasActionError` (private-set, `SetProperty`, additive; **no ctor change**)
  - wrap **all 9** Category-A methods (`NewConversationAsync`, `OpenConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `SearchHistoryAsync`, `ClearHistoryAsync`, `ExportSessionAsync`, `SaveSettingsAsync`, `SaveConfigurationAsync`) in `try { …existing body…; ActionErrorMessage = null; HasActionError = false; } catch (Exception) { ActionErrorMessage = Strings.Common_ActionFailedMessage; HasActionError = true; LogOperationFailed(nameof(<Method>)); }` with the established `#pragma warning disable/restore CA1031` boundary comment
  - the two expression-bodied methods (`SearchHistoryAsync`, `ExportSessionAsync`) convert to block bodies
  - `SaveSettingsAsync` / `SaveConfigurationAsync`: keep `StatusMessage = "Settings saved." / "Model configuration saved."` on the **success path only** (inside the `try`, after the await); leave it untouched on failure
  - **`CurrentSessionId` / `SelectedSection` on failure:** do **not** revert (consistent with Wave B/C/D guards, which never revert a selection — the session id that was set is valid; only a downstream read/reload failed, and a retry re-clicks cleanly). `SelectedSection = Chat` stays at the end of `NewConversationAsync` / `OpenConversationAsync`, inside the `try` → it simply does not switch on failure. `EnsureActiveSessionAsync()` calls (in `DeleteSessionAsync` / `ClearHistoryAsync`) stay inside the guarded block.
  - **do not touch** `LoadAsync`, `SendMessageAsync`, `ReloadSessionsAsync` / `EnsureActiveSessionAsync` / `LoadMessagesAsync` bodies (covered transitively), the ctor, or the `[LoggerMessage]` signature
- `tests/Rojan.Desktop.Presentation.Tests/AI/StubAIRepository.cs`: ~7 additive `Exception?` seams (§F.1), null-path byte-identical
- `tests/Rojan.Desktop.Presentation.Tests/AI/AiCenterPageViewModelTests.cs`: ~17 new tests (§F.2); existing tests unchanged

**DO NOT:** change any service / DI / ViewModel constructor / backend contract / RBAC / `CanExecute` / navigation / `IConversationManager` or any AI Application service / `[LoggerMessage]` signature / `Strings.cs` / `.resx` / `LoadAsync` / `SendMessageAsync`. No commit.

**Risk: LOW** (per §F.4).

**Validation expectation:**
- `dotnet build -c Debug` → **0 warnings / 0 errors** (single `ILogger` + instance form → no `SYSLIB1020`; no `CA1031` / `CA1848`).
- Full suite → **~2,695 / ~2,695 PASS** (Presentation 735 → ~752; Domain 456, Application 791, Infrastructure 609, Shell 80 unchanged).
- Architecture tests → **7 / 7 PASS**.
- Deliverable: `ROJAN_PHASE8_90_AI_CENTER_IMPLEMENTATION_REPORT_v1.md`. STOP before commit; wait for Phase 8.91 commit scope review.

**Downstream:** after 8.92 → **Wave F — Automation tabs** (`WorkflowsTabViewModel` ×3, `ScheduledJobsTabViewModel` ×2, `BusinessRulesTabViewModel` ×2 — `ROJAN_PHASE8_64_*` §D / §F, matching the tabs' existing `catch (Exception) when (exception is not OperationCanceledException)` shape) → Wave G (P2 infra). Separately, a "sanitize load-error surfacing" P2 phase should cover `AiCenterPageViewModel`'s `LoadAsync` / `SendMessageAsync` + `ReportingPageViewModel`'s three `= exception.Message` leaks in one pass.

---

## STOP

Phase 8.89 audit complete. HEAD `6f64ffa`, tracked tree clean, baseline 2,678 / 2,678.
The AI Center domain has one ViewModel (`AiCenterPageViewModel`) with **9 unguarded user-triggered command methods** — `NewConversationAsync`, `OpenConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `SearchHistoryAsync`, `ClearHistoryAsync`, `ExportSessionAsync`, `SaveSettingsAsync`, `SaveConfigurationAsync` (the private helpers `ReloadSessionsAsync` / `EnsureActiveSessionAsync` / `LoadMessagesAsync` are covered transitively). `LoadAsync` + `SendMessageAsync` are already guarded (Category C — the `SendMessageAsync` chat-text-non-leak test stays intact). Wave E = one additive `ActionErrorMessage`/`HasActionError` pair + 9 `try`/`catch` reusing the existing operation-name-only `[LoggerMessage]` + `Common_ActionFailedMessage`; no ctor / DI / service / `[LoggerMessage]`-signature / localization-file change; `SYSLIB1020`-safe. ~3 files, ~17 tests, one commit `fix(desktop): guard AI Center command failures`.
**Recommended next: Phase 8.90 — Wave E (AI Center) Implementation.** Awaiting authorization.
