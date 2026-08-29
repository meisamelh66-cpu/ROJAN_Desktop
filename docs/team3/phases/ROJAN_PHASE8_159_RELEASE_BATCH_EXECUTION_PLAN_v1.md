# ROJAN Reception v1.0 — RELEASE BATCH EXECUTION PLAN v1

| | |
|---|---|
| **Release** | ROJAN Reception v1.0 |
| **Main** | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) · version `1.0.0` (frozen) |
| **Audit-trail commit** | `da0c36b` (branch; `docs/team3/**`; not on `main`) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Plan created** | 2026-08-29 · Phase 8.159 · by Team 3 |
| **Plan owner** | Release Engineering |
| **Companions** | 8.153 board · 8.154 owner actions · 8.155 checklist · 8.156 war-room log · 8.157 execution session · 8.158 response intake + status model |

> The seven blocking gates are batched into **3 sequential passes**. A pass is COMPLETE only when every gate in it is PASS (per the Phase 8.158 status model). Passes run in order: PASS 1 → PASS 2 → PASS 3 → Final Release. Team 3 executes none of these (no certificate, no backend route, no clean VM, no CI access, no product authority — Phase 8.151); it authored the plan.

---

## PASS 1 — RELEASE APPROVAL PACKAGE

**Combined gates:** B1 (Code Signing) · B7 (API Environment Decision) · B8 (Product Sign-off + Tag Authorization) · B-DOCS (Release Notes Approval)

| Gate | Owner | Can start | Depends on (within pass) |
|---|---|---|---|
| **B1 — Code Signing** | Release Engineering | Now | — |
| **B7 — API Environment Decision** | Product + DevOps | Now (parallel with B1) | — |
| **B-DOCS — Release Notes** | Team 3 (draft/apply, authorized phase) → Product (approve) | Now (draft ready, Phase 8.151 TASK F) | — |
| **B8 — Product Sign-off + Tag Auth** | Product | After B1 credential exists + B7 decided + B-DOCS approved | B1, B7, B-DOCS |

### Required inputs

| Item | Detail | Owner |
|---|---|---|
| Certificate availability | Authenticate code-signing cert (EV recommended); Microsoft-trusted CA; ROJAN legal entity; delivered as CI secret and/or `.pfx` on an SDK machine | Release Engineering |
| Signature verification plan | `signtool verify /pa /v` on the installer + confirm payload exe + uninstaller signed + timestamp present + trusted chain; first-run named-publisher check on a non-dev machine | Release Engineering |
| Production API decision | Choose: Option 1 flip Release default to `https://api.rojanai.ir` (~5 LOC + 1 test, Team 3 follow-up) / Option 2 onboarding prompt / Option 3 ship-as-is + document. Record endpoint + approver + reason. | Product + DevOps |
| Product approval | Ratify v1.0 scope (Inv/HR/Acct/POS "coming soon" or cut); sign off the Phase 8.151 TASK G checklist; authorize the release tag (naming commit SHA + tag string) | Product |
| Final release notes | Apply the `CHANGELOG.md [1.0.0]` block + refresh `RELEASE_NOTES.md` + consolidate Known Issues; Product approves wording | Team 3 → Product |

### Exit condition — **PASS 1 COMPLETE** when

- B1 = PASS (signature verified on a locally-signed build **or** the signing credential is confirmed available to CI and dry-verified)
- B7 = PASS (decision recorded; if code-affecting, the follow-up phase is authorized/merged with a green test)
- B-DOCS = PASS (`CHANGELOG.md` dated `## [1.0.0]`, no stale `[Unreleased]`, Product-approved)
- B8 = PASS (written Product approval + written tag authorization on record)

---

## PASS 2 — PRODUCTION BUILD PIPELINE

**Gate:** B4 (`release.yml` Pipeline Run)

**Dependencies:** B1 = PASS · B8 = PASS · (also: `da0c36b`-equivalent on `main`; version reconciliation done)

| Required input | Detail | Owner |
|---|---|---|
| Approved tag / version | From B8: commit SHA + tag string. Existing `v1.0.0` → old `d518218`; resolve by moving the tag or bumping `<VersionPrefix>` to `1.0.1` (separate authorized change) — `release.yml` requires tag == `Directory.Build.props` version | Product (auth) + DevOps (execute) |
| CI secrets configured | `CODE_SIGNING_CERT_BASE64` + `CODE_SIGNING_CERT_PASSWORD` (or EV/HSM workflow swap) | DevOps |
| Signing enabled | The workflow's signing step activates when the secrets are present | DevOps |
| Pipeline green | `git tag -a v1.0.x <sha> && git push origin v1.0.x` → workflow runs; all steps green incl. tag-vs-version check | DevOps |

### Evidence

- Workflow result: Actions run URL, all steps green
- Signed artifact: `ROJAN Reception Setup.exe` — `signtool verify /pa` = success, timestamped
- Release artifact checksum: `.sha256` asset matches `Get-FileHash -SHA256` of the downloaded installer; ZIP present; exe ProductVersion = `1.0.x`

### Exit condition — **PASS 2 COMPLETE** when

B4 = PASS: `release.yml` completed green on `v1.0.x`; a **signed** installer + matching `.sha256` + ZIP are attached to a GitHub Release named `v1.0.x`; version verified.

---

## PASS 3 — QA ACCEPTANCE BATCH

**Combined gates:** B2 (Live Login Test) + B3 (Clean VM Install)

**Dependencies:** B1 = PASS · B4 = PASS · B7 = PASS (for B2's production API)

| Environment | Detail |
|---|---|
| Windows 10 clean VM | Fresh, x64, no .NET runtime/SDK, no prior ROJAN install |
| Windows 11 clean VM | Same |
| Network | Outbound access to `https://api.rojanai.ir` (for B2) |

### Required

- Signed installer (from PASS 2)
- Production API reachable
- OTP login with a real phone number
- Install (GUI wizard, per-user, no runtime prompt)
- Launch (Start-Menu shortcut → login screen)
- Login (OTP → session → main shell → real dashboard data)
- Uninstall verification (all traces removed: install dir, Start-Menu folder, ARP entry, `%LocalAppData%\RojanDesktop`)

### Evidence

- Screenshots: wizard, running login screen, dashboard with real data, clean ARP post-uninstall (per OS)
- Logs: app file log excerpt showing successful API round-trip; backend build/env noted
- QA approval: signed result per gate — B2 PASS/FAIL, B3 PASS/FAIL (both OS)

### Exit condition — **PASS 3 COMPLETE** when

- B2 = PASS (full login chain PASS against production with a real OTP)
- B3 = PASS (6/6 install checks PASS on **both** Windows 10 and Windows 11)

---

## TASK B — DEPENDENCY GRAPH

```
B1 (Signing) ──┬──────────────► B4 (Pipeline)
               ├──────────────► B2 (Live Login)
               └──────────────► B3 (Clean VM)

B7 (API Env) ──┬──────────────► B2 (Live Login)
               └──────────────► B8 (Product Sign-off)   [API decision feeds sign-off]

B-DOCS ────────┬──────────────► B8 (Product Sign-off)   [notes approved before tag auth]
               └──────────────► Final Release

B8 (Sign-off) ─┬──────────────► B4 (Pipeline)            [tag authorization]

B4 (Pipeline) ─┬──────────────► B2 (Live Login)          [produces the signed build]
               └──────────────► B3 (Clean VM)

B2 + B3 ───────────────────────► Final Release
```

### Resolved to passes

```
PASS 1:  B1 ∥ B7 ∥ B-DOCS  ──►  B8
                 │
                 ▼
PASS 2:  B4   (needs B1 PASS + B8 PASS)
                 │
                 ▼
PASS 3:  B2 ∥ B3   (need B1 PASS + B4 PASS; B2 also needs B7 PASS)
                 │
                 ▼
FINAL RELEASE:  Production deployment
                (needs PASS 1 + PASS 2 + PASS 3 all COMPLETE)
```

### Critical path (longest chain)

`B1 → B8 → B4 → B2 (or B3) → Final Release`
Parallel tracks that must also finish: `B7 → B8`, `B-DOCS → B8`, `B7 → B2`.

---

## TASK C — FINAL GO CONDITION

### ✅ GO

**PASS 1 COMPLETE ∧ PASS 2 COMPLETE ∧ PASS 3 COMPLETE**, i.e. all seven blocking gates = PASS:

| | |
|---|---|
| PASS 1 | B1 ∧ B7 ∧ B-DOCS ∧ B8 all PASS |
| PASS 2 | B4 PASS (signed artifact on a `v1.0.x` GitHub Release, checksum verified) |
| PASS 3 | B2 PASS ∧ B3 PASS (both OS) |
| Baseline | Desktop still READY on the tagged commit — suite 2,715/2,715, architecture 7/7, 0 P0 |

→ **GO → Production deployment** (publish the Release publicly + sync `ROJAN_Web` `release-registry.ts` with the signed installer URL + SHA-256).

### ⛔ NO-GO

**Any** blocking gate FAIL / BLOCKED / WAITING / IN PROGRESS, or:

- Signature missing / untrusted / un-timestamped, or "Unknown Publisher" persists (B1)
- API-environment undecided, or a fresh production install can't reach the API and shows no prompt (B7)
- No Product approval or no written tag authorization (B8)
- `CHANGELOG.md` still holds `[Unreleased]` release content, or notes unapproved (B-DOCS)
- `release.yml` red, tag-vs-version check fails, or the produced installer is unsigned / hash-mismatched (B4)
- Login chain fails at any step, or the dashboard shows no real data (B2)
- Any of the 6 install checks fails on either OS, or a ".NET runtime required" prompt appears (B3)
- A new P0 defect on the tagged commit, or the suite is not 2,715/2,715

→ **NO-GO → fix the failing gate, re-run its exit check, re-evaluate the affected pass.**

---

## STATE AT PLAN CREATION (Phase 8.159)

```
Release:  ROJAN Reception v1.0
Main:     77414de   (1.0.0, frozen)
Desktop:  READY ✅

PASS 1 — RELEASE APPROVAL PACKAGE
   B1  Signing            WAITING     Release Engineering
   B7  API Environment    WAITING     Product + DevOps
   BD  Release Notes      WAITING     Team 3 → Product
   B8  Product Sign-off   BLOCKED     (needs B1, B7, B-DOCS)
   → PASS 1: NOT STARTED

PASS 2 — PRODUCTION BUILD PIPELINE
   B4  Pipeline           BLOCKED     (needs B1, B8)
   → PASS 2: NOT STARTED

PASS 3 — QA ACCEPTANCE BATCH
   B2  Live Login         BLOCKED     (needs B1, B4, B7)
   B3  Clean VM           BLOCKED     (needs B1, B4)
   → PASS 3: NOT STARTED

Blocking gates PASS: 0 / 7
DECISION:            NO-GO
```
