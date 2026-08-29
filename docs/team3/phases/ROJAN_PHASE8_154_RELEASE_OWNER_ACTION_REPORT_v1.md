# ROJAN_PHASE8_154 — RELEASE OWNER ACTION REQUEST PACKAGE — REPORT v1

**Phase:** 8.154 · **Type:** External owner execution package · **Date:** 2026-08-29
**Mode:** STRICT — documentation only · no source change · no commit · no branch change
**State:** `origin/main` = `77414de` · local HEAD = `da0c36b` (docs; `src`/`tests` == main) · tracked tree clean

---

## TASK A — ACTION PACKAGE

Created: **`ROJAN_PHASE8_154_RELEASE_OWNER_ACTION_PACKAGE_v1.md`** — four self-contained owner sections:

| Owner | Actions packaged |
|---|---|
| **Release Engineering** | Provide certificate (OV `.pfx` vs EV/HSM guidance) · sign installer (`publish-installer.ps1 -CertificatePath …`) · verify signature (`signtool verify /pa`) · (later) production deployment + `ROJAN_Web` registry sync |
| **QA** | Execute live login test (real OTP → dashboard against `api.rojanai.ir`) · execute clean-VM install (fresh Win10 + Win11, no .NET runtime) |
| **DevOps** | Configure secrets (`CODE_SIGNING_CERT_BASE64` / `CODE_SIGNING_CERT_PASSWORD`, or EV/HSM workflow swap) · run `release.yml` (tag push) · verify artifact (signature + hash + version + assets) |
| **Product** | Approve API environment (B7) · approve scope (B8) · authorize release tag (B8) · approve release notes (B-DOCS) |

Each action lists the exact command/step, the upstream dependency, and the specific hazard (notably: the existing `v1.0.0` tag points at old commit `d518218`, so the release version against `77414de`/`da0c36b` must be reconciled — move the tag or bump `<VersionPrefix>`).

---

## TASK B — ACCEPTANCE

Defined for every owner action, as a consolidated matrix with 5 columns per row:

**Required input · Execution action · Evidence required · Completion state** (+ gate ID).

Highlights:

| Gate | Completion state (acceptance) |
|---|---|
| B1 | `signtool verify /pa` = success, timestamped, publisher named; exe + installer + uninstaller all signed |
| B2 | Full chain PASS against production with a real OTP; dashboard shows real data |
| B3 | 6 checks PASS on **both** Windows 10 and Windows 11 (install, no runtime prompt, shortcut, launch, uninstall clean) |
| B4 | Green `release.yml` on `v1.0.x`; signed installer + `.sha256` + ZIP on a GitHub Release; version verified |
| B7 | Written decision (option + endpoint + approver + reason); if code-affecting, merged with a green test |
| B8 | Written scope note + written checklist approval + tag authorization naming SHA + tag string |
| B-DOCS | Dated `## [1.0.0]` in `CHANGELOG.md` (no stale `[Unreleased]`); notes current; Product approved |
| Deployment | Public Release URL + `ROJAN_Web` registry commit + verified download hash match |

---

## TASK C — FINAL HANDOFF SUMMARY

This report.

### Where things stand

**Team 3 — Desktop: COMPLETE ✅.** Code, installer, artifact, tests (2,715/2,715 both configs), architecture (7/7), build-machine validation, signing toolchain, and 144 phase reports of audit trail — all on `main` at `77414de`, audit trail on the branch at `da0c36b`. Zero P0 defects.

**Production release: WAITING FOR EXTERNAL OWNERS ⏳.** Seven distribution-blocking gates, now packaged as concrete, per-owner action requests with acceptance criteria:

- **Release Engineering** — B1 (certificate + signing + verification), then production deployment
- **QA** — B2 (live login), B3 (clean VM, shared)
- **DevOps** — B4 (secrets + `release.yml` + artifact verification)
- **Product** — B7 (API environment), B8 (scope + tag), B-DOCS gate (approve notes)
- **Team 1** — B5 (backend contracts, post-v1.0, non-blocking)
- **Product + Backend** — B6 (POS idempotency, post-v1.0, non-blocking)

**Critical path:** B1 → (B-DOCS + B7) → B8 → B4 → (B2 ∥ B3) → Production deployment.

### Team 3's remaining optional contributions (future phase only)

Apply the release-notes/CHANGELOG update (B-DOCS); implement the API-env default change if Product picks option 1 (B7, ~5 LOC + 1 test); push `da0c36b` and fast-forward `main`; relocate reports 8.142–8.154 into `docs/team3/phases/`. Team 3 performs **no** signing, QA, CI, deployment, or product-decision action.

### Verification

| Check | Result |
|---|---|
| `.cs` / `.xaml` / project / build-script changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits created | ❌ none |
| Branch changed | ❌ none |
| Tracked working tree | 0 dirty |
| Files created this phase | `ROJAN_PHASE8_154_RELEASE_OWNER_ACTION_PACKAGE_v1.md`, `ROJAN_PHASE8_154_RELEASE_OWNER_ACTION_REPORT_v1.md` (untracked, repo root) |

**Documentation only. Confirmed.**

---

**STOP.** Awaiting PHASE 8.155 authorization.
