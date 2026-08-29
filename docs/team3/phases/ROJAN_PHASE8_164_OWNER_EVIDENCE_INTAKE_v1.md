# ROJAN Reception v1.0 — OWNER EVIDENCE INTAKE v1

| | |
|---|---|
| **Scope** | PASS 1 owner evidence intake forms — B1, B7, B-DOCS |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Intake created** | 2026-08-29 · Phase 8.164 · by Team 3 |
| **Intake owner** | Release Engineering |
| **Companions** | 8.162 evidence collection board · 8.163 verification session (workflow + checks) |

> Structured intake forms for owners to fill with real evidence. **No evidence has been received.** Team 3 provides none (no certificate, no product authority — Phase 8.151) and does **not** fabricate placeholder values — every field below is blank pending a genuine owner submission. Once a form is complete it moves to the Phase 8.163 verification workflow.

---

## B1 — CODE SIGNING

**Owner:** Release Engineering · **Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***

| Field | Value | Notes |
|---|---|---|
| Certificate issuer (CA) | `________________________` | Must be on Microsoft's trusted list (DigiCert / Sectigo / SSL.com / GlobalSign) |
| Certificate subject (CN / O) | `________________________` | Must be the ROJAN legal entity |
| Certificate type | ☐ OV ☐ EV | EV recommended (immediate SmartScreen trust) |
| Expiration date | `________________________` | Valid-from / valid-to |
| Thumbprint (SHA-1) + serial | `________________________` | Identity only — **no private key / password** |
| Signed installer SHA-256 | `________________________` | Of the signed `ROJAN Reception Setup.exe`; compare to unsigned baseline `69cb1f29…097615` |
| Signed installer size + source | `________________________` | Bytes + build commit (`77414de` / `da0c36b` / `v1.0.x`) |
| signtool verification output | `________________________` | Full `signtool verify /pa /v "ROJAN Reception Setup.exe"` console text |
| Payload exe + uninstaller signed | ☐ yes ☐ no | `Rojan.Desktop.Shell.exe` + embedded `unins000.exe` |
| Timestamp status | ☐ timestamped (RFC 3161) ☐ not timestamped | "The signature is timestamped" must appear |
| First-run publisher | ☐ named publisher ☐ "Unknown Publisher" | Screenshot on a non-developer machine |
| Submitted by / date | `________________________` | Name / role / date |

---

## B7 — API ENVIRONMENT

**Owner:** Product + DevOps · **Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***

| Field | Value | Notes |
|---|---|---|
| Production API URL | `________________________` | Expected `https://api.rojanai.ir` |
| Decision option selected | ☐ Option 1 — flip Release default (code) ☐ Option 2 — first-run onboarding prompt ☐ Option 3 — ship as-is + document | Option 1 recommended (~5 LOC + 1 test, Team 3 follow-up if authorized) |
| Approval owner | `________________________` | Product owner name + role; DevOps concurrence |
| Approval date | `________________________` | |
| Approval record link | `________________________` | Ticket / email / signed doc |
| Reason | `________________________` | |
| Configuration target | `________________________` | How it's applied: `ROJAN_API_BASE_URL` (deployment) / `ApiEnvironmentService` default (code, future phase) / documentation only |
| If code-affecting: merged + tested | ☐ merged to `main` ☐ test green | Link to the change + the passing test |
| Fresh-install behaviour | ☐ reaches intended API automatically ☐ deliberate prompt shown ☐ neither | |
| Submitted by / date | `________________________` | Name / role / date |

---

## B-DOCS — RELEASE NOTES

**Owner:** Product (approve) · Team 3 (draft/apply, authorized editing phase) · **Status:** ☐ WAITING ☐ EVIDENCE RECEIVED ☐ VERIFICATION RUNNING ☐ PASS ☐ FAIL — *current: **WAITING***

| Field | Value | Notes |
|---|---|---|
| Version section | `________________________` | Must be `## [1.0.0] - <date>` in `CHANGELOG.md`; the `[Unreleased]` stub must no longer hold release content |
| `CHANGELOG.md` commit / diff | `________________________` | The commit that applies the Security / Fixed / Changed blocks (draft: Phase 8.151 TASK F) |
| `RELEASE_NOTES.md` updated | ☐ yes | "Reliability & Security Hardening (Team 3)" section + refreshed Production-Readiness table |
| Known Issues consolidated | ☐ yes | unsigned→signed status · first-launch API default · "coming soon" domains · POS re-charge · window-title inconsistency |
| Product approval | `________________________` | Approver name + role |
| Product approval link | `________________________` | Comment / sign-off referencing the docs commit |
| Publication date | `________________________` | |
| Final changelog location | `________________________` | Path on `main` (`CHANGELOG.md`) + the GitHub Release notes source (`--notes-file` content) + `ROJAN_Web` release-registry entry |
| Submitted by / date | `________________________` | Name / role / date |

---

## POST-v1.0 (tracking only — not PASS 1)

| Gate | Owner | Fields | Status |
|---|---|---|---|
| B5 — Backend contracts | Team 1 | contracts published (link) · Desktop connected · integration tests green | WAITING |
| B6 — POS idempotency | Product + Backend | `/charge` idempotency confirmed · retry UX shipped · double-charge test green | WAITING |

---

## INTAKE STATE (Phase 8.164)

```
Form      Fields completed   Status
B1        0 / 12             WAITING
B7        0 / 10             WAITING
B-DOCS    0 / 9              WAITING
──────────────────────────────────────────
Evidence bundles received: 0 / 3   (0 / 9 required-evidence items per 8.162 board)
PASS 1 gates PASS:          0 / 4   (B8 BLOCKED)
Production decision:        NO-GO
```
