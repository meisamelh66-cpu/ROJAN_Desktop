# ROJAN AI — TEAM 3 — PHASE 8.110 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 2 — COMMIT REPORT v1

**Type:** Commit execution. One commit created. No push / merge / rebase / amend. No source/test change beyond Phase 8.108.
**Authorization:** APPROVED (Phase 8.110 block).

---

## A. NEW HEAD

```
1260d4e  fix(desktop): sanitize customer, HR and membership error surfacing
76d3f61  fix(desktop): sanitize reporting, AI center and accounting error surfacing   (parent)
0260bc3  fix(desktop): guard settings page command failures
```

- **Branch:** `feature/team3-desktop-completion`
- **New HEAD:** `1260d4e` — child of `76d3f61`
- **Not pushed.** Tracked tree after commit: **clean**; untracked = `ROJAN_*.md` reports only.

### Staged & committed — 8 files, exactly the approved set

```
 src/Rojan.Desktop.Presentation/ViewModels/Customers/CustomerPageViewModel.cs        |  4 +--
 src/Rojan.Desktop.Presentation/ViewModels/HR/HrPageViewModel.cs                     |  8 ++---
 src/Rojan.Desktop.Presentation/ViewModels/HR/EmployeeProfileViewModel.cs            |  4 +--
 src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs       |  8 ++---
 tests/.../Customers/CustomerPageViewModelTests.cs                                   | 19 ++++++-----
 tests/.../HR/HrPageViewModelTests.cs                                                | 36 ++++++++++++++++-----
 tests/.../HR/EmployeeProfileViewModelTests.cs                                       | 10 +++---
 tests/.../Membership/AcceptInviteViewModelTests.cs                                  | 23 +++++++++-----
 8 files changed, 73 insertions(+), 39 deletions(-)
```

Staging: `git reset` then explicit per-path `git add` (no `git add .` / `-A`). No report `.md` staged.

### Commit message (as committed)

```
fix(desktop): sanitize customer, HR and membership error surfacing

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

Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

---

## B. SANITIZED SITES — 6 of the 7 audited

Phase 8.107 §B scoped **7** sites / 5 VMs. Phase 8.108's STRICT SCOPE authorised **4 production files** — omitting `CustomerProfileViewModel.cs`. This commit sanitizes **6 sites**; `CustomerProfileViewModel.LoadAsync` (`:274`, `ErrorMessage`) is a documented follow-up (its `CustomerProfileViewModelTests` assertions remain green, unchanged — no false state).

| # | VM · method | Surface | `State = Error` | `finally` / guard |
|---|---|---|---|---|
| 1 | `CustomerPageViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | inside `if (requestVersion == _filterVersion)` — unchanged |
| 2 | `HrPageViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | loads employees + commission rules + **commission transactions** + **payroll summaries** in the same `try` |
| 3 | `HrPageViewModel.SearchAsync` | `ErrorMessage` | ✅ kept | inside `if (searchText == SearchText)` — unchanged |
| 4 | `EmployeeProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ kept | — |
| 5 | `AcceptInviteViewModel.LookupAsync` | `LookupErrorMessage` | n/a | `finally { IsLookingUp = false; }` — unchanged |
| 6 | `AcceptInviteViewModel.AcceptAsync` | `AcceptErrorMessage` | n/a | `finally { IsAccepting = false; }` — unchanged |

Each: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`.

**Byte-unchanged:** every `State = DashboardState.Error`, the `HasLookupError` / `HasAcceptError` computed flags, the `CustomerPageViewModel` stale-response guard, the `HrPageViewModel.SearchAsync` out-of-order guard, both `AcceptInviteViewModel` `finally` blocks, every `#pragma warning disable/restore CA1031` comment, every `LogOperationFailed(nameof(<Method>))` call, every `[LoggerMessage]` signature. **No `using` additions in any prod file** (all 4 already `using Rojan.Desktop.Presentation.Localization;`).

---

## C. ACCEPTINVITE TOKEN-LEAK CLOSURE

`AcceptInviteViewModel` was the standout: Phase 8.35 hardened its **log** to be token-safe + identity-safe, but the **UI surface** leaked, and the tests documented it as intentional-for-now.

| Before `1260d4e` | After `1260d4e` |
|---|---|
| `AcceptInviteViewModelTests:144` asserted `Assert.Contains(SecretToken, sut.LookupErrorMessage!, …); // the user still sees the raw backend message` | now `Assert.Equal(Strings.Common_ActionFailedMessage, sut.LookupErrorMessage)` + `Assert.DoesNotContain(SecretToken, sut.LookupErrorMessage!)` |
| `:51` asserted `Assert.Equal("Salon invite not found or no longer available", sut.LookupErrorMessage)` (a raw stubbed backend string) | now `Assert.Equal(Strings.Common_ActionFailedMessage, sut.LookupErrorMessage)` |
| `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` checked only the log — `AcceptErrorMessage` silently held `"accept failed for <SecretToken>"` | now `+ Assert.Equal(Strings.Common_ActionFailedMessage, sut.AcceptErrorMessage)` + `Assert.DoesNotContain(SecretToken, sut.AcceptErrorMessage!)` |
| `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` checked only the log — `AcceptErrorMessage` silently held `"session resolution failed for user owner@salon.example (id u-4821)"` | now `+ Assert.DoesNotContain("owner@salon.example" / "u-4821", sut.AcceptErrorMessage!)` |

The invite token, invitee email, and user id are now **structurally unreachable** from both `LookupErrorMessage` and `AcceptErrorMessage` (no exception variable is bound). The Phase 8.35 **log** assertions are retained and still pass. `Token` is still left intact after a failed lookup; the session is still never touched on a failed accept (regression tests unchanged and green).

---

## D. CUSTOMER / HR DATA PROTECTION

| Data class | Was reachable via | Now |
|---|---|---|
| Customer name / phone / email / address / notes | `CustomerPageViewModel.LoadAsync` (filter values / returned records in a backend message) | **not reachable** — sentinel test seeds `"boom for customer Amelia Hart"`, asserts `DoesNotContain("Amelia Hart", sut.ErrorMessage)` |
| **Salary / payroll / commission figures / employee records** | `HrPageViewModel.LoadAsync` / `.SearchAsync` (payroll + commission summaries), `EmployeeProfileViewModel.LoadAsync` (`EmployeeProfileDto` compensation) | **not reachable** — tests seed `"payroll 15,000 …"` / `PiiSecret = "Jordan Lee / jordan.lee@rojan.example / +1 555 / salary 3200"`, assert `DoesNotContain("15,000" / PiiSecret, sut.ErrorMessage)` |
| Backend bodies / internal hosts / file paths / DB fragments | all 6 | **not reachable** — generic constant |

---

## E. TEST DELTA

| | `76d3f61` | `1260d4e` | Δ |
|---|---|---|---|
| Domain | 456 | 456 | — |
| **Presentation** | 770 | **771** | **+1** |
| Application | 791 | 791 | — |
| Infrastructure | 609 | 609 | — |
| Shell | 80 | 80 | — |
| Architecture | 7 | 7 | — |
| **Total** | **2,713** | **2,714** | **+1** |

**+1 net** — `HrPageViewModelTests.SearchAsync_QueryThrows_LogsError_AndSurfacesGenericMessage`, via the pre-existing `StubEmployeeQueryService` `searchEmployees` ctor func (no stub change). Plus ~11 in-place assertion flips (`Assert.Equal("boom" / "Salon invite not found …", surface)` → `Assert.Equal(Strings.Common_ActionFailedMessage, surface)`; `EmployeeProfileViewModelTests` / `AcceptInviteViewModelTests` gained explicit surface `DoesNotContain(...)` assertions) and 6 test renames. **No new test files, no stub changes.**

(Below the ~2,715 estimate — the deferred `CustomerProfileViewModel` site carried ~1 planned test addition.)

---

## F. POST-COMMIT VALIDATION

| Gate | Expected | Actual (at `1260d4e`) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | 2,714 / 2,714 | **2,714 / 2,714 PASS** ✅ (Domain 456, Application 791, Presentation 771, Architecture 7, Shell 80, Infrastructure 609) |
| Architecture tests | 7 / 7 | **7 / 7 PASS** ✅ |

Suite progression: 2,710 (`0260bc3`) → 2,713 (`76d3f61`, sub-wave 1) → **2,714** (`1260d4e`, sub-wave 2, +1).

---

## G. CHECKPOINT

`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` updated: §A HEAD `1260d4e` + banner + audit-phase list (+8.109) + commit chain; §B commit table (+`1260d4e` row); §E `Debug verified at 1260d4e` / `2,714/2,714` / Presentation 771 / progression line; §F new Phase 8.108 detail bullet (incl. the `AcceptInviteViewModel` leak closure + the `CustomerProfileViewModel` deferral); §G P2 track (sub-wave 2 ✅ — 6/7 sites; sub-waves 3–6 + `CustomerProfileViewModel` remain); §H items 1/2/5. No code changed by the checkpoint update.

---

## H. REMAINING P2 SUB-WAVES

`ROJAN_PHASE8_102_*` §F, priority-ordered. Each: one audit → one commit-scope-review → one commit; drop the `catch` variable, swap to `Strings.Common_ActionFailedMessage`, keep `State = Error` + `LogOperationFailed`; **no localization / DI / service / logging change**.

| # | Sub-wave | VMs (sites) |
|---|---|---|
| ~~1~~ | ~~Reporting + AI Center + Accounting/POS~~ | **✅ `76d3f61`** — 11 sites |
| ~~2~~ | ~~Customers + HR + Membership~~ | **✅ `1260d4e`** — 6 of 7 sites (`CustomerProfileViewModel.LoadAsync` deferred) |
| 3 | Organization + Specialists + Services | `OrganizationPageViewModel` (1), `SpecialistPageViewModel` (1), `SpecialistProfileViewModel` (1), `SpecialistScheduleViewModel` (2), `SpecialistAvailabilityViewModel` (1), `ServicePageViewModel` (1), `ServiceProfileViewModel` (1) = **8** |
| 4 | Automation tabs | `WorkflowsTabViewModel` (5), `ScheduledJobsTabViewModel` (3), `BusinessRulesTabViewModel` (2), `ApprovalsTabViewModel` (2), `AutomationDashboardTabViewModel` (1) = **13** — **keep the `when (exception is not OperationCanceledException)` filter** |
| 5 | Booking + Calendar + Inventory | `BookingPageViewModel` (5), `CalendarPageViewModel` (3), `InventoryPageViewModel` (2), `InventoryProfileViewModel` (1) = **11** |
| 6 | Dashboard + Analytics + Salon + QR + Support + Settings + **CustomerProfileViewModel** | `DashboardPageViewModel` (1), `AnalyticsPageViewModel` (1), `SalonPageViewModel` (2), `QrCodesPageViewModel` (2), `SupportPageViewModel` (2), `SettingsPageViewModel` (2 — Category D), **`CustomerProfileViewModel.LoadAsync` (1 — carried over)** = **10–12** |

**Recommended next: Phase 8.111 — sub-wave 3 scope audit** (Organization + Specialists + Services). Also still available: Phase 8.99.1 (Settings XAML visibility tweak, LOW risk).

---

## STOP

Phase 8.110 complete. HEAD `1260d4e` (`fix(desktop): sanitize customer, HR and membership error surfacing`), not pushed. Build 0/0, **2,714/2,714** tests pass, Architecture 7/7.
**6 of the 7 audited sub-wave-2 sites sanitized** — `CustomerPageViewModel.LoadAsync`, `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync`, `AcceptInviteViewModel.LookupAsync` / `.AcceptAsync`. `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = Error`, the `Has*Error` flags, the stale-response / out-of-order guards, both invite `finally` blocks, and every operation-name-only log call are byte-unchanged; no `using` / localization / DI / service / contract / stub change. **The `AcceptInviteViewModel` invite-token / invitee-email / user-id UI leak — previously live and test-documented — is closed.** Customer PII and salary / payroll / commission figures no longer reach any UI surface. +1 net test. `CustomerProfileViewModel.LoadAsync` (site 7) was outside this phase's file list and carries into sub-wave 6. Sub-waves 3–6 of the "sanitize load-error surfacing" P2 remain (~42 sites).

**Awaiting next authorization.**
