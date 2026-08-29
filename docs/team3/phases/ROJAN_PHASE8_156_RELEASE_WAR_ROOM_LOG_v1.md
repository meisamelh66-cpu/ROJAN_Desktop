# ROJAN Reception — RELEASE WAR ROOM FINAL EXECUTION LOG v1

| | |
|---|---|
| **Release Version** | `1.0.0` (frozen — `Directory.Build.props` `<VersionPrefix>`) |
| **Current Main** | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) |
| **Audit-trail commit** | `da0c36b` (branch `feature/team3-desktop-completion`; `docs/team3/**` only; `src`/`tests` == `77414de`; not yet on `main`) |
| **Desktop Status** | **READY ✅** |
| **Log created by** | Team 3, Phase 8.156 · 2026-08-29 |
| **Log owner after handoff** | Release Engineering |
| **Companions** | Phase 8.153 (tracking board) · 8.154 (owner action package) · 8.155 (execution checklist) |

> **Instructions for the war room:** one row per gate. Fill `Start Time`, `Evidence` (link/path to the artifact), `Result` (PASS / FAIL / BLOCKED), `Approval` (name + role + timestamp) as each gate executes. Team 3 cannot populate any row — no certificate, no backend route, no clean VM, no CI access, no product authority (Phase 8.151). All rows are **PENDING** at log creation.

---

## GATE EXECUTION LOG

### B1 — CODE SIGNING

| Field | Entry |
|---|---|
| **ID** | B1 |
| **Owner** | Release Engineering (certificate + budget); DevOps if signing runs in CI |
| **Start Time** | _______________________ |
| **Evidence** | Cert subject/issuer/thumbprint/expiry: __________ · `signtool verify /pa /v "ROJAN Reception Setup.exe"` output: __________ · payload exe + uninstaller signature confirmed: ☐ · named-publisher first-run screenshot: __________ · signed installer SHA-256: __________ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Approval** | _______________________ (name / role / time) |
| **Exit criteria** | Valid, timestamped Authenticode signature verified on installer + exe + uninstaller; SmartScreen names the publisher |
| **Status @ 8.156** | PENDING |

### B2 — LIVE LOGIN

| Field | Entry |
|---|---|
| **ID** | B2 |
| **Owner** | QA |
| **Depends on** | B1, B4, B7 |
| **Start Time** | _______________________ |
| **Test environment** | Machine + network with access to `https://api.rojanai.ir`; signed installer; API = Production; real phone for OTP; backend build/env: __________ |
| **Evidence** | Per step PASS/FAIL/BLOCKED — startup __ · API connect __ · OTP request __ · OTP verify → session __ · shell renders __ · dashboard real data __ · silent refresh __ · screenshots: __________ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Approval** | _______________________ (name / role / time) |
| **Exit criteria** | Full login chain PASS against production backend with a real OTP |
| **Status @ 8.156** | PENDING |

### B3 — CLEAN VM INSTALLATION

| Field | Entry |
|---|---|
| **ID** | B3 |
| **Owner** | QA / Release Engineering |
| **Depends on** | B1, B4 |
| **Start Time** | _______________________ |
| **Windows versions** | ☐ Windows 10 x64 (no .NET runtime) ☐ Windows 11 x64 (no .NET runtime) |
| **Install result** | Win10: launches __ / installs __ / no runtime prompt __ / shortcut __ / app starts __ / uninstall clean __ · Win11: launches __ / installs __ / no runtime prompt __ / shortcut __ / app starts __ / uninstall clean __ |
| **Evidence** | Wizard screenshots (both OS): __________ · login-screen screenshots: __________ · clean ARP post-uninstall: __________ · SmartScreen note: __________ |
| **Result** | ☐ PASS (both OS) ☐ FAIL ☐ BLOCKED |
| **Approval** | _______________________ (name / role / time) |
| **Exit criteria** | All 6 checks PASS on **both** Windows 10 and Windows 11 |
| **Status @ 8.156** | PENDING |

### B4 — RELEASE PIPELINE

| Field | Entry |
|---|---|
| **ID** | B4 |
| **Owner** | DevOps |
| **Depends on** | B1 (credential), B8 (tag auth), `da0c36b`-equiv on `main`, version reconciliation |
| **Start Time** | _______________________ |
| **Workflow** | `.github/workflows/release.yml` — run URL: __________ · all steps green (incl. tag-vs-version check): ☐ |
| **Artifact** | ☐ signed `ROJAN Reception Setup.exe` ☐ `.sha256` ☐ `RojanDesktop-v1.0.x-win-x64.zip` — GitHub Release `v1.0.x`: __________ |
| **Signature** | `signtool verify /pa` on CI installer: __________ · hash matches `.sha256` asset: ☐ · exe ProductVersion: __________ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Approval** | _______________________ (name / role / time) |
| **Exit criteria** | `release.yml` green on `v1.0.x`; signed installer + matching checksum + ZIP on a GitHub Release |
| **Status @ 8.156** | PENDING |

### B7 — API ENVIRONMENT

| Field | Entry |
|---|---|
| **ID** | B7 |
| **Owner** | Product (decision) + DevOps (rollout) |
| **Start Time** | _______________________ |
| **Decision** | ☐ Option 1 — flip Release default to `https://api.rojanai.ir` (~5 LOC + 1 test, Team 3 follow-up) ☐ Option 2 — first-run onboarding prompt ☐ Option 3 — ship as-is + document · **chosen:** __________ |
| **Evidence** | Decision record (endpoint / approver / reason / date): __________ · if Option 1/2: follow-up phase auth + green test: __________ |
| **Result** | ☐ DECIDED ☐ PENDING |
| **Approval** | _______________________ (name / role / time) |
| **Exit criteria** | Decision recorded; if code-affecting, merged with a passing test; fresh install reaches intended API with no manual step (or prompt shown) |
| **Status @ 8.156** | PENDING |

### B8 — PRODUCT SIGN-OFF

| Field | Entry |
|---|---|
| **ID** | B8 |
| **Owner** | Product |
| **Depends on** | B1, B7, B-DOCS, Phase 8.151 TASK G checklist |
| **Start Time** | _______________________ |
| **Sign-off** | ☐ v1.0 scope ratified (connected features ship; Inv/HR/Acct/POS "coming soon" or cut) ☐ Phase 8.151 TASK G checklist approved in writing ☐ release-notes wording + Known Issues approved |
| **Tag approval** | ☐ Written authorization for DevOps to push the tag — commit SHA: __________ · tag string: __________ |
| **Evidence** | Scope note: __________ · checklist approval (ticket/email): __________ · tag authorization: __________ |
| **Result** | ☐ APPROVED ☐ REJECTED ☐ PENDING |
| **Approval** | _______________________ (Product owner / time) |
| **Exit criteria** | Product approval + tag authorization on record |
| **Status @ 8.156** | PENDING |

### B-DOCS — RELEASE NOTES / CHANGELOG

| Field | Entry |
|---|---|
| **ID** | B-DOCS |
| **Owner** | Team 3 (draft/apply in an authorized phase) → Product (approve) |
| **Start Time** | _______________________ |
| **Evidence** | ☐ `CHANGELOG.md` has dated `## [1.0.0]` (draft text: Phase 8.151 TASK F) ☐ no stale `[Unreleased]` holding release content ☐ `RELEASE_NOTES.md` refreshed ☐ Known Issues consolidated ☐ Product approval comment: __________ |
| **Result** | ☐ PASS ☐ PENDING |
| **Approval** | _______________________ (Product / time) |
| **Exit criteria** | Dated `## [1.0.0]`; notes current; Product approved |
| **Status @ 8.156** | PENDING |

---

## POST-v1.0 (NOT release-blocking — logged for completeness)

| ID | Owner | Exit criteria | Status |
|---|---|---|---|
| B5 | Team 1 | Inv/HR/Acct/POS contracts published; Desktop swapped from fakes; integration tests green | PENDING |
| B6 | Product + Backend | Backend `/charge` idempotency confirmed; POS retry UX shipped; double-charge test green | PENDING |

---

## PRODUCTION DEPLOYMENT (after all blocking gates PASS)

| Field | Entry |
|---|---|
| **Owner** | Release Engineering |
| **Depends on** | B1 ∧ B2 ∧ B3 ∧ B4 ∧ B7 ∧ B8 ∧ B-DOCS all PASS |
| **Action** | Publish the GitHub Release publicly; update `ROJAN_Web` `release-registry.ts` with the signed installer URL + SHA-256 |
| **Evidence** | Public Release URL: __________ · registry commit: __________ · verified download hash match: ☐ |
| **Result** | ☐ DEPLOYED ☐ PENDING |
| **Status @ 8.156** | PENDING |

---

## TASK B — GO / NO-GO TEMPLATE

### Decision meeting inputs

| Gate | Owner | Result | Blocking? |
|---|---|---|---|
| B1 Signing | Release Eng | ______ | Yes |
| B2 Live Login | QA | ______ | Yes |
| B3 Clean VM | QA / Release Eng | ______ | Yes |
| B4 Pipeline | DevOps | ______ | Yes |
| B7 API Environment | Product + DevOps | ______ | Yes |
| B8 Product Sign-off | Product | ______ | Yes |
| B-DOCS Release Notes | Team 3 → Product | ______ | Yes |
| B5 Backend contracts | Team 1 | ______ | No (post-v1.0) |
| B6 POS idempotency | Product + Backend | ______ | No (post-v1.0) |

### ✅ GO CONDITIONS — **ALL** must hold

1. **All 7 blocking gates = PASS** — B1 ∧ B2 ∧ B3 ∧ B4 ∧ B7 ∧ B8 ∧ B-DOCS.
2. B1: `signtool verify /pa` succeeds, timestamped, publisher named.
3. B2: full login chain PASS against production with a real OTP.
4. B3: 6/6 checks PASS on **both** Windows 10 and Windows 11.
5. B4: `release.yml` green; **signed** installer + matching `.sha256` + ZIP on a GitHub Release `v1.0.x`.
6. B7: API-environment decision recorded; production install reaches the intended API with no manual step (or a deliberate onboarding prompt).
7. B8: written Product approval + written tag authorization on record.
8. B-DOCS: `CHANGELOG.md` dated `## [1.0.0]`, no stale `[Unreleased]`; Product-approved release notes.
9. Desktop status still READY (unchanged from `77414de`; no regression); test suite 2,715/2,715 on the tagged commit.

**If 1–9 all true → GO → Production deployment.**

### ⛔ NO-GO CONDITIONS — **ANY** triggers NO-GO

- Any blocking gate (B1, B2, B3, B4, B7, B8, B-DOCS) = **FAIL** or **BLOCKED** or **PENDING**.
- B1: signature missing / untrusted chain / no timestamp / "Unknown Publisher" persists.
- B2: login chain fails at any step, or the dashboard shows no real data.
- B3: any of the 6 checks fails on either OS, or a ".NET runtime required" prompt appears.
- B4: `release.yml` fails, the tag-vs-version check fails, or the produced installer is unsigned / hash-mismatched.
- B7: no recorded decision, or a production install cannot reach the API and no prompt is shown.
- B8: no written Product approval, or no tag authorization.
- B-DOCS: `CHANGELOG.md` still carries `[Unreleased]` with release content, or notes are stale / unapproved.
- Any new P0 defect surfaces on the tagged commit, or the test suite is not 2,715/2,715.

**If any condition true → NO-GO → fix the failing gate, re-run its exit check, re-convene.**

### Decision record

| Field | Entry |
|---|---|
| Meeting date/time | _______________________ |
| Attendees (Release Eng / QA / DevOps / Product) | _______________________ |
| Blocking gates status | _______________________ |
| **DECISION** | ☐ GO ☐ NO-GO |
| If NO-GO: failing gate(s) + owner + ETA | _______________________ |
| Next review | _______________________ |
| Signed | _______________________ |

---

## STATUS AT LOG CREATION (Phase 8.156)

```
Release Version:   1.0.0
Current Main:      77414de
Desktop Status:    READY ✅   (2,715/2,715 · 7/7 · 0/0 · installer built · 0 P0)

B1 Signing        PENDING   Release Engineering
B2 Live Login     PENDING   QA
B3 Clean VM       PENDING   QA / Release Engineering
B4 Pipeline       PENDING   DevOps
B7 API Environment PENDING   Product + DevOps
B8 Product Sign-off PENDING  Product
B-DOCS Release Notes PENDING Team 3 → Product

GO / NO-GO:        NO-GO  (0 of 7 blocking gates PASS)
```
