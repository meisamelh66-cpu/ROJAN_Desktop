# ROJAN_PHASE8_152 — RELEASE OWNERSHIP HANDOFF & BLOCKER TRANSFER — REPORT v1

**Phase:** 8.152 · **Type:** External team handoff · **Date:** 2026-08-29
**Mode:** STRICT — documentation only · no source change · no commit · no branch change
**State:** `origin/main` = `77414de` · local HEAD = `da0c36b` (docs; `src`/`tests` == main) · tracked tree clean

---

## PURPOSE

Team 3's Desktop hardening engagement is **complete**. This report formally hands the remaining release work to its external owners. After this phase, **no release blocker is owned by Team 3** — every open item belongs to Release Engineering, QA, DevOps, Product, or Team 1.

---

## TASK A — HANDOFF

### TEAM 3 — CLOSED (no further action)

| Area | Deliverable | Evidence |
|---|---|---|
| **Desktop code** | Error-handling / reliability / security / diagnostic-logging hardening across all 55 ViewModels; 58/58 error surfaces sanitized; 7 confirmed live leaks closed; nav back-stack bounded; Settings UX fix | `main` `77414de` (Phases 8.0–8.131) |
| **Installer** | `ROJAN Reception Setup.exe` — Inno Setup 6.7.3, per-user, AppId `{D804D0AC-…773CF}`, icon embedded, clean uninstall | Phase 8.144 |
| **Artifact** | Installer + `RojanDesktop-v1.0.0-win-x64.zip` + self-contained publish output; version `1.0.0` single-sourced; reproducible via `build/publish-installer.ps1` | Phases 8.144 / 8.148 |
| **Tests** | 2,715 / 2,715 passed, 0 skipped — Debug **and** Release; Architecture 7/7; build 0 warn / 0 err both configs | Phases 8.141 / 8.145 |
| **Validation** | Install / launch-to-login-screen / uninstall on the build machine; structural coverage by the 2,715-test suite | Phase 8.144 |
| **Documentation** | 144 phase reports + checkpoint + README under `docs/team3/` (commit `da0c36b`); signing runbook (`docs/standards/code-signing.md`); release-notes draft text (Phase 8.151 TASK F) | Phase 8.142 + `da0c36b` |
| **Signing enablement** | `publish-installer.ps1` cert params · `.iss` `#ifdef SignInstaller` · `release.yml` secret-driven signing step · RFC-3161 timestamping · proven unsigned fallback | Phases 8.149 / 8.151 |

**No P0 defect exists in the Desktop codebase.** Residual `= exception.Message` (2 Settings sites) is a fixed local developer string, deliberately excluded (Category-D).

### TRANSFER — to external owners

| Owner | Items transferred |
|---|---|
| **Release Engineering** | Code-signing certificate (procure) · Authenticode signing execution · production deployment (GitHub Release + `ROJAN_Web` release-registry sync) · clean-VM install (shared with QA) |
| **QA** | Live backend OTP login → dashboard validation · clean-VM installation test (Win10 + Win11) |
| **DevOps** | `release.yml` first real run · CI signing secrets (`CODE_SIGNING_CERT_BASE64`, `CODE_SIGNING_CERT_PASSWORD`) · API-environment default (shared with Product) |
| **Product** | API-environment default decision (shared with DevOps) · v1.0 scope ratification · final release approval · release-tag authorization · release-notes / CHANGELOG approval |
| **Team 1** | Inventory / HR / Accounting / POS backend contracts · POS payment-idempotency (shared with Product/Backend) — **not v1.0-blocking** |

---

## TASK B — BLOCKER TABLE

| ID | Owner | Current state | Required action | Release impact |
|---|---|---|---|---|
| **B1** | Release Engineering / budget owner | No Authenticode certificate. Hooks + scripts + `.iss` + `release.yml` wiring complete and proven inert. | Procure an Authenticode code-signing certificate (EV recommended per `code-signing.md` — immediate SmartScreen trust; OV cheaper but reputation builds over weeks). Issue in the name of the legal entity behind ROJAN; ~a few business days. | **BLOCKS DISTRIBUTION.** Unsigned installer → SmartScreen "Windows protected your PC / Unknown Publisher" on every first run. |
| **B2** | QA | Login *screen* renders on a fresh install (8.144); endpoint contracts confirmed by prior read-only probes; auth ViewModels covered by 1,381 tests. Real round-trip never executed. | From a network with backend access, sign in with a real phone + OTP, confirm session creation and that the dashboard loads real data. Record PASS/FAIL. | **BLOCKS DISTRIBUTION.** The primary end-user journey is unproven end to end. |
| **B3** | QA / Release Engineering | Install validated only on the build machine (not clean). Self-contained single-file `win-x64` payload ⇒ no runtime prompt *expected*. | Install `ROJAN Reception Setup.exe` on fresh Windows 10 and Windows 11 VMs with no .NET runtime/SDK. Verify launch, shortcut, uninstall. Runbook: `docs/team3/phases/…Production_Checklist.md` §8. Record PASS/FAIL. | **BLOCKS DISTRIBUTION.** Behaviour on a bare host is unverified. |
| **B4** | DevOps | `release.yml` authored, locally dry-verified (8.143–8.148), never run against a tag. CI signing secrets unset. | Set the two secrets; ensure the audit-trail commit (`da0c36b` or equivalent) is on `main`; after B8, `git tag -a v1.0.x <sha> && git push origin v1.0.x`. Confirm: build ✅ / publish ✅ / installer ✅ / checksum ✅ / version `1.0.0` / **signed** / GitHub Release created. | **BLOCKS DISTRIBUTION.** No reproducible, audited release build; no published Release. |
| **B7** | Product + DevOps | `ApiEnvironmentService.SelectedEnvironment` defaults to `Development` → `http://localhost:8080`. Production = `https://api.rojanai.ir`; `ROJAN_API_BASE_URL` overrides. | Decide: (1) flip the Release-build default to Production — **~5-line change + 1 test, a small Team 3 follow-up if authorized in a future phase**; (2) force the choice in first-run onboarding; (3) ship as-is + document. Option (1) recommended. Record endpoint + approver + reason. | **BLOCKS DISTRIBUTION.** A production install would not reach the real API without user action. |
| **B8** | Product | No authorization to cut `v1.0.x`. v1.0 scope of Inventory/HR/Accounting/POS ("coming soon" vs cut) not ratified. | Ratify v1.0 scope; approve the final release checklist (Phase 8.151 TASK G); authorize the release tag. | **BLOCKS DISTRIBUTION.** No approval to release. |
| **B-DOCS** | Team 3 (draft) → Product (approve) | `CHANGELOG.md` still `## [Unreleased]` (stale, last edit 2026-08-21); `RELEASE_NOTES.md` predates the hardening. Draft `[1.0.0]` text prepared (Phase 8.151 TASK F). | In an authorized editing phase: convert `[Unreleased]` → `## [1.0.0] - <date>`, add the Security/Fixed/Changed blocks, refresh release notes, consolidate Known Issues. Product approves. | **BLOCKS DISTRIBUTION (process).** Release must ship with accurate notes. |
| **B5** | Team 1 | Inventory / HR / Accounting / POS have no backend. Desktop runs on `Fake*Repository` with full layers + tests. | Publish API contracts; Desktop connects per contract (small Desktop follow-up). | **Does not block v1.0** — viable as "coming soon" scope. |
| **B6** | Product + Backend | `PosCheckoutViewModel.ChargeAsync` re-chargeable after a failed payment; backend idempotency unverified. | Confirm backend `/charge` idempotency; decide POS retry UX. | **Does not block v1.0** — POS out of v1.0 scope. |

**Distribution-blocking: B1, B2, B3, B4, B7, B8, B-DOCS (7). Non-blocking: B5, B6.**

---

## TASK C — FINAL OWNERSHIP STATE

| Scope | State |
|---|---|
| **Desktop** | **READY ✅** — code frozen on `main` `77414de`; 2,715/2,715 tests (both configs); architecture 7/7; build 0/0; no P0; installer built + validated; artifact reproducible; signing wired; audit trail archived. Nothing outstanding for Team 3. |
| **Production Release** | **WAITING FOR EXTERNAL OWNERS ⏳** — 7 distribution-blocking gates (B1, B2, B3, B4, B7, B8, B-DOCS), each owned by Release Engineering / QA / DevOps / Product. None is a Desktop code defect. |

---

## TASK D — REPORT

This document is the Phase 8.152 report (TASK A and TASK D specify the same filename).

### Handoff statement

Team 3 has completed and verified every Desktop-side deliverable for ROJAN Reception v1.0: the application code, the Windows installer, the artifact package, the full test and architecture baseline, build-machine install validation, the signing toolchain, and 144 phase reports of audit trail. All of it is on `main` at `77414de`, with the audit trail on the branch at `da0c36b`.

The remaining path to a shipped v1.0 — a signing certificate, a live OTP login test, a clean-VM install, one `release.yml` run, the API-environment default decision, finalized release notes, and Product's scope sign-off and tag authorization — is now formally owned by Release Engineering, QA, DevOps, and Product. Each blocker has an ID, an owner, a current state, a required action, and a stated release impact in the table above.

**Desktop: READY ✅. Production Release: WAITING FOR EXTERNAL OWNERS ⏳.**

### Optional Team 3 follow-ups (only if a future phase authorizes)

- Draft-apply the `CHANGELOG.md [1.0.0]` + release-notes update (B-DOCS) — commit required.
- Apply the Release-build API-environment default change (B7 option 1) — ~5 lines + 1 test.
- Push `da0c36b` to `origin/feature/team3-desktop-completion` and fast-forward `origin/main` so the audit trail is on `main` before tagging.
- Relocate phase reports 8.142–8.152 from repo root into `docs/team3/phases/`.

---

## VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / project / build-script changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits created | ❌ none |
| Branch changed | ❌ none |
| Tracked working tree | 0 dirty |
| Files created this phase | `ROJAN_PHASE8_152_RELEASE_OWNERSHIP_HANDOFF_REPORT_v1.md` (untracked, repo root) |

**Documentation only. Confirmed.**

---

**STOP.** Awaiting PHASE 8.153 authorization.
