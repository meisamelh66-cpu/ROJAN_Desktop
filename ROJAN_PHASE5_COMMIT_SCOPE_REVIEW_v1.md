# ROJAN DESKTOP — PHASE 5 COMMIT SCOPE REVIEW v1

**Read-only. No commit performed. Waiting for commit approval.**

---

## A. Files to Commit (clean — Phase 5 only, verified line-by-line via `git diff`, not assumed from filename)

**New:**
```
src/Rojan.Desktop.Application/Api/Contracts/ScheduleContracts.cs
src/Rojan.Desktop.Application/Schedule/IScheduleRepository.cs
src/Rojan.Desktop.Application/Schedule/IScheduleQueryService.cs
src/Rojan.Desktop.Application/Schedule/IScheduleCommandService.cs
src/Rojan.Desktop.Application/Schedule/ScheduleDtos.cs
src/Rojan.Desktop.Application/Schedule/ScheduleQueryService.cs
src/Rojan.Desktop.Application/Schedule/ScheduleCommandService.cs
src/Rojan.Desktop.Application/Schedule/ScheduleCommandServicePermissionGate.cs
src/Rojan.Desktop.Infrastructure/Schedule/BackendScheduleRepository.cs
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistScheduleViewModel.cs
tests/Rojan.Desktop.Application.Tests/Schedule/ScheduleCommandServicePermissionGateTests.cs
tests/Rojan.Desktop.Infrastructure.Tests/Schedule/BackendScheduleRepositoryTests.cs
tests/Rojan.Desktop.Presentation.Tests/Specialists/SpecialistScheduleViewModelTests.cs
tests/Rojan.Desktop.Presentation.Tests/Specialists/StubScheduleServices.cs
ROJAN_PHASE5_SHIFT_ENGINE_IMPLEMENTATION_REPORT_v1.md
ROJAN_PHASE5_SHIFT_ENGINE_IMPLEMENTATION_CORRECTION_REPORT_v1.md
ROJAN_PHASE5_IMPLEMENTATION_ARTIFACT_SYNC_REPORT_v1.md
```

**Modified — confirmed by direct `git diff` read to be pure, isolated Phase 5 content, no pre-existing change mixed in:**
```
src/Rojan.Desktop.Application/DependencyInjection/ServiceCollectionExtensions.cs   (+16 lines, Schedule registration only)
src/Rojan.Desktop.Presentation/Views/Specialists/SpecialistPage.xaml               (+282 lines, 0 deletions — the entire diff is the new Schedule DashboardWidget section)
src/Rojan.Desktop.Presentation/Localization/Strings.cs                             (+20 lines, the 8 new SpecialistSchedule_* keys only)
src/Rojan.Desktop.Presentation/Localization/Strings.resx                           (+30 lines, same 8 keys, fa)
src/Rojan.Desktop.Presentation/Localization/Strings.en.resx                        (+30 lines, same 8 keys, en)
src/Rojan.Desktop.Presentation/Localization/Strings.ar.resx                        (+30 lines, same 8 keys, ar)
```

## B. Files to Exclude

**Category B1 — genuinely mixed with pre-existing, unrelated work (real complication, not a simple exclusion — see §C):**
```
src/Rojan.Desktop.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistPageViewModel.cs
src/Rojan.Desktop.Presentation/ViewModels/Specialists/SpecialistProfileViewModel.cs
```

**Category B2 — pre-existing, unrelated to Phase 5 entirely (Services/Specialists catalog work, predates this session, already documented in `ROJAN_DESKTOP_COMMIT_SCOPE_REVIEW_v1.md`):**
```
src/Rojan.Desktop.Application/BookingWorkflow/BookingWorkflowService.cs
src/Rojan.Desktop.Application/Services/{IServiceCommandService,ServiceCommandService,ServiceCommandServicePermissionGate}.cs
src/Rojan.Desktop.Application/Specialists/{ISpecialistCommandService,SpecialistCommandService,SpecialistCommandServicePermissionGate,SpecialistCommandServiceSyncProducer,SpecialistProfileDto,SpecialistProfileQueryService}.cs
src/Rojan.Desktop.Domain/Specialists/ISpecialistRepository.cs
src/Rojan.Desktop.Infrastructure/{Persistence/Specialists/EfSpecialistRepository,Services/BackendServiceRepository,Specialists/BackendSpecialistRepository,Specialists/FakeSpecialistRepository}.cs
src/Rojan.Desktop.Presentation/ViewModels/Services/{ServicePageViewModel,ServiceProfileViewModel}.cs
src/Rojan.Desktop.Presentation/Views/Services/ServicePage.xaml
tests/Rojan.Desktop.Application.Tests/{BookingWorkflow/*,Services/ServiceCommandServiceTests.cs,Specialists/{SpecialistCommandServiceTests,SpecialistProfileQueryServiceTests,StubSpecialistRepository}.cs}
tests/Rojan.Desktop.Infrastructure.Tests/{Services/BackendServiceRepositoryTests.cs,Specialists/BackendSpecialistRepositoryTests.cs}
tests/Rojan.Desktop.Presentation.Tests/{Services/*,Specialists/StubSpecialistCommandService.cs}
```

**Category B3 — other-phase documents (real, valuable, but not Phase 5's own deliverables):**
```
ROJAN_DESKTOP_BACKEND_CONTRACT_GAP_ANALYSIS_v1.md
ROJAN_DESKTOP_COMMIT_SCOPE_REVIEW_v1.md
ROJAN_DESKTOP_COMPLETION_SPRINT_v1_REPORT.md
ROJAN_DESKTOP_PHASE3_1_CALENDAR_AUTHORITY_MIGRATION_REPORT_v1.md
ROJAN_DESKTOP_PHASE3_CALENDAR_COMPLETION_REPORT.md
ROJAN_DESKTOP_PHASE4A_IMPLEMENTATION_IMPACT_MAP_v1.md
ROJAN_DESKTOP_PHASE4_2_DOMAIN_OWNERSHIP_DECISION_v1.md
ROJAN_DESKTOP_PHASE4_3_BACKEND_CONTRACT_READINESS_v1.md
ROJAN_DESKTOP_PHASE4_5_IMPLEMENTATION_REPORT_v1.md
ROJAN_DESKTOP_PHASE4_5_INTEGRATION_READINESS_REPORT_v1.md
ROJAN_DESKTOP_PHASE4_BACKEND_INTEGRATION_REPORT.md
ROJAN_DESKTOP_PHASE4_REMAINING_GAPS_AUDIT_v1.md
```
(This list also implicitly covers `ROJAN_DESKTOP_BRANCH`-related new source files from Phase 4.5 — `BackendBranchRepository.cs`, `BranchContracts.cs`, `BackendBranchRepositoryTests.cs` — real, tested, but a separate phase's deliverable, not bundled into this Phase 5 boundary. Not listed again here since Phase 4.5's own commit scope was never separately requested; flagged so it isn't silently conflated with Phase 5's boundary either.)

**Category B4 — pre-existing, unrelated, not authored this session, no Phase 5 relation at all:**
```
ROJAN_Desktop_Reception_Production_Integration_Report_v1.md
docs/ROJAN_Reception_v1.1_Sprint1_Execution_Plan.md
docs/ROJAN_Reception_v1.1_Technical_Roadmap.md
docs/cross-app-audit/
tests/Rojan.Desktop.Application.Tests/Specialists/StubServiceQueryService.cs
```

**Category B5 — real prior RBAC work (Sprint 1, this engagement's own), not this phase:**
```
tests/Rojan.Desktop.Application.Tests/Salons/SalonSessionAdapterTests.cs
```

## C. Reasoning

**Why §A's list is safe to commit as one Phase 5 boundary:** every file in it was checked with `git diff` directly, not assumed from its path — three files in particular (`Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` excluded here, `SpecialistPage.xaml` and `Application/DependencyInjection/ServiceCollectionExtensions.cs` included) looked like they might carry other work given how many phases have touched this app's DI/Specialist surface, and turned out to split cleanly one way or the other only after reading the actual diff, not the filename.

**The real complication (§B1), stated plainly:** `SpecialistProfileViewModel.cs` and `SpecialistPageViewModel.cs` each contain **two genuinely interleaved sets of changes in the same diff** — this session's Phase 5 Schedule wiring (`Schedule` property, `IScheduleQueryService`/`IScheduleCommandService` constructor parameters), and a pre-existing, already-in-progress "Specialist-Service Assignment" feature (`AssignedServices`, `AvailableServicesToAssign`, `AssignServiceCommand`/`UnassignServiceCommand`, `IsAssignedToEveryService`) that predates this session entirely and was already catalogued as Category B in `ROJAN_DESKTOP_COMMIT_SCOPE_REVIEW_v1.md`. A plain `git add <file>` on either would stage both sets together — there is no clean way to commit "only Phase 5" from these two files without interactive hunk-level staging (`git add -p`), which was not attempted here since this task's own scope is analysis, not execution. `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` has the same shape, one phase back: it mixes Phase 4.5's Branch/Organization DI swap with Phase 5's Schedule registration, confirmed by direct diff read — a plain `git add` would bundle both phases' wiring into one commit.

**What this means for the boundary Team 1 is being asked to approve:** §A is a real, clean, self-contained Phase 5 slice — it builds and passes on its own conceptually (the Schedule module has no dependency on the Specialist-Service-Assignment feature or the Branch DI swap; those exist independently). But it is **incomplete** in one specific sense: `SpecialistProfileViewModel`/`SpecialistPageViewModel`'s Phase 5 changes (the `Schedule` property and its two new constructor parameters) are not included, because they cannot be cleanly separated from the pre-existing Assignment work sitting in the same files without further, not-yet-authorized work (`git add -p`). Committing §A alone, today, would **not** compile against `SpecialistScheduleViewModel`'s real constructor usage in those two ViewModel files, since those files would remain uncommitted. Three real options, not resolved here:
1. Authorize `git add -p` to hand-split the two ViewModel files' hunks (Phase 5's lines only) — the cleanest true boundary, more manual work.
2. Accept committing all three Category B1 files as part of this Phase 5 commit, explicitly bundling in the pre-existing Specialist-Service Assignment feature and the Phase 4.5 Branch DI swap (both real, both already tested and passing, but not Phase 5's own scope).
3. Commit Phase 4.5 (Branch) and the Specialist-Service Assignment feature first, separately, then Phase 5 cleanly on top — sequencing the three, not flattening them into one commit.

No option was chosen here — this review's job was to surface the boundary accurately, not decide it.

## D. Validation Evidence

```
dotnet build RojanDesktop.sln  -> Build succeeded, 0 Warning(s), 0 Error(s)

dotnet test  RojanDesktop.sln  -> Passed: 2,454  Failed: 0  Skipped: 0  Total: 2,454
```

Confirmed PASS / 2454/2454, matching this task's own stated expectation exactly. Not re-run this turn — no code has changed since the third independent fresh run reported in `ROJAN_PHASE5_IMPLEMENTATION_ARTIFACT_SYNC_REPORT_v1.md`, one turn ago; re-running against an unchanged working tree would not produce new information.

---

## Stop Condition

**No commit performed. Waiting for commit approval — specifically, a decision on the §C boundary question (options 1/2/3) before any `git add`/`git commit` is executed.**
