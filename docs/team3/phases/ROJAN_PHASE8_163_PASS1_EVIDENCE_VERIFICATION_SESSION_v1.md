# ROJAN Reception v1.0 — PASS 1 EVIDENCE VERIFICATION SESSION v1

| | |
|---|---|
| **Scope** | Verification workflow for PASS 1 evidence — B1, B7, B-DOCS |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Session created** | 2026-08-29 · Phase 8.163 · by Team 3 |
| **Verifier / session owner** | Release Engineering |
| **Companions** | 8.160 PASS 1 execution · 8.161 blocker resolution · 8.162 evidence collection board + status model |

> Defines *how* each evidence bundle is verified once an owner submits it. Team 3 runs no verification here — no evidence has been received, and Team 3 has no certificate / product authority (Phase 8.151). All gate results are **PENDING**; the session stays open until owners supply evidence.

---

## B1 — CODE SIGNING

**Owner:** Release Engineering · **Board status:** WAITING · **Result:** ☐ PASS ☐ FAIL — *current: PENDING (no evidence)*

| Step | Action | Verifier checks | Evidence in | Outcome |
|---|---|---|---|---|
| **1. Receive certificate metadata** | Owner submits B1-E1 | Type (OV/EV) stated · CA is on Microsoft's trusted list · Subject = ROJAN legal entity · thumbprint + serial + validity dates present · **no private key / password included** · delivery channel named | `____________` | ☐ OK ☐ reject |
| **2. Verify installer signature** | Verifier runs `signtool verify /pa /v "ROJAN Reception Setup.exe"` on the submitted signed installer | "Successfully verified" · chains to a trusted root · **"The signature is timestamped"** present · payload `Rojan.Desktop.Shell.exe` signed · embedded uninstaller signed · first-run screenshot shows a **named publisher** (not "Unknown Publisher") | `____________` | ☐ OK ☐ reject |
| **3. Verify SHA-256 artifact integrity** | Verifier runs `Get-FileHash -Algorithm SHA256` on the signed installer; compares to B1-E2 and to the `.sha256` sidecar | Hash matches the submitted value and the sidecar · file size recorded · build source = `77414de` / `da0c36b` / the `v1.0.x` tag · differs from the unsigned baseline `69cb1f29…097615` (expected — signing changes bytes) | `____________` | ☐ OK ☐ reject |
| | **Result rule** | **PASS** iff steps 1–3 all OK. **FAIL** if any rejects → return to Release Engineering. | | |

---

## B7 — API ENVIRONMENT

**Owner:** Product + DevOps · **Board status:** WAITING · **Result:** ☐ PASS ☐ FAIL — *current: PENDING (no evidence)*

| Step | Action | Verifier checks | Evidence in | Outcome |
|---|---|---|---|---|
| **1. Receive approved production URL** | Owner submits B7-E1 | Endpoint stated (expected `https://api.rojanai.ir`) · rollout option named (1 code flip / 2 onboarding / 3 doc-only) | `____________` | ☐ OK ☐ reject |
| **2. Verify owner approval** | Verifier inspects B7-E2 | Dated written record · names an approver with the authority to decide (Product) + DevOps concurrence · reason stated · link resolves | `____________` | ☐ OK ☐ reject |
| **3. Verify release configuration decision** | Verifier confirms the decision is actually effective for the Release build | Option 1: the `ApiEnvironmentService` default change is merged to `main` with a passing test; a fresh Release install reaches `https://api.rojanai.ir` with no manual step. Option 2: the onboarding prompt is implemented + shown on first run. Option 3: the limitation is documented in the release notes / Known Issues. | `____________` | ☐ OK ☐ reject |
| | **Result rule** | **PASS** iff steps 1–3 all OK. **FAIL** if any rejects → return to Product + DevOps. | | |

---

## B-DOCS — RELEASE NOTES

**Owner:** Product (approve) · Team 3 (draft/apply) · **Board status:** WAITING · **Result:** ☐ PASS ☐ FAIL — *current: PENDING (no evidence)*

| Step | Action | Verifier checks | Evidence in | Outcome |
|---|---|---|---|---|
| **1. Receive final v1.0.0 notes** | Owner submits BD-E1 | `CHANGELOG.md` diff shows `## [1.0.0] - <date>` with Security / Fixed / Changed blocks (content matches Phase 8.151 TASK F intent) · the `## [Unreleased]` stub no longer holds release content · `RELEASE_NOTES.md` refreshed with the hardening section + updated readiness table · Known Issues consolidated | `____________` | ☐ OK ☐ reject |
| **2. Verify Product approval** | Verifier inspects BD-E2 | Approval comment/sign-off from the Product owner · dated · references the specific docs commit/diff · Known Issues list explicitly accepted for a public release | `____________` | ☐ OK ☐ reject |
| **3. Verify published text** | Verifier compares BD-E3 to BD-E1 | The GitHub Release notes (`--notes-file` content or accepted auto-generated notes) and the `ROJAN_Web` release-registry entry match the approved `CHANGELOG.md [1.0.0]` · no stale "Unreleased" wording anywhere in the published text | `____________` | ☐ OK ☐ reject |
| | **Result rule** | **PASS** iff steps 1–3 all OK. **FAIL** if any rejects → return to Team 3 / Product. | | |

---

## TASK B — BOARD UPDATE

| Gate | Evidence received | Verification | Board status | Result |
|---|---|---|---|---|
| **B1** | 0 / 3 | not started | **WAITING** | PENDING |
| **B7** | 0 / 3 | not started | **WAITING** | PENDING |
| **B-DOCS** | 0 / 3 | not started | **WAITING** | PENDING |

No transitions this phase — nothing submitted. Board status model (Phase 8.162): `WAITING → EVIDENCE RECEIVED → VERIFICATION RUNNING → {PASS | FAIL}`.

---

## SESSION STATE (Phase 8.163)

```
Session:   PASS 1 Evidence Verification — OPEN, awaiting submissions
Main:      77414de   (1.0.0, frozen)
Desktop:   READY ✅

B1     WAITING   0/3 evidence   verification PENDING   Release Engineering
B7     WAITING   0/3 evidence   verification PENDING   Product + DevOps
B-DOCS WAITING   0/3 evidence   verification PENDING   Product / Team 3

Evidence received: 0 / 9 slots
PASS 1 gates PASS: 0 / 4   (B8 BLOCKED)
Production:         NO-GO
```
