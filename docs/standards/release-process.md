# Release Process

**Status:** Desktop Productionization Sprint 2. Operational runbook for the full release chain.

## The chain

```
Development
   ↓
Build            dotnet build RojanDesktop.sln --configuration Release
   ↓
Test             dotnet test RojanDesktop.sln --configuration Release
   ↓
Publish win-x64  build/publish.ps1 (self-contained, single-file)
self-contained
   ↓
Generate         build/publish-installer.ps1 (wraps publish.ps1 +
installer        Inno Setup — see build/installer/RojanReception.iss)
   ↓
Generate         Get-FileHash -Algorithm SHA256 on the installer
checksum
   ↓
Update release   ROJAN_Web's lib/downloads/release-registry.ts +
registry         lib/constants/app-showcase.ts (cross-repo — manual, see §4)
```

`.github/workflows/release.yml` automates every step through "Generate
checksum" on a `vMAJOR.MINOR.PATCH[-PRERELEASE]` tag push, per
`docs/standards/versioning.md` §6. "Update release registry" is the one
step CI doesn't do — see §4.

## 1. Cutting a release locally

```powershell
# From a clean working tree on main, with Directory.Build.props already
# bumped to the version being released (per versioning.md §5 — a
# deliberate PR, never bundled silently):

dotnet build RojanDesktop.sln --configuration Release
dotnet test RojanDesktop.sln --configuration Release --no-build

# Unsigned (no certificate — the default, see docs/standards/code-signing.md):
.\build\publish-installer.ps1

# Signed (once a certificate exists):
.\build\publish-installer.ps1 -CertificatePath "..." -CertificatePassword "..."

Get-FileHash "artifacts\ROJAN Reception Setup.exe" -Algorithm SHA256
```

## 2. Cutting a release via CI

1. Bump `Directory.Build.props`'s `VersionPrefix` (and `VersionSuffix` if
   this is a prerelease), commit, get it merged to `main` — a normal,
   reviewed PR, per `versioning.md` §5.
2. Tag the merge commit: `git tag v1.0.0 && git push origin v1.0.0`.
3. `.github/workflows/release.yml` runs automatically: Build → Test →
   verify the tag matches `Directory.Build.props` exactly (already
   existed pre-Sprint-2, unchanged) → `publish.ps1` (ZIP) →
   `publish-installer.ps1` (installer, signed if
   `CODE_SIGNING_CERT_BASE64` is set, unsigned otherwise) → SHA-256
   checksum → both artifacts + a GitHub Release.

## 3. What CI does NOT do

CI never touches `ROJAN_Web` — that's a separate repository with its own
deployment. This is deliberate, not an oversight: a cross-repo CI trigger
that this session cannot actually test end-to-end (no access to
`ROJAN_Web`'s own deploy pipeline/hosting) would be a fabricated
automation claim, not a real one. §4 below is the real process today.

## 4. Updating the release registry (manual, cross-repo)

After a CI run (or a local `publish-installer.ps1`) produces a real
installer + checksum:

1. Download the `ROJAN-Reception-Setup-vX.Y.Z` artifact (and its
   `.sha256` sidecar) from the GitHub Release.
2. In `ROJAN_Web`, place the installer at
   `apps/website/public/downloads/reception/rojan-reception-v<version>-win-x64-setup.exe`
   (the existing naming convention — see that repo's
   `lib/downloads/release-registry.ts` for the full contract).
3. Update `lib/downloads/release-registry.ts`'s `reception` entry:
   `fileName`, `version`, `releaseDate`, `checksum` (from the `.sha256`
   sidecar), `architecture: "x64"`.
4. Update `lib/constants/app-showcase.ts`'s `reception` entry:
   `version`, `releaseDate` (display copy — `available`/`downloadHref`
   only need to change on a brand-new app's *first* release, already
   done in Sprint 1).
5. `npx tsc --noEmit && npx vitest run && npm run build` in
   `apps/website/` — must all pass before merging.
6. Deploy `ROJAN_Web` per its own deployment process (out of this
   document's scope — a separate repo's own concern).

A future improvement worth naming but not built here: a scheduled or
manually-triggered `ROJAN_Web` workflow that pulls the latest
`ROJAN_Desktop` GitHub Release via the GitHub API and opens this same PR
automatically. Not built in this sprint — cross-repo automation like
that needs its own design/approval, not a drive-by addition to a
Desktop-repo-focused sprint.
