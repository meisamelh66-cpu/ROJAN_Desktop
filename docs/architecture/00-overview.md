# ROJAN Desktop — Architecture Overview

> **⚠ Superseded by the phase-gated SDLC.** This was written before the
> formal phase process existed. Its content will be formally reissued as
> part of **Phase 02 — Enterprise Architecture**, reviewed and approved
> under that phase's own checklist rather than standing on its own. Kept
> here for reference/continuity, not as an active or approved deliverable.
> See `docs/phases/phase-01-foundation.md` for what's actually in force
> right now.

**Status:** Preliminary draft — pending Phase 02.

## 1. Scope of this document

This overview exists only to give the [Desktop Shell design](01-desktop-shell.md)
context — it is not itself a request for approval of the Application/Domain/
Infrastructure layers. Those will each get their own design document, written
and approved the same way, before any business module is implemented. This
sequencing is deliberate, not an oversight: the Shell is the one layer every
other layer's design depends on (it decides how DI, navigation, and
composition work), so it has to be settled first.

## 2. Goals

- **Independence.** No file, package, asset, or architectural decision is
  shared with or dependent on `ROJAN_DesignLab`. The two are separate
  products on separate platforms that happen to share a brand name.
- **Clean Architecture.** Dependencies point in one direction only — inward,
  toward `Domain`. Outer layers (`Infrastructure`, `Presentation`, `Shell`)
  depend on inner layers through interfaces the inner layers define, never
  the reverse.
- **SOLID**, applied concretely (see §5 of the Shell document for how each
  principle maps onto real decisions, not just named in passing).
- **Testability by construction.** Domain and Application logic must be
  unit-testable with zero WPF/UI dependency. ViewModels must be testable
  without a running `Application`/`Dispatcher`.
- **Production-grade from the start.** Structured logging, global exception
  handling, configuration management, and dependency-direction enforcement
  are part of the initial design, not deferred "later" work.

## 3. Layers

```
┌─────────────────────────────────────────────────────────────┐
│  Shell            (Rojan.Desktop.Shell)   — WPF executable   │
│  composition root · app lifecycle · window chrome            │
├─────────────────────────────────────────────────────────────┤
│  Presentation     (Rojan.Desktop.Presentation)                │
│  ViewModels · Views · Converters · Behaviors  (MVVM)          │
├─────────────────────────────────────────────────────────────┤
│  Infrastructure   (Rojan.Desktop.Infrastructure)               │
│  persistence · file system · logging setup · external I/O     │
├─────────────────────────────────────────────────────────────┤
│  Application      (Rojan.Desktop.Application)                  │
│  use cases · CQRS commands/queries · service interfaces       │
├─────────────────────────────────────────────────────────────┤
│  Domain           (Rojan.Desktop.Domain)          — innermost │
│  entities · value objects · domain services · repo interfaces │
└─────────────────────────────────────────────────────────────┘
        Rojan.Desktop.Common (cross-cutting utility, referenced by all)
```

**The dependency rule:** an arrow from A to B means "A references B."

```
Shell ──────────────┬──────────────┐
                     ▼              ▼
Presentation ──▶ Application ◀── Infrastructure
                     │
                     ▼
                  Domain
```

`Domain` and `Application` never reference `Infrastructure`, `Presentation`,
or `Shell`. Where an outer layer needs to be called from an inner one (e.g.
Application needs to persist something, which is Infrastructure's job),
the inner layer defines an interface and the outer layer implements it —
standard Dependency Inversion. `Shell` is the only project allowed to
reference every other project, because it's the composition root: the one
place all the concrete implementations get wired to their abstractions.

This overview stops here. Full responsibilities for `Domain`/`Application`/
`Infrastructure` are out of scope until their own design docs — the
[Desktop Shell document](01-desktop-shell.md) covers `Shell` and, to the
extent needed for context, its boundary with `Presentation`.
