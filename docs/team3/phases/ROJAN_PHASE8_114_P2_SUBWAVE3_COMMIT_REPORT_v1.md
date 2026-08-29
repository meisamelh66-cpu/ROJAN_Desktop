# ROJAN AI — TEAM 3 — PHASE 8.114 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 3 — COMMIT REPORT v1

**Type:** Commit execution. One commit created. No push / merge / rebase / amend. No source/test change beyond Phase 8.112.
**Authorization:** APPROVED (Phase 8.114 block).

---

## A. NEW HEAD

```
b509054  fix(desktop): sanitize organization specialists and services error surfacing
1260d4e  fix(desktop): sanitize customer, HR and membership error surfacing   (parent)
76d3f61  fix(desktop): sanitize reporting, AI center and accounting error surfacing
```

- **Branch:** `feature/team3-desktop-completion`
- **New HEAD:** `b509054` — child of `1260d4e`
- **Not pushed.** Tracked tree after commit: **clean**; untracked = `ROJAN_*.md` reports only.

### Staged & committed — 14 files, exactly the approved set

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

Staging: `git reset` then explicit per-path `git add` (no `git add .` / `-A`). No report `.md` staged.

### Commit message subject (as committed — the Phase 8.114 exact string)

```
fix(desktop): sanitize organization specialists and services error surfacing
```

Body: the 7-VM list, the "no exception variable / State = Error / both `UnauthorizedOperationException` branches / `[CallerMemberName]` / `TryMutateAsync` success clearing / stale-response guards unchanged" note, the `using`-additions note, and "No .resx, DI, service or contract change. +1 test." Trailers: `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>` + `Claude-Session: …session_018qKcQuzpsf2kvARD6nVjVX`.

---

## B. 8 / 8 SANITIZED SITES

| VM · method | Surface | Ref |
|---|---|---|
| `OrganizationPageViewModel.LoadAsync` | `ErrorMessage` | `Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage` (fully-qualified) |
| `SpecialistPageViewModel.LoadAsync` (inside stale-response `if`) | `ErrorMessage` | `Strings.Common_ActionFailedMessage` |
| `SpecialistProfileViewModel.LoadAsync` | `ErrorMessage` | `Strings.…` |
| `SpecialistScheduleViewModel.LoadAsync` | `ErrorMessage` | `Strings.…` |
| `SpecialistScheduleViewModel.TryMutateAsync` (shared 8-caller mutation boundary) | `ErrorMessage` | `Strings.…` |
| `SpecialistAvailabilityViewModel.LoadAsync` | `ErrorMessage` | `Strings.…` (`+ using`) |
| `ServicePageViewModel.LoadAsync` (inside stale-response `if`) | `ErrorMessage` | `Strings.…` |
| `ServiceProfileViewModel.LoadAsync` | `ErrorMessage` | `Strings.…` |

Each: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`.

**Byte-unchanged:** every `State = DashboardState.Error` (sites 1–4, 6–8), **both `catch (UnauthorizedOperationException)` typed branches** in `SpecialistScheduleViewModel` (still ahead of the general catch — `IsPermissionDenied` + `LogPermissionDenied` at **Warning**), the `[CallerMemberName] string operationName = ""` parameter, `TryMutateAsync`'s success path (`IsPermissionDenied = false; ErrorMessage = null; return true;`), both stale-response guards, every `#pragma warning disable/restore CA1031` comment, every `LogOperationFailed` / static-form `LogOperationFailed(Logger, …)` / `LogLoadFailed` / `LogPermissionDenied` call, every `[LoggerMessage]` signature.

`using` additions: `SpecialistAvailabilityViewModel.cs` (prod) + `SpecialistScheduleViewModelTests.cs` / `SpecialistAvailabilityViewModelTests.cs` / `ServicePageViewModelTests.cs` (test). `OrganizationPageViewModel.cs` keeps its fully-qualified form.

---

## C. ORGANIZATION DATA PROTECTION

`OrganizationPageViewModel.LoadAsync` loads organizations + branches + RBAC role/permission data. Before `b509054`, a backend validation error could put a **branch id / branch name / company name / role name / permission string** on screen via `ErrorMessage = exception.Message`.

After: `ErrorMessage = Rojan.Desktop.Presentation.Localization.Strings.Common_ActionFailedMessage` — the exception is not bound. Sentinel test `OrganizationPageViewModelTests.LoadAsync_QueryThrows_LogsError_AndSurfacesGenericMessage` seeds `"boom: branch b-77 / role SalonManager"` and asserts `DoesNotContain("b-77" / "SalonManager", sut.ErrorMessage)`.

---

## D. SPECIALIST DATA PROTECTION

| Surface | Now blocked |
|---|---|
| `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync` | staff / specialist identifiers, specialist PII (name / email) — tests seed `"…for specialist s-42 / Jordan Lee"` / `"…for Jordan Lee / jordan.lee@rojan.example"`, assert `DoesNotContain("s-42" / "Jordan Lee")` |
| `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync` | availability windows, specialist id — tests seed `"…for specialist-1 / 09:00-17:00"` / `"backend body: specialist-1 / 09:00-13:00 / status 500"`, assert `DoesNotContain("specialist-1" / "status 500")` |

**`UnauthorizedOperationException` behaviour unchanged** — `SpecialistScheduleViewModelTests`: `LoadCommand_UnauthorizedOperationException_SetsIsPermissionDenied_NotGenericError`, `…_LogsAsWarningNotError`, `SetWeeklyAvailabilityCommand_PermissionDenied_SetsIsPermissionDenied_NeverReloads` — all unchanged and green. The permission path still sets `IsPermissionDenied`, logs at **Warning**, never touches `ErrorMessage`, and preserves the input buffer.

---

## E. SERVICE PRICING / CONFIGURATION PROTECTION

`ServiceProfileViewModel.LoadAsync` loads the full service config (price, cost, duration, category, commission). `ServicePageViewModel.LoadAsync` loads the catalog. Before `b509054`, a validation error could put a **price / cost / commission %** on screen.

After: the generic constant. Sentinel tests (`ServicePageViewModelTests` ×2, `ServiceProfileViewModelTests` ×1) seed `"boom: price 45.00 / cost 12.00 / commission 15%"` and assert `DoesNotContain("45.00" / "commission", sut.ErrorMessage)`.

---

## F. TEST DELTA

| | `1260d4e` | `b509054` | Δ |
|---|---|---|---|
| Domain | 456 | 456 | — |
| **Presentation** | 771 | **772** | **+1** |
| Application | 791 | 791 | — |
| Infrastructure | 609 | 609 | — |
| Shell | 80 | 80 | — |
| Architecture | 7 | 7 | — |
| **Total** | **2,714** | **2,715** | **+1** |

**+1 net** — `SpecialistScheduleViewModelTests.SetWeeklyAvailabilityCommand_BackendThrows_SetsGenericErrorMessage_NoLeak`, via the pre-existing `StubSpecialistScheduleCommandService.Fail` seam with an `InvalidOperationException` (non-permission) — directly covers site 5 (`TryMutateAsync`). Plus ~12 in-place assertion flips (`Assert.Equal("boom" / seeded, surface)` → `Assert.Equal(Strings.Common_ActionFailedMessage, surface)`) and 9 test renames. **No new test files, no stub changes.**

---

## G. POST-COMMIT VALIDATION

| Gate | Expected | Actual (at `b509054`) |
|---|---|---|
| `dotnet build -c Debug` | 0 / 0 | **0 Warning(s) / 0 Error(s)** ✅ |
| Full test suite | ~2,716 / ~2,716 | **2,715 / 2,715 PASS** ✅ (Domain 456, Application 791, Presentation 772, Architecture 7, Shell 80, Infrastructure 609) |
| Architecture tests | 7 / 7 | **7 / 7 PASS** ✅ |

Suite progression: 2,713 (`76d3f61`) → 2,714 (`1260d4e`, sub-wave 2) → **2,715** (`b509054`, sub-wave 3, +1).

---

## H. CHECKPOINT

`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` updated: §A HEAD `b509054` + banner + audit-phase list (+8.113) + commit chain; §B commit table (+`b509054` row); §E `Debug verified at b509054` / `2,715/2,715` / Presentation 772 / progression line; §F new Phase 8.112 detail bullet; §G P2 track (sub-wave 3 ✅ — 8 sites; sub-waves 4–6 + `CustomerProfileViewModel` remain); §H items 1/2/5 + STOP-line update-history entry. No code changed by the checkpoint update.

---

## I. REMAINING P2 SUB-WAVES

`ROJAN_PHASE8_102_*` §F, priority-ordered. Each: one audit → one commit-scope-review → one commit; drop the `catch` variable, swap to `Strings.Common_ActionFailedMessage`, keep `State = Error` + `LogOperationFailed`; **no localization / DI / service / logging change**.

| # | Sub-wave | VMs (sites) |
|---|---|---|
| ~~1~~ | ~~Reporting + AI Center + Accounting/POS~~ | **✅ `76d3f61`** — 11 sites |
| ~~2~~ | ~~Customers + HR + Membership~~ | **✅ `1260d4e`** — 6 of 7 |
| ~~3~~ | ~~Organization + Specialists + Services~~ | **✅ `b509054`** — 8 sites |
| 4 | Automation tabs | `WorkflowsTabViewModel` (5), `ScheduledJobsTabViewModel` (3), `BusinessRulesTabViewModel` (2), `ApprovalsTabViewModel` (2), `AutomationDashboardTabViewModel` (1) = **13** — **keep the `when (exception is not OperationCanceledException)` filter on every site** |
| 5 | Booking + Calendar + Inventory | `BookingPageViewModel` (5), `CalendarPageViewModel` (3), `InventoryPageViewModel` (2), `InventoryProfileViewModel` (1) = **11** |
| 6 | Dashboard + Analytics + Salon + QR + Support + Settings + **CustomerProfileViewModel** | `DashboardPageViewModel` (1), `AnalyticsPageViewModel` (1), `SalonPageViewModel` (2), `QrCodesPageViewModel` (2), `SupportPageViewModel` (2), `SettingsPageViewModel` (2 — Category D), **`CustomerProfileViewModel.LoadAsync` (1 — carried over from sub-wave 2)** = **10–12** |

**Recommended next: Phase 8.115 — sub-wave 4 scope audit** (Automation tabs — note the filtered-catch shape). Also still available: Phase 8.99.1 (Settings XAML visibility tweak, LOW risk).

---

## STOP

Phase 8.114 complete. HEAD `b509054` (`fix(desktop): sanitize organization specialists and services error surfacing`), not pushed. Build 0/0, **2,715/2,715** tests pass, Architecture 7/7.
**8 / 8 sub-wave-3 sites sanitized** — `OrganizationPageViewModel.LoadAsync`, `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync`, `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`, `SpecialistAvailabilityViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync`, `ServiceProfileViewModel.LoadAsync`. `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = Error`, both `UnauthorizedOperationException` typed branches, the `[CallerMemberName]` operation name, `TryMutateAsync`'s success-path `ErrorMessage` clearing, and both stale-response guards are byte-unchanged; 1 prod + 3 test files gained `+ using …Localization;`; `OrganizationPageViewModel` keeps its FQ form; no `.resx` / DI / service / contract / stub change. **RBAC role/permission strings, staff PII, specialist identifiers, availability windows, and service pricing / cost / commission % no longer reach any UI surface.** +1 net test. Sub-waves 4–6 of the "sanitize load-error surfacing" P2 remain (~34 sites, incl. the carried-over `CustomerProfileViewModel.LoadAsync`).

**Awaiting next authorization.**
