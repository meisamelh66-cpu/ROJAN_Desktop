# Branch Strategy

**Phase:** 01 — Repository & Solution Foundation
**Status:** Draft, pending approval.

## 1. Model: trunk-based, with short-lived feature branches

**Decision: trunk-based development off `main`, not GitFlow.**

GitFlow (`develop`/`release`/`hotfix`/`feature` branches, all long-lived)
was designed for a release cadence this project doesn't have yet — no
external customers on pinned versions requiring parallel maintenance
branches. It adds real process overhead (which branch does a fix land on
first, how does it propagate to the others) that isn't earning its keep
at this stage. Trunk-based is the default for a single product with one
active release line, which is what this is until proven otherwise.
Revisit this decision explicitly (not silently) if the product reaches a
point where multiple versions are in the field simultaneously — that's a
real trigger to reconsider, not a hypothetical.

## 2. Branches

| Branch | Purpose | Lifetime |
|---|---|---|
| `main` | Always buildable, always passes CI. The one branch that represents "current state of the product." | Permanent |
| `phase/NN-short-name` | One per SDLC phase (e.g. `phase/01-foundation`, `phase/02-architecture`) — every phase's deliverables land here, reviewed as a whole, merged to `main` only on your approval. | Lives for the phase's duration |
| `feature/short-description` | Once past Phase 07, one per approved business module/feature. Branches from `main`, merges back via PR. | Days, not weeks |
| `fix/short-description` | Bug fixes against `main`. | Days |

No `develop` branch — `main` *is* the trunk. No long-lived `release/*`
branches until Phase 09 (Release Engineering) actually defines a release
process that needs one.

## 3. Rules

- **`main` is protected**: no direct pushes, no force-push, merge only via
  reviewed PR. (GitHub branch protection rules — to be configured once
  this repo has a remote; noted here as a requirement, not yet
  actionable on a purely local repo.)
- **Every PR into `main` must build and pass the architecture tests**
  (§9 of the — superseded, to-be-reissued — Shell doc; formalized
  properly once Phase 02 defines them) **before merge**, once CI exists
  (Phase 09 sets up the pipeline; until then, this is a manual
  pre-merge check).
- **Phase branches merge as a single reviewed PR**, not a series of small
  auto-merged commits — the point of phase-gating is that you review the
  phase's output as a coherent whole before it becomes part of `main`'s
  history.
- **Commit messages**: imperative mood, present tense (`"Add X"`, not
  `"Added X"` or `"Adds X"`) — matches standard git convention
  (`git log` reads as a changelog when every entry follows this).

## 4. Versioning tags

Tags follow the scheme defined in `versioning.md` (`vMAJOR.MINOR.PATCH[-PRERELEASE]`)
and are only ever applied to `main`, never to a phase or feature branch —
a tag represents a real, shippable state of the product.
