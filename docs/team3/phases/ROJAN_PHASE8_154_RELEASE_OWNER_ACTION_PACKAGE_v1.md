# ROJAN Reception v1.0 — RELEASE OWNER ACTION REQUEST PACKAGE v1

**Created by:** Team 3, Phase 8.154 · **Date:** 2026-08-29
**Code baseline:** `origin/main` = `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` · version `1.0.0` (frozen — do not modify `.cs` / `.xaml` / `.csproj` / build logic)
**Audit-trail commit (branch, not yet on `main`):** `da0c36b` — `docs/team3/**` only; `src`/`tests` identical to `77414de`
**Companion:** Phase 8.153 tracking board (gate IDs, exit conditions, critical path)

This package is addressed to four owners. Each section is self-contained: what you receive, what to run, what evidence to file, and what "done" looks like. Nothing here requires a Desktop code change; where a tiny code change is optional (B7), it is flagged and scoped for a Team 3 follow-up phase.

**Critical path:** B1 → (B-DOCS + B7) → B8 → B4 → (B2 ∥ B3) → Production deployment.

---

## OWNER 1 — RELEASE ENGINEERING

### Actions
1. **Provide certificate** (B1)
2. **Sign installer** (B1 — or delegate the signing execution to DevOps via CI, OWNER 3)
3. **Verify signature** (B1)
4. (Later) **Production deployment** — publish the GitHub Release + sync `ROJAN_Web` release registry

### 1 — Provide certificate

| | |
|---|---|
| **Required input** | An **Authenticode code-signing certificate** for a Windows desktop `.exe` (NOT a TLS cert). Issued by a Microsoft-trusted CA (DigiCert, Sectigo, SSL.com, GlobalSign) in the name of the legal entity behind ROJAN. Identity-verified — allow a few business days + business documentation. |
| **Type decision** | **OV** — file-based `.pfx`, cheaper, SmartScreen reputation builds over weeks of installs. **EV** (recommended in `docs/standards/code-signing.md`) — immediate SmartScreen trust, but the private key must live on a hardware token / cloud-HSM, which changes the CI invocation (see OWNER 3). Pick one; the repo supports both. |
| **Execution action** | Purchase / obtain the certificate. For OV: export as `.pfx` with a strong private-key password. For EV: obtain token or cloud-HSM signing credentials from the CA. |
| **Evidence required** | Certificate subject + issuer + thumbprint + expiry recorded (not the private key). For OV: confirmation the `.pfx` + password are stored in the org secret manager. |
| **Completion state** | A usable signing credential exists and is handed to DevOps (CI) and/or a Release Engineering signing machine with the Windows SDK. |

### 2 — Sign installer

| | |
|---|---|
| **Required input** | The certificate (above); a machine with `signtool.exe` (Windows SDK) **or** the CI path (OWNER 3); the code at `77414de`. |
| **Execution action (local option)** | `pwsh build/publish-installer.ps1 -CertificatePath <path.pfx> -CertificatePassword <pw>` — this publishes a fresh Release build, Authenticode-signs `Rojan.Desktop.Shell.exe`, then signs the installer **and** the embedded uninstaller (via `/DSignInstaller=1`), timestamped against `http://timestamp.digicert.com` (override with `-TimestampUrl`). |
| **Evidence required** | Console output showing `Signing …` for the exe and `(signed)` for the installer; the produced `artifacts/ROJAN Reception Setup.exe` + its `.sha256`. |
| **Completion state** | A signed `ROJAN Reception Setup.exe` exists (or is produced by CI in B4). |

### 3 — Verify signature

| | |
|---|---|
| **Required input** | The signed installer. |
| **Execution action** | `signtool verify /pa /v "artifacts\ROJAN Reception Setup.exe"` · right-click → Properties → Digital Signatures tab · confirm the embedded exe and uninstaller are also signed. |
| **Evidence required** | `signtool verify` output: `Successfully verified`; chain to a trusted root; **timestamp present**; publisher name matches the legal entity. A screenshot of a first-run on a non-developer machine showing a **named publisher** (not "Unknown Publisher"). |
| **Completion state (B1 DONE)** | Valid, timestamped Authenticode signature verified on the installer, payload exe, and uninstaller; SmartScreen shows a named publisher. |

### 4 — Production deployment (after B4)

| | |
|---|---|
| **Required input** | Green `release.yml` run (B4); the signed installer + checksum on a draft GitHub Release. |
| **Execution action** | Publish the GitHub Release publicly. Update `ROJAN_Web`'s `release-registry.ts` with the signed installer URL + SHA-256 (manual cross-repo step, per `docs/standards/release-process.md §4`). |
| **Evidence required** | Public Release URL; `release-registry.ts` diff/commit; a clean download of the installer whose SHA-256 matches the `.sha256` asset. |
| **Completion state** | Public download live; web release registry points at the signed `v1.0.x` installer with a matching checksum. |

---

## OWNER 2 — QA

### Actions
1. **Execute live login test** (B2)
2. **Execute clean VM install** (B3 — shared with Release Engineering)

### 1 — Live login test (B2)

| | |
|---|---|
| **Required input** | The **signed** `ROJAN Reception Setup.exe` (post-B1/B4); a machine on a network with outbound access to `https://api.rojanai.ir`; a **real phone number** able to receive OTP SMS; the API-environment decision applied or a known way to point the app at production (`ROJAN_API_BASE_URL=https://api.rojanai.ir` or Settings → API environment → Production → restart). |
| **Execution action** | Install → launch → (ensure API = production) → enter the real phone number → request OTP → enter the received code → confirm sign-in → observe the main shell and dashboard. Repeat once for token refresh (leave idle past access-token expiry, confirm silent refresh). |
| **Evidence required** | Recorded result **PASS / FAIL / BLOCKED** for each step: app startup · API connection (no connection error) · OTP request accepted · OTP verify → session created · main shell renders · dashboard shows **real** data (not empty/error). Screenshots of the dashboard + the "logged in as" state. Note the backend build/env used. |
| **Completion state (B2 DONE)** | Full chain PASS against the production backend with a real OTP. |

### 2 — Clean VM install (B3)

| | |
|---|---|
| **Required input** | The **signed** installer; two fresh VMs — **Windows 10 x64** and **Windows 11 x64** — with **no .NET runtime or SDK** installed and no prior ROJAN install. |
| **Execution action** | On each VM: copy the installer over → run it (GUI, not silent) → complete the wizard (default per-user location) → confirm no ".NET Desktop Runtime required" prompt → launch from the Start-Menu shortcut → confirm the login screen renders → run the Start-Menu "Uninstall ROJAN Reception" → confirm all traces removed (`%LocalAppData%\Programs\ROJAN Reception`, Start-Menu folder, ARP entry, `%LocalAppData%\RojanDesktop`). |
| **Evidence required** | Per VM (Win10, Win11): installer launches ✅/❌ · install succeeds ✅/❌ · runtime prompt absent ✅/❌ · shortcut works ✅/❌ · app starts to login screen ✅/❌ · uninstall clean ✅/❌. SmartScreen behaviour noted (should show a named publisher once B1 is done). Screenshots of the wizard, the running login screen, and a clean ARP list post-uninstall. |
| **Completion state (B3 DONE)** | All six checks PASS on **both** Windows 10 and Windows 11. |

> Runbook reference: `docs/team3/phases/…RojanReception_v1.0_Production_Checklist.md §8`.

---

## OWNER 3 — DEVOPS

### Actions
1. **Configure secrets** (B4 prerequisite)
2. **Run `release.yml`** (B4 — after B1 credential + B8 tag authorization)
3. **Verify artifact** (B4)

### 1 — Configure secrets

| | |
|---|---|
| **Required input** | From Release Engineering: the signing credential. For **OV/`.pfx`**: `CODE_SIGNING_CERT_BASE64` = `base64(pfx bytes)`, `CODE_SIGNING_CERT_PASSWORD` = the private-key password. For **EV/HSM**: coordinate with the CA's cloud-HSM signing service — the current `release.yml` step (base64→`.pfx`→`signtool /f`) must be swapped for the CA's signing action/CLI (a workflow YAML change, no repo source change). |
| **Execution action** | Add the two repository secrets in GitHub → Settings → Secrets and variables → Actions. For EV, additionally adjust the "Publish installer" step of `.github/workflows/release.yml` to the HSM signing method. |
| **Evidence required** | Screenshot of the Actions secrets list showing both names present (values masked). For EV: the workflow diff. |
| **Completion state** | Secrets configured; `release.yml` can sign. |

### 2 — Run `release.yml`

| | |
|---|---|
| **Required input** | Secrets configured; **B8 tag authorization from Product** (written); the audit-trail commit (`da0c36b` or an equivalent) on `origin/main` so the tagged tree includes `docs/team3/**`; agreement on the exact version — source is frozen at `1.0.0`, and the existing `v1.0.0` tag points at `d518218` (an old commit), so the release tag against `77414de`/`da0c36b` must be a **new** value the workflow will accept (`release.yml` requires the tag to equal `Directory.Build.props` `<VersionPrefix>`; if that stays `1.0.0`, the old `v1.0.0` tag must be moved/deleted first — a Release Engineering + Product decision — otherwise bump `<VersionPrefix>` to `1.0.1` in a separate authorized version-bump change and tag `v1.0.1`). |
| **Execution action** | Create and push the annotated tag: `git tag -a v1.0.x <sha> -m "ROJAN Reception v1.0.x"` then `git push origin v1.0.x`. The workflow triggers automatically. |
| **Evidence required** | The Actions run URL; all steps green, including "Verify tag matches project version". |
| **Completion state** | `release.yml` completes successfully. |

### 3 — Verify artifact

| | |
|---|---|
| **Required input** | The completed run's artifacts / draft Release. |
| **Execution action** | Download `ROJAN Reception Setup.exe` from the run; `signtool verify /pa` (or hand to Release Engineering for B1 step 3); `Get-FileHash -SHA256` and compare to the `.sha256` asset; confirm the exe's ProductVersion = `1.0.x` and the ZIP is present. |
| **Evidence required** | `signtool verify` = success; hash match; version string; the list of Release assets (`*.exe`, `*.sha256`, `*.zip`). |
| **Completion state (B4 DONE)** | A **signed** installer + matching checksum + ZIP are attached to a GitHub Release named `v1.0.x`; version verified `1.0.x`. |

---

## OWNER 4 — PRODUCT

### Actions
1. **Approve API environment** (B7 — with DevOps)
2. **Approve scope** (B8)
3. **Authorize release tag** (B8)
4. (Gate) **Approve release notes** (B-DOCS)

### 1 — Approve API environment (B7)

| | |
|---|---|
| **Required input** | The situation: `ApiEnvironmentService.SelectedEnvironment` defaults to `Development` (`http://localhost:8080`); production is `https://api.rojanai.ir`; `ROJAN_API_BASE_URL` overrides; a user can switch in Settings (restart-required). A fresh production install currently points at localhost. |
| **Execution action** | Choose one: **(1)** flip the Release-build default to Production — a **~5-line change in `ApiEnvironmentService` + 1 unit test**, executable by Team 3 in a future authorized phase (does not change behaviour in Debug); **(2)** force the environment choice in first-run onboarding (larger UX change); **(3)** ship as-is and document that reception staff must set the environment on first launch. Recommendation: **(1)**. |
| **Evidence required** | A written decision record: chosen option · endpoint (`https://api.rojanai.ir`) · approver name/role · reason · date. If option (1) or (2): a follow-up phase authorization for Team 3 to implement + a green test. |
| **Completion state (B7 DONE)** | Decision recorded; if code-affecting, the change is merged to `main` with a passing test and a fresh install reaches the intended API with no manual step (or the onboarding prompt appears). |

### 2 — Approve scope (B8)

| | |
|---|---|
| **Required input** | The v1.0 feature reality: Auth, Salon, Dashboard, Customers, Services, Specialists, Booking/Calendar, QR, Support, Automation — real, backend-connected. Inventory, HR, Accounting, POS — Desktop-complete but on `Fake*Repository` (no backend). |
| **Execution action** | Ratify that v1.0 ships the connected feature set and presents Inventory/HR/Accounting/POS as "coming soon" (or explicitly cut them from the v1.0 UI). Confirm B5/B6 are post-v1.0. |
| **Evidence required** | A written scope note: what ships, what is "coming soon", what is cut; approver + date. |
| **Completion state** | Published v1.0 scope note. |

### 3 — Authorize release tag (B8)

| | |
|---|---|
| **Required input** | B1 (signing credential exists), B7 (API decision), B-DOCS (release notes drafted), B3/B2 plan agreed, the Phase 8.151 TASK G sign-off checklist. |
| **Execution action** | Approve the final release checklist in writing. Authorize DevOps to create and push the `v1.0.x` tag against the agreed commit. State the version explicitly (see OWNER 3 step 2 re: the existing `v1.0.0` tag). |
| **Evidence required** | Written approval (ticket comment / signed checklist / email) naming the commit SHA and the tag string, from the Product owner. |
| **Completion state (B8 DONE)** | Written Product approval + tag authorization on record. |

### 4 — Approve release notes (B-DOCS)

| | |
|---|---|
| **Required input** | Team 3's draft `CHANGELOG.md [1.0.0]` block + refreshed `RELEASE_NOTES.md` + consolidated Known Issues (draft text in Phase 8.151 TASK F; to be applied by Team 3 in an authorized editing phase). |
| **Execution action** | Review and approve the wording; confirm the Known Issues list is acceptable for a public release (unsigned→signed status, API default, "coming soon" domains, POS re-charge, window-title inconsistency). |
| **Evidence required** | Approval comment on the docs change; confirmation that `CHANGELOG.md` no longer carries a stale `[Unreleased]` holding release content. |
| **Completion state (B-DOCS DONE)** | Dated `## [1.0.0]` in `CHANGELOG.md`; release notes current; Product approved. |

---

## ACCEPTANCE MATRIX (TASK B — consolidated)

| Owner | Gate | Required input | Execution action | Evidence required | Completion state |
|---|---|---|---|---|---|
| Release Eng | B1 | Authenticode cert (OV `.pfx` or EV token) | `publish-installer.ps1 -CertificatePath …` **or** CI (OWNER 3); then `signtool verify /pa` | cert subject/issuer/thumbprint/expiry; `signtool verify` = success + timestamped; named-publisher screenshot | Signed installer + exe + uninstaller; SmartScreen names the publisher |
| QA | B2 | Signed installer; production network; real phone for OTP; API=prod | Install → login with real OTP → observe shell + dashboard; verify silent refresh | PASS/FAIL/BLOCKED per step; dashboard screenshots; backend env noted | Full login chain PASS against production |
| QA / Release Eng | B3 | Signed installer; fresh Win10 + Win11 VMs, no .NET runtime | Install (GUI) → confirm no runtime prompt → launch → uninstall clean, both VMs | 6 checks ✅/❌ per OS; wizard/login/clean-ARP screenshots | All 6 PASS on both Windows 10 and 11 |
| DevOps | B4 | Signing secrets (from Release Eng); B8 tag auth; `da0c36b`-equiv on `main`; agreed version | Add secrets → push `v1.0.x` tag → `release.yml` runs → verify artifact | secrets list screenshot; green run URL; `signtool verify` success; hash match; asset list | Signed installer + `.sha256` + ZIP on a GitHub Release `v1.0.x`; version verified |
| Product + DevOps | B7 | Current default = localhost; prod = `api.rojanai.ir` | Choose: flip default (Team 3 ~5 LOC + test) / onboarding prompt / ship as-is + document | written decision: option + endpoint + approver + reason + date | Decision recorded; if code-affecting, merged with green test |
| Product | B8 | B1/B7/B-DOCS in progress; sign-off checklist (8.151 G) | Ratify scope; approve checklist; authorize tag (name SHA + tag string) | written scope note; written checklist approval + tag authorization | Product approval + tag authorization on record |
| Team 3 → Product | B-DOCS | Draft `[1.0.0]` text (8.151 F) | Team 3 applies in an authorized phase; Product approves wording | docs change approved; no stale `[Unreleased]` with release content | Dated `## [1.0.0]`; notes current; Product approved |
| Release Eng | Deployment | Green B4; signed assets on draft Release | Publish Release; update `ROJAN_Web` `release-registry.ts` | public Release URL; registry commit; verified download hash match | Public download live; registry points at signed `v1.0.x` + checksum |
| Team 1 | B5 (post-v1.0) | — | Publish Inventory/HR/Accounting/POS API contracts | contracts published; Desktop repos swapped from fakes; integration tests green | Domains connected (not required for v1.0) |
| Product + Backend | B6 (post-v1.0) | — | Confirm `/charge` idempotency; define POS retry UX | idempotency confirmed; retry UX spec + impl; double-charge test green | POS charge safe on retry (POS out of v1.0 scope) |

---

## WHAT TEAM 3 STILL OFFERS (only on a future phase authorization)

- Apply the `CHANGELOG.md [1.0.0]` + release-notes update (B-DOCS) — needs a commit.
- Implement the Release-build API-environment default change (B7 option 1) — ~5 LOC + 1 test.
- Push `da0c36b` to `origin/feature/team3-desktop-completion` and fast-forward `origin/main` so the audit trail is on `main` before tagging.
- Relocate phase reports 8.142–8.154 from repo root into `docs/team3/phases/`.

Team 3 will **not** perform any signing, QA, CI, deployment, or product-decision action — those are the owners' by design.
