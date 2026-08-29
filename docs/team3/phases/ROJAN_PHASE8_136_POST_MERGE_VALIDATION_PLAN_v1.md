# ROJAN AI — TEAM 3 — PHASE 8.136 — POST-MERGE VALIDATION PLAN v1

**Type:** Planning document. **STRICT MODE — no source/test change, no fix, no commit/push/merge/rebase.** Documentation only.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `58a2c88` (frozen, unchanged)
**Reference:** `ROJAN_PHASE8_135_DESKTOP_MERGE_READINESS_REVIEW_v1.md`

**Bottom line:** The real merge target is **`origin/main` = `d518218`** (the `v1.0.0` tag commit), not the stale local `main` (`b915e04`). `origin/main` **is a strict ancestor of `58a2c88`** → a fast-forward merge is still possible (`origin/main..58a2c88` = **45 commits**, 1 pre-existing merge). The branch `58a2c88` is **local-only and must be pushed** before a PR. This document defines the merge prerequisites, the exact pre-merge checkpoint, the post-merge validation matrix, the tag decision (no tag this phase), and the rollback plan.

---

## A. MERGE EXECUTION PLAN

| Item | Value |
|---|---|
| Source branch | `feature/team3-desktop-completion` @ **`58a2c88`** (local only — **not yet pushed to origin**) |
| Target branch | **`origin/main` @ `d518218`** — this is the `v1.0.0` tag commit and the true GitHub main. **Local `main` (`b915e04`) is stale** — 4 commits behind `origin/main` (all 4 already contained in `58a2c88`). |
| Strategy | **Fast-forward** — `origin/main` (`d518218`) is a strict ancestor of `58a2c88` (`git merge-base --is-ancestor origin/main 58a2c88` → true). No three-way merge, no conflicts. |
| What the merge brings | `origin/main..58a2c88` = **45 commits**: the 30-commit Team 3 hardening track (`801cc65..HEAD`, all `fix(desktop):`, strictly linear) + 15 pre-`801cc65` commits `origin/main` is behind on (booking-conflict-authority refactor, `feat(desktop)` specialist mgmt / service catalog / booking intelligence / eligibility filtering, HTTP-API observability, a booking-authority test, and **1 pre-existing merge commit `b48740d`** from baseline reconciliation — not Team 3 work). |
| Remote | `origin` = `https://github.com/meisamelh66-cpu/ROJAN_Desktop.git` |

### Merge prerequisites (in order)

1. **Sync local `main` to origin:** `git fetch origin` → confirm `origin/main` still `d518218` (record if it moved — see below). Local `main` is stale; do not merge into it.
2. **Push the source branch:** `git push origin feature/team3-desktop-completion` (currently local-only). Nothing to review on GitHub until this happens.
3. **Confirm FF still valid:** `git merge-base --is-ancestor origin/main 58a2c88` must be `true`. If `origin/main` advanced since this plan, re-run Phase 8.135's §C conflict assessment against the new tip.
4. **Green CI on the pushed branch** (if branch protection runs checks): the branch must build + pass 2,715/2,715 in CI, matching the local Debug **and** Release verification (Phase 8.133).
5. **Working tree clean, HEAD unmoved** (Task B checkpoint).

### Required approvals

| Approval | Who | Basis |
|---|---|---|
| Merge scope + quality | Team 3 lead / designated reviewer | Phase 8.135 (`READY TO MERGE`, LOW conflict risk, clean quality bar) + Phase 8.132/8.133 (completion + release-prep audits) |
| "Brings 45 commits incl. v1.0.0 + baseline, not just the 30 hardening commits" acknowledged in the PR description | PR author | Reviewer note 1, Phase 8.135 §F |
| Repo-hygiene call: FF vs merge-commit; whether to also fast-forward the stale local `main` | Team 3 lead | mechanical preference only |

### Rollback point

**`d518218`** (`origin/main` immediately before the merge). Record it in the PR and in Task E. A FF merge moves the `main` ref only — reverting is a ref move back to `d518218` (see §E).

---

## B. PRE-MERGE CHECKPOINT

**Verified now (Phase 8.136), to be re-verified immediately before the merge:**

| Check | Expected | Verified at Phase 8.136 |
|---|---|---|
| HEAD unchanged | `58a2c88069ac90da319e3e900478935a518649ef` | ✅ `58a2c88…` |
| Branch | `feature/team3-desktop-completion` | ✅ |
| Working tree (tracked) | clean — 0 modified / 0 deleted / 0 staged | ✅ 0 dirty |
| `origin/main` (rollback SHA) | `d5182184aa33cb3a0e204cca39e0ff71a833b606` (= `v1.0.0` tag) | ✅ |
| FF eligibility | `origin/main` strict ancestor of HEAD | ✅ true (`origin/main..HEAD` = 45) |
| Merge commits in `origin/main..HEAD` | exactly 1 (`b48740d`, pre-existing) | ✅ |
| Test baseline preserved | 2,715 / 2,715, 0 skipped (Debug) | ✅ (Phase 8.132) |
| Release baseline preserved | 2,715 / 2,715, 0 skipped + build 0/0 (Release) | ✅ (Phase 8.133) |
| Architecture | 7 / 7 (both configs) | ✅ |
| Security | 58 / 58 Category-A sanitized; logs operation-name-only | ✅ (Phase 8.132) |
| No stray artifacts in the range | all changed paths `.cs`/`.xaml`/`.resx`; no `bin`/`obj`/`.user` tracked; 0 skipped tests | ✅ (Phase 8.135 §B) |

**Pre-merge SHA checkpoint (record verbatim in the PR):**
```
source (to merge):   58a2c88069ac90da319e3e900478935a518649ef   feature/team3-desktop-completion
target (main now):   d5182184aa33cb3a0e204cca39e0ff71a833b606   origin/main  (== tag v1.0.0)   ← ROLLBACK POINT
merge-base:          d5182184aa33cb3a0e204cca39e0ff71a833b606   (target is a strict ancestor → fast-forward)
commits brought:     45   (30 Team 3 hardening + 15 pre-existing baseline, incl. 1 merge b48740d)
```

---

## C. POST-MERGE VALIDATION PLAN

Run in order. Any failure → stop, do not push further, invoke §E rollback.

### 1. Git verification

| Check | Pass criterion |
|---|---|
| `main` HEAD | `git rev-parse main` == `58a2c88069ac90da319e3e900478935a518649ef` |
| FF confirmed (no merge commit created, if FF chosen) | `git log --merges d518218..main` == only `b48740d` (the pre-existing one); no new merge commit |
| History integrity | `git rev-list --count d518218..main` == 45; `git log --oneline` shows the 30 `fix(desktop):` commits contiguous and unaltered (SHAs match the branch) |
| No accidental commits | `git diff 58a2c88 main` == empty; no commit authored during the merge window other than the (optional) merge commit |
| Tag unaffected | `git rev-list -n1 v1.0.0` still `d518218` |
| Remote in sync | `git rev-parse origin/main` == `main` after `git push` |

### 2. Build verification

| Check | Pass criterion |
|---|---|
| Debug build | `dotnet build -c Debug` → **0 warnings, 0 errors** |
| Release build | `dotnet build -c Release` → **0 warnings, 0 errors** (deterministic) |

### 3. Test verification

| Check | Pass criterion |
|---|---|
| Full suite (Debug) | `dotnet test -c Debug` → **2,715 / 2,715**, 0 failed, **0 skipped** |
| Full suite (Release) | `dotnet test -c Release` → **2,715 / 2,715**, 0 failed, **0 skipped** |
| Architecture tests | `Rojan.Desktop.ArchitectureTests` → **7 / 7** (both configs) |
| Per-project parity | Domain 456 / Application 791 / Presentation 772 / Infrastructure 609 / Shell 80 |

### 4. Smoke tests

Automated where a test already covers the path (✅ = existing test asserts it — re-confirmed by the full suite above); manual only for the interactive UI flows (▶ = run against a `-c Release` build, ideally after `build/publish.ps1`). None require backend write access — use the read-only probes / stub-backed flows already in use.

| Journey | Step | Coverage | How |
|---|---|---|---|
| **Customer** | Login (OTP screen renders, validation) | ✅ `Security` (3 test files) + `Shell/CurrentSessionServiceTests` | full suite |
| | Login → real OTP round-trip | ▶ manual (Blocker B2 — needs a real phone) | out of this plan's scope |
| | Search (query → results, out-of-order discard) | ✅ `Search` + `CustomerPageViewModelTests` | full suite |
| | Booking wizard (customer → service → specialist → availability → slot → confirm) | ✅ `Bookings` + `BookingWorkflow` + `BookingWizardViewModelTests` | full suite |
| | Booking wizard renders + advances end-to-end | ▶ manual | launch, walk the wizard |
| **Manager** | Dashboard (KPIs load; financial KPIs gated behind `AccountingView`; Error state on failure shows generic message) | ✅ `DashboardPageViewModelTests` (incl. the sub-wave-6 no-leak assertion) | full suite |
| | Services (catalog browse, authoring) | ✅ `Services` (2 test files) | full suite |
| | Calendar (daily/weekly availability; Error state generic) | ✅ `CalendarPageViewModelTests` (sub-wave-5 no-leak) | full suite |
| | Dashboard / Services / Calendar render with live data | ▶ manual | launch, open each page |
| **Automation** | Workflow execution (run now, publish, rollback; filtered-catch cancellation; Error surface generic) | ✅ `Automation` (6 test files, incl. sub-wave-4 no-leak assertions) | full suite |
| | Workflows tab renders, run-now round-trips | ▶ manual | launch, open Automation → Workflows |
| **Settings** | Theme (apply → restart-required message; **failure message now visible** — Phase 8.129) | ✅ `SettingsPageViewModelTests` (34) | full suite |
| | Language (current language display, pack "coming soon") | ✅ `SettingsPageViewModelTests` + `Shell/Localization*` (5 test files) | full suite |
| | API Environment (switch Dev↔Prod → restart-required message; failure message visible) | ✅ `SettingsPageViewModelTests` + `Infrastructure/Api/ApiEnvironmentServiceTests` | full suite |
| | Settings page renders; all 3 status messages appear on both success **and** failure (the Phase 8.129 fix) | ▶ manual | launch → Settings → trigger a theme change and (with backend unreachable) an API-env change; confirm the warning text shows in both cases |

**Smoke-test gate:** all ✅ rows are covered by the full suite (step 3). The ▶ manual rows are a short launch-and-click pass on a Release build — recommended before any real release, not a merge blocker (the merge changes no behaviour vs. what the suite already proves).

---

## D. RELEASE TAG STRATEGY

**Decision this phase: NO TAG CREATED** (STRICT MODE, and per the phase instruction).

| Option | Assessment |
|---|---|
| **Keep `v1.0.0`** (`d518218`) as-is | ✅ Correct for now. The tag marks the released v1.0.0 build; the hardening work is post-release maintenance. After the merge, `main` = `58a2c88` = `v1.0.0-45` — ahead of the tag, which is accurate and fine. |
| **Create a patch tag `v1.0.1`** on the merge commit | ⏸ Defer. A patch tag implies a *shipped* patch build. That needs the §D-of-Phase-8.134 gates first (signing cert, live test, clean-VM, a real `release.yml` run). Cutting `v1.0.1` now would tag an unbuilt/unsigned artifact. **Recommended only when a real v1.0.1 installer is produced and validated** — a Release Engineering call. |
| **Create a release-candidate tag `v1.1.0-rc.1`** | ⏸ Defer, same reasoning. Only if the team wants a formal RC cycle for the accumulated hardening + baseline work — again after a real build + smoke pass. |

**Recommendation:** merge first (no tag); once Release Engineering produces + validates a real Release build/installer at the merged commit, tag it then — `v1.0.1` if scoped as "hardening patch, same feature set", `v1.1.0` if the 6 pre-`801cc65` `feat(desktop)` commits (specialist mgmt, service catalog, booking intelligence, etc.) are considered new user-facing features in this line. That's a versioning-doc call (`docs/standards/versioning.md`), not this phase's.

---

## E. ROLLBACK PLAN

**Rollback target: `d518218`** (`origin/main` immediately before the merge; == `v1.0.0` tag). No data is created by a FF merge — only the `main` ref moves — so rollback is a ref move.

### If the merge fails or post-merge validation (§C) fails

| Situation | Action |
|---|---|
| **Merge not yet pushed** (local FF only) | `git checkout main && git reset --hard d518218` — done. Nothing left origin. |
| **Merge pushed, caught before others pulled** | `git push origin d518218:main --force-with-lease` (restores `origin/main` to the rollback SHA). Announce in the team channel. |
| **Merge pushed, others may have branched from it** | Do **not** force-push. Instead `git revert -m 1 <merge-sha>` (if a merge commit) **or** `git revert d518218..58a2c88` (range revert of all 45 commits, in reverse) → one or more revert commits → PR → merge. History stays append-only. |
| **A specific hardening commit is the problem** (unlikely — all green) | `git revert <that-sha>` in isolation; the sanitization commits are independent and each self-contained. |

### Branch pointer restoration

- `feature/team3-desktop-completion` @ `58a2c88` is **immutable during this process** — never rebased, never amended. It remains the canonical source of the 30 hardening commits and can be re-merged after any rollback + fix.
- If the branch itself must be restored: `git reflog` on the branch → `git reset --hard 58a2c88`. The SHA is recorded in every Phase 8.12x–8.13x report.

### Report preservation

- The **~55 `ROJAN_PHASE8_*` audit reports + `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`** are currently **untracked** (240 untracked `.md` files total, incl. other engagements'). A merge / rollback of tracked code **does not touch them**.
- To preserve the Team 3 engagement record explicitly: `git add ROJAN_PHASE8_* ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` on a dedicated `docs/team3-desktop-hardening-audit-trail` branch (or a `docs/` subfolder) and commit — **a separate, deliberate decision**, independent of the code merge, recommended regardless of merge outcome so the audit trail survives.
- Do **not** blanket `git add .` — 185 of the 240 untracked `.md` files belong to other teams/engagements.

---

## F. OWNERSHIP AFTER MERGE

| Area | Owner | Scope |
|---|---|---|
| **Desktop maintenance** — ViewModels, state handling, commands, error surfaces, navigation, diagnostic logging, the Settings XAML | **Team 3** | Keep the sanitization + guard patterns intact on any new ViewModel (audit → guard → sanitize → operation-name-only log). Fold the P3 list in opportunistically. The one code-touching release item — first-launch API-environment default (Blocker B7) — is a Team 3 follow-up **if** Product chooses "flip for Release". |
| **Backend contracts** — Inventory / HR / Accounting APIs | **Team 1** | Deliver the contracts. On delivery, ping Team 3 for the connection follow-up (`Fake*Repository` → `Backend*Repository`, promote the legacy `IPermissionGate`). |
| **Installer / pipeline** — code-signing, `release.yml` execution, publish + checksum, clean-VM install | **Release Engineering** | All hooks + runbooks delivered. First real `release.yml` run after a signing cert + the endpoint decision + a fresh publish at the merged commit. |
| **Production decisions** — first-launch API-environment default; POS payment-retry UX; whether/when to tag `v1.0.1` vs `v1.1.0` | **Product** (+ **DevOps** for endpoint, + **Backend** for POS idempotency confirmation) | Each has a documented options list (Phase 8.134 §F, this doc §D). |
| **Live authentication validation** — real OTP → login → real dashboard | **Release Engineering / QA** | Needs a real phone number; manual runbook in `docs/RojanReception_v1.0_Production_Checklist.md` §8. |

**Post-merge, the Team 3 Desktop hardening engagement is CLOSED.** Team 3's continuing role is steady-state Desktop maintenance + the small backend-connection follow-ups when Team 1's contracts land.

---

## G. NEXT AUTHORIZATION

**Recommended Phase 8.137 = MERGE EXECUTION** — the first phase that actually touches git state (push branch, fetch, fast-forward `main`, push `main`), gated on:
- explicit "APPROVED" authorization (this is a push/merge — outside every prior phase's STRICT MODE),
- the §A prerequisites satisfied (branch pushed, `origin/main` unchanged at `d518218` or re-assessed),
- the §B pre-merge checkpoint re-verified at execution time.

Then Phase 8.138 = post-merge validation (run §C), and a separate phase for the audit-trail-preservation commit (§E) if the team wants it.

**Alternative:** if the merge is done via a GitHub PR by a human (not this session), Phase 8.137 becomes "push the branch + prepare the PR body" only, and validation (§C) runs after the human merges.

---

## STOP

Phase 8.136 post-merge validation plan complete. **Nothing modified.** HEAD `58a2c88`, tracked tree clean, **Team 3 Desktop track FROZEN**.

**Key correction from Phase 8.135:** the real merge target is **`origin/main` = `d518218`** (the `v1.0.0` tag commit), not the stale local `main` (`b915e04`, 4 commits behind). `origin/main` **is a strict ancestor of `58a2c88`** → fast-forward still possible; `origin/main..58a2c88` = **45 commits** (30 Team 3 hardening + 15 pre-existing baseline, incl. 1 merge `b48740d`). The branch `58a2c88` is **local-only and must be pushed** before a PR. **Rollback SHA: `d518218`.**

Plan delivered: merge prerequisites + approvals (§A), the pre-merge SHA checkpoint (§B), a 4-part post-merge validation matrix — git / build / test / smoke (§C), tag decision = **no tag now, defer `v1.0.1`/`v1.1.0` to a real validated build** (§D), a ref-move rollback plan that preserves the immutable branch and the untracked audit reports (§E), and the post-merge ownership map (§F).

**Recommendation: Phase 8.137 = MERGE EXECUTION (requires explicit push/merge authorization — outside prior STRICT MODE), then Phase 8.138 = post-merge validation per §C.**

**Awaiting Phase 8.137 authorization.**
