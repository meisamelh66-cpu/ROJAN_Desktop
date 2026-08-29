# ROJAN AI — TEAM 3 — PHASE 8.113 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 3 — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No** source / test / fix / refactor / commit / push / merge / rebase / amend. Nothing staged.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `1260d4e` (unchanged)
**Reference:** `ROJAN_PHASE8_111_P2_SUBWAVE3_SCOPE_AUDIT_v1.md`, `ROJAN_PHASE8_112_P2_SUBWAVE3_IMPLEMENTATION_REPORT_v1.md`
**Verdict: READY TO COMMIT** at Phase 8.114.

---

## A. GIT STATE

```
git rev-parse HEAD        → 1260d4eee70191d6c306145d2de32b5c57d46eb7
git branch --show-current → feature/team3-desktop-completion
git diff --cached --stat  → (empty — nothing staged)
```

### Modified tracked files — 14, all Phase 8.112

```
 M src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Services/ServicePageViewModel.cs
 M src/Rojan.Desktop.Presentation/ViewModels/Services/ServiceProfileViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistProfileViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistAvailabilityViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Services/ServicePageViewModelTests.cs
 M tests/Rojan.Desktop.Presentation.Tests/Services/ServiceProfileViewModelTests.cs
```

Diffstat: `14 files changed, 84 insertions(+), 48 deletions(-)`. Untracked: only `ROJAN_*.md`. **Confirmed: only Phase 8.112 changes present; staging empty.**

The 48 deletions inspected — all are: (a) `catch (Exception exception)` → `catch (Exception)` and `= exception.Message` lines, (b) seed exception-message strings lengthened (`"boom"` → `"boom: price 45.00 / …"` etc.), (c) comment updates, (d) test-method renames. **No test removed.**

**Line endings:** several edited files show `git diff` warnings *"LF will be replaced by CRLF the next time Git touches it"* — the edit tool wrote LF into changed regions of CRLF files, making them momentarily mixed in the working copy. `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent, and every existing `.cs` blob in this repo is LF). **Cosmetic only — build/tests unaffected** (same as phases 8.78 / 8.86 / 8.104 / 8.108).

---

## B. SCOPE

| Required prod file | Modified? | Notes |
|---|---|---|
| `OrganizationPageViewModel.cs` | ✅ | 1 catch (`LoadAsync`); `= Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage` (fully-qualified — matches the file's Wave D guards; **no `using`**) |
| `SpecialistPageViewModel.cs` | ✅ | 1 catch (`LoadAsync`, inside `if (requestVersion == _filterVersion)`); `= Strings.Common_ActionFailedMessage`; static-form `LogOperationFailed(Logger, …)` unchanged |
| `SpecialistProfileViewModel.cs` | ✅ | 1 catch (`LoadAsync`); `= Strings.Common_ActionFailedMessage` |
| `SpecialistScheduleViewModel.cs` | ✅ | 2 catches (`LoadAsync`, `TryMutateAsync`); `= Strings.Common_ActionFailedMessage`. **Both `catch (UnauthorizedOperationException)` typed branches (lines ~274, ~464) are outside the diff — byte-unchanged.** `[CallerMemberName] operationName` and `TryMutateAsync`'s success path unchanged. |
| `SpecialistAvailabilityViewModel.cs` | ✅ | `+ using Rojan.Desktop.Presentation.Localization;` (1 line); 1 catch (`LoadAsync`); `= Strings.Common_ActionFailedMessage`; `LogLoadFailed(nameof(LoadAsync))` unchanged |
| `ServicePageViewModel.cs` | ✅ | 1 catch (`LoadAsync`, inside `if (requestVersion == _filterVersion)`); `= Strings.Common_ActionFailedMessage` |
| `ServiceProfileViewModel.cs` | ✅ | 1 catch (`LoadAsync`); `= Strings.Common_ActionFailedMessage` |

| Test files | 7 — `OrganizationPageViewModelTests`, `SpecialistPageViewModelTests`, `SpecialistProfileViewModelTests`, `SpecialistScheduleViewModelTests`, `SpecialistAvailabilityViewModelTests`, `ServicePageViewModelTests`, `ServiceProfileViewModelTests`. All existing, all directly related. `+ using …Localization;` added to `SpecialistScheduleViewModelTests`, `SpecialistAvailabilityViewModelTests`, `ServicePageViewModelTests`. |

| Must stay untouched | Status |
|---|---|
| Services / query & command services | ✅ not in diff |
| Backend contracts / DTOs | ✅ not in diff |
| DI registration | ✅ not in diff |
| Localization files (`Strings.resx` / `.en` / `.ar`) | ✅ not in diff — `Common_ActionFailedMessage` reused (Wave A) |
| Test stubs (`StubSpecialistScheduleQueryService` / `…CommandService`, `StubSpecialistQueryService`, `StubServiceQueryService`, `StubOrganizationQueryService`, `StubServiceProfileQueryService`, `StubSpecialistProfileQueryService`) | ✅ not in diff — every failure path uses a pre-existing seam |
| Shell / `MainWindowViewModel` / navigation / authentication | ✅ not in diff |
| Any other ViewModel | ✅ not in diff — sub-waves 4–6 untouched |

**14 files, 100% within the STRICT SCOPE allowance.**

---

## C. SANITIZATION — 8/8 verified against the diff

| # | VM · method | Before | After | `State = Error` | Typed catch / guard | `Log…(nameof())` | `[LoggerMessage]` |
|---|---|---|---|---|---|---|---|
| 1 | `OrganizationPageViewModel.LoadAsync` | `catch (Exception exception) { ErrorMessage = exception.Message; …` | `catch (Exception) { ErrorMessage = Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage; …` | ✅ kept | n/a | ✅ `LogOperationFailed(nameof(LoadAsync))` | ✅ unchanged |
| 2 | `SpecialistPageViewModel.LoadAsync` | `… ErrorMessage = exception.Message` (inside `if (requestVersion == _filterVersion)`) | `… = Strings.Common_ActionFailedMessage` (same `if`) | ✅ kept | ✅ stale-response `if` unchanged | ✅ `LogOperationFailed(Logger, nameof(LoadAsync))` (static form) | ✅ |
| 3 | `SpecialistProfileViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |
| 4 | `SpecialistScheduleViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | ✅ **`catch (UnauthorizedOperationException) { IsPermissionDenied = true; State = Error; LogPermissionDenied(nameof(LoadAsync)); }` precedes the general catch — byte-unchanged** | ✅ `LogOperationFailed(nameof(LoadAsync))` | ✅ (`LogOperationFailed` Error + `LogPermissionDenied` Warning) |
| 5 | `SpecialistScheduleViewModel.TryMutateAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | n/a (mutation — inline `ErrorMessage`) | ✅ **`catch (UnauthorizedOperationException) { IsPermissionDenied = true; LogPermissionDenied(operationName); return false; }` precedes — byte-unchanged**; the `[CallerMemberName] string operationName = ""` parameter; the success path `IsPermissionDenied = false; ErrorMessage = null; return true;`; `return false;` — all unchanged | ✅ `LogOperationFailed(operationName)` | ✅ |
| 6 | `SpecialistAvailabilityViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ `LogLoadFailed(nameof(LoadAsync))` | ✅ |
| 7 | `ServicePageViewModel.LoadAsync` | `ErrorMessage = exception.Message` (inside `if (requestVersion == _filterVersion)`) | `= Strings.Common_ActionFailedMessage` (same `if`) | ✅ kept | ✅ stale-response `if` unchanged | ✅ | ✅ |
| 8 | `ServiceProfileViewModel.LoadAsync` | `ErrorMessage = exception.Message` | `= Strings.Common_ActionFailedMessage` | ✅ kept | n/a | ✅ | ✅ |

Every catch now binds **no exception variable**. Every `#pragma warning disable/restore CA1031` boundary comment is byte-unchanged.

### Confirmed unchanged (TASK C checklist)

| Item | Verified against the diff |
|---|---|
| `State = DashboardState.Error` | ✅ retained at sites 1–4, 6–8; site 5 (`TryMutateAsync`) has no `State` — unchanged |
| `catch (UnauthorizedOperationException)` typed catches | ✅ both (in `LoadAsync` and `TryMutateAsync`) are outside the diff hunks — byte-unchanged, still ahead of the general catch |
| permission-denied behaviour | ✅ `IsPermissionDenied` assignment + `LogPermissionDenied` calls unchanged |
| Warning logs | ✅ `[LoggerMessage(… Level = LogLevel.Warning …)] LogPermissionDenied` untouched |
| `[CallerMemberName]` operation names | ✅ `TryMutateAsync(Func<Task> mutate, [CallerMemberName] string operationName = "")` signature unchanged; `LogOperationFailed(operationName)` unchanged |
| `TryMutateAsync` success `ErrorMessage` clearing | ✅ the `try` success block `IsPermissionDenied = false; ErrorMessage = null; return true;` is outside the diff — unchanged |
| stale-response guards | ✅ `SpecialistPageViewModel` / `ServicePageViewModel` `if (requestVersion == _filterVersion)` unchanged |

### Business behaviour — unchanged

- Every page still recovers to the Error state (sites 1–4, 6–8) or shows the inline mutation `ErrorMessage` (site 5) — not a crash.
- The distinct permission-denied state is untouched: `SpecialistScheduleViewModelTests.LoadCommand_UnauthorizedOperationException_SetsIsPermissionDenied_NotGenericError`, `…_LogsAsWarningNotError`, `SetWeeklyAvailabilityCommand_PermissionDenied_SetsIsPermissionDenied_NeverReloads` — all unchanged and green.
- `SpecialistPageViewModel` / `ServicePageViewModel` still discard stale search responses.

---

## D. SECURITY

Every one of the 8 surfaces now assigns the fixed localized constant — the caught exception is **not bound to a variable**, so `.Message` / `.ToString()` / `.InnerException` are structurally unreachable from the surface.

| Data class | Was reachable via | Now |
|---|---|---|
| Company / branch data, **RBAC roles / permission details**, org & branch ids | `OrganizationPageViewModel.LoadAsync` | **not reachable** — test seeds `"boom: branch b-77 / role SalonManager"`, asserts `DoesNotContain("b-77" / "SalonManager", sut.ErrorMessage)` |
| **Staff / specialist identifiers, specialist PII** (name / email) | `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync` | **not reachable** — tests seed `"…for specialist s-42 / Jordan Lee"` / `"…for Jordan Lee / jordan.lee@rojan.example"`, assert `DoesNotContain("s-42" / "Jordan Lee", sut.ErrorMessage)` |
| **Availability details**, specialist id, time windows | `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync` | **not reachable** — tests seed `"…for specialist-1 / 09:00-17:00"` / `"backend body: specialist-1 / 09:00-13:00 / status 500"`, assert `DoesNotContain("specialist-1" / "status 500", sut.ErrorMessage)` |
| **Pricing / cost / commission %, configuration** | `ServiceProfileViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync` | **not reachable** — tests seed `"boom: price 45.00 / cost 12.00 / commission 15%"`, assert `DoesNotContain("45.00" / "commission", sut.ErrorMessage)` |
| Backend bodies / internal hosts / file paths / DB fragments | all 8 | **not reachable** — generic constant |

### Logs — unchanged, still operation-name-only

All 8 catches keep `LogOperationFailed(nameof(<Method>))` / `LogOperationFailed(Logger, nameof(<Method>))` / `LogLoadFailed(nameof(<Method>))`; the permission branches keep `LogPermissionDenied(...)`. `[LoggerMessage]` message templates byte-unchanged. The pre-existing operation-name-only **log** no-leak assertions (`SpecialistScheduleViewModelTests` `DoesNotContain("specialist-1" / "backend body")`, `SpecialistProfileViewModelTests` `DoesNotContain("ROJAN_Backend" / "status 500")`) are retained and still pass.

**No exception payload reaches the UI or the logs.**

---

## E. TESTS

| Gate | Expected | Actual (working tree = `1260d4e` + Phase 8.112) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full suite | ~2,716 | **2,715 / 2,715 PASS** ✅ |
| — Domain / Application / Infrastructure / Shell | 456 / 791 / 609 / 80 | unchanged ✅ |
| — **Presentation** | 771 → 772 | **772** (+1) ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-3 subset (Organization + Specialists + Services) | — | **148 / 148 PASS** ✅ |

Suite progression: 2,713 (`76d3f61`) → 2,714 (`1260d4e`, sub-wave 2) → **2,715** (sub-wave 3, +1). (One under the ~2,716 estimate — renamed tests replaced rather than added; only the `TryMutateAsync` test is net-new.)

### Review of the requested test categories

| Category | Present |
|---|---|
| **organization leak tests** | ✅ `OrganizationPageViewModelTests.LoadAsync_QueryThrows_LogsError_AndSurfacesGenericMessage` — `DoesNotContain("b-77" / "SalonManager", sut.ErrorMessage)` |
| **specialist leak tests** | ✅ `SpecialistPageViewModelTests` / `SpecialistProfileViewModelTests` — `DoesNotContain("s-42" / "Jordan Lee")`; `SpecialistScheduleViewModelTests` / `SpecialistAvailabilityViewModelTests` — `DoesNotContain("specialist-1")` |
| **service pricing leak tests** | ✅ `ServicePageViewModelTests` / `ServiceProfileViewModelTests` — `DoesNotContain("45.00" / "commission", sut.ErrorMessage)` |
| **`UnauthorizedOperationException` regression** | ✅ `SpecialistScheduleViewModelTests.LoadCommand_UnauthorizedOperationException_SetsIsPermissionDenied_NotGenericError` / `…_LogsAsWarningNotError` / `SetWeeklyAvailabilityCommand_PermissionDenied_SetsIsPermissionDenied_NeverReloads` — all unchanged and green |

### Test additivity

- **+1 net test** — `SpecialistScheduleViewModelTests.SetWeeklyAvailabilityCommand_BackendThrows_SetsGenericErrorMessage_NoLeak`, via the pre-existing `StubSpecialistScheduleCommandService.Fail` seam (an `InvalidOperationException`, not `UnauthorizedOperationException`) — directly covers site 5 (`TryMutateAsync`), asserts `IsPermissionDenied == false`, the generic constant, and `DoesNotContain("specialist-1" / "status 500")`.
- ~12 in-place assertion flips + 9 test renames.
- No new test files, **no stub changes**.

---

## F. COMMIT READINESS

| Gate | State |
|---|---|
| Scope | ✅ 14 files (7 prod + 7 test), all authorised |
| Base HEAD | `1260d4e` — unchanged; staging empty |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; subset 148 / 148 |
| Sanitization | ✅ 8/8 — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / the two `UnauthorizedOperationException` branches / `[CallerMemberName]` / `TryMutateAsync` success path / stale-response guards / log calls byte-unchanged |
| Security | ✅ RBAC roles/permissions, staff PII, specialist ids, availability data, service pricing/cost/commission, backend bodies structurally unreachable from every surface; sentinel-enforced |
| Behaviour | ✅ unchanged — error-state recovery, permission-denied path, mutation success/failure semantics, stale-response guards all preserved |
| Localization | ✅ no `.resx` change; `+ using …Localization;` in 1 prod + 3 test files only |
| DI / services / contracts / stubs | ✅ none |
| Line endings | mixed LF/CRLF in the working copy of some edited files; `core.autocrlf=true` → LF in the committed blob (repo-consistent) — cosmetic only |

### Proposed commit

**Subject (as given in the Phase 8.113 brief):**
```
fix(desktop): sanitize organization specialists and services error surfacing
```
*(Note: the Phase 8.112 report proposed `fix(desktop): sanitize organization, specialists and services error surfacing` with a comma; the Phase 8.113 brief omits it. The Phase 8.114 authorisation block is the authority on the exact string.)*

**Body (suggested):**
```
Swap the raw exception.Message in the pre-existing top-level broad
catches to the generic Strings.Common_ActionFailedMessage so a failed
load/mutation shows a safe message instead of RBAC role/permission
strings, staff PII, a specialist id, availability windows, or service
pricing / cost / commission data.

- OrganizationPageViewModel: LoadAsync
- SpecialistPageViewModel: LoadAsync
- SpecialistProfileViewModel: LoadAsync
- SpecialistScheduleViewModel: LoadAsync, TryMutateAsync (shared 8-caller
  mutation boundary)
- SpecialistAvailabilityViewModel: LoadAsync
- ServicePageViewModel: LoadAsync
- ServiceProfileViewModel: LoadAsync

Each catch now binds no exception variable. State = Error, both
UnauthorizedOperationException typed branches (IsPermissionDenied +
Warning log), the [CallerMemberName] operation name, TryMutateAsync's
success-path ErrorMessage clearing, and the stale-response guards are
unchanged. SpecialistAvailabilityViewModel + 3 test files gain a
using; OrganizationPageViewModel keeps its fully-qualified form. No
.resx, DI, service or contract change. +1 test.
```

**Trailers (required):**
```
Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>
Claude-Session: https://claude.ai/code/session_018qKcQuzpsf2kvARD6nVjVX
```

### Proposed staging (Phase 8.114 — explicit paths, NO `git add -A` / `git add .`)

```
git add \
  src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Services/ServicePageViewModel.cs \
  src/Rojan.Desktop.Presentation/ViewModels/Services/ServiceProfileViewModel.cs \
  tests/Rojan.Desktop.Presentation.Tests/Organizations/OrganizationPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistPageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistProfileViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistAvailabilityViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Services/ServicePageViewModelTests.cs \
  tests/Rojan.Desktop.Presentation.Tests/Services/ServiceProfileViewModelTests.cs
```

Expected post-commit: new HEAD child of `1260d4e`; `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` update (§B commit table, §E 2,714 → 2,715, §G P2 sub-wave-3 ✅, sub-waves 4–6 + `CustomerProfileViewModel` remain).

---

## STOP

Phase 8.113 review complete. **Verdict: READY.** HEAD `1260d4e`, staging empty, 14 sub-wave-3 files modified and nothing else, build 0/0, 2,715/2,715, Architecture 7/7, subset 148/148. All 8 sites drop the `catch` variable and swap `exception.Message` → `Strings.Common_ActionFailedMessage`; `State = Error`, both `UnauthorizedOperationException` typed branches, the `[CallerMemberName]` operation name, `TryMutateAsync`'s success-path `ErrorMessage` clearing, and both stale-response guards are byte-unchanged; no `.resx` / DI / service / contract / stub change. RBAC roles/permissions, staff PII, specialist ids, availability data, and service pricing/cost/commission are no longer reachable from any UI surface.

**Awaiting Phase 8.114 — Sub-Wave 3 Commit Authorization.**
