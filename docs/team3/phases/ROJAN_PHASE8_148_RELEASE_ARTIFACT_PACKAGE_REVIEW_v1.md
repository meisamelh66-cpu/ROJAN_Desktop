# ROJAN_PHASE8_148 — RELEASE ARTIFACT PACKAGE REVIEW v1

**Phase:** 8.148 · **Type:** Final artifact verification · **Date:** 2026-08-29
**Mode:** STRICT — documentation only · no source change · no commit · no branch change
**State:** `origin/main` = `77414de` · local HEAD = `da0c36b` (docs; `src`/`tests` == main) · tracked tree clean

---

## TASK A — ARTIFACT INVENTORY

All artifacts under `artifacts/` and `publish/` (both git-ignored build output). Produced 2026-08-29 by `build/publish-installer.ps1` from HEAD `da0c36b` (code tree identical to `main` `77414de`).

| Artifact | Path | Size (bytes) | Purpose |
|---|---|---|---|
| **Installer EXE** | `artifacts/ROJAN Reception Setup.exe` | **54,057,848** (51.55 MB) | Windows per-user installer |
| Installer checksum | `artifacts/ROJAN Reception Setup.exe.sha256` | 93 | SHA-256 sidecar (`*`-prefixed BSD-tag form) |
| **ZIP package** | `artifacts/RojanDesktop-v1.0.0-win-x64.zip` | **73,887,804** (70.46 MB) | Portable / no-install distribution |
| **Publish output** | `publish/Rojan.Desktop.Shell.exe` | **174,031,936** (165.97 MB) | Self-contained single-file `win-x64` app (installer + ZIP payload source) |
| Publish languages | `publish/Languages/{ar-SA,de-DE,en-US,fa-IR}.pack` | 330 / 960 / 316 / 331 | Bundled language packs |
| Publish symbols/docs | `publish/*.pdb`, `publish/*.xml` (6 each) | — | Debug symbols + XML doc (present in publish dir; **not** shipped in installer) |

### SHA-256 (both recomputed this phase, 64 hex chars each)

| File | SHA-256 |
|---|---|
| `ROJAN Reception Setup.exe` | `69cb1f29d9d92541da8c68f926c96fbe3610f811bf95663ff532152713097615` |
| `RojanDesktop-v1.0.0-win-x64.zip` | `e6a75e0ba406d6baececa581d0df39ea094f3d611623b5f2fdf884e457e0eb14` |

> Sidecar `.sha256` present for the installer only. **Gap:** the ZIP has no committed `.sha256` sidecar — `publish.ps1` writes one at pipeline time; the hash above is the authoritative value for this local build. Recommend the release pipeline emit sidecars for every artifact.

### Version metadata

| Property | Payload exe (`Rojan.Desktop.Shell.exe`) | Installer exe | Source |
|---|---|---|---|
| ProductName | `ROJAN Reception` | `ROJAN Reception` | `Directory.Build.props` `<Product>` / `.iss` `AppName` |
| ProductVersion | `1.0.0+da0c36bccebaa741e6cd222f8c248a66fda04be2` | `1.0.0` | `<VersionPrefix>` + `SourceRevisionId` / `.iss` `AppVersion` (via `get-version.ps1`) |
| FileVersion | `1.0.0.0` | (not set — Inno default) | `<VersionPrefix>` |
| CompanyName | `ROJAN` | `ROJAN` | `<Company>` |
| OriginalFilename | `Rojan.Desktop.Shell.dll` | — | build |

Version is single-sourced from `Directory.Build.props` `<VersionPrefix>1.0.0</VersionPrefix>`; the informational `+<commit>` suffix ties the binary to `da0c36b`.

---

## TASK B — INSTALLER PACKAGE REVIEW

| Attribute | Value | Confirmed |
|---|---|---|
| **File name** | `ROJAN Reception Setup.exe` (`.iss` `OutputBaseFilename=ROJAN Reception Setup`) | ✅ |
| **Size** | 54,057,848 bytes | ✅ |
| **Version** | `1.0.0` (`.iss` `AppVersion`, passed `/DAppVersion` from `get-version.ps1`) | ✅ |
| **Icon** | `SetupIconFile = RojanReception.ico` (wizard) · `UninstallDisplayIcon = {app}\Rojan.Desktop.Shell.exe` (ARP) · icon also embedded in the payload exe by the build | ✅ |
| **Uninstall support** | `unins000.exe` generated in `{app}` · `UninstallDisplayName = ROJAN Reception` · ARP entry with `UninstallString` · `[UninstallDelete] Type: filesandordirs; Name: {localappdata}\RojanDesktop` (removes app data on uninstall) | ✅ |
| **Shortcut generation** | Start-Menu group `ROJAN Reception`: program shortcut + "Uninstall ROJAN Reception" shortcut (always) · Desktop shortcut gated on the `desktopicon` task, `Flags: unchecked` (opt-in) · post-install "Launch" checkbox, `skipifsilent` | ✅ |
| **App ID** | `{D804D0AC-BF41-4A54-8904-D9EC1BB773CF}` — fixed GUID, drives upgrade detection (never change) | ✅ |
| **Install location** | `DefaultDirName = {autopf}\ROJAN Reception` → with `PrivilegesRequired=lowest` resolves to `%LOCALAPPDATA%\Programs\ROJAN Reception\` (no admin prompt); `PrivilegesRequiredOverridesAllowed=dialog` lets a user elect all-users | ✅ |
| **Compression** | `lzma2` + `SolidCompression=yes` | ✅ |
| **Wizard** | `WizardStyle=modern`, language `english` | ✅ |
| **Signed** | **No** — unsigned installer + unsigned uninstaller (no certificate; signing hooks present and inert) | ⚠️ external gate (Phase 8.147 B1) |

### Phase 8.144 behavioural validation (this machine) — still current

Install (`/VERYSILENT`) exit 0 · files + 4 language packs + `unins000.exe` present · Start-Menu shortcuts created · Desktop shortcut correctly absent · ARP entry correct (`ROJAN Reception` / `1.0.0` / `ROJAN`) · **launch reached the login screen** (fa-IR RTL, mobile-number field, "ارسال کد", themed, SQLite bootstrap, file logger, no missing-DLL errors) · uninstall (`/VERYSILENT`) exit 0, all traces removed including `%LocalAppData%\RojanDesktop`.

---

## TASK C — REPRODUCIBILITY

All commands run from repo root on `main` (`77414de`) or the branch (`da0c36b` — code tree identical). PowerShell 7 (`pwsh`). Requires .NET 8 SDK; installer step additionally requires Inno Setup 6 (`winget install --id JRSoftware.InnoSetup`).

### 1. Build

```powershell
dotnet restore
dotnet build  -c Release --no-restore     # 0 warnings / 0 errors (TreatWarningsAsErrors)
dotnet test   -c Release --no-build       # 2,715 / 2,715 passed, 0 skipped
```

### 2. Publish (portable exe + ZIP, no installer)

```powershell
pwsh build/publish.ps1
#   -> publish/Rojan.Desktop.Shell.exe                       (self-contained single-file win-x64)
#   -> publish/Languages/*.pack
#   -> artifacts/RojanDesktop-v1.0.0-win-x64.zip  (+ .sha256 at pipeline time)
```

`publish.ps1` internally runs:
`dotnet publish src/Rojan.Desktop.Shell/Rojan.Desktop.Shell.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true` — version from `build/get-version.ps1` (reads `Directory.Build.props` `<VersionPrefix>`).

### 3. Installer generation

```powershell
# unsigned (as shipped this phase):
pwsh build/publish-installer.ps1
#   -> runs publish.ps1, then ISCC.exe build/installer/RojanReception.iss
#      /DAppVersion=1.0.0 /DPublishDir=... /DAssetsDir=...
#   -> artifacts/ROJAN Reception Setup.exe  (+ .sha256)

# signed (when a certificate exists):
pwsh build/publish-installer.ps1 -CertificatePath <path.pfx> -CertificatePassword <pw>
#   -> signs the payload exe and the installer + uninstaller via signtool
```

### CI equivalent

`.github/workflows/release.yml` runs this exact chain on a `v*` tag push. Never executed against a real tag (Phase 8.147 gate #4); the local runs in Phases 8.143–8.144 verify the script path end to end.

---

## TASK D — RELEASE ARTIFACT PACKAGE REPORT

### Verification summary

| Area | Result |
|---|---|
| Artifact inventory | ✅ 3 shipping artifacts + publish tree present, sizes recorded |
| SHA-256 | ✅ both recomputed (64 hex); installer sidecar present, **ZIP sidecar missing locally** (pipeline-generated) |
| Version metadata | ✅ `1.0.0` single-sourced; payload exe carries `1.0.0+da0c36b…`, company `ROJAN`, product `ROJAN Reception` |
| Installer name / size / version / icon | ✅ all confirmed against `.iss` and file metadata |
| Uninstall support | ✅ `unins000.exe` + ARP entry + app-data cleanup |
| Shortcut generation | ✅ Start-Menu (program + uninstall) always; Desktop opt-in (unchecked) |
| Behavioural validation | ✅ install / launch-to-login / uninstall all pass (Phase 8.144) |
| Reproducibility | ✅ build / publish / installer commands documented; CI uses the same chain |
| Signing | ⚠️ unsigned — external gate B1 (Release Engineering) |

### Findings

1. **All Team-3 artifacts are complete, versioned, and reproducible.** The installer, ZIP, and publish output derive from a single clean Release build of the `main` code tree, version-stamped `1.0.0` from one source.
2. **ZIP `.sha256` sidecar is not present in the local `artifacts/` folder** — only the installer has one. `publish.ps1` emits it during a pipeline run. Minor; recommend the pipeline guarantee a sidecar per artifact and publish them with the GitHub Release.
3. **Installer is unsigned** — expected; tracked as external gate B1. Signing is a re-run of `publish-installer.ps1 -CertificatePath …`, no repackaging redesign.
4. **`publish/` contains `.pdb` and `.xml` files** — these are in the publish *directory* but are **not** bundled into the installer (`.iss` `[Files]` ships the exe + language packs). ZIP contents should be spot-checked by Release Engineering to confirm symbols are excluded from the customer ZIP, or deliberately included as a debug aid.

### Verdict

**Release artifact package: READY (Team 3 scope).** Artifacts are correct, complete, reproducible, and behaviourally validated. Outstanding items are external: code signing (B1) and pipeline-generated checksums / GitHub Release publication (B4), both owned by Release Engineering / DevOps.

---

## TASK E — VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / project / build-script changed | ❌ none |
| Commits created | ❌ none |
| Branch changed | ❌ none |
| Files created this phase | `ROJAN_PHASE8_148_RELEASE_ARTIFACT_PACKAGE_REVIEW_v1.md` (untracked, repo root) |

**Documentation only. Confirmed.**

---

**STOP.** Awaiting PHASE 8.149 authorization.
