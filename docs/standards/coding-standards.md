# Coding Standards

**Phase:** 01 — Repository & Solution Foundation
**Status:** Draft, pending approval.

## 1. Enforcement, not aspiration

Every rule below that can be machine-enforced *is* machine-enforced —
via `.editorconfig` (repo root) and `Directory.Build.props`
(`TreatWarningsAsErrors`, `AnalysisMode=Recommended`,
`EnforceCodeStyleInBuild`). A rule that only lives in this document and
nowhere in the build is a rule that will silently rot. If you find a
standard described here that isn't backed by an analyzer or an
`.editorconfig` entry, that's a gap to close, not an acceptable state.

## 2. Language & style

- **C#, latest language version**, `Nullable` enabled solution-wide.
  Nullable warnings (`CS8600`/`CS8602`/`CS8603`/`CS8618`) are errors, not
  suggestions — see `.editorconfig`. A codebase that treats nullability
  as advisory accumulates `NullReferenceException`s exactly like one with
  no nullable annotations at all.
- **File-scoped namespaces** (`namespace Rojan.Desktop.Domain;`), not
  block-scoped — less indentation, no reason to prefer the old form in a
  new codebase.
- **Braces required** on every `if`/`for`/`while`/etc., even single-line
  bodies. Omitting them is a well-known source of real bugs when someone
  later adds a second statement without noticing the missing braces.
- **`var`** only when the type is apparent from the right-hand side
  (`var list = new List<Order>();`) — not for `var result = GetOrder();`
  where the return type isn't visible at the call site.
- **Expression-bodied members** for genuinely single-expression
  properties/methods; not forced onto anything with real logic.

## 3. Naming

Language-level naming (PascalCase types, `_camelCase` private fields,
`I`-prefixed interfaces, PascalCase constants) is enforced via
`.editorconfig` — see that file for the exact rules.

**Architecture-level naming** (how a ViewModel, a use case, a repository
implementation, etc. are named as a category) is **Phase 02's**
deliverable, not this document's — it can't be decided sensibly before
the layers themselves are. Recorded here as a forward reference so it
isn't forgotten, not answered here.

## 4. File & project organization

- One public type per file; filename matches the type name exactly.
- Folder structure inside each project mirrors its namespace structure —
  no flat "dump everything in the project root" layout once a project has
  more than a handful of files.
- `using` directives: outside the namespace, `System.*` first, then
  alphabetical — enforced via `.editorconfig`
  (`dotnet_sort_system_directives_first`).

## 5. Comments & documentation

- `GenerateDocumentationFile` is enabled solution-wide — every public
  member should have an XML doc comment, but only when it says something
  the signature doesn't already say. A comment that restates the method
  name in prose is worse than no comment: it's something that can go
  stale.
- Explain **why**, not **what** — the same rule that applies to this
  entire document's own writing style applies to code comments.

## 6. Static analysis

- `AnalysisLevel=latest`, `AnalysisMode=Recommended` (`Directory.Build.props`)
  — the .NET SDK's built-in analyzers run on every build, not just in CI.
- `TreatWarningsAsErrors=true`. The one blanket exception is `CS1591`
  (missing XML doc on a public member) — a warning, not an error, since
  it fires constantly during early scaffolding; it's expected to
  effectively disappear in practice as §5 is followed, not because the
  rule was weakened.
- Adding a project-specific analyzer suppression requires a one-line
  comment explaining why, next to the suppression — never a bare
  `#pragma warning disable` with no explanation.

## 7. Testing conventions

(Full testing strategy is **Phase 08's** deliverable. This section covers
only the naming/structure convention, since it's a coding-standards
concern that needs to exist before any test is written, not after.)

- Test method naming: `MethodUnderTest_Scenario_ExpectedResult` — e.g.
  `CalculateTotal_WithDiscount_AppliesPercentageCorrectly`. The
  `[*Tests.cs]` `.editorconfig` override exists specifically because this
  convention uses underscores, which the general PascalCase-only rule
  would otherwise flag.
- Arrange/Act/Assert, with the three sections visually separated by a
  blank line — not enforced by tooling, enforced by code review.

## 8. What this document deliberately does not cover

Architecture-specific rules (project boundaries, dependency direction,
DI conventions, layer-naming conventions) belong to **Phase 02**. This
document is C#-the-language and repo-hygiene only — it would apply
almost unchanged even if the architecture underneath it were completely
different.
