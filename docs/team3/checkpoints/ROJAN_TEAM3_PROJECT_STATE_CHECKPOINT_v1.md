# ROJAN AI — TEAM 3 — PROJECT STATE CHECKPOINT v1

**Type:** Recovery document. No code changes, no commit performed in producing this file.
**Repo:** `C:\AndroidProjects\ROJAN_Desktop_team3` (backend reference: `C:\AndroidProjects\ROJAN_Backend`)
**Branch:** `feature/team3-desktop-completion`
**HEAD at time of writing:** `77414de` (`merge: supersede origin/main Service Catalog + Shift Engine fork` —
Phase 8.140 `-s ours` merge; parents `58a2c88` ^1 + `origin/main`/`53ae2fb` ^2; **tree byte-identical to
`58a2c88`**). **Phase 8.141: PUSHED — `origin/main` fast-forwarded `53ae2fb` → `77414de`;
`origin/feature/team3-desktop-completion` = `77414de`.** Prior code HEAD `58a2c88` (Phase 8.129, committed
8.131). Updated across phases from the original `801cc65`.
**The ViewModel diagnostic-logging architecture is CLOSED and RULE-CONSISTENT as of `5ba554c`** — every
ViewModel with a swallowing broad `catch` is instrumented (coverage 33/55, Wave 2D `6a1bced`), and every
ViewModel `[LoggerMessage]` is operation-name-only (legacy harmonization `5ba554c`). **The Missing-Guard
Sweep (Production Hardening reliability track) is now in progress — Wave A done (`794648e`), Wave B/HR done
(`a5be831`), Wave C/Inventory+Accounting done (`66c8490`), Wave D/Organization done (`525fd4b`), Reporting
mini-wave done (`5640123`), Export Dialog micro-phase done (`6f64ffa`) — Reporting domain fully closed —
Wave E/AI Center done (`4b1afca`), Wave F/Automation tabs done (`7c9c132`), Settings-page P2 carve-out done
(`0260bc3`) — **every backend-connected user-triggered command in the app is now guarded; the Missing-Guard
Sweep is effectively complete.** The **"sanitize load-error surfacing" P2 track is now COMPLETE** — all 58
Category-A `= exception.Message` UI surfaces across 30 VMs (`ROJAN_PHASE8_102_*`) are sanitized, over 6 domain
sub-waves: **sub-wave 1 (Reporting + AI Center + Accounting/POS, 11) `76d3f61` (8.104/8.106); sub-wave 2
(Customers + HR + Membership, 6/7) `1260d4e` (8.108/8.110) — `AcceptInviteViewModel` live invite-token /
email / user-id leak closed; sub-wave 3 (Organization + Specialists + Services, 8) `b509054` (8.112/8.114);
sub-wave 4 (Automation tabs, 13/13) `d10f9bc` (8.116 + 8.117.1, committed 8.119); sub-wave 5 (Booking +
Calendar + Inventory, 11/11) `71fb472` (8.121, committed 8.123) — `CreateBookingAsync` / `InitializeAsync`
backend-body leaks closed; sub-wave 6 / FINAL (Dashboard + Analytics + Salon + QR + Support +
`CustomerProfileViewModel`, 9/9) `17306d9` (8.125, committed 8.127) — Dashboard / Salon `CreateSalonAsync` /
QR `GenerateReceptionInviteAsync` backend leaks closed.** **The only `= exception.Message` left app-wide is the
2 `SettingsPageViewModel` `NotSupportedException`→`StatusMessage` Category-D branches — a hard-coded local
developer string ("…not available yet - Phase 19A ships the framework only"), NOT untrusted data; deliberately
left as-is (optional localization-polish follow-up only).**
Also remaining: the 3 local-only infra VMs (`WorkspaceHostViewModel` / `NotificationCenterViewModel` /
`CommandPaletteViewModel`) as documented **P3**. **The Phase 8.99.1 `SettingsPage.xaml` visibility follow-up
is DONE (`58a2c88`, Phase 8.129, committed 8.131)** — the 3 `*StatusMessage` TextBlocks now show on any
non-empty message (not just `Is*RestartRequired`), so the Phase 8.99 Settings-guard failure text is actually
visible. See §A / §F / §G.
**Working tree at time of writing:** 0 modified/deleted tracked files, untracked files are all `.md`
reports (this engagement's own audit trail, plus some pre-existing docs from other teams/phases). No
code is pending, no commit is pending.
**✅ MERGE COMPLETE (Phase 8.137 blocked → 8.138/8.139 reconciled → 8.140 `-s ours` → 8.141 pushed):** an
upstream divergence (`origin/main` moved to a parallel Service-Catalog + Shift-Engine fork `53ae2fb`) blocked
the FF at Phase 8.137. Phase 8.138/8.139 confirmed the fork is **superseded Phase-5 predecessor work by the
same author** (rebuilt post-`7103647` on the branch, functionally equivalent, nothing to port). Phase 8.140
`git merge -s ours origin/main` → merge commit **`77414de`** (parents `58a2c88` ^1 + `53ae2fb` ^2; **tree
byte-identical to `58a2c88`**). **Phase 8.141 PUSHED: `git push origin feature/team3-desktop-completion`
(`origin/feature/team3-desktop-completion` = `77414de`) then `git push origin 77414de:main` — plain
fast-forward `53ae2fb → 77414de`, no `--force`, no new merge commit.** `origin/main` now == `77414de`; tag
`v1.0.0` (`d518218`) untouched. **`main` carries the full Team 3 line (15 baseline + 30 hardening commits) +
the `-s ours` merge; NO fork code merged.** Post-merge validation: Debug+Release build 0/0, suite 2,715/2,715
both configs (0 skipped), Arch 7/7, `git diff 58a2c88 origin/main` empty. **Rollback point: `53ae2fb`.**
Local `main` in the primary worktree (`C:/AndroidProjects/ROJAN_Desktop`) is now stale (`b915e04`, 46 behind)
— its owner should `git pull` (outside this worktree's scope). Next: Phase 8.142 = audit-trail `docs/` commit
(`git add ROJAN_PHASE8_* ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` **only** — NOT `git add .`; 185 of 240
untracked `.md` are other teams'). Deliverable `ROJAN_PHASE8_141_MAIN_FAST_FORWARD_VALIDATION_REPORT_v1.md`.
(Historical detail below.)
**⚠️ (8.137 detail):** the
`feature/team3-desktop-completion` → `main` merge was **BLOCKED by an upstream divergence.** `origin/main` moved
from `d518218` (v1.0.0 tag) to **`53ae2fb`** on 2026-08-25/26 — a **3-commit divergent fork** by the same
developer (`meisamelh66`): `5ac87dc feat: complete service catalog management`, `92052c7 feat: implement
specialist shift engine integration`, `53ae2fb fix: harden specialist shift engine` — a **parallel Service
Catalog + `Schedule/` shift engine on PRE-refactor architecture** (it still has the local calendar command
authority the branch deliberately removed in `7103647`; +7 `ROJAN_PHASE5_*` reports = a separate line).
**Phase 8.138 analysis:** `origin/main` is **missing 45 commits** the branch has — 15 baseline
(calendar-authority removal, booking intelligence, HTTP observability + `LocalFileLoggerProvider`, eligibility
filtering, RBAC alignment, checkout hardening, specialist mgmt, auth UX, the branch's own newer Service
Catalog + `Application/Specialists/Schedule/` engine) + all 30 hardening commits. `53ae2fb` has **no downstream
refs**. Fast-forward impossible; ~30 conflicts, `SpecialistScheduleViewModel[.Tests]` add/add. **Canonical =
the Team 3 branch** (newer, complete, reviewed, correct architecture) — pending owner confirmation (same
author wrote both). **RECOMMENDATION: Option 3a — `git merge -s ours origin/main` onto the branch** (zero
regression: tree stays == `58a2c88`; zero conflicts; re-enables a fast-forward to `main`; records the
superseded fork). Reject Option 1 (rebase onto stale fork) and Option 2 (cherry-pick hardening only → silent
loss of 15 baseline commits). **Nothing executed; `58a2c88` unchanged; `origin/main` untouched at `53ae2fb`.**
Proposed: **8.139** = owner confirms canonical + scoped review of the 3 fork commits for orphan value;
**8.140** = execute 3a + re-verify (Debug+Release, expect 2,715/2,715); **8.141** = re-authorized `→ main`
fast-forward merge + validation. The branch `58a2c88` is the immutable canonical source of the 30 hardening
commits regardless.

**READ THIS FILE FIRST in any future session before continuing implementation work on this repo.**

---

## A. Project Current Status

This is a long-running Team 3 (Desktop Production Owner / Architecture Review Owner / Reliability
Owner — role has shifted by phase, always Team 3) engagement on the .NET 8 / WPF Desktop client,
following strict **Architecture First → Contract Second → Implementation Third** discipline, with every
substantive change going through an established, repeated rhythm:

```
Audit → Scope Review (readiness only, no commit) → Commit Execution → Fresh Validation
```

**As of this checkpoint:** all code-level remediation identified through Phase 7.4 is committed and
validated. Nine further hardening changes have since landed:
- **Phase 8.6 — Navigation BackStack Hardening** → `94fca6a` (executed Phase 8.8).
- **Phase 8.11 — ViewModel Diagnostic Logging, Wave 1** → `2453a7f` (executed Phase 8.13): `ILogger<T>`
  added to `DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel`.
- **Phase 8.15 — Mobile OTP Logging** → `31f4b63` (executed Phase 8.17): `MobileOtpLoginViewModel` logs
  its generic `ApiException` fallthrough at `Warning`, operation-name-only (no exception, no PII). This
  completed the Phase 8.2 named-ViewModel logging set.
- **Phase 8.19 — ViewModel Logging Wave 2A** → `75357e1` (executed Phase 8.21): `ILogger<T>` added to
  `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`,
  `ReportingPageViewModel` — `Error` level, operation-name-only (exception never passed).
- **Phase 8.23 — ViewModel Logging Wave 2B** → `2ed685a` (executed Phase 8.25): `AnalyticsPageViewModel`,
  `AiCenterPageViewModel` (incl. the chat boundary — no chat-text leak, test-enforced), `SalonPageViewModel`,
  `QrCodesPageViewModel` — same pattern.
- **Phase 8.27 — Organization Page Logging** → `cbc3a82` (executed Phase 8.29): `OrganizationPageViewModel`
  (`LoadAsync` boundary); shipped with the first dedicated `OrganizationPageViewModelTests.cs`.
- **Phase 8.31 — Support Page Logging** (Wave 2C-1, first half) → `0542041` (executed Phase 8.33):
  `SupportPageViewModel` (`SubmitMessageAsync` / `SubmitApplicationAsync` boundaries); PII-safe,
  test-enforced.
- **Phase 8.35 — AcceptInvite Security Logging** (Wave 2C-1, second half) → `38c24da` (executed
  Phase 8.37): `AcceptInviteViewModel` (`LookupAsync` / `AcceptAsync` boundaries); **token-safe +
  identity-safe, test-enforced**. Completes Wave 2C-1.
- **Phase 8.39 — Automation Tabs Logging** (Wave 2C-2) → `c01d0ce` (executed Phase 8.41):
  `ILogger<T>` added to all 5 Automation tab VMs (`AutomationDashboardTabViewModel`,
  `ApprovalsTabViewModel`, `BusinessRulesTabViewModel`, `ScheduledJobsTabViewModel`,
  `WorkflowsTabViewModel` — 13 broad catches), with **parent→child logger plumbing** through
  `AutomationPageViewModel` (5 optional nullable `ILogger<TChild>?` pass-through params, forwarded to
  each `new`). `Error` level, operation-name-only, exception never passed, test-enforced. First full
  application of the pass-through pattern to a `new`-by-parent child set. Completes Wave 2C-2.
  (First commit `b643adc` had a malformed subject from a shell here-string bug; corrected via one
  authorized message-only `git commit --amend` → `c01d0ce`, no content change.)
- **Phase 8.43 — Profile Panels Logging** (Wave 2C-3a) → `7aa1d1b` (executed Phase 8.45):
  `ILogger<T>` added to `CustomerProfileViewModel` (1 catch: `LoadAsync`), `ServiceProfileViewModel`
  (3: `LoadAsync`/`SaveChangesAsync`/`DeactivateAsync`), `InventoryProfileViewModel` (1: `LoadAsync`)
  via the `sealed partial` + optional-ctor-param + `NullLogger<T>` + instance-form `[LoggerMessage]`
  pattern. **Parent→child plumbing** through `CustomerPageViewModel`, `ServicePageViewModel`,
  `InventoryPageViewModel` uses **`ILoggerFactory? loggerFactory = null`** (not a 2nd `ILogger<TChild>`
  field — these 3 parents already carry `ILogger<TSelf> _logger` + an instance-form `[LoggerMessage]`
  from Wave 2A, so a 2nd `ILogger` field would trip `SYSLIB1020`); `_loggerFactory?.CreateLogger<TChild>()`
  at the child `new` site. `Error` level, operation-name-only, exception never passed, test-enforced.
  6 production + 6 test files + 1 new test helper (`RecordingLoggerFactory.cs`); **+11 tests**. No DI /
  interface / shared-stub change. Full detail in `ROJAN_PHASE8_45_PROFILE_LOGGING_COMMIT_REPORT_v1.md`.
- **Phase 8.47 — BookingWizard Logging** (Wave 2C-3b) → `884cec3` (executed Phase 8.49):
  `ILogger<BookingWizardViewModel>` added via the `sealed partial` + optional-ctor-param (appended after
  `Action? onBookingCreated = null`) + `NullLogger<T>` + instance-form `[LoggerMessage(EventId=1,
  Level=Error, "Booking wizard operation failed. Operation={Operation}")]` pattern — **4 of 5** catches
  instrumented (`LoadOptionsAsync`, `AddGuestCustomerAsync`, `LoadAvailableSlotsAsync`,
  `ConfirmBookingAsync`); **`SearchNextAvailableDateAsync` deliberately NOT instrumented** (best-effort
  cancellable probe, swallowed by design, never mutates `ErrorMessage`/`State` — byte-unchanged,
  test-guarded). **Parent plumbing** through `BookingPageViewModel` uses **`ILoggerFactory? loggerFactory
  = null`** (appended after its existing `logger`); it already carries `ILogger<BookingPageViewModel>
  _logger` + the **legacy `(string operation, Exception exception)`-form `[LoggerMessage]`** from
  `da18c18` — both left untouched, `ILoggerFactory` is not `ILogger` so no `SYSLIB1020`.
  `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` at `OpenWizard()`. `Error` level,
  operation-name-only, **exception never passed** — guest name/phone, booking notes, slot times,
  customer/service/specialist data all stay out; test-enforced with seeded secrets. 2 production + 2 test
  files, **+7 tests**; reused `RecordingLogger<T>` + `RecordingLoggerFactory` (no new helper). No DI /
  interface / shared-stub change. Full detail in `ROJAN_PHASE8_49_BOOKINGWIZARD_LOGGING_COMMIT_REPORT_v1.md`.
- **Phase 8.51 — Detail Panels Logging** (Wave 2C-3c) → `5b7f6ca` (executed Phase 8.53): `ILogger<T>`
  added to `EmployeeProfileViewModel` (1 catch: `LoadAsync`), `InvoiceProfileViewModel` (1: `LoadAsync`),
  `SpecialistProfileViewModel` (4: `LoadAsync` / `SaveChangesAsync` / `AssignServiceAsync` /
  `RemoveServiceAssignmentAsync`) via the `sealed partial` + optional-ctor-param + `NullLogger<T>` +
  instance-form `[LoggerMessage(EventId=1, Level=Error)]` pattern — **6 call sites**. **Parent plumbing**
  through `HrPageViewModel`, `AccountingPageViewModel`, `SpecialistPageViewModel` uses **`ILoggerFactory?
  loggerFactory = null`** (appended after each parent's previously-last param); `_loggerFactory?.CreateLogger<TChild>()`
  at the child `new` site. `ILoggerFactory` (not a 2nd `ILogger` field) for all three: HrPage already has
  `ILogger<HrPageViewModel>` + instance `[LoggerMessage]` (2nd field → `SYSLIB1020`); AccountingPage has 2
  `ILogger` fields + static-form `[LoggerMessage]` + `_posCheckoutLogger` (kept untouched); SpecialistPage
  has 2 typed grandchild-logger fields `_scheduleLogger`/`_availabilityLogger` (kept untouched, factory
  future-proofs the Wave 2D `SYSLIB1020` risk). `SpecialistProfileViewModel`'s own `scheduleLogger` /
  `availabilityLogger` remain ctor *params* passed to the grandchildren — not fields, so its single new
  `_logger` field is `SYSLIB1020`-safe. `Error` level, operation-name-only, **exception never passed** —
  employee salary/commission, invoice amounts/payments/receipts, specialist email/phone/bio/performance
  all stay out; test-enforced with seeded secrets. 6 production + 6 test files, **+12 tests** (6 boundary
  failure-logs + 3 NullLogger + 3 parent factory forwarding); reused `RecordingLogger<T>` +
  `RecordingLoggerFactory` (no new helper), **no shared-stub change** (`StubSpecialistCommandService`
  already had `AssignServiceException` / `RemoveServiceAssignmentException` / `UpdateSpecialistException`
  hooks). No DI / interface change. `SaveChangesAsync` was added via the Phase 8.51 Scope Correction
  Authorization (the original 8.51 explicit list accidentally omitted the 6th audited boundary). Full
  detail in `ROJAN_PHASE8_53_DETAIL_PANELS_LOGGING_COMMIT_REPORT_v1.md`.
- **Phase 8.56 — SpecialistPage Logging** (Wave 2D / final P1) → `6a1bced` (executed Phase 8.58):
  `SpecialistPageViewModel.LoadAsync` — the **last uninstrumented swallowing broad `catch (Exception)`**
  in the Presentation layer. `sealed class` → `sealed partial class`; **static-form** `[LoggerMessage]`
  `private static partial void LogOperationFailed(ILogger logger, string operation)` — **no `Exception`
  parameter**. Logger derived **inline** at the call site from the `ILoggerFactory` the class already
  takes (Phase 8.51): `_loggerFactory?.CreateLogger<SpecialistPageViewModel>() ?? NullLogger<…>.Instance`
  — **no new `ILogger<SpecialistPageViewModel>` field, no new ctor param**. Static form because the class
  already holds 2 `ILogger` fields (`_scheduleLogger`, `_availabilityLogger`, forwarded to the profile
  child's grandchildren) — an instance-form `[LoggerMessage]` + a 3rd `ILogger` field would trip
  `SYSLIB1020`; static form is field-count-agnostic (`AccountingPageViewModel` precedent). 1 call, inside
  the existing `if (requestVersion == _filterVersion)` staleness guard, after the unchanged
  `ErrorMessage`/`State`. `Error` level, operation-name-only — no specialist name/email/phone/bio, no
  search-filter text, no backend body; test-enforced. 1 production + 1 test file, **+3 tests** (failure
  logs operation-only/no-PII-leak, NullLogger safety, stale-request-logs-nothing). Reused
  `RecordingLogger<T>` + `RecordingLoggerFactory`, no shared-stub change, no interface / DI change. Full
  detail in `ROJAN_PHASE8_58_SPECIALIST_PAGE_LOGGING_COMMIT_REPORT_v1.md`.

Phases 7.5–8.5, 8.9–8.10/8.12, 8.14/8.16, 8.18/8.20, 8.22/8.24, 8.26/8.28, 8.30/8.32, 8.34/8.36,
8.38/8.40, 8.42, 8.44, 8.46, 8.48, 8.50, 8.52, 8.54, 8.55, 8.57, 8.59/8.60, 8.62, 8.64/8.65, 8.67,
8.69/8.71, 8.73/8.75, 8.77/8.79, 8.81/8.83, 8.85/8.87, 8.89/8.91, 8.93, 8.95, 8.97, 8.98, 8.100, 8.102, 8.103, 8.105, 8.107, 8.109, 8.111, 8.113, 8.115, 8.117, 8.118, 8.120, 8.122, 8.124, 8.126, 8.128, 8.130, 8.132, 8.133, 8.134, 8.135, 8.136, 8.138, 8.139 were pure audit/planning/scope-review; 8.137 was a merge attempt aborted at pre-verification; 8.140 executed the `-s ours` merge (tree unchanged); **8.141 pushed — `origin/main` = `77414de`.** The Missing-Guard Sweep is the active track —
Wave A `794648e` (Phase 8.66/8.68), Wave B/HR `a5be831` (Phase 8.70/8.72), Wave C/Inventory+Accounting
`66c8490` (Phase 8.74/8.76), Wave D/Organization `525fd4b` (Phase 8.78/8.80), Reporting mini-wave `5640123`
(Phase 8.82/8.84), Export Dialog micro-phase `6f64ffa` (Phase 8.86/8.88), Wave E/AI Center `4b1afca`
(Phase 8.90/8.92), Wave F/Automation tabs `7c9c132` (Phase 8.94+8.94.1, committed 8.96), Settings-page
P2 carve-out `0260bc3` (Phase 8.99, committed 8.101) done — **every backend-connected user-triggered command
is guarded; the Missing-Guard Sweep is complete.** The **"sanitize load-error surfacing" P2** is the active
track (`ROJAN_PHASE8_102_*`): sub-wave 1 (Reporting/AI Center/Accounting+POS, 11 sites) `76d3f61` (Phase 8.104,
committed 8.106) done; sub-wave 2 (Customers/HR/Membership, 6/7 sites) `1260d4e` (Phase 8.108, committed
8.110) done; sub-wave 3 (Organization/Specialists/Services, 8 sites) `b509054` (Phase 8.112, committed
8.114) done; sub-wave 4 (Automation tabs, 13/13 sites) `d10f9bc` (Phase 8.116 + 8.117.1, committed 8.119)
done; sub-wave 5 (Booking + Calendar + Inventory, 11/11 sites) `71fb472` (Phase 8.121, committed 8.123)
done; sub-wave 6 / FINAL (Dashboard + Analytics + Salon + QR + Support + `CustomerProfileViewModel`, 9/9
sites) `17306d9` (Phase 8.125, committed 8.127) done — **P2 track complete, all 58 Category-A sites
sanitized** (only the 2 Settings `NotSupportedException` Category-D branches deliberately left). See §G.

---

## B. Completed Commits Table

All on `feature/team3-desktop-completion`, none pushed, none merged, in chronological order:

| Commit | Message | Phase |
|---|---|---|
| `f691dea` | `feat(desktop): complete specialist schedule shift engine` | Shift Engine Implementation |
| `ea03d83` | `fix(desktop): harden shift engine error diagnostics` | Shift Engine Hardening |
| `da18c18` | `fix(desktop): harden booking and checkout error handling` | Booking/Checkout Hardening |
| `53090c1` | `fix(desktop): register specialist schedule services in DI` | Shift Engine DI Fix |
| `c59d7c0` | `fix(desktop): align permissions with backend authority` | RBAC Alignment |
| `7103647` | `refactor(desktop): remove local calendar authority` | Calendar Authority Cleanup |
| `801cc65` | `fix(desktop): improve authentication error handling UX` | Authentication UX Hardening |
| `94fca6a` | `fix(desktop): bound navigation back-stack depth` | Phase 8.6 Navigation BackStack Hardening (committed at Phase 8.8) |
| `2453a7f` | `fix(desktop): add ViewModel diagnostic logging (wave 1)` | Phase 8.11 ViewModel Logging Wave 1 (committed at Phase 8.13) |
| `31f4b63` | `fix(desktop): log unexpected OTP API failures` | Phase 8.15 Mobile OTP Logging (committed at Phase 8.17) |
| `75357e1` | `fix(desktop): add ViewModel diagnostic logging (wave 2a)` | Phase 8.19 Logging Wave 2A (committed at Phase 8.21) |
| `2ed685a` | `fix(desktop): add ViewModel diagnostic logging (wave 2b)` | Phase 8.23 Logging Wave 2B (committed at Phase 8.25) |
| `cbc3a82` | `fix(desktop): add ViewModel diagnostic logging (organization page)` | Phase 8.27 Organization Page Logging (committed at Phase 8.29) |
| `0542041` | `fix(desktop): add ViewModel diagnostic logging (support page)` | Phase 8.31 Support Page Logging / Wave 2C-1a (committed at Phase 8.33) |
| `38c24da` | `fix(desktop): log invite lookup and accept failures` | Phase 8.35 AcceptInvite Security Logging / Wave 2C-1b (committed at Phase 8.37) |
| `c01d0ce` | `fix(desktop): add ViewModel diagnostic logging (automation tabs)` | Phase 8.39 Automation Tabs Logging / Wave 2C-2 (committed at Phase 8.41; message-only amend of `b643adc`) |
| `7aa1d1b` | `fix(desktop): add ViewModel diagnostic logging (profile panels)` | Phase 8.43 Profile Panels Logging / Wave 2C-3a (committed at Phase 8.45) |
| `884cec3` | `fix(desktop): add ViewModel diagnostic logging (booking wizard)` | Phase 8.47 BookingWizard Logging / Wave 2C-3b (committed at Phase 8.49) |
| `5b7f6ca` | `fix(desktop): add ViewModel diagnostic logging (detail panels)` | Phase 8.51 Detail Panels Logging / Wave 2C-3c (committed at Phase 8.53) |
| `6a1bced` | `fix(desktop): add ViewModel diagnostic logging (specialist page)` | Phase 8.56 SpecialistPage Logging / Wave 2D final P1 (committed at Phase 8.58) — **logging track CLOSED** |
| `5ba554c` | `fix(desktop): drop exception payload from diagnostic logging` | Phase 8.61 Legacy `[LoggerMessage]` Harmonization (committed at Phase 8.63) — **logging architecture CLOSED & rule-consistent** |
| `794648e` | `fix(desktop): guard customer/service/specialist command failures` | Phase 8.66 Missing-Guard Sweep Wave A (committed at Phase 8.68) — 12 backend-connected write commands guarded |
| `a5be831` | `fix(desktop): guard HR command failures` | Phase 8.70 Missing-Guard Sweep Wave B / HR (committed at Phase 8.72) — 13 HR command methods guarded |
| `66c8490` | `fix(desktop): guard inventory and invoice-cancel command failures` | Phase 8.74 Missing-Guard Sweep Wave C / Inventory+Accounting (committed at Phase 8.76) — 6 Inventory + 1 invoice-cancel command guarded |
| `525fd4b` | `fix(desktop): guard organization command failures` | Phase 8.78 Missing-Guard Sweep Wave D / Organization (committed at Phase 8.80) — 4 command + 2 secondary-load setter-path guards; `SwitchRoleAsync` role-picker revert |
| `5640123` | `fix(desktop): guard reporting command failures` | Phase 8.82 Missing-Guard Sweep Reporting mini-wave (committed at Phase 8.84) — `ReportingPageViewModel.ToggleSavedAsync` + `DeleteSnapshotAsync` guarded; non-destructive `ActionErrorMessage` (leaves `StatusMessage` intact) |
| `6f64ffa` | `fix(desktop): guard report export failures` | Phase 8.86 Missing-Guard Sweep Export Dialog micro-phase (committed at Phase 8.88) — `ExportDialogViewModel.ExportAsync` guarded; `sealed partial` + injected `ILoggerFactory?` + operation-only `[LoggerMessage]`; 4-line `ReportingPageViewModel` factory forwarding. **Reporting domain fully closed.** |
| `4b1afca` | `fix(desktop): guard AI Center command failures` | Phase 8.90 Missing-Guard Sweep Wave E / AI Center (committed at Phase 8.92) — 9 `AiCenterPageViewModel` command methods guarded; non-destructive `ActionErrorMessage`; `CurrentSessionId`/`SelectedSection` not reset on failure; `ExportSessionAsync` leaves no partial transcript. **AI Center domain fully closed.** |
| `7c9c132` | `fix(desktop): guard remaining automation tab command failures` | Phase 8.94 (+ 8.94.1 toggle correction) Missing-Guard Sweep Wave F / Automation tabs (committed at Phase 8.96) — 7 guards across `WorkflowsTabViewModel` (`ArchiveAsync`/`DeleteAsync`/`LoadVersionHistoryAsync`), `ScheduledJobsTabViewModel` (`DeleteAsync`/`ToggleEnabledAsync`), `BusinessRulesTabViewModel` (`ToggleEnabledAsync`/`DeleteAsync`); **filtered** `catch (Exception exception) when (exception is not OperationCanceledException)` (Phase 8.39 shape) reusing the existing `ErrorMessage` property + generic `Common_ActionFailedMessage` (no `exception.Message`, no `State=Error`, no `ActionErrorMessage`); `LoadVersionHistoryAsync` also clears `ErrorMessage` on success. +10 tests, +7 additive `Exception?` stub seams. **Automation user-triggered command guard coverage now complete (19/19).** |
| `0260bc3` | `fix(desktop): guard settings page command failures` | Phase 8.99 Missing-Guard Sweep Settings-page P2 carve-out (audited 8.97/8.98, committed at Phase 8.101) — 6 `SettingsPageViewModel` commands guarded (`ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `DownloadOrInstallAsync`, `RemovePackAsync`, `SignOutAsync`); filtered catch, reuse the 3 existing section-scoped `*StatusMessage` surfaces + new `AccountStatusMessage` (SignOut) with 1 XAML `<TextBlock>`; generic `Common_ActionFailedMessage` (no `exception.Message`, **no API URL leak**, no `State=Error`); `NotSupportedException` branches on Download/Remove kept. `sealed`→`sealed partial` + optional `ILogger<SettingsPageViewModel>? = null` (NullLogger fallback, **no DI change**, no ctor break) + one instance `[LoggerMessage]`. `ApplyLanguageAsync` was outside this phase's list. +9 tests, +4 additive stub seams. **Known non-blocking follow-up:** the 3 existing `*StatusMessage` TextBlocks are visibility-gated on `Is*RestartRequired`, so failure text is set but not visually shown for Theme/API/pack-refresh → Phase 8.99.1 XAML tweak. **Every backend-connected user-triggered command is now guarded.** |
| `76d3f61` | `fix(desktop): sanitize reporting, AI center and accounting error surfacing` | Phase 8.104 "sanitize load-error surfacing" P2 sub-wave 1 (audited 8.102/8.103, committed at Phase 8.106) — 11 pre-existing top-level broad-catch UI surfaces across `ReportingPageViewModel` (`LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync`), `AiCenterPageViewModel` (`LoadAsync` / `SendMessageAsync`), `AccountingPageViewModel` (`LoadAsync` / `SearchAsync`), `PosCheckoutViewModel` (`LoadOptionsAsync` / `ProceedToPaymentAsync` / `ChargeAsync`), `InvoiceProfileViewModel` (`LoadAsync`): `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = DashboardState.Error`, the `RunReportAsync` `OperationCanceledException` branch, every `finally`, and every operation-name-only `[LoggerMessage]` call are byte-unchanged. `PosCheckoutViewModel` + `InvoiceProfileViewModel` each `+ using …Localization;`. **No** localization / DI / service / contract / stub change. +3 net tests (sentinel-enforced — customer names, revenue figures, payment-gateway detail); the **confirmed live `SendMessageAsync` customer-name leak** (`StatusMessage` showed `"…for customer Sarah Johnson"`) is now closed. |
| `1260d4e` | `fix(desktop): sanitize customer, HR and membership error surfacing` | Phase 8.108 "sanitize load-error surfacing" P2 sub-wave 2 (audited 8.107, committed at Phase 8.110) — 6 of the 7 audited sites: `CustomerPageViewModel.LoadAsync`, `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync`, `AcceptInviteViewModel.LookupAsync` (`LookupErrorMessage`) / `.AcceptAsync` (`AcceptErrorMessage`). Same swap: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }`. `State = DashboardState.Error`, the `Has*Error` flags, the `CustomerPageViewModel` stale-response guard, the `HrPageViewModel.SearchAsync` out-of-order guard, both `AcceptInviteViewModel` `finally` blocks, and every operation-name-only `[LoggerMessage]` call are byte-unchanged. **No `using` additions in prod** (all 4 already import `…Localization`); no localization / DI / service / contract / stub change. +1 net test (`HrPageViewModelTests.SearchAsync_QueryThrows_…`). **Security:** `AcceptInviteViewModel`'s **live, test-documented invite-token leak** (`AcceptInviteViewModelTests:144` used to assert `Contains(SecretToken, sut.LookupErrorMessage!)` — *"the user still sees the raw backend message"*) plus the undetected `AcceptErrorMessage` token / invitee-email / user-id leaks are **closed**; customer PII and salary/payroll figures no longer reach any UI surface. **Deferred:** `CustomerProfileViewModel.LoadAsync` (site 7 of 7) was outside this phase's authorised file list — remains for a follow-up. |
| `b509054` | `fix(desktop): sanitize organization specialists and services error surfacing` | Phase 8.112 "sanitize load-error surfacing" P2 sub-wave 3 (audited 8.111, committed at Phase 8.114) — 8 sites / 7 VMs: `OrganizationPageViewModel.LoadAsync`, `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync`, `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync` (the shared 8-caller mutation boundary), `SpecialistAvailabilityViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync`, `ServiceProfileViewModel.LoadAsync`. Same swap. `State = DashboardState.Error`, **both `catch (UnauthorizedOperationException)` typed branches** in `SpecialistScheduleViewModel` (`IsPermissionDenied` + Warning log), the `[CallerMemberName] operationName` parameter, `TryMutateAsync`'s success-path `ErrorMessage = null` clearing, the `SpecialistPageViewModel` / `ServicePageViewModel` stale-response guards, and every operation-name-only `[LoggerMessage]` call (`LogOperationFailed` / static-form `LogOperationFailed(Logger, …)` / `LogLoadFailed` / `LogPermissionDenied`) are byte-unchanged. `+ using …Localization;` in 1 prod (`SpecialistAvailabilityViewModel`) + 3 test files; `OrganizationPageViewModel` keeps its fully-qualified `Rojan.Desktop.Presentation.Localization.Strings.` form. No `.resx` / DI / service / contract / stub change. 7 prod + 7 existing test files, **+1 net test** (`SpecialistScheduleViewModelTests.SetWeeklyAvailabilityCommand_BackendThrows_…` covering the `TryMutateAsync` boundary; Presentation 771 → 772). **Security:** RBAC role/permission strings, staff PII, specialist identifiers, availability windows, and service pricing / cost / commission % no longer reach any UI surface; sentinel-enforced. |
| `d10f9bc` | `fix(desktop): sanitize automation tab error surfacing` | Phase 8.116 + Phase 8.117.1 addendum "sanitize load-error surfacing" P2 sub-wave 4 (audited 8.115; scope-reviewed 8.117 / 8.118; committed at Phase 8.119) — **all 13 filtered-catch UI surfaces across the 5 Automation tab VMs**: `WorkflowsTabViewModel` (`LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync`), `ScheduledJobsTabViewModel` (`LoadAsync` / `CreateAsync` / `RunNowAsync`), `BusinessRulesTabViewModel` (`LoadAsync` / `CreateAsync`), `ApprovalsTabViewModel` (`LoadAsync` / `DecideAsync`), `AutomationDashboardTabViewModel` (`LoadAsync`). Each: **only** the surface line changed — `ErrorMessage = exception.Message;` → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`. The `catch (Exception exception) when (exception is not OperationCanceledException)` clause (Phase 8.39 filtered shape — the `when` predicate keeps the `exception` variable bound; unused-but-no-warning), every `State = DashboardState.Error`, every `LogOperationFailed(nameof(<Method>))`, both `[LoggerMessage]` signatures, and the `await LoadAsync()` success-path reloads are byte-unchanged. **No `using` additions in prod** (all 5 use the fully-qualified `Localization.Strings.` form); 2 test files gained `+ using …Localization;`. No `.resx` / DI / service / contract / stub change. 5 prod + 5 existing test files, **+0 net tests** (13 surface no-leak assertions — `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain(Secret, …)` — added to the existing Phase 8.39 failure tests; `WorkflowsTabViewModelTests` gained a private `AssertGenericSurfaceNoLeak` helper). **Security:** workflow definitions (step names/descriptions/triggers), cron expressions, business-rule conditions/actions, approval decision comments (payroll / disciplinary / PII), dashboard workflow names, execution details, and backend payloads no longer reach any UI surface; sentinel-enforced. Logs remain operation-name-only. **Sub-wave 4 complete — 13/13.** Separate from Missing-Guard Wave F (`7c9c132`), which added *new* guards to the same files; this changes only the *message string* in *pre-existing* filtered catches. |
| `71fb472` | `fix(desktop): sanitize booking calendar inventory error surfacing` | Phase 8.121 "sanitize load-error surfacing" P2 sub-wave 5 (audited 8.120; scope-reviewed 8.122; committed at Phase 8.123) — **11 plain broad-catch UI surfaces / 4 VMs**: `BookingPageViewModel` (`LoadAsync` / `CreateBookingAsync` / `ChangeStatusAsync` / `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync`), `CalendarPageViewModel` (`InitializeAsync` / `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync`), `InventoryPageViewModel` (`LoadAsync` / `SearchAsync`), `InventoryProfileViewModel` (`LoadAsync`). Each: `catch (Exception exception)` → `catch (Exception)` (variable dropped — referenced only for `.Message`), `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`. The `#pragma warning disable/restore CA1031` pair, every `State = DashboardState.Error`, every operation-name-only log call (`LogOperationFailed` / `LogLoadFailed(nameof(<Method>))`), the `BookingPageViewModel.LoadAsync` stale-response `if (requestVersion == _filterVersion)` guard, the `InventoryPageViewModel.SearchAsync` out-of-order `if (string.Equals(searchText, SearchText, Ordinal))` guard, the 4 Booking-command `await LoadAsync()` reloads, and the Calendar null guards are byte-unchanged. `+ using Rojan.Desktop.Presentation.Localization;` in 2 prod (`BookingPageViewModel`, `CalendarPageViewModel`) + 2 test files; the 2 Inventory VMs + tests already imported it (Wave C `66c8490`). No `.resx` / DI / service / contract / stub change. 4 prod + 4 existing test files, **+0 net tests** (11 existing raw-message failure-test assertions updated to `Strings.Common_ActionFailedMessage`; 3 `DoesNotContain` sentinel additions). **Security:** customer names / appointment times / specialist assignments, staff schedules & availability, and stock levels / supplier names+terms / cost prices / transaction history no longer reach any UI surface; sentinel-enforced. **Two confirmed live test-documented backend-body leaks closed** — `BookingPageViewModel.CreateBookingAsync` and `CalendarPageViewModel.InitializeAsync` previously had tests asserting `Assert.Equal(backendBody, sut.ErrorMessage)`. Logs remain operation-name-only. **Sub-wave 5 complete — 11/11.** |
| `17306d9` | `fix(desktop): sanitize dashboard analytics salon qr support errors` | Phase 8.125 "sanitize load-error surfacing" P2 sub-wave 6 / FINAL (audited 8.124; scope-reviewed 8.126; committed at Phase 8.127) — **the last 9 Category-A UI surfaces / 6 VMs**: `DashboardPageViewModel.LoadAsync`, `AnalyticsPageViewModel.LoadAsync`, `SalonPageViewModel` (`LoadAsync` / `CreateSalonAsync`), `QrCodesPageViewModel` (`LoadAsync` / `GenerateReceptionInviteAsync`), `SupportPageViewModel` (`SubmitMessageAsync` / `SubmitApplicationAsync`), `CustomerProfileViewModel.LoadAsync` (carried over from sub-wave 2). 7 plain `catch (Exception exception)` → `catch (Exception)` + `= exception.Message;` → `= Strings.Common_ActionFailedMessage;`; the 2 Support catches are the filtered `catch (Exception exception) when (exception is not OperationCanceledException)` shape — the `when` clause + `exception` variable are byte-unchanged, only the assignment swapped (`= Localization.Strings.Common_ActionFailedMessage;`, FQ). The `#pragma warning disable/restore CA1031` pairs, every `State = DashboardState.Error` (5 sites), every operation-name-only log call (`LogLoadFailed` / `LogOperationFailed(nameof(<Method>))`), the `SalonPageViewModel.CreateSalonAsync` + `QrCodesPageViewModel.GenerateReceptionInviteAsync` `finally` blocks, the QR `if (Salon is null) return;` guard, the Support `MessageStatus`/`ApplicationStatus` resets + 11-field success-path form-clears, and the Dashboard financial-KPI gating are byte-unchanged. `+ using Rojan.Desktop.Presentation.Localization;` in 3 prod (`Analytics`, `Salons`, `QrCodes`) + 5 test files; `Dashboard` / `CustomerProfile` prod + `CustomerProfile` test already imported it; `SupportPageViewModel` prod keeps the FQ form. No `.resx` / DI / service / contract / stub change. 6 prod + 6 existing test files, **+0 net tests** (~14 raw-message failure-test assertions updated to `Strings.Common_ActionFailedMessage`; 5 `DoesNotContain` sentinel additions). **Security:** Dashboard revenue / financial KPIs, analytics insights (revenue trends / retention / spend), salon configuration + owner-contact PII, QR **invite tokens / invite ids**, Support **applicant PII** (name / mobile / email / résumé URL), and **customer PII** (name / email / phone) + notes + full appointment history no longer reach any UI surface; sentinel-enforced. **Three confirmed live test-documented backend leaks closed** — `DashboardPageViewModel.LoadAsync` (`Assert.Equal(backendBody, …)`), `SalonPageViewModel.CreateSalonAsync` (`Assert.Equal("Validation failed", …)`), `QrCodesPageViewModel.GenerateReceptionInviteAsync` (`Assert.Equal("Forbidden", …)`). Logs remain operation-name-only. **Sub-wave 6 complete — 9/9. The P2 track is COMPLETE — all 58 Category-A `= exception.Message` UI surfaces sanitized.** The 2 `SettingsPageViewModel` `NotSupportedException`→`StatusMessage` branches are Category-D (local fixed developer string from `LocalOnlyLanguagePackRepository`) — deliberately excluded, NOT a security leak. |
| `58a2c88` | `fix(desktop): fix settings error message visibility` | Phase 8.129 Settings UX visibility fix (the Phase 8.99.1 follow-up; scope-reviewed 8.130; committed at Phase 8.131) — **XAML only, 1 file** (`Views/Settings/SettingsPage.xaml`, +16/−36). The 3 section `*StatusMessage` `TextBlock`s (Language/Packs `StatusMessage`, Theme `ThemeStatusMessage`, API-env `ApiEnvironmentStatusMessage`) had their visibility gate switched from a `<TextBlock.Style>` `DataTrigger Binding="{Binding Is*RestartRequired}" Value="True"` to an inline `Visibility="{Binding <StatusMessage>, Converter={StaticResource CollectionToVisibilityConverter}}"` — the non-empty-string pattern already used by `AccountStatusMessage`. Now the failure text the Phase 8.99 Settings guards (`0260bc3`) already set (`Common_ActionFailedMessage` on Theme / API-env / pack refresh / download / remove failure, plus the pack `NotSupportedException` "coming soon" string) is **actually shown** — previously it was assigned but the `TextBlock` stayed `Collapsed` because `Is*RestartRequired` is only ever `true` after a *successful* change needing a relaunch. The 3 "Restart Now" `Button`s keep their `Is*RestartRequired` `DataTrigger` gate (a relaunch button must only appear on a real pending relaunch). `CollectionToVisibilityConverter` was already a declared page resource. **No ViewModel / service / contract / DI / `.resx` / test change; +0 tests.** No behaviour regression — success+restart still Visible, success+no-restart still Collapsed (`*StatusMessage` is `string.Empty` on those paths, per `SettingsPageViewModelTests`). **Non-blocker noted:** the API-Environment "Restart Now" button uses `Settings_Theme_RestartNow` as its label (pre-existing mislabel, not touched — candidate for a separate 1-line follow-up). |

**Phase 8.6 detail:** `NavigationService._backStack` capped at 20 entries with FIFO (oldest-first)
eviction; bare `Stack<T>` replaced with a `LinkedList<T>` deque for O(1) evict-from-bottom.
`INavigationService` and all call sites unchanged; `_forwardStack` left as-is (self-bounding). One
production file + its test file; 5 new tests (cap respected, FIFO eviction order, `WeakReference`-proven
release, GoBack-after-eviction, GoForward regression). Full detail in
`ROJAN_PHASE8_8_NAVIGATION_BACKSTACK_COMMIT_REPORT_v1.md`.

**Phase 8.11 detail:** `ILogger<T>` added to `DashboardPageViewModel` (1 load boundary),
`CalendarPageViewModel` (3 — Initialize/Daily/Weekly), `AccountingPageViewModel` (2 — Load/Search) via
the established optional-ctor-param + `NullLogger<T>` + `[LoggerMessage]` pattern. Each swallowed
broad-catch now logs at `Error` **after** the unchanged `ErrorMessage`/`State` assignment — no DI,
interface, or behaviour change. 3 production + 3 test files; 9 new tests. Accounting uses the static-form
`[LoggerMessage]` (two logger fields). Full detail in `ROJAN_PHASE8_13_LOGGING_COMMIT_REPORT_v1.md`.

**Phase 8.15 detail:** `MobileOtpLoginViewModel` — `ILogger<T>` added; the generic `catch (ApiException)`
fallthrough of the request/resend/verify flows logs at **`Warning`**, **operation name only**. The
exception is deliberately **never passed** (the OTP client `AuthBootstrapHttpClient` embeds the raw
backend response body in `ApiException` messages). Typed/expected failures (rate-limit, connectivity,
timeout, auth-rejection 401/403) and validation paths log nothing. 1 production + 1 test file; 7 new
tests, two of which assert phone/code/response-body are absent from the log line. No DI, interface, or
auth-flow change. Full detail in `ROJAN_PHASE8_17_MOBILE_OTP_LOGGING_COMMIT_REPORT_v1.md`.

**Phase 8.19 detail:** `ILogger<T>` added to 5 `AddTransient` page ViewModels — `CustomerPageViewModel`
(1 boundary), `ServicePageViewModel` (3 — Load / LoadCategories [previously a **silent** swallow] /
CreateService), `InventoryPageViewModel` (2), `HrPageViewModel` (2), `ReportingPageViewModel` (3 —
Load / RunReport / RerunSnapshot). Same optional-ctor-param + `NullLogger<T>` + `[LoggerMessage]` pattern;
`Error` level; **operation-name-only, the exception is never passed** (per the wave's SECURITY rule). Log
appended after the unchanged `ErrorMessage`/`State`/`StatusMessage` line. 5 production + 5 test files;
10 new tests (failure-logs-Error + NullLogger safety per VM). No DI/interface/shared-stub change. Full
detail in `ROJAN_PHASE8_21_LOGGING_WAVE2A_COMMIT_REPORT_v1.md`.

**Phase 8.23 detail:** `ILogger<T>` added to 4 `AddTransient` page ViewModels — `AnalyticsPageViewModel`
(1 boundary), `AiCenterPageViewModel` (2 — Load + **chat `SendMessageAsync`**), `SalonPageViewModel`
(2 — Load + CreateSalon), `QrCodesPageViewModel` (2 — Load + GenerateReceptionInvite). Same
optional-ctor-param + `NullLogger<T>` + `[LoggerMessage]` pattern, `Error` level, **operation-name-only,
exception never passed**. The AI Center chat boundary handles the user's raw chat text — a dedicated test
asserts the chat text is absent from the log line. 4 production + 4 test files; 10 new tests. No DI /
interface / shared-stub change (`OrganizationPageViewModel` deferred to Wave 2B-2). Full detail in
`ROJAN_PHASE8_25_LOGGING_WAVE2B_COMMIT_REPORT_v1.md`.

**Phase 8.27 detail:** `ILogger<T>` added to `OrganizationPageViewModel` — its one swallowing broad
catch (`LoadAsync`, `:418`) now logs at `Error` before the unchanged `ErrorMessage`/`State`. Same
optional-ctor-param + `NullLogger<T>` + `[LoggerMessage]` pattern, **operation-name-only, exception never
passed** (the page loads org name / tax info / VAT / receipt text — none logged). 1 production file + a
**new** `OrganizationPageViewModelTests.cs` (2 tests, 2 private nested stubs — the VM had no dedicated
test file before). No DI / interface / shared-stub change. Full detail in
`ROJAN_PHASE8_29_ORGANIZATION_LOGGING_COMMIT_REPORT_v1.md`.

**Phase 8.31 detail:** `ILogger<T>` added to `SupportPageViewModel` — its two filtered broad catches
(`SubmitMessageAsync`, `SubmitApplicationAsync`) now log at `Error` before the unchanged
`MessageError`/`ApplicationError`. Same optional-ctor-param + `NullLogger<T>` + `[LoggerMessage]` pattern,
**operation-name-only, exception never passed** — both forms carry PII (sender name/email, message body,
applicant email/resume URL); two tests seed recognizable PII and assert it is absent from the log line.
1 production + 1 test file; 3 new tests. No DI / interface / shared-stub change. Full detail in
`ROJAN_PHASE8_33_SUPPORT_LOGGING_COMMIT_REPORT_v1.md`.

**Phase 8.35 detail:** `ILogger<T>` added to `AcceptInviteViewModel` — its two broad catches
(`LookupAsync` `:164`, `AcceptAsync` `:209`) now log at `Error` before the unchanged
`LookupErrorMessage`/`AcceptErrorMessage`. `MobileOtpLoginViewModel` security precedent:
**operation-name-only, exception never passed**. The invite **token** (`_token`), the user's identity
(resolved by `_currentSessionService.InitializeAsync()` inside `AcceptAsync`), and any backend response
stay out of the log — three tests seed a recognizable token / email / user-id into exception messages and
assert `DoesNotContain` on the log line. 1 production + 1 test file; 4 new tests. No DI / interface / shared
stub change (only the *private nested* `StubCurrentSessionService` gained an `InitializeException` seam).
Application-only dependency boundary preserved. Full detail in
`ROJAN_PHASE8_37_ACCEPTINVITE_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **20 of 56** — **Wave 2C-1 complete**; every `AddTransient` page
ViewModel with a swallowing broad `catch (Exception)` is now instrumented, remaining are `new`-by-parent.

**Phase 8.39 detail:** `ILogger<T>` added to all 5 Automation tab VMs via the established
`sealed partial` + optional-ctor-param + `NullLogger<T>` + instance-form `[LoggerMessage(EventId = 1,
Level = Error)]` pattern — `AutomationDashboardTabViewModel` (1 catch: `LoadAsync`), `ApprovalsTabViewModel`
(2: `LoadAsync`/`DecideAsync`), `BusinessRulesTabViewModel` (2: `LoadAsync`/`CreateAsync`),
`ScheduledJobsTabViewModel` (3: `LoadAsync`/`CreateAsync`/`RunNowAsync`), `WorkflowsTabViewModel` (5:
`LoadAsync`/`CreateDraftAsync`/`PublishAsync`/`RunNowAsync`/`RollbackAsync`) — 13 sites, each appended
**after** the unchanged `ErrorMessage = exception.Message;`. **Parent plumbing:** `AutomationPageViewModel`
gained 5 optional nullable `ILogger<TChild>?` params (appended after the existing 7 services), each
forwarded to its `new XxxTabViewModel(...)`; parent stays `sealed class` (0 catches, no `[LoggerMessage]`);
no DI registration change (open-generic `ILogger<T>` resolves; all params optional). **Operation-name-only,
exception never passed** — workflow/rule/approval content, decision comments, cron expressions, user/org/branch
ids all stay out; 13 tests seed secrets and assert `DoesNotContain`. 6 production + 7 test files; **+19 tests**
(13 failure-logging + 5 NullLogger + 1 parent pass-through wiring). `StubAutomationServices.cs` gained 16
additive default-null `Exception?` failure hooks (test-only, null-path byte-identical). No interface / shared
production stub change. Full detail in `ROJAN_PHASE8_41_AUTOMATION_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **25 of 56** — **Wave 2C-2 complete**; all Automation tabs instrumented,
remaining unlogged are the Wave 2C-3 detail/profile `new`-by-parent VMs.

**Phase 8.43 detail:** `ILogger<T>` added to 3 profile-panel child VMs — `CustomerProfileViewModel`
(1 catch: `LoadAsync`), `ServiceProfileViewModel` (3: `LoadAsync`/`SaveChangesAsync`/`DeactivateAsync`),
`InventoryProfileViewModel` (1: `LoadAsync`) — via `sealed partial` + optional-ctor-param +
`NullLogger<T>` + instance-form `[LoggerMessage(EventId = 1, Level = Error)]`; each call appended
**after** the unchanged error-surfacing (incl. `ServiceProfileViewModel.SaveChangesAsync`'s edit-buffer
revert, test-asserted intact). **Parent plumbing:** `CustomerPageViewModel`, `ServicePageViewModel`,
`InventoryPageViewModel` each gained one optional `ILoggerFactory? loggerFactory = null` param (appended
after the existing optional `ILogger<TSelf>? logger`) + `_loggerFactory` field; `_loggerFactory?.CreateLogger<TChild>()`
at the child `new` site in the `SelectedX` setter. **`ILoggerFactory` (not a 2nd `ILogger<TChild>` field)**
because these 3 parents already carry `ILogger<TSelf> _logger` + an instance-form `[LoggerMessage]`
(Wave 2A) — a 2nd `ILogger` field would trip `SYSLIB1020`; `ILoggerFactory` is not `ILogger`, so the
parent's own logging is untouched. No DI registration change (all params optional; `ILoggerFactory`
provided by `AddLogging()`). **Operation-name-only, exception never passed** — customer PII, service
price, inventory SKU/cost, supplier data all stay out; failure tests seed recognizable secrets and
assert `DoesNotContain`. 6 production + 6 test files + **1 new test helper** (`RecordingLoggerFactory.cs`,
`ILoggerFactory` recorder, test project only, next to `RecordingLogger.cs`); **+11 tests** (5
failure-logging + 3 NullLogger + 3 parent factory pass-through). No interface / shared production stub
change (the 3 profile query stubs were already delegate-driven). Full detail in
`ROJAN_PHASE8_45_PROFILE_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **28 of 56** — **Wave 2C-3a complete**.

**Phase 8.47 detail:** `ILogger<BookingWizardViewModel>` added via `sealed partial` + optional-ctor-param
(appended **after** `Action? onBookingCreated = null`) + `NullLogger<T>` + one instance-form
`[LoggerMessage(EventId=1, Level=Error, "Booking wizard operation failed. Operation={Operation}")]`;
**4 of 5** catches instrumented — `LoadOptionsAsync`, `AddGuestCustomerAsync`, `LoadAvailableSlotsAsync`,
`ConfirmBookingAsync` — each appended **after** the unchanged `ErrorMessage = ToFriendlyErrorMessage(exception);
State = DashboardState.Error;`. **`SearchNextAvailableDateAsync` deliberately NOT instrumented**
(best-effort cancellable probe, swallowed by design, never mutates `ErrorMessage`/`State`; its catch /
`finally` / `_nextAvailableDateSearchCts` handling is byte-for-byte unchanged) — guarded by test
`SearchNextAvailableDateAsync_ProbeFails_LogsNothing`. **Parent plumbing:** `BookingPageViewModel` gained
one optional `ILoggerFactory? loggerFactory = null` (appended after its existing `logger`) + `_loggerFactory`
field; `_loggerFactory?.CreateLogger<BookingWizardViewModel>()` at `OpenWizard()`. **`ILoggerFactory`
(not a 2nd `ILogger` field)** because `BookingPageViewModel` already carries `ILogger<BookingPageViewModel>
_logger` + the **legacy `(string operation, Exception exception)`-form `[LoggerMessage]`** from `da18c18`
— both left untouched (its 5 call sites unchanged); `ILoggerFactory` is not `ILogger` so no `SYSLIB1020`.
**Operation-name-only, exception never passed** — guest name/phone, booking notes, slot times,
customer/service/specialist ids+names+price all stay out; failure tests seed secrets and assert
`DoesNotContain` (incl. the `ConfirmBookingAsync` stub interpolating `CustomerName / ServiceName /
SpecialistName / Notes / Price`). 2 production + 2 test files; **+7 tests** (4 boundary failure-logs + 1
probe-no-log guard + 1 NullLogger + 1 parent factory forwarding). Reused `RecordingLogger<T>` +
`RecordingLoggerFactory` — **no new test helper**, no shared stub change, no interface change. Full detail
in `ROJAN_PHASE8_49_BOOKINGWIZARD_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **29 of 56** — **Wave 2C-3b complete**.

**Phase 8.51 detail:** `ILogger<T>` added to the 3 remaining detail-profile child VMs —
`EmployeeProfileViewModel` (`LoadAsync`), `InvoiceProfileViewModel` (`LoadAsync`),
`SpecialistProfileViewModel` (`LoadAsync` / `SaveChangesAsync` / `AssignServiceAsync` /
`RemoveServiceAssignmentAsync`) — 6 call sites, each appended **after** the unchanged error-surfacing
(incl. `SpecialistProfileViewModel.SaveChangesAsync`'s `EditableStatus` revert, test-asserted intact).
**Parent plumbing:** `HrPageViewModel`, `AccountingPageViewModel`, `SpecialistPageViewModel` each gained
one optional `ILoggerFactory? loggerFactory = null` param + `_loggerFactory` field;
`_loggerFactory?.CreateLogger<TChild>()` at the child `new` site in the `SelectedX` setter.
**`ILoggerFactory` for all three** (not a 2nd `ILogger<TChild>` field): HrPage has `ILogger<HrPageViewModel>`
+ instance `[LoggerMessage]` (2nd field → `SYSLIB1020`); AccountingPage has 2 `ILogger` fields + static-form
`[LoggerMessage]` (kept untouched); SpecialistPage has 2 typed grandchild-logger fields (kept untouched,
factory future-proofs Wave 2D). No DI registration change. **Operation-name-only, exception never passed**
— employee salary/commission, invoice amounts/payments/receipts, specialist email/phone/bio/performance
all stay out; failure tests seed secrets and assert `DoesNotContain`. 6 production + 6 test files; **+12
tests** (6 failure-logging + 3 NullLogger + 3 parent factory forwarding). Reused `RecordingLogger<T>` +
`RecordingLoggerFactory` — **no new test helper**, no shared stub change (`StubSpecialistCommandService`
already had the throw hooks), no interface change. `SaveChangesAsync` added via the Phase 8.51 Scope
Correction Authorization. Full detail in `ROJAN_PHASE8_53_DETAIL_PANELS_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **32 of 55** — **Wave 2C-3c complete**; the entire detail/profile
`new`-by-parent set is instrumented.

**Phase 8.56 detail:** `SpecialistPageViewModel.LoadAsync` instrumented — the last uninstrumented
swallowing broad `catch (Exception)` in the Presentation layer (found by the Phase 8.54 Wave 2D sweep).
`sealed partial`; **static-form** `[LoggerMessage(EventId=1, Level=Error)] private static partial void
LogOperationFailed(ILogger logger, string operation)` — no `Exception` param. Logger derived inline from
the existing `_loggerFactory` (`?? NullLogger<SpecialistPageViewModel>.Instance`) — **no new field, no new
ctor param**. Static form because the class already carries 2 `ILogger` fields → instance form + a 3rd
field = `SYSLIB1020`. Call inside the existing `_filterVersion` staleness guard, after the unchanged
`ErrorMessage`/`State`. Operation-name-only; no specialist PII / search text / backend body; test-enforced.
1 production + 1 test file, **+3 tests**; reused `RecordingLogger<T>` + `RecordingLoggerFactory`, no
shared-stub change. Full detail in `ROJAN_PHASE8_58_SPECIALIST_PAGE_LOGGING_COMMIT_REPORT_v1.md`.
Self-logging ViewModel coverage: **33 of 55** — **the ViewModel diagnostic-logging track is CLOSED.**

> **Logging coverage: final (as of `6a1bced`).** Every ViewModel in the ROJAN Desktop Presentation layer
> with a swallowing broad `catch (Exception)` that surfaces a user-facing error state is instrumented with
> PII-safe, operation-name-only diagnostic logging at `Error` (`MobileOtpLoginViewModel` at `Warning`).
> Self-logging: **33 of 55**. The remaining 22 are pure state/layout holders (Workspaces ×7,
> Notifications ×5, Search rows, dialogs), thin wrappers, singleton UI hosts, `AutomationPageViewModel`
> (pass-through parent), or a retired implementation (`Security/LoginViewModel` — no view). None has a
> failure boundary. One deliberate, authorizer-approved, test-guarded skip:
> `BookingWizardViewModel.SearchNextAvailableDateAsync` (best-effort cancellable probe). **Track closed.**

**Every commit above was individually audited, scope-reviewed (explicit-path staging only, never
`git add -A`/`git add .`), executed, and validated (build + full test suite + architecture tests) before
being made** — no exceptions. Where a shared file mixed concerns (e.g. `ServiceCollectionExtensions.cs`
carrying both Shift Engine and RBAC/Calendar content), isolation was achieved via hand-verified
`git apply --check --cached` dry-run patches, not risky interactive staging.

**Not pushed. Not merged. Not rebased.** One amend has occurred in the entire arc: at Phase 8.41, a
**message-only** `git commit --amend` (explicitly authorized) to correct a malformed commit subject on
`b643adc` (shell here-string bug) → `c01d0ce`. `git diff b643adc c01d0ce` is empty — no content, tree,
or scope change. No other amend at any point.

---

## C. Architecture Decisions

Confirmed, re-verified multiple times across this arc, most recently at Phase 7.5/8.1:

1. **Backend is sole Business Authority.** Booking-conflict resolution, pricing, and payment recording
   are all backend-decided. Desktop performs zero authoritative business-rule computation for any
   backend-connected domain.
2. **Zero local database authority remains anywhere.** No `Ef*Repository` registration exists in
   `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs` — confirmed by direct grep,
   re-verified at Phase 7.5. Calendar's was the last one; removed in `7103647`.
3. **Permission enforcement is backend-sourced for every backend-connected domain, and only those.**
   6 permission gates use `IBackendPermissionGate` (Bookings ×2, Customers, Specialists, Services,
   Specialists/Schedule) — exactly the 6 domains with real backend data authority. 12 gates
   (Reporting ×2, Organizations, Inventory, HR ×5, Accounting ×2, AI ×2) still use the legacy local
   `IPermissionGate`/`RolePermissions` — exactly, and only, the domains still on `Fake*Repository`. This
   correspondence is exact and intentional, not accidental or incomplete.
4. **Fail-closed by design.** `WorkspaceRole.Unknown` grants an explicit, tested, empty permission set —
   not a fallback default. Every RBAC change made across this arc narrowed access; none widened it.
5. **No local scheduling authority.** Calendar's one local write path (manual slot reserve/release) is
   fully removed. Shift Engine's schedule data is 100% backend-sourced.
6. **Retired implementations are kept, unreferenced, never deleted** — the established convention for
   every `Fake*`/`Ef*` repository across this whole codebase (confirmed precedent, not new).
7. **DI lifetime model is intentionally simple:** `AddSingleton` for every repository/Application
   service, `AddTransient` for every page ViewModel, **zero `AddScoped` usage anywhere** — this app has
   no request-scope concept and has never introduced one to manage incorrectly.

---

## D. Domain Status Matrix

(As established at Phase 7.5, re-confirmed unchanged at Phase 8.1 — nothing has changed since.)

| Domain | Status | Notes |
|---|---|---|
| **Authentication** | **Production Ready** | Backend-connected (`BackendAuthenticationService`/`BackendSessionService`), UX hardened (`801cc65`), credential/token flow verified untouched and correct throughout |
| **Booking** | **Production Ready** | Backend-connected, all 5 command paths guarded + logged (`da18c18`) |
| **Calendar** | **Backend Dependent** | Pure read/API layer since `7103647` — no local authority of any kind by design |
| **Shift Engine** | **Production Ready** | Backend-connected, DI-complete (`53090c1`), diagnostics hardened (`ea03d83`). One disclosed, deliberately conservative limitation: its permission gate checks `MANAGE_STAFF` only, not the backend's own `MANAGE_SCHEDULE_OWN` self-service allowance — stricter than necessary, not a gap |
| **RBAC** | **Production Ready** (for the 6 domains it governs) | See §C.3 |
| **Inventory** | **Pending Contract** | `FakeInventoryRepository`, legacy `IPermissionGate`. **Backend has zero Inventory code at any layer** — re-confirmed exhaustively at Phase 8.0 (case-insensitive sweep across all of `ROJAN_Backend`, every branch's commit history, zero results). Desktop side is fully prepared (16 test files, complete Domain/Application/Presentation layers) and needs no further prep work — blocked entirely upstream on Backend/Team 1 |
| **Accounting** | **Pending Contract** | `FakeAccountingRepository`, legacy `IPermissionGate`. UI/error-handling already hardened (`da18c18`) ahead of backend connection. **One open, unresolved risk:** `PosCheckoutViewModel.ChargeAsync` leaves the invoice re-chargeable after a failed payment — backend payment-idempotency unverified from this codebase, documented via a behavior-confirming test, not fixed (out of scope where first found) |
| **HR** | **Pending Contract** | `FakeHrRepository`, legacy `IPermissionGate` across all 5 of its gates |

---

## E. Production Readiness Status

**No P0 blocker exists anywhere in this codebase**, as of the most recent full audit (Phase 7.5,
re-confirmed at 8.1).

- Build: clean (0 warnings, 0 errors), **Debug verified at `58a2c88`**; Release last verified at 8.1.
- Full test suite: **2,715/2,715 passing** as of `58a2c88` (Domain 456, Presentation 772, Application
  791, Infrastructure 609, Shell 80, Architecture 7). Progression: 2,507 (`801cc65`) → … → 2,550
  (`cbc3a82`, Organization) → 2,553 (`0542041`, Support) → 2,557 (`38c24da`, +4 AcceptInvite) → 2,576
  (`c01d0ce`, +19 Automation tabs / Wave 2C-2) → 2,587 (`7aa1d1b`, +11 Profile panels / Wave 2C-3a) →
  2,594 (`884cec3`, +7 BookingWizard / Wave 2C-3b) → 2,606 (`5b7f6ca`, +12 Detail panels / Wave 2C-3c) →
  2,609 (`6a1bced`, +3 SpecialistPage / Wave 2D final P1) → 2,609 (`5ba554c`, +0 legacy-`[LoggerMessage]` harmonization) →
  2,622 (`794648e`, +13 Missing-Guard Sweep Wave A) →
  2,641 (`a5be831`, +19 Missing-Guard Sweep Wave B / HR) →
  2,654 (`66c8490`, +13 Missing-Guard Sweep Wave C / Inventory+Accounting) →
  2,666 (`525fd4b`, +12 Missing-Guard Sweep Wave D / Organization) →
  2,672 (`5640123`, +6 Missing-Guard Sweep Reporting mini-wave) →
  2,678 (`6f64ffa`, +6 Missing-Guard Sweep Export Dialog micro-phase) →
  2,691 (`4b1afca`, +13 Missing-Guard Sweep Wave E / AI Center) →
  2,701 (`7c9c132`, +10 Missing-Guard Sweep Wave F / Automation tabs — Phase 8.94 +8, Phase 8.94.1 +2) →
  2,710 (`0260bc3`, +9 Missing-Guard Sweep Settings-page P2 carve-out — Phase 8.99) →
  2,713 (`76d3f61`, +3 "sanitize load-error surfacing" P2 sub-wave 1 — Phase 8.104) →
  2,714 (`1260d4e`, +1 "sanitize load-error surfacing" P2 sub-wave 2 — Phase 8.108) →
  2,715 (`b509054`, +1 "sanitize load-error surfacing" P2 sub-wave 3 — Phase 8.112) →
  2,715 (`d10f9bc`, +0 "sanitize load-error surfacing" P2 sub-wave 4 / Automation tabs — Phase 8.116 + 8.117.1; no-leak assertions added to existing Phase 8.39 tests) →
  2,715 (`71fb472`, +0 "sanitize load-error surfacing" P2 sub-wave 5 / Booking + Calendar + Inventory — Phase 8.121; existing raw-message failure-test assertions updated to the generic constant) →
  2,715 (`17306d9`, +0 "sanitize load-error surfacing" P2 sub-wave 6 / FINAL — Dashboard + Analytics + Salon + QR + Support + CustomerProfile — Phase 8.125; **P2 track complete, all 58 Category-A sites sanitized**) →
  2,715 (`58a2c88`, +0 Settings UX visibility fix — Phase 8.129; XAML-only, `SettingsPage.xaml` 3 `*StatusMessage` TextBlocks → non-empty-string visibility).
- Architecture tests: **7/7 passing.**
- Global exception handling: all three .NET unhandled-exception surfaces covered (`AppDomain`,
  `TaskScheduler.UnobservedTaskException`, `DispatcherUnhandledException`), each logging via a real,
  production-grade file logger (`LocalFileLoggerProvider` — daily-rotated, 14-day retention, fail-safe
  writes) before recovering (UI thread) or accepting the unavoidable termination (non-UI fatal).
- **Known gap, being closed incrementally:** self-logging ViewModels: 4 → 13 (Wave 2A) → 17 (Wave 2B)
  → 20 (Wave 2C-1) → 25 (`c01d0ce`, Wave 2C-2 / Automation tabs) → 28 (`7aa1d1b`, Wave 2C-3a / profile
  panels) → 29 (`884cec3`, Wave 2C-3b / BookingWizard) → 32 (`5b7f6ca`, Wave 2C-3c / detail panels) →
  **33 of 55** (`6a1bced`, Wave 2D final P1 / SpecialistPage). Phase 8.54 corrected the real population
  to **55** concrete `*ViewModel.cs` (earlier audits cited "56"/"71"). **The ViewModel diagnostic-logging
  track is CLOSED** — every ViewModel with a swallowing broad `catch (Exception)` surfacing a user-facing
  error state is instrumented, operation-name-only, exception never passed
  (`BookingWizardViewModel.SearchNextAvailableDateAsync` is a deliberate, test-guarded skip — best-effort
  cancellable probe). The remaining 22 uninstrumented VMs are pure state/layout holders, thin wrappers,
  singleton UI hosts, `AutomationPageViewModel` (pass-through parent), or a retired implementation
  (`Security/LoginViewModel`, no view) — none has a failure boundary. See §F.

**Scorecard (Phase 8.1):** Architecture 9/10, Security 9/10, Reliability 7/10, Maintainability 9/10,
Production Readiness 8/10.

---

## F. Pending Hardening Items

All P2 or lower — **none blocking, all identified, none yet implemented.**

| Item | Source | Status |
|---|---|---|
| Logging coverage — Phase 8.2 named set (`MobileOtpLoginViewModel`, `DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel`) | Phase 8.2 | **FULLY RESOLVED — `2453a7f` (Wave 1: Dashboard/Calendar/Accounting) + `31f4b63` (Mobile OTP)** |
| Logging coverage — **Wave 2A** (Customer/Service/Inventory/HR/Reporting page VMs) | Phase 8.18 | **RESOLVED — `75357e1` (Phase 8.19, executed Phase 8.21).** `Error` level, operation-name-only |
| Logging coverage — **Wave 2B** (`Analytics`/`AiCenter`/`Salon`/`QrCodes` page VMs) | Phase 8.18/8.22 | **RESOLVED — `2ed685a` (Phase 8.23, executed Phase 8.25).** `Error` level, operation-name-only; AI Center chat-text non-leak test-enforced |
| Logging coverage — **Wave 2B-2** (`OrganizationPageViewModel`) | Phase 8.22 §F / 8.26 | **RESOLVED — `cbc3a82` (Phase 8.27, executed Phase 8.29).** Shipped with a new `OrganizationPageViewModelTests.cs` (2 tests, 2 private nested stubs) |
| Logging coverage — **Wave 2C-1** (`SupportPageViewModel` + `AcceptInviteViewModel`) | Phase 8.30 | **RESOLVED — `0542041` (Support, PII-safe) + `38c24da` (AcceptInvite, token-safe + identity-safe).** Both test-enforced |
| Logging coverage — **Wave 2C-2** (5 Automation tab VMs + `AutomationPageViewModel` logger plumbing) | Phase 8.18 | **RESOLVED — `c01d0ce` (Phase 8.39, executed Phase 8.41).** 13 catches instrumented; `AutomationPageViewModel` carries 5 optional nullable `ILogger<TChild>?` pass-through params; `Error` level, operation-name-only, test-enforced. +19 tests |
| Logging coverage — **Wave 2C-3a**: `CustomerProfileViewModel` / `ServiceProfileViewModel` / `InventoryProfileViewModel` + `ILoggerFactory` parent plumbing | Phase 8.18 / 8.42 | **RESOLVED — `7aa1d1b` (Phase 8.43, executed Phase 8.45).** `Error` level, operation-name-only, `SYSLIB1020`-safe via `ILoggerFactory`; +11 tests, new `RecordingLoggerFactory` helper |
| Logging coverage — **Wave 2C-3b**: `BookingWizardViewModel` (4 of 5 catches) + `BookingPageViewModel` `ILoggerFactory` plumbing | Phase 8.18 / 8.42 | **RESOLVED — `884cec3` (Phase 8.47, executed Phase 8.49).** `Error` level, operation-name-only, `SYSLIB1020`-safe via `ILoggerFactory`; `SearchNextAvailableDateAsync` skipped (test-guarded); `BookingPageViewModel`'s legacy `[LoggerMessage]` untouched; +7 tests, no new helper |
| Logging coverage — **Wave 2C-3c**: `EmployeeProfileViewModel` / `InvoiceProfileViewModel` / `SpecialistProfileViewModel` + `ILoggerFactory` parent plumbing | Phase 8.18 / 8.42 / 8.50 | **RESOLVED — `5b7f6ca` (Phase 8.51, executed Phase 8.53).** 6 call sites; `Error` level, operation-name-only, `SYSLIB1020`-safe via `ILoggerFactory` for all 3 parents; +12 tests, no new helper, no shared-stub change. `SaveChangesAsync` added via the Phase 8.51 Scope Correction Authorization |
| Logging coverage — **Wave 2D**: fresh gap audit + close the track | Phase 8.9 / 8.54 | **RESOLVED — audit `ROJAN_PHASE8_54_*` (55 concrete VMs; exactly 1 uninstrumented swallowing catch: `SpecialistPageViewModel.LoadAsync`) → `6a1bced` (Phase 8.56, executed Phase 8.58).** Static-form `[LoggerMessage]` (SYSLIB1020-safe), logger inline from the existing `_loggerFactory`, no new field/ctor param, +3 tests. **Logging track CLOSED at 33/55.** |
| Logging — **P2 harmonize legacy `[LoggerMessage]` to operation-name-only** | Phase 8.54 §D.3 / 8.59 §D / 8.60 | **RESOLVED — `5ba554c` (Phase 8.61, executed Phase 8.63).** 7 VMs (`AccountingPage`, `PosCheckout`, `BookingPage`, `CalendarPage`, `DashboardPage`, `SpecialistAvailability`, `SpecialistSchedule`) — 8 `[LoggerMessage]` methods / 19 call sites → `(string operation)`. No form/field/ctor/DI change, no `SYSLIB1020`, no behaviour change. `App.LogUnhandledException` + `HttpApiClient` intentionally keep their `Exception`. Every ViewModel `[LoggerMessage]` is now operation-name-only. |
| Logging — shared-stub throw hooks for fuller per-boundary test coverage of untested Wave 2A/2B log sites (incl. `AiCenterPageViewModel.LoadAsync`) | Phase 8.19 §D.3 / 8.23 §C.1 | Follow-up test-infra pass — not a correctness risk |
| `AuthBootstrapHttpClient` has no logging of its own (OTP's only trail is now `MobileOtpLoginViewModel`) | Phase 8.14 §A.3 | Disclosed structural gap — a separate Infrastructure-layer decision, not the ViewModel track |
| `PosCheckoutViewModel.ChargeAsync` double-charge-on-retry risk | Phase 7.4.4 | Documented, unresolved, blocks Accounting's eventual backend connection specifically |
| ~~`AccountingPageViewModel.CancelInvoiceAsync` — no try/catch~~ | Phase 8.10 | **RESOLVED — `66c8490` (Phase 8.74, executed Phase 8.76 / Missing-Guard Sweep Wave C).** Wrapped in the in-page `try`/`catch` + `ActionErrorMessage` pattern; static-form `[LoggerMessage]` reused; no cancellation/payment/rollback-logic change. |
| ~~Navigation BackStack unbounded growth~~ | Phase 8.4/8.5 | **RESOLVED — committed `94fca6a` (Phase 8.6, executed Phase 8.8).** `_backStack` capped at 20, FIFO eviction |
| `DashboardKpiCollection`'s missing event unsubscribe | Phase 8.1/8.3 | **Investigated and resolved as No Risk** — publisher/subscriber share one lifetime root; no fix needed. (Superseded finding: the real retention mechanism is the Navigation BackStack item above, not this) |
| CancellationToken propagation — highest value at `CommandPaletteViewModel` (Search), medium at Booking filter-reload/Calendar navigation-reload | Phase 8.2 | Planned, not started |
| Startup UX — no progress indicator across `App.OnStartup`'s 13 blocking initialization stages | Phase 8.2 | Planned, not started |
| RBAC migration for the 6 still-local domains (Inventory/HR/Accounting/AI/Organization/Reporting), once/if each gets backend integration | Phase 7.5 | Sequenced future work, blocked on each domain's own backend contract |
| Calendar's dead EF migration/tables (3 permanently-unused tables a fresh local DB still creates) | Phase 7.4.15 | Disclosed technical debt, deliberately deferred |
| `RolePermissions`' `CustomerEdit`/`ServiceEdit`/`SpecialistEdit` dead enum members | Phase 7.5 | Cleanup opportunity, no urgency |

---

## G. Current Next Action

**Done and committed:**
- **Phase 8.6 — Navigation BackStack Hardening** → `94fca6a` (Phase 8.8). Reports `ROJAN_PHASE8_6/7/8_*`.
- **Phase 8.11 — ViewModel Logging Wave 1** → `2453a7f` (Phase 8.13): `DashboardPageViewModel`,
  `CalendarPageViewModel`, `AccountingPageViewModel`. Reports `ROJAN_PHASE8_9/10/11/12/13_*`.
- **Phase 8.15 — Mobile OTP Logging** → `31f4b63` (Phase 8.17): `MobileOtpLoginViewModel`. Reports
  `ROJAN_PHASE8_14/15/16/17_*`. Completed the Phase 8.2 named-ViewModel logging set.
- **Phase 8.19 — Logging Wave 2A** → `75357e1` (Phase 8.21): `Customer`/`Service`/`Inventory`/`Hr`/
  `Reporting` page VMs. Reports `ROJAN_PHASE8_18/19/20/21_*`.
- **Phase 8.23 — Logging Wave 2B** → `2ed685a` (Phase 8.25): `Analytics`/`AiCenter`/`Salon`/`QrCodes`
  page VMs (AiCenter incl. the chat boundary). Reports `ROJAN_PHASE8_22/23/24/25_*`.
- **Phase 8.27 — Organization Page Logging** → `cbc3a82` (Phase 8.29): `OrganizationPageViewModel` +
  new `OrganizationPageViewModelTests.cs`. Reports `ROJAN_PHASE8_26/27/28/29_*`.
- **Phase 8.31 — Support Page Logging** (Wave 2C-1a) → `0542041` (Phase 8.33): `SupportPageViewModel`.
  Reports `ROJAN_PHASE8_30/31/32/33_*`.
- **Phase 8.35 — AcceptInvite Security Logging** (Wave 2C-1b) → `38c24da` (Phase 8.37):
  `AcceptInviteViewModel`. Reports `ROJAN_PHASE8_34/35/36/37_*`. **Wave 2C-1 complete.**
- **Phase 8.39 — Automation Tabs Logging** (Wave 2C-2) → `c01d0ce` (Phase 8.41): all 5 Automation tab
  VMs + `AutomationPageViewModel` parent plumbing (5 optional nullable `ILogger<TChild>?` pass-through
  params). Reports `ROJAN_PHASE8_38/39/40/41_*`. **Wave 2C-2 complete.** (Commit `b643adc` corrected via
  one authorized message-only amend → `c01d0ce`.)
- **Phase 8.43 — Profile Panels Logging** (Wave 2C-3a) → `7aa1d1b` (Phase 8.45): `CustomerProfileViewModel`,
  `ServiceProfileViewModel`, `InventoryProfileViewModel` + `ILoggerFactory` parent plumbing through
  `Customer`/`Service`/`InventoryPageViewModel` (their pre-existing `ILogger<TSelf>` + instance
  `[LoggerMessage]` from Wave 2A rules out a 2nd `ILogger` field / `SYSLIB1020`). New test helper
  `RecordingLoggerFactory`. Reports `ROJAN_PHASE8_42/43/44/45_*`. **Wave 2C-3a complete.**
- **Phase 8.47 — BookingWizard Logging** (Wave 2C-3b) → `884cec3` (Phase 8.49): `BookingWizardViewModel`
  4-of-5 catches (`SearchNextAvailableDateAsync` deliberately skipped, test-guarded) + `BookingPageViewModel`
  `ILoggerFactory` plumbing (its legacy `(operation, exception)`-form `[LoggerMessage]` left untouched).
  Reused `RecordingLogger<T>` + `RecordingLoggerFactory` (no new helper). Reports `ROJAN_PHASE8_46/47/48/49_*`.
  **Wave 2C-3b complete.**
- **Phase 8.51 — Detail Panels Logging** (Wave 2C-3c) → `5b7f6ca` (Phase 8.53): `EmployeeProfileViewModel`
  (`LoadAsync`), `InvoiceProfileViewModel` (`LoadAsync`), `SpecialistProfileViewModel` (`LoadAsync` /
  `SaveChangesAsync` / `AssignServiceAsync` / `RemoveServiceAssignmentAsync`) — 6 call sites +
  `ILoggerFactory` plumbing through `HrPageViewModel` / `AccountingPageViewModel` / `SpecialistPageViewModel`
  (each parent's own logger(s) + `[LoggerMessage]` left untouched). Reused `RecordingLogger<T>` +
  `RecordingLoggerFactory` (no new helper), no shared-stub change. `SaveChangesAsync` added via the
  Phase 8.51 Scope Correction Authorization. Reports `ROJAN_PHASE8_50/51/52/53_*`. **Wave 2C-3c complete.**
- **Phase 8.56 — SpecialistPage Logging** (Wave 2D / final P1) → `6a1bced` (Phase 8.58):
  `SpecialistPageViewModel.LoadAsync` — the last uninstrumented swallowing broad catch. Static-form
  `[LoggerMessage]` (SYSLIB1020-safe — the class already holds 2 `ILogger` fields), logger derived inline
  from the existing `_loggerFactory`, no new field/ctor param, +3 tests. Reports `ROJAN_PHASE8_54/55/56/57/58_*`.
  **Wave 2D complete → the ViewModel diagnostic-logging track is CLOSED (33/55).**
- **Phase 8.61 — Legacy `[LoggerMessage]` Harmonization** → `5ba554c` (Phase 8.63): the 7 pre-8.15 VMs
  (`AccountingPage`, `PosCheckout`, `BookingPage`, `CalendarPage`, `DashboardPage`, `SpecialistAvailability`,
  `SpecialistSchedule`) whose `[LoggerMessage]` still forwarded the caught `Exception` (and, in 2, a
  `SpecialistId`) converted to operation-name-only. 8 methods / 19 call sites. No form / field / ctor / DI
  change, no `SYSLIB1020`, no behaviour change. `App.LogUnhandledException` + `HttpApiClient` intentionally
  keep their `Exception`. 7 prod + 7 test files, net 0 tests (2 breaking assertions fixed, 5 strengthened).
  Reports `ROJAN_PHASE8_59/60/61/62/63_*`. **→ Every ViewModel `[LoggerMessage]` is now operation-name-only
  — the ViewModel diagnostic-logging architecture is CLOSED and RULE-CONSISTENT.**
- **Phase 8.66 — Missing-Guard Sweep Wave A** (Production Hardening reliability track) → `794648e`
  (Phase 8.68): the 12 unguarded backend-connected write commands across `CustomerPageViewModel`
  (`CreateCustomer`), `CustomerProfileViewModel` (`AddNote`/`AddTag`/`RemoveTag`/`SaveChanges`),
  `ServiceProfileViewModel` (`AssignSpecialist`/`UnassignSpecialist`), `SpecialistProfileViewModel`
  (`AddSkill`/`RemoveSkill`), `SpecialistPageViewModel` (`CreateSpecialist`) wrapped in the app's in-page
  `try`/`catch` + inline-error pattern (`ServicePageViewModel.CreateServiceAsync` precedent). New
  `CreateErrorMessage`/`HasCreateError` + `SaveErrorMessage`/`HasSaveError` pairs where missing; new
  shared string `Common_ActionFailedMessage` (all 3 locales). Reuses each VM's existing `[LoggerMessage]`
  (`SpecialistPageViewModel` via a new `Logger` computed property — no new field, no `SYSLIB1020`).
  `catch (Exception)` no-variable in all 12 → no `Exception.Message`/body/identifier/PII surfaced or
  logged. **No business-behaviour change** — validation / `CanExecute` / RBAC / success path / reload
  untouched; `CustomerProfileViewModel.SaveChanges` reverts `EditableStatus`. 5 prod + 4 loc + 3 stubs
  (additive `Exception?` seams) + 5 test files, **+13 tests**. `ServicePageViewModel` / `AsyncRelayCommand`
  / `App.xaml.cs` / DI / interfaces / Domain / backend contracts untouched. Reports
  `ROJAN_PHASE8_64/65/66/67/68_*`. **Missing-Guard Sweep Wave A complete.**
- **Phase 8.70 — Missing-Guard Sweep Wave B / HR** → `a5be831` (Phase 8.72): the 13 unguarded HR command
  methods across `HrPageViewModel` (`CreateEmployee` / `RecordAttendance` / `CreateShift` / `AssignShift` /
  `RequestLeave` / `ApproveLeave` / `RejectLeave` / `CreateCommissionRule` / `GenerateCommissions` /
  `GeneratePayroll` — 10) and `EmployeeProfileViewModel` (`Activate` / `Deactivate` / `Suspend` — 3)
  wrapped in the Wave A in-page `try`/`catch` + inline-error pattern. One new **additive**
  `ActionErrorMessage`/`HasActionError` pair per VM (private-set, no ctor change) → `Common_ActionFailedMessage`
  (reused from Wave A — no `.resx` change). Validation / `TryParse` early-returns / `CanExecute` / success
  path stay outside the `try`, byte-identical. `_onChanged?.Invoke()` (Activate/Deactivate/Suspend) is
  inside the `try` after the awaited command → a failed lifecycle change no longer triggers a parent
  reload; `await LoadAsync()` (self-guarded) kept inside the guarded block per the Wave A
  `CustomerProfileViewModel.SaveChangesAsync` precedent. `GenerateCommissions` leaves `StatusMessage`
  untouched on failure. Each catch reuses the VM's **existing** instance-form `[LoggerMessage]`
  (`LogOperationFailed(nameof(Method))`, operation-name-only) — no new logger, no DI change, no `SYSLIB1020`.
  `catch (Exception)` no-variable in all 13 → no `Exception.Message` / backend body / salary / payroll-net /
  commission value / employee PII / identifier surfaced or logged (test-enforced with seeded secrets).
  **No payroll / commission / attendance / leave business-logic change.** 2 prod + 5 HR stub command
  services (additive `Exception?` seams, null-path byte-identical; `GenerateCommissions` failure uses the
  pre-existing ctor delegate) + 2 test files, **+19 tests**. `Strings.cs` / `.resx` / services / DI /
  interfaces / backend contracts / RBAC / `AsyncRelayCommand` / `App.xaml.cs` / `LoadAsync` / every
  `[LoggerMessage]` signature untouched. Reports `ROJAN_PHASE8_69/70/71/72_*`. **Missing-Guard Sweep Wave B complete.**
- **Phase 8.74 — Missing-Guard Sweep Wave C / Inventory+Accounting** → `66c8490` (Phase 8.76): 7 unguarded
  command methods — `InventoryPageViewModel` (`CreateProduct` / `AddCategory` / `AddSupplier` — 3),
  `InventoryProfileViewModel` (`RecordTransaction` / `MapService` / `UnmapService` — 3),
  `AccountingPageViewModel.CancelInvoiceAsync` (1) — wrapped in the Wave A/B in-page `try`/`catch` +
  inline-error pattern. One new **additive** `ActionErrorMessage`/`HasActionError` pair per VM (private-set,
  no ctor change) → `Common_ActionFailedMessage` (reused — no `.resx` change). Validation / early-returns /
  `CanExecute` / request-building stay outside the `try`, byte-identical. **Stock consistency preserved** —
  every `InventoryProfileViewModel` write is followed by the authoritative `await LoadAsync()` inside the
  guarded block; on failure the reload is skipped and `Stock`/`RecentTransactions`/`ServiceMappings` keep
  last-known-good; page adds append the returned DTO only post-await. No manual recovery added. Inventory VMs
  reuse their existing instance-form `[LoggerMessage]`; `AccountingPageViewModel` reuses its **static-form**
  `LogOperationFailed(_logger, nameof(CancelInvoiceAsync))` (2 `ILogger` fields). `catch (Exception)`
  no-variable in all 7 → no `Exception.Message` / body / SKU / cost / supplier / stock / invoice-amount /
  payment / billing data surfaced or logged (test-enforced with seeded sentinels). **`CancelInvoiceAsync` is
  not a payment operation** — no invoice-cancellation / payment / rollback / transaction-rule change;
  **`PosCheckoutViewModel` / `ChargeAsync` / `IPaymentCommandService` untouched**; no retry loop, no
  idempotency assumption. This closes the Phase 8.10 `CancelInvoiceAsync`-no-try/catch backlog item.
  3 prod + `StubInventoryCommandService` (+6 additive `Exception?` seams, null-path byte-identical;
  `StubInvoiceCommandService` unchanged — used its pre-existing `cancelInvoice` delegate) + 3 test files,
  **+13 tests**. `Strings.cs` / `.resx` / services / DI / interfaces / DTOs / backend contracts / RBAC /
  `AsyncRelayCommand` / `App.xaml.cs` / `InvoiceProfileViewModel` / `LoadAsync` / every `[LoggerMessage]`
  signature untouched. Reports `ROJAN_PHASE8_73/74/75/76_*`. **Missing-Guard Sweep Wave C complete.**
- **Phase 8.78 — Missing-Guard Sweep Wave D / Organization** → `525fd4b` (Phase 8.80): 6 unguarded paths in
  the one `OrganizationPageViewModel` — `CreateOrganizationAsync` / `CreateBranchAsync` /
  `SaveBranchSettingsAsync` / `SwitchRoleAsync` (4 commands) + the two selection-triggered secondary-load
  **setter fire-and-forget paths** (`LoadBranchesForSelectedOrganizationAsync` /
  `LoadSettingsForSelectedBranchAsync`, via two new thin private wrappers `ReloadBranchesForSelectionAsync` /
  `ReloadSettingsForSelectionAsync`). One new **additive** `ActionErrorMessage`/`HasActionError` pair
  (private-set, no ctor change) → `Common_ActionFailedMessage` (reused — no `.resx` change). Validation /
  `CanExecute` / early-returns / `workingDays` list building stay outside the `try`, byte-identical;
  `TimeOnly.Parse` moved inside (malformed time now surfaces inline). Success-path side effects (form
  clears, `await LoadAsync()` reload, `StatusMessage`, `OnPropertyChanged` trio) unchanged and success-only.
  **`SwitchRoleAsync` special case:** on failure the catch reverts `SelectedRoleToSwitchTo =
  _currentSessionService.CurrentRole` so the two-way-bound picker agrees with the session's actual
  (unchanged) role again — the Wave-D analogue of `CustomerProfileViewModel.SaveChangesAsync`'s
  `EditableStatus` revert; the session's role is genuinely unchanged (the service throws before persisting),
  no RBAC behaviour change, no permission mutation, `OnPropertyChanged` fires success-only. **Secondary
  loads:** only the setter fire-and-forget path is wrapped — `LoadAsync` still `await`s the originals
  directly, so an initial-load failure still propagates to `LoadAsync`'s existing catch → `State = Error`
  (no regression); the `LoadAsync` body is byte-unchanged. Reuses the existing instance-form
  `[LoggerMessage]` (single `ILogger` → no `SYSLIB1020`); `catch (Exception)` no-variable in all 6 → no
  `Exception.Message` / org legal·tax data / VAT / receipt text / branch contacts / role / permission /
  identifier surfaced or logged (test-enforced with a seeded sentinel). 1 prod + 1 test file; the test
  file's 3 **private nested** doubles rewritten (2 upgraded with `Exception?` seams + 1 new
  `StubCurrentSessionService`) — **no shared `FakeCurrentSessionService` change**; the 2 pre-existing
  `LoadAsync` tests retained. **+12 tests**. `Strings.cs` / `.resx` / services / DI / `ICurrentSessionService`
  interface + impl / `IPermissionEngine` / `PermissionEngine` / RBAC infra / auth / navigation / Shell
  contracts / backend contracts / `AsyncRelayCommand` / `App.xaml.cs` / other VMs / `LoadAsync` body / every
  `[LoggerMessage]` signature untouched. Reports `ROJAN_PHASE8_77/78/79/80_*`. **Missing-Guard Sweep Wave D complete.**
- **Phase 8.82 — Missing-Guard Sweep Reporting mini-wave** → `5640123` (Phase 8.84): the 2 unguarded
  backend-connected snapshot commands in `ReportingPageViewModel` — `ToggleSavedAsync` (pin/unpin a saved
  report) and `DeleteSnapshotAsync` — wrapped in the Wave A–D `try`/`catch` + inline-error pattern.
  Guarding these two also covers the private `ReloadSnapshotsAsync` helper (no other unguarded caller —
  `LoadAsync` / `RunReportAsync` are already guarded). One new **additive** `ActionErrorMessage`/`HasActionError`
  pair (private-set, no ctor change) → `Common_ActionFailedMessage` (reused — no `.resx` change); it is
  **non-destructive** and deliberately distinct from **both** `State`/`ErrorMessage` (page not blanked)
  **and** `StatusMessage` (which keeps the last report-run result — "N rows" / "Run cancelled"). The
  command call + `await ReloadSnapshotsAsync()` follow-on stay verbatim inside the `try`; a reload failure
  after a successful toggle/delete also surfaces inline. Reuses the existing instance-form `[LoggerMessage]`
  (single `ILogger` → no `SYSLIB1020`); `catch (Exception)` no-variable in both → no `Exception.Message` /
  backend body / revenue / customer / report content / snapshot-id surfaced or logged (test-enforced with a
  seeded revenue/customer sentinel). **`AnalyticsPageViewModel` needed nothing** (audited clean).
  **Report generation (`RunReportAsync` + its `CancellationToken` handling) / `RerunSnapshotAsync` / export
  (`ExportDialogViewModel`) untouched.** 1 prod + `StubReportingServices.cs` (+3 additive `Exception?` seams,
  null-path byte-identical) + `ReportingPageViewModelTests.cs` (`CreateSut` +2 optional params — the 16
  existing tests unaffected), **+6 tests**. `Strings.cs` / `.resx` / services / DI / interfaces / DTOs /
  backend contracts / RBAC / auth / navigation / `IDialogService` / `AsyncRelayCommand` / `App.xaml.cs` /
  `DashboardPageViewModel` / every `[LoggerMessage]` signature / `LoadAsync` body untouched. Reports
  `ROJAN_PHASE8_81/82/83/84_*`. **Missing-Guard Sweep Reporting mini-wave complete.**
- **Phase 8.86 — Missing-Guard Sweep Export Dialog micro-phase** → `6f64ffa` (Phase 8.88): the last
  unguarded user-triggered action in the Reporting domain — `ExportDialogViewModel.ExportAsync`, which had
  a `try`/`finally` with **no `catch`** so an unexpected export failure (`Directory.CreateDirectory` /
  `File.WriteAllText` `IOException` / `UnauthorizedAccessException` / path-too-long, or the unknown-format
  `ArgumentOutOfRangeException`) escaped as an unobserved `async void` task exception → `App.DispatcherUnhandledException`.
  Now guarded via **Option B** (`ROJAN_PHASE8_85_*`): `sealed class` → `sealed partial class`; `+ ILoggerFactory?
  loggerFactory = null` ctor param → `_logger = loggerFactory?.CreateLogger<ExportDialogViewModel>() ??
  NullLogger<…>.Instance` (single `ILogger` field — Phase 8.56 inline idiom); `+ [LoggerMessage(EventId=1,
  Level=Error, "Export dialog operation failed. Operation={Operation}")]` — instance-form, **no `Exception`
  parameter**, no `SYSLIB1020`. New **additive non-destructive** `ActionErrorMessage`/`HasActionError` pair
  (private-set) → `Common_ActionFailedMessage` (reused — no `.resx` change). The `catch (Exception)`
  no-variable sits **between** the existing `try` and `finally`, so `finally { IsExporting = false; }` is
  byte-unchanged and always runs; the `try` body (`await _exportService.ExportAsync(...)` + the
  `StatusMessage = result.Success && result.FilePath is not null ? "{Message} ({FilePath})" : result.Message`
  ternary) is byte-unchanged — CSV success still shows the path, Pdf/Excel/Print still show the honest "not
  yet implemented" message. **`ReportingPageViewModel` — 4-line minimal parent forwarding** (the Phase 8.86
  STRICT-SCOPE "allowed" carve-out): `+ ILoggerFactory? _loggerFactory` field + optional ctor param +
  assignment; `OpenExportDialog()` passes `_loggerFactory` at the `new ExportDialogViewModel(...)` site
  (Phase 8.43 pattern — `ILoggerFactory` ≠ `ILogger`, so its own instance-form `[LoggerMessage]` is
  unaffected; ctor-compatible — the 22 `ReportingPageViewModelTests` unchanged). **`ReportExportService`
  implementation / `RunReportAsync` / `CancellationToken` logic / `RerunSnapshotAsync` / `AnalyticsPageViewModel`
  / `DashboardPageViewModel` / backend contracts / DI / RBAC / auth / navigation / `Strings.cs` / `.resx` /
  every other `[LoggerMessage]` signature untouched.** **Security improvement:** the file path that
  previously reached `App.LogUnhandledException` (which logs the full `Exception`) no longer reaches the
  log (operation name only) or the UI (fixed constant); test-enforced with a seeded `secretPath` +
  `customer=Amelia Hart` sentinel. 2 prod + `StubReportExportService` (+2 additive seams,
  null-path byte-identical) + **new `ExportDialogViewModelTests.cs`** (the VM had no test file), **+6 tests**.
  Reports `ROJAN_PHASE8_85/86/87/88_*`. **Missing-Guard Sweep Export Dialog micro-phase complete — the
  Reporting domain is now fully closed.**
- **Phase 8.90 — Missing-Guard Sweep Wave E / AI Center** → `4b1afca` (Phase 8.92): the 9 unguarded
  user-triggered command methods in the one `AiCenterPageViewModel` — `NewConversationAsync`,
  `OpenConversationAsync`, `TogglePinAsync`, `DeleteSessionAsync`, `SearchHistoryAsync`, `ClearHistoryAsync`,
  `ExportSessionAsync`, `SaveSettingsAsync`, `SaveConfigurationAsync` — wrapped in the Wave A–D
  `try`/`catch` + inline-error pattern (the 2 expression-bodied ones converted to block bodies). One new
  **additive non-destructive** `ActionErrorMessage`/`HasActionError` pair (private-set, no ctor change) →
  `Common_ActionFailedMessage` (reused — no `.resx` change); it touches **neither** `State`/`ErrorMessage`
  (page not blanked) **nor** `StatusMessage` (which keeps the last chat / "Settings saved." / "Model
  configuration saved." status — success-path only). `catch (Exception)` no-variable in all 9 → no
  `Exception.Message` / backend body / prompt / AI response / transcript / customer data / model-id /
  session-id surfaced or logged (test-enforced with seeded transcript / session-id / model-id sentinels).
  **State safety:** `CurrentSessionId` / `SelectedSection` are **not reset on failure** (the id set is
  valid; only a downstream read/reload failed); no session-collection corruption (a failed mutation throws
  before any result-dependent local change); `ExportSessionAsync` leaves `ExportPreviewText` **unwritten**
  (`null`) on failure — no partial transcript. Reuses the existing instance-form `[LoggerMessage]` (single
  `ILogger` → no `SYSLIB1020`, no `ILoggerFactory`, no DI change). The private helpers `ReloadSessionsAsync`
  / `EnsureActiveSessionAsync` / `LoadMessagesAsync` are covered transitively (no unguarded call path).
  **`LoadAsync` + `SendMessageAsync` (both already guarded Phase 8.23 — the chat-text-non-leak test) are
  untouched.** 1 prod + `StubAIRepository.cs` (Presentation.Tests-local; +7 additive `Exception?` seams,
  null-path byte-identical) + `AiCenterPageViewModelTests.cs` (the 15 existing tests unchanged), **+13
  tests**. AI service contracts / concrete services / `IAIRepository` / DI / RBAC / auth / navigation /
  `Strings.cs` / `.resx` / `ILocalizationService` / other VMs / every other `[LoggerMessage]` signature
  untouched. **Security improvement:** the prompt / response / transcript / identifier that previously
  reached `App.LogUnhandledException` (full-exception log) now reaches neither the log (operation name
  only) nor the UI. Reports `ROJAN_PHASE8_89/90/91/92_*`. **Missing-Guard Sweep Wave E complete — the AI
  Center domain is now fully closed.**
- **Phase 8.94 / 8.94.1 — Missing-Guard Sweep Wave F / Automation tabs** → `7c9c132` (Phase 8.96): 7
  unguarded user-triggered members across the 3 Automation tab VMs — `WorkflowsTabViewModel.ArchiveAsync`
  / `.DeleteAsync` / `.LoadVersionHistoryAsync` (the last a fire-and-forget from the `SelectedWorkflow`
  setter), `ScheduledJobsTabViewModel.DeleteAsync` / `.ToggleEnabledAsync` (the toggle added in the 8.94.1
  correction — it was outside 8.94's method list), `BusinessRulesTabViewModel.ToggleEnabledAsync` /
  `.DeleteAsync`. Each wrapped in the **filtered** `catch (Exception exception) when (exception is not
  OperationCanceledException)` (Phase 8.39 shape — cancellation propagates, no log, no `ErrorMessage`),
  reusing the VM's existing single `ILogger` + operation-name-only instance `[LoggerMessage]` (no
  `SYSLIB1020`, no `ILoggerFactory`, no DI / ctor change) and the existing inline `ErrorMessage` property
  set to the generic `Common_ActionFailedMessage` — **not** `exception.Message`, **no** `State = Error`,
  **no** new `ActionErrorMessage`. `LoadVersionHistoryAsync` additionally sets `ErrorMessage = null` on a
  successful load (it has no follow-on `LoadAsync`). Existing method bodies verbatim inside the `try`;
  `AutomationPageViewModel`, the other 2 tab VMs' commands, service/backend contracts, DI, RBAC, auth,
  navigation, `Strings.cs` / `.resx` untouched. 3 prod + `StubAutomationServices.cs` (+7 additive
  `Exception?` seams, null-path byte-identical) + 3 Automation test files, **+10 tests** (Presentation
  748 → 758). **Security improvement:** workflow definitions / business-rule payloads / cron expressions /
  customer triggers / backend exception bodies that previously reached `App.DispatcherUnhandledException`
  (full-exception log) now reach neither the log (operation name only) nor the UI (fixed constant);
  sentinel-enforced. Reports `ROJAN_PHASE8_93/94/94_1/95/96_*`. **Missing-Guard Sweep Wave F complete —
  Automation user-triggered command guard coverage is now complete (19/19); the Automation domain is fully closed.**
- **Phase 8.99 — Missing-Guard Sweep Settings-page P2 carve-out** → `0260bc3` (Phase 8.101; audited
  8.97/8.98): 6 `SettingsPageViewModel` commands guarded — `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`,
  `RefreshAvailablePacksAsync`, `DownloadOrInstallAsync`, `RemovePackAsync`, `SignOutAsync` (the command
  lambda promoted to a named method). `ApplyLanguageAsync` was outside the phase's method list. Each wrapped
  in the filtered `catch (Exception exception) when (exception is not OperationCanceledException)` (no token
  exists in this VM — the filter is defensive), failure sets the section's existing `ThemeStatusMessage` /
  `ApiEnvironmentStatusMessage` / `StatusMessage` — or the **new** `AccountStatusMessage` (SignOut, with one
  new XAML `<TextBlock>` using the pre-existing `CollectionToVisibilityConverter`) — to the generic
  `Common_ActionFailedMessage`. **No** `exception.Message`, **no API production-URL leak** (the `productionUrl`
  local is scoped inside the `try`), **no** `State = Error`. The `catch (NotSupportedException)` branches on
  `DownloadOrInstallAsync` / `RemovePackAsync` (the Phase 19A "coming soon" static string) are **kept**, with
  the new general branch added after. Class `sealed` → `sealed partial`; **optional** `ILogger<SettingsPageViewModel>? logger = null`
  6th ctor param (`NullLogger` fallback — **no DI change**, no ctor break) + one instance-form `[LoggerMessage]`
  (no `SYSLIB1020`, no `ILoggerFactory`). 1 prod VM + `SettingsPage.xaml` (1 `<TextBlock>` + 1 converter
  resource) + `SettingsPageViewModelTests.cs` + 4 Settings-local stub doubles (+5 additive `Exception?` seams,
  null-path byte-identical) — **+9 tests** (Presentation 758 → 767). `MainWindowViewModel`, Shell infra, DI
  registration, `IAuthenticationService` / backend contracts, other VMs, `Strings.cs` / `.resx` untouched.
  **Security improvement:** a failing theme / API-env / language-pack / sign-out op that previously reached
  `App.DispatcherUnhandledException` (full-exception log — potentially the internal API URL or an auth body)
  now reaches neither the log (operation name only) nor the UI (fixed constant); sentinel-enforced.
  **Non-blocking known follow-up (Phase 8.99.1 / P2):** the 3 pre-existing `*StatusMessage` TextBlocks are
  visibility-gated on `Is*RestartRequired == True`, so a failure message is *set on the property* (test-verified)
  but not *visually shown* for the Theme / API / pack-refresh sections until those 3 triggers are broadened to
  a non-empty-string test (behaviour-equivalent for the success path; also fixes the latent invisibility of the
  Download/Remove "coming soon" message). `AccountStatusMessage` (SignOut) **does** display. Reports
  `ROJAN_PHASE8_97/98/99/100/101_*`. **Missing-Guard Sweep Settings carve-out complete — every backend-connected
  user-triggered command in the app is now guarded.**
- **Phase 8.104 — "sanitize load-error surfacing" P2, sub-wave 1 (Reporting + AI Center + Accounting/POS)**
  → `76d3f61` (Phase 8.106; audited 8.102/8.103): 11 pre-existing top-level broad-catch UI surfaces sanitized
  — `ReportingPageViewModel` (`LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync`), `AiCenterPageViewModel`
  (`LoadAsync` / `SendMessageAsync`), `AccountingPageViewModel` (`LoadAsync` / `SearchAsync`),
  `PosCheckoutViewModel` (`LoadOptionsAsync` / `ProceedToPaymentAsync` / `ChargeAsync`),
  `InvoiceProfileViewModel` (`LoadAsync`). Each: `catch (Exception exception) { <Surface> = exception.Message;
  … }` → `catch (Exception) { <Surface> = Strings.Common_ActionFailedMessage; … }` — the caught exception is
  no longer bound, so `.Message` is structurally unreachable from the surface. `State = DashboardState.Error`,
  the `RunReportAsync` `catch (OperationCanceledException) → Reporting_RunCancelled` branch, every `finally`
  (`IsRunning` / `IsSending`), the `SearchAsync` out-of-order-completion guard, the POS re-charge semantics,
  and every operation-name-only `[LoggerMessage]` call are byte-unchanged. `PosCheckoutViewModel` +
  `InvoiceProfileViewModel` each `+ using Rojan.Desktop.Presentation.Localization;`. **No** localization
  (`Common_ActionFailedMessage` reused) / DI / service / contract / stub change. 5 prod + 5 existing test
  files, **+3 net tests** (Presentation 767 → 770) — sentinel-enforced no-leak assertions for customer names,
  revenue figures, payment-gateway detail, and financial detail. **Security improvement:** a failed report
  run / AI chat / POS charge / invoice load that previously showed a raw backend body, internal URL, customer
  name, revenue figure, or payment-gateway decline reason now shows only the generic constant — including the
  **confirmed live `AiCenterPageViewModel.SendMessageAsync` leak** where `StatusMessage` displayed
  `"upstream failed for customer Sarah Johnson"`. Reports `ROJAN_PHASE8_102/103/104/105/106_*`.
- **Phase 8.108 — "sanitize load-error surfacing" P2, sub-wave 2 (Customers + HR + Membership)** → `1260d4e`
  (Phase 8.110; audited 8.107): 6 of the 7 audited sites — `CustomerPageViewModel.LoadAsync`,
  `HrPageViewModel.LoadAsync` / `.SearchAsync`, `EmployeeProfileViewModel.LoadAsync`,
  `AcceptInviteViewModel.LookupAsync` (`LookupErrorMessage`) / `.AcceptAsync` (`AcceptErrorMessage`). Same
  swap: `catch (Exception exception) { <Surface> = exception.Message; … }` → `catch (Exception) { <Surface>
  = Strings.Common_ActionFailedMessage; … }` — no exception variable bound. `State = DashboardState.Error`,
  the `Has*Error` flags, the `CustomerPageViewModel` stale-response guard, the `HrPageViewModel.SearchAsync`
  out-of-order guard, both `AcceptInviteViewModel` `finally` blocks, and every operation-name-only
  `[LoggerMessage]` call byte-unchanged. **No `using` additions in prod** (all 4 already import
  `…Localization`); no localization / DI / service / contract / stub change. 4 prod + 4 existing test files,
  **+1 net test** (`HrPageViewModelTests.SearchAsync_QueryThrows_…`; Presentation 770 → 771). **Security:**
  `AcceptInviteViewModel`'s **live, test-documented invite-token leak** (`AcceptInviteViewModelTests:144`
  used to assert `Contains(SecretToken, sut.LookupErrorMessage!)` with the comment *"the user still sees the
  raw backend message"*) plus the undetected `AcceptErrorMessage` token / invitee-email / user-id leaks are
  **closed**; customer PII and salary / payroll / commission figures no longer reach any UI surface;
  sentinel-enforced. The Phase 8.35 token-safe / identity-safe **log** assertions are retained. **Deferred:**
  `CustomerProfileViewModel.LoadAsync` (site 7 of 7) was outside this phase's authorised file list — remains
  for a follow-up. Reports `ROJAN_PHASE8_107/108/109/110_*`.
- **Phase 8.112 — "sanitize load-error surfacing" P2, sub-wave 3 (Organization + Specialists + Services)**
  → `b509054` (Phase 8.114; audited 8.111): 8 sites / 7 VMs — `OrganizationPageViewModel.LoadAsync`,
  `SpecialistPageViewModel.LoadAsync`, `SpecialistProfileViewModel.LoadAsync`,
  `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync` (the shared 8-caller mutation boundary),
  `SpecialistAvailabilityViewModel.LoadAsync`, `ServicePageViewModel.LoadAsync`,
  `ServiceProfileViewModel.LoadAsync`. Same swap — no exception variable bound. `State = DashboardState.Error`,
  **both `catch (UnauthorizedOperationException)` typed branches** in `SpecialistScheduleViewModel`
  (`IsPermissionDenied` + **Warning** log via `LogPermissionDenied`), the `[CallerMemberName] operationName`
  parameter, `TryMutateAsync`'s success-path `IsPermissionDenied = false; ErrorMessage = null; return true;`,
  and the `SpecialistPageViewModel` / `ServicePageViewModel` stale-response guards are byte-unchanged. Every
  log call unchanged (`LogOperationFailed` / static-form `LogOperationFailed(Logger, …)` for
  `SpecialistPageViewModel` / `LogLoadFailed` for `SpecialistAvailabilityViewModel`). `+ using …Localization;`
  in 1 prod (`SpecialistAvailabilityViewModel`) + 3 test files; `OrganizationPageViewModel` keeps its
  fully-qualified `Rojan.Desktop.Presentation.Localization.Strings.` form. No `.resx` / DI / service /
  contract / stub change. 7 prod + 7 existing test files, **+1 net test**
  (`SpecialistScheduleViewModelTests.SetWeeklyAvailabilityCommand_BackendThrows_SetsGenericErrorMessage_NoLeak`
  — covers `TryMutateAsync` via the pre-existing `Fail` seam; Presentation 771 → 772). **Security:** RBAC
  role/permission strings, staff PII, specialist identifiers, availability windows, and service pricing /
  cost / commission % no longer reach any UI surface; sentinel-enforced. Reports `ROJAN_PHASE8_111/112/113/114_*`.
- **Phase 8.116 + 8.117.1 — "sanitize load-error surfacing" P2, sub-wave 4 (Automation tabs)**
  → `d10f9bc` (scope-reviewed 8.117 / 8.118; committed 8.119; audited 8.115): **13 of 13 sites / 5 tab VMs** —
  `WorkflowsTabViewModel` (`LoadAsync` / `CreateDraftAsync` / `PublishAsync` / `RunNowAsync` / `RollbackAsync`),
  `ScheduledJobsTabViewModel` (`LoadAsync` / `CreateAsync` / `RunNowAsync`), `BusinessRulesTabViewModel`
  (`LoadAsync` / `CreateAsync`), `ApprovalsTabViewModel` (`LoadAsync` / `DecideAsync`),
  `AutomationDashboardTabViewModel` (`LoadAsync`). Phase 8.116 did the first 10 (Workflows/ScheduledJobs/
  BusinessRules); the Phase 8.117.1 addendum closed the last 3 (`ApprovalsTabViewModel` ×2 +
  `AutomationDashboardTabViewModel` ×1 — outside 8.116's authorised file list). Every site is the Phase 8.39
  filtered shape `catch (Exception exception) when (exception is not OperationCanceledException)`; the `when`
  predicate references `exception`, so the fix is minimal — **only** `ErrorMessage = exception.Message;` →
  `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`. The `catch` clause, every
  `State = DashboardState.Error`, every `LogOperationFailed(nameof(<Method>))`, both `[LoggerMessage]`
  signatures, and the `await LoadAsync()` reloads are byte-unchanged. **No prod `using` addition** (FQ form);
  2 test files `+ using …Localization;`. No `.resx` / DI / service / contract / stub change. 5 prod + 5 test,
  **+0 net tests** (13 surface no-leak assertions on the existing Phase 8.39 tests; `AssertGenericSurfaceNoLeak`
  helper in `WorkflowsTabViewModelTests`). **Security:** workflow definitions, cron expressions, business-rule
  conditions/actions, approval decision comments, dashboard workflow names, execution details, and backend
  payloads no longer reach any UI surface; logs operation-name-only. **Sub-wave 4 complete — 13/13.** Reports
  `ROJAN_PHASE8_115/116/117/117_1/118/119_*`.
- **Phase 8.121 — "sanitize load-error surfacing" P2, sub-wave 5 (Booking + Calendar + Inventory)**
  → `71fb472` (audited 8.120; scope-reviewed 8.122; committed 8.123): **11 sites / 4 VMs** —
  `BookingPageViewModel` (`LoadAsync` / `CreateBookingAsync` / `ChangeStatusAsync` /
  `CancelSelectedBookingAsync` / `RescheduleSelectedBookingAsync`), `CalendarPageViewModel` (`InitializeAsync`
  / `LoadDailyAvailabilityAsync` / `LoadWeeklyAvailabilityAsync`), `InventoryPageViewModel` (`LoadAsync` /
  `SearchAsync`), `InventoryProfileViewModel` (`LoadAsync`). All plain `catch (Exception exception)` (sub-wave
  2/3 shape — variable dropped): `catch (Exception exception)` → `catch (Exception)`,
  `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`. The
  `#pragma warning disable/restore CA1031` pair, every `State = DashboardState.Error`, every operation-name-only
  log call (`LogOperationFailed` / `LogLoadFailed(nameof(<Method>))`), the `BookingPageViewModel.LoadAsync`
  stale-response `if (requestVersion == _filterVersion)` guard, the `InventoryPageViewModel.SearchAsync`
  out-of-order `if (string.Equals(searchText, SearchText, Ordinal))` guard, the 4 Booking-command
  `await LoadAsync()` reloads, and the Calendar null guards are byte-unchanged. `+ using
  Rojan.Desktop.Presentation.Localization;` in 2 prod (`BookingPageViewModel`, `CalendarPageViewModel`) + 2
  test files; the 2 Inventory VMs + tests already imported it (Wave C `66c8490`). No `.resx` / DI / service /
  contract / stub change. 4 prod + 4 existing test files, **+0 net tests** (11 raw-message failure-test
  assertions updated to `Strings.Common_ActionFailedMessage`; 3 `DoesNotContain` sentinel additions).
  **Security:** customer names / appointment times / specialist assignments, staff schedules & availability,
  and stock levels / supplier names+terms / cost prices / transaction history no longer reach any UI surface;
  sentinel-enforced. **Two confirmed live test-documented backend-body leaks closed** —
  `BookingPageViewModel.CreateBookingAsync` and `CalendarPageViewModel.InitializeAsync` previously had tests
  asserting `Assert.Equal(backendBody, sut.ErrorMessage)`. Logs remain operation-name-only. **Sub-wave 5
  complete — 11/11.** Reports `ROJAN_PHASE8_120/121/122/123_*`.
- **Phase 8.125 — "sanitize load-error surfacing" P2, sub-wave 6 / FINAL (Dashboard + Analytics + Salon + QR
  + Support + CustomerProfile)** → `17306d9` (audited 8.124; scope-reviewed 8.126; committed 8.127): **9
  Category-A sites / 6 VMs** — `DashboardPageViewModel.LoadAsync`, `AnalyticsPageViewModel.LoadAsync`,
  `SalonPageViewModel` (`LoadAsync` / `CreateSalonAsync`), `QrCodesPageViewModel` (`LoadAsync` /
  `GenerateReceptionInviteAsync`), `SupportPageViewModel` (`SubmitMessageAsync` / `SubmitApplicationAsync`),
  `CustomerProfileViewModel.LoadAsync` (carried over from sub-wave 2). 7 plain `catch (Exception exception)` →
  `catch (Exception)` + `= Strings.Common_ActionFailedMessage;`; the 2 Support catches are filtered
  `when (exception is not OperationCanceledException)` — the `when` clause + variable byte-unchanged, only the
  assignment swapped (`Localization.Strings.Common_ActionFailedMessage`, FQ). `#pragma CA1031` pairs, every
  `State = DashboardState.Error` (5 sites), every operation-name-only log call, the `CreateSalonAsync` +
  `GenerateReceptionInviteAsync` `finally` blocks, the QR `if (Salon is null) return;` guard, the Support
  status resets + 11-field success-path form-clears, and the Dashboard financial-KPI gating are byte-unchanged.
  `+ using …Localization;` in 3 prod (`Analytics`, `Salons`, `QrCodes`) + 5 test; `Dashboard` /
  `CustomerProfile` + `CustomerProfileViewModelTests` already had it; `SupportPageViewModel` prod keeps the FQ
  form. No `.resx` / DI / service / contract / stub change. 6 prod + 6 test, **+0 net tests** (~14 raw-message
  assertions updated; 5 `DoesNotContain` sentinels). **Security:** Dashboard revenue / financial KPIs,
  analytics insights, salon config + owner PII, QR **invite tokens**, Support **applicant PII**, and
  **customer PII** + notes + history no longer reach any UI surface; sentinel-enforced. **Three confirmed
  live test-documented backend leaks closed** — Dashboard `LoadAsync`, Salon `CreateSalonAsync`, QR
  `GenerateReceptionInviteAsync`. Logs operation-name-only. **Sub-wave 6 complete — 9/9. The P2 track is
  COMPLETE — all 58 Category-A `= exception.Message` UI surfaces sanitized.** The 2 `SettingsPageViewModel`
  `NotSupportedException`→`StatusMessage` branches (Category-D — local fixed developer string from
  `LocalOnlyLanguagePackRepository`) are deliberately excluded; NOT a security leak. Reports
  `ROJAN_PHASE8_124/125/126/127_*`.

**Missing-Guard Sweep — track progress** (Production Hardening reliability; `ROJAN_PHASE8_64_*` §E.2):
Wave A ✅ (`794648e`), Wave B / HR ✅ (`a5be831`), Wave C / Inventory+Accounting ✅ (`66c8490`),
Wave D / Organization ✅ (`525fd4b`), Reporting mini-wave ✅ (`5640123`), Export Dialog micro-phase ✅
(`6f64ffa`), Wave E / AI Center ✅ (`4b1afca`), **Wave F / Automation tabs ✅ (`7c9c132`, Phase 8.94 +
8.94.1, committed 8.96)** — **the Reporting, AI Center and Automation domains are fully closed.**
Wave F guarded 7 members: `WorkflowsTabViewModel` (`ArchiveAsync` / `DeleteAsync` / `LoadVersionHistoryAsync`),
`ScheduledJobsTabViewModel` (`DeleteAsync` / `ToggleEnabledAsync`), `BusinessRulesTabViewModel`
(`ToggleEnabledAsync` / `DeleteAsync`); `AutomationDashboardTabViewModel` + `ApprovalsTabViewModel` were
already clean. The guards match the tabs' existing **filtered** `catch (Exception exception) when (exception
is not OperationCanceledException)` shape (Phase 8.39), reuse the existing `ErrorMessage` property with the
generic `Common_ActionFailedMessage` constant (no `exception.Message`, no `State = Error`, no
`ActionErrorMessage`); `LoadVersionHistoryAsync` also clears `ErrorMessage` on success. **Automation
user-triggered command guard coverage is now complete — 19/19.** +10 tests, +7 additive `Exception?` stub
seams. Reports `ROJAN_PHASE8_93/94/94_1/95/96_*`.
**Wave G (P2) audited at Phase 8.97** (`ROJAN_PHASE8_97_WAVE_G_P2_INFRA_SCOPE_AUDIT_v1.md`): the 4 target
VMs — `WorkspaceHostViewModel`, `NotificationCenterViewModel`, `CommandPaletteViewModel`,
`SettingsPageViewModel` — carry ~34 async user-triggered methods (~22 Category-A), but **all backing stores
are local** (`LocalWorkspaceStore` / `LocalNotificationRepository` / `LocalSearchHistoryStore` /
`LocalSearchFavoritesStore` / local settings — only `SettingsPageViewModel.SignOutCommand` is backend/auth),
failures are non-destructive + already recovered, and **none of the 4 has an `ILogger` or (3/4) an error
surface** — 3 are `new`'d in `Rojan.Desktop.Shell/MainWindowViewModel` (which itself has no logger), so a
real guard needs a Shell-project `ILoggerFactory` injection + new bindable error props + XAML. **Verdict:
P2 infra, not a P1 gap. Recommendation: DEFER (Option B)** with an optional low-cost carve-out — a
`SettingsPageViewModel`-only micro-wave. Accept
`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel` as P3.
**The `SettingsPageViewModel` carve-out was audited at Phase 8.98** (`ROJAN_PHASE8_98_SETTINGS_SCOPE_AUDIT_v1.md`):
DI-registered, Presentation-only, has 3 existing section-scoped `*StatusMessage` surfaces (all XAML-bound),
no `ILogger`, no `CancellationToken`, 2 partial `NotSupportedException` guards. **6 methods to guard**
(`ApplyLanguageAsync` / `ApplyThemeAsync` / `ApplyApiEnvironmentAsync` / `RefreshAvailablePacksAsync` /
`SignOutAsync` + broaden `DownloadOrInstallAsync` / `RemovePackAsync`), reuse the `*StatusMessage` surfaces
(`SignOutAsync` needs either a minimal new `AccountStatusMessage` + 1 XAML line, or log-only), add the
standard `ILogger<T>? = null` optional-param + instance `[LoggerMessage]` (no DI change), ~4 additive stub
seams (`StubLocalizationService.ThrowOnSetLanguage` already exists), **~8 tests**, suite ~2,701 → ~2,709.
**The `SettingsPageViewModel` carve-out was implemented at Phase 8.99 and committed at Phase 8.101 → `0260bc3`**
(6 commands guarded — `ApplyThemeAsync` / `ApplyApiEnvironmentAsync` / `RefreshAvailablePacksAsync` /
`DownloadOrInstallAsync` / `RemovePackAsync` / `SignOutAsync`; `ApplyLanguageAsync` was outside the phase's
list; filtered catch, generic `Common_ActionFailedMessage`, reuse the 3 `*StatusMessage` surfaces + new
`AccountStatusMessage`; optional `ILogger<T>? = null`, no DI change; +9 tests → 2,710). **Known non-blocking
follow-up (Phase 8.99.1 / P2): broaden the 3 `Is*RestartRequired`-gated `*StatusMessage` TextBlock visibility
triggers to a non-empty-string test** so failure text shows (behaviour-equivalent for success; also fixes the
latent Download/Remove "coming soon" invisibility). ~3 TextBlock edits, no VM change, LOW risk.

**→ Missing-Guard Sweep status: every backend-connected user-triggered command in the app is now guarded.**
The reusable pattern (in-page `try`/`catch` + inline error prop + reuse existing `[LoggerMessage]` +
`Common_ActionFailedMessage`) is set by Waves A–F + the Settings carve-out. **Remaining, all documented, none
a P1/P0:** the 3 local-only infra VMs (`WorkspaceHostViewModel` / `NotificationCenterViewModel` /
`CommandPaletteViewModel`) as **P3** — local persistence, non-destructive, Shell-project cost (§8.97 §F); the
Phase 8.99.1 XAML visibility tweak; and the "sanitize load-error surfacing" P2 below.

**"Sanitize load-error surfacing" P2 — the active track** (`ROJAN_PHASE8_102_*`: 58 Category-A
`= exception.Message` UI surfaces / 30 VMs; ~6 priority-ordered domain sub-waves; uniform behaviour-neutral
fix — drop the `catch` variable, swap to `Strings.Common_ActionFailedMessage`, keep `State = Error` +
`LogOperationFailed`; no localization / DI / service / logging change):
- **sub-wave 1 (Reporting + AI Center + Accounting/POS, 11 sites) ✅ `76d3f61`** (Phase 8.104, committed 8.106).
- **sub-wave 2 (Customers + HR + Membership, 6 of 7 sites) ✅ `1260d4e`** (Phase 8.108, committed 8.110) —
  `AcceptInviteViewModel` live token / email / user-id UI leak closed; `CustomerProfileViewModel.LoadAsync`
  (site 7) deferred (was outside 8.108's file list).
- **sub-wave 3 (Organization + Specialists + Services, 8 sites / 7 VMs) ✅ `b509054`** (Phase 8.112, committed
  8.114) — `OrganizationPageViewModel.LoadAsync`, `SpecialistPageViewModel.LoadAsync`,
  `SpecialistProfileViewModel.LoadAsync`, `SpecialistScheduleViewModel.LoadAsync` / `.TryMutateAsync`
  (shared 8-caller mutation boundary), `SpecialistAvailabilityViewModel.LoadAsync`,
  `ServicePageViewModel.LoadAsync`, `ServiceProfileViewModel.LoadAsync`. Both `SpecialistScheduleViewModel`
  `catch (UnauthorizedOperationException)` branches + the `[CallerMemberName]` arg + the `TryMutateAsync`
  success path kept; RBAC / staff PII / specialist id / availability / service pricing no longer reach the UI.
- **sub-wave 4 — Automation tabs (13 / 13 sites / 5 tab VMs) ✅ `d10f9bc`** (Phase 8.116 did 10; Phase 8.117.1
  addendum closed the last 3; scope-reviewed 8.117 / 8.118; committed 8.119; audited 8.115) —
  `WorkflowsTabViewModel` ×5, `ScheduledJobsTabViewModel` ×3, `BusinessRulesTabViewModel` ×2,
  `ApprovalsTabViewModel` ×2, `AutomationDashboardTabViewModel` ×1. Every site is the Phase 8.39 filtered
  shape `catch (Exception exception) when (exception is not OperationCanceledException)` — the `when` clause
  keeps `exception` bound, so the fix was minimal: **only** `ErrorMessage = exception.Message;` →
  `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;`; the filter, `State = Error`,
  `LogOperationFailed`, and `await LoadAsync()` reloads byte-unchanged. No `catch`-clause / prod-`using` /
  `.resx` change; 2 test files `+ using …Localization;`; +0 net tests (no-leak assertions on the existing
  Phase 8.39 tests). Workflow definitions, cron expressions, business-rule conditions/actions, approval
  decision comments, dashboard workflow names, execution details all no longer reach the UI.
- **sub-wave 5 — Booking + Calendar + Inventory (11 / 11 sites / 4 VMs) ✅ `71fb472`** (audited 8.120;
  scope-reviewed 8.122; committed 8.123) — `BookingPageViewModel` ×5, `CalendarPageViewModel` ×3,
  `InventoryPageViewModel` ×2, `InventoryProfileViewModel` ×1. All plain `catch (Exception exception)` →
  `catch (Exception)`, `ErrorMessage = exception.Message;` → `ErrorMessage = Strings.Common_ActionFailedMessage;`.
  `#pragma CA1031` pair, `State = Error`, operation-name-only log calls, the Booking stale-response guard and
  the Inventory out-of-order guard byte-unchanged. `+ using …Localization;` in 2 prod + 2 test (Inventory pair
  already had it); no `.resx` / DI / service / stub change; +0 net tests. **2 confirmed live test-documented
  backend-body leaks closed** (`BookingPageViewModel.CreateBookingAsync`, `CalendarPageViewModel.InitializeAsync`).
  Customer/appointment/specialist data, staff schedules & availability, stock/supplier/cost data no longer
  reach the UI.
- **sub-wave 6 / FINAL — Dashboard + Analytics + Salon + QR + Support + CustomerProfile (9 / 9 Category-A
  sites / 6 VMs) ✅ `17306d9`** (audited 8.124; scope-reviewed 8.126; committed 8.127) —
  `DashboardPageViewModel.LoadAsync`, `AnalyticsPageViewModel.LoadAsync`, `SalonPageViewModel` ×2,
  `QrCodesPageViewModel` ×2, `SupportPageViewModel` ×2 (filtered `when` shape — `exception` kept),
  `CustomerProfileViewModel.LoadAsync`. `#pragma CA1031`, `State = Error` (5 sites), operation-name-only log
  calls, the 2 `finally` blocks (Salon `CreateSalonAsync`, QR `GenerateReceptionInviteAsync`), the QR
  `Salon is null` guard, Support success-path form-clears byte-unchanged. `+ using …Localization;` in 3 prod
  + 5 test; Support prod keeps FQ form; no `.resx` / DI / service / stub change; +0 net tests. **3 confirmed
  live test-documented backend leaks closed** (Dashboard `LoadAsync`, Salon `CreateSalonAsync`, QR
  `GenerateReceptionInviteAsync`). Revenue / financial KPIs, analytics insights, salon config PII, invite
  tokens, applicant PII, customer PII no longer reach the UI. **P2 track COMPLETE — all 58 Category-A sites
  sanitized.**
  The 2 `SettingsPageViewModel` `NotSupportedException`→`StatusMessage` branches (Category D — local fixed
  developer string from `LocalOnlyLanguagePackRepository`) are deliberately excluded; NOT a security leak
  (optional localization-polish follow-up only).
  `LoginViewModel` / `MobileOtpLoginViewModel` are already correct (typed catches → `Strings.*`).

**Other backlog** (none authorized): `CancellationToken` propagation (`CommandPaletteViewModel` first);
Wave G P3 (`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`); Startup UX;
the `HttpApiClient` Infra-observability payload decision; the pre-existing API-Environment "Restart Now"
button mislabel (`Settings_Theme_RestartNow` — 1-line fix). **The Phase 8.99.1 Settings XAML visibility tweak
is DONE (`58a2c88`).**

**Not authorized yet** — recommendations only. Follow the same rhythm: audit → scope review (readiness
only) → implementation authorization → implement + validate → commit scope review → commit execution,
explicit-path staging only, one isolated commit per group.

---

## H. Continuation Instructions

1. **Read this file first.** Phases 8.6, 8.11, 8.15, 8.19, 8.23, 8.27, 8.31, 8.35, 8.39, 8.43, 8.47, 8.51,
   8.56, 8.61, 8.66, 8.70, 8.74, 8.78, 8.82, 8.86, 8.90, 8.94 (+ 8.94.1), 8.99, 8.104, 8.108, 8.112, 8.116 (+ 8.117.1), 8.121, 8.125, 8.129 are done. The ViewModel diagnostic-logging architecture is CLOSED and RULE-CONSISTENT.
   **The Missing-Guard Sweep is complete** (Waves A–F + Settings carve-out `0260bc3` — every backend-connected
   user-triggered command is guarded). **The "sanitize load-error surfacing" P2 track is COMPLETE** —
   all 58 Category-A `= exception.Message` UI surfaces / 30 VMs (`ROJAN_PHASE8_102_*`) sanitized over 6 domain
   sub-waves: **sub-wave 1 (Reporting/AI Center/Accounting+POS) `76d3f61` (8.104/8.106); sub-wave 2
   (Customers/HR/Membership, 6/7) `1260d4e` (8.108/8.110); sub-wave 3 (Organization/Specialists/Services, 8)
   `b509054` (8.112/8.114); sub-wave 4 (Automation tabs, 13/13) `d10f9bc` (8.116 + 8.117.1, committed 8.119);
   sub-wave 5 (Booking/Calendar/Inventory, 11/11) `71fb472` (8.121, committed 8.123); sub-wave 6 / FINAL
   (Dashboard/Analytics/Salon/QR/Support/CustomerProfile, 9/9) `17306d9` (8.125, committed 8.127)**. **The only
   `= exception.Message` left is the 2 `SettingsPageViewModel` `NotSupportedException` Category-D branches — a
   hard-coded local developer string, deliberately excluded.** **The Phase 8.99.1 `SettingsPage.xaml`
   visibility fix is DONE (`58a2c88`, Phase 8.129, committed 8.131).** Remaining P3: the 3 local-only infra VMs
   (`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`) — see §G. Both the
   Missing-Guard Sweep and the P2 sanitization track (plus this Settings follow-up) are now closed; the
   Desktop error-handling / reliability / diagnostic-logging surface is fully done.
2. **Verify state hasn't drifted** before doing anything: `git rev-parse HEAD` should be `58a2c88` (or
   later, if further work has landed — check `git log --oneline -20` against §B's table), and
   `git status` should show a clean tracked tree unless work is genuinely in progress.
3. **Do not re-litigate settled architecture decisions** (§C) without new evidence — they were each
   independently verified, not merely asserted, across multiple phases.
4. **Do not attempt Inventory/HR/Accounting backend integration** — all three remain blocked entirely
   upstream on Backend/Team 1 (§D); Desktop-side prep is already complete and re-confirmed exhaustively
   as of Phase 8.0.
5. **Phases 8.6/8.11/8.15/8.19/8.23/8.27/8.31/8.35/8.39/8.43/8.47/8.51/8.56/8.61/8.66/8.70/8.74/8.78/8.82/8.86/8.90/8.94/8.99/8.104/8.108/8.112/8.116(+8.117.1)/8.121/8.125/8.129 are committed** (`94fca6a`,
   `2453a7f`, `31f4b63`, `75357e1`, `2ed685a`, `cbc3a82`, `0542041`, `38c24da`, `c01d0ce`, `7aa1d1b`,
   `884cec3`, `5b7f6ca`, `6a1bced`, `5ba554c`, `794648e`, `a5be831`, `66c8490`, `525fd4b`, `5640123`, `6f64ffa`, `4b1afca`, `7c9c132`, `0260bc3`, `76d3f61`, `1260d4e`, `b509054`, `d10f9bc`, `71fb472`, `17306d9`, `58a2c88`) — do not re-implement. Navigation: bounded (20)
   `LinkedList<T>` deque, FIFO eviction. **Logging architecture CLOSED & RULE-CONSISTENT (33/55):** every
   ViewModel with a swallowing broad `catch (Exception)` surfacing a user-facing error state is
   instrumented at `Error` (Mobile OTP + SpecialistSchedule-permission at `Warning`); **every ViewModel
   `[LoggerMessage]` is operation-name-only** — the caught `Exception` and any record identifier are never
   passed (legacy harmonization `5ba554c`). Automation tabs use parent→child `ILogger<TChild>?` pass-through
   via `AutomationPageViewModel`; the 6 profile panels + `BookingWizardViewModel` use **`ILoggerFactory`**
   pass-through via their page parents; `SpecialistPageViewModel` + `AccountingPageViewModel` use
   **static-form** `[LoggerMessage]` (2 pre-existing `ILogger` fields → static form avoids `SYSLIB1020`).
   `BookingWizardViewModel.SearchNextAvailableDateAsync` is a deliberate test-guarded skip. Remaining 22
   uninstrumented VMs have no failure boundary. Only `App.LogUnhandledException` + `HttpApiClient` still
   log an `Exception` (both intentional, outside the ViewModel track). See
   `ROJAN_PHASE8_8/13/17/21/25/29/33/37/41/45/49/53/58/63_*`.
6. **To continue hardening:** the **Missing-Guard Sweep** is the active track (Wave A `794648e`, Wave B/HR
   `a5be831`, Wave C/Inventory+Accounting `66c8490`, Wave D/Organization `525fd4b`, Reporting mini-wave
   `5640123`, Export Dialog micro-phase `6f64ffa`, Wave E/AI Center `4b1afca`, **Wave F/Automation tabs
   `7c9c132` (Phase 8.94 + 8.94.1, committed 8.96)** done — **the Reporting, AI Center and Automation
   domains are fully closed; Automation user-triggered command guard coverage is complete, 19/19**).
   **Wave G (P2 infra) audited at Phase 8.97 → verdict P2; the `SettingsPageViewModel` carve-out was
   implemented (8.99) and committed (8.101) → `0260bc3`** (6 commands guarded, +9 tests → 2,710). **The
   Missing-Guard Sweep is now effectively complete — every backend-connected user-triggered command in the
   app is guarded.** Remaining, all P3 / documented:
   (a) **`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`** — local-only
   persistence, non-destructive, Shell-project `ILoggerFactory` + XAML cost, MEDIUM risk, disproportionate
   (§8.97 §F). Accepted as **P3**.
   (b) **Phase 8.99.1 — DONE (`58a2c88`, Phase 8.129, committed 8.131).** The 3 `Is*RestartRequired`-gated
   `*StatusMessage` `TextBlock`s in `SettingsPage.xaml` now use a non-empty-string
   `CollectionToVisibilityConverter` binding (the `AccountStatusMessage` pattern), so the Phase 8.99
   Settings-guard failure text is visible. XAML only, +0 tests. The 3 "Restart Now" buttons keep their
   `Is*RestartRequired` gate.
   (c) The **"sanitize load-error surfacing" P2** — **audited at Phase 8.102**
   (`ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md`): **58 Category-A sites across 30 ViewModels**
   surface `exception.Message` to a bound `TextBlock` from a top-level broad catch (backend bodies / URLs /
   file paths / PII / revenue data / AI responses), contradicting the already-safe operation-name-only
   logging in the same catch. Uniform behaviour-neutral fix: drop the `catch` variable, swap
   `= exception.Message` → `= Strings.Common_ActionFailedMessage` (key already ships — **no localization / DI
   / service / logging change**), keep `State = Error` + the `LogOperationFailed` call. **~6 domain sub-waves,
   priority-ordered:**
   **(1) Reporting + AI Center + Accounting/POS (11 sites) ✅ `76d3f61` (Phase 8.104, committed 8.106).**
   **(2) Customers + HR + Membership (6 of 7 sites) ✅ `1260d4e` (Phase 8.108, committed 8.110)** —
   `CustomerPageViewModel.LoadAsync`, `HrPageViewModel.LoadAsync` / `.SearchAsync`,
   `EmployeeProfileViewModel.LoadAsync`, `AcceptInviteViewModel.LookupAsync` / `.AcceptAsync`.
   `AcceptInviteViewModel`'s live test-documented invite-token / invitee-email / user-id UI leak closed.
   **`CustomerProfileViewModel.LoadAsync` (site 7) deferred** — outside 8.108's authorised file list; fold
   into sub-wave 6 or a short addendum.
   **(3) Organization + Specialists + Services (8 sites / 7 VMs) ✅ `b509054` (Phase 8.112, committed 8.114)** —
   both `SpecialistScheduleViewModel` `UnauthorizedOperationException` branches + `[CallerMemberName]` arg +
   `TryMutateAsync` success path kept.
   **(4) Automation tabs (13 / 13 sites / 5 tab VMs) ✅ `d10f9bc` (Phase 8.116 + 8.117.1 addendum, committed 8.119)** —
   all Phase 8.39 filtered shape; the `when` clause keeps `exception` bound, so only `ErrorMessage = exception.Message;`
   → `ErrorMessage = Localization.Strings.Common_ActionFailedMessage;` changed; filter / `State = Error` /
   `LogOperationFailed` / `await LoadAsync()` reloads byte-unchanged; +0 net tests.
   **(5) Booking + Calendar + Inventory (11 / 11 sites / 4 VMs) ✅ `71fb472` (Phase 8.121, committed 8.123)** —
   all plain `catch (Exception exception)` → `catch (Exception)`; `#pragma CA1031`, `State = Error`,
   operation-name-only log calls, the Booking stale-response guard + Inventory out-of-order guard byte-unchanged;
   2 live test-documented backend-body leaks closed (`CreateBookingAsync`, `InitializeAsync`); +0 net tests.
   **(6) / FINAL — Dashboard + Analytics + Salon + QR + Support + CustomerProfile (9 / 9 Category-A sites /
   6 VMs) ✅ `17306d9` (Phase 8.125, committed 8.127)** — `DashboardPageViewModel.LoadAsync`,
   `AnalyticsPageViewModel.LoadAsync`, `SalonPageViewModel` ×2, `QrCodesPageViewModel` ×2, `SupportPageViewModel`
   ×2 (filtered `when` shape), `CustomerProfileViewModel.LoadAsync`. 7 plain (var dropped) + 2 filtered Support
   (var kept); `#pragma CA1031`, `State = Error`, log calls, the 2 `finally` blocks, the QR `Salon is null`
   guard, Support form-clears byte-unchanged; 3 live test-documented backend leaks closed
   (`DashboardPageViewModel.LoadAsync`, `SalonPageViewModel.CreateSalonAsync`,
   `QrCodesPageViewModel.GenerateReceptionInviteAsync`); +0 net tests.
   **→ The P2 track is COMPLETE — all 58 Category-A `= exception.Message` UI surfaces sanitized.** The only
   `= exception.Message` left is the 2 `SettingsPageViewModel` `NotSupportedException`→`StatusMessage` branches
   (Category-D — a hard-coded local developer string from `LocalOnlyLanguagePackRepository`; NOT untrusted
   data; deliberately excluded, optional localization-polish follow-up only).
   `LoginViewModel` / `MobileOtpLoginViewModel` are already correct (typed catches → `Strings.*`).
   **Phase 8.99.1 Settings XAML visibility fix is DONE (`58a2c88`, Phase 8.129, committed 8.131).**
   Other backlog (unauthorized): `CancellationToken` propagation, Startup UX, `HttpApiClient` Infra decision,
   Wave G P3 (3 local-only infra VMs), the API-Environment "Restart Now" button mislabel (1-line). See §G.
7. **This file itself should be kept current** — if a future session completes any item in §F, update
   §B (new commit), §E (test count), §F (item resolved), and §G (new next action) rather than letting it
   drift.

---

## STOP

Checkpoint created at `801cc65`. Updated at Phases 8.8/8.13/8.17/8.21/8.25/8.29/8.33/8.37/8.41/8.45/8.49/8.53/8.58/8.63/8.68/8.72/8.76/8.80/8.84/8.88/8.92/8.93,
and **Phase 8.96** for `7c9c132` (Missing-Guard Sweep Wave F / Automation tabs — Phase 8.94 + 8.94.1,
committed at Phase 8.96; parent `4b1afca`; build 0/0, suite 2,701 / 2,701, Architecture 7/7): §A HEAD +
banner + audit-phase list (+8.95) + commit chain, §B commit table (+`7c9c132` row), §E build/test count
(2,691 → 2,701, +10; Presentation 748 → 758) + progression line, §F Phase 8.94/8.94.1 detail bullet, §G
Missing-Guard Sweep track progress (Wave F ✅ — Automation domain closed, coverage 19/19; Wave G now the
only remaining wave), §H items 1/2/5/6 + STOP line. No code changed in performing this checkpoint update.
**Phase 8.97 / 8.98** (Wave G / P2 infra scope audit + `SettingsPageViewModel` carve-out scope audit — audit
only, no code change; HEAD unchanged at `7c9c132`, tracked tree clean, suite 2,701 / 2,701): §A audit-phase
list (+8.97 / +8.98), §G Wave G audit outcome (P2 verdict, defer recommendation) + Phase 8.98 Settings
carve-out plan (6 methods, ~8 tests, LOW risk, implement at 8.99). No code changed.
**Phase 8.101** for `0260bc3` (Missing-Guard Sweep Settings-page P2 carve-out — Phase 8.99, committed at
Phase 8.101; parent `7c9c132`; build 0/0, suite 2,710 / 2,710, Architecture 7/7, Settings subset 26/26):
§A HEAD + banner + audit-phase list (+8.100) + commit chain, §B commit table (+`0260bc3` row), §E build/test
count (2,701 → 2,710, +9; Presentation 758 → 767) + progression line, §F Phase 8.99 detail bullet, §G
Missing-Guard Sweep track progress (Settings carve-out ✅ — sweep effectively complete; remaining is P3 /
Phase 8.99.1 XAML / "sanitize load-error surfacing" P2), §H items 1/2/5/6. No code changed in performing
this checkpoint update.
**Phase 8.102** ("sanitize load-error surfacing" P2 scope audit — audit only, no code change; HEAD unchanged
at `0260bc3`, tracked tree clean, suite 2,710 / 2,710): §A audit-phase list (+8.102), §G "sanitize
load-error surfacing" P2 entry expanded with the audit outcome (58 Category-A sites / 30 VMs, uniform
behaviour-neutral fix, ~6 priority-ordered domain sub-waves). No code changed.
**Phase 8.103** (P2 sanitize sub-wave 1 — Reporting + AI Center + Accounting/POS — scope audit; audit only,
HEAD unchanged at `0260bc3`, tracked tree clean, suite 2,710 / 2,710): §A audit-phase list (+8.103), §G
P2 entry (sub-wave 1 scoped — 11 sites / 5 VMs: `ReportingPageViewModel` ×3, `AiCenterPageViewModel` ×2,
`AccountingPageViewModel` ×2, `PosCheckoutViewModel` ×3, `InvoiceProfileViewModel` ×1; one commit,
LOW risk, ~2,713; `SendMessageAsync` confirmed a live customer-name leak; `PosCheckoutViewModel` +
`InvoiceProfileViewModel` each need one `using` line; implement at 8.104). No code changed.
**Phase 8.106** for `76d3f61` ("sanitize load-error surfacing" P2 sub-wave 1 — Reporting + AI Center +
Accounting/POS — Phase 8.104, committed at Phase 8.106; parent `0260bc3`; build 0/0, suite 2,713 / 2,713,
Architecture 7/7, sub-wave-1 subset 98/98): §A HEAD + banner + audit-phase list (+8.105) + commit chain,
§B commit table (+`76d3f61` row), §E build/test count (2,710 → 2,713, +3; Presentation 767 → 770) +
progression line, §F Phase 8.104 detail bullet, §G P2 track (sub-wave 1 ✅ — 11 sites; sub-waves 2–6 remain),
§H items 1/2/5/6/6(c). No code changed in performing this checkpoint update.
**Phase 8.107** (P2 sanitize sub-wave 2 — Customers + HR + Membership — scope audit; audit only, HEAD
unchanged at `76d3f61`, tracked tree clean, suite 2,713 / 2,713): §A audit-phase list (+8.107), §G/§H
sub-wave 2 scoped (7 sites / 5 VMs — `CustomerPageViewModel`, `CustomerProfileViewModel`, `HrPageViewModel`,
`EmployeeProfileViewModel`, `AcceptInviteViewModel`; no `using` / `.resx` change; `AcceptInviteViewModel`
carries a live test-documented invite-token leak; one commit, LOW risk, implement at 8.108). No code changed.
**Phase 8.110** for `1260d4e` ("sanitize load-error surfacing" P2 sub-wave 2 — Customers + HR + Membership —
Phase 8.108, committed at Phase 8.110; parent `76d3f61`; build 0/0, suite 2,714 / 2,714, Architecture 7/7,
sub-wave-2 subset 247/247): §A HEAD + banner + audit-phase list (+8.109) + commit chain, §B commit table
(+`1260d4e` row), §E build/test count (2,713 → 2,714, +1; Presentation 770 → 771) + progression line, §F
Phase 8.108 detail bullet, §G P2 track (sub-wave 2 ✅ — 6/7 sites; `CustomerProfileViewModel` + sub-waves 3–6
remain), §H items 1/2/5. No code changed in performing this checkpoint update.
**Phase 8.111** (P2 sanitize sub-wave 3 — Organization + Specialists + Services — scope audit; audit only,
HEAD unchanged at `1260d4e`, tracked tree clean, suite 2,714 / 2,714): §A audit-phase list (+8.111), §G/§H
sub-wave 3 scoped (8 sites / 7 VMs; keep the `SpecialistScheduleViewModel` `UnauthorizedOperationException`
branches + `[CallerMemberName]` arg; 1 prod + 3 test `using` additions; one commit, LOW risk, implement at
8.112). No code changed.
**Phase 8.114** for `b509054` ("sanitize load-error surfacing" P2 sub-wave 3 — Organization + Specialists +
Services — Phase 8.112, committed at Phase 8.114; parent `1260d4e`; build 0/0, suite 2,715 / 2,715,
Architecture 7/7, sub-wave-3 subset 148/148): §A HEAD + banner + audit-phase list (+8.113) + commit chain,
§B commit table (+`b509054` row), §E build/test count (2,714 → 2,715, +1; Presentation 771 → 772) +
progression line, §F Phase 8.112 detail bullet, §G P2 track (sub-wave 3 ✅ — 8 sites; sub-waves 4–6 +
`CustomerProfileViewModel` remain), §H items 1/2/5. No code changed in performing this checkpoint update.
**Phase 8.115** (P2 sanitize sub-wave 4 — Automation tabs — scope audit; audit only, HEAD unchanged at
`b509054`, tracked tree clean, suite 2,715 / 2,715): §A audit-phase list (+8.115), §G/§H sub-wave 4 scoped
(13 sites / 5 tab VMs — all Phase 8.39 filtered shape; minimal string-only swap keeping the `when` filter;
no `catch`-clause / `using`(prod) / `.resx` change; LOWEST risk; implement at 8.116). No code changed.
**Phases 8.117 + 8.118** (P2 sanitize sub-wave 4 commit scope reviews — review only, no code/commit; HEAD
unchanged at `b509054`; working tree = Phase 8.116 + 8.117.1 changes; build 0/0, suite 2,715 / 2,715,
Architecture 7/7, Automation subset 54/54): 8.117 flagged that Phase 8.116's file list covered only 10 of
the 13 audited sites → Phase 8.117.1 addendum authorised + implemented the last 3 (`ApprovalsTabViewModel`
×2, `AutomationDashboardTabViewModel` ×1); 8.118 confirmed 13/13, READY. §A audit-phase list (+8.117/8.118).
No code changed in these review phases.
**Phase 8.119** for `d10f9bc` ("sanitize load-error surfacing" P2 sub-wave 4 — Automation tabs — Phase 8.116
+ 8.117.1 addendum, committed at Phase 8.119; parent `b509054`; build 0/0, suite 2,715 / 2,715, Architecture
7/7, Automation subset 54/54): §A HEAD + banner + audit-phase list + commit chain, §B commit table
(+`d10f9bc` row), §E build/test count (2,715 → 2,715, +0; Presentation stays 772) + progression line, §F
Phase 8.116 + 8.117.1 detail bullet + §G P2 track (sub-wave 4 ✅ — 13/13 sites; sub-waves 5–6 +
`CustomerProfileViewModel` remain), §H items 1/2/5/6. No code changed in performing this checkpoint update.
**Phase 8.120** (P2 sanitize sub-wave 5 — Booking + Calendar + Inventory — scope audit; audit only, HEAD
unchanged at `d10f9bc`, tracked tree clean, suite 2,715 / 2,715): §A audit-phase list (+8.120), §F/§G
sub-wave 5 scoped (11 sites / 4 VMs — `BookingPageViewModel` ×5, `CalendarPageViewModel` ×3,
`InventoryPageViewModel` ×2, `InventoryProfileViewModel` ×1; all plain `catch (Exception exception)` — drop
the variable; 2 prod + 2 test `using` additions; 2 confirmed live backend-body leaks; LOW risk, one commit,
implement at 8.121). No code changed.
**Phase 8.122** (P2 sanitize sub-wave 5 commit scope review — review only, no code/commit; HEAD unchanged at
`d10f9bc`; working tree = Phase 8.121 changes; build 0/0, suite 2,715 / 2,715, Architecture 7/7, subset
130/130): confirmed 11/11 sites, scope clean, verdict READY. §A audit-phase list (+8.122). No code changed.
**Phase 8.124** (P2 sanitize sub-wave 6 / FINAL — Dashboard + Analytics + Salon + QR + Support + CustomerProfile
— scope audit; audit only, HEAD unchanged at `71fb472`, tracked tree clean, suite 2,715 / 2,715): §A
audit-phase list (+8.124), §F/§G sub-wave 6 scoped — **9 Category-A sites / 7 VMs** (`DashboardPageViewModel`
×1, `AnalyticsPageViewModel` ×1, `SalonPageViewModel` ×2, `QrCodesPageViewModel` ×2, `SupportPageViewModel`
×2 [filtered `when` shape], `CustomerProfileViewModel.LoadAsync` ×1); 7 plain `catch (Exception exception)`
(drop the variable) + 2 filtered (Support — keep it); `+ using …Localization;` in 3 prod (`Analytics`,
`Salons`, `QrCodes`) + 4 test; preserve 2 `finally` blocks + the QR `Salon is null` guard + Support
success-path form-clears; no `.resx` / DI / service / stub change; 3 confirmed live test-documented leaks
(Dashboard backend body, Salon "Validation failed", QR "Forbidden") + CustomerProfile PII. **The 2
`SettingsPageViewModel` `NotSupportedException`→`StatusMessage` branches are Category-D (local fixed developer
string — `LocalOnlyLanguagePackRepository` "…not available yet - Phase 19A ships the framework only") — NOT a
security leak; EXCLUDED (optional localization-polish follow-up only).** Sanitizing the 9 completes the P2
track (all 58 Category-A sites closed). LOW risk, one commit, implement at 8.125. No code changed.
**Phase 8.123** for `71fb472` ("sanitize load-error surfacing" P2 sub-wave 5 — Booking + Calendar + Inventory
— Phase 8.121, committed at Phase 8.123; parent `d10f9bc`; build 0/0, suite 2,715 / 2,715, Architecture 7/7,
subset 130/130): §A HEAD + banner + audit-phase list + commit chain, §B commit table (+`71fb472` row), §E
build/test count (2,715 → 2,715, +0; Presentation stays 772) + progression line, §F Phase 8.121 detail bullet
+ §G P2 track (sub-wave 5 ✅ — 11/11 sites; sub-wave 6 + `CustomerProfileViewModel` remain), §H items
1/2/5/6. No code changed in performing this checkpoint update.
**Phase 8.126** (P2 sanitize sub-wave 6 / FINAL commit scope review — review only, no code/commit; HEAD
unchanged at `71fb472`; working tree = Phase 8.125 changes; build 0/0, suite 2,715 / 2,715, Architecture 7/7,
subset 78/78): confirmed 9/9 Category-A sites, scope clean, verdict READY. §A audit-phase list (+8.126). No
code changed.
**Phase 8.127** for `17306d9` ("sanitize load-error surfacing" P2 sub-wave 6 / FINAL — Dashboard + Analytics
+ Salon + QR + Support + CustomerProfile — Phase 8.125, committed at Phase 8.127; parent `71fb472`; build
0/0, suite 2,715 / 2,715, Architecture 7/7, subset 78/78): §A HEAD + banner (P2 track now marked COMPLETE) +
audit-phase list + commit chain, §B commit table (+`17306d9` row), §E HEAD refs + build/test count (2,715 →
2,715, +0; Presentation stays 772) + progression line, §F Phase 8.125 detail bullet + §G P2 track (sub-wave 6
✅ — 9/9 sites; **P2 track COMPLETE, all 58 Category-A sites**; only the 2 Settings Category-D
`NotSupportedException` branches deliberately excluded), §H items 1/2/5/6. No code changed in performing this
checkpoint update.
**Phase 8.128** (post-P2 closure review — review only, no code/commit; HEAD unchanged at `17306d9`, tracked
tree clean, build 0/0, suite 2,715 / 2,715, Architecture 7/7): verified the full P2 sanitization matrix
(58/58 Category-A across 30 VMs, 6 sub-waves `76d3f61`→`17306d9`, +424/−214 over 62 files, +5 net tests,
6 live test-documented leaks closed); confirmed no `exception.Message` / `.ToString()` / `.StackTrace` /
`.InnerException` / raw-exception assignment on any ViewModel error surface, logs operation-name-only, 17
typed + 28 filtered catches preserved; only remaining `= exception.Message` is the 2 Settings Category-D
branches. Deliverable `ROJAN_PHASE8_128_POST_P2_CLOSURE_REVIEW_v1.md`. Recommended next: **Phase 8.129 =
Phase 8.99.1 `SettingsPage.xaml` visibility-trigger fix** (LOW risk, makes the `0260bc3` Settings-guard
failure text user-visible), optionally bundled with the Category-D localization polish. §A audit-phase list
(+8.128). No code changed.
**Phase 8.130** (Settings UX visibility fix commit scope review — review only, no code/commit; HEAD unchanged
at `17306d9`; working tree = Phase 8.129 change; build 0/0, suite 2,715 / 2,715, Settings subset 34/34,
Architecture 7/7): confirmed 1-file XAML change (`SettingsPage.xaml` only), scope clean, no behaviour
regression, verdict READY; flagged the pre-existing API-Environment "Restart Now" mislabel as a non-blocker.
§A audit-phase list (+8.130). No code changed.
**Phase 8.131** for `58a2c88` ("fix settings error message visibility" — Phase 8.129 / the Phase 8.99.1
follow-up, committed at Phase 8.131; parent `17306d9`; build 0/0, suite 2,715 / 2,715, Settings subset 34/34,
Architecture 7/7): §A HEAD + banner (Phase 8.99.1 marked DONE) + audit-phase list + commit chain, §B commit
table (+`58a2c88` row), §E HEAD refs + build/test count (2,715 → 2,715, +0) + progression line, §F/§G (Phase
8.99.1 item marked DONE; API-env "Restart Now" mislabel added to backlog), §H items 1/2/5/6. No code changed
in performing this checkpoint update.
**Phase 8.132** (Desktop final completion audit — review only, no code/commit; HEAD unchanged at `58a2c88`,
tracked tree clean, build 0/0, suite 2,715 / 2,715, Architecture 7/7): full-track verification — 30 clean
commits from `801cc65`; diagnostic-logging CLOSED, Missing-Guard Sweep COMPLETE, P2 error-surface
sanitization COMPLETE (58/58 Category-A), Settings-visibility follow-up DONE; 0 warnings / 0 errors, no
`TODO`/`FIXME`/`NotImplementedException` in source; 35 ViewModel `[LoggerMessage]` all operation-name-only,
0 ViewModel loggers pass the exception. **Finding (P3 informational):** `App.ShowErrorDialog`
(`Shell/App.xaml.cs:513`) shows `exception.Message` in the last-resort crash dialog — recommend a generic
message + "details in log file". **No P0. P1 = 3 upstream backend contracts (Inventory / HR / Accounting) +
POS payment-idempotency + a Release-build re-check — none are Desktop hardening work.** **Recommendation:
FREEZE the Desktop hardening track at `58a2c88`; run a Release-config build/test pass; open the
`feature/team3-desktop-completion` → `main` PR.** Deliverable `ROJAN_PHASE8_132_DESKTOP_FINAL_COMPLETION_AUDIT_v1.md`.
§A audit-phase list (+8.132). No code changed.
**Phase 8.133** (Desktop release-preparation audit — review only + `-c Release` build/test verification;
no publish, no tag, no commit; HEAD unchanged at `58a2c88`, tracked tree clean): **`-c Release` build
0 warnings / 0 errors (2m03s, deterministic); `-c Release` full suite 2,715 / 2,715 PASS, 0 skipped;
Architecture 7/7 — full Debug↔Release parity.** No dev-only config in Release (demo mode `#if DEBUG`-gated;
no `appsettings`; single `#if DEBUG` block solution-wide). Packaging infra (version 1.0.0 single-source,
real branding, Inno Setup per-user installer + signing hooks, self-contained `win-x64` single-file publish,
`release.yml`) complete + proven; on-disk artifacts stale (from an earlier commit). `git describe` =
`v1.0.0-45-g58a2c88`. **7 release blockers, all pre-existing / non-Desktop-hardening:** (1) unsigned
installer, (2) no live login test, (3) no clean-VM test, (4) pipeline never run, (5) Inventory/HR/Accounting
on `Fake*Repository` (Team 1), (6) POS payment-idempotency, (7) **product decision needed** — first-launch
`ApiEnvironmentService.SelectedEnvironment` defaults to `Development` (`http://localhost:8080`), reconcile
with "connected to the real backend" (flip default for Release / force the choice / accept+document; ~5 lines
+ a test). **Recommendation: freeze at `58a2c88`; make the first-launch-environment decision; then work the
signing → live-test → clean-VM → pipeline chain if a genuine launch is the goal.** Deliverable
`ROJAN_PHASE8_133_DESKTOP_RELEASE_PREPARATION_AUDIT_v1.md`. §A audit-phase list (+8.133). No code changed.
**Phase 8.134** (Desktop release handoff report — documentation only, no code/build/commit; HEAD unchanged at
`58a2c88`, tracked tree clean, **Team 3 Desktop track FROZEN**): documented the completed 6-item scope
(Missing-Guard Sweep COMPLETE · error-surface sanitization 58/58 Category-A · security hardening COMPLETE ·
Automation reliability COMPLETE · Settings UX fix COMPLETE · Release build verification COMPLETE), the
ownership matrix (Desktop UI/ViewModels → Team 3 done; Backend contracts → Team 1; payment idempotency →
Product + Backend; production API config → Product/DevOps; installer signing + pipeline + live test →
Release Engineering), the 7-blocker matrix (B1–B7, all external to Team 3 hardening; only B7 first-launch
environment default + possibly B6 touch Desktop code, both small), the 9-item release checklist (6 green:
Release build / Release tests / architecture / version metadata / installer readiness / signing hooks;
3 pending: fresh publish, backend contracts + live test, endpoint decision), and a concise Team 1 handoff
message. **Recommendation: merge `feature/team3-desktop-completion` → `main`; make the first-launch-environment
decision; hand B1–B6 to their owners. The Team 3 Desktop hardening engagement is complete.** Deliverable
`ROJAN_PHASE8_134_DESKTOP_RELEASE_HANDOFF_REPORT_v1.md`. §A audit-phase list (+8.134). No code changed.
**Phase 8.135** (Desktop merge readiness review — review only, no code/build/commit; HEAD unchanged at
`58a2c88`, tracked tree clean, **Team 3 Desktop track FROZEN**): **`main` = `b915e04` is a strict ancestor of
`58a2c88` → fast-forward merge possible, ZERO conflicts today.** `main..HEAD` = 48 commits (36 fix, 6 feat,
2 test, 1 release, 1 refactor, 1 ci, 1 pre-existing merge `b48740d`); the Team 3 track `801cc65..HEAD` = 30
strictly-linear `fix(desktop):` commits (0 merges), 96 files (41 src/Presentation incl. 1 XAML + 4
Localization files with 1 additive key; 1 src/Shell `NavigationService.cs`; 54 test files), all
`.cs`/`.xaml`/`.resx`, +6,847/−613. No stray artifacts, no skipped tests, no `.csproj`/build-config churn.
`main` is 4 commits BEHIND the `v1.0.0` tag (`d518218`) — pre-existing repo hygiene, not Team 3. Conflict
risk if `main` advances first: **LOW** (only NavigationService.cs in Shell + 1 additive localization key;
no shared-controls / build-config / project-file changes). Quality: build 0/0 + suite 2,715/2,715 (0 skipped)
in **both Debug and Release**, Architecture 7/7, 58/58 error surfaces sanitized. **Merge decision: ✅ READY TO
MERGE — no blockers; release gates all external.** Recommendation: open the `feature/team3-desktop-completion`
→ `main` PR (fast-forward preferred); route the 5 external next-actions (endpoint decision, POS idempotency,
backend contracts, signing, pipeline) to their owners. Deliverable
`ROJAN_PHASE8_135_DESKTOP_MERGE_READINESS_REVIEW_v1.md`. §A audit-phase list (+8.135). No code changed.
**Phase 8.136** (post-merge validation plan — planning doc only, no code/build/commit; HEAD unchanged at
`58a2c88`, tracked tree clean, **Team 3 Desktop track FROZEN**): **CORRECTION to 8.135 —** the real merge
target is **`origin/main` = `d518218`** (the `v1.0.0` tag commit), NOT the stale local `main` (`b915e04`, 4
commits behind, those 4 all already in `58a2c88`). `origin/main` **is a strict ancestor of `58a2c88`** → FF
still possible; `origin/main..58a2c88` = **45 commits** (30 Team 3 hardening + 15 pre-existing baseline incl.
1 merge `b48740d`). The branch `58a2c88` is **local-only — must `git push origin
feature/team3-desktop-completion` before a PR.** **ROLLBACK SHA: `d518218`.** Plan delivered: merge
prerequisites (fetch → push branch → confirm FF → green CI) + approvals; pre-merge SHA checkpoint; 4-part
post-merge validation matrix (git / Debug+Release build / Debug+Release suite / smoke — Customer login+search+
booking, Manager dashboard+services+calendar, Automation workflow, Settings theme+language+API-env, most ✅
covered by the suite, a few ▶ manual on a Release build); **tag decision = NO tag now, defer `v1.0.1` /
`v1.1.0` to a real validated build (Release Engineering / versioning-doc call)**; ref-move rollback preserving
the immutable branch + the untracked audit reports (recommend a dedicated `docs/` audit-trail commit,
`git add ROJAN_PHASE8_* ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` only — NOT `git add .`, 185 of 240
untracked `.md` belong to other teams); post-merge ownership map. **Recommendation: Phase 8.137 = MERGE
EXECUTION (requires explicit push/merge authorization — outside all prior STRICT MODE), then Phase 8.138 =
post-merge validation per §C.** Deliverable `ROJAN_PHASE8_136_POST_MERGE_VALIDATION_PLAN_v1.md`. §A
audit-phase list (+8.136). No code changed.
**Phase 8.137** (MERGE EXECUTION — **ABORTED at pre-merge verification; nothing pushed, nothing merged,
`main` untouched, HEAD unchanged at `58a2c88`, working tree clean**): authorized to fetch/push/merge for the
first time. `git fetch origin` (the only network op, read-only) revealed **`origin/main` MOVED** from the
plan's `d518218` → **`53ae2fb`** — 3 external commits by `meisamelh66` (2026-08-25/26): `5ac87dc feat(desktop):
complete service catalog management`, `92052c7 feat(desktop): implement specialist shift engine integration`,
`53ae2fb fix(desktop): harden specialist shift engine` — a **parallel Service-Catalog + Specialist-Shift-
Engine implementation** (the branch already carries its own from the pre-`801cc65` baseline; +7 `ROJAN_PHASE5_*`
reports confirm a separate concurrent line). **Fast-forward is now IMPOSSIBLE** (diverged at `d518218`); a
local dry-run merge (aborted, no commit) produced **~30 conflicts** — every conflicting ViewModel
(`SpecialistPageViewModel` ×4 Team-3 commits, `SpecialistProfileViewModel` ×3, `SpecialistScheduleViewModel`
×2 add/add, `ServicePageViewModel` ×3, `ServiceProfileViewModel` ×3) + `ServicePage.xaml` / `SpecialistPage.xaml`
+ ~10 test stubs is one the hardening track sanitized. Per TASK C ("fast-forward only · no merge commit · no
conflict resolution") the merge was **not performed**. SOURCE `58a2c88`, TARGET (actual) `53ae2fb`, MERGE-BASE
`d518218`. No rollback needed. **Recommendation: Phase 8.138 = Main-Divergence Reconciliation Plan** — assess
the two parallel Service/Shift-Engine implementations (cross-team question — `meisamelh66`'s line), pick an
integration strategy (rebase onto `origin/main` / merge-in / **cherry-pick just the 30 hardening commits
`801cc65..HEAD` onto `origin/main`** if its Service/Specialist code is a superset), re-verify, fresh
merge-readiness review, then a re-authorized merge. Deliverable
`ROJAN_PHASE8_137_MERGE_EXECUTION_REPORT_v1.md`. §A audit-phase list (+8.137). No code changed.
**Phase 8.138** (Desktop main-divergence reconciliation plan — analysis only, no execute/merge/rebase/
cherry-pick/commit; HEAD unchanged at `58a2c88`, tracked tree clean, `origin/main` untouched at `53ae2fb`):
full divergence inventory (commit graph, 38 overlapping files, 2 duplicate parallel features — Service
Catalog + Shift Engine, ~30 conflict zones, `SpecialistScheduleViewModel[.Tests]` add/add), ownership
analysis (30 hardening + 15 baseline commits = **branch only**; 3 Service/Schedule commits = **`origin/main`
only, on stale pre-`7103647` architecture with no downstream refs**), 3 strategy options evaluated
(1 rebase = VERY HIGH risk / reject; 2 cherry-pick hardening only = HIGH silent-loss / reject; 3a
`merge -s ours` = LOW risk, zero conflicts, tree unchanged / **RECOMMENDED**; 3b full 3-way merge = MEDIUM /
fallback). **Recommendation: Option 3a — `git merge -s ours origin/main` onto the branch**, after a scoped
review of the fork's 3 commits for orphan value (EF `ServiceEntityMapper` / `ServiceCategoryDto` / 3 test
suites), gated on owner confirming the branch's Service Catalog + `Application/Specialists/Schedule/` engine
is canonical. Proposed 8.139 (owner confirm + fork review) → 8.140 (execute 3a + Debug+Release re-verify) →
8.141 (re-authorized `→ main` fast-forward + validation) → 8.142 (audit-trail `docs/` commit). Deliverable
`ROJAN_PHASE8_138_DESKTOP_MAIN_DIVERGENCE_RECONCILIATION_PLAN_v1.md`. §A audit-phase list (+8.138). No code changed.
**Phase 8.139** (owner confirmation + scoped fork review — analysis only, no execute/merge/commit; HEAD
unchanged at `58a2c88`, tracked tree clean, `origin/main` untouched at `53ae2fb`): **KEY FINDING — one
developer (`Meisam Elhaee <meisamelh66@gmail.com>`) authored BOTH lines; the `origin/main` fork is
EARLIER Phase-5 work** superseded on the branch — its Service Catalog + Shift Engine were rebuilt AFTER
`7103647` (calendar-authority removal, 2026-08-27+) with the `Application/Specialists/Schedule/` architecture,
full test coverage, and 30 hardening commits. Both feature areas are **functionally equivalent** (same
backend `SpecialistScheduleController`, same endpoint shape, same design, same 8-mutation-method guarding —
the fork's `53ae2fb` report even says "No new capability"), differing only in type names + the fork's older
pre-refactor architecture. **Fork review: every fork-unique element = DROP** (feature UI superseded by the
branch's sanitized versions; fork's `Schedule/` layer = competing arch; fork's unique tests cover dropped
code incl. the retired local calendar authority — must NOT port). **Nothing to PORT.** Dependency review:
dropping the fork via `-s ours` breaks nothing — branch is a self-consistent, 2,715/2,715 (Debug+Release),
fully-DI-wired superset; no DTO / ViewModel-contract / test / navigation / Application-layer impact.
**FINALIZED STRATEGY: Option 3 — `git merge -s ours origin/main`** on `feature/team3-desktop-completion`
(tree stays == `58a2c88`; zero conflicts; re-enables a fast-forward to `main`; records the superseded
predecessor). Proposed 8.140 = execute `-s ours` + Debug+Release re-verify + merge-readiness review; 8.141 =
re-authorized `→ main` fast-forward + validation; 8.142 = audit-trail `docs/` commit. Deliverable
`ROJAN_PHASE8_139_OWNER_CONFIRMATION_FORK_REVIEW_v1.md`. §A audit-phase list (+8.139). No code changed.
**Phase 8.140** (`-s ours` merge execution — **local merge commit made; no source/test change; no push**):
pre-merge checks passed (tree clean, HEAD `58a2c88`, `origin/main` `53ae2fb`, merge-base `d518218`).
`git merge -s ours --no-ff origin/main` → **merge commit `77414de`** (parents `58a2c88` ^1 + `53ae2fb` ^2;
author Meisam Elhaee; message records the superseded Phase-5 fork + `ROJAN_PHASE8_138/139` provenance;
Co-Authored-By + Claude-Session trailers). **Tree verification: merge tree SHA `46bc0c9` == `58a2c88` tree
SHA — byte-identical; `git diff 58a2c88 HEAD` empty; `git diff HEAD^1 HEAD` empty.** Nothing from the fork
(`Application/Schedule/`, `BackendScheduleRepository`, fork's `SpecialistScheduleViewModel`,
`ServiceEntityMapper`, `ServiceCategoryDto`, fork-unique tests) entered the tree. Quality gates: Debug build
0/0, Release build 0/0 (1m38s deterministic), full suite **2,715/2,715 in Debug AND Release** (0 skipped),
Architecture **7/7** both configs — identical to the pre-merge baseline, no regression. **`origin/main`
(`53ae2fb`) is now a strict ancestor of `feature/team3-desktop-completion` (`77414de`) → `→ main` is a clean
fast-forward.** §A HEAD + banner + audit-phase list. Deliverable
`ROJAN_PHASE8_140_OURS_MERGE_EXECUTION_REPORT_v1.md`. Rollback: `git reset --hard 58a2c88` (local-only,
trivial). Next: Phase 8.141 = `git fetch` (re-confirm `origin/main` `53ae2fb`) → `git push origin
feature/team3-desktop-completion` → fast-forward `main` → `git push origin main` → post-merge validation
(Phase 8.136 §C); Phase 8.142 = audit-trail `docs/` commit.
**Phase 8.141** (`main` fast-forward + push + validation — **outward-facing: `origin` updated**): pre-push
checks passed (tree clean, HEAD `77414de`, `git fetch` → `origin/main` still `53ae2fb`, FF valid).
`git push origin feature/team3-desktop-completion` → `* [new branch]` (was local-only). `git push origin
77414de:main` → **`53ae2fb..77414de  77414de -> main`** — plain fast-forward (range `..` not `+`), no
`--force`, no new merge commit on `main`. (Local `main` could not be checked out here — it's held by the
primary worktree; the FF was done by pushing the commit directly to the remote `main` ref.) Verified:
`origin/main` == `origin/feature/team3-desktop-completion` == `77414de`; `git diff 58a2c88 origin/main`
empty; `v1.0.0` = `d518218` unchanged; working tree clean. Post-merge validation at `77414de`: Debug build
0/0, Release build 0/0, full suite **2,715/2,715 in Debug AND Release** (0 skipped), Architecture **7/7**
both configs — identical to baseline, no regression. **`main` now carries the entire Team 3 hardening line +
the `-s ours` merge superseding the `origin/main` Service-Catalog + Shift-Engine fork; no fork code merged.**
Rollback point: `53ae2fb` (FF-back via `git push origin 53ae2fb:main --force-with-lease` if caught early;
else revert). §A HEAD/banner + audit-phase list. Deliverable
`ROJAN_PHASE8_141_MAIN_FAST_FORWARD_VALIDATION_REPORT_v1.md`. Next: Phase 8.142 = audit-trail `docs/` commit.
**Phase 8.142** (audit-trail `docs/` commit — **documentation only, no source/test/project change**): the 144
`ROJAN_PHASE8_*.md` engagement reports (Phases 8.0–8.141, all "TEAM 3 — PHASE 8") + this checkpoint were
**moved** from the repo root into a new tracked `docs/team3/` subtree — `docs/team3/phases/` (the 144
reports) and `docs/team3/checkpoints/` (`ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`, i.e. this file) — plus
a `docs/team3/README.md` index. Explicit-path `git add docs/team3/` only; the 117 other `ROJAN_*.md` +
other-team `ROJAN_TEAM3_*` / `ROJAN_PHASE5_*` / `ROJAN_PHASE7_*` / `ROJAN_PHASE4_*` docs stay untracked at
root (out of scope). Verified: `git diff` shows **0** `.cs` / `.xaml` / `.csproj` / build-config changes —
only new `docs/team3/*.md` files. Commit `docs(team3): add desktop hardening audit trail`. `main` /
`origin/main` remain `77414de` for the code tree; the doc commit is a separate history entry on
`feature/team3-desktop-completion` (then fast-forwarded to `main` or landed via PR). Deliverable
`ROJAN_PHASE8_142_AUDIT_TRAIL_COMMIT_REPORT_v1.md`.
**NOTE:** this checkpoint file now lives at `docs/team3/checkpoints/ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`.
