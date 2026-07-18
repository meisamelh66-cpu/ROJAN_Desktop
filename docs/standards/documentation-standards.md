# Documentation Standards

**Phase:** 01 — Repository & Solution Foundation
**Status:** Draft, pending approval.

## 1. Principle: documentation is a deliverable, not an artifact of code

Per the SDLC rules governing this project, every phase produces
documentation *before* the implementation it describes, and that
documentation is itself reviewed and approved — it is not a summary
written after the fact to describe what was already built. This document
defines where that documentation lives and what form it takes, so every
phase produces it the same way.

## 2. Structure

```
docs/
├── standards/       This document, coding-standards.md, branch-strategy.md,
│                    versioning.md — cross-cutting rules that don't belong
│                    to any single phase.
├── phases/          One document per SDLC phase: phase-01-foundation.md,
│                    phase-02-architecture.md, etc. Each is the formal
│                    record of that phase's objectives, deliverables,
│                    risks, validation checklist, and approval status —
│                    the exact structure requested for every phase update.
├── gates/           Mandatory checkpoint validations that block a phase
│                    from starting but aren't themselves a numbered SDLC
│                    phase — e.g. gate-01-environment-validation.md. A
│                    gate has a binary PASS/BLOCKED outcome per item
│                    checked, not a completion percentage: partial
│                    environment validation isn't a meaningful state to
│                    report as "60% done," it's either safe to build on
│                    or it isn't.
├── architecture/    Living design documents for the system itself (layer
│                    responsibilities, the Shell design, the Design System,
│                    etc.) — content, not phase-tracking. A phase document
│                    in docs/phases/ typically produces or updates one or
│                    more documents here.
└── adr/             Architecture Decision Records — one file per
                     significant, hard-to-reverse decision (see §4).
```

## 3. Phase documents (`docs/phases/phase-NN-name.md`)

Every phase document uses this exact structure, matching what you've
asked to see reported at every phase boundary:

```markdown
# Phase NN — <Name>

**Status:** Not Started | In Progress | Awaiting Approval | Approved
**Completion:** NN%

## Objectives
...

## Deliverables
- [ ] Deliverable 1
- [ ] Deliverable 2

## Risks
...

## Validation Checklist
- [ ] Check 1
- [ ] Check 2

## Approval
Approved by: <pending> — <date>
```

The checkbox lists in *Deliverables* and *Validation Checklist* are the
literal source of truth for the completion percentage — not a separately
maintained number that can drift from what's actually done.

## 4. Architecture Decision Records (ADRs)

For decisions that are (a) significant and (b) expensive to reverse later
— e.g. "Generic Host over Prism," "WPF over WinUI 3," "trunk-based over
GitFlow" — a short ADR goes in `docs/adr/NNNN-title.md`:

```markdown
# NNNN. <Decision Title>

**Status:** Proposed | Accepted | Superseded by NNNN
**Date:** YYYY-MM-DD

## Context
What problem/question forced this decision.

## Decision
What was decided.

## Consequences
What this makes easier, what it makes harder, what it forecloses.
```

Not every decision needs an ADR — routine choices belong in the relevant
phase/architecture document's own prose (with rationale, per this
project's existing convention of always stating *why*, not just *what*).
An ADR is for the subset of decisions someone will reasonably ask "wait,
why did we do it this way?" about, a year from now, without the context
that was obvious at the time.

## 5. Diagrams

Mermaid, inline in the relevant Markdown file — renders natively on
GitHub and in most Markdown tooling, stays version-controlled as text
(diffable, reviewable) rather than a binary image that silently goes
stale.

## 6. Writing style

Documentation in this repository follows the same voice as this document
and the existing architecture drafts: state the decision, then the
reasoning, then (where relevant) what was considered and rejected and
why. A document that only states conclusions without reasoning invites
the next reader to silently second-guess or route around it; a document
that shows the reasoning lets them agree, disagree with cause, or trust
it and move on.
