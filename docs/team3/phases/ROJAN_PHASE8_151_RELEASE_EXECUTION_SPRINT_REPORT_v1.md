# ROJAN_PHASE8_151 — RELEASE EXECUTION SPRINT — REPORT v1

**Phase:** 8.151 · **Type:** External release gate execution · **Date:** 2026-08-29
**Mode:** STRICT — release operations only · no `.cs` / `.xaml` / `.csproj` / build-logic / behavior change · no commit · no merge · no refactor
**State at start:** `origin/main` = `77414de` · local HEAD = `da0c36b` (docs) · tracked tree clean · `src`/`tests` == `main`

---

## EXECUTIVE SUMMARY — ENVIRONMENT CANNOT CLOSE THESE GATES

This phase directs Team 3 to *execute* the six external release gates. **This is a non-interactive build/automation environment with no route to the systems each gate requires.** Every gate was attempted and is recorded below with the concrete blocker. **No gate was closed. No result was fabricated. No production release tag was pushed** (see TASK B rationale).

| Capability probed this phase | Result |
|---|---|
| Code-signing certificate (`.pfx` / EV token) | ❌ none exists |
| `signtool.exe` (Windows SDK) | ❌ not installed |
| `gh` CLI (trigger Actions / create Release / set secrets) | ❌ `command not found` |
| Repository admin (configure `CODE_SIGNING_CERT_*` secrets) | ❌ no access |
| Network egress to `https://api.rojanai.ir` | ❌ connection timed out (HTTP 000) |
| Clean Windows 10/11 VM | ❌ not available |
| Interactive session / real phone for OTP | ❌ non-interactive automation env |
| GitHub reachability | ✅ HTTP 200 (but no `gh`, and tag push is not authorized — TASK B) |
| .NET 8 SDK | ✅ `8.0.424` |

**Desktop remains GO (Phase 8.150). Production remains NO-GO. This phase does not change either verdict** — it confirms the gates are owned by, and executable only by, Release Engineering / QA / DevOps / Product.

---

## TASK A — CODE SIGNING

**Executed:** capability verification. **Outcome: NOT EXECUTABLE.**

| Sub-task | Result |
|---|---|
| Install signing certificate if available | **No certificate available.** `docs/standards/code-signing.md`: "no code-signing certificate exists in this environment, and purchasing one was explicitly out of scope." Nothing to install. |
| Configure signing secrets | **No access.** `CODE_SIGNING_CERT_BASE64` / `CODE_SIGNING_CERT_PASSWORD` are GitHub repo secrets; `gh` is absent and Team 3 has no repository-admin rights. |
| Run Authenticode signing step | **Cannot run.** `build/publish-installer.ps1 -CertificatePath …` requires (a) a `.pfx`/token — none — and (b) `signtool.exe` — not found under `C:\Program Files (x86)\Windows Kits\10\bin\**`; no Windows SDK on this machine. |
| Verify signed installer | **N/A** — no signed installer produced. |

**Records:**
- **Certificate status:** NOT PROCURED. Type required: Authenticode code-signing cert (OV or EV; `code-signing.md` recommends EV for immediate SmartScreen trust). Owner: Release Engineering / budget owner.
- **Signature verification result:** N/A (no signature). Current installer is **unsigned** → SmartScreen "Unknown Publisher" on first run.
- **Artifact hash after signing:** N/A. Unsigned installer hash (unchanged since Phase 8.144): `69cb1f29d9d92541da8c68f926c96fbe3610f811bf95663ff532152713097615`.

**Ready when:** a certificate + the two CI secrets exist → `release.yml` signs automatically, or `pwsh build/publish-installer.ps1 -CertificatePath <pfx> -CertificatePassword <pw>` locally on a machine with the Windows SDK. No code change.

---

## TASK B — RELEASE PIPELINE

**Executed:** trigger analysis + pre-conditions check. **Outcome: NOT EXECUTED — deliberately not triggered.**

`.github/workflows/release.yml` triggers **only** on a `push` of a `v*` tag. Running it means pushing a tag to `origin`. Team 3 did **not** do this, for six reasons — any one of which is disqualifying:

1. **Product sign-off is explicitly still pending** (this phase's own TASK G ends at "Product: ⬜ Pending approval"). Pushing a release tag *is* the release; doing it before its own gate is contradictory.
2. **Tag conflict.** `v1.0.0` already exists (locally at `d518218`, annotated-tag object `d00c44e`) — **not** on `77414de`. The workflow verifies the tag matches `Directory.Build.props` version *exactly*; source is frozen at `1.0.0`, so the only valid tag is `v1.0.0`. Re-pointing / force-pushing an existing release tag is destructive and was not authorized.
3. **The build would be unsigned** (TASK A) — defeating the pipeline's main remaining purpose.
4. **Unverifiable from here.** No `gh` to watch the run; no network route to confirm the published artifact or the `api.rojanai.ir` release-registry sync.
5. **CI signing secrets are unset** and Team 3 cannot set them.
6. The phase's own rules forbid "merge branches"; the docs commit `da0c36b` is also not on `origin/main`, so a tag on `77414de` would omit the audit trail, and a tag on `da0c36b` would require pushing that commit first.

**Records (what the pipeline *would* verify, confirmed by local dry equivalents in Phases 8.143–8.148):**
| Check | Expected | Local evidence |
|---|---|---|
| Build succeeds | ✅ | Release build 0 warn / 0 err (8.141, 8.145) |
| Publish succeeds | ✅ | `publish/Rojan.Desktop.Shell.exe` self-contained single-file produced (8.144, 8.148) |
| Installer artifact generated | ✅ | `ROJAN Reception Setup.exe` 54,057,848 B (8.144) |
| Hash generated | ✅ | `release.yml` step recomputes `Get-FileHash -SHA256` → `.sha256` sidecar |
| Version = 1.0.0 | ✅ | `get-version.ps1` → `Directory.Build.props` `<VersionPrefix>1.0.0</VersionPrefix>`; workflow rejects a mismatched tag |

**Ready when:** Product authorizes a tag; DevOps sets the signing secrets; `da0c36b` (or an equivalent) is on `main`; then `git tag -a v1.0.x <sha> && git push origin v1.0.x`. Owner: DevOps + Product.

---

## TASK C — CLEAN VM INSTALLATION

**Executed:** attempted. **Outcome: NOT EXECUTABLE — no clean VM.**

| Sub-task | Result |
|---|---|
| Installer launches / installs / shortcut / app starts / uninstall | **Cannot test on a clean VM** — none available in this environment. |

**Record: NOT TESTABLE (this environment).**
Best available evidence — Phase 8.144, on the **build machine** (not clean): silent install exit 0 · files + 4 language packs + `unins000.exe` · Start-Menu shortcuts created · Desktop shortcut correctly absent · ARP entry correct · **launch reached the login screen** · silent uninstall exit 0, all traces removed. The self-contained single-file `win-x64` payload means no ".NET runtime required" prompt is *expected* on a bare host — **unverified**.

**Ready when:** QA / Release Engineering runs `ROJAN Reception Setup.exe` on a fresh Windows 10 and Windows 11 VM with no .NET runtime/SDK. Runbook: `docs/team3/phases/…Production_Checklist.md` §8. Owner: QA / Release Engineering.

---

## TASK D — LIVE LOGIN VALIDATION

**Executed:** network probe. **Outcome: BLOCKED.**

| Sub-task | Result |
|---|---|
| Network egress to `https://api.rojanai.ir` | ❌ **connection timed out after 15 s (HTTP 000)** — no route from this environment |
| Application startup / API connection / OTP flow / session / shell load | **Cannot execute** — no backend route, non-interactive env, no real phone number for OTP SMS |

**Record: BLOCKED (no network route + non-interactive environment).**
Structural evidence: the login screen renders on a fresh install (Phase 8.144); the OTP/refresh endpoint contracts were confirmed by prior read-only probes (Productionization Sprint 2, pre-Team-3); 772 Presentation + 609 Infrastructure tests cover the auth ViewModels and `HttpApiClient` paths. The real OTP → sign-in → shell-with-real-data round-trip is **unproven**.

**Ready when:** QA runs the app from a network with backend access, signs in with a real phone number + OTP, confirms the dashboard loads real data. Owner: QA.

---

## TASK E — API ENVIRONMENT DECISION

**Executed:** cannot be decided by Team 3. **Outcome: DECISION PENDING (Product + DevOps).**

| Field | Value |
|---|---|
| **Current Release behavior** | `ApiEnvironmentService.SelectedEnvironment` defaults to `ApiEnvironment.Development` → `http://localhost:8080` |
| **Production endpoint available** | `ApiEnvironmentService.ProductionUrlDefault = "https://api.rojanai.ir"` |
| **Override** | `ROJAN_API_BASE_URL` env var supersedes both; user can switch in Settings (restart-required) |
| **Recommended decision** | **Production API** — `https://api.rojanai.ir` as the Release-build default (a fresh install should reach production without user action) |
| **Owner approval** | ⬜ **NOT GIVEN** — this requires Product + DevOps sign-off; Team 3 has no authority to record an approval |
| **Reason** | A production installer that defaults to `localhost:8080` cannot function out of the box for an end user. Options: (1) flip the default for Release builds — **a ~5-line change + 1 test, a small Team 3 follow-up if authorized in a future phase**; (2) force the choice in first-run onboarding; (3) ship as-is and document. Option (1) recommended. |

**Records:** Endpoint = `https://api.rojanai.ir` (proposed). Owner approval = pending. Reason = as above. **No configuration was changed this phase** (source frozen).

---

## TASK F — RELEASE DOCUMENTS

**Executed:** review only — **not finalized** (finalizing requires editing tracked `.md` files and committing; this phase forbids commits, and Phase 8.143 established that dirtying tracked files in a no-commit phase is out of bounds).

| Document | Current state | Needed |
|---|---|---|
| `CHANGELOG.md` | `## [Unreleased]` section present; last real edit `56dd2ed` (2026-08-21). **Stale "Unreleased" marker.** No `## [1.0.0]` entry for the Team 3 hardening. | Convert `[Unreleased]` → `## [1.0.0] - <release date>`; add a "Security / Fixed" block for the hardening engagement (error-surface sanitization 58/58, missing-guard sweep, diagnostic-logging harmonization, nav back-stack bound, Settings UX fix, fork supersession). |
| `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` | Dated 2026-08-21; "Code signing 🟡 Hooks ready, unsigned"; no mention of Team 3 hardening. | Update date; add a "Reliability & Security Hardening (Team 3)" section; refresh the Production-Readiness table; keep the signing row until B1 closes. |
| Known Issues | Scattered across `RELEASE_NOTES.md` "Known Limitations" + blocker reports. | Consolidate: unsigned installer (SmartScreen), first-launch API default = localhost, Inventory/HR/Accounting/POS = "coming soon", POS re-charge after failed payment, window-title inconsistency. |

**Draft text for the `CHANGELOG.md [1.0.0]` block** (for an authorized editing phase — not applied):

```markdown
## [1.0.0] - 2026-08-DD

### Security
- Sanitized all 58 user-facing error surfaces (30 ViewModels): backend
  response bodies, stack traces, internal URLs, PII, financial KPIs,
  AI prompts, automation payloads, and invite tokens no longer reach any
  UI element — replaced with a generic localized message. 7 confirmed
  live data-exposure paths closed.
- ViewModel diagnostic logging harmonized to operation-name-only across
  all 35 [LoggerMessage] templates; no ViewModel logs an exception object.

### Fixed
- Every backend-connected, user-triggered command is now wrapped in a
  guard that surfaces a safe error state instead of crashing (missing-
  guard sweep, all modules).
- Navigation back-stack bounded at 20 entries (was unbounded).
- Settings restart-required feedback now renders (was invisible).

### Changed
- main reconciled with the superseded Service-Catalog + Shift-Engine fork
  via an `-s ours` merge (77414de); no code regression.
```

**Verification of "no stale Unreleased markers":** ❌ **NOT MET** — `CHANGELOG.md` still has `## [Unreleased]`. Closing this is a documentation edit + commit, owned by Team 3 (draft) → Product (approve), to be done in an authorized phase.

---

## TASK G — PRODUCT SIGN-OFF CHECKLIST

| Area | Status | Basis |
|---|---|---|
| **Desktop** | ✅ Complete | Code frozen on `main` `77414de`; 2,715/2,715 tests (Debug+Release); architecture 7/7; 0/0 build; no P0; 58/58 error surfaces sanitized |
| **Installer** | ✅ Complete (unsigned) | `ROJAN Reception Setup.exe` built, versioned 1.0.0, install/first-run/uninstall validated on build machine (8.144) |
| **Pipeline** | 🟡 Wired, not run | `release.yml` complete and locally dry-verified; never executed against a tag; CI signing secrets unset |
| **QA** | 🟡 Partial | Structural: 2,715 tests + build-machine install. Missing: live OTP login, clean-VM install |
| **Signing** | ⬜ Pending | No certificate |
| **Release docs** | ⬜ Pending | `CHANGELOG` still `[Unreleased]`; release notes stale |
| **Product** | ⬜ **Pending approval** | Requires: (1) v1.0 scope ratification (Inventory/HR/Accounting/POS as "coming soon"); (2) API-environment default decision (TASK E); (3) authorization to cut `v1.0.x` |

**Gate to Product approval:** Signing (B1), Pipeline first run (B4), QA live+clean-VM (B2/B3), Release docs, and the API-env decision (B7) must land first. Team 3 owns only the Release-docs draft and (if authorized) the API-env code change.

---

## TASK H — FINAL REPORT

### H.A — Gate execution timeline (this phase)

| Time (approx) | Action | Result |
|---|---|---|
| T0 | Probe release-ops capability (`gh`, `signtool`, SDK, remotes, tags) | `gh` absent · `signtool` absent · SDK 8.0.424 · 1 remote · `v1.0.0` exists at `d518218` |
| T1 | TASK A — locate certificate / signtool / secret access | None available → NOT EXECUTABLE |
| T2 | TASK B — analyze `release.yml` trigger + pre-conditions | 6 disqualifiers → NOT TRIGGERED (no tag pushed) |
| T3 | TASK C — locate clean VM | None → NOT TESTABLE |
| T4 | TASK D — probe `https://api.rojanai.ir` | Connection timed out (HTTP 000) → BLOCKED |
| T5 | TASK E — API-environment decision | Requires Product+DevOps → PENDING (proposal recorded) |
| T6 | TASK F — review release docs | `[Unreleased]` stale; drafts prepared; not applied (no-commit phase) |
| T7 | TASK G — assemble sign-off checklist | Desktop/Installer ✅; Pipeline/QA 🟡; Signing/Docs/Product ⬜ |
| T8 | TASK H — write this report | Done |
| — | Source / commits / branches / config | **UNCHANGED** |

### H.B — Owner matrix

| Gate | Owner | Team 3 contribution | Status |
|---|---|---|---|
| B1 Code signing | Release Engineering / budget owner | Hooks + scripts + `.iss` + `release.yml` wiring (done) | ⏳ PENDING (certificate) |
| CI signing secrets | DevOps | Secret names documented (`code-signing.md`) | ⏳ PENDING |
| B4 Pipeline first run | DevOps | `release.yml` authored + locally dry-verified | ⏳ PENDING (tag + secrets) |
| B3 Clean-VM install | QA / Release Engineering | Installer + runbook (done) | ⏳ PENDING |
| B2 Live OTP login | QA | Auth ViewModels + 1,381 supporting tests (done) | ⏳ PENDING |
| B7 API-environment default | Product + DevOps | Proposal + (if authorized) the ~5-line change | ⏳ PENDING (decision) |
| Release docs / CHANGELOG | Team 3 draft → Product approve | Draft text in TASK F (done) | ⏳ PENDING (authorized edit phase) |
| B8 Product sign-off + tag auth | Product | Sign-off checklist (done) | ⏳ PENDING |
| B5 Inventory/HR/Accounting/POS contracts | Team 1 | Desktop layers + fakes + tests (done) | ⏳ PENDING (not v1.0-blocking) |
| B6 POS payment-idempotency | Product + Backend | — | ⏳ PENDING (POS out of v1.0 scope) |

### H.C — Passed gates

**This phase: 0 external gates closed** (environment cannot reach any of the required systems).

**Already green entering this phase (Team 3, Phase 8.150):**
- ✅ Desktop code complete + merged to `main` (`77414de`)
- ✅ Debug + Release build 0/0
- ✅ 2,715/2,715 tests (both configs, 0 skipped)
- ✅ Architecture 7/7
- ✅ Installer built + install/first-run/uninstall validated (build machine)
- ✅ Artifact package reproducible, version 1.0.0 single-sourced
- ✅ Signing toolchain wired + unsigned fallback proven
- ✅ Audit trail committed (`da0c36b`)

### H.D — Remaining blockers

| # | Blocker | Owner | Blocks distribution? |
|---|---|---|---|
| B1 | Code-signing certificate | Release Engineering | **Yes** |
| B2 | Live backend OTP login validation | QA | **Yes** |
| B3 | Clean-VM installation test | QA / Release Engineering | **Yes** |
| B4 | `release.yml` first real run (+ CI secrets) | DevOps | **Yes** |
| B7 | Production API-environment default decision | Product + DevOps | **Yes** |
| B8 | Product v1.0 scope sign-off + tag authorization | Product | **Yes** |
| — | Release notes / CHANGELOG `[1.0.0]` finalization | Team 3 → Product | **Yes** (process) |
| B5 | Inventory/HR/Accounting/POS backend contracts | Team 1 | No (out of v1.0 scope) |
| B6 | POS payment-idempotency | Product + Backend | No (POS out of v1.0 scope) |

### H.E — Final recommendation

# NOT READY (for distribution)

**Reason:** Not one of the six distribution-blocking external gates could be executed from this environment — there is no certificate, no Windows SDK, no repo-admin access, no clean VM, no network route to the backend, and no authority to push a production release tag or record a Product approval. This phase confirmed, concretely, that **these gates are executable only by their named owners (Release Engineering, QA, DevOps, Product)** on infrastructure Team 3 does not have.

**Desktop remains GO** (unchanged from Phase 8.150) — the application, installer, tests, architecture, and artifact package are complete and verified, with no P0 defect.

**Path to READY FOR DISTRIBUTION:**
1. Release Engineering procures an Authenticode (EV recommended) certificate → B1.
2. DevOps sets `CODE_SIGNING_CERT_BASE64` + `CODE_SIGNING_CERT_PASSWORD` secrets.
3. Team 3 (authorized phase) drafts the `CHANGELOG [1.0.0]` + release-notes update; Product approves. Optionally applies the API-env default change (B7 option 1).
4. Product ratifies v1.0 scope + authorizes the tag → B8.
5. Ensure the audit-trail commit (`da0c36b` or equivalent) is on `main`; `git push origin v1.0.x` → `release.yml` runs, produces a **signed** installer + checksum + GitHub Release → B4.
6. QA installs that signed installer on clean Win10/Win11 VMs → B3; and runs a real OTP login → dashboard against the production backend → B2.
7. Release Engineering syncs `ROJAN_Web`'s release registry.

When 1–7 are green, the release is **READY FOR DISTRIBUTION**. No further Desktop code work is anticipated.

---

## VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / `.csproj` / build-logic / behavior changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits created | ❌ none |
| Branches merged | ❌ none |
| Tags pushed | ❌ none |
| Tracked working tree | 0 dirty |
| Files created this phase | `ROJAN_PHASE8_151_RELEASE_EXECUTION_SPRINT_REPORT_v1.md` (untracked, repo root) |

**Release operation review only. No source, commit, branch, or config change. Confirmed.**

---

**STOP.** Awaiting PHASE 8.152 authorization.
