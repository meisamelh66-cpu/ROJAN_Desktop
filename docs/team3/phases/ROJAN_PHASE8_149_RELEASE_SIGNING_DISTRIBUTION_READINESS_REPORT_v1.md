# ROJAN_PHASE8_149 — RELEASE SIGNING & DISTRIBUTION READINESS REVIEW — REPORT v1

**Phase:** 8.149 · **Type:** Final distribution readiness · **Date:** 2026-08-29
**Mode:** STRICT — documentation only · no source change · no commit · no branch change
**State:** `origin/main` = `77414de` · local HEAD = `da0c36b` (docs; `src`/`tests` == main) · tracked tree clean

---

## TASK A — SIGNING REVIEW

### A.1 Installer signing hooks — **READY (fully wired, unexercised)**

| Component | Mechanism | Status |
|---|---|---|
| **Payload exe signing** | `build/publish-installer.ps1` — optional `-CertificatePath` / `-CertificatePassword` / `-TimestampUrl` (default `http://timestamp.digicert.com`) / `-SignToolPath`. When `-CertificatePath` is supplied: `signtool sign /f <pfx> /p <pw> /tr <ts> /td sha256 /fd sha256 <exe>` runs on the published `Rojan.Desktop.Shell.exe` before packaging (line 126). | ✅ implemented |
| **Installer + uninstaller signing** | Same script passes `/DSignInstaller=1` + `/Ssigntool="…"` to `ISCC.exe` (lines 144–145). `build/installer/RojanReception.iss` lines 67–69: `#ifdef SignInstaller` → `SignTool=signtool` + `SignedUninstaller=yes`. | ✅ implemented |
| **Unsigned fallback** | Omitting `-CertificatePath` ⇒ `$signingRequested = $false` ⇒ zero signing code runs; build byte-identical to every prior unsigned run. `#ifdef SignInstaller` inactive. | ✅ proven (Phases 8.144, 8.148) |
| **Timestamping** | RFC 3161 via `/tr … /td sha256` — signature survives certificate expiry. | ✅ built in |

### A.2 Certificate requirement — **PENDING (procurement)**

Per `docs/standards/code-signing.md`:

- **Type needed:** Authenticode code-signing certificate (Windows desktop `.exe`) — **not** a TLS cert.
- **OV vs EV:** OV = lower cost, SmartScreen reputation builds over weeks of installs. **EV = immediate SmartScreen trust**, but private key must live on a hardware token (USB HSM) — cannot be a plain `.pfx`, which changes the CI approach (runner needs the token attached, or the CA's cloud-HSM signing service).
- **Doc recommendation:** EV for a salon-facing product where first-run trust matters — **but this is a purchasing decision for the budget owner, not Team 3.**
- **Issuer:** a CA on Microsoft's trusted list (DigiCert, Sectigo, SSL.com, GlobalSign), in the name of the legal entity behind ROJAN. Identity-verified — needs business documentation, typically a few business days.
- **Current state:** no certificate purchased. `ROJAN Reception Setup.exe` is unsigned → SmartScreen "Windows protected your PC" / "Unknown Publisher" on first run.

### A.3 signtool availability — **NOT PRESENT in this environment**

- `C:\Program Files (x86)\Windows Kits\10\bin\**\signtool.exe` — **not found** (no Windows SDK installed on this machine).
- `publish-installer.ps1` auto-detects it under the SDK path, or accepts `-SignToolPath`; throws a clear error if absent (line 111).
- **Impact:** none on the unsigned build. A signing run requires the Windows SDK (or `-SignToolPath`) on whichever machine/runner performs it. On GitHub-hosted `windows-latest` runners, signtool is preinstalled — so the CI path needs no extra tooling, only the secrets.

### A.4 Signing pipeline readiness — **READY (wired, needs 2 secrets)**

`.github/workflows/release.yml`:

- Triggers on `tags: v*` push only (deliberate — a tag is a reviewed manual act, not CI-decided).
- **Verifies the tag matches `Directory.Build.props` version** — a mismatched tag never produces a release.
- Signing step (lines 78–104): reads `secrets.CODE_SIGNING_CERT_BASE64` + `secrets.CODE_SIGNING_CERT_PASSWORD`; if `CODE_SIGNING_CERT_BASE64` is set, base64-decodes it to a `.pfx` in `RUNNER_TEMP`, passes `-CertificatePath`/`-CertificatePassword` to `publish-installer.ps1`, then deletes the `.pfx`. Absent secrets ⇒ unsigned build, identical behaviour.
- Post-build: generates `artifacts/ROJAN Reception Setup.exe.sha256`, uploads `*.zip *.exe *.sha256`, `gh release create <tag> … --generate-notes`, then a **manual** cross-repo step to update `ROJAN_Web`'s release registry.

**Only missing input:** the two repository secrets (and, for an EV/HSM cert, a different signing invocation — the base64-`.pfx` path assumes an OV/file cert).

---

## TASK B — DISTRIBUTION PACKAGE

| Item | Status | Detail |
|---|---|---|
| **Installer artifact** | ✅ present, unsigned | `artifacts/ROJAN Reception Setup.exe` — 54,057,848 bytes — install/uninstall/launch-to-login validated (8.144) |
| **Hash files** | ⚠️ partial | Installer: `ROJAN Reception Setup.exe.sha256` present — `69cb1f29d9d92541da8c68f926c96fbe3610f811bf95663ff532152713097615`. ZIP: **no sidecar locally** (`RojanDesktop-v1.0.0-win-x64.zip` — SHA-256 `e6a75e0ba406d6baececa581d0df39ea094f3d611623b5f2fdf884e457e0eb14`, computed Phase 8.148). CI `release.yml` generates the installer sidecar fresh against the published artifact. |
| **Version metadata** | ✅ consistent | Payload exe: ProductName `ROJAN Reception`, ProductVersion `1.0.0+da0c36b…`, FileVersion `1.0.0.0`, Company `ROJAN`. Installer: ProductName `ROJAN Reception`, ProductVersion `1.0.0`. Single-sourced from `Directory.Build.props` `<VersionPrefix>1.0.0</VersionPrefix>`. |
| **Release notes readiness** | ⚠️ stale | `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` exists (dated 2026-08-21, from Productionization Sprint 2) and `CHANGELOG.md` `[Unreleased]` section exists — **but neither reflects the Team 3 hardening engagement** (P2 error-surface sanitization, missing-guard sweep, diagnostic-logging harmonization, nav back-stack bound, Settings UX fix, fork supersession merge). `CHANGELOG.md` last touched `56dd2ed` (2026-08-21) — no `[1.0.0]` entry cut for the current `main`. `gh release create --generate-notes` would auto-generate from commit titles, which does cover the Team 3 `fix(desktop): …` commits. |

---

## TASK C — OWNERSHIP HANDOFF

| Item | Owner | Status |
|---|---|---|
| **Signing — certificate procurement** | Release Engineering / budget owner | ⏳ PENDING — no cert; OV-vs-EV decision open (`code-signing.md` recommends EV) |
| **Signing — CI secrets** (`CODE_SIGNING_CERT_BASE64`, `CODE_SIGNING_CERT_PASSWORD`) | DevOps | ⏳ PENDING — depends on cert; EV/HSM cert needs a different CI invocation than the base64-`.pfx` path |
| **Signing — hooks / scripts / `.iss` / `release.yml` wiring** | Team 3 | ✅ COMPLETE — parameter, not redesign |
| **QA validation — live backend OTP login → dashboard** | QA | ⏳ PENDING — needs real phone + authenticated session |
| **QA validation — clean Windows 10/11 VM install** | QA / Release Engineering | ⏳ PENDING — validated on build machine only |
| **Pipeline — `release.yml` first real run via a `v*` tag** | DevOps | ⏳ PENDING — never executed; script chain verified locally (8.143–8.144) |
| **Pipeline — `ROJAN_Web` release-registry sync** (manual cross-repo) | Release Engineering | ⏳ PENDING — post-release manual step |
| **Backend readiness — Inventory / HR / Accounting / POS contracts** | Team 1 | ⏳ PENDING — not v1.0-blocking (Desktop on `Fake*Repository`, "coming soon" scope viable) |
| **Product approval — first-launch API-environment default** | Product + DevOps | ⏳ PENDING — defaults to `localhost:8080`; decision needed before a production build |
| **Product approval — v1.0 scope sign-off & tag authorization** | Product | ⏳ PENDING — authorize cutting `v1.0.x` against `main` |
| **Release notes / CHANGELOG `[1.0.0]` entry for Team 3 work** | Team 3 (draft) → Product (approve) | ⏳ PENDING — optional Team 3 follow-up; `--generate-notes` is a fallback |
| **Desktop application code / UI / installer generation / validation / audit trail** | Team 3 | ✅ COMPLETE (`main` `77414de`; docs `da0c36b`) |

---

## TASK D — FINAL RELEASE STATE

### READY FOR SIGNING — **YES** ✅

Everything a signing operation consumes is in place: `publish-installer.ps1` signing parameters, `.iss` `SignInstaller` gate, `release.yml` secret-driven signing step, timestamping default, unsigned-fallback proven. The moment an Authenticode `.pfx` (or EV token + adjusted invocation) and the two CI secrets exist, signing is a single parameterized run — no code or packaging change.
*Caveat:* an **EV/HSM** certificate needs a one-line change to how the CI step invokes signing (cloud-HSM or attached-token instead of base64-`.pfx`) — a DevOps config change, not a Team 3 code change.

### READY FOR DISTRIBUTION — **NO / BLOCKED** ⛔

Blocked on external gates, none a Desktop code defect:

| Blocker | Owner |
|---|---|
| Installer unsigned (no certificate) → SmartScreen "Unknown Publisher" | Release Engineering |
| Live backend OTP login → dashboard never validated | QA |
| Clean-VM install never validated | QA / Release Engineering |
| `release.yml` never run — no reproducible audited release build, no GitHub Release | DevOps |
| First-launch API-environment default points at `localhost:8080` | Product + DevOps |
| Product v1.0 scope sign-off + tag authorization | Product |
| Release notes / CHANGELOG not updated for Team 3 work | Team 3 draft → Product |

### Overall

| Dimension | State |
|---|---|
| **Desktop application** | ✅ READY (complete, on `main`, 2,715/2,715, 7/7, 0/0, artifacts built + validated) |
| **Ready for signing** | ✅ YES (hooks + pipeline wired; needs certificate + secrets) |
| **Ready for distribution** | ⛔ BLOCKED (7 external gates — signing, live login, clean VM, pipeline run, API default, product sign-off, release notes) |

---

## TASK E — VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / project / build-script changed | ❌ none |
| Commits created | ❌ none |
| Branch changed | ❌ none |
| Files created this phase | `ROJAN_PHASE8_149_RELEASE_SIGNING_DISTRIBUTION_READINESS_REPORT_v1.md` (untracked, repo root) |

**Documentation only. Confirmed.**

---

## OUTCOME

**Ready for signing: YES.** The signing toolchain (scripts, `.iss` gate, `release.yml` secret path, timestamping, unsigned fallback) is fully implemented and verified inert — a certificate turns it on with no redesign. `signtool.exe` is absent from this machine but is preinstalled on GitHub `windows-latest` runners, so the CI path needs only the two secrets.

**Ready for distribution: NO — BLOCKED** on 7 external gates: (1) code-signing certificate, (2) live backend login validation, (3) clean-VM install, (4) `release.yml` first run, (5) first-launch API-environment default decision, (6) Product v1.0 scope sign-off + tag authorization, (7) release-notes/CHANGELOG update for the Team 3 hardening. Each has a named owner; none is a Desktop code defect.

**Optional Team 3 follow-ups (only if authorized):** draft the `CHANGELOG.md [1.0.0]` / release-notes section covering the hardening engagement; flip the Release-build API-environment default; push `da0c36b` + fast-forward `main`.

---

**STOP.** Awaiting PHASE 8.150 authorization.
