# ROJAN Reception v1.0 — Final Release Checklist

**Status:** Release preparation complete. Nothing committed, pushed, or tagged — every change described below is in the working tree of `ROJAN_Desktop` and `ROJAN_Web`, ready for review before anyone commits/tags/ships.

---

## 1. Release Freeze

**Confirmed: no uncommitted feature/architecture/backend-contract changes outside this release's own approved scope.** Full audit of both repos' working trees, categorized:

### `ROJAN_Desktop` (67 changed paths)

| Category | What | Traces to |
|---|---|---|
| Installer & build infra | `build/generate-icon.ps1`, `build/installer/`, `build/publish-installer.ps1`, `build/publish.ps1` (modified), `.github/workflows/release.yml`, `src/Rojan.Desktop.Shell/Assets/`, `Rojan.Desktop.Shell.csproj` (icon), `Directory.Build.props` (Product/Version), `Directory.Packages.props` (QRCoder) | Sprint 1 (installer) + Sprint 2 (signing hooks, branding, release pipeline) |
| QR Ecosystem feature | `Application/QrCodes/`, `Infrastructure/QrCodes/`, `Presentation/{Converters,Modules,ViewModels,Views}/QrCodes*`, plus the Salon/Membership extensions it required (`ISalonRepository`/`ISalonInviteService`/`BackendSalonRepository`/`BackendSalonInviteRepository`/`IApiClient.GetBytesAsync`/`HttpApiClient`), and every test file for the above | Sprint 1, explicitly requested and approved |
| Production API default | `ApiEnvironmentService.cs`, `IApiEnvironmentService.cs`, `ApiEnvironmentServiceTests.cs` | Sprint 2 |
| Branding fix (RC2) | `LoginWindow.xaml`, `MainWindow.xaml` (4 hardcoded strings) | RC1 audit finding → RC2 fix, this release cycle |
| Module registration | `App.xaml.cs` (QrCodesModule registration) | Sprint 1, part of the QR Ecosystem wiring |
| Documentation | `CHANGELOG.md`, `docs/standards/versioning.md`, `docs/standards/code-signing.md`, `docs/standards/release-process.md`, `docs/RojanReception_v1.0_Production_Checklist.md`, `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md`, `docs/ROJAN_Reception_v1.0_Smoke_Test_Plan.md` | Sprint 2 + this release cycle |
| Pre-existing, unrelated | `ROJAN_Desktop_Reception_Production_Integration_Report_v1.md` | Already untracked before any of these sprints began — not part of this release's own work, left untouched |

**No architecture changes, no backend contract changes, no business-logic changes outside the QR Ecosystem feature** (which was explicitly requested and approved before "not a feature sprint" was ever said) **were found.**

### `ROJAN_Web` (6 changed paths)

| Category | What |
|---|---|
| Download routing | `app/no-tenant/download/[appId]/` (new, additive route) |
| Registry sync | `lib/downloads/release-registry.ts`, `lib/constants/app-showcase.ts`, `lib/downloads/release-registry.test.ts` — synced to the final v1.0.0 artifact's real checksum in this release cycle |
| Component threading | `app-showcase-hero.tsx` (`initialActiveId` prop, additive) |
| Real asset | `public/downloads/reception/rojan-reception-v1.0.0-win-x64-setup.exe` — the actual final installer binary |

All traceable, all previously reviewed with you across Sprints 1-2. **Freeze confirmed clean.**

---

## 2. Final Release Artifact

| Item | Value |
|---|---|
| File | `artifacts/ROJAN Reception Setup.exe` |
| Product | `ROJAN Reception` |
| Version | `1.0.0` |
| Size | 53,943,864 bytes |
| SHA-256 | `eb4c59b5646f5cc0f0a0790df6bb762fb519e9a85dd42f1c13fb854a82218be5` |
| Checksum file | `artifacts/ROJAN Reception Setup.exe.sha256` (present, cross-verified with two independent tools — `Get-FileHash` and `sha256sum` — identical result) |
| Window title (verified) | `ROJAN Reception` (RC2 fix confirmed via `MainWindowTitle`) |
| Build state | Full solution: 0 warnings, 0 errors, 2,280/2,280 tests passing |
| Install/uninstall | Verified clean on this machine (real silent install → launch → verify → uninstall → verify) |
| Signing | Unsigned — hooks ready, no certificate (`docs/standards/code-signing.md`) |

A stale `RojanDesktop-v0.1.0-alpha-win-x64.zip` left over from before the version bump was removed from `artifacts/` as part of this cleanup.

---

## 3. Website Release Sync — Verified

| Check | Result |
|---|---|
| `/download` | ✅ Builds, prerendered static |
| `/download/reception` | ✅ Builds, prerendered (SSG via `generateStaticParams`) |
| `release-registry.ts` `reception` entry | ✅ `fileName`, `version: "1.0.0"`, `checksum` all match the exact final artifact above |
| Real file on disk | ✅ `public/downloads/reception/rojan-reception-v1.0.0-win-x64-setup.exe` present, its own SHA-256 independently re-verified to match the registry's recorded value |
| Website test suite | ✅ 39/39 download-related tests, `tsc --noEmit` clean, full `next build` clean |

---

## 4. CI Release Readiness — Exact Steps (not executed)

`.github/workflows/release.yml` (built in Sprint 2) automates everything from "CI build" onward. The exact sequence, for whoever runs this for real:

```
1. git tag v1.0.0
2. git push origin v1.0.0
   → triggers .github/workflows/release.yml on windows-latest:
3.    Checkout
4.    Setup .NET 8 SDK
5.    dotnet restore RojanDesktop.sln
6.    dotnet build RojanDesktop.sln --configuration Release --no-restore   [Build]
7.    dotnet test RojanDesktop.sln --configuration Release --no-build     [Test]
8.    Resolve version from Directory.Build.props, verify it equals "1.0.0"
       (fails the whole run if Directory.Build.props and the tag disagree)
9.    ./build/publish.ps1 -Version 1.0.0                                  [Publish - ZIP]
10.   choco install innosetup -y
11.   ./build/publish-installer.ps1 -Version 1.0.0 [+ signing args if
       CODE_SIGNING_CERT_BASE64/CODE_SIGNING_CERT_PASSWORD secrets exist] [Installer]
12.   Get-FileHash on the installer, write .sha256 sidecar                [Checksum]
13.   Upload all three artifacts (.zip, .exe, .sha256)
14.   gh release create v1.0.0 <artifacts> --generate-notes
```

**Manual step after CI finishes** (deliberately not automated — cross-repo, see `docs/standards/release-process.md` §3-4): download the real artifacts from the GitHub Release, confirm the CI-generated checksum matches what's already staged in `ROJAN_Web` (§3 above — if CI's real build differs even slightly from this session's local one, re-sync `release-registry.ts` to CI's actual checksum, not this one), then deploy `ROJAN_Web`.

**Not executed as part of this release-preparation pass** — pushing a real tag triggers a real, public GitHub Release. That action is yours to take when ready, not something taken on your behalf.

---

## 5. Production Smoke Test Package

Finalized: `docs/ROJAN_Reception_v1.0_Smoke_Test_Plan.md` — now covers all 7 requested flows (OTP Login, JWT Session, Refresh Token, Salon Connection, Dashboard, Customer, Booking) with exact manual steps, expected results, and failure modes to watch for. Not executed — needs a real phone number, by design (no real OTP sent this session).
