# ROJAN Reception v1.0 — FIRST EVIDENCE PROCESSING SESSION v1

| | |
|---|---|
| **Scope** | Processing workflow for a submitted PASS 1 gate evidence package |
| **Release** | ROJAN Reception v1.0 · Main `77414de` · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Session created** | 2026-08-29 · Phase 8.166 · by Team 3 |
| **Processor / session owner** | Release Engineering |
| **Companions** | 8.162 evidence board · 8.163 verification workflow · 8.164 intake forms · 8.165 verification queue |

> Defines the repeatable processing pipeline applied to each gate evidence package. **No package has been submitted** — Team 3 has no evidence to process (no certificate, no product authority — Phase 8.151). The session is armed and idle.

---

## TASK A — PROCESSING WORKFLOW

### INPUT

**Gate Evidence Package** = { gate ID (B1 / B7 / B-DOCS) · completed Phase 8.164 intake form · attached artifacts (signtool output, screenshots, approval links, CHANGELOG diff) · submitter identity + date }.

### PROCESS

```
        ┌──────────────────────────────────────────────────────┐
        │  INPUT: Gate Evidence Package                         │
        └───────────────────────┬──────────────────────────────┘
                                ▼
   ┌─────────────────────────────────────────────────────────────┐
   │ STEP 1 — Validate owner submission                           │
   │  • submitter is the gate's designated owner (8.152 matrix)   │
   │  • submission is dated and attributable                      │
   │  • package targets a single gate                             │
   │  → fail ⇒ reject to sender, state stays WAITING              │
   └───────────────────────┬─────────────────────────────────────┘
                           ▼
   ┌─────────────────────────────────────────────────────────────┐
   │ STEP 2 — Validate required fields                            │
   │  • every field on the 8.164 intake form is populated         │
   │  • no placeholder / TBD values                               │
   │  • artifact links resolve                                    │
   │  → fail ⇒ FAIL (incomplete), return to owner                 │
   │  → pass ⇒ state = EVIDENCE RECEIVED                          │
   └───────────────────────┬─────────────────────────────────────┘
                           ▼
   ┌─────────────────────────────────────────────────────────────┐
   │ STEP 3 — Run verification checklist (Phase 8.163 workflow)   │
   │  state = VERIFICATION RUNNING                                │
   │  B1:   signtool verify /pa /v  +  timestamp  +  chain  +     │
   │        exe/uninstaller signed  +  SHA-256 match  +  named    │
   │        publisher screenshot                                  │
   │  B7:   endpoint + option recorded  +  approver authority     │
   │        confirmed  +  decision effective (merged+test /       │
   │        prompt / documented)                                  │
   │  B-DOCS: CHANGELOG dated ## [1.0.0]  +  no stale             │
   │        [Unreleased]  +  Product approval  +  published text  │
   │        matches                                              │
   │  → all checks OK ⇒ PASS                                      │
   │  → any check fails ⇒ FAIL, itemise, return to owner         │
   └───────────────────────┬─────────────────────────────────────┘
                           ▼
   ┌─────────────────────────────────────────────────────────────┐
   │ STEP 4 — Update gate state                                   │
   │  • write result (PASS / FAIL) + verifier + date to the       │
   │    8.162 board and the 8.165 queue                           │
   │  • on PASS: re-evaluate B8 (unblocks when B1∧B7∧B-DOCS PASS) │
   │  • on FAIL: gate returns to WAITING after owner resupplies   │
   └─────────────────────────────────────────────────────────────┘
```

### STATE MODEL

| State | Entered when | Exit |
|---|---|---|
| **WAITING** | No package, or a prior package was rejected/failed and not yet resupplied | → EVIDENCE RECEIVED (Step 2 pass) |
| **EVIDENCE RECEIVED** | Steps 1–2 passed; all fields present | → VERIFICATION RUNNING (Step 3 start) |
| **VERIFICATION RUNNING** | Checklist in progress | → PASS or FAIL (Step 3 result) |
| **PASS** | Every checklist item verified | terminal (unless reopened by a later contradiction) |
| **FAIL** | Any checklist item failed, or fields incomplete | → WAITING (owner resupplies) |

**Gate PASS 1 result** flips to PASS only on a **PASS** here. **PASS 1 COMPLETE** = B1 ∧ B7 ∧ B-DOCS PASS + B8 signed off.

---

## TASK B — INITIAL STATE

| Gate | Owner | Package received? | State | Evidence |
|---|---|---|---|---|
| **B1** | Release Engineering | No | **WAITING** | 0 / 3 |
| **B7** | Product + DevOps | No | **WAITING** | 0 / 3 |
| **B-DOCS** | Product / Team 3 | No | **WAITING** | 0 / 3 |

**Evidence received: 0 / 9.** No processing performed — session idle.

---

## SESSION STATE (Phase 8.166)

```
Processing session: ARMED, idle — no evidence packages in the input queue
Main:      77414de   (1.0.0, frozen)
Desktop:   READY ✅

B1     WAITING   0/3   Release Engineering
B7     WAITING   0/3   Product + DevOps
B-DOCS WAITING   0/3   Product / Team 3

Evidence received: 0 / 9
PASS 1 gates PASS: 0 / 4   (B8 BLOCKED)
Production:         NO-GO
```
