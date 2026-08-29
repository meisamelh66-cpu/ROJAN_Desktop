# ROJAN AI — TEAM 3 — PHASE 8.112 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 3 — IMPLEMENTATION v1

**Type:** Implementation. Code + tests changed. **No commit performed** (STOP — Phase 8.113 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `1260d4e` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_111_P2_SUBWAVE3_SCOPE_AUDIT_v1.md`

---

## A. FILES CHANGED — 14 (7 prod + 7 test), all within STRICT SCOPE

```
 src/Rojan.Desktop.Presentation/ViewModels/Organizations/OrganizationPageViewModel.cs      |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs          |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs       |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs      |  8 +++----
 src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistAvailabilityViewModel.cs  |  5 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Services/ServicePageViewModel.cs                |  4 ++--
 src/Rojan.Desktop.Presentation/ViewModels/Services/ServiceProfileViewModel.cs             |  4 ++--
 tests/.../Organizations/OrganizationPageViewModelTests.cs                                 |  8 ++++---
 tests/.../Specialists/SpecialistPageViewModelTests.cs                                     | 12 ++++++----
 tests/.../Specialists/SpecialistProfileViewModelTests.cs                                  | 11 +++++----
 tests/.../Specialists/SpecialistScheduleViewModelTests.cs                                 | 28 ++++++++++++++++---
 tests/.../Specialists/SpecialistAvailabilityViewModelTests.cs                             |  8 ++++---
 tests/.../Services/ServicePageViewModelTests.cs                                           | 21 ++++++++-------
 tests/.../Services/ServiceProfileViewModelTests.cs                                        | 11 +++++----
 14 files changed, 84 insertions(+), 48 deletions(-)
```

**Not touched:** services, backend contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, authentication, other ViewModels. No new files, no new stubs.

### `using` additions — 1 prod + 3 test

| File | Added |
|---|---|
| `SpecialistAvailabilityViewModel.cs` | `+ using Rojan.Desktop.Presentation.Localization;` (the file had no `Strings` reference) |
| `SpecialistScheduleViewModelTests.cs` | `+ using Rojan.Desktop.Presentation.Localization;` |
| `SpecialistAvailabilityViewModelTests.cs` | `+ using Rojan.Desktop.Presentation.Localization;` |
| `ServicePageViewModelTests.cs` | `+ using Rojan.Desktop.Presentation.Localization;` |

`OrganizationPageViewModel.cs` keeps its fully-qualified form (`Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage`) — **no `using`**, consistent with its Wave D guards. The other 5 prod VMs and 4 test files already imported `…Localization`.

---

## B. SITES SANITIZED — 8 / 8

| # | VM · method | Surface | `State = Error` | Preserved |
|---|---|---|---|---|
| 1 | `OrganizationPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | — |
| 2 | `SpecialistPageViewModel.LoadAsync` | `ErrorMessage` | ✅ | stale-response `if (requestVersion == _filterVersion)` |
| 3 | `SpecialistProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ | — |
| 4 | `SpecialistScheduleViewModel.LoadAsync` | `ErrorMessage` | ✅ | **preceding `catch (UnauthorizedOperationException) { IsPermissionDenied = true; State = Error; LogPermissionDenied(nameof(LoadAsync)); }` — byte-unchanged** |
| 5 | `SpecialistScheduleViewModel.TryMutateAsync` | `ErrorMessage` | n/a (mutation) | **preceding `catch (UnauthorizedOperationException) { IsPermissionDenied = true; LogPermissionDenied(operationName); return false; }` — byte-unchanged**; the `[CallerMemberName] string operationName = ""` parameter; the success-path `IsPermissionDenied = false; ErrorMessage = null; return true;`; `LogOperationFailed(operationName)`; `return false;` |
| 6 | `SpecialistAvailabilityViewModel.LoadAsync` | `ErrorMessage` | ✅ | — |
| 7 | `ServicePageViewModel.LoadAsync` | `ErrorMessage` | ✅ | stale-response `if (requestVersion == _filterVersion)` |
| 8 | `ServiceProfileViewModel.LoadAsync` | `ErrorMessage` | ✅ | — |

Each: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }` (Organization: `Rojan.Desktop.Presentation.Localization.Strings.…`).

**Byte-unchanged everywhere:** every `State = DashboardState.Error`, both `catch (UnauthorizedOperationException)` typed branches in `SpecialistScheduleViewModel`, the `[CallerMemberName]` parameter, `TryMutateAsync`'s success path, both stale-response guards, every `#pragma warning disable/restore CA1031` comment, every `LogOperationFailed` / `LogLoadFailed` / `LogPermissionDenied` call, every `[LoggerMessage]` signature.

---

## C. SECURITY IMPACT

Every one of the 8 catches now binds **no exception variable** — `.Message` / `.ToString()` / `.InnerException` structurally unreachable from the surface assignment. The bound `TextBlock` receives only the fixed localized constant.

| Data class | Was reachable via | Now |
|---|---|---|
| Company / branch data, org & branch ids, **RBAC role / permission strings** | `OrganizationPageViewModel.LoadAsync` | **not reachable** — test seeds `"boom: branch b-77 / role SalonManager"`, asserts `DoesNotContain("b-77" / "SalonManager", sut.ErrorMessage)` |
| Staff PII (name / email / phone), **specialist identifiers** | `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync` | **not reachable** — tests seed `"…for specialist s-42 / Jordan Lee"` / `"…for Jordan Lee / jordan.lee@rojan.example"`, assert `DoesNotContain("s-42" / "Jordan Lee", sut.ErrorMessage)` |
| Availability data, specialist id, time windows | `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync` | **not reachable** — tests seed `"…for specialist-1 / 09:00-17:00"` / `"backend body: specialist-1 / 09:00-13:00 / status 500"`, assert `DoesNotContain("specialist-1" / "status 500", sut.ErrorMessage)` |
| **Pricing / cost / commission %, service configuration** | `ServiceProfileViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync` | **not reachable** — tests seed `"boom: price 45.00 / cost 12.00 / commission 15%"`, assert `DoesNotContain("45.00" / "commission", sut.ErrorMessage)` |
| Backend bodies / internal hosts / file paths / DB fragments | all 8 | **not reachable** — generic constant |

**`UnauthorizedOperationException` behaviour unchanged** — `SpecialistScheduleViewModelTests`: `LoadCommand_UnauthorizedOperationException_SetsIsPermissionDenied_NotGenericError` (line ~69), `…_LogsAsWarningNotError` (line ~263), `SetWeeklyAvailabilityCommand_PermissionDenied_SetsIsPermissionDenied_NeverReloads` (line ~125) — all unchanged and green. The permission path still sets `IsPermissionDenied`, logs at **Warning**, never touches `ErrorMessage`.

**Logs unchanged** — operation-name-only in all 8. Pre-existing no-leak log assertions (`SpecialistScheduleViewModelTests.LoadCommand_QueryThrows_LogsTheFailure_OperationNameOnly_NoLeak` — `DoesNotContain("specialist-1" / "backend body")`; `SpecialistProfileViewModelTests` — `DoesNotContain("ROJAN_Backend" / "status 500")`) retained and green.

---

## D. TEST CHANGES

**+1 net** (Presentation.Tests 771 → **772**). ~12 in-place assertion flips; 9 tests renamed to reflect the strengthened contract; 1 genuinely new test. No new test files, **no stub changes** — every failure path uses a pre-existing seam.

| File | Detail |
|---|---|
| `OrganizationPageViewModelTests` | `LoadAsync_QueryThrows_LogsError` → `…_AndSurfacesGenericMessage`; seeds `"boom: branch b-77 / role SalonManager"`, asserts the constant + `DoesNotContain("b-77" / "SalonManager")`. |
| `SpecialistPageViewModelTests` | `LoadAsync_Failure_WithoutLoggerFactory_…NeverThrows` → `…_AndSurfacesGenericMessage`; `Constructor_QueryServiceThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage` (+ `DoesNotContain("s-42" / "Jordan Lee")`). |
| `SpecialistProfileViewModelTests` | `Constructor_ProfileQueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage` (+ `DoesNotContain("Jordan Lee")`); `LoadAsync_Failure_WithoutLogger_…NeverThrows` → `…_AndSurfacesGenericMessage`. |
| `SpecialistScheduleViewModelTests` | `+ using …Localization;`. `LoadCommand_QueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage` (+ `DoesNotContain("specialist-1")`). **New:** `SetWeeklyAvailabilityCommand_BackendThrows_SetsGenericErrorMessage_NoLeak` — via the pre-existing `StubSpecialistScheduleCommandService.Fail` seam with an `InvalidOperationException` (non-permission), asserts `IsPermissionDenied == false`, `ErrorMessage == Common_ActionFailedMessage`, `DoesNotContain("specialist-1" / "status 500")` — this covers site 5 (`TryMutateAsync`) directly. |
| `SpecialistAvailabilityViewModelTests` | `+ using …Localization;`. `LoadCommand_QueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage` (+ `DoesNotContain("specialist-1")`). |
| `ServicePageViewModelTests` | `+ using …Localization;`. `Constructor_QueryServiceThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage`; `LoadAsync_QueryServiceThrows_LogsError` → `…_AndSurfacesGenericMessage`. Both seed `"boom: price 45.00 / cost 12.00 / commission 15%"`, assert the constant + `DoesNotContain("45.00" / "commission")`. Comment updated. |
| `ServiceProfileViewModelTests` | `Constructor_ProfileQueryThrows_…SetsErrorMessage` → `…SetsGenericErrorMessage` (+ `DoesNotContain("45.00")`); `LoadAsync_Failure_WithoutLogger_…NeverThrows` → `…_AndSurfacesGenericMessage`. |

**Subset run:** Organization + Specialists + Services → **148 / 148 PASS**.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `1260d4e` + this change) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,716 | **2,715 / 2,715 PASS** ✅ |
| — Domain | 456 | 456 |
| — **Presentation** | +1 → 772 | **772** ✅ |
| — Application | 791 | 791 |
| — Infrastructure | 609 | 609 |
| — Shell | 80 | 80 |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |
| Sub-wave-3 subset | — | **148 / 148 PASS** ✅ |

Suite progression: 2,714 (`1260d4e`) → **2,715** (+1, P2 sub-wave 3). (One under the ~2,716 estimate — the renamed tests replaced rather than added; only the `TryMutateAsync` test is net-new.)

---

## F. COMMIT RECOMMENDATION

| Item | State |
|---|---|
| Scope | ✅ 14 files (7 prod + 7 test), all within the STRICT SCOPE allowance |
| Base HEAD | `1260d4e` — unchanged; nothing staged |
| Build | ✅ 0 / 0 |
| Tests | ✅ 2,715 / 2,715; Architecture 7 / 7; subset 148 / 148 |
| Sites | ✅ 8 / 8 — `catch` variable dropped, surface = `Common_ActionFailedMessage`, `State = Error` / the two `UnauthorizedOperationException` branches / `[CallerMemberName]` / `TryMutateAsync` success path / stale-response guards / log calls byte-unchanged |
| Security | ✅ RBAC role/permission strings, staff PII, specialist ids, availability data, service pricing/cost/commission, and backend bodies all structurally unreachable from every surface; sentinel-enforced |
| Behaviour | ✅ unchanged — error-state recovery, permission-denied path (`IsPermissionDenied` + Warning log), mutation success/failure semantics, stale-response guards all preserved |
| Localization | ✅ no `.resx` change; `+ using …Localization;` in 1 prod + 3 test files only |
| DI / services / contracts / stubs | ✅ none |
| Line endings | some edited files became mixed LF/CRLF in the working copy (`git diff` warns "LF will be replaced by CRLF"); `core.autocrlf=true` normalises to LF in the committed blob (repo-consistent) — cosmetic only, build/tests unaffected (same as phases 8.78 / 8.86 / 8.104) |
| Proposed commit subject | `fix(desktop): sanitize organization, specialists and services error surfacing` |
| Proposed staged files | the 14 above — **no `git add -A` / `git add .`** |

### Separate from Missing-Guard work

This changes the *message string* in *pre-existing* catches. No new guard, no behaviour. The Missing-Guard Sweep (`794648e` … `0260bc3`) is complete and untouched.

---

## STOP

Phase 8.112 implementation complete. Base HEAD `1260d4e` unchanged (no commit). Build 0/0, **2,715 / 2,715** tests pass, Architecture 7/7, sub-wave-3 subset 148/148.
**8 Category-A sites sanitized** — `OrganizationPageViewModel.LoadAsync`, `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync`, `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync`, `ServiceProfileViewModel.LoadAsync`. `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = Error`, the two `UnauthorizedOperationException` typed branches, the `[CallerMemberName]` parameter, `TryMutateAsync`'s success path, both stale-response guards, and every operation-name-only log call are byte-unchanged. 1 prod (`SpecialistAvailabilityViewModel`) + 3 test files gained `+ using …Localization;`; `OrganizationPageViewModel` keeps its FQ form. No `.resx` / DI / service / contract / stub change. **RBAC strings, staff PII, specialist ids, availability data, and service pricing / cost / commission no longer reach any UI surface.** +1 net test (`TryMutateAsync` mutation-boundary no-leak).

**Awaiting Phase 8.113 — Sub-Wave 3 Commit Scope Review.**
