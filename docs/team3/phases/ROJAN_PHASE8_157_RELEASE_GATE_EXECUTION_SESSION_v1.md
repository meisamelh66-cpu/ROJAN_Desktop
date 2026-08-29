# ROJAN Reception v1.0 — RELEASE GATE EXECUTION SESSION v1

| | |
|---|---|
| **Release** | ROJAN Reception v1.0 |
| **Main** | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) |
| **Version** | `1.0.0` (frozen) |
| **Audit-trail commit** | `da0c36b` (branch; `docs/team3/**` only; not on `main`) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Session status** | **WAITING FOR OWNER INPUT** |
| **Session opened** | 2026-08-29 · Phase 8.157 · by Team 3 |
| **Session owner** | Release Engineering |
| **Companions** | 8.153 board · 8.154 owner actions · 8.155 checklist · 8.156 war-room log + GO/NO-GO template |

> Live tracking sheet. Each gate section below is pre-formatted for the owner to fill: enter the executor, timestamp, evidence link, and set **Result = PASS / FAIL**. Team 3 opens the session but cannot execute any gate (no certificate, no backend route, no clean VM, no CI access, no product authority — Phase 8.151). All results are **UNSET** at session open.

---

## TASK B — EXECUTION RECORDS

### B1 — SIGNING CERTIFICATE

| Field | Entry |
|---|---|
| Owner | Release Engineering |
| Executor / date | ______________________ |
| Certificate | Type (OV/EV): ______ · CA: ______ · Subject: ______ · Thumbprint: ______ · Expiry: ______ |
| Signing method | ☐ local `publish-installer.ps1 -CertificatePath` ☐ CI (`release.yml` secrets) |
| Verification | `signtool verify /pa /v "ROJAN Reception Setup.exe"` → ______ · timestamp present: ☐ · trusted chain: ☐ · exe + uninstaller signed: ☐ |
| Signed installer SHA-256 | ______________________ |
| First-run publisher | ☐ named publisher shown ☐ still "Unknown Publisher" |
| **Result** | ☐ **PASS** ☐ **FAIL** |
| Exit criteria | Valid, timestamped Authenticode signature on installer + exe + uninstaller; SmartScreen names the publisher |
| Status @ 8.157 | UNSET |

### B2 — LIVE LOGIN

| Field | Entry |
|---|---|
| Owner | QA |
| Depends on | B1, B4, B7 |
| Executor / date | ______________________ |
| Environment | Network → `https://api.rojanai.ir`: ☐ · signed build installed: ☐ · API = Production: ☐ · real OTP phone: ☐ · backend build/env: ______ |
| Steps | startup ___ · API connect ___ · OTP request ___ · OTP verify → session ___ · shell renders ___ · dashboard real data ___ · silent refresh ___ |
| Evidence | screenshots / recording: ______________________ |
| **Result** | ☐ **PASS** ☐ **FAIL** |
| Exit criteria | Full login chain PASS against production with a real OTP |
| Status @ 8.157 | UNSET |

### B3 — CLEAN VM

| Field | Entry |
|---|---|
| Owner | QA / Release Engineering |
| Depends on | B1, B4 |
| Executor / date | ______________________ |
| Windows 10 x64 (no .NET runtime) | launches ___ · installs ___ · no runtime prompt ___ · shortcut ___ · app starts ___ · uninstall clean ___ |
| Windows 11 x64 (no .NET runtime) | launches ___ · installs ___ · no runtime prompt ___ · shortcut ___ · app starts ___ · uninstall clean ___ |
| Evidence | wizard + login + clean-ARP screenshots (both OS): ______________________ |
| **Result** | ☐ **PASS** (both OS) ☐ **FAIL** |
| Exit criteria | All 6 checks PASS on both Windows 10 and Windows 11 |
| Status @ 8.157 | UNSET |

### B4 — PIPELINE

| Field | Entry |
|---|---|
| Owner | DevOps |
| Depends on | B1 (credential), B8 (tag auth), `da0c36b`-equiv on `main`, version reconciliation (existing `v1.0.0` tag → old `d518218`) |
| Executor / date | ______________________ |
| Secrets set | `CODE_SIGNING_CERT_BASE64`: ☐ · `CODE_SIGNING_CERT_PASSWORD`: ☐ (or EV/HSM workflow swap: ☐) |
| Tag | commit SHA: ______ · tag string: `v1.0.x` = ______ · pushed: ☐ |
| Workflow run | URL: ______ · all steps green (incl. tag-vs-version): ☐ |
| Artifacts on Release | signed `.exe`: ☐ · `.sha256`: ☐ · `.zip`: ☐ · Release URL: ______ |
| Signature check | `signtool verify /pa` on CI installer: ______ · hash == `.sha256`: ☐ · ProductVersion: ______ |
| **Result** | ☐ **PASS** ☐ **FAIL** |
| Exit criteria | `release.yml` green on `v1.0.x`; signed installer + matching checksum + ZIP on a GitHub Release |
| Status @ 8.157 | UNSET |

### B7 — API ENVIRONMENT

| Field | Entry |
|---|---|
| Owner | Product (decision) + DevOps (rollout) |
| Executor / date | ______________________ |
| Decision | ☐ Option 1 — flip Release default to `https://api.rojanai.ir` (~5 LOC + 1 test; Team 3 follow-up) ☐ Option 2 — first-run onboarding prompt ☐ Option 3 — ship as-is + document |
| Decision record | endpoint: ______ · approver (name/role): ______ · reason: ______ · date: ______ |
| If code-affecting | follow-up phase auth: ☐ · change merged to `main`: ☐ · test green: ☐ |
| **Result** | ☐ **PASS** (decided + effective) ☐ **FAIL** (undecided) |
| Exit criteria | Decision recorded; if code-affecting, merged with a passing test; fresh install reaches the intended API with no manual step (or prompt shown) |
| Status @ 8.157 | UNSET |

### B8 — PRODUCT APPROVAL

| Field | Entry |
|---|---|
| Owner | Product |
| Depends on | B1, B7, B-DOCS, Phase 8.151 TASK G checklist |
| Executor / date | ______________________ |
| Scope ratified | connected features ship: ☐ · Inv/HR/Acct/POS "coming soon" or cut: ☐ · scope note published: ______ |
| Checklist approved | Phase 8.151 TASK G checklist signed off (ticket/email): ______ |
| Release notes approved | wording + Known Issues: ☐ |
| Tag authorization | written auth for DevOps, naming SHA ______ + tag ______: ☐ |
| **Result** | ☐ **PASS** (approved) ☐ **FAIL** (rejected / withheld) |
| Exit criteria | Product approval + tag authorization on record |
| Status @ 8.157 | UNSET |

### B-DOCS — RELEASE NOTES

| Field | Entry |
|---|---|
| Owner | Team 3 (draft/apply, authorized phase) → Product (approve) |
| Executor / date | ______________________ |
| CHANGELOG | `## [1.0.0] - <date>` added (draft: Phase 8.151 TASK F): ☐ · no stale `[Unreleased]` with release content: ☐ |
| Release notes | `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` refreshed: ☐ · Known Issues consolidated: ☐ |
| Product approval | comment / sign-off: ______ |
| **Result** | ☐ **PASS** ☐ **FAIL** |
| Exit criteria | Dated `## [1.0.0]`; notes current; Product approved |
| Status @ 8.157 | UNSET |

---

## POST-v1.0 (not blocking this decision)

| ID | Owner | Result | Exit criteria |
|---|---|---|---|
| B5 — Backend contracts | Team 1 | UNSET | Contracts published; Desktop swapped from fakes; integration tests green |
| B6 — POS idempotency | Product + Backend | UNSET | Backend `/charge` idempotency confirmed; POS retry UX shipped; double-charge test green |

---

## TASK C — FINAL DECISION BLOCK

### Gate tally

| Gate | Blocking | Result |
|---|---|---|
| B1 Signing | Yes | ______ |
| B2 Live Login | Yes | ______ |
| B3 Clean VM | Yes | ______ |
| B4 Pipeline | Yes | ______ |
| B7 API Environment | Yes | ______ |
| B8 Product Approval | Yes | ______ |
| B-DOCS Release Notes | Yes | ______ |

### ✅ GO

- **All 7 blocking gates = PASS.**
- Desktop still READY on the tagged commit (no regression); test suite 2,715/2,715; 0 P0.
- Signed artifact verified; GitHub Release `v1.0.x` published with matching checksum.
- Product approval + tag authorization on record.
→ **GO → proceed to Production deployment** (publish Release publicly + `ROJAN_Web` release-registry sync).

### ⛔ NO-GO

- **Any** blocking gate = FAIL / BLOCKED / UNSET.
- Unsigned or hash-mismatched installer; pipeline red; tag-vs-version check fails.
- Production API unreachable on a fresh install with no onboarding prompt.
- No written Product approval or tag authorization.
- `CHANGELOG.md` still carries `[Unreleased]` with release content.
- Any new P0 defect, or the suite is not 2,715/2,715 on the tagged commit.
→ **NO-GO → fix the failing gate(s), re-run the exit check, re-open the session.**

### Decision record

| Field | Entry |
|---|---|
| Session close date/time | ______________________ |
| Attendees | Release Eng: ____ · QA: ____ · DevOps: ____ · Product: ____ |
| Blocking gates PASS count | ___ / 7 |
| **DECISION** | ☐ GO ☐ NO-GO |
| Failing gate(s) + owner + ETA | ______________________ |
| Next session | ______________________ |
| Signed | ______________________ |

---

## SESSION STATE AT OPEN (Phase 8.157)

```
Release:  ROJAN Reception v1.0
Main:     77414de   (version 1.0.0, frozen)
Desktop:  READY ✅

B1  Signing Certificate   UNSET    Release Engineering
B2  Live Login            UNSET    QA
B3  Clean VM              UNSET    QA / Release Engineering
B4  Pipeline              UNSET    DevOps
B7  API Environment       UNSET    Product + DevOps
B8  Product Approval      UNSET    Product
BD  Release Notes         UNSET    Team 3 → Product

Blocking gates PASS: 0 / 7
DECISION:            NO-GO   (status: WAITING FOR OWNER INPUT)
```
