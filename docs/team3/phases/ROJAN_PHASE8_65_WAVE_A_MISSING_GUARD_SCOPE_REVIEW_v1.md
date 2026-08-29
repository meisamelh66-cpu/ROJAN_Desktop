# ROJAN AI — TEAM 3 — PHASE 8.65 — MISSING-GUARD SWEEP WAVE A (Customer / Service / Specialist commands) — SCOPE REVIEW v1

**Type:** Readiness review only. **No source change. No test change. No guard / service / DI change. No commit / push / merge / rebase / amend.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `5ba554ceb588e5780b87aebdf280538f6b25c485` — `fix(desktop): drop exception payload from diagnostic logging` (Phase 8.61, committed 8.63)
**Reference:** `ROJAN_PHASE8_64_MISSING_GUARD_SWEEP_SCOPE_AUDIT_v1.md` §E.2 Wave A, `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`.
**Verdict:** ✅ **READY TO IMPLEMENT.** LOW-MEDIUM risk. Additive `try`/`catch` + in-page error surfaces; no business-behaviour change.

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `5ba554ceb588e5780b87aebdf280538f6b25c485` |
| HEAD subject | `fix(desktop): drop exception payload from diagnostic logging` |
| Branch | `feature/team3-desktop-completion` |
| Pushed / merged / rebased | none |
| Tracked working-tree changes | **none** — `git status --porcelain` shows only untracked `ROJAN_*.md` reports |
| Unrelated tracked modifications | **none** |

Working tree clean. This review adds no code.

---

## B. WAVE A COMMAND INVENTORY

**12 unguarded user-triggered command methods across 5 ViewModels.** (`ServicePageViewModel` is in the audit scope but its 3 command methods — `LoadAsync`, `LoadCategoriesAsync`, `CreateServiceAsync` — are **already guarded** (Wave 2A `75357e1`); it contributes 0 changes and is the pattern precedent, see §C.1.)

Every catch will reuse the ViewModel's **existing** `[LoggerMessage] LogOperationFailed(...)` — the logging track is closed, no new logging plumbing.

| # | ViewModel | Method | Current body (unguarded) | Existing error surface | Guard target |
|---|---|---|---|---|---|
| 1 | `CustomerPageViewModel` | `CreateCustomerAsync` | `await _commandService.CreateCustomerAsync(req); clear 4 form fields; await LoadAsync(); SelectedCustomer = …` | `ErrorMessage`/`State` (Load only); form fields | **new `CreateErrorMessage`/`HasCreateError`** — mirrors `ServicePageViewModel` |
| 2 | `CustomerProfileViewModel` | `AddNoteAsync` | `await _commandService.AddNoteAsync(id, NewNoteText); NewNoteText = ""; await LoadAsync()` | `ErrorMessage`/`State` (Load); `EditableStatus` buffer; **no action-error property** | **new `SaveErrorMessage`/`HasSaveError`** — mirrors `ServiceProfileViewModel`/`SpecialistProfileViewModel` |
| 3 | `CustomerProfileViewModel` | `AddTagAsync` | `await _commandService.AddTagAsync(id, NewTagText); NewTagText = ""; await LoadAsync()` | same | same |
| 4 | `CustomerProfileViewModel` | `RemoveTagAsync` | `if (tag is null) return; await _commandService.RemoveTagAsync(id, tag.Id); await LoadAsync()` | same | same |
| 5 | `CustomerProfileViewModel` | `SaveChangesAsync` | `if (Customer is null) return; build UpdateCustomerRequest; await _commandService.UpdateCustomerAsync(req); await LoadAsync()` | same | same (+ revert `EditableStatus` to `Customer.Status` on failure — mirrors `ServiceProfileViewModel.SaveChangesAsync`'s buffer revert) |
| 6 | `ServiceProfileViewModel` | `AssignSpecialistAsync` | `await _commandService.AssignSpecialistAsync(id, NewSpecialistName); NewSpecialistName = ""; await LoadAsync()` | **`SaveErrorMessage`/`HasSaveError` exist** (save/deactivate) | reuse `SaveErrorMessage`/`HasSaveError` + `Strings.Services_SaveError` |
| 7 | `ServiceProfileViewModel` | `UnassignSpecialistAsync` | `if (assignment is null) return; await _commandService.UnassignSpecialistAsync(id, assignment.Id); await LoadAsync()` | same | same |
| 8 | `SpecialistProfileViewModel` | `AddSkillAsync` | `await _commandService.AddSkillAsync(id, NewSkillText); NewSkillText = ""; await LoadAsync()` | **`SaveErrorMessage`/`HasSaveError` + `AssignmentErrorMessage`/`HasAssignmentError` exist** | reuse `SaveErrorMessage`/`HasSaveError` + `Strings.Specialists_SaveError` (skills are a specialist-config edit — `SaveErrorMessage`'s "saving changes to this specialist" wording fits; `AssignmentErrorMessage` is service-assignment-specific by its own doc comment) |
| 9 | `SpecialistProfileViewModel` | `RemoveSkillAsync` | `if (skill is null) return; await _commandService.RemoveSkillAsync(id, skill.Id); await LoadAsync()` | same | same |
| 10 | `SpecialistPageViewModel` | `CreateSpecialistAsync` | `await _commandService.CreateSpecialistAsync(req); clear 4 form fields; await LoadAsync(); SelectedSpecialist = …` | `ErrorMessage`/`State` (Load only); form fields | **new `CreateErrorMessage`/`HasCreateError`** — mirrors `ServicePageViewModel`/`CustomerPageViewModel` |
| — | `ServicePageViewModel` | `CreateServiceAsync` | **already guarded** (Wave 2A) — the reference pattern | `CreateErrorMessage`/`HasCreateError` | ✅ no change |

### B.1 User impact of the current gap (per §B.1/§C of the Phase 8.64 audit)

Each of the 12 methods, on a backend/network failure: exception propagates out of `AsyncRelayCommand.Execute` (`async void`, `try/finally` no `catch`) → `App.DispatcherUnhandledException` → **logged + generic modal `MessageBox` + `e.Handled = true`** → app recovers, **no crash, no data corruption** (backend is the sole write authority). The UX defect: a *generic developer-worded modal* instead of the app's inline, contextual, retryable `CreateErrorMessage` / `SaveErrorMessage` pattern that `CreateServiceAsync` (same VM family) already uses.

---

## C. GUARD STRATEGY

### C.1 The established precedent — `ServicePageViewModel.CreateServiceAsync` (Wave 2A)

```csharp
try
{
    var created = await _commandService.CreateServiceAsync(request).ConfigureAwait(true);
    CreateErrorMessage = null;
    HasCreateError = false;
    // clear form fields
    await LoadAsync().ConfigureAwait(true);
    SelectedService = Services.FirstOrDefault(s => s.Id == created.Id);
}
#pragma warning disable CA1031 // Save boundary: any failure must surface as a safe, user-facing message and preserve the form's contents, never crash or leak internal exception detail
catch (Exception)
#pragma warning restore CA1031
{
    CreateErrorMessage = Strings.Services_SaveError;   // generic, localized, never the raw exception
    HasCreateError = true;
    LogOperationFailed(nameof(CreateServiceAsync));     // reuse the existing [LoggerMessage]
}
```

Also established: `ServiceProfileViewModel.SaveChangesAsync` (Wave 2C-3a) wraps `await command` **+** `await LoadAsync()` in the same `try`, sets `SaveErrorMessage`/`HasSaveError`, and **reverts the edit buffer** on failure.

### C.2 Chosen pattern per ViewModel

**Option A — local `try`/`catch` + in-page error property — for ALL 5 ViewModels.**

| ViewModel | Option | Rationale |
|---|---|---|
| `CustomerPageViewModel` | **A** — new `CreateErrorMessage`/`HasCreateError` | mirrors `ServicePageViewModel` exactly; `ErrorMessage`/`State` would replace the whole page (wrong for a create-form failure) |
| `CustomerProfileViewModel` | **A** — new `SaveErrorMessage`/`HasSaveError` (one shared inline area for note/tag/save) | mirrors `ServiceProfileViewModel`/`SpecialistProfileViewModel`; a single inline area is fine — the panel is non-destructive |
| `ServiceProfileViewModel` | **A** — reuse existing `SaveErrorMessage`/`HasSaveError` | property already exists and is semantically correct ("saving changes to this service") |
| `SpecialistProfileViewModel` | **A** — reuse existing `SaveErrorMessage`/`HasSaveError` | as above |
| `SpecialistPageViewModel` | **A** — new `CreateErrorMessage`/`HasCreateError` | mirrors `ServicePageViewModel`; note its logger is the **static-form** `LogOperationFailed(ILogger, string)` (Phase 8.56) — call `LogOperationFailed(_loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<SpecialistPageViewModel>.Instance, nameof(CreateSpecialistAsync))` (extract a tiny `Logger` helper property to avoid repeating the expression) |

- **Option B (DialogService)** — rejected. This app's whole error-UX pattern is in-page; no ViewModel uses `DialogService` for command errors.
- **Option C (global handler only)** — rejected. That is the status quo being fixed.

### C.3 Wrapping scope & no-regression

- Wrap the **command `await` + the subsequent `await LoadAsync()` + selection** in one `try` (the `ServiceProfileViewModel.SaveChangesAsync` precedent). If a post-command `LoadAsync` fails, `LoadAsync`'s own catch sets `State = Error` AND this catch sets the inline error — both are safe generic strings; the page-level error wins visually. Acceptable, rare, matches precedent.
- **Success path unchanged:** form-field clears, `NewNoteText = ""`, `LoadAsync()` reload, `SelectedCustomer`/`SelectedSpecialist` selection all preserved (moved inside the `try`, before the catch).
- **`CustomerProfileViewModel.SaveChangesAsync`:** on failure, revert `EditableStatus = Customer.Status` (mirrors `ServiceProfileViewModel`) so a rejected status change is not left displayed as applied.
- **`CanExecute` predicates unchanged** (`AddNoteCommand` gated on `!string.IsNullOrWhiteSpace(NewNoteText)` etc.).
- **No duplicate logging:** each catch calls the VM's existing `LogOperationFailed(nameof(<Method>))` **once**; the global `LogUnhandledException` no longer fires for these paths (the exception is now caught locally, never reaches `DispatcherUnhandledException`).
- **No business-behaviour change:** no service call added/removed/reordered; RBAC gates, `Domain.*Rules`, and backend authority untouched.

---

## D. SECURITY REVIEW

| Concern | Assessment |
|---|---|
| Backend response bodies to the user | ✅ never — every new catch sets a **fixed localized string** (`Strings.Services_SaveError` / `Strings.Specialists_SaveError` / a new `Strings.Common_ActionFailedMessage`), never `exception.Message` |
| `Exception.Message` to the user | ✅ never — the raw exception is not surfaced; `catch (Exception)` (no variable) or `catch (Exception exception)` used only for the revert value / nothing |
| Backend bodies / identifiers to the **log** | ✅ never — reuses the existing operation-name-only `[LoggerMessage]` (`LogOperationFailed(nameof(<Method>))`) — the Phase 8.61 harmonization already removed all exception/identifier payloads |
| PII (customer/specialist name, email, phone) | ✅ never referenced by a new catch or a new error string |
| Internal identifiers (`_customerId`, `_serviceId`, `_specialistId`, assignment ids) | ✅ never logged or surfaced |
| Existing `ErrorMessage = exception.Message` in the **Load** catches | ⚠️ **pre-existing, unchanged** — `CustomerPageViewModel.LoadAsync` / `CustomerProfileViewModel.LoadAsync` / `SpecialistPageViewModel.LoadAsync` still assign `exception.Message` to the on-screen `ErrorMessage`. This is a **separate, pre-existing pattern** (load-boundary error surfacing, present since before the logging track) and is **out of Wave A scope** — Wave A only adds *command* guards. Flag for a future "sanitize load-error surfacing" P2 item if desired; not introduced or worsened here. |

**Do the current `ErrorMessage` patterns need sanitization?** The **new** Wave A command surfaces: no — they use fixed strings by design. The **existing** load surfaces (`ErrorMessage = exception.Message`): a latent P2 (a backend `ApiException` body could show on-screen on a load failure) — **pre-existing, not a Wave A change**, recommend a separate small phase.

---

## E. TEST PLAN

### E.1 Shared-stub seams to add (additive, null-path byte-identical)

| Stub | Add `Exception?` hooks for |
|---|---|
| `tests/…/Customers/StubCustomerCommandService.cs` | `CreateCustomerException`, `UpdateCustomerException`, `AddNoteException`, `AddTagException`, `RemoveTagException` (currently call-recording only — **no** exception seams) |
| `tests/…/Services/StubServiceCommandService.cs` | `AssignSpecialistException`, `UnassignSpecialistException` (has `Create*`/`Update*` only) |
| `tests/…/Specialists/StubSpecialistCommandService.cs` | `AddSkillException`, `RemoveSkillException`, `CreateSpecialistException` (has `UpdateSpecialist*`/`AssignService*`/`RemoveServiceAssignment*` only) |

~10 seams. Same established pattern as Wave 2C-2 (`StubAutomationServices` +16 hooks) and Wave 2C-3c (`StubSpecialistCommandService` +2 hooks) — additive property + `?? Task.CompletedTask`/`Task.FromException`, existing call sites unaffected.

### E.2 Tests to add (~15)

| Per method (12) | Assert |
|---|---|
| `<Method>_BackendThrows_SetsInlineError_DoesNotThrow_DoesNotCorruptState` | (a) `Command.Execute(null)` does not throw / does not raise `DispatcherUnhandledException`; (b) the correct in-page property is set (`HasCreateError`/`HasSaveError` true, message = the fixed string); (c) form fields / selection / `EditableStatus` / list not corrupted (e.g. for `SaveChangesAsync`: `EditableStatus` reverted); (d) `RecordingLogger` has one `Error` entry `Operation=<method>`, no exception/PII |
| Additional (3) | `CreateErrorMessage`/`HasCreateError` clears on the next **successful** create (Customer + Specialist pages); `CustomerProfileViewModel` `SaveErrorMessage` clears on next successful action; NullLogger safety (construct with no logger → guarded failure still no-throw) |

Reuse `RecordingLogger<T>` / `RecordingLoggerFactory`. **No new test helper.**

### E.3 Estimate

| | Count |
|---|---|
| Production ViewModels | **5** |
| Localization files | **4** (`Strings.cs` + `Strings.resx` + `Strings.en.resx` + `Strings.ar.resx`) — one new key `Common_ActionFailedMessage` |
| Shared test stubs | **3** |
| Test ViewModel files | **5** (`CustomerPageViewModelTests`, `CustomerProfileViewModelTests`, `ServiceProfileViewModelTests`, `SpecialistProfileViewModelTests`, `SpecialistPageViewModelTests`) |
| **Total files** | **17** |
| New bindable property pairs | 3 (`CustomerPageViewModel.CreateError*`, `CustomerProfileViewModel.SaveError*`, `SpecialistPageViewModel.CreateError*`) |
| New tests | **~15** |
| Test-count delta | 2,609 → **~2,624** |

---

## F. COMMIT PLAN

### F.1 Recommendation — **one Wave A commit**

```
fix(desktop): guard customer/service/specialist command failures
```

### F.2 Single vs split (Customer / Service / Specialist)

| | Single commit | Split (3 commits) |
|---|---|---|
| The new `Common_ActionFailedMessage` string (4 loc files) | one place, used by Customer; declared once | must land in the Customer commit; the Service/Specialist commits then can't touch loc — awkward ordering |
| Change shape | identical `try`/`catch` + inline-error pattern across all 12 methods, one security narrative | 3 near-identical diffs |
| Review surface | ~17 files, one conceptual change ("guard Wave A commands") | 3 × ~6 files, 3 review/commit cycles |
| Risk of half-guarded tree between commits | none (atomic) | a split leaves Customer guarded, Service/Specialist not — harmless but pointless |
| Precedent | Wave 2A bundled 5 VMs; Wave 2C-3c bundled 6 VMs + 3 parents + 1 correction | — |
| **Verdict** | **RECOMMENDED** | not recommended — the shared loc string alone makes splitting messy |

### F.3 Staging

`git reset` → 17 explicit `git add <path>` (never `git add .` / `-A`). Trailers `Co-Authored-By` + `Claude-Session`. No push/merge/rebase/amend.

---

## G. PHASE 8.66 RECOMMENDATION — **Missing-Guard Sweep Wave A — Implementation**

| Field | Detail |
|---|---|
| **Goal** | Guard the 12 backend-connected Customer/Service/Specialist write commands with the established in-page `try`/`catch` + inline-error pattern (`ServicePageViewModel.CreateServiceAsync` / `ServiceProfileViewModel.SaveChangesAsync` precedent). Reuse each VM's existing `[LoggerMessage]`. No business-behaviour change. Set the reusable pattern + `Common_ActionFailedMessage` string for Waves B–F. |
| **Scope — production (5 VMs + 4 loc)** | `Customers/CustomerPageViewModel.cs` (`CreateCustomerAsync` + new `CreateErrorMessage`/`HasCreateError`); `Customers/CustomerProfileViewModel.cs` (`AddNoteAsync`/`AddTagAsync`/`RemoveTagAsync`/`SaveChangesAsync` + new `SaveErrorMessage`/`HasSaveError`); `Services/ServiceProfileViewModel.cs` (`AssignSpecialistAsync`/`UnassignSpecialistAsync` — reuse `SaveErrorMessage`); `Specialists/SpecialistProfileViewModel.cs` (`AddSkillAsync`/`RemoveSkillAsync` — reuse `SaveErrorMessage`); `Specialists/SpecialistPageViewModel.cs` (`CreateSpecialistAsync` + new `CreateErrorMessage`/`HasCreateError` + a tiny `Logger` helper for the static `[LoggerMessage]`). `Localization/Strings.cs` + `Strings.resx` + `Strings.en.resx` + `Strings.ar.resx` — one key `Common_ActionFailedMessage`. |
| **Scope — tests (5 + 3)** | the 5 `*ViewModelTests.cs`; `StubCustomerCommandService.cs` (+5 seams), `StubServiceCommandService.cs` (+2), `StubSpecialistCommandService.cs` (+3). ~+15 tests. |
| **NOT touched** | `AsyncRelayCommand`, `App.xaml.cs`, DI, Domain, backend contracts, RBAC, authentication, navigation, any `[LoggerMessage]` signature (logging track closed), the Load-boundary catches (`ErrorMessage = exception.Message` — separate pre-existing P2), the HR/Inventory/Accounting/Org/Reporting/AI/Automation/infra VMs (Waves B–G), `ServicePageViewModel` (already guarded). |
| **Risk** | **LOW-MEDIUM.** Additive `try`/`catch` around existing awaits; 3 new bindable property pairs (additive, no ctor change); one new localized string in 4 files (mechanical, non-behavioural). Design points: (1) wrap command + reload together (precedent), (2) `SaveChangesAsync` reverts `EditableStatus` on failure, (3) `SpecialistPageViewModel`'s static logger needs the `_loggerFactory`-derived call. |
| **Validation** | `dotnet build -c Debug` → 0 warnings / 0 errors (watch: new resx keys must be in all 3 files or `Get()` falls through; `Strings.cs` line must match `nameof`); full suite ~2,609 → ~2,624; architecture 7/7. |
| **Commit** | one isolated commit — `fix(desktop): guard customer/service/specialist command failures`. Rhythm: 8.66 implementation (STOP before commit) → 8.67 commit scope review → 8.68 commit execution → checkpoint update. Then Waves B–F. |

---

## STOP

Scope review complete. 12 unguarded commands · 5 ViewModels · Option A (in-page `try`/`catch` + inline error) for all · reuse existing `[LoggerMessage]` · 1 new shared string · 3 new property pairs · ~17 files · one commit. No P0 — the gap is UX-consistency, all currently recovered by `App.DispatcherUnhandledException`. No business-behaviour change. No source or test change, no commit/push/merge/rebase/amend. HEAD remains `5ba554c`. **Awaiting Phase 8.66 implementation authorization.**
