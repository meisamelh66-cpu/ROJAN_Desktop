# Phase 09 — Release Engineering

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Implement the CI/CD and packaging pipeline `docs/standards/branch-strategy.md`
and `docs/standards/versioning.md` both describe as "Phase 09's" deliverable,
per the approved implementation plan: a PR/branch build+test gate, a
tag-triggered packaging/release pipeline, and the version-verification
tooling connecting them to the existing `Directory.Build.props` single
source of truth. No `src/` changes, no UI changes, no architecture changes
— this phase is exclusively `.github/workflows/`, `build/` tooling, and
release documentation.

## Deliverables

- [x] `.github/workflows/ci.yml` — build+test gate on every PR into `main`
      and every push to `phase/**`/`feature/**`/`fix/**`, matching the
      branch model in `branch-strategy.md` §2. Runs `dotnet build` (Release,
      `TreatWarningsAsErrors` already the real gate) then `dotnet test`
      across all five Phase 08 test projects — including
      `ArchitectureTests` — satisfying `branch-strategy.md`'s "must build
      and pass the architecture tests" requirement automatically instead
      of as a manual pre-merge check.
- [x] `.github/workflows/release.yml` — triggers only on a `v*` tag pushed
      to `main`, per `versioning.md` §6. Independently re-builds and
      re-tests (never assumes an earlier PR's green CI), resolves the
      version from `Directory.Build.props` via `build/get-version.ps1`,
      **fails the workflow if the tag doesn't match that version exactly**,
      publishes+zips via `build/publish.ps1`, uploads the ZIP as a workflow
      artifact, and creates a GitHub Release via the runner's built-in `gh`
      CLI (no third-party marketplace action).
- [x] `build/get-version.ps1` — reads `<VersionPrefix>`/`<VersionSuffix>`
      from `Directory.Build.props` (`versioning.md` §2's single source of
      truth), pure PowerShell/XML, no new dependency. Validated locally:
      correctly resolves the current `0.1.0-alpha`.
- [x] `build/publish.ps1` — self-contained, single-file, `win-x64` publish
      of `Rojan.Desktop.Shell`, zipped to `artifacts/`. **ZIP-only
      packaging** — no installer (MSIX/MSI/WiX/Squirrel), per the approved
      plan's §7 resolution of the open packaging decision flagged in
      `docs/architecture/01-desktop-shell.md` §10: deferred as a distinct
      future decision rather than assumed, since every installer option
      costs a new dependency this phase avoids. Validated locally: produced
      a real ~65 MB `RojanDesktop-v0.1.0-alpha-win-x64.zip`, then cleaned
      up (output is git-ignored, not committed).
- [x] `build/README.md` — documents both scripts and why ZIP-only.
- [x] `.gitignore`: added `artifacts/` alongside the existing `publish/`
      entry, so generated release output is never accidentally committed.
- [x] Removed the now-redundant `build/.gitkeep` — the directory has real
      content now.
- [x] `docs/adr/0001-phase-09-naming-collision.md` context carried
      forward: this phase fills the Phase 09 slot that ADR reserved for
      Release Engineering — confirms rather than collides with it.

## Risks

- **Never executed on a real GitHub Actions runner.** No git remote
  exists in this repository and pushing one was explicitly out of scope
  for this phase — both workflow files are structurally reviewed and
  their underlying scripts locally validated, but the first real
  `ci.yml`/`release.yml` run happens only once a remote is connected and
  pushed to. Treat that first run as the actual integration test, not a
  formality.
- **No installer.** ZIP-only was a deliberate scope decision (§7 of the
  approved plan), not an oversight — end users currently get a
  self-contained folder to unzip and run, not a double-click installer.
  Revisit explicitly if that UX becomes a real requirement.
- **`GITHUB_TOKEN` permissions unverified.** `release.yml`'s
  `gh release create` step relies on the default `GITHUB_TOKEN` having
  `contents: write` — correct by default for GitHub Actions on a repo the
  workflow's own repo owns, but worth a explicit check the first time this
  runs for real, since permission defaults can be tightened
  org-wide.

## Validation Checklist

- [x] `build/get-version.ps1` run directly and via `& ./build/get-version.ps1`
      (the exact pattern `release.yml` uses) — both resolved `0.1.0-alpha`
      correctly.
- [x] `build/publish.ps1` run end-to-end locally — real self-contained
      `win-x64` publish succeeded, ZIP created and inspected
      (`RojanDesktop-v0.1.0-alpha-win-x64.zip`, ~65 MB), then removed
      (git-ignored generated output, not a committed artifact).
- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors (unchanged
      from Phase 08 — this phase touches no `src/` files).
- [x] `dotnet test RojanDesktop.sln` — all 36 tests still passing
      (unchanged from Phase 08, re-verified after this phase's changes).
- [x] No new NuGet packages, no third-party GitHub Actions, no external
      services — every tool used is either already pinned
      (`coverlet.collector`, `NetArchTest.Rules`) or built into the .NET
      SDK / GitHub-hosted runner (`gh` CLI, `Compress-Archive`,
      `actions/checkout`, `actions/setup-dotnet`, `actions/upload-artifact`).

## Approval

Approved by: <pending> — <date>
