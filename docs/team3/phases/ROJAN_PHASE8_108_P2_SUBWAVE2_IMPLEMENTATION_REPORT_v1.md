# ROJAN AI — TEAM 3 — PHASE 8.108 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 2 — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.109 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `76d3f61` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_107_P2_SUBWAVE2_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 8 (4 prod + 4 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs        |  4 +--
 src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs                     |  8 ++---
 src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs            |  4 +--
 src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs       |  8 ++---
 tests/Rojan.Desktop.Presentation.Tests/Customers/CustomerPageViewModelTests.cs      | 19 ++++++-----
 tests/Rojan.Desktop.Presentation.Tests/HR/HrPageViewModelTests.cs                   | 36 ++++++++++++++++-----
 tests/Rojan.Desktop.Presentation.Tests/HR/EmployeeProfileViewModelTests.cs          | 10 +++---
 tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs     | 23 +++++++++-----
 8 files changed, 73 insertions(+), 39 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, authentication, other ViewModels. No new files, no new stubs, **no `using` additions** (all 4 prod VMs already `using Rojan.Desktop.Presentation.Localization;`).

### Scope note — one audited site deferred by this phase's file list

Phase 8.107 §B scoped **7 sites / 5 VMs**. Phase 8.108's STRICT SCOPE lists **4 production files** — it omits `CustomerProfileViewModel.cs` (site `:274`, `LoadAsync` → `ErrorMessage`). That one site is therefore **not** touched here (its `CustomerProfileViewModelTests` assertions `Assert.Equal("boom", sut.ErrorMessage)` remain green unchanged). It should be folded into a later sub-wave or a short addendum. **6 of the 7 audited sites are sanitized by this phase.**

---

## B. SITES SANITIZED — 6

| # | VM · method | Surface | `State = Error` | `finally` / guard |
|---|---|---|---|---|
| 1 | `CustomerPageViewModel.LoadAsync` | `ErrorMessage` (inside `if (requestVersion == _filterVersion)`) | ✅ kept | stale-response `if` unchanged |
| 2 | `HrPageViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | — |
| 3 | `HrPageViewModel.SearchAsync` | `ErrorMessage` (inside `if (searchText == SearchText)`) | ✅ kept | out-of-order `if` unchanged |
| 4 | `EmployeeProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | — |
| 5 | `AcceptInviteViewModel.LookupAsync` | `LookupErrorMessage` | n/a | `finally { IsLookingUp = false; }` unchanged |
| 6 | `AcceptInviteViewModel.AcceptAsync` | `AcceptErrorMessage` | n/a | `finally { IsAccepting = false; }` unchanged |

Each: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`.

**Byte-unchanged everywhere:** every `State = DashboardState.Error`, every `#pragma warning disable/restore CA1031` comment, both `AcceptInviteViewModel` `finally` blocks, the `HasLookupError` / `HasAcceptError` computed flags (still `true` for the generic message), the `CustomerPageViewModel` stale-response guard, the `HrPageViewModel.SearchAsync` out-of-order guard, and every `LogOperationFailed(nameof(<Method>))` call + `[LoggerMessage]` signature.

---

## C. SECURITY IMPACT

Every one of the 6 catches now binds **no exception variable** — `.Message` / `.ToString()` / `.InnerException` structurally unreachable from the surface assignment. The bound `TextBlock` receives only `Strings.Common_ActionFailedMessage`.

| Data class | Was reachable via | Now |
|---|---|---|
| **Invite token** (a credential) | `AcceptInviteViewModel.LookupAsync` — **previously live + test-asserted** (`AcceptInviteViewModelTests:144` used to assert `Contains(SecretToken, sut.LookupErrorMessage!)` with the comment *"the user still sees the raw backend message"*) | **not reachable** — that assertion is now `Assert.Equal(Common_ActionFailedMessage, …)` + `Assert.DoesNotContain(SecretToken, sut.LookupErrorMessage!)` |
| **Invite token in `AcceptErrorMessage`** | `AcceptInviteViewModel.AcceptAsync` — previously undetected (`"accept failed for <SecretToken>"`) | **not reachable** — new assertion `DoesNotContain(SecretToken, sut.AcceptErrorMessage!)` |
| **Invitee email + user id** | `AcceptInviteViewModel.AcceptAsync` (session-init failure) — previously undetected (`"session resolution failed for user owner@salon.example (id u-4821)"`) | **not reachable** — new assertions `DoesNotContain("owner@salon.example" / "u-4821", sut.AcceptErrorMessage!)` |
| Customer name / phone / email / address / notes | `CustomerPageViewModel.LoadAsync` (search filters / returned records in a backend message) | **not reachable** — test seeds `"boom for customer Amelia Hart"`, asserts `DoesNotContain("Amelia Hart", sut.ErrorMessage)` |
| **Salary / payroll / commission figures** | `HrPageViewModel.LoadAsync` / `.SearchAsync` (payroll + commission summaries), `EmployeeProfileViewModel.LoadAsync` (`EmployeeProfileDto` compensation) | **not reachable** — tests seed `"payroll 15,000 …"` / `PiiSecret = "Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200"`, assert `DoesNotContain("15,000" / PiiSecret, sut.ErrorMessage)` |
| Backend bodies / internal hosts / file paths / DB fragments | all 6 | **not reachable** — generic constant |

**Logs unchanged** — operation-name-only in all 6. The Phase 8.35 token-safe / identity-safe **log** assertions in `AcceptInviteViewModelTests` (`DoesNotContain(SecretToken / "owner@salon.example" / "u-4821", entry.Message)`) still pass.

---

## D. TEST CHANGES

**+1 net** (Presentation.Tests 770 → **771**). ~11 in-place assertion flips; 6 tests renamed to reflect the strengthened contract; 1 genuinely new test. No new test files, **no stub changes** — every failure path uses a pre-existing seam.

| File | Detail |
|---|---|
| `CustomerPageViewModelTests` | `Constructor_QueryServiceThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage`; `LoadAsync_QueryServiceThrows_LogsError` → `…_AndSurfacesGenericMessage`. Both: seed `"boom for customer Amelia Hart"`, assert `ErrorMessage == Common_ActionFailedMessage` + `DoesNotContain("Amelia Hart")`. Comment updated. |
| `HrPageViewModelTests` | `Constructor_QueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage`; `LoadAsync_QueryThrows_LogsError` → `…_AndSurfacesGenericMessage`. Both: seed `"boom: payroll 15,000 for New Hire"`, assert the constant + `DoesNotContain("15,000")`. **New:** `SearchAsync_QueryThrows_LogsError_AndSurfacesGenericMessage` (via the pre-existing `StubEmployeeQueryService` `searchEmployees` ctor func). |
| `EmployeeProfileViewModelTests` | `LoadAsync_Failure_LogsErrorWithOperationNameOnly_NoPiiLeak` — **added** `ErrorMessage == Common_ActionFailedMessage` + `DoesNotContain(PiiSecret, sut.ErrorMessage)`. `LoadAsync_Failure_WithoutLogger_…` → `…_AndSurfacesGenericMessage`. `Constructor_ProfileQueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage`. |
| `AcceptInviteViewModelTests` | `+ using …Localization;`. `LookupCommand_Failure_SetsLookupErrorAndLeavesTokenIntact` line 51 → the constant. `LookupCommand_Failure_LogsErrorWithoutLeakingToken` → `…LogsErrorAndSurfacesGenericMessage_NoTokenLeak`; the `Contains(SecretToken, …)` "user still sees the raw backend message" assertion → `Equal(Common_ActionFailedMessage, …)` + `DoesNotContain(SecretToken, …)`. `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` + `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` — **added** `AcceptErrorMessage == Common_ActionFailedMessage` + `DoesNotContain(SecretToken / "owner@salon.example" / "u-4821", sut.AcceptErrorMessage!)`. Phase 8.35 comment updated to note the UI is now also sanitized. |

**Subset run:** Customers + HR + Membership → **247 / 247 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `76d3f61` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,715 | **2,714 / 2,714 PASS** ✅ |
| — Domain | 456 | 456 |
| — **Presentation** | +1 → 771 | **771** ✅ |
| — Application | 791 | 791 |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-2 subset | — | **247 / 247 PASS** ✅ |

Suite progression: 2,713 (`76d3f61`) → **2,714** (+1, P2 sub-wave 2). (Below the ~2,715 estimate — one audited site (`CustomerProfileViewModel`) was outside this phase's file list, so its planned test additions were not made.)

---

## F. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 8 files (4 prod + 4 test), all within the STRICT SCOPE allowance |
| Base HEAD | `76d3f61` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,714 / 2,714; Architecture 7 / 7; subset 247 / 247 |
| Sites | ✅ 6 / 6 (this phase's list) — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / `finally` / guards / log calls byte-unchanged |
| Security | ✅ invite token, invitee email, user id, customer PII, salary/payroll figures, backend bodies all structurally unreachable from every surface; sentinel-enforced — the **live test-documented `AcceptInviteViewModel` token leak is closed** |
| Behaviour | ✅ unchanged — error-state recovery, `Has*Error` flags, stale-response / out-of-order guards, `IsLookingUp` / `IsAccepting` resets all preserved |
| Localization | ✅ no `.resx` change; no `using` additions |
| DI / services / contracts / stubs | ✅ none |
| Deferred | `CustomerProfileViewModel.LoadAsync` (site 7 of 7 from the audit) — outside this phase's file list; fold into a later sub-wave or addendum |
| Line endings | working-copy CRLF; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |
| Proposed commit subject | `fix(desktop): sanitize customer, HR and membership error surfacing` |
| Proposed staged files | the 8 above — **no `git add -A` / `git add .`** |

### Separate from Missing-Guard work

This changes the *message string* in *pre-existing* catches. No new guard, no behaviour. The Missing-Guard Sweep (`794648e` … `0260bc3`) is complete and untouched.

---

## STOP

Phase 8.108 implementation complete. Base HEAD `76d3f61` unchanged (no commit). Build 0/0, **2,714 / 2,714** tests pass, Architecture 7/7, sub-wave-2 subset 247/247.
**6 Category-A sites sanitized** — `CustomerPageViewModel.LoadAsync`, `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync`, `AcceptInviteViewModel.LookupAsync` / `.AcceptAsync`. `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = Error`, both `finally` blocks, the stale-response / out-of-order guards, and every operation-name-only log call are byte-unchanged; no `using` / localization / DI / service / contract / stub change. **The invite token, invitee email, and user id are no longer reachable from any UI surface** — including the previously live, test-documented `LookupErrorMessage` token leak. +1 net test. One audited site (`CustomerProfileViewModel`) was outside this phase's file list and remains for a follow-up.

**Awaiting Phase 8.109 — Sub-Wave 2 Commit Scope Review.**
