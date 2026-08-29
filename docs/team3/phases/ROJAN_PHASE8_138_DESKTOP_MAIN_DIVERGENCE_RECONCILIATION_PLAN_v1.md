# ROJAN AI — TEAM 3 — PHASE 8.138 — DESKTOP MAIN-DIVERGENCE RECONCILIATION PLAN v1

**Type:** Reconciliation plan. **STRICT MODE — no execution, no merge, no rebase, no cherry-pick, no commit/push.** Analysis + recommendation only.
**Branch:** `feature/team3-desktop-completion` @ `58a2c88` (unchanged) · **Reference:** `ROJAN_PHASE8_137_MERGE_EXECUTION_REPORT_v1.md`

**Bottom line:** `origin/main` (`53ae2fb`) is a **short-lived divergent fork** — 3 commits, 2 days, **no downstream branches** — that built a **parallel Service-Catalog + Shift-Engine** on the *pre-refactor* architecture. It is **missing 45 commits** the Team 3 branch has: 15 baseline commits (calendar-authority removal, booking intelligence, HTTP observability incl. the file logger, RBAC alignment, checkout hardening, specialist management, auth UX, and the branch's own newer-architecture Service Catalog + `SpecialistSchedule/` engine) **+ all 30 Team 3 hardening commits.** The branch is the canonical, complete, current line. **Recommended path: Option 3a — `git merge -s ours origin/main` onto the Team 3 branch** (after a short scoped review of the fork), which has **zero regression risk, zero conflict resolution, and re-enables a fast-forward to `main`.**

---

## TASK A — DIVERGENCE INVENTORY

### Commit graph

```
d518218  (merge-base; == tag v1.0.0; "ci: grant contents:write to the release job")
   │
   ├──────────────► origin/main  (53ae2fb)   — 3 commits, meisamelh66, 2026-08-25/26
   │                  5ac87dc  feat(desktop): complete service catalog management
   │                  92052c7  feat(desktop): implement specialist shift engine integration
   │                  53ae2fb  fix(desktop): harden specialist shift engine
   │                  (+ 7 ROJAN_PHASE5_* report files — a separate phase-numbering line)
   │
   └──────────────► feature/team3-desktop-completion  (58a2c88)  — 45 commits, 2026-08-24 → 2026-08-29
                      ├─ 15 BASELINE commits (d518218..801cc65):
                      │    b915e04 refactor: remove client booking conflict authority
                      │    b48740d Merge feature/p0-booking-authority-cleanup  ← the 1 merge in the range
                      │    3757d51 test: guard booking workflow authority boundary
                      │    b843abe feat: add http api observability foundation
                      │    8d993a6 feat: complete specialist management workflows
                      │    98017a9 feat: add specialist service eligibility filtering
                      │    2028e6f feat: implement service catalog authoring          ← branch's Service Catalog
                      │    9c7a285 feat: implement booking intelligence phase 1
                      │    f691dea feat: complete specialist schedule shift engine     ← branch's Shift Engine
                      │    ea03d83 fix: harden shift engine error diagnostics
                      │    da18c18 fix: harden booking and checkout error handling
                      │    53090c1 fix: register specialist schedule services in DI
                      │    c59d7c0 fix: align permissions with backend authority
                      │    7103647 refactor: remove local calendar authority          ← ARCHITECTURE DECISION
                      │    801cc65 fix: improve authentication error handling UX
                      └─ 30 TEAM 3 HARDENING commits (801cc65..58a2c88):
                           94fca6a nav bounding · 13× diagnostic logging (2453a7f…5ba554c)
                           9× Missing-Guard Sweep (794648e…0260bc3)
                           6× P2 error-surface sanitization (76d3f61…17306d9)
                           58a2c88 Settings UX visibility fix
```

- `git merge-base origin/main 58a2c88` = **`d518218`** — branches diverged there.
- `origin/main..58a2c88` = **45** · `58a2c88..origin/main` = **3** · fast-forward **impossible** either direction.
- **None** of the branch's 15 baseline commits are on `origin/main` (`git merge-base --is-ancestor <each> origin/main` → false for all).
- `53ae2fb` is referenced **only** by `origin/main` / `origin/HEAD` — **no feature or PR branch is built on it** (`git branch -r --contains 53ae2fb`).

### Overlapping files — 38 changed on BOTH sides (from `d518218`)

| Group | Files | Nature |
|---|---|---|
| **Presentation ViewModels** | `Services/ServicePageViewModel.cs`, `Services/ServiceProfileViewModel.cs`, `Specialists/SpecialistPageViewModel.cs`, `Specialists/SpecialistProfileViewModel.cs`, `Specialists/SpecialistScheduleViewModel.cs` (**add/add** — did not exist at `d518218`; branch 502 lines, origin/main 526 lines) | competing implementations; branch's then hardened by 2–4 Team 3 commits each |
| **Views** | `Views/Services/ServicePage.xaml`, `Views/Specialists/SpecialistPage.xaml` | competing |
| **Application/Services** | `IServiceCommandService`, `IServiceQueryService`, `ServiceCommandService`, `ServiceQueryService`, `ServiceCommandServicePermissionGate`, `CreateServiceRequest`, `UpdateServiceRequest`, `Api/Contracts/ServiceResponse` | competing service layer |
| **Domain/Services** | `IServiceRepository`, `Service.cs`, `ServiceCategoryOption` | competing |
| **Infrastructure/Services** | `EfServiceRepository`, `BackendServiceRepository`, `FakeServiceRepository` | competing |
| **DI** | `Application/DependencyInjection/ServiceCollectionExtensions.cs`, `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` | both register their own services |
| **Localization** | `Strings.resx` / `.en.resx` / `.ar.resx` / `Strings.cs` | branch adds `Common_ActionFailedMessage` (+ others); origin/main adds Service/Schedule strings |
| **Test stubs** | 6× `StubServiceQueryService` / `StubQueryServices*` variants, `StubServiceRepository`, `StubServiceCommandService`, `ServicePageViewModelTests`, `ServiceProfileViewModelTests`, `SpecialistScheduleViewModelTests` (**add/add**) | competing |

### Duplicate features (built independently on each side)

| Feature | Branch implementation | `origin/main` implementation |
|---|---|---|
| **Service Catalog** | `2028e6f feat: implement service catalog authoring` + hardening | `5ac87dc feat: complete service catalog management` — adds `ServiceCategoryDto`, `ServiceEntityMapper`, dedicated `ServiceCommandServiceTests` / `ServiceQueryServiceTests` |
| **Specialist Shift Engine** | `Application/Specialists/Schedule/` — **12 files**: `ISpecialistScheduleCommandService` / `…QueryService` / `SpecialistScheduleCommandService` / `…QueryService` / `SpecialistScheduleMapper` / `…PermissionGate` + 6 DTOs + `Api/Contracts/SpecialistScheduleContracts.cs`; tests under `Application.Tests/Specialists/Schedule/` + `Infrastructure.Tests/Specialists/Schedule/BackendSpecialistScheduleRepositoryTests` | `Application/Schedule/` — **8 files**: `IScheduleCommandService` / `…QueryService` / `IScheduleRepository` / `ScheduleCommandService` / `…QueryService` / `…PermissionGate` / `ScheduleDtos` + `Api/Contracts/ScheduleContracts.cs`; tests `Application.Tests/Schedule/ScheduleCommandServicePermissionGateTests` + `Infrastructure.Tests/Schedule/BackendScheduleRepositoryTests` |

**Different namespaces, different service names, different DTO shapes.** The `SpecialistScheduleViewModel.cs` on each side is wired to its own layer.

### Unique changes

| Only on `origin/main` (6 code files + 2 tests) | Only on the branch (everything else — ~150 files) |
|---|---|
| `Application/Schedule/*` (8), `Application/Services/ServiceCategoryDto.cs`, `Infrastructure/Persistence/Services/ServiceEntityMapper.cs`, `Infrastructure/Schedule/BackendScheduleRepository.cs`; tests `Schedule/ScheduleCommandServicePermissionGateTests`, `Services/ServiceCommandServiceTests`, `Services/ServiceQueryServiceTests`, `Infrastructure.Tests/Schedule/BackendScheduleRepositoryTests`, `Infrastructure.Tests/Services/BackendServiceRepositoryTests` | calendar-authority removal (`7103647` — deletes `Calendar/CalendarCommandService*` — **origin/main still has these**), booking intelligence phase 1, HTTP observability foundation (**incl. `Infrastructure/Observability/LocalFileLoggerProvider.cs` — origin/main lacks the file logger**), specialist eligibility filtering, RBAC alignment, checkout hardening, specialist management workflows, auth UX, the branch's `SpecialistSchedule/` layer, **all 30 Team 3 hardening commits** (nav bounding, diagnostic logging ×13, Missing-Guard ×9, P2 sanitization ×6, Settings XAML) |

### Conflict zones (dry-run merge — aborted)

**~30 conflicted files**, all in the Service / Specialist / Schedule slice + test stubs + XAML. `SpecialistScheduleViewModel.cs` and `SpecialistScheduleViewModelTests.cs` are **add/add** (each side created them from nothing). Every conflicting ViewModel was touched by 2–4 Team 3 hardening commits.

### Architecture signal

`origin/main` **still has local Calendar command authority** (`Calendar/CalendarCommandServiceTests`, `Calendar/CalendarCommandServicePermissionGateTests`) — which the branch **deliberately removed** in `7103647 refactor(desktop): remove local calendar authority` (checkpoint §D: "Calendar — Backend Dependent — Pure read/API layer since `7103647`, no local authority of any kind by design"). **`origin/main`'s fork predates and contradicts that architecture decision.**

---

## TASK B — OWNERSHIP ANALYSIS

### Conflict classification

| Class | Content | On which side |
|---|---|---|
| **Team 3 hardening** — diagnostic logging, command guards, error-surface sanitization (58/58 Category-A), the Settings XAML fix, their tests, navigation bounding | 30 commits `801cc65..58a2c88` | **branch only** — not on `origin/main` |
| **Branch baseline** — calendar-authority removal, booking intelligence, HTTP observability + file logger, eligibility filtering, RBAC alignment, checkout hardening, specialist management, auth UX, **branch's Service Catalog + `SpecialistSchedule/` engine** | 15 commits `d518218..801cc65` | **branch only** — not on `origin/main` |
| **`origin/main` new work** — a parallel Service Catalog + `Schedule/` shift engine, on the pre-`7103647` architecture, with retained local calendar authority | 3 commits `d518218..53ae2fb` | **`origin/main` only** — a superseded design branch |

### Which implementation is canonical?

**The branch's — near-certain, subject to owner confirmation.**

| Criterion | Branch (`58a2c88`) | `origin/main` (`53ae2fb`) |
|---|---|---|
| Recency | 2026-08-29 | 2026-08-26 |
| Architecture | post-`7103647` (calendar authority removed — a deliberate decision) | pre-`7103647` (retains local calendar authority) |
| Completeness | 45 commits: baseline + Service Catalog + Shift Engine + 30 hardening | 3 commits: Service Catalog + Shift Engine only |
| Missing from the other | — | booking intelligence, HTTP observability + **file logger**, eligibility filtering, RBAC alignment, checkout hardening, specialist mgmt, auth UX, all 30 hardening commits |
| Review pedigree | 30+ phases of audit → scope review → implement → commit-scope-review → commit; 2,715/2,715 Debug **and** Release; Architecture 7/7 | none in this engagement's record |
| Test files | 296 | 289 |
| Downstream refs | canonical Team 3 line | **none** (`53ae2fb` on `origin/main`/`origin/HEAD` only) |

**`origin/main` is a divergent fork built on stale architecture, missing the bulk of the current line, with no downstream dependents.** Its 3 commits' *only* possibly-orphan value: the EF `ServiceEntityMapper`, `ServiceCategoryDto`, and 3 dedicated Service/Schedule test suites — worth a quick review before discarding, not worth adopting the fork.

**⚠️ Owner decision required (Phase 8.139 gate):** the same developer authored both lines. There may be a reason `main` received a rebuilt Service/Schedule (a design mandate, a known defect in the branch's version). Phase 8.139 must open with the owner confirming "the branch's Service Catalog + `SpecialistSchedule/` engine is canonical; `origin/main`'s `5ac87dc`/`92052c7`/`53ae2fb` are superseded."

---

## TASK C — STRATEGY OPTIONS

### Option 1 — Rebase the Team 3 branch onto `origin/main`

`git rebase --onto 53ae2fb d518218 feature/team3-desktop-completion` (replay all 45 commits onto the fork).

| Aspect | Assessment |
|---|---|
| **Risk** | **VERY HIGH** |
| **Conflicts** | 45 commits replayed; the 15 baseline commits (calendar-authority *removal* over `origin/main`'s *retained* authority; branch's `SpecialistSchedule/` over `origin/main`'s `Schedule/`; branch's Service Catalog over `origin/main`'s) fight at nearly every step; then sub-wave 3 fights `origin/main`'s ViewModels. Dozens of resolutions, many on obsolete code. |
| **Regression probability** | **HIGH** — replaying the newer, more complete architecture on top of an older competing one; any missed resolution silently corrupts. Full re-verification mandatory and likely to surface breakage. |
| **Preserves** | linear history; but forces `origin/main`'s stale architecture as the base — wrong direction. |
| **Verdict** | **REJECT.** You do not rebase 45 newer/reviewed commits onto a 3-commit stale fork. |

### Option 2 — Cherry-pick the 30 Team 3 hardening commits onto `origin/main`

New branch from `53ae2fb`; `git cherry-pick 801cc65..58a2c88`.

| Aspect | Assessment |
|---|---|
| **Risk** | **HIGH — silent feature loss** |
| **Conflicts** | ~5–8 commits conflict (SpecialistSchedule / Service sanitization targets `origin/main`'s different ViewModel + a different Schedule layer). The logging/guard/other-domain sanitization commits pick cleanly (`origin/main` didn't touch those files). |
| **Regression probability** | **HIGH** — the result is a `main` **missing 15 baseline commits** of tested, reviewed work (booking intelligence, HTTP observability + **the file logger**, calendar-authority refactor, eligibility filtering, RBAC alignment, checkout hardening, specialist mgmt, auth UX) **and** carrying an architecture the hardening assumes was refactored (`7103647`). It would not build/behave as verified. |
| **Preserves** | only the hardening. Discards ~15 commits of baseline work + the branch's Service/Schedule architecture. |
| **Verdict** | **REJECT** — unless `origin/main` first receives the 15 baseline commits by another route, at which point you may as well merge the branch. |

### Option 3 — Manual merge reconciliation (branch as canonical base)

Merge `origin/main` into the Team 3 branch; the branch's tree wins.

#### 3a — `git merge -s ours origin/main` on `feature/team3-desktop-completion`

One merge commit; **tree stays byte-identical to `58a2c88`**; records that `origin/main`'s fork was evaluated and superseded.

| Aspect | Assessment |
|---|---|
| **Risk** | **LOW** |
| **Conflicts** | **NONE** — `-s ours` takes zero content from `origin/main`. |
| **Regression probability** | **NONE** — resulting tree == `58a2c88`, already green in Debug **and** Release (2,715/2,715, Architecture 7/7). Re-run the suite to re-confirm; expect identical. |
| **Preserves** | 100% of the branch. Makes the branch a descendant of `origin/main` → `feature/team3-desktop-completion` → `main` becomes a **clean fast-forward** again. |
| **Cost** | `origin/main`'s 6 unique files (`Schedule/*`, `ServiceEntityMapper`, `ServiceCategoryDto`, its 3 test suites) are **not carried**. Mitigation: a short scoped review of the 3 fork commits first; cherry-pick any genuinely-valuable orphan (e.g. adapt a test) as a follow-up commit on the branch. |
| **Verdict** | **RECOMMENDED** (see Task D). |

#### 3b — Full 3-way merge, resolve all ~30 conflicts to "ours"

`git merge origin/main`; resolve every conflict to the branch's version; then a deliberate keep/drop decision on `origin/main`'s 6 orphan files.

| Aspect | Assessment |
|---|---|
| **Risk** | **MEDIUM** |
| **Conflicts** | ~30, all resolved "ours" — mechanical but error-prone at volume; plus the orphan-file decisions. |
| **Regression probability** | **LOW–MEDIUM** — hand-resolving 30 conflicts risks a stray "theirs" hunk; the full suite re-verifies but a subtle behaviour change could slip past. |
| **Preserves** | 100% of the branch + optional selective adoption of `origin/main`'s Service/Schedule pieces. |
| **Verdict** | **Fallback only** — choose over 3a *only if* the Task-D pre-review finds substantial salvageable value in `origin/main`'s `Schedule/` layer that's easier to interleave than to port. |

---

## TASK D — RECOMMENDATION

# ✅ Option 3a — `git merge -s ours origin/main` onto the Team 3 branch

**Preceded by a short scoped review of `origin/main`'s 3 fork commits; gated on the owner confirming the branch's Service Catalog + `SpecialistSchedule/` engine is canonical.**

### Why

1. **Zero regression risk** — the reconciled tree is byte-identical to `58a2c88`, which is already verified green in Debug and Release (2,715/2,715, Architecture 7/7, 58/58 error surfaces sanitized).
2. **Zero conflict resolution** — deterministic; no hand-merging of two service layers.
3. **Correct direction** — the branch is the newer, complete, reviewed line with the deliberate calendar-authority architecture; `origin/main`'s fork is 3 commits on stale architecture with no dependents.
4. **Unblocks the merge** — after 3a, the branch descends from `origin/main`, so `feature/team3-desktop-completion` → `main` is a clean fast-forward (the Phase 8.135/8.136 plan resumes, minus the stale-target assumption).
5. **Honest history** — the merge commit message records: *"`origin/main`'s parallel Service Catalog + Shift Engine fork (`5ac87dc` / `92052c7` / `53ae2fb`) was reviewed; the Team 3 branch's newer-architecture implementation — which also carries the `7103647` calendar-authority refactor and 30 hardening commits — is canonical; the fork is superseded. Orphan value assessed in `ROJAN_PHASE8_139_*`."*

### The one cost, mitigated

`origin/main`'s 6 unique files are dropped. **Mitigation (part of Phase 8.139):** a 30-minute review of `5ac87dc` / `92052c7` / `53ae2fb` — read the diffs, list anything (a test case, `ServiceEntityMapper` logic, a validation rule) that the branch's Service/Schedule implementation genuinely lacks, and port it as one follow-up commit on the branch *before* the `-s ours` merge. If nothing is worth porting (likely, given the architecture mismatch), proceed straight to 3a.

### NOT recommended

- **Option 1 (rebase)** — replays the complete line onto a stale fork. Wrong base, very high risk.
- **Option 2 (cherry-pick hardening only)** — ships a `main` missing 15 baseline commits + the file logger + the calendar refactor. Silent feature loss.
- **`main` force-reset to the branch** — mechanically possible (`53ae2fb` has no downstream refs) and would produce the cleanest history, but it **rewrites published `main`** and discards `origin/main`'s 3 commits without a trace. Only acceptable with explicit owner sign-off *and* confirmation there is no open PR against `main`; even then, 3a is safer (keeps the fork commits reachable via the merge parent).

### Proposed phase sequence

| Phase | Action | Mode |
|---|---|---|
| **8.139** | (a) Owner confirms canonical Service/Schedule = branch's. (b) Scoped review of `origin/main`'s 3 commits → orphan-value list → optional single follow-up commit on the branch. **STRICT — review + at most one small additive commit, authorized.** |
| **8.140** | Execute Option 3a: `git merge -s ours origin/main` on the branch → full Debug + Release suite + Architecture re-verify (expect 2,715/2,715 unchanged) → fresh merge-readiness review. **Authorized: this one merge command + verification.** |
| **8.141** | Re-authorized merge `feature/team3-desktop-completion` → `main` (now a clean fast-forward) + post-merge validation per Phase 8.136 §C. **Authorized: push + merge.** |
| **8.142** | (separate, any time) commit the engagement audit trail (`git add ROJAN_PHASE8_* ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` **only**) to a `docs/` path. |

---

## TASK E — STOP

Phase 8.138 Desktop main-divergence reconciliation plan complete. **Nothing executed — no merge, no rebase, no cherry-pick, no commit, no push.** HEAD `58a2c88`, tracked tree clean, `origin/main` untouched at `53ae2fb`.

**Divergence:** `origin/main` (`53ae2fb`) is a 3-commit fork from `d518218` (2026-08-25/26, no downstream refs) building a **parallel Service Catalog + `Schedule/` shift engine on pre-refactor architecture** (retains the local calendar authority the branch deliberately removed in `7103647`). It is **missing 45 commits** the Team 3 branch has — 15 baseline (calendar refactor, booking intelligence, HTTP observability + file logger, RBAC alignment, checkout hardening, specialist mgmt, auth UX, the branch's own newer Service Catalog + `SpecialistSchedule/` engine) + 30 hardening. **~30 merge conflicts, all in the Service/Specialist/Schedule slice; `SpecialistScheduleViewModel[.Tests]` are add/add.**

**Canonical implementation: the Team 3 branch's** (newer, complete, reviewed, correct architecture) — subject to owner confirmation, since the same developer authored both.

**Recommendation: Option 3a — `git merge -s ours origin/main` onto `feature/team3-desktop-completion`** (after a scoped review of the fork's 3 commits for orphan value). Zero regression risk (tree unchanged from the verified `58a2c88`), zero conflict resolution, re-enables a fast-forward to `main`, and records the superseded fork honestly. Options 1 (rebase) and 2 (cherry-pick hardening only) are rejected — both put the stale architecture in charge or silently drop tested work.

**Proposed: Phase 8.139 = owner confirmation + scoped fork review; Phase 8.140 = execute 3a + re-verify; Phase 8.141 = re-authorized `→ main` merge.**

**Awaiting Phase 8.139 authorization.**
