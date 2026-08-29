# ROJAN Reception v1.0 — RELEASE OWNER RESPONSE INTAKE v1

| | |
|---|---|
| **Release** | ROJAN Reception v1.0 |
| **Main** | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) · version `1.0.0` (frozen) |
| **Audit-trail commit** | `da0c36b` (branch; `docs/team3/**`; not on `main`) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Intake created** | 2026-08-29 · Phase 8.158 · by Team 3 |
| **Intake owner** | Release Engineering |
| **Companions** | 8.153 board · 8.154 owner actions · 8.155 checklist · 8.156 war-room log · 8.157 execution session |

> Each owner completes their own form below, attaches evidence, and sets the gate **Status** using the model in TASK B. Team 3 collects the forms; it does **not** fill them (no certificate, no backend route, no clean VM, no CI access, no product authority — Phase 8.151). All forms are **blank / WAITING** at intake creation.

---

## TASK A — RESPONSE FORMS

### B1 — RELEASE ENGINEERING · Code Signing

| Question | Response |
|---|---|
| **Certificate provided?** | ☐ Yes ☐ No — Type (OV/EV): ______ · CA: ______ · Subject: ______ · Thumbprint: ______ · Expiry: ______ |
| Delivered to | ☐ CI secrets (`CODE_SIGNING_CERT_BASE64` / `_PASSWORD`) ☐ Signing machine (`.pfx` + Windows SDK) ☐ EV token / cloud-HSM |
| Signing executed via | ☐ `publish-installer.ps1 -CertificatePath …` ☐ `release.yml` (B4) |
| **Signature verified?** | ☐ Yes ☐ No — `signtool verify /pa /v "ROJAN Reception Setup.exe"` result: ______ · timestamp present: ☐ · trusted chain: ☐ · payload exe signed: ☐ · uninstaller signed: ☐ |
| Signed installer SHA-256 | ______________________ |
| First-run SmartScreen | ☐ named publisher ☐ "Unknown Publisher" persists |
| **Evidence** | (links/paths) ______________________ |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B2 — QA · Live Login

| Question | Response |
|---|---|
| **Environment** | Machine/OS: ______ · network → `https://api.rojanai.ir` reachable: ☐ · signed build installed: ☐ · API = Production: ☐ · real OTP phone: ☐ · backend build/env: ______ |
| **Live login result** | startup ___ · API connect ___ · OTP request ___ · OTP verify → session ___ · main shell ___ · dashboard real data ___ · silent token refresh ___ (PASS/FAIL/BLOCKED each) |
| Overall | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Evidence** | screenshots / screen recording / log excerpt: ______________________ |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B3 — QA · Clean VM Installation

| Question | Response |
|---|---|
| **Clean VM OS** | ☐ Windows 10 x64 (no .NET runtime) ☐ Windows 11 x64 (no .NET runtime) — both required |
| **Install result — Win10** | launches ___ · installs ___ · no runtime prompt ___ · shortcut works ___ · app starts to login ___ · uninstall clean ___ |
| **Install result — Win11** | launches ___ · installs ___ · no runtime prompt ___ · shortcut works ___ · app starts to login ___ · uninstall clean ___ |
| SmartScreen on VM | ______ (expect named publisher once B1 PASS) |
| Overall | ☐ PASS (both OS) ☐ FAIL ☐ BLOCKED |
| **Evidence** | wizard + login + post-uninstall ARP screenshots (both OS): ______________________ |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B4 — DEVOPS · Release Pipeline

| Question | Response |
|---|---|
| Secrets configured | `CODE_SIGNING_CERT_BASE64`: ☐ · `CODE_SIGNING_CERT_PASSWORD`: ☐ (or EV/HSM workflow swap: ☐) |
| Version reconciliation | existing `v1.0.0` → old `d518218`; resolution: ☐ move tag ☐ bump `<VersionPrefix>` to `1.0.1` (separate authorized change) — chosen: ______ |
| `da0c36b`-equiv on `main` | ☐ yes ☐ no |
| **Pipeline run** | tag pushed: `v1.0.x` = ______ · commit SHA: ______ · run URL: ______ · all steps green (incl. tag-vs-version check): ☐ |
| **Workflow** | `.github/workflows/release.yml` |
| **Artifact** | signed `ROJAN Reception Setup.exe`: ☐ · `.sha256`: ☐ · `RojanDesktop-v1.0.x-win-x64.zip`: ☐ · GitHub Release URL: ______ · `signtool verify /pa` on CI installer: ______ · hash == `.sha256`: ☐ · ProductVersion: ______ |
| Overall | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Evidence** | run URL · secrets-list screenshot (masked) · Release asset list · verification output: ______________________ |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B7 — PRODUCT / DEVOPS · API Environment

| Question | Response |
|---|---|
| **API decision** | ☐ Option 1 — flip Release default to `https://api.rojanai.ir` (~5 LOC + 1 test; Team 3 follow-up if authorized) ☐ Option 2 — first-run onboarding prompt ☐ Option 3 — ship as-is + document |
| Decision record | endpoint: ______ · reason: ______ · date: ______ |
| **Approval** | approver (name/role): ______ · written record link: ______ |
| If code-affecting | follow-up phase authorized: ☐ · change merged to `main`: ☐ · test green: ☐ |
| Fresh-install behaviour | ☐ reaches intended API with no manual step ☐ deliberate onboarding prompt shown ☐ neither |
| Overall | ☐ DECIDED & EFFECTIVE ☐ DECIDED, PENDING ROLLOUT ☐ UNDECIDED |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B8 — PRODUCT · Sign-off

| Question | Response |
|---|---|
| **Scope approval** | connected features ship: ☐ · Inventory/HR/Accounting/POS presented as "coming soon" ☐ or cut from v1.0 UI ☐ · scope note published: ______ |
| Checklist approval | Phase 8.151 TASK G final release checklist signed off (ticket/email link): ______ |
| Release-notes approval | wording + Known Issues acceptable: ☐ (feeds B-DOCS) |
| **Tag approval** | written authorization for DevOps — commit SHA: ______ · tag string: ______ · link: ______ |
| Overall | ☐ APPROVED ☐ REJECTED ☐ PENDING |
| Respondent | ______________________ (Product owner / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

### B-DOCS — Release Notes / CHANGELOG

| Question | Response |
|---|---|
| CHANGELOG updated | `## [1.0.0] - <date>` added (draft text: Phase 8.151 TASK F): ☐ · no stale `[Unreleased]` holding release content: ☐ |
| Release notes | `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` refreshed: ☐ · Known Issues consolidated: ☐ |
| Applied by | ☐ Team 3 (authorized editing phase ______) |
| **Release notes approved?** | ☐ Yes — Product approver: ______ · link: ______ ☐ No |
| Overall | ☐ PASS ☐ FAIL |
| Respondent | ______________________ (name / role / date) |
| **Gate status** | ☐ WAITING ☐ IN PROGRESS ☐ PASS ☐ FAIL ☐ BLOCKED |

---

## POST-v1.0 (not blocking; intake for tracking only)

| Gate | Owner | Response | Status |
|---|---|---|---|
| B5 — Backend contracts (Inv/HR/Acct/POS) | Team 1 | contracts published: ☐ · Desktop connected: ☐ · integration tests green: ☐ | ☐ WAITING |
| B6 — POS payment idempotency | Product + Backend | backend `/charge` idempotency confirmed: ☐ · retry UX shipped: ☐ · double-charge test green: ☐ | ☐ WAITING |

---

## TASK B — STATUS MODEL

| State | Meaning | Set by |
|---|---|---|
| **WAITING** | Owner has not started; no input received. Default at intake. | — |
| **IN PROGRESS** | Owner has started execution; evidence not yet complete. | Gate owner |
| **PASS** | Exit criteria fully met; evidence attached and verified. | Gate owner (Product-facing gates: Product) |
| **FAIL** | Executed and did not meet exit criteria. Requires a fix + re-execution. | Gate owner |
| **BLOCKED** | Cannot start/finish because an upstream dependency is not PASS, or an external input (certificate, network, VM, budget, decision) is missing. Names the blocker. | Gate owner |

### Transition rules

- `WAITING → IN PROGRESS → {PASS | FAIL | BLOCKED}`.
- `FAIL → IN PROGRESS` (after a fix) → re-evaluate.
- `BLOCKED → IN PROGRESS` once the named blocker clears.
- A gate may only reach **PASS** when every gate it depends on is already **PASS** (B2, B3 depend on B1+B4; B4 depends on B1+B8; B8 depends on B7+B-DOCS).
- **Release decision = GO** only when B1, B2, B3, B4, B7, B8, B-DOCS are **all PASS**. Any other combination = **NO-GO**.

### Dependency-aware initial states (Phase 8.158)

| Gate | Initial state | Reason |
|---|---|---|
| B1 | WAITING | No certificate; owner action not started |
| B7 | WAITING | Decision not made; can start in parallel with B1 |
| B-DOCS | WAITING | Draft text ready (8.151 F); not applied/approved |
| B8 | BLOCKED | Depends on B1, B7, B-DOCS |
| B4 | BLOCKED | Depends on B1, B8 |
| B2 | BLOCKED | Depends on B1, B4, B7 |
| B3 | BLOCKED | Depends on B1, B4 |

---

## INTAKE STATE AT CREATION (Phase 8.158)

```
Release:  ROJAN Reception v1.0
Main:     77414de   (1.0.0, frozen)
Desktop:  READY ✅

B1  Signing            WAITING    Release Engineering
B7  API Environment    WAITING    Product + DevOps
BD  Release Notes      WAITING    Team 3 → Product
B8  Product Sign-off   BLOCKED    (needs B1, B7, B-DOCS)
B4  Pipeline           BLOCKED    (needs B1, B8)
B2  Live Login         BLOCKED    (needs B1, B4, B7)
B3  Clean VM           BLOCKED    (needs B1, B4)

Blocking gates PASS: 0 / 7
DECISION:            NO-GO
```
