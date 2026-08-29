# ROJAN AI — TEAM 3 — PHASE 8.111 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 3 (ORGANIZATION + SPECIALISTS + SERVICES) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / localization / service / DI change. No commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `1260d4e` (`fix(desktop): sanitize customer, HR and membership error surfacing`)
**Reference:** `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md` §F sub-wave 3, `ROJAN_PHASE8_110_P2_SUBWAVE2_COMMIT_REPORT_v1.md`
**Recommendation: ship all 8 sites (7 VMs) in ONE commit. LOW risk — uniform UI-string swap. 1 prod + 3 test `using` additions; no localization, behaviour, logging, DI, service or contract change.**

---

## A. GIT STATE

```
git rev-parse HEAD        → 1260d4eee70191d6c306145d2de32b5c57d46eb7
git branch --show-current → feature/team3-desktop-completion
git status (tracked)      → clean
git diff --cached         → (empty)
```

Untracked: only `ROJAN_*.md`. Baseline (checkpoint §E, `1260d4e`): **2,714 / 2,714** — Domain 456, Presentation 771, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. TARGET INVENTORY — 8 sites / 7 ViewModels

Confirmed against a fresh grep — matches Phase 8.102 §B.1 exactly. Every site is the same shape:
```csharp
#pragma warning disable CA1031 // Top-level load/mutation boundary: …
catch (Exception exception)
#pragma warning restore CA1031
{
    <Surface> = exception.Message;          // ← the leak
    State = DashboardState.Error;           // load boundaries only
    Log…(nameof(<Method>));                 // already operation-name-only
}
```

### B.1 Organization — 1 site

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `Organizations/OrganizationPageViewModel.cs:441` | `LoadAsync` | `ErrorMessage` | ✅ | loads organizations + branches + RBAC role/permission data |

- Logger: instance-form `LogOperationFailed(string)`.
- **No `using …Localization;`** — the file references the constant **fully-qualified** in its Wave D command guards (`ActionErrorMessage = Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage`). Impl uses the **same FQ form** → **no `using` addition** (consistent with the file).

### B.2 Specialists — 5 sites / 4 VMs

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `Specialists/SpecialistPageViewModel.cs:282` | `LoadAsync` | `ErrorMessage` | ✅ | inside `if (requestVersion == _filterVersion)` (stale-response guard — unchanged). Logger: **static-form** `LogOperationFailed(Logger, string)` (this class has 2 `ILogger` fields). |
| `Specialists/SpecialistProfileViewModel.cs:313` | `LoadAsync` | `ErrorMessage` | ✅ | loads the full `SpecialistProfileDto` (staff record, services, assignments). Logger: instance-form. |
| `Specialists/SpecialistScheduleViewModel.cs:284` | `LoadAsync` | `ErrorMessage` | ✅ | **preceded by** `catch (UnauthorizedOperationException) { IsPermissionDenied = true; State = Error; LogPermissionDenied(nameof(LoadAsync)); }` — **keep that typed branch first, unchanged** |
| `Specialists/SpecialistScheduleViewModel.cs:474` | `TryMutateAsync` (`[CallerMemberName] operationName`) | `ErrorMessage` | ❌ (mutation boundary — inline error, no `State`) | **the shared mutation error boundary for 8 callers** (set weekly availability / add override / add leave / add block / remove block / …). **Preceded by** `catch (UnauthorizedOperationException) { IsPermissionDenied = true; LogPermissionDenied(operationName); return false; }` — keep. On success it already does `ErrorMessage = null`. |
| `Specialists/SpecialistAvailabilityViewModel.cs:109` | `LoadAsync` | `ErrorMessage` | ✅ | loads availability windows. Logger: instance-form `LogLoadFailed(string)`. **No `using …Localization;`, no `Strings` reference anywhere in the file** → **impl needs `+ using Rojan.Desktop.Presentation.Localization;`** (or fully-qualify the one line). |

- `SpecialistPageViewModel` / `SpecialistProfileViewModel` / `SpecialistScheduleViewModel`: `using …Localization;` present; all reference `Strings.…` already.

### B.3 Services — 2 sites / 2 VMs

| File · line | Method | Surface | `State = Error`? | Notes |
|---|---|---|---|---|
| `Services/ServicePageViewModel.cs:337` | `LoadAsync` | `ErrorMessage` | ✅ | inside `if (requestVersion == _filterVersion)` (stale-response guard — unchanged). Logger: instance-form. |
| `Services/ServiceProfileViewModel.cs:245` | `LoadAsync` | `ErrorMessage` | ✅ | loads the full service config (pricing, duration, category, commission). Logger: instance-form. |

- Both files: `using …Localization;` present; both use `Strings.Common_ActionFailedMessage` in their Wave A/B command guards.

### B.4 Not in scope — already correct / other

`LoginViewModel` / `MobileOtpLoginViewModel` (typed catches → `Strings.Login_*`). The Wave A–F command guards in these 7 VMs → `Common_ActionFailedMessage` (do not touch). `SpecialistScheduleViewModel` / `SpecialistProfileViewModel` inline validation surfaces (`InputErrorMessage`, `SaveErrorMessage`, `AssignmentErrorMessage`) are either already localized constants or command-guard surfaces — **not** `= exception.Message` load-catches — out of scope.

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — sensitive user-visible leak** | **all 8 sites** — bare `catch (Exception exception)` → `<Surface> = exception.Message` bound to a `TextBlock` | **sanitize — the sub-wave-3 work** |
| **B — already sanitized / keep** | the two `catch (UnauthorizedOperationException)` typed branches in `SpecialistScheduleViewModel` (`LoadAsync` line ~274 and `TryMutateAsync` line ~464) → `IsPermissionDenied` + `LogPermissionDenied` (**Warning**, operation-name-only). Do **not** touch — they are more specific and must stay ahead of the general catch. Also the Wave A–F command guards in these VMs. |
| **C — intentional technical message** | none in this cluster |
| **D — out of scope** | sub-waves 4–6; `LoginViewModel` / `MobileOtpLoginViewModel`; the inline validation surfaces noted in §B.4 |

All 8 Category-A sites are the identical shape and the identical fix.

---

## D. SECURITY

| Data class | Where reachable | Concrete |
|---|---|---|
| **Company / branch data, org & branch ids** | `OrganizationPageViewModel.LoadAsync` | a backend validation message quoting a branch id, branch name/address, or company name |
| **Roles / permissions** | `OrganizationPageViewModel.LoadAsync` (RBAC role/permission data in the load) | an authorization error quoting a role name or permission string |
| **Staff PII** (name / email / phone) | `SpecialistProfileViewModel.LoadAsync` (`SpecialistProfileDto`) | a backend error quoting a returned record field |
| **Availability / leave / blocks** | `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync` | a schedule-rule validation error quoting a time window or specialist id (`SpecialistScheduleViewModelTests` already seeds `"backend body / specialist-1"` to prove the **log** omits it — the **UI** does not) |
| **Pricing / service configuration / commission rules** | `ServiceProfileViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync` | a validation error quoting a price, cost, or commission % |
| Backend bodies / internal hosts / file paths / DB fragments | all 8 (`HttpRequestException`, `IOException`, EF text echoed in a 500) | generic infra leak |

**Answer to TASK D:** yes — `exception.Message` at these 8 sites can expose every listed item (company/branch data, roles, permissions; staff information, availability, personal information; pricing, service configuration, business rules).

### Sanitization pattern (identical to sub-waves 1–2)

```csharp
// before
catch (Exception exception)
{
    ErrorMessage = exception.Message;
    State = DashboardState.Error;                 // where present — UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}

// after
catch (Exception)                                // drop the variable → leak structurally impossible
{
    ErrorMessage = Strings.Common_ActionFailedMessage;   // Organization: Rojan.Desktop.Presentation.Localization.Strings.…
    State = DashboardState.Error;                 // UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}
```

- Keep the `catch (UnauthorizedOperationException)` branch **before** the general catch in `SpecialistScheduleViewModel` (both sites), unchanged.
- Keep `SpecialistScheduleViewModel.TryMutateAsync`'s `[CallerMemberName] operationName` argument and its success-path `ErrorMessage = null` exactly.
- Keep the `CustomerPageViewModel`-style stale-response `if` in `SpecialistPageViewModel.LoadAsync` / `ServicePageViewModel.LoadAsync`.
- No `State` is added or removed anywhere.

### Logs — unchanged

All 8 catches keep `LogOperationFailed(...)` / `LogLoadFailed(...)` / (permission branch) `LogPermissionDenied(...)`. The pre-existing operation-name-only **log** assertions (`SpecialistScheduleViewModelTests` `DoesNotContain("specialist-1" / "backend body")`, `SpecialistProfileViewModelTests` `DoesNotContain("ROJAN_Backend" / "status 500")`) still pass.

---

## E. ARCHITECTURE

| Question | Answer |
|---|---|
| Existing `[LoggerMessage]` availability | **Yes — no logging change.** All 7 VMs have an `ILogger` + operation-name-only generated logger invoked in the same catch (`SpecialistPageViewModel` static-form; the rest instance-form; `SpecialistAvailabilityViewModel` uses `LogLoadFailed`; `SpecialistScheduleViewModel` also has `LogPermissionDenied` at Warning). Untouched. |
| Existing localization usage | `Strings.Common_ActionFailedMessage` ships (all 3 `.resx`, Wave A). 5 of 7 prod VMs already `using …Localization;`; `OrganizationPageViewModel` uses the fully-qualified form (kept). **`SpecialistAvailabilityViewModel` has no reference → `+ using Rojan.Desktop.Presentation.Localization;` (1 line).** No `.resx` change. |
| Test impact | Each VM has a `LoadCommand…Throws` / `Constructor…Throws` test; the surface-value assertions flip in place (~10–14). `SpecialistScheduleViewModelTests` should gain a `TryMutateAsync` general-failure surface no-leak test (via the existing `StubSpecialistScheduleCommandService.Fail` seam). **3 test files need `+ using …Localization;`** (`SpecialistScheduleViewModelTests`, `SpecialistAvailabilityViewModelTests`, `ServicePageViewModelTests`). |
| Stub impact | **None** — every failure path uses a pre-existing seam (`StubSpecialistScheduleQueryService.WeeklyAvailability` func, `StubSpecialistScheduleCommandService.Fail`, `StubServiceQueryService` / `StubSpecialistQueryService` / `StubOrganizationQueryService` ctor funcs). |
| DI impact | **None.** |
| Service / contract impact | **None expected** — confirmed. |
| `SYSLIB1020` / partial | Not relevant — no `[LoggerMessage]` touched; classes already `partial`. |

---

## F. RECOMMENDATION — WAVE SIZE

### Ship all 8 sites in ONE commit.

| Metric | Value |
|---|---|
| Sites | **8** |
| ViewModels | **7** (`OrganizationPageViewModel`, `SpecialistPageViewModel`, `SpecialistProfileViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel`, `ServicePageViewModel`, `ServiceProfileViewModel`) |
| Files | 7 prod + 7 test = **14** |
| `using` additions | 1 prod (`SpecialistAvailabilityViewModel.cs`) + 3 test (`SpecialistScheduleViewModelTests`, `SpecialistAvailabilityViewModelTests`, `ServicePageViewModelTests`) |
| Estimated tests | ~10–14 in-place assertion flips + ~1–2 new (`SpecialistScheduleViewModel.TryMutateAsync` surface no-leak; optionally `SpecialistAvailabilityViewModel`); suite ≈ 2,714 → **~2,716** |
| Risk | **LOW** — identical single-shape change; `State = Error`, the two `UnauthorizedOperationException` typed branches, the `[CallerMemberName]` argument, the stale-response guards, and all logging are preserved; every failure path already has a test + a seam |

**No split needed.** Organization + Specialists + Services is one coherent "business-structure / catalog" cluster, reviewed together. The `SpecialistScheduleViewModel.TryMutateAsync` fix improves 8 command callers in a single edit.

### Implementation plan (Phase 8.112)

- **Prod files (7) — each: drop the `catch` variable, `= exception.Message` → `= Strings.Common_ActionFailedMessage` (Organization: fully-qualified); nothing else moves:**
  - `OrganizationPageViewModel.cs` — `LoadAsync` (1); FQ constant, no `using`
  - `SpecialistPageViewModel.cs` — `LoadAsync` (1)
  - `SpecialistProfileViewModel.cs` — `LoadAsync` (1)
  - `SpecialistScheduleViewModel.cs` — `LoadAsync` (1) + `TryMutateAsync` (1); keep both `catch (UnauthorizedOperationException)` branches ahead of the general catch; keep `[CallerMemberName] operationName` + the success `ErrorMessage = null`
  - `SpecialistAvailabilityViewModel.cs` — `+ using Rojan.Desktop.Presentation.Localization;`; `LoadAsync` (1)
  - `ServicePageViewModel.cs` — `LoadAsync` (1)
  - `ServiceProfileViewModel.cs` — `LoadAsync` (1)
- **Test files (7):** flip `Assert.Equal("boom", sut.ErrorMessage)` → `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` (all 7); add `+ using …Localization;` to the 3 that lack it; add a `SpecialistScheduleViewModelTests` `TryMutateAsync` general-failure surface no-leak test.
- **No** DI / service / contract / `.resx` / stub / new-file change.
- **Commit subject:** `fix(desktop): sanitize organization, specialists and services error surfacing`

### Separate from Missing-Guard work

Missing-Guard Sweep (`794648e` … `0260bc3`) is complete. This changes the *message string* in *pre-existing* catches — no new guard, no behaviour.

---

## STOP

Phase 8.111 audit complete. HEAD `1260d4e`, tracked tree clean, baseline 2,714 / 2,714.
**8 Category-A sites across 7 ViewModels** — `OrganizationPageViewModel.LoadAsync`, `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync`, `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync`, `ServiceProfileViewModel.LoadAsync` — surface `exception.Message` to a bound `TextBlock`, exposing company/branch/RBAC data, staff PII / availability, and service pricing / configuration. Uniform behaviour-neutral fix: **drop the `catch` variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**; keep `State = Error`, the two `UnauthorizedOperationException` typed branches in `SpecialistScheduleViewModel`, the `[CallerMemberName]` mutation argument, the stale-response guards, and every operation-name-only log call. 1 prod (`SpecialistAvailabilityViewModel`) + 3 test files need `+ using …Localization;`; `OrganizationPageViewModel` keeps its fully-qualified form. **No `.resx` / DI / service / contract / stub change.**
**Recommendation: one commit, all 8 sites, LOW risk. ~10–14 assertion flips + ~1–2 new tests, suite ~2,716.**

**Awaiting Phase 8.112 authorization.**
