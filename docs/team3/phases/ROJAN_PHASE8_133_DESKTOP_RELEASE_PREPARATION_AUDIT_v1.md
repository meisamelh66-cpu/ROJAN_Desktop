# ROJAN AI — TEAM 3 — PHASE 8.133 — DESKTOP RELEASE PREPARATION AUDIT v1

**Type:** Release-preparation audit. **STRICT MODE — no source/test change, no fix, no commit/push/merge/rebase.** Read-only verification + `-c Release` build/test (verification only, no publish, no tag).
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `58a2c88` (unchanged)
**Reference:** `ROJAN_PHASE8_132_DESKTOP_FINAL_COMPLETION_AUDIT_v1.md`, `docs/RojanReception_v1.0_Production_Checklist.md`, `docs/ROJAN_Reception_v1.0_Final_Release_Checklist.md`

**Bottom line:** **Release configuration is clean and reproducible.** `-c Release` build **0 warnings / 0 errors**; `-c Release` full suite **2,715 / 2,715 PASS, 0 skipped**; Architecture **7 / 7**. No development-only configuration reaches a Release build (demo mode is `#if DEBUG`-gated). The Team 3 hardening track added nothing that changes the release posture — it only improved it. **The genuine release gates are all pre-existing and non-Desktop-hardening** (unsigned installer, no live login test, no clean-VM test, pipeline never executed, 3 backend contracts pending, POS idempotency, first-launch API-environment default). **Recommendation: this is a safe freeze/tag point for the Desktop hardening work; a real v1.0 launch still needs the blocker list worked.**

---

## A. GIT STATE

| Check | Value |
|---|---|
| HEAD | `58a2c88069ac90da319e3e900478935a518649ef` |
| HEAD subject | `fix(desktop): fix settings error message visibility` (2026-08-29) |
| Branch | `feature/team3-desktop-completion` |
| Tracked working tree | **clean** (0 modified / 0 deleted / 0 staged) |
| Untracked | `.md` reports only |
| `git describe --tags` | **`v1.0.0-45-g58a2c88`** — tag `v1.0.0` exists, 45 commits behind HEAD |
| Commits since `801cc65` (Team 3 baseline) | **30** — all `fix(desktop): …`, linear, no merges/reverts/force-pushes |

**Frozen state preserved.** The working tree matches the Phase 8.131 commit exactly; nothing has changed since Phase 8.132. No tag was created by this phase (STRICT MODE; tagging is a Phase-8.134+ decision).

**Tag/release readiness:** `Directory.Build.props` `VersionPrefix` = `1.0.0` is the single source of truth; `build/get-version.ps1` reads it; `.github/workflows/release.yml` verifies a pushed tag matches. A `v1.0.1` (or `v1.1.0`) tag on `58a2c88` would be internally consistent — but see §F before tagging.

---

## B. RELEASE BUILD VERIFICATION

`dotnet build -c Release` (full solution, from clean):

| Metric | Value |
|---|---|
| Result | **Build succeeded** |
| **Warnings** | **0** |
| **Errors** | **0** |
| Duration | **00:02:02.6** (2m03s) |
| `TreatWarningsAsErrors` | `true` (solution-wide, `Directory.Build.props`) — 0 warnings ⇒ genuinely clean, not suppressed |
| `Deterministic` | `true` — reproducible builds |
| `GenerateDocumentationFile` | `true` — XML docs emitted for all 6 projects |
| `EnforceCodeStyleInBuild` + `AnalysisMode=Recommended` | on — analyzers ran, 0 findings |

**Generated artifacts (Release output present, not re-published):**
`src/Rojan.Desktop.Shell/bin/Release/net8.0-windows/` contains `Rojan.Desktop.Shell.exe` (`WinExe`, `UseWPF`), all 6 Rojan assemblies + their XML docs, the framework/EF-Core/Hosting/SQLite dependency set, `Languages/` (`.pack` manifests `CopyToOutputDirectory`), and `Assets/RojanReception.ico` embedded as `<ApplicationIcon>`. No `appsettings*.json` (by design — see §D).

---

## C. TEST RELEASE BASELINE

`dotnet test -c Release --no-build` (full solution):

| Suite | Passed | Failed | Skipped |
|---|---|---|---|
| `Rojan.Desktop.Domain.Tests` | 456 | 0 | 0 |
| `Rojan.Desktop.Application.Tests` | 791 | 0 | 0 |
| `Rojan.Desktop.Presentation.Tests` | 772 | 0 | 0 |
| `Rojan.Desktop.Infrastructure.Tests` | 609 | 0 | 0 |
| `Rojan.Desktop.Shell.Tests` | 80 | 0 | 0 |
| **`Rojan.Desktop.ArchitectureTests`** | **7** | **0** | **0** |
| **TOTAL** | **2,715** | **0** | **0** |

**Debug ↔ Release parity confirmed:** identical counts, identical pass rate, **0 skipped in either configuration**. Architecture rules (dependency direction, EF-Core confinement, booking authority, shared-controls independence, ViewModel testability) all hold in Release.

---

## D. CONFIGURATION REVIEW

**No `appsettings.json` / `App.config` / `web.config` anywhere in `src/`.** Configuration is: (1) code constants, (2) environment-variable overrides, (3) a per-user persisted-settings JSON written at runtime. Nothing is bundled that a build could leak.

| Config surface | Finding |
|---|---|
| **Demo / mock mode** | `EnvironmentDemoModeProvider.IsEnabled` — reads `ROJAN_DESKTOP_DEMO_MODE`, but **compiled to constant `false` in Release** (`#if DEBUG` / `#else false`). Defense-in-depth mirror of the backend's `DevOtpModeGuard`. ✅ **No dev-only path reachable in a Release build.** |
| **`#if DEBUG` blocks** | Only one in all of `src/` — the demo-mode gate above. ✅ |
| **API endpoints** | `ApiEnvironmentService`: `ProductionUrlDefault = "https://api.rojanai.ir"` (confirmed the real, live host by the Sprint-2 checklist §3 — reachability + contract probes), `DevelopmentUrl = "http://localhost:8080"` (a constant, only used when the user explicitly selects Development). `ROJAN_API_BASE_URL` env var overrides both. `RojanBrandConfiguration.ApiBaseUrl = "api.rojanai.ir"`. **⚠️ See §F — `SelectedEnvironment` defaults to `ApiEnvironment.Development` on first launch** (documented design: "switching to Production is an explicit user action"). |
| **Logging** | `Host.CreateDefaultBuilder()` wires the default Console + Debug providers; `App` adds `LocalFileLoggerProvider` (daily-rotated file, 14-day retention, fail-safe). No `Logging` config section (no `appsettings`), so minimum level is the framework default `Information`. Console/Debug providers in a `WinExe` are inert (no console attached; Debug provider only writes to an attached debugger) — **not a leak, but a candidate for an explicit `ClearProviders()` + file-only setup if a reviewer wants Release logging to be strictly file-only.** ViewModel logs are operation-name-only (35 templates verified in Phase 8.132); `App.LogUnhandledException` + `HttpApiClient` log full exceptions by design (post-mortem / HTTP diagnostics). |
| **Build constants / flags** | `Directory.Build.props`: `TargetFramework net8.0-windows`, `Nullable enable`, `TreatWarningsAsErrors true`, `WarningsNotAsErrors/NoWarn CS1591`, `Deterministic true`, `AnalysisMode Recommended`. All production-appropriate. No `DefineConstants` overrides, no `DEBUG` leakage into Release. |
| **Secrets / credentials** | None in source. `Microsoft.Extensions.Configuration.UserSecrets.dll` ships in the output (transitive via Hosting) but there is no `UserSecretsId` and no secrets file — inert. Session tokens are DPAPI-encrypted at rest (per release notes). |

**Verdict: no development-only configuration leaks into a Release build.** The one item to *decide* (not fix) is the first-launch environment default — §F.

---

## E. PACKAGING READINESS

| Item | State |
|---|---|
| **Application version** | `1.0.0` — `Directory.Build.props` `VersionPrefix`, single source of truth (`docs/standards/versioning.md`). Inherited by all 6 projects. `git describe` at HEAD = `v1.0.0-45-g58a2c88`, so an `InformationalVersion` resolves to `1.0.0+<commit>` (confirmed on the built exe as `ProductVersion: 1.0.0+<commit>` in the Sprint-2 checklist §4). |
| **Assembly metadata** | `Company = ROJAN`, `Product = "ROJAN Reception"`, `Copyright = "Copyright © ROJAN <year>"` (dynamic). Verified on the built exe (`ProductName: ROJAN Reception`) in Sprint-2. Solution/namespace names stay `Rojan.Desktop.*` (deliberate — cosmetic-only rename avoided). |
| **Icon / branding** | `Rojan.Desktop.Shell.csproj` `<ApplicationIcon>Assets\RojanReception.ico` (16/32/48/256 px, from `build/generate-icon.ps1`). Drives the exe icon, taskbar, installer wizard, shortcuts, Add/Remove Programs. |
| **Installer** | `build/installer/RojanReception.iss` — Inno Setup. `AppName "ROJAN Reception"`, `AppPublisher "ROJAN"`, `AppExeName "Rojan.Desktop.Shell.exe"`, stable `AppId` GUID `{D804D0AC-BF41-4A54-8904-D9EC1BB773CF}`, `PrivilegesRequired=lowest` (per-user, no admin prompt), `OutputBaseFilename "ROJAN Reception Setup"`. `AppVersion` injected via `/DAppVersion` from `get-version.ps1` (fallback `0.1.0-alpha` only if undefined). Start Menu + optional desktop shortcut, clean uninstall. **Verified install→verify→uninstall→verify cycle twice** in Sprint-2 (at an earlier commit). |
| **Publish** | `build/publish.ps1` — **self-contained, single-file, `win-x64`** (`--runtime win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true`). Sprint-2 confirmed `"includedFrameworks"` bundles both `Microsoft.NETCore.App` + `Microsoft.WindowsDesktop.App` — **no .NET runtime install required on the target machine.** |
| **Code signing** | **Hooks ready, NOT signed.** `build/publish-installer.ps1` accepts `-CertificatePath/-CertificatePassword/-TimestampUrl` → `signtool.exe`; `.iss` has the `#ifdef SignInstaller` block; `release.yml` signs if `CODE_SIGNING_CERT_BASE64`/`_PASSWORD` secrets exist. **No certificate purchased** (documented out of scope). Until then: SmartScreen "Unknown Publisher" on first run. |
| **Release pipeline** | `.github/workflows/release.yml` — Build → Test → Publish ZIP → Inno Setup → Publish installer → SHA-256 → GitHub Release, on a version tag. **Defined and reasoned-through, never executed via a real tag push.** |
| **Output artifacts (current)** | `artifacts/RojanDesktop-v1.0.0-win-x64.zip` — a **stale ZIP from a prior session** (not regenerated at `58a2c88`). The Sprint-2 `ROJAN Reception Setup.exe` (53.9 MB, SHA-256 `eb4c59b5…be5`) was built at an **earlier commit** — a fresh publish + checksum is needed for any real `58a2c88` release. |

**Verdict: packaging infrastructure is complete and proven; the artifacts on disk are stale and would need a fresh `publish.ps1` + `publish-installer.ps1` run (out of scope here — STRICT MODE) for a real `58a2c88` release.**

---

## F. FINAL HANDOFF STATUS

### READY — items complete

| Area | Evidence |
|---|---|
| Solution builds clean, both configs | Debug 0/0, Release 0/0 (§B) |
| Full test suite, both configs | 2,715 / 2,715, 0 skipped, in Debug **and** Release (§C) |
| Architecture rules | 7 / 7 in both configs |
| **Error-handling / reliability / security hardening (Team 3)** | Missing-Guard Sweep COMPLETE; P2 error-surface sanitization COMPLETE (58/58 Category-A); Settings-visibility fix DONE; diagnostic-logging CLOSED — see Phase 8.132 |
| No dev-only config in Release | demo mode `#if DEBUG`, single `#if DEBUG` block total, no `appsettings` (§D) |
| Version / assembly metadata | `1.0.0`, single source of truth, verified on-exe in Sprint-2 (§E) |
| Branding | real ROJAN icon wired end-to-end (§E) |
| Installer infrastructure | Inno Setup, per-user, stable `AppId`, verified install/uninstall cycle (§E) |
| Self-contained publish | `win-x64` single-file, no runtime dependency, proven via `includedFrameworks` (§E) |
| Release pipeline definition | `release.yml` full chain defined (§E) |
| Backend reachability + contract | read-only + code-level verified live against `https://api.rojanai.ir` (Sprint-2 §3) |

### BLOCKERS — must resolve before a genuine v1.0 production launch (none are Team 3 Desktop-hardening work)

| # | Blocker | Owner | Notes |
|---|---|---|---|
| 1 | **Installer unsigned** | Team 3 / procurement | No code-signing certificate. SmartScreen "Unknown Publisher" on first run. Hooks fully ready — a purchased cert plugs in with zero redesign (`docs/standards/code-signing.md`). **Biggest external dependency.** |
| 2 | **No live end-user login test** | Team 3 (needs a real phone) | OTP / JWT / refresh / salon / dashboard / services / customers / booking verified live-reachable + contract-correct, **not exercised as a real user session** writing real data. |
| 3 | **Clean-VM install not tested** | Team 3 (needs a clean Win10/11 VM) | Self-contained bundling proven technically; an actual install on a machine with no .NET runtime not performed. Manual runbook in `docs/RojanReception_v1.0_Production_Checklist.md` §8. |
| 4 | **Release pipeline never executed** | Team 3 | `release.yml` extended + reasoned, never run via a real tag push (pushing a release tag triggers a real GitHub Release — deliberately not done). |
| 5 | **Inventory / HR / Accounting on `Fake*Repository`** | **Backend / Team 1** | Backend has zero code for these domains (re-confirmed Phase 8.0). Desktop side fully prepared — connection is a small follow-up when each contract lands. |
| 6 | **POS `ChargeAsync` payment-idempotency** | Backend + Team 3 | Invoice stays re-chargeable after a failed payment; backend idempotency unverified from this codebase. Documented via a behaviour-confirming test, not fixed. |
| 7 | **First-launch API-environment default = `Development` (`http://localhost:8080`)** | **Product decision needed** | `ApiEnvironmentService.SelectedEnvironment` defaults to `Development`; switching to Production is an explicit Settings action. A fresh install on an end-user machine points at a non-existent localhost until switched. The release notes say "connected to the real ROJAN backend — no demo/mock mode". **Reconcile:** either (a) flip the first-launch default to `Production` for Release builds (a `#if DEBUG` split, ~5 lines + a test), or (b) confirm the onboarding flow forces the choice, or (c) accept + document. Small work, but needs an explicit call before shipping. |

### PENDING — optional improvements (P3, none authorized)

- Fresh `-c Release` publish + installer + SHA-256 at `58a2c88` (the on-disk artifacts are from an earlier commit).
- Explicit `ClearProviders()` + file-only logging for Release (Console/Debug providers are inert in a `WinExe` but conceptually noisy).
- The Phase 8.132 P3 list: `SettingsPageViewModel` Category-D → localized string; `App.ShowErrorDialog` generic message; API-Environment "Restart Now" button mislabel; Wave G P3 (3 infra VMs); `CancellationToken` propagation; Startup UX.

---

## G. RELEASE RECOMMENDATION

**The Team 3 Desktop hardening track is release-safe and adds no new blockers.** `58a2c88` builds and tests clean in Release, contains no dev-only configuration, and *strengthens* the release posture (every error surface sanitized, every command guarded, logs safe). Compared with the Sprint-2 `v1.0.0` checklist, the delta since is 30 commits of pure reliability/security hardening plus test growth (2,280 → **2,715**), with the same clean quality bar.

**Recommended path:**

1. **Freeze the Desktop hardening track at `58a2c88`** (Phase 8.132's recommendation, re-affirmed). Open the `feature/team3-desktop-completion` → `main` PR for normal review.
2. **Make the one product decision (Blocker 7)** — first-launch API-environment default — before any real build. It's the only *code* item, and it's small.
3. **If a genuine v1.0(.x) launch is the goal**, work Blockers 1–4 in order: a signing certificate (external, longest lead time) → a real OTP login test → a clean-VM install → a real pipeline run via a tag push. Blockers 5–6 (Inventory/HR/Accounting, POS idempotency) are **Backend/Team 1** gated and do not block a launch that scopes those domains as "coming soon" (they already surface as prepared-but-pending).
4. **After the decision + a signing cert exist**, regenerate the Release publish + installer + checksums at the frozen commit, tag it, and let `release.yml` run for real once.

**Do not** hold the freeze for the P3 list — none of it blocks a release.

---

## STOP

Phase 8.133 Desktop release-preparation audit complete. **Nothing modified** (STRICT MODE; `-c Release` build/test were verification-only — no publish, no tag, no commit). HEAD `58a2c88`, tracked tree clean.

**Release configuration is clean and reproducible:** `-c Release` build **0 warnings / 0 errors** (2m03s, deterministic); `-c Release` full suite **2,715 / 2,715 PASS, 0 skipped**; Architecture **7 / 7** — full Debug↔Release parity. **No development-only configuration reaches a Release build** (demo mode `#if DEBUG`-gated; no `appsettings`; single `#if DEBUG` block solution-wide). Packaging infrastructure (version 1.0.0 single-source, real branding, Inno Setup per-user installer with signing hooks, self-contained `win-x64` single-file publish, `release.yml` pipeline) is **complete and proven** — on-disk artifacts are stale (from an earlier commit) and would need a fresh publish for a real `58a2c88` release.

**No P0. Release blockers (7) are all pre-existing and non-Desktop-hardening:** unsigned installer, no live login test, no clean-VM test, pipeline never run, Inventory/HR/Accounting backend contracts (Team 1), POS payment-idempotency, and one **product decision** — the first-launch API-environment default (`Development` / localhost).

**Recommendation: freeze at `58a2c88`; make the first-launch-environment decision; then work the signing → live-test → clean-VM → pipeline chain if a genuine launch is the goal. The Desktop hardening work needs nothing further.**

**Awaiting Phase 8.134 authorization.**
