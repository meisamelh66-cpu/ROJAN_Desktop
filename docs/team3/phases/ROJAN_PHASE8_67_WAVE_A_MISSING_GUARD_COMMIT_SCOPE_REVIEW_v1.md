# ROJAN AI — TEAM 3 — PHASE 8.67 — MISSING-GUARD SWEEP WAVE A — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No source change. No test change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `5ba554ceb588e5780b87aebdf280538f6b25c485` — `fix(desktop): drop exception payload from diagnostic logging` (Phase 8.61, committed 8.63)
**Scope under review:** Phase 8.66 (Wave A — guard Customer/Service/Specialist write commands) working-tree changes, pending commit.
**Verdict:** ✅ **READY TO COMMIT.** No blocking findings. No business-behaviour change.

---

## A. GIT STATE

| Check | Expected | Actual | Status |
|---|---|---|---|
| HEAD | `5ba554c` | `5ba554ceb588e5780b87aebdf280538f6b25c485` | ✅ |
| Branch | `feature/team3-desktop-completion` | same | ✅ |
| Staged files | none | none (`git diff --cached` empty) | ✅ |
| Tracked code changes | 17 modified | 17 modified, **0 new**, 0 deleted | ✅ |
| Pushed / merged / rebased / amended | none | none | ✅ |
| Unrelated modifications | none | none | ✅ |

`git diff --stat`: **17 files changed, 592 insertions(+), 42 deletions(-)**. All remaining `??` entries are `ROJAN_*.md` reports.

### A.1 The 42 deletions

Every `-` line is an original single-line command body (`await _commandService.X(...); …; await LoadAsync();`) being replaced by its `try { … } catch (Exception) { … }` wrapped form, plus the one `SpecialistPageViewModel.LoadAsync` line whose logger expression was replaced by the new `Logger` helper. **No property, no validation, no service call, no test assertion removed.**

### A.2 Tracked changes (17)

```
 M src/…/Localization/Strings.ar.resx
 M src/…/Localization/Strings.cs
 M src/…/Localization/Strings.en.resx
 M src/…/Localization/Strings.resx
 M src/…/ViewModels/Customers/CustomerPageViewModel.cs
 M src/…/ViewModels/Customers/CustomerProfileViewModel.cs
 M src/…/ViewModels/Services/ServiceProfileViewModel.cs
 M src/…/ViewModels/Specialists/SpecialistPageViewModel.cs
 M src/…/ViewModels/Specialists/SpecialistProfileViewModel.cs
 M tests/…/Customers/CustomerPageViewModelTests.cs
 M tests/…/Customers/CustomerProfileViewModelTests.cs
 M tests/…/Customers/StubCustomerCommandService.cs
 M tests/…/Services/ServiceProfileViewModelTests.cs
 M tests/…/Services/StubServiceCommandService.cs
 M tests/…/Specialists/SpecialistPageViewModelTests.cs
 M tests/…/Specialists/SpecialistProfileViewModelTests.cs
 M tests/…/Specialists/StubSpecialistCommandService.cs
```

---

## B. SCOPE VERIFICATION

### B.1 Production — matches expected exactly

| File | Change | Verdict |
|---|---|---|
| `CustomerPageViewModel.cs` | `+using …Localization`; `+CreateErrorMessage`/`HasCreateError` (new pair, mirrors `ServicePageViewModel`); `CreateCustomerAsync` wrapped | ✅ in scope |
| `CustomerProfileViewModel.cs` | `+using …Localization`; `+SaveErrorMessage`/`HasSaveError` (new pair, mirrors `ServiceProfileViewModel`); `AddNoteAsync`/`AddTagAsync`/`RemoveTagAsync`/`SaveChangesAsync` wrapped; `SaveChangesAsync` catch reverts `EditableStatus` | ✅ in scope |
| `ServiceProfileViewModel.cs` | `AssignSpecialistAsync`/`UnassignSpecialistAsync` wrapped — **reuses existing** `SaveErrorMessage`/`HasSaveError`; no new property; no new `using` | ✅ in scope |
| `SpecialistProfileViewModel.cs` | `AddSkillAsync`/`RemoveSkillAsync` wrapped — **reuses existing** `SaveErrorMessage`/`HasSaveError`; no new property; no new `using` | ✅ in scope |
| `SpecialistPageViewModel.cs` | `+using …Localization`; `+CreateErrorMessage`/`HasCreateError` (new pair); `+private ILogger Logger => …` (**property, not field** — no SYSLIB1020); `LoadAsync`'s existing log call switched to `LogOperationFailed(Logger, …)`; `CreateSpecialistAsync` wrapped | ✅ in scope |

### B.2 Localization — one key, all 3 locale files + the wrapper

| File | Change |
|---|---|
| `Strings.cs` | `+ Common_ActionFailedMessage => Get(nameof(…))` (with doc comment) |
| `Strings.resx` (fa/invariant) | `+ <data name="Common_ActionFailedMessage">` |
| `Strings.en.resx` | `+ <value>The action could not be completed. Please try again.</value>` |
| `Strings.ar.resx` | `+ <value>تعذّر إكمال العملية. يُرجى المحاولة مرة أخرى.</value>` |

Key present in **all 3** locale files → `Strings.Common_ActionFailedMessage` resolves in every culture (test-verified via equality asserts).

### B.3 Test stubs — additive `Exception?` seams only

| Stub | Seams added | Behaviour change |
|---|---|---|
| `StubCustomerCommandService.cs` | `CreateCustomerException`, `UpdateCustomerException`, `AddNoteException`, `AddTagException`, `RemoveTagException` | **none** — each `is not null ? Task.FromException : <original>`; calls still recorded first; every hook defaults `null` |
| `StubServiceCommandService.cs` | `AssignSpecialistException`, `UnassignSpecialistException` | none (same pattern) |
| `StubSpecialistCommandService.cs` | `CreateSpecialistException`, `AddSkillException`, `RemoveSkillException` | none (same pattern) |

Identical seam pattern to Wave 2C-2 / 2C-3c. No existing hook or default value altered.

### B.4 Confirmed UNTOUCHED

| Area | Evidence |
|---|---|
| `ServicePageViewModel` (precedent only — already guarded) | not in `git status` |
| `IServiceCommandService` / `ICustomerCommandService` / `ISpecialistCommandService` / any interface / DTO | not in `git status` |
| DI — `Presentation`/`Infrastructure` `ServiceCollectionExtensions.cs` | not in `git status` |
| `AsyncRelayCommand` / `RelayCommand` (command infrastructure) | not in `git status` |
| `App.xaml.cs` (`DispatcherUnhandledException` / `LogUnhandledException`) | not in `git status` |
| Domain / Infrastructure / Shell / Application projects | not in `git status` |
| Backend contracts | not in `git status` |
| RBAC / permission gates | not in `git status` |
| Authentication | not in `git status` |
| Navigation / back-stack | not in `git status` |
| Every `[LoggerMessage]` **signature** (logging track closed) | reused as-is — no signature line in the diff |
| The Load-boundary `ErrorMessage = exception.Message` (pre-existing P2) | unchanged (only `SpecialistPageViewModel.LoadAsync`'s *logger call* line changed, not the `ErrorMessage` assignment) |
| HR / Inventory / Accounting / Org / Reporting / AI / Automation / infra VMs (Waves B–G) | not in `git status` |

---

## C. GUARD REVIEW

Every added guard follows the `ServicePageViewModel.CreateServiceAsync` / `ServiceProfileViewModel.SaveChangesAsync` precedent (verified against the full diff):

| Aspect | Result |
|---|---|
| Wraps existing command flow only | ✅ — the `await _commandService.<X>(...)` call, its arguments, and the following `clear-input` / `await LoadAsync()` / `re-select` lines are moved **verbatim** into the `try`; nothing added to the flow itself |
| Preserves success path | ✅ — on success: `CreateErrorMessage/SaveErrorMessage = null; Has*Error = false;` then the unchanged form-clear + `LoadAsync()` + selection |
| Preserves validation | ✅ — the early `if (tag is null) return;` / `if (skill is null) return;` / `if (Customer is null) return;` guards are **before** the new `try` and untouched |
| Preserves `CanExecute` | ✅ — no command binding or predicate touched (`AddNoteCommand` still gated on `!IsNullOrWhiteSpace(NewNoteText)`, `CreateCustomerCommand` on `NewCustomerFullName`, etc.) |
| Preserves RBAC | ✅ — no permission gate referenced or changed; the backend remains the sole write authority; a failed write simply does not persist |
| Catch shape | ✅ — `#pragma warning disable CA1031` + boundary comment + `catch (Exception)` (no variable) + set error prop + `Has*Error = true` + `LogOperationFailed(...)` |
| Business logic change | ✅ **NONE** — no service added/removed/reordered, no `Domain.*Rules`, no backend contract, no new decision |

**12 methods guarded:** `CreateCustomerAsync`, `AddNoteAsync`, `AddTagAsync`, `RemoveTagAsync`, `SaveChangesAsync` (Customer); `AssignSpecialistAsync`, `UnassignSpecialistAsync` (ServiceProfile); `AddSkillAsync`, `RemoveSkillAsync` (SpecialistProfile); `CreateSpecialistAsync` (SpecialistPage). *(ServiceProfile `SaveChanges`/`Deactivate` and SpecialistProfile `SaveChanges`/`Assign`/`Remove` were already guarded — untouched.)*

---

## D. SECURITY REVIEW

| Check | Result |
|---|---|
| Failures use localized messages | ✅ — `CreateErrorMessage`/`SaveErrorMessage` set to `Strings.Common_ActionFailedMessage` (Customer ×5) / `Strings.Services_SaveError` (ServiceProfile ×2) / `Strings.Specialists_SaveError` (SpecialistProfile ×2, SpecialistPage ×1) — all fixed, localized, generic |
| `Exception.Message` surfaced | ✅ never — `catch (Exception)` with **no exception variable** in all 12; the exception object is not referenced |
| Backend response body surfaced | ✅ never — not reachable (no `exception` in the catch body) |
| Internal identifiers surfaced (`_customerId`, `_serviceId`, `_specialistId`, tag/skill/assignment ids) | ✅ never — not referenced by any new catch or the new string |
| PII (name / email / phone / company) | ✅ never — not referenced |
| Backend body / identifier to the **log** | ✅ never — reuses the operation-name-only `[LoggerMessage]` (`LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(Logger, nameof(...))`) — Phase 8.61 already stripped all payloads |
| New string content (`Common_ActionFailedMessage`) | "The action could not be completed. Please try again." — generic, no detail |

**Test-enforced:** the `…_LogsOperationOnly` tests seed `"HTTP 500: backend response body / … PII secret"` into the thrown exception and assert `Assert.DoesNotContain(backendBody, entry.Message)` + `Assert.Contains("<Method>Async", entry.Message)`, while asserting the on-screen error `== Strings.*` (the fixed value).

---

## E. LOGGING REVIEW

| Check | Result |
|---|---|
| Existing `[LoggerMessage]` reused | ✅ — 4 VMs use their existing instance-form `LogOperationFailed(string operation)`; `SpecialistPageViewModel` uses its existing static-form `LogOperationFailed(ILogger, string operation)` via the new `Logger` helper |
| `Operation=nameof(Method)` only | ✅ — every new call is `LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(Logger, nameof(<Method>))` |
| No duplicate logging | ✅ — each catch logs **once**; `App.LogUnhandledException` no longer fires for these 12 paths (the exception is caught locally now, never reaches `DispatcherUnhandledException`) |
| No new logger architecture | ✅ — no new `ILogger` field, no `ILoggerFactory` added, no ctor change; `SpecialistPageViewModel.Logger` is a **computed property** over the `_loggerFactory` it already holds |
| No `SYSLIB1020` | ✅ — no `[LoggerMessage]` signature changed, no `ILogger` field added; `dotnet build -c Debug` → **0 warnings / 0 errors** |

---

## F. SPECIAL CASES

| Case | Result |
|---|---|
| `CustomerProfileViewModel.SaveChanges` — `EditableStatus` rollback | ✅ **added** — catch does `if (Customer is not null) { EditableStatus = Customer.Status; }` before setting the inline error (mirrors `ServiceProfileViewModel.SaveChangesAsync`). There was no rollback before because there was no catch; behaviour is now consistent with the other profile VMs. |
| `ServiceProfileViewModel.SaveChanges` — existing revert | ✅ **untouched** — `SaveChangesAsync`/`DeactivateAsync` were already guarded (Wave 2C-3a); not in Wave A scope; not in the diff |
| `SpecialistProfileViewModel.SaveChanges` — existing revert | ✅ **untouched** — already guarded (Wave 2C-3c); not in the diff |
| Reload / re-selection flows | ✅ **unchanged** — every guarded method still calls `await LoadAsync()` and (for creates) re-selects the created entity, on the success path only; on failure the reload is skipped (the command failed, nothing to reload) — inside the guarded block per precedent |
| `SpecialistPageViewModel.LoadAsync` logger call | changed only the expression `_loggerFactory?.CreateLogger<…>() ?? NullLogger<…>.Instance` → `Logger` — **semantically identical**, one line, same file, dedupe of a Phase 8.56 line |

---

## G. TEST REVIEW

| Check | Result |
|---|---|
| +13 tests, 0 existing bodies changed | ✅ (Customer page +2, Customer profile +5, Service profile +2, Specialist profile +2, Specialist page +2) |
| Failure-handling tests | ✅ every guarded method has a `_BackendThrows_…` test asserting `Record.Exception(() => Command.Execute(null))` is `null` (no throw / no `DispatcherUnhandledException`) + `Has*Error == true` + message `== Strings.*` |
| State-preservation tests | ✅ form-input preserved (`NewNoteText`/`NewTagText`/`NewSkillText`/`NewSpecialistName`/`NewCustomerFullName` unchanged on failure); `State != DashboardState.Error` (panel not replaced); `CustomerProfileViewModel.SaveChanges` test asserts `EditableStatus` reverts to `Active`; 2 tests assert the inline error **clears on the next successful action** |
| No-leak tests | ✅ seeded backend-body secret asserted absent from the log line; on-screen message asserted `== Strings.*` (fixed) |
| Reuse `RecordingLogger<T>` / `RecordingLoggerFactory` | ✅ both from prior waves; no new helper |
| Shared-stub *behaviour* change | ✅ none — additive `Exception?` seams only |

### G.1 Fresh validation run (this phase, working tree)

```
dotnet build -c Debug            → Build succeeded.  0 Warning(s)  0 Error(s)
dotnet test  -c Debug --no-build → all projects Passed
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **679** | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | **7** | 0 | 0 |
| **TOTAL** | **2,622** | **0** | **0** |

| Expected | Actual | Status |
|---|---|---|
| Build 0 warnings / 0 errors | 0 / 0 | ✅ |
| Tests 2,622 / 2,622 PASS | 2,622 / 2,622 | ✅ |
| Architecture 7 / 7 PASS | 7 / 7 | ✅ |

Delta vs `5ba554c` (2,609): **+13**, all in `Presentation.Tests` (666 → 679).

---

## H. COMMIT READINESS

| Gate | Status |
|---|---|
| HEAD `5ba554c`; nothing staged / pushed / merged / rebased / amended | ✅ |
| Exactly 17 code files, all Phase 8.66 authorized scope; 0 new files | ✅ |
| `ServicePageViewModel` / interfaces / DI / `AsyncRelayCommand` / `App.xaml.cs` / Domain / backend contracts / RBAC / auth / navigation NOT touched | ✅ |
| No business-behaviour change — guards wrap existing flow only; validation / `CanExecute` / RBAC / success path / reload preserved | ✅ |
| Failures surface a fixed localized string; no `Exception.Message` / backend body / identifier / PII surfaced or logged | ✅ (test-enforced) |
| Existing `[LoggerMessage]` reused, operation-name-only, once; no new logger / `ILoggerFactory` / field; no `SYSLIB1020` | ✅ (build 0/0) |
| `CustomerProfileViewModel.SaveChanges` reverts `EditableStatus`; `Service`/`Specialist` profile existing reverts untouched; reload/reselection unchanged | ✅ |
| New `Common_ActionFailedMessage` present in all 3 locale files | ✅ |
| Shared stubs: additive `Exception?` seams only, null-path identical; no existing test body changed | ✅ |
| Build 0/0 · Tests 2,622/2,622 · Architecture 7/7 | ✅ |

### H.1 Recommendation

**READY.** Proceed to **Phase 8.68 — Commit Execution** on authorization. No remediation required.

Planned commit:
- Subject (exact): `fix(desktop): guard customer/service/specialist command failures`
- Staging: `git reset` → 17 explicit `git add <path>` (never `git add .` / `-A`).
- Trailers: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` + `Claude-Session: …`
- Commit-message gotcha: Bash tool does not interpret PowerShell `@'…'@` here-strings — use repeated `-m` or `git commit -F <file>`.
- No push / merge / rebase / amend.

After this commit, Wave A of the missing-guard sweep is landed. Waves B–F (HR, Inventory+Accounting, Organization+Reporting, AI Center, Automation tabs) + the P2 infra wave remain, each its own audit → review → implement → commit cycle.

---

## STOP

Commit scope review complete. 12 guarded commands · 5 ViewModels · 1 new shared string · 3 new bindable property pairs · additive stub seams · 17 files · no business-behaviour change · no P0. No source or test change, no commit/push/merge/rebase/amend. HEAD remains `5ba554c`. **Awaiting Phase 8.68 commit authorization.**
