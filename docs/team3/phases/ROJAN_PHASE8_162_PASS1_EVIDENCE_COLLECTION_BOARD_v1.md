# ROJAN Reception v1.0 — PASS 1 EVIDENCE COLLECTION BOARD v1

| | |
|---|---|
| **Scope** | PASS 1 evidence intake — B1, B7, B-DOCS |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Board created** | 2026-08-29 · Phase 8.162 · by Team 3 |
| **Board owner** | Release Engineering |
| **Companions** | 8.158 status model · 8.159 batch plan · 8.160 PASS 1 execution · 8.161 blocker resolution tracker |

> One evidence slot per required artifact. Owners drop the artifact (link or inline), then set the gate status per TASK B. Team 3 collects and verifies format only — it produces no evidence (no certificate, no product authority; Phase 8.151). All slots **empty / WAITING** at board creation.

---

## B1 — CODE SIGNING

**Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***
**Owner:** Release Engineering

| # | Required evidence | Format expected | Slot | Received |
|---|---|---|---|---|
| B1-E1 | **Certificate metadata** | Type (OV/EV) · CA · Subject/CN · Thumbprint (SHA-1) · Serial · Valid-from / Valid-to · delivery channel (CI secret / `.pfx` / EV token). **No private key or password.** | `________________________` | ☐ |
| B1-E2 | **Signed installer hash** | `SHA-256` of the signed `ROJAN Reception Setup.exe` + file size + build source commit (`77414de` / `da0c36b` / `v1.0.x`). Compare against the unsigned baseline `69cb1f29…097615` (expected to differ — signature changes the bytes). | `________________________` | ☐ |
| B1-E3 | **Signature verification output** | Full `signtool verify /pa /v "ROJAN Reception Setup.exe"` console output showing "Successfully verified", the signing cert chain, and **"The signature is timestamped"**. Plus confirmation the payload `Rojan.Desktop.Shell.exe` and the embedded uninstaller are signed, and a first-run screenshot on a non-developer machine showing a **named publisher**. | `________________________` | ☐ |
| | **Verify → PASS** when | B1-E1 present · B1-E2 present · B1-E3 shows verified + timestamped + trusted chain + exe/uninstaller signed + named publisher. | | |
| | **Current** | No certificate; `signtool.exe` absent from the automation environment. | | |

---

## B7 — API ENVIRONMENT

**Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***
**Owner:** Product + DevOps

| # | Required evidence | Format expected | Slot | Received |
|---|---|---|---|---|
| B7-E1 | **Approved API URL** | The endpoint the shipped Release build will use by default: `https://api.rojanai.ir` (expected). Plus the chosen rollout option — (1) code default flip / (2) onboarding prompt / (3) doc-only. | `________________________` | ☐ |
| B7-E2 | **Approval record** | A dated written decision: chosen option · endpoint · approver name + role · reason. Link to the ticket / email / signed doc. | `________________________` | ☐ |
| B7-E3 | **Configuration owner** | Who applies it and how: DevOps (`ROJAN_API_BASE_URL` / deployment config), Team 3 (the `ApiEnvironmentService` default change — on a future phase authorization, ~5 LOC + 1 test), or Product (documentation only). If code-affecting: link to the merged change + the passing test. | `________________________` | ☐ |
| | **Verify → PASS** when | B7-E1 + B7-E2 + B7-E3 present; if code-affecting, the change is on `main` with a green test; a fresh install reaches the intended API with no manual step (or a deliberate onboarding prompt appears). | | |
| | **Current** | Decision requires Product + DevOps authority; not made or recorded. | | |

---

## B-DOCS — RELEASE NOTES

**Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***
**Owner:** Product (approve) · Team 3 (draft/apply, authorized editing phase)

| # | Required evidence | Format expected | Slot | Received |
|---|---|---|---|---|
| BD-E1 | **Final release notes** | The `CHANGELOG.md` diff/commit adding a dated `## [1.0.0]` (Security / Fixed / Changed — draft: Phase 8.151 TASK F) **and** the removal of the `## [Unreleased]` stub holding release content; plus the refreshed `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` + consolidated Known Issues. | `________________________` | ☐ |
| BD-E2 | **Product approval** | Approval comment / sign-off on the docs change: approver name + role + date + link. | `________________________` | ☐ |
| BD-E3 | **Published version text** | The final notes as they will appear on the GitHub Release (`--notes-file` content or accepted auto-generated notes) and in the `ROJAN_Web` release-registry entry. | `________________________` | ☐ |
| | **Verify → PASS** when | BD-E1 shows dated `## [1.0.0]` + no stale `[Unreleased]` release content · BD-E2 present · BD-E3 matches BD-E1. | | |
| | **Current** | `CHANGELOG.md:9` still `## [Unreleased]`. Applying BD-E1 needs a commit (forbidden this phase). Needs a future authorized editing phase + Product approval. | | |

---

## TASK B — PASS 1 EVIDENCE STATUS MODEL

| State | Meaning | Trigger |
|---|---|---|
| **WAITING** | No evidence submitted for this gate. Default. | — |
| **EVIDENCE RECEIVED** | All required evidence slots for the gate are filled; not yet checked. | Gate owner submits the last slot |
| **VERIFICATION RUNNING** | Evidence is being validated (format, authenticity, cross-checks — e.g. re-running `signtool verify`, confirming the approver's authority, diffing the CHANGELOG). | Board owner / verifier starts |
| **PASS** | Verification succeeded; the gate's exit criteria are met. | Verifier confirms |
| **FAIL** | Evidence incomplete, malformed, contradictory, or verification failed. Returns to the owner. | Verifier rejects |

### Transitions

`WAITING → EVIDENCE RECEIVED → VERIFICATION RUNNING → {PASS | FAIL}` · `FAIL → WAITING` (owner resupplies) → repeat.
A gate's overall PASS 1 result (Phase 8.158 model) becomes **PASS** only when this board shows **PASS** for it.
**PASS 1 COMPLETE** = B1 ∧ B7 ∧ B-DOCS all **PASS** on this board **and** B8 signed off.

---

## EVIDENCE STATE AT BOARD CREATION (Phase 8.162)

```
Gate      Evidence slots filled   Board status
B1        0 / 3                   WAITING
B7        0 / 3                   WAITING
B-DOCS    0 / 3                   WAITING
─────────────────────────────────────────────
Total evidence received: 0 / 3 gates  (0 / 9 slots)
PASS 1 gates PASS:        0 / 4
Production decision:      NO-GO
```
