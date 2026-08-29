# ROJAN Reception v1.0 — EVIDENCE VERIFICATION QUEUE v1

| | |
|---|---|
| **Scope** | PASS 1 gate verification queue — B1, B7, B-DOCS |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Queue created** | 2026-08-29 · Phase 8.165 · by Team 3 |
| **Queue owner** | Release Engineering |
| **Companions** | 8.162 evidence board · 8.163 verification workflow · 8.164 intake forms |

> Priority-ordered processing queue. Items are verified in priority order (B1 first — head of the critical path). Team 3 processes nothing here — no evidence is queued, and Team 3 has no certificate / product authority (Phase 8.151). All items **WAITING**.

---

## PRIORITY 1 — B1 CODE SIGNING

| | |
|---|---|
| **Owner** | Release Engineering |
| **Why P1** | Head of the critical path — B8, B4, B2, B3 all wait on it |
| **Required (3)** | 1. Certificate evidence (issuer, subject, type, thumbprint, expiry; no key) · 2. Signature verification (`signtool verify /pa /v` → verified + timestamped + trusted chain + exe/uninstaller signed + named publisher) · 3. Hash match (`Get-FileHash -SHA256` == submitted == `.sha256` sidecar; differs from unsigned `69cb1f29…097615`) |
| **PASS condition** | All 3 verified per the Phase 8.163 B1 workflow |
| **State** | **WAITING** — 0/3 evidence |
| **Blocker** | No certificate procured; `signtool.exe` absent from the automation environment |

---

## PRIORITY 2 — B7 API ENVIRONMENT

| | |
|---|---|
| **Owner** | Product + DevOps |
| **Why P2** | Feeds B8 (sign-off ratifies the shipped build's API behaviour); needed by B2 (live login target) |
| **Required (3)** | 1. Production URL (`https://api.rojanai.ir` + rollout option 1/2/3) · 2. Product approval (dated written record, authorized approver, reason) · 3. Config owner (who applies it + mechanism; if code-affecting, merged to `main` with a green test) |
| **PASS condition** | All 3 verified per the Phase 8.163 B7 workflow; fresh install reaches the intended API with no manual step (or a deliberate prompt) |
| **State** | **WAITING** — 0/3 evidence |
| **Blocker** | Decision requires Product + DevOps authority; not made or recorded |

---

## PRIORITY 3 — B-DOCS RELEASE NOTES

| | |
|---|---|
| **Owner** | Product (approve) · Team 3 (draft/apply, authorized editing phase) |
| **Why P3** | Feeds B8; can proceed in parallel with B1/B7 but Product approval is the last input to sign-off |
| **Required (3)** | 1. Final notes (`CHANGELOG.md` dated `## [1.0.0]`, no stale `[Unreleased]` release content; refreshed `RELEASE_NOTES.md`; consolidated Known Issues) · 2. Approval (Product sign-off referencing the commit) · 3. Publication record (GitHub Release notes source + `ROJAN_Web` registry entry matching the approved text) |
| **PASS condition** | All 3 verified per the Phase 8.163 B-DOCS workflow |
| **State** | **WAITING** — 0/3 evidence |
| **Blocker** | `CHANGELOG.md:9` still `## [Unreleased]`; applying the edit needs a commit (forbidden this phase) → future authorized editing phase + Product approval |

---

## TASK B — STATUS MATRIX

| Gate | Owner | Evidence Count | Verification State | PASS Condition |
|---|---|---|---|---|
| **B1** | Release Engineering | **0 / 3** | **WAITING** | Certificate evidence valid · `signtool verify /pa` = verified + timestamped + trusted + exe/uninstaller signed + named publisher · SHA-256 matches submitted + sidecar |
| **B7** | Product + DevOps | **0 / 3** | **WAITING** | Production URL + rollout option recorded · dated Product approval by an authorized approver · config owner named + (if code) merged with green test · fresh install reaches intended API without manual step |
| **B-DOCS** | Product / Team 3 | **0 / 3** | **WAITING** | `CHANGELOG.md` dated `## [1.0.0]`, no stale `[Unreleased]` · Product approval referencing the commit · published text matches approved text |

Verification-state model (Phase 8.162): `WAITING → EVIDENCE RECEIVED → VERIFICATION RUNNING → {PASS | FAIL}`.

**Downstream (not in queue yet):** B8 (BLOCKED — needs B1 ∧ B7 ∧ B-DOCS PASS) → B4 (BLOCKED — needs B8 ∧ B1).

---

## QUEUE STATE (Phase 8.165)

```
P1  B1     WAITING   0/3   Release Engineering
P2  B7     WAITING   0/3   Product + DevOps
P3  B-DOCS WAITING   0/3   Product / Team 3
────────────────────────────────────────────
Evidence received: 0 / 9
PASS 1 gates PASS: 0 / 4   (B8 BLOCKED)
Production:         NO-GO
```
