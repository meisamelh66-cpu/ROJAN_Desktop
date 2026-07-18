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

`0.1.0-alpha` — set in `Directory.Build.props` at repo creation. This is
correct SemVer for "structure exists, nothing functional yet" and will
move to `0.x.y` MINOR bumps as each phase lands, `1.0.0` only once the
first genuinely usable release goes out (a judgment call made
explicitly at that time, not automatically).

## 4. Why start at `0.1.0`, not `1.0.0`

Per SemVer's own spec: "Major version zero (0.y.z) is for initial
development. Anything MAY change at any time." That's an accurate
description of this project's current state — the architecture itself is
still pending approval. Jumping to `1.0.0` before there's a real,
stable, shipped product overstates the project's maturity to anyone
reading the version number.

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
