# ROJAN AI — TEAM 3 — PHASE 8.109 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 2 — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No** source / test / fix / new-file / commit / push / merge / rebase / amend. Nothing staged.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `76d3f61` (unchanged)
**Reference:** `ROJAN_PHASE8_107_P2_SUBWAVE2_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_108_P2_SUBWAVE2_IMPLEMENTATION_REPORT_v1.md`
**Verdict: READY TO COMMIT** at Phase 8.110. One audited site (`CustomerProfileViewModel`) was outside this phase's authorised file list and is a documented follow-up — see §C.

---

## A. GIT STATE

```
git rev-parse HEAD        → 76d3f61228e9ff5c6275bb1ed57508072dd66cee
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
```

### Modified tracked files — 8, all Phase 8.108

```
 M src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
```

Diffstat: `8 files changed, 73 insertions(+), 39 deletions(-)`. Untracked: only `ROJAN_*.md`. **Confirmed: only Phase 8.108 changes present; staging empty.**

The 39 deletions inspected — all are: (a) `catch (Exception exception)` → `catch (Exception)` and `= exception.Message` lines, (b) two test-method renames (signature-line changes), (c) comment reflow / updates. **No test removed.**

---

## B. SCOPE

| Required prod file | Modified? | Notes |
|---|---|---|
| `CustomerPageViewModel.cs` | ✅ | 1 catch (`LoadAsync`); `= Strings.Common_ActionFailedMessage` (file already `using …Localization;`) |
| `HrPageViewModel.cs` | ✅ | 2 catches (`LoadAsync`, `SearchAsync`); `= Strings.Common_ActionFailedMessage` |
| `EmployeeProfileViewModel.cs` | ✅ | 1 catch (`LoadAsync`); `= Strings.Common_ActionFailedMessage` |
| `AcceptInviteViewModel.cs` | ✅ | 2 catches (`LookupAsync` → `LookupErrorMessage`, `AcceptAsync` → `AcceptErrorMessage`); `= Strings.Common_ActionFailedMessage` |

**No `using` additions in any prod file** — all 4 already import `Rojan.Desktop.Presentation.Localization`.

| Test files | 4 — `CustomerPageViewModelTests`, `HrPageViewModelTests`, `EmployeeProfileViewModelTests`, `AcceptInviteViewModelTests`. All existing, all directly related. `AcceptInviteViewModelTests` gained `+ using …Localization;` (the only new using anywhere in the diff). |

| Must stay untouched | Status |
|---|---|
| Services / query & command services | ✅ not in diff |
| Backend contracts / DTOs | ✅ not in diff |
| DI registration | ✅ not in diff |
| Localization files (`Strings.resx` / `.en` / `.ar`) | ✅ not in diff — `Common_ActionFailedMessage` reused (Wave A) |
| Test stubs (`StubEmployeeQueryService`, `StubCustomerQueryService`, `StubSalonInviteService`, `StubCurrentSessionService`) | ✅ not in diff — every failure path uses a pre-existing seam |
| Shell / `MainWindowViewModel` / navigation / authentication | ✅ not in diff |
| Any other ViewModel | ✅ not in diff — `CustomerProfileViewModel.cs` **not** modified (see §C) |

**8 files, 100% within the STRICT SCOPE allowance.**

---

## C. SANITIZATION — 6 of the 7 audited sites

The Phase 8.107 audit scoped **7** sites. This phase's STRICT SCOPE authorised **4 production files** — omitting `CustomerProfileViewModel.cs`. So **6 sites** are sanitized here; `CustomerProfileViewModel.LoadAsync` (`:274`, `ErrorMessage`) is **deferred** (its `CustomerProfileViewModelTests` assertions `Assert.Equal("boom", sut.ErrorMessage)` remain green, unchanged — no false state).

| # | VM · method | Before | After | `State = Error` | `finally` / guard | `Log…(nameof())` | `[LoggerMessage]` |
|---|---|---|---|---|---|---|---|
| 1 | `CustomerPageViewModel.LoadAsync` | `catch (Exception exception) { if (requestVersion == _filterVersion) { ErrorMessage = exception.Message; …` | `catch (Exception) { if (…) { ErrorMessage = Strings.Common_ActionFailedMessage; …` | ✅ kept | ✅ stale-response `if (requestVersion == _filterVersion)` unchanged | ✅ `LogOperationFailed(nameof(LoadAsync))` | ✅ unchanged |
| 2 | `HrPageViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `ErrorMessage = Strings.Common_ActionFailedMessage` | ✅ kept | n/a — loads employees + commission rules + **commission transactions** + **payroll summaries** in the same `try` | ✅ | ✅ |
| 3 | `HrPageViewModel.SearchAsync` | `ErrorMessage = exception.Message` (inside `if (searchText == SearchText)`) | `= Strings.Common_ActionFailedMessage` (same `if`) | ✅ kept | ✅ out-of-order `if (string.Equals(searchText, SearchText, …))` unchanged | ✅ | ✅ |
| 4 | `EmployeeProfileViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 5 | `AcceptInviteViewModel.LookupAsync` | `LookupErrorMessage = exception.Message` | `LookupErrorMessage = Strings.Common_ActionFailedMessage` | n/a | ✅ `finally { IsLookingUp = false; }` unchanged | ✅ `LogOperationFailed(nameof(LookupAsync))` | ✅ |
| 6 | `AcceptInviteViewModel.AcceptAsync` | `AcceptErrorMessage = exception.Message` | `AcceptErrorMessage = Strings.Common_ActionFailedMessage` | n/a | ✅ `finally { IsAccepting = false; }` unchanged | ✅ `LogOperationFailed(nameof(AcceptAsync))` | ✅ |

Every catch now binds **no exception variable**. Every `#pragma warning disable/restore CA1031` boundary comment is byte-unchanged.

### Confirmed unchanged (TASK C checklist)

| Item | Verified against the diff |
|---|---|
| `State = DashboardState.Error` | ✅ retained at sites 1–4 (the 4 with a `State`); sites 5–6 have no `State` (invite page uses `Has*Error` flags) — unchanged |
| `Has*Error` flags | ✅ `HasLookupError` / `HasAcceptError` are computed `!string.IsNullOrEmpty(...)` — still `true` for the non-empty generic message; not in the diff |
| stale-response guard | ✅ `CustomerPageViewModel.LoadAsync` `if (requestVersion == _filterVersion)` unchanged |
| out-of-order guard | ✅ `HrPageViewModel.SearchAsync` `if (string.Equals(searchText, SearchText, …))` unchanged |
| `finally` blocks | ✅ both `AcceptInviteViewModel` `finally { Is… = false; }` unchanged |
| `LoggerMessage` calls | ✅ all 6 keep `LogOperationFailed(nameof(<Method>))`; `[LoggerMessage]` templates byte-unchanged |

### Business behaviour — unchanged

- Every affected page still recovers to the Error state (sites 1–4) or shows the inline `Has*Error` message (sites 5–6) — not a crash.
- `HrPageViewModel.SearchAsync` still only surfaces if the response is still current.
- `AcceptInviteViewModel` — `Token` is still left intact after a failed lookup (`LookupCommand_Failure_SetsLookupErrorAndLeavesTokenIntact` still asserts `sut.Token == "does-not-exist"`); the session is still never touched on a failed accept (`AcceptCommand_Failure_SetsAcceptErrorAndNeverTouchesTheSession` unchanged and green).

---

## D. SECURITY

Every one of the 6 surfaces now assigns the fixed localized constant `Strings.Common_ActionFailedMessage` — the caught exception is **not bound to a variable**, so `.Message` / `.ToString()` / `.InnerException` are structurally unreachable from the surface.

| Data class | Was reachable via | Now |
|---|---|---|
| Customer name / phone / email / address / notes | `CustomerPageViewModel.LoadAsync` (filter values / returned records in a backend message) | **not reachable** — test seeds `"boom for customer Amelia Hart"`, asserts `DoesNotContain("Amelia Hart", sut.ErrorMessage)` |
| **Salary / payroll / commission figures / employee records** | `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync` | **not reachable** — tests seed `"payroll 15,000 …"` / `PiiSecret = "Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200"`, assert `DoesNotContain("15,000" / PiiSecret, sut.ErrorMessage)` |
| **Invite token** (a credential) | `AcceptInviteViewModel.LookupAsync` — **previously live + test-asserted** at `:144` (`Contains(SecretToken, sut.LookupErrorMessage!)`, comment *"the user still sees the raw backend message"*) | **not reachable** — that assertion is now `Equal(Common_ActionFailedMessage, …)` + `DoesNotContain(SecretToken, sut.LookupErrorMessage!)` |
| **Invite token in `AcceptErrorMessage`** | `AcceptInviteViewModel.AcceptAsync` — previously undetected | **not reachable** — new `DoesNotContain(SecretToken, sut.AcceptErrorMessage!)` |
| **Invitee email + user id** | `AcceptInviteViewModel.AcceptAsync` (session-init failure) — previously undetected | **not reachable** — new `DoesNotContain("owner@salon.example" / "u-4821", sut.AcceptErrorMessage!)` |
| Salon id / role | `AcceptInviteViewModel` (both) | **not reachable** — generic constant |
| Backend bodies / internal hosts / file paths / DB fragments | all 6 | **not reachable** — generic constant |

### Logs — unchanged, still operation-name-only

All 6 catches keep `LogOperationFailed(nameof(<Method>))`. The Phase 8.35 token-safe / identity-safe **log** assertions in `AcceptInviteViewModelTests` (`DoesNotContain(SecretToken / "owner@salon.example" / "u-4821", entry.Message)`) are retained and still pass. `[LoggerMessage]` message templates (`"Customer page operation failed. …"`, `"HR page operation failed. …"`, `"Employee profile operation failed. …"`, `"Invite operation failed. Operation={Operation}"`) byte-unchanged.

---

## E. TESTS

| Gate | Expected | Actual (working tree = `76d3f61` + Phase 8.108) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full suite | 2,714 / 2,714 | **2,714 / 2,714 PASS** ✅ |
| — Domain / Application / Infrastructure / Shell | 456 / 791 / 609 / 80 | unchanged ✅ |
| — **Presentation** | 770 → 771 | **771** (+1) ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-2 subset (Customers + HR + Membership) | — | **247 / 247 PASS** ✅ |

Suite progression: 2,710 (`0260bc3`) → 2,713 (`76d3f61`, sub-wave 1) → **2,714** (sub-wave 2, +1). (Below the ~2,715 estimate — the deferred `CustomerProfileViewModel` site carried ~1 planned test addition.)

### Review of the requested test categories

| Category | Present |
|---|---|
| **token leak tests** | ✅ `AcceptInviteViewModelTests.LookupCommand_Failure_LogsErrorAndSurfacesGenericMessage_NoTokenLeak` + `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` (now `+ DoesNotContain(SecretToken, sut.AcceptErrorMessage!)`) |
| **email / user-id leak tests** | ✅ `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` — `+ DoesNotContain("owner@salon.example" / "u-4821", sut.AcceptErrorMessage!)` |
| **generic error assertions** | ✅ every failure test now asserts `Assert.Equal(Strings.Common_ActionFailedMessage, <surface>)` |
| **regression / no-behaviour-change** | ✅ `LookupCommand_Failure_SetsLookupErrorAndLeavesTokenIntact` (token retained), `AcceptCommand_Failure_SetsAcceptErrorAndNeverTouchesTheSession` (session untouched), `NoLoggerSupplied_…NeverThrows`, `LookupCommand_Success_…`, `AcceptCommand_Success_…` all unchanged and green |

### Test additivity

- **+1 net test** — `HrPageViewModelTests.SearchAsync_QueryThrows_LogsError_AndSurfacesGenericMessage`, via the pre-existing `StubEmployeeQueryService` `searchEmployees` ctor func (no stub change).
- ~11 in-place assertion flips (`Assert.Equal("boom" / "Salon invite not found …", surface)` → `Assert.Equal(Strings.Common_ActionFailedMessage, surface)`; `EmployeeProfileViewModelTests` / `AcceptInviteViewModelTests` gained explicit surface `DoesNotContain(...)` assertions).
- 6 tests renamed to reflect the strengthened contract.
- **No new test files, no stub changes.**

---

## F. COMMIT READINESS

| Gate | State |
|---|---|
| Scope | ✅ 8 files (4 prod + 4 test), all authorised |
| Base HEAD | `76d3f61` — unchanged; staging empty |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,714 / 2,714; Architecture 7 / 7; subset 247 / 247 |
| Sanitization | ✅ 6/6 (this phase's file list) — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / `finally` / guards / log calls byte-unchanged |
| Security | ✅ invite token / email / user id / customer PII / salary-payroll figures / backend bodies structurally unreachable from every surface; sentinel-enforced — the **live test-documented `AcceptInviteViewModel` token leak is closed** |
| Behaviour | ✅ unchanged — error-state recovery, `Has*Error` flags, stale-response / out-of-order guards, token-retained / session-untouched semantics all preserved |
| Localization | ✅ no `.resx` change; no `using` additions in prod |
| DI / services / contracts / stubs | ✅ none |
| Deferred | `CustomerProfileViewModel.LoadAsync` (site 7 of the audit) — outside this phase's file list; documented for a follow-up |
| Line endings | working-copy CRLF; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |

### Proposed commit

**Subject:**
```
fix(desktop): sanitize customer, HR and membership error surfacing
```

**Body (suggested):**
```
Swap the raw exception.Message in the pre-existing top-level broad
catches to the generic Strings.Common_ActionFailedMessage so a failed
load/search/lookup/accept shows a safe message instead of customer
PII, salary/payroll figures, or - previously live and test-documented
- the invite token, invitee email, and user id.

- CustomerPageViewModel: LoadAsync
- HrPageViewModel: LoadAsync, SearchAsync
- EmployeeProfileViewModel: LoadAsync
- AcceptInviteViewModel: LookupAsync, AcceptAsync

Each catch now binds no exception variable. State = Error, the
Has*Error flags, the stale-response / out-of-order guards, both invite
finally blocks, and every operation-name-only [LoggerMessage] call are
unchanged. No localization, DI, service or contract change. +1 test;
the AcceptInviteViewModel LookupErrorMessage / AcceptErrorMessage
token/email/user-id leak is now closed.

CustomerProfileViewModel.LoadAsync is a separate follow-up.
```

**Trailers (required):**
```
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

### Proposed staging (Phase 8.110 — explicit paths, NO `git add -A` / `git add .`)

```
git add \
  src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs \
  tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
```

Expected post-commit: new HEAD child of `76d3f61`; `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update (§B commit table, §E 2,713 → 2,714, §G P2 sub-wave-2 ✅ (6/7 sites; `CustomerProfileViewModel` follow-up), sub-waves 3–6 remain).

---

## STOP

Phase 8.109 review complete. **Verdict: READY.** HEAD `76d3f61`, staging empty, 8 sub-wave-2 files modified and nothing else, build 0/0, 2,714/2,714, Architecture 7/7, subset 247/247. All 6 sites drop the `catch` variable and swap `exception.Message` → `Strings.Common_ActionFailedMessage`; `State = Error`, the `Has*Error` flags, the stale-response / out-of-order guards, both invite `finally` blocks, and every operation-name-only log call are byte-unchanged; no localization / DI / service / contract / stub change. The confirmed, test-documented `AcceptInviteViewModel` invite-token leak (and the undetected email / user-id leaks in `AcceptErrorMessage`) are closed. `CustomerProfileViewModel.LoadAsync` (1 of the 7 audited sites) was outside this phase's authorised file list and remains a documented follow-up.

**Awaiting Phase 8.110 — Sub-Wave 2 Commit Authorization.**
