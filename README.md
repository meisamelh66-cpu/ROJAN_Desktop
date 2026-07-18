# ROJAN Desktop

A production-grade Windows desktop application, built independently from
[ROJAN_DesignLab](../ROJAN_DesignLab) (the Android app). No code, assets,
or architecture decisions are shared between the two projects.

This project is developed as an enterprise product intended for long-term
commercial maintenance — not a demo or prototype — under a formal,
phase-gated SDLC. See [`docs/phases/`](docs/phases/) for the current phase
status; each phase is documented, reviewed, and explicitly approved before
the next one begins.

## Status

**Phase 01 — Repository & Solution Foundation** — ✅ Approved.

**Gate 01 — Development Environment Validation** — 🔴 **BLOCKED.**
Two required actions before Phase 02 can begin: install the .NET 8 SDK,
update Visual Studio 2022 to 17.8+. See
[`docs/gates/gate-01-environment-validation.md`](docs/gates/gate-01-environment-validation.md)
for the full report and exact steps.

## Stack

- **.NET 8**, **C#**, **WPF**
- Clean Architecture (Domain → Application → Infrastructure/Presentation → Shell)
- SOLID throughout
- MVVM

## Development phases

| # | Phase | Status |
|---|---|---|
| 01 | Repository & Solution Foundation | Awaiting approval |
| 02 | Enterprise Architecture | Not started |
| 03 | Infrastructure | Not started |
| 04 | Design System | Not started |
| 05 | Desktop Shell | Not started |
| 06 | Business Module Analysis | Not started |
| 07 | Implementation | Not started |
| 08 | Testing | Not started |
| 09 | Release Engineering | Not started |

## Repository layout

```
src/                  Source projects (added in Phase 02)
tests/                Test projects (added alongside their source projects)
build/                CI/CD and build tooling (populated in Phase 09)
docs/
├── standards/        Coding standards, branch strategy, versioning, documentation standards
├── phases/           Formal per-phase objective/deliverable/risk/approval records
├── gates/            Mandatory checkpoint validations (e.g. environment validation)
├── architecture/     Living system design documents
└── adr/              Architecture Decision Records
```

No business code exists yet, by design — see `docs/phases/phase-01-foundation.md`
for why, and what comes next.
