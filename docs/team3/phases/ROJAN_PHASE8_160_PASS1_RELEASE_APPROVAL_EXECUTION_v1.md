# ROJAN Reception v1.0 — PASS 1: RELEASE APPROVAL EXECUTION v1

| | |
|---|---|
| **Scope** | PASS 1 ONLY — B1, B7, B8, B-DOCS (per Phase 8.159 batch plan) |
| **Release** | ROJAN Reception v1.0 |
| **Main** | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) · version `1.0.0` (frozen) |
| **Desktop status** | READY ✅ (2,715/2,715 · 7/7 · 0/0 · installer built + build-machine-validated · 0 P0) |
| **Execution doc created** | 2026-08-29 · Phase 8.160 · by Team 3 |
| **PASS 1 owner** | Release Engineering (coordination) · Product (B7 decision, B8, B-DOCS approval) |
| **Companions** | 8.155 checklist · 8.156 war-room log · 8.157 execution session · 8.158 response intake + status model · 8.159 batch plan |

> This tracks execution of PASS 1's four approval gates. **Team 3 cannot execute or approve any of them** — no certificate, no product authority (Phase 8.151). Every gate below is at its Phase 8.158 dependency-aware initial state; owners update the Result + Evidence fields as they act.

---

## GATE B1 — CODE SIGNING

| Field | Entry |
|---|---|
| **Owner** | Release Engineering |
| **State** | WAITING (actionable now — head of the critical path) |
| **Required — Certificate** | Authenticode code-signing certificate (EV recommended per `docs/standards/code-signing.md`; OV acceptable). Microsoft-trusted CA (DigiCert / Sectigo / SSL.com / GlobalSign). Issued to the ROJAN legal entity. Identity-verified — allow a few business days. ☐ obtained |
| **Required — Signing execution** | `pwsh build/publish-installer.ps1 -CertificatePath <pfx> -CertificatePassword <pw>` on a machine with the Windows SDK, **or** via `release.yml` with the CI secrets (that run is PASS 2 / B4 — for B1 a local signed build or a confirmed-available credential + dry-verification suffices). ☐ executed |
| **Required — Signature verification** | `signtool verify /pa /v "artifacts\ROJAN Reception Setup.exe"` → "Successfully verified"; timestamp present; chains to a trusted root; payload `Rojan.Desktop.Shell.exe` and the embedded uninstaller also signed. First-run on a non-developer machine shows a **named publisher**. ☐ verified |
| **Evidence — Certificate info** | Type: ______ · CA: ______ · Subject/CN: ______ · Thumbprint: ______ · Valid to: ______ · delivered as: ☐ CI secret ☐ `.pfx` on SDK machine ☐ EV token/cloud-HSM |
| **Evidence — signtool output** | ``` <paste signtool verify /pa /v output> ``` · signed installer SHA-256: ______ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Blocker (if any)** | (default) No certificate procured; `signtool.exe` not present in the automation environment. |
| **Respondent** | ______________________ (name / role / date) |

---

## GATE B7 — API ENVIRONMENT

| Field | Entry |
|---|---|
| **Owner** | Product (decision) + DevOps (rollout) |
| **State** | WAITING (actionable now — parallel with B1) |
| **Current behaviour** | `ApiEnvironmentService.SelectedEnvironment` defaults to `Development` → `http://localhost:8080`. Production = `https://api.rojanai.ir`. `ROJAN_API_BASE_URL` overrides. User-switchable in Settings (restart-required). |
| **Required — Production API decision** | Choose one: **Option 1** — flip the Release-build default to `https://api.rojanai.ir` (**~5-line change in `ApiEnvironmentService` + 1 unit test**; a Team 3 follow-up if a future phase authorizes it; no Debug behaviour change). **Option 2** — force the environment choice in first-run onboarding. **Option 3** — ship as-is and document that reception staff set the environment on first launch. Recommendation: **Option 1**. ☐ decided — chosen: ______ |
| **Required — Written approval** | Decision record: chosen option · endpoint · approver (name/role) · reason · date. If Option 1/2: a follow-up phase authorization + a green test. ☐ recorded |
| **Evidence — Approved environment value** | Endpoint: ______________________ · approver: ______ · record link: ______ · rollout status: ☐ merged to `main` with green test ☐ onboarding prompt implemented ☐ documented only |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Blocker (if any)** | (default) Decision requires Product + DevOps authority; Team 3 cannot make or record it. |
| **Respondent** | ______________________ (name / role / date) |

---

## GATE B8 — PRODUCT SIGN-OFF

| Field | Entry |
|---|---|
| **Owner** | Product |
| **State** | BLOCKED — depends on B1 (credential exists), B7 (decision), B-DOCS (notes approved) |
| **Required — Scope approval** | Ratify that v1.0 ships the connected feature set (Auth, Salon, Dashboard, Customers, Services, Specialists, Booking/Calendar, QR, Support, Automation) and presents Inventory / HR / Accounting / POS as "coming soon" (or explicitly cuts them from the v1.0 UI). Confirm B5/B6 are post-v1.0. ☐ ratified — scope note: ______ |
| **Required — Version approval** | Confirm the release version. Source is frozen at `1.0.0`; the existing `v1.0.0` tag points at an old commit (`d518218`) — approve either moving that tag to the release commit **or** bumping `<VersionPrefix>` to `1.0.1` (separate authorized change) and tagging `v1.0.1`. ☐ version confirmed: ______ |
| **Required — Tag authorization** | Written authorization for DevOps to create and push the release tag, naming the **commit SHA** and the exact **tag string**. ☐ authorized |
| **Evidence — Approval record** | Checklist sign-off (Phase 8.151 TASK G) link: ______ · scope note link: ______ · tag authorization link: ______ · approver: ______ · date: ______ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Blocker (if any)** | (default) Depends on B1, B7, B-DOCS; none is PASS. |
| **Respondent** | ______________________ (Product owner / date) |

---

## GATE B-DOCS — RELEASE NOTES

| Field | Entry |
|---|---|
| **Owner** | Team 3 (draft/apply in an authorized editing phase) → Product (approve) |
| **State** | WAITING — draft text ready (Phase 8.151 TASK F); not applied |
| **Current state of docs** | `CHANGELOG.md` line 9: `## [Unreleased]` — **stale**; contains the Productionization-Sprint-2 entry but **no `## [1.0.0]` for the Team 3 hardening**. `docs/ROJAN_Reception_v1.0_RELEASE_NOTES.md` dated 2026-08-21, predates the hardening. (Line 1279 `## [0.1.0-alpha] - Unreleased` is an old historical heading — leave as-is or correct separately.) |
| **Required — Final 1.0.0 notes** | Apply: `CHANGELOG.md` `## [Unreleased]` → `## [1.0.0] - <release date>` with Security / Fixed / Changed blocks (draft in Phase 8.151 TASK F); refresh `RELEASE_NOTES.md` with a "Reliability & Security Hardening (Team 3)" section + updated Production-Readiness table; consolidate Known Issues (unsigned→signed status, first-launch API default, "coming soon" domains, POS re-charge, window-title inconsistency). ☐ applied (needs a commit — authorized phase) |
| **Required — Remove Unreleased state** | After the edit, `CHANGELOG.md` must carry a dated `## [1.0.0]` and no `[Unreleased]` stub holding release content. ☐ done |
| **Evidence — Approved document** | `CHANGELOG.md` diff/commit: ______ · `RELEASE_NOTES.md` diff/commit: ______ · Product approval comment: ______ · approver: ______ · date: ______ |
| **Result** | ☐ PASS ☐ FAIL ☐ BLOCKED |
| **Blocker (if any)** | (default) Applying the edit requires a commit; this phase (and 8.151) forbid commits. Needs a future authorized editing phase, then Product approval. |
| **Respondent** | ______________________ (name / role / date) |

---

## TASK B — PASS 1 EXIT CONDITION

| Outcome | Condition |
|---|---|
| **PASS 1 COMPLETE (PASS)** | B1 = PASS ∧ B7 = PASS ∧ B8 = PASS ∧ B-DOCS = PASS |
| **PASS 1 FAIL / INCOMPLETE** | Any of B1 / B7 / B8 / B-DOCS is FAIL, BLOCKED, WAITING, or IN PROGRESS — i.e. any missing approval, missing certificate, missing decision, or stale `[Unreleased]` |

**On PASS 1 COMPLETE:** unlock PASS 2 (B4 — `release.yml` run).
**On PASS 1 FAIL:** remain NO-GO; the named owner resolves the failing gate; re-run this execution doc.

---

## PASS 1 STATE (Phase 8.160)

```
Scope:    PASS 1 — Release Approval Package
Main:     77414de   (1.0.0, frozen)
Desktop:  READY ✅

B1     Code Signing      WAITING    Release Engineering   (no certificate)
B7     API Environment   WAITING    Product + DevOps      (no decision recorded)
B-DOCS Release Notes     WAITING    Team 3 → Product      (CHANGELOG still [Unreleased])
B8     Product Sign-off  BLOCKED    Product               (needs B1, B7, B-DOCS)

PASS 1 gates PASS: 0 / 4
PASS 1 RESULT:     FAIL / INCOMPLETE  —  no owner input received
NEXT:             PASS 2 remains locked; Release decision remains NO-GO
```
