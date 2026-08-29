# ROJAN Reception v1.0 — RELEASE GATE EXECUTION CHECKLIST v1

**Created by:** Team 3, Phase 8.155 · **Date:** 2026-08-29
**Code baseline:** `origin/main` = `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` · version `1.0.0` (frozen)
**Board owner after handoff:** Release Engineering · **Companions:** Phase 8.153 (tracking board), Phase 8.154 (owner action package)
**Status legend:** ☐ NOT STARTED · ◐ IN PROGRESS · ☑ DONE · — N/A this phase

> This is a fill-in execution checklist for the external owners. Every box is ☐ as of Phase 8.155 — Team 3 cannot check any of them (no certificate, no backend route, no clean VM, no CI access, no product authority; see Phase 8.151). Owners tick the boxes and attach the evidence artifacts as they execute.

---

## B1 — CODE SIGNING

| Field | Detail |
|---|---|
| **Owner** | Release Engineering (certificate + budget); DevOps executes if signing runs in CI |
| **Depends on** | — (this is the head of the critical path) |
| **Action** | 1. Procure an Authenticode code-signing certificate (EV recommended — immediate SmartScreen trust; OV cheaper). Microsoft-trusted CA, issued to the ROJAN legal entity. 2. Sign: `pwsh build/publish-installer.ps1 -CertificatePath <pfx> -CertificatePassword <pw>` (local, needs Windows SDK) **or** via `release.yml` (B4). Signs the payload exe, the installer, and the embedded uninstaller; RFC-3161 timestamped. 3. Verify. |
| **Evidence** | ☐ Certificate subject / issuer / thumbprint / expiry recorded (not the key) · ☐ `signtool verify /pa /v "ROJAN Reception Setup.exe"` → "Successfully verified" + timestamp present + trusted chain · ☐ payload exe + uninstaller also show a valid signature · ☐ first-run screenshot on a non-dev machine showing a **named publisher** (not "Unknown Publisher") · ☐ signed installer SHA-256 recorded |
| **Completion criteria** | Valid, timestamped Authenticode signature verified on installer + exe + uninstaller; SmartScreen names the publisher |
| **Status** | ☐ NOT STARTED |

---

## B2 — QA LIVE LOGIN

| Field | Detail |
|---|---|
| **Owner** | QA |
| **Depends on** | B1 (signed build), B4 (build produced), B7 (API points at production) |
| **Test environment** | Physical/VM machine on a network with outbound access to `https://api.rojanai.ir` · signed `ROJAN Reception Setup.exe` installed · API environment = Production (`ROJAN_API_BASE_URL=https://api.rojanai.ir` or Settings → API environment → Production → restart) · a **real phone number** able to receive OTP SMS · known production backend build/env |
| **Action** | Install → launch → confirm API = production → enter real phone → request OTP → enter received code → confirm sign-in → observe main shell + dashboard → leave idle past access-token expiry → confirm silent refresh |
| **Evidence** | Per step, record **PASS / FAIL / BLOCKED**: ☐ app startup · ☐ API connection (no error) · ☐ OTP request accepted · ☐ OTP verify → session created · ☐ main shell renders · ☐ dashboard shows **real** data · ☐ silent token refresh works · ☐ dashboard + "logged in as" screenshots · ☐ backend env noted |
| **Completion criteria** | Full chain PASS against the production backend with a real OTP |
| **Status** | ☐ NOT STARTED |

---

## B3 — CLEAN VM INSTALLATION

| Field | Detail |
|---|---|
| **Owner** | QA / Release Engineering |
| **Depends on** | B1 (signed build), B4 (build produced) |
| **Windows versions** | ☐ Windows 10 x64 (fresh, no .NET runtime/SDK, no prior ROJAN install) · ☐ Windows 11 x64 (same) |
| **Action (per VM)** | Copy installer → run GUI wizard → default per-user location → confirm **no ".NET Desktop Runtime required" prompt** → launch from Start-Menu shortcut → confirm login screen renders → run "Uninstall ROJAN Reception" → confirm all traces gone (`%LocalAppData%\Programs\ROJAN Reception`, Start-Menu folder, ARP entry, `%LocalAppData%\RojanDesktop`) |
| **Install result** | Win10: ☐ launches ☐ installs ☐ no runtime prompt ☐ shortcut works ☐ app starts ☐ uninstall clean · Win11: ☐ launches ☐ installs ☐ no runtime prompt ☐ shortcut works ☐ app starts ☐ uninstall clean |
| **Evidence** | ☐ wizard screenshot (both OS) · ☐ running login screen screenshot (both OS) · ☐ clean ARP list post-uninstall (both OS) · ☐ SmartScreen behaviour noted (named publisher once B1 done) · runbook: `docs/team3/phases/…Production_Checklist.md §8` |
| **Completion criteria** | All 6 checks PASS on **both** Windows 10 and Windows 11 |
| **Status** | ☐ NOT STARTED |

---

## B4 — RELEASE PIPELINE

| Field | Detail |
|---|---|
| **Owner** | DevOps |
| **Depends on** | B1 (signing credential), B8 (tag authorization), audit-trail commit on `main`, version reconciliation |
| **Workflow** | `.github/workflows/release.yml` — triggers on `push` of a `v*` tag; steps: checkout → setup .NET 8 → restore → **verify tag == `Directory.Build.props` version** → build → test → `publish-installer.ps1` (signs if `CODE_SIGNING_CERT_BASE64`/`_PASSWORD` set) → generate `.sha256` → upload artifacts → `gh release create` |
| **Action** | 1. DevOps sets `CODE_SIGNING_CERT_BASE64` + `CODE_SIGNING_CERT_PASSWORD` secrets (or swap the step for EV/HSM signing). 2. Ensure `da0c36b` (or equivalent) on `origin/main`. 3. Reconcile version: existing `v1.0.0` tag points at old `d518218` — either move it to the release commit or bump `<VersionPrefix>` to `1.0.1` (separate authorized change) and tag `v1.0.1`. 4. `git tag -a v1.0.x <sha> -m "…" && git push origin v1.0.x`. |
| **Artifact** | ☐ `ROJAN Reception Setup.exe` (signed) · ☐ `ROJAN Reception Setup.exe.sha256` · ☐ `RojanDesktop-v1.0.x-win-x64.zip` — all attached to a GitHub Release named `v1.0.x` |
| **Signature** | ☐ `signtool verify /pa` on the CI-produced installer = success · ☐ `Get-FileHash -SHA256` matches the `.sha256` asset · ☐ exe ProductVersion = `1.0.x` |
| **Evidence** | ☐ Actions run URL, all steps green (incl. version check) · ☐ secrets-list screenshot (values masked) · ☐ Release asset list · ☐ verification output |
| **Completion criteria** | `release.yml` green on `v1.0.x`; signed installer + matching checksum + ZIP on a GitHub Release |
| **Status** | ☐ NOT STARTED |

---

## B7 — API ENVIRONMENT

| Field | Detail |
|---|---|
| **Owner** | Product (decision) + DevOps (rollout) |
| **Depends on** | — (can proceed in parallel with B1) |
| **Current state** | `ApiEnvironmentService.SelectedEnvironment` defaults to `Development` → `http://localhost:8080`; production = `https://api.rojanai.ir`; `ROJAN_API_BASE_URL` overrides; user-switchable in Settings (restart-required) |
| **Decision** | ☐ Option 1 — flip Release-build default to Production (**~5-line change in `ApiEnvironmentService` + 1 unit test**; Team 3 follow-up if authorized; no Debug behaviour change) · ☐ Option 2 — force environment choice in first-run onboarding · ☐ Option 3 — ship as-is, document that staff must set it on first launch · **Recommended: Option 1** |
| **Approval** | ☐ Written decision record: chosen option · endpoint (`https://api.rojanai.ir`) · approver name/role · reason · date · ☐ if Option 1/2: follow-up phase authorization for Team 3 + green test |
| **Completion criteria** | Decision recorded; if code-affecting, merged to `main` with a passing test; a fresh install reaches the intended API with no manual step (or the onboarding prompt appears) |
| **Status** | ☐ NOT STARTED |

---

## B8 — PRODUCT

| Field | Detail |
|---|---|
| **Owner** | Product |
| **Depends on** | B1 (credential exists), B7 (API decision), B-DOCS (notes drafted), Phase 8.151 TASK G checklist |
| **Sign-off** | ☐ Ratify v1.0 scope: Auth/Salon/Dashboard/Customers/Services/Specialists/Booking/QR/Support/Automation ship connected; Inventory/HR/Accounting/POS presented as "coming soon" (or cut from v1.0 UI) · ☐ Approve the Phase 8.151 TASK G final release checklist in writing · ☐ Approve release-notes wording + Known Issues list (B-DOCS gate) |
| **Tag approval** | ☐ Written authorization for DevOps to create + push the release tag, naming the **commit SHA** and the exact **tag string** (see B4 version reconciliation) |
| **Evidence** | ☐ Published v1.0 scope note (what ships / coming soon / cut; approver + date) · ☐ Written checklist approval (ticket comment / signed doc / email) · ☐ Written tag authorization |
| **Completion criteria** | Product approval + tag authorization on record |
| **Status** | ☐ NOT STARTED |

---

## B-DOCS — RELEASE NOTES (gate, not in the phase's B-list but on the critical path)

| Field | Detail |
|---|---|
| **Owner** | Team 3 (draft/apply, authorized phase) → Product (approve) |
| **Action** | Convert `CHANGELOG.md [Unreleased]` → `## [1.0.0] - <date>` with Security/Fixed/Changed blocks (draft text: Phase 8.151 TASK F); refresh `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md`; consolidate Known Issues |
| **Evidence** | ☐ `CHANGELOG.md` has a dated `## [1.0.0]` section · ☐ no stale `[Unreleased]` holding release content · ☐ release notes reflect the hardening + current Known Issues · ☐ Product approval comment |
| **Completion criteria** | Dated `## [1.0.0]`; notes current; Product approved |
| **Status** | ☐ NOT STARTED |

---

## POST-v1.0 (not release-blocking)

| Gate | Owner | Completion criteria | Status |
|---|---|---|---|
| B5 — Inventory/HR/Accounting/POS backend contracts | Team 1 | Contracts published; Desktop repos swapped from `Fake*Repository`; integration tests green | ☐ NOT STARTED |
| B6 — POS payment idempotency | Product + Backend | Backend `/charge` idempotency confirmed; POS retry UX specified + implemented; double-charge test green | ☐ NOT STARTED |

---

## SEQUENCING

```
B1 ─┬─────────────────────────► B4 ─┬─► B2
    │   B-DOCS ─┐                   └─► B3
    │   B7 ─────┼─► B8 ─────────────┘
    └───────────┘                        └─► Production deployment
```

**Release is shippable when B1 ∧ B2 ∧ B3 ∧ B4 ∧ B7 ∧ B8 ∧ B-DOCS = ☑**, then Production deployment.

---

## DESKTOP (Team 3) — ALREADY ☑

| Item | Status |
|---|---|
| Desktop code (hardening, security, logging; 58/58 error surfaces sanitized) | ☑ DONE (`main` `77414de`) |
| Build — Debug + Release | ☑ 0 warn / 0 err |
| Tests + Architecture | ☑ 2,715/2,715 (0 skipped) both configs · 7/7 |
| Installer generation | ☑ `ROJAN Reception Setup.exe` (unsigned) |
| Installer validation (build machine) | ☑ install / launch-to-login / uninstall |
| Artifact package | ☑ reproducible, version 1.0.0 single-sourced |
| Signing toolchain wiring | ☑ scripts + `.iss` + `release.yml` verified inert |
| Audit trail | ☑ 144 reports, commit `da0c36b` |
| P0 defects | ☑ 0 |
