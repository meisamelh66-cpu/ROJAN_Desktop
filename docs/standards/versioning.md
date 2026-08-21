# Versioning

**Phase:** 01 — Repository & Solution Foundation
**Status:** Draft, pending approval.

## 1. Scheme: Semantic Versioning (SemVer 2.0)

`MAJOR.MINOR.PATCH[-PRERELEASE]` — e.g. `0.1.0-alpha`, `1.4.2`.

- **MAJOR** — breaking change to anything another module, or a future
  plugin/extension point, depends on. Pre-`1.0.0`, this project is in
  initial development per SemVer's own rule (§4 below), so this mostly
  doesn't apply yet.
- **MINOR** — new functionality, backward compatible.
- **PATCH** — bug fix only, no behavior/API change.
- **PRERELEASE** (`-alpha`, `-beta`, `-rc.1`) — anything before the first
  stable release, and any build that hasn't been through Phase 08
  (Testing) sign-off.

## 2. Single source of truth

The version lives in exactly one place: `<VersionPrefix>`/`<VersionSuffix>`
in the root `Directory.Build.props`, inherited by every project. No
project overrides it individually unless it ships as an independently
versioned artifact (not the case for anything currently planned — the
Shell and every library ship together as one product).

## 3. Current version

`1.0.0`, set in Desktop Productionization Sprint 2 — the deliberate,
explicit judgment call §4 always said this bump would require. Reached
after: a real installer (Sprint 1), production API connectivity baked in
and live-verified (Sprint 2), real branding/icon (Sprint 2), and a
documented release pipeline (Sprint 2). `1.0.0` is a version label, not
a certification that every gap is closed — see
`docs/RojanReception_v1.0_Production_Checklist.md` for exactly what's
verified and what still isn't (installer is unsigned - no certificate
purchased, signing hooks only; a live end-user login was not exercised,
only read-only/code-level verification; POS/Checkout remains
intentionally out of scope, unchanged).

Prior to this: `0.1.0-alpha`, set at repo creation, correct SemVer for
"structure exists, nothing functional yet."

## 4. Why this project started at `0.1.0`, not `1.0.0`

Per SemVer's own spec: "Major version zero (0.y.z) is for initial
development. Anything MAY change at any time." That was an accurate
description of this project's state at repo creation — the architecture
itself was still pending approval. Jumping to `1.0.0` before there was a
real, stable, shipped product would have overstated the project's
maturity to anyone reading the version number. §3 above records when
that stopped being true.

## 5. What bumps the version, and when

- Version bumps happen **at the end of an approved phase or feature**,
  not per-commit — a version number should always describe a coherent,
  reviewed state, not an arbitrary point mid-work.
- Until CI/release tooling exists (Phase 09), version bumps are a manual
  edit to `Directory.Build.props`, called out explicitly in the PR that
  bumps it — never bundled silently into an unrelated change.

## 6. Tags & releases

A git tag `vMAJOR.MINOR.PATCH[-PRERELEASE]` is created on `main` at each
version bump, matching `Directory.Build.props` exactly. Once Phase 09
exists, tag creation triggers the packaging/release pipeline — not
defined yet, referenced here only so this document doesn't need a
rewrite when that phase lands.

## 7. Changelog

`CHANGELOG.md` (repo root) follows the
[Keep a Changelog](https://keepachangelog.com/) format — `Added`/
`Changed`/`Deprecated`/`Removed`/`Fixed`/`Security` sections per version.
Updated as part of the same PR that bumps the version, not
retroactively.
