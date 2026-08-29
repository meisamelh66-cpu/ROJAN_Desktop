# ROJAN Reception v1.0 — PASS 1 BLOCKER RESOLUTION TRACKER v1

| | |
|---|---|
| **Scope** | PASS 1 blockers only — B1, B7, B-DOCS (the three actionable-now gates; B8 is downstream) |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Tracker created** | 2026-08-29 · Phase 8.161 · by Team 3 |
| **Tracker owner** | Release Engineering |
| **Companions** | 8.158 status model · 8.159 batch plan · 8.160 PASS 1 execution doc |

> Follow-up tracker for the three PASS 1 blockers that can be worked now. Team 3 cannot resolve any (no certificate, no product authority — Phase 8.151). Owners close open items and move Status per the Phase 8.158 model: `WAITING → IN PROGRESS → {PASS | FAIL | BLOCKED}`.

---

## B1 — CODE SIGNING

| | |
|---|---|
| **Owner** | Release Engineering |
| **Status** | **WAITING** |

| # | Open item | Detail | Done when | ✓ |
|---|---|---|---|---|
| B1.1 | **Certificate acquisition** | Purchase an Authenticode code-signing certificate (EV recommended — immediate SmartScreen trust; OV cheaper, reputation builds). Microsoft-trusted CA (DigiCert / Sectigo / SSL.com / GlobalSign), issued to the ROJAN legal entity. Identity-verified; allow a few business days + business docs. | Certificate issued; subject/issuer/thumbprint/expiry recorded; stored in the org secret manager (OV `.pfx` + password) or CA token/cloud-HSM credentials obtained (EV). | ☐ |
| B1.2 | **Signing execution** | `pwsh build/publish-installer.ps1 -CertificatePath <pfx> -CertificatePassword <pw>` on a Windows-SDK machine, **or** hand the credential to DevOps for CI (B4). Signs payload exe + installer + embedded uninstaller; RFC-3161 timestamped. | A signed `ROJAN Reception Setup.exe` exists (or the credential is confirmed available to CI and dry-verified). | ☐ |
| B1.3 | **Signature verification** | `signtool verify /pa /v "ROJAN Reception Setup.exe"` → "Successfully verified"; timestamp present; trusted chain; payload exe + uninstaller signed; first-run on a non-dev machine shows a named publisher. | All checks pass; signtool output + signed SHA-256 + named-publisher screenshot filed. | ☐ |
| | **Exit → B1 = PASS** | | B1.1 ∧ B1.2 ∧ B1.3 done. | |
| | **Current blocker** | No certificate procured; `signtool.exe` absent from the automation environment. | | |

---

## B7 — API ENVIRONMENT

| | |
|---|---|
| **Owner** | Product + DevOps |
| **Status** | **WAITING** |

| # | Open item | Detail | Done when | ✓ |
|---|---|---|---|---|
| B7.1 | **Production API decision** | Choose: **Option 1** — flip Release-build default from `Development` (`localhost:8080`) to Production (`https://api.rojanai.ir`) — a ~5-line change in `ApiEnvironmentService` + 1 unit test (Team 3 follow-up if a future phase authorizes; no Debug behaviour change). **Option 2** — first-run onboarding prompt. **Option 3** — ship as-is + document. Recommendation: **Option 1**. | One option selected and recorded. | ☐ |
| B7.2 | **Written approval** | Decision record: chosen option · endpoint (`https://api.rojanai.ir`) · approver (name/role) · reason · date. | Record exists and is linked in the tracker. | ☐ |
| B7.3 | **Configuration owner** | Name who applies the decision: DevOps (env var / deployment config), or Team 3 (the code change, on a future phase authorization), or Product (documentation only). Confirm the mechanism (`ROJAN_API_BASE_URL`, code default, or onboarding UX). | Owner + mechanism named; if code-affecting, a follow-up phase authorized and the change merged to `main` with a green test. | ☐ |
| | **Exit → B7 = PASS** | | B7.1 ∧ B7.2 ∧ B7.3 done; a fresh install reaches the intended API with no manual step (or a deliberate onboarding prompt is shown). | |
| | **Current blocker** | Decision requires Product + DevOps authority; Team 3 cannot make or record it. | | |

---

## B-DOCS — RELEASE NOTES

| | |
|---|---|
| **Owner** | Product (approve) · Team 3 (draft/apply in an authorized editing phase) |
| **Status** | **WAITING** |

| # | Open item | Detail | Done when | ✓ |
|---|---|---|---|---|
| BD.1 | **Replace `[Unreleased]`** | `CHANGELOG.md` line 9 `## [Unreleased]` → `## [1.0.0] - <release date>` with Security / Fixed / Changed blocks (draft text: Phase 8.151 TASK F). (Historical `## [0.1.0-alpha] - Unreleased` at line 1279 is a separate old heading — out of scope here.) | `CHANGELOG.md` carries a dated `## [1.0.0]`; no `[Unreleased]` stub holds release content. Requires a commit (future authorized phase). | ☐ |
| BD.2 | **Approve v1.0.0 notes** | Product reviews the `CHANGELOG.md [1.0.0]` wording + the refreshed `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` (add "Reliability & Security Hardening (Team 3)" section; update Production-Readiness table) + consolidated Known Issues (unsigned→signed status, first-launch API default, "coming soon" domains, POS re-charge, window-title inconsistency). | Product approval comment on the docs change; approver + date recorded. | ☐ |
| BD.3 | **Publish final text** | The approved notes are the source for `gh release create … --notes-file` (or the auto-generated notes are accepted as a supplement) and for the `ROJAN_Web` release-registry entry. | Final notes committed to `main` and referenced by the release. | ☐ |
| | **Exit → B-DOCS = PASS** | | BD.1 ∧ BD.2 ∧ BD.3 done. | |
| | **Current blocker** | Applying BD.1 needs a commit; Phases 8.151 and 8.160 forbid commits. Needs a future authorized editing phase, then Product approval. | | |

---

## TASK B — UNBLOCK MAP

```
B1  ─────────────┐
                 │
B7  ─────────────┼──────►  B8  ──────►  B4
                 │       (Product      (release.yml
B-DOCS  ─────────┘        Sign-off +    pipeline run)
                          Tag Auth)
```

| Edge | Meaning |
|---|---|
| **B1 → B8** | Product will not authorize the tag until a signing credential is confirmed available (an unsigned release is not shippable). |
| **B7 → B8** | Product sign-off includes ratifying the API-environment behaviour of the shipped build. |
| **B-DOCS → B8** | Product approves the release notes as part of sign-off; tag authorization presumes accurate notes. |
| **B8 → B4** | DevOps runs `release.yml` only after written Product approval + tag authorization (the workflow triggers on the authorized tag push). |

**Chain:** `(B1 ∧ B7 ∧ B-DOCS) → B8 → B4`. B8 is BLOCKED until all three inputs are PASS. B4 is BLOCKED until B8 is PASS (and B1 for the signing secrets).

---

## PASS 1 BLOCKER STATE (Phase 8.161)

```
B1     Code Signing     WAITING    Release Engineering   3 open items (B1.1–B1.3)
B7     API Environment  WAITING    Product + DevOps      3 open items (B7.1–B7.3)
B-DOCS Release Notes    WAITING    Product / Team 3      3 open items (BD.1–BD.3)
──────────────────────────────────────────────────────────────────────────────
B8     Product Sign-off BLOCKED    Product               (needs B1, B7, B-DOCS)
B4     Pipeline         BLOCKED    DevOps                (needs B1, B8)

PASS 1 gates PASS: 0 / 4        Open items resolved: 0 / 9
```
