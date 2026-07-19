# build/

CI/CD and build tooling, per the repository layout in `README.md` and the
Phase 09 Release Engineering plan (`docs/phases/phase-09-release-engineering.md`).

- **`get-version.ps1`** — reads `<VersionPrefix>`/`<VersionSuffix>` from
  the root `Directory.Build.props` (the single source of truth per
  `docs/standards/versioning.md` §2) and prints `MAJOR.MINOR.PATCH[-SUFFIX]`.
  Used by `.github/workflows/release.yml` to verify a pushed tag matches
  the committed version before packaging anything.
- **`publish.ps1`** — produces the self-contained, single-file, `win-x64`
  ZIP release artifact from `Rojan.Desktop.Shell`. Output goes to
  `artifacts/` (git-ignored) via an intermediate `publish/` folder
  (also git-ignored). No installer (MSIX/MSI/WiX/Squirrel) — ZIP-only
  packaging, per the approved plan; an installer is a distinct future
  decision, not assumed here.

Both scripts are plain PowerShell with no new dependency — runnable
locally the same way `.github/workflows/release.yml` runs them in CI.
