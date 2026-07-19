# 0001. Phase 09 Naming Collision — Customer CRM vs. Release Engineering

**Status:** Accepted
**Date:** 2026-07-18

## Context

`docs/standards/branch-strategy.md` §2/§3 and `docs/standards/versioning.md`
§5/§6 both reserve "Phase 09" for CI/CD & Release Engineering ("Phase 09
sets up the pipeline"; "tag creation triggers the packaging/release
pipeline... Once Phase 09 exists"). These reservations were written during
Phase 01 and never revisited.

Separately, when the Customer CRM work was implemented, a numbering
collision was caught and resolved against `docs/standards/coding-standards.md`
§7 (which reserves "Phase 08" for the Testing Strategy deliverable) by
numbering Customer CRM as "Phase 09" instead. That resolution did not check
`branch-strategy.md`/`versioning.md`, so it walked directly into the
second, still-unnoticed reservation — Customer CRM now collides with
Release Engineering's reserved slot the same way it originally collided
with Testing's.

By the time this was caught, Customer CRM was already committed
(`35b809e`, "Phase 09: Enterprise Customer CRM") and its phase document
already exists at `docs/phases/phase-09-customer-crm.md`. Per this
project's git workflow rules, existing commits are not rewritten and
existing files are not renamed/moved to paper over a naming mistake after
the fact.

## Decision

- **Phase 08 remains Testing Strategy**, per `coding-standards.md` §7 —
  unaffected by this collision, implemented starting with this commit.
- **Phase 09 remains reserved for Release Engineering** (CI/CD, packaging,
  tag-triggered release pipeline), per `branch-strategy.md` and
  `versioning.md`, pending a future roadmap review — not reassigned here.
- **The Customer CRM commit and its phase document are not renamed or
  rewritten.** Commit `35b809e` keeps its literal message ("Phase 09:
  Enterprise Customer CRM") and `docs/phases/phase-09-customer-crm.md`
  keeps its filename and internal heading, exactly as committed. History
  is a record of what happened, not a corrected index.
- **Going forward, Customer CRM is referred to as Phase 10** in any new
  documentation, commit message, or conversation that needs to reference
  it by number (e.g. "the Phase 10 Customer CRM module") — the filename
  and existing commit trailer are the one exception, kept as-is per the
  point above.

## Consequences

- Anyone reading `docs/phases/phase-09-customer-crm.md` or `git log` needs
  this ADR to understand why a "Phase 09" artifact is referred to as
  "Phase 10" everywhere else — this document is that pointer, and should
  be linked from `docs/phases/phase-09-customer-crm.md` itself.
- The next roadmap review must explicitly decide Phase 10's real
  successor numbering (i.e., what comes after Customer CRM once Release
  Engineering's actual slot is settled) rather than assuming a gap-free
  sequence.
- This is the second time a phase-numbering collision was caught only
  after the fact. Before assigning a new phase number going forward, check
  every file under `docs/standards/` for an existing reservation, not just
  the one file that happens to be topically closest.
