# ROJAN Reception v1.0 — RELEASE FINAL TRACKING BOARD v1

**Owner of this board:** Release Engineering (from Phase 8.152 handoff) · **Created by:** Team 3, Phase 8.153 · **Date:** 2026-08-29
**Code baseline:** `origin/main` = `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` · version `1.0.0` (frozen)
**Legend:** ✅ DONE · 🟡 READY / IN PROGRESS · ⏳ PENDING (owner action) · ⛔ BLOCKED (on another gate)

---

## A — TRACKING BOARD

### Team 3 — CLOSED

| Release Gate | Owner | Status | Last Action | Next Action | Completion Criteria |
|---|---|---|---|---|---|
| Desktop code complete | Team 3 | ✅ DONE | 58/58 error surfaces sanitized; merged to `main` `77414de` (P8.141) | — | Code frozen, no P0, on `main` — **met** |
| Build (Debug + Release) | Team 3 | ✅ DONE | 0 warn / 0 err both configs (P8.145) | — | `dotnet build -c Release` 0/0 — **met** |
| Tests + Architecture | Team 3 | ✅ DONE | 2,715/2,715 (0 skipped) both configs; 7/7 (P8.145) | — | Full suite green in Release — **met** |
| Installer generation | Team 3 | ✅ DONE | `ROJAN Reception Setup.exe` built, Inno 6.7.3 (P8.144) | — | Installer produced from `main` — **met** |
| Installer validation (build machine) | Team 3 | ✅ DONE | install / launch-to-login / uninstall all pass (P8.144) | Superseded by B3 (clean VM) | Build-machine cycle clean — **met** |
| Artifact package | Team 3 | ✅ DONE | Installer + ZIP + publish output; version single-sourced (P8.148) | — | Reproducible via `publish-installer.ps1` — **met** |
| Signing enablement (hooks/scripts/CI wiring) | Team 3 | ✅ DONE | `publish-installer.ps1` + `.iss` + `release.yml` signing path verified inert (P8.149/8.151) | — | Signing is a parameter, not a redesign — **met** |
| Audit trail | Team 3 | ✅ DONE | 144 phase reports committed `da0c36b` (P8.142) | Optional: push + FF `main` | Engagement fully documented — **met** |

### External — OPEN

| Release Gate | Owner | Status | Last Action | Next Action | Completion Criteria |
|---|---|---|---|---|---|
| **B1 — Code signing** | Release Engineering / budget owner | ⏳ PENDING | Phase 8.151: no certificate, no `signtool`, no repo-secret access → not executable | Procure an Authenticode certificate (EV recommended). Provide as CI secret + (for local) `.pfx` on an SDK machine. | **DONE =** `signtool verify /pa "ROJAN Reception Setup.exe"` succeeds; Windows shows publisher "ROJAN" (or legal entity); no "Unknown Publisher" on first run. |
| **B4 — Release pipeline** | DevOps | ⏳ PENDING (⛔ on B1, B8) | Phase 8.151: `release.yml` locally dry-verified; not triggered (no `gh`, tag conflict, unsigned, unverifiable) | Set `CODE_SIGNING_CERT_BASE64` + `CODE_SIGNING_CERT_PASSWORD`; ensure `da0c36b` (or equiv) on `main`; `git tag -a v1.0.x <sha> && git push origin v1.0.x`. | **DONE =** `release.yml` run is green; a **signed** `ROJAN Reception Setup.exe` + `.sha256` + ZIP are attached to a GitHub Release named `v1.0.x`; version check passed; artifact version = `1.0.0`. |
| **B2 — Live login validation** | QA | ⏳ PENDING | Phase 8.151: no network route to `api.rojanai.ir` (timeout); non-interactive env | From a backend-connected network, install the signed build, sign in with a real phone + OTP. | **DONE =** startup → API connect → OTP request → OTP verify → session created → main shell + real dashboard data all PASS; recorded PASS/FAIL with screenshots. |
| **B3 — Clean VM installation** | QA / Release Engineering | ⏳ PENDING | Phase 8.144: validated on build machine only (not clean) | Install the signed `ROJAN Reception Setup.exe` on fresh Windows 10 **and** Windows 11 VMs (no .NET runtime/SDK). | **DONE =** on both OS versions: installer launches, install succeeds (no runtime prompt), Start-Menu shortcut works, app starts to login screen, uninstall removes all traces. PASS on both. |
| **B7 — API environment default** | Product + DevOps | ⏳ PENDING (decision) | Phase 8.151: proposal recorded (Production default); Team 3 cannot approve | Choose: (1) flip Release default to `https://api.rojanai.ir` (~5 LOC + 1 test — Team 3 follow-up if authorized); (2) force choice in onboarding; (3) ship as-is + document. | **DONE =** decision recorded (endpoint + approver + reason); if option 1/2, the change is merged to `main` with a passing test; a fresh install reaches the intended API with no manual step (or the onboarding prompt appears). |
| **B8 — Product sign-off + tag** | Product | ⏳ PENDING (⛔ on B-DOCS, B7) | Phase 8.151: sign-off checklist assembled (Desktop ✅ / Installer ✅ / Pipeline 🟡 / QA 🟡 / Signing ⬜ / Docs ⬜ / Product ⬜) | Ratify v1.0 scope (Inventory/HR/Accounting/POS = "coming soon"); approve the checklist; authorize the tag. | **DONE =** written Product approval on record; `v1.0.x` tag authorized in writing; scope note published. |
| **B-DOCS — Release notes / CHANGELOG** | Team 3 (draft) → Product (approve) | ⏳ PENDING | Phase 8.151: `[1.0.0]` draft text prepared; not applied (no-commit phase) | Authorized editing phase: convert `CHANGELOG.md [Unreleased]` → `## [1.0.0] - <date>` + Security/Fixed/Changed blocks; refresh `RELEASE_NOTES.md`; consolidate Known Issues. Product approves. | **DONE =** `CHANGELOG.md` has a dated `## [1.0.0]` section, **no `[Unreleased]` stub with release content**; release notes reflect the hardening + current Known Issues; Product approved. |
| **B5 — Backend contracts** (Inventory/HR/Accounting/POS) | Team 1 | ⏳ PENDING · **not v1.0-blocking** | Desktop layers + `Fake*Repository` + tests in place | Publish API contracts; Desktop connects per contract (post-v1.0 follow-up). | **DONE =** contracts published; Desktop repos swapped from fakes to real; integration tests green. (Not required for v1.0 "coming soon" scope.) |
| **B6 — POS payment idempotency** | Product + Backend | ⏳ PENDING · **not v1.0-blocking** | Identified: `PosCheckoutViewModel.ChargeAsync` re-chargeable after failed payment | Confirm backend `/charge` idempotency key handling; define POS retry UX. | **DONE =** backend idempotency confirmed; Desktop retry UX specified + implemented; double-charge test green. (POS out of v1.0 scope.) |
| **Production deployment** | Release Engineering | ⛔ BLOCKED (on B4) | — | After B4: publish the GitHub Release publicly; update `ROJAN_Web` `release-registry.ts` with the signed installer URL + checksum (manual cross-repo). | **DONE =** GitHub Release public; `ROJAN_Web` release registry points at the signed `v1.0.x` installer with a matching SHA-256; download link verified live. |

---

## B — EXIT CONDITIONS (what "DONE" means)

| Gate | DONE when… |
|---|---|
| **B1 Signing** | A valid Authenticode signature is verified on `ROJAN Reception Setup.exe` (`signtool verify /pa` passes; timestamped; chains to a Microsoft-trusted CA) and the payload exe + uninstaller are signed too. First run shows a named publisher, not "Unknown Publisher". |
| **B4 Pipeline** | `release.yml` completes green on a `v1.0.x` tag and a **signed** installer + `.sha256` + ZIP are attached to a GitHub Release; the tag-vs-`Directory.Build.props` version check passed. |
| **B2 QA — Live login** | Full chain PASS against the production backend: startup → API connect → OTP send → OTP verify → session created → main shell renders with real dashboard data. |
| **B3 QA — Clean VM** | Signed installer installs, launches to the login screen, creates a working shortcut, and uninstalls cleanly on fresh Windows 10 **and** Windows 11 with no .NET runtime preinstalled. Both PASS. |
| **B7 API environment** | Final endpoint decision recorded with approver + reason; a fresh production install reaches the intended API with no manual Settings change (or a deliberate onboarding prompt is shown). |
| **B8 Product** | Written Product approval of the final release checklist + written authorization to cut the `v1.0.x` tag + published v1.0 scope note. |
| **B-DOCS** | `CHANGELOG.md` carries a dated `## [1.0.0]` section (no stale `[Unreleased]` holding release content); `RELEASE_NOTES.md` current; Known Issues consolidated; Product approved. |
| **Production deployment** | GitHub Release public + `ROJAN_Web` release registry updated to the signed installer + checksum; download verified. |
| **B5 / B6** (post-v1.0) | Contracts published & Desktop connected (B5); backend idempotency confirmed & retry UX shipped (B6). Not required for the v1.0 cut. |

### Release is SHIPPABLE when

**B1 ∧ B2 ∧ B3 ∧ B4 ∧ B7 ∧ B8 ∧ B-DOCS are all DONE**, then Production deployment. B5 and B6 are explicitly out of the v1.0 gate.

### Critical path

`B1 (certificate)` → `B-DOCS + B7 (decisions/docs)` → `B8 (Product approval + tag auth)` → `B4 (pipeline run → signed artifact)` → `B2 + B3 (QA on the signed build, parallel)` → `Production deployment`.

---

## C — FINAL DASHBOARD

```
ROJAN Reception v1.0 — RELEASE STATUS
════════════════════════════════════════════════════

COMPLETED  ✅   TEAM 3 — DESKTOP
  ├─ Desktop code (hardening, security, logging)     ✅
  ├─ 58/58 error surfaces sanitized                  ✅
  ├─ Merged to main (77414de)                        ✅
  ├─ Build 0/0 (Debug + Release)                     ✅
  ├─ Tests 2,715/2,715 · Architecture 7/7            ✅
  ├─ Installer built + validated (build machine)     ✅
  ├─ Artifact package (reproducible, v1.0.0)         ✅
  ├─ Signing toolchain wired                         ✅
  └─ Audit trail (144 reports, da0c36b)              ✅

PENDING    ⏳   EXTERNAL RELEASE GATES
  ├─ B1  Code signing certificate      Release Eng   ⏳
  ├─ B4  release.yml first run         DevOps        ⏳ (⛔ B1,B8)
  ├─ B2  Live OTP login validation     QA            ⏳
  ├─ B3  Clean VM install (Win10/11)   QA/Release    ⏳
  ├─ B7  API environment default       Product+DevOps ⏳
  ├─ B8  Product sign-off + tag        Product       ⏳ (⛔ B-DOCS,B7)
  └─ BD  Release notes / CHANGELOG     Team3→Product ⏳

OUT OF v1.0 SCOPE
  ├─ B5  Inventory/HR/Accounting/POS contracts   Team 1
  └─ B6  POS payment idempotency                 Product+Backend

────────────────────────────────────────────────────
DESKTOP:            READY ✅
PRODUCTION RELEASE: WAITING FOR EXTERNAL OWNERS ⏳
P0 DEFECTS:         0
════════════════════════════════════════════════════
```

---

**This board is a static snapshot.** Team 3 does not maintain it after Phase 8.153 — ownership sits with Release Engineering. Update the Status / Last Action / Next Action columns as gates close.
