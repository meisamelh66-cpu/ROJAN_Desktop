# ROJAN AI — TEAM 3 — PHASE 8.107 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 2 (CUSTOMERS + HR + MEMBERSHIP) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / localization / service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `76d3f61` (`fix(desktop): sanitize reporting, AI center and accounting error surfacing`)
**Reference:** `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md` §F sub-wave 2, `ROJAN_PHASE8_106_P2_SUBWAVE1_COMMIT_REPORT_v1.md`
**Recommendation: ship all 7 sites (5 VMs) in ONE commit. LOW risk — uniform UI-string swap; no `using`, localization, behaviour, logging, DI, service or contract change.**

---

## A. GIT STATE

```
git rev-parse HEAD        → 76d3f61228e9ff5c6275bb1ed57508072dd66cee
git branch --show-current → feature/team3-desktop-completion
git status (tracked)      → clean
git diff --cached         → (empty)
```

Untracked: only `ROJAN_*.md`. Baseline (checkpoint §E, `76d3f61`): **2,713 / 2,713** — Domain 456, Presentation 770, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. TARGET INVENTORY — 7 sites / 5 ViewModels

The Phase 8.102 §B.1 sub-wave-2 cluster. Note: the phase-brief example names (`CustomerListViewModel`, `CustomerTimelineViewModel`, `PayrollViewModel`, `SubscriptionViewModel`, `LoyaltyViewModel`, …) **do not exist** — the actual VMs are below. `ViewModels/Membership/` contains exactly one VM (`AcceptInviteViewModel`).

Every site is the same shape:
```csharp
#pragma warning disable CA1031 // Top-level load/command boundary: …
catch (Exception exception)
#pragma warning restore CA1031
{
    <Surface> = exception.Message;          // ← the leak
    State = DashboardState.Error;           // load boundaries only
    LogOperationFailed(nameof(<Method>));   // already operation-name-only
}
```

### B.1 Customers — 2 sites

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `Customers/CustomerPageViewModel.cs:254` | `LoadAsync` | `ErrorMessage` | ✅ | inside `if (requestVersion == _filterVersion)` (stale-response guard — unchanged) |
| `Customers/CustomerProfileViewModel.cs:274` | `LoadAsync` | `ErrorMessage` | ✅ | loads the full `CustomerProfileDto` — customer, notes, tags, activity, statistics, booking summary, insights (score / loyalty / engagement) |

- Logger: instance-form `LogOperationFailed(string)` (`"Customer page operation failed. …"` / `"Customer profile operation failed. …"`), doc comment already says *"never … its message, customer data, or any backend response detail"*.
- Both files: `using Rojan.Desktop.Presentation.Localization;` present; both use `Strings.Common_ActionFailedMessage` in their Wave A/B command guards (`CreateErrorMessage` / `SaveErrorMessage`).

### B.2 HR — 3 sites

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `HR/HrPageViewModel.cs:413` | `LoadAsync` | `ErrorMessage` | ✅ | loads employees + commission rules + **commission transactions** + **payroll summaries** |
| `HR/HrPageViewModel.cs:442` | `SearchAsync` | `ErrorMessage` | ✅ | inside `if (searchText == SearchText)` (out-of-order-completion guard — unchanged) |
| `HR/EmployeeProfileViewModel.cs:116` | `LoadAsync` | `ErrorMessage` | ✅ | loads the full `EmployeeProfileDto` (employee record + compensation) |

- Logger: instance-form `LogOperationFailed(string)` (`"HR page operation failed. …"` — comment: *"never … employee/payroll data"*; `"Employee profile operation failed. …"`).
- Both files: `using …Localization;` present; both use `Strings.Common_ActionFailedMessage` in their Wave B `ActionErrorMessage` guards.

### B.3 Membership — 2 sites  ← the standout

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `Membership/AcceptInviteViewModel.cs:166` | `LookupAsync` | `LookupErrorMessage` | ❌ (in `finally { IsLookingUp = false; }`) | `_inviteService.GetDetailsAsync(Token.Trim())` — **the invite token is in scope** |
| `Membership/AcceptInviteViewModel.cs:211` | `AcceptAsync` | `AcceptErrorMessage` | ❌ (in `finally { IsAccepting = false; }`) | `_inviteService.AcceptAsync(Token, salon)` + `_currentSessionService.InitializeAsync()` — token, invitee email, user id, salon id/role in scope |

- Logger: instance-form `LogOperationFailed(string)` (`"Invite operation failed. Operation={Operation}"`), doc comment (Phase 8.35): *"never … the invite token, any bearer token, the user's identity/email, salon identifiers/role, or any backend response detail"*.
- `using …Localization;` present.
- **Phase 8.35 hardened the LOG to be token-safe + identity-safe — but the UI surface was left leaking.** The current tests **document this as intentional-for-now**:
  - `AcceptInviteViewModelTests.cs:144` — `Assert.Contains(SecretToken, sut.LookupErrorMessage!, …); // the user still sees the raw backend message`
  - `AcceptInviteViewModelTests.cs:51` — `Assert.Equal("Salon invite not found or no longer available", sut.LookupErrorMessage)` (a raw stubbed backend string, **not** a `Strings.*` constant)
  - `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` (line 152) and `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` (line 176) assert **only the log** — they never check `sut.AcceptErrorMessage`, which currently shows `"accept failed for <SecretToken>"` / `"session resolution failed for user owner@salon.example (id u-4821)"` → **undetected live UI leaks of the token, email, and user id.**

### B.4 Not in scope — already correct

`LoginViewModel` / `MobileOtpLoginViewModel` — typed `ApiException` catches → localized `Strings.Login_*` constants (Phase 8.102 §B.2). All Wave A–F command guards in these same 5 VMs → `Common_ActionFailedMessage` (do not touch).

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — sensitive user-visible leak** | **all 7 sites** — bare `catch (Exception exception)` → `<Surface> = exception.Message` bound to a `TextBlock` | **sanitize — the sub-wave-2 work** |
| **B — already sanitized** | the Wave A/B command guards in these VMs (`CreateErrorMessage` / `SaveErrorMessage` / `ActionErrorMessage` → `Common_ActionFailedMessage`) | **do not touch** |
| **C — intentional technical message** | none in this cluster |
| **D — out of scope** | sub-waves 3–6; `LoginViewModel` / `MobileOtpLoginViewModel` (already typed) |

All 7 are the identical shape and the identical fix.

---

## D. SECURITY

### What `exception.Message` can currently put on screen

| Data class | Where reachable | Concrete |
|---|---|---|
| **Invite token (a credential)** | `AcceptInviteViewModel.LookupAsync` / `AcceptAsync` | **test-documented at `:144`** (`LookupErrorMessage` contains `SecretToken`); undetected in `AcceptErrorMessage` (`"accept failed for <SecretToken>"`) |
| **Invitee email + user id** | `AcceptInviteViewModel.AcceptAsync` (`_currentSessionService.InitializeAsync()` failure) | undetected — `AcceptErrorMessage` shows `"session resolution failed for user owner@salon.example (id u-4821)"` |
| Salon id / role | `AcceptInviteViewModel` (both) | a backend membership error quoting the salon id or `RECEPTIONIST`/`MANAGER` |
| **Customer name / phone / email / address / personal notes** | `CustomerProfileViewModel.LoadAsync` (`CustomerProfileDto`), `CustomerPageViewModel.LoadAsync` (search results / filters) | a backend validation message quoting a filter value or a returned record field |
| Customer loyalty level / score / engagement / booking history | `CustomerProfileViewModel.LoadAsync` (`Insights` / `BookingSummary`) | as above |
| **Salary / payroll / commission figures** | `HrPageViewModel.LoadAsync` (payroll summaries + commission transactions), `EmployeeProfileViewModel.LoadAsync` (`EmployeeProfileDto` compensation) | a backend 500 / validation error quoting an amount |
| Employee PII / internal records | `HrPageViewModel` / `EmployeeProfileViewModel` | as above |
| Backend bodies / internal hosts / file paths / DB fragments | all 7 (`HttpRequestException`, `IOException`, EF text echoed in a 500) | generic infra leak |

**Answer to TASK D:** yes — `exception.Message` at these 7 sites can expose every listed item (customer names/phone/email/address/notes; salary/employee-info/attendance/internal records; membership token/status/history). For `AcceptInviteViewModel` the token leak into `LookupErrorMessage` is **currently live and test-asserted**.

### Sanitization pattern (identical to sub-wave 1)

```csharp
// before
catch (Exception exception)
{
    ErrorMessage = exception.Message;             // or LookupErrorMessage / AcceptErrorMessage
    State = DashboardState.Error;                 // where present — UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}

// after
catch (Exception)                                // drop the variable → leak structurally impossible
{
    ErrorMessage = Strings.Common_ActionFailedMessage;
    State = DashboardState.Error;                 // UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}
```

- `AcceptInviteViewModel`: keep both `finally { IsLookingUp / IsAccepting = false; }` blocks and the `HasLookupError` / `HasAcceptError` computed flags (they derive from `!string.IsNullOrEmpty(...)` — still true for the generic message). No `State` involved.
- `CustomerPageViewModel.LoadAsync` / `HrPageViewModel.SearchAsync`: keep the `if (requestVersion == _filterVersion)` / `if (searchText == SearchText)` stale-guard exactly.

### UX note — `AcceptInviteViewModel.LookupAsync`

Replacing `"Salon invite not found or no longer available"` (raw stubbed backend text today) with the generic `Common_ActionFailedMessage` is a slight specificity loss for the "bad/expired link" case. **The trade strongly favours sanitizing** — the current behaviour leaks the token. If nicer copy is wanted, the impl phase could add one key (`AcceptInvite_LookupFailed` — "Couldn't check that invite link. Check the link and try again.") in all 3 `.resx`; that is an **impl-phase decision** and the audit's STRICT MODE forbids adding it here. Recommendation: use `Common_ActionFailedMessage` for consistency with the rest of the P2; the optional key is a follow-up nicety, not a blocker.

### Logs — unchanged

All 7 catches keep `LogOperationFailed(nameof(<Method>))`. `[LoggerMessage]` templates byte-unchanged. The Phase 8.35 token-safe / identity-safe **log** assertions in `AcceptInviteViewModelTests` (`DoesNotContain(SecretToken / "owner@salon.example" / "u-4821", entry.Message)`) still pass.

---

## E. ARCHITECTURE

| Question | Answer |
|---|---|
| Existing `[LoggerMessage]` availability | **Yes — no logging change.** All 5 VMs have an `ILogger<T>` + operation-name-only instance-form `LogOperationFailed` invoked in the same catch. Untouched. |
| Existing localization availability | **Yes — `Strings.Common_ActionFailedMessage` already ships** (all 3 `.resx`, Wave A) **and all 5 VMs already `using Rojan.Desktop.Presentation.Localization;` and already reference `Strings.Common_ActionFailedMessage`** in their Wave A/B guards → **no `using` addition, no `.resx` change.** |
| Need for new tests | Minimal. Each VM has a `Constructor…Throws` / `…Failure…` test; the assertions that check the surface value flip in place. `AcceptInviteViewModel` gains explicit `AcceptErrorMessage` no-leak assertions (currently absent). Est. ~10–14 assertion edits + ~0–3 new/strengthened tests. |
| Need for stubs | **None** — every failure path uses a pre-existing seam (`StubCustomerProfileQueryService` ctor func, `StubEmployeeQueryService` ctor func, `StubSalonInviteService.DetailsException` / `.AcceptException`, `StubCurrentSessionService.InitializeException`). |
| DI impact | **None.** |
| Service / contract impact | **None expected** — confirmed: no service or `ISalonInviteService` / `ICustomerProfileQueryService` / `IEmployeeQueryService` change. |
| `SYSLIB1020` / partial | Not relevant — no `[LoggerMessage]` touched; classes already `partial`. |

---

## F. RECOMMENDATION — WAVE SIZE

### Ship all 7 sites in ONE commit.

| Metric | Value |
|---|---|
| Sites | **7** |
| ViewModels | **5** (`CustomerPageViewModel`, `CustomerProfileViewModel`, `HrPageViewModel`, `EmployeeProfileViewModel`, `AcceptInviteViewModel`) |
| Files | 5 prod + 5 test = **10** |
| Estimated tests | ~10–14 in-place assertion flips + ~0–3 new/strengthened (`AcceptInviteViewModel` surface no-leak); suite ≈ 2,713 → **~2,715** |
| Risk | **LOW** — identical single-shape change; no `using` / localization / behaviour / logging / DI / service change; every failure path already has a test + a seam; the `finally` blocks, stale-response guards, `State = Error`, and `Has*Error` flags are all preserved |

**No split needed.** Customers + HR + Membership is one coherent "people-data" cluster (customers, employees, invitees), reviewed together. Splitting into 3 commits triples the audit→review→commit overhead for zero risk reduction. `AcceptInviteViewModel` is the highest-value target (a live, test-documented token leak) but takes the identical fix.

### Implementation plan (Phase 8.108)

- **Prod files (5) — each: drop the `catch` variable, `= exception.Message` → `= Strings.Common_ActionFailedMessage`; nothing else moves:**
  - `CustomerPageViewModel.cs` — `LoadAsync` (1)
  - `CustomerProfileViewModel.cs` — `LoadAsync` (1)
  - `HrPageViewModel.cs` — `LoadAsync`, `SearchAsync` (2)
  - `EmployeeProfileViewModel.cs` — `LoadAsync` (1)
  - `AcceptInviteViewModel.cs` — `LookupAsync` (`LookupErrorMessage`), `AcceptAsync` (`AcceptErrorMessage`) (2); keep both `finally` blocks
  - **No `using` additions** (all 5 already import `…Localization`).
- **Test files (5):** flip `Assert.Equal("boom", sut.ErrorMessage)` → `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` (Customer/Hr/Employee); in `AcceptInviteViewModelTests` flip `:51` and `:144` to the generic constant + `DoesNotContain(SecretToken)`, and **add** `Assert.Equal(Strings.Common_ActionFailedMessage, sut.AcceptErrorMessage)` + `DoesNotContain(SecretToken / "owner@salon.example" / "u-4821", sut.AcceptErrorMessage)` to the two Accept-failure tests. Update the `// user-visible behaviour … unchanged` comments.
- **No** DI / service / contract / `.resx` / stub / new-file change.
- **Commit subject:** `fix(desktop): sanitize customer, HR and membership error surfacing`

### Separate from Missing-Guard work

Missing-Guard Sweep (`794648e` … `0260bc3`) is complete. This changes the *message string* in *pre-existing* catches — no new guard, no behaviour.

---

## STOP

Phase 8.107 audit complete. HEAD `76d3f61`, tracked tree clean, baseline 2,713 / 2,713.
**7 Category-A sites across 5 ViewModels** — `CustomerPageViewModel.LoadAsync`, `CustomerProfileViewModel.LoadAsync`, `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync`, `AcceptInviteViewModel.LookupAsync` / `.AcceptAsync` — surface `exception.Message` to a bound `TextBlock`, exposing customer PII / loyalty data, salary / payroll / employee records, and — **currently live and test-documented** — the invite token, invitee email, and user id in `AcceptInviteViewModel`. Uniform behaviour-neutral fix: **drop the `catch` variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**; keep `State = Error`, every `finally`, the stale-response guards, and every operation-name-only log call. **All 5 VMs already import `…Localization` and reference `Common_ActionFailedMessage` — no `using`, no `.resx`, no DI / service / contract change.**
**Recommendation: one commit, all 7 sites, LOW risk. ~10–14 assertion flips + ~0–3 new AcceptInvite surface no-leak assertions, suite ~2,715.**

**Awaiting Phase 8.108 authorization.**
