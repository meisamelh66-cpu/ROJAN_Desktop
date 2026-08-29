# ROJAN AI — TEAM 3 — PHASE 8.124 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 6 (FINAL) — SCOPE AUDIT v1

**Type:** Scope audit. **AUDIT ONLY — no source/test/localization/service/DI change, no commit.**
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `71fb472` (unchanged)
**Reference:** `ROJAN_PHASE8_123_P2_SUBWAVE5_COMMIT_REPORT_v1.md`, `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md`

**Bottom line:** **11 remaining `= exception.Message` sites** across 8 VMs. **9 are Category-A** (recommend sanitizing): Dashboard ×1, Analytics ×1, Salon ×2, QR ×2, Support ×2, `CustomerProfileViewModel.LoadAsync` ×1. **2 are Category-D** (`SettingsPageViewModel` `DownloadOrInstallAsync` / `RemovePackAsync` — `catch (NotSupportedException)` surfacing a **hard-coded local developer string**, not untrusted data) — **recommend EXCLUDE** from this security sub-wave. Sanitizing the 9 completes the P2 track (58 → 58, all Category-A closed). **Three confirmed live test-documented leaks** (Dashboard `LoadAsync` backend body, Salon `CreateSalonAsync` "Validation failed", QR `GenerateReceptionInviteAsync` "Forbidden"), plus `CustomerProfileViewModel` carries customer PII. LOW risk. One commit.

---

## A. GIT STATE

| | |
|---|---|
| HEAD | `71fb472d6306ec609a5a6ba5b46c775a19f7e40e` (`fix(desktop): sanitize booking calendar inventory error surfacing` — Phase 8.121, committed 8.123) |
| Branch | `feature/team3-desktop-completion` |
| Tracked working tree | clean (0 modified/deleted) |
| Staged | none |
| Untracked | `.md` reports only |

Sub-waves 1–5 landed (`76d3f61`, `1260d4e`, `b509054`, `d10f9bc`, `71fb472`). This audit covers **sub-wave 6 — the final one**.

---

## B. INVENTORY — 11 sites / 8 ViewModels

`grep -rn "= exception.Message" src/Rojan.Desktop.Presentation/ViewModels/` → 11 hits (all remaining app-wide).

### B.1 Category-A — recommend sanitizing (9 sites / 7 VMs)

| # | VM · method | Line | Surface | Catch shape | `State = Error` | Log call | Extras to preserve |
|---|---|---|---|---|---|---|---|
| 1 | `Dashboard/DashboardPageViewModel.LoadAsync` | 291 | `ErrorMessage` | plain `catch (Exception exception)` | ✅ | `LogLoadFailed(nameof(LoadAsync))` | the `canViewFinancials` KPI-filter logic (in `try`) |
| 2 | `Analytics/AnalyticsPageViewModel.LoadAsync` | 113 | `ErrorMessage` | plain `catch (Exception exception)` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | `Task.WhenAll` fan-out (in `try`) |
| 3 | `Salons/SalonPageViewModel.LoadAsync` | 166 | `ErrorMessage` | plain `catch (Exception exception)` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 4 | `Salons/SalonPageViewModel.CreateSalonAsync` | 203 | `CreateErrorMessage` | plain `catch (Exception exception)` | n/a | `LogOperationFailed(nameof(CreateSalonAsync))` | **`finally { IsCreating = false; }`**; `HasCreateError` notify; form-field retention |
| 5 | `QrCodes/QrCodesPageViewModel.LoadAsync` | 163 | `ErrorMessage` | plain `catch (Exception exception)` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | — |
| 6 | `QrCodes/QrCodesPageViewModel.GenerateReceptionInviteAsync` | 193 | `GenerateInviteErrorMessage` | plain `catch (Exception exception)` | n/a | `LogOperationFailed(nameof(GenerateReceptionInviteAsync))` | **`finally { IsGeneratingReceptionInvite = false; }`**; `HasGenerateInviteError` notify; `if (Salon is null) return;` guard |
| 7 | `Support/SupportPageViewModel.SubmitMessageAsync` | 254 | `MessageError` | **filtered** `catch (Exception exception) when (exception is not OperationCanceledException)` | n/a | `LogOperationFailed(nameof(SubmitMessageAsync))` | `when` predicate references `exception` → **keep the variable**; `MessageStatus = null` reset |
| 8 | `Support/SupportPageViewModel.SubmitApplicationAsync` | 289 | `ApplicationError` | **filtered** `catch (Exception exception) when (exception is not OperationCanceledException)` | n/a | `LogOperationFailed(nameof(SubmitApplicationAsync))` | **keep the variable**; the 11-field form-clear-on-success block |
| 9 | `Customers/CustomerProfileViewModel.LoadAsync` | 274 | `ErrorMessage` | plain `catch (Exception exception)` | ✅ | `LogOperationFailed(nameof(LoadAsync))` | carried over from sub-wave 2 (was outside Phase 8.108's file list) |

**7 plain / 2 filtered.** Plain (1–6, 9) → **drop the `exception` variable**, `= exception.Message` → `= Strings.Common_ActionFailedMessage` (sub-wave 2/3/5 pattern). Filtered (7–8) → **keep the variable** (unused in body, no warning — `when` reads it), swap only the assignment (sub-wave 4 pattern); Support uses the fully-qualified `Localization.Strings.` form (matching its existing `Localization.Strings.Support_Message_Sent` / `_Application_Sent`).

### B.2 Category-D — recommend EXCLUDE (2 sites / 1 VM)

| # | VM · method | Line | Surface | Catch shape |
|---|---|---|---|---|
| D1 | `Settings/SettingsPageViewModel.DownloadOrInstallAsync` | 300 | `StatusMessage` | `catch (NotSupportedException exception) { StatusMessage = exception.Message; }` |
| D2 | `Settings/SettingsPageViewModel.RemovePackAsync` | 322 | `StatusMessage` | `catch (NotSupportedException exception) { StatusMessage = exception.Message; }` |

The `NotSupportedException` is thrown **by local code with a fixed, developer-authored English string** — `Shell/Localization/LocalOnlyLanguagePackRepository.cs:19,22`:
> `"Online language pack downloads are not available yet - Phase 19A ships the framework only."`
> `"Language pack removal is not available yet - Phase 19A ships the framework only."`

It is **not** a backend body, PII, URL, or any untrusted value — it is the intended Phase-19A "not available yet" notice. The broad `catch (Exception exception) when (exception is not OperationCanceledException)` **below** each (lines 302–306, 324–328) already assigns `Strings.Common_ActionFailedMessage`. There is **no security exposure** here. The only nit is that the message is un-localized English in an otherwise-localized UI — a **localization-polish** item, not a security fix, and one that needs a new/reused `.resx` string (out of scope for STRICT MODE and for the P2 security track). **Excluded; logged as an optional follow-up (see §F).**

### Not targets (verified)

- `CustomerProfileViewModel` `AddNoteAsync` / save mutations (lines 294/314/338/372) — already `catch (Exception) { SaveErrorMessage = Strings.Common_ActionFailedMessage; }` (Missing-Guard Wave B).
- `SettingsPageViewModel` all 6 broad `when`-filtered command catches — already `Strings.Common_ActionFailedMessage` (Phase 8.99 `0260bc3`).
- `Dashboard` news-ticker / quick-action "coming soon" paths — static local strings in code-behind, no `= exception.Message`.
- `SalonPageViewModel` / `QrCodesPageViewModel` — no other `= exception.Message`.

---

## C. CLASSIFICATION

| Aspect | Finding |
|---|---|
| Category-A sites | **9** — all `= exception.Message` to a bound `TextBlock` from a top-level broad catch |
| Catch shapes | 7 plain `catch (Exception exception)` (drop the variable) + 2 filtered `when (exception is not OperationCanceledException)` (keep the variable — Support) |
| Category-D sites | **2** — `NotSupportedException` → local fixed string; **exclude** |
| `finally` blocks to preserve | `SalonPageViewModel.CreateSalonAsync` (`IsCreating = false`), `QrCodesPageViewModel.GenerateReceptionInviteAsync` (`IsGeneratingReceptionInvite = false`) |
| `State = Error` sites | 5 of 9 (Dashboard, Analytics, Salon `LoadAsync`, QR `LoadAsync`, CustomerProfile) — the 4 command/submit sites have no `State` (their success paths reload or set a status) |
| New surfaces / flags | none — reuse the existing `ErrorMessage` / `CreateErrorMessage` / `GenerateInviteErrorMessage` / `MessageError` / `ApplicationError` property in each VM |
| Fix | plain: `catch (Exception exception)` → `catch (Exception)` + `= exception.Message` → `= Strings.Common_ActionFailedMessage`. filtered: `= exception.Message` → `= Localization.Strings.Common_ActionFailedMessage`, catch clause byte-unchanged. |

---

## D. SECURITY

Every Category-A surface is a bound error `TextBlock`. The raw `exception.Message` currently reaches the user.

| Domain | VM · method | What a raw message can expose |
|---|---|---|
| **Dashboard** — revenue / KPIs / business metrics | `DashboardPageViewModel.LoadAsync` | The KPI overview + recent-activity feed — **revenue figures, financial KPIs** (the very data gated behind `AccountingView` in the success path), staff names, activity detail; a 500 echoing the query. **`DashboardPageViewModelTests:85` asserts `Assert.Equal(backendBody, sut.ErrorMessage)` today — live leak.** |
| **Analytics** — reports / customer insights | `AnalyticsPageViewModel.LoadAsync` | KPI values, analytics summary (revenue trends, retention, spend), chart series — a 500 body over `GetKpisAsync` / `GetAnalyticsSummaryAsync` / `GetDashboardChartsAsync` |
| **Salon** — configuration / business data | `SalonPageViewModel.LoadAsync` | Salon name / phone / email / address (owner contact PII), org/branch ids |
| | `SalonPageViewModel.CreateSalonAsync` | **Backend validation bodies** — `CreateSalonCommand` echoes (name/phone/email/address), uniqueness-conflict detail. **`SalonPageViewModelTests:226` asserts `Assert.Equal("Validation failed", sut.CreateErrorMessage)` today — live leak.** |
| **QR** — links / customer access data | `QrCodesPageViewModel.LoadAsync` | Salon record, the customer-facing salon QR payload / download URL |
| | `QrCodesPageViewModel.GenerateReceptionInviteAsync` | **Invite tokens / invite ids** in `CreateReceptionInviteAsync` / `GetInviteQrCodeAsync` failures; **authz bodies**. **`QrCodesPageViewModelTests:90,138` assert `Assert.Equal("Forbidden", sut.GenerateInviteErrorMessage)` today — live leak.** |
| **Support** — tickets / customer info | `SupportPageViewModel.SubmitMessageAsync` | Sender name / email, message subject / body echoed in a validation 400 |
| | `SupportPageViewModel.SubmitApplicationAsync` | **Applicant PII** — first/last name, mobile, email, city, GitHub / LinkedIn / portfolio / resume URLs echoed in a validation body |
| **CustomerProfile** — PII / notes / history | `CustomerProfileViewModel.LoadAsync` | **Customer PII** (name / email / phone), notes, tags, full appointment history, loyalty / engagement insights. `CustomerProfileViewModelTests` already seeds `PiiSecret = "Amelia Hart / amelia.hart@example.com / 555-0100"` for the log no-leak test — the **surface** is currently unguarded. |

**Logs — already clean.** All 9 sites call `Log…(nameof(<Method>))`; the exception object is never passed. Each `[LoggerMessage]` template is `Operation={Operation}` only. Existing log no-leak assertions (`DashboardPageViewModelTests:87`, `CustomerProfileViewModelTests:27`, the Support/Salon/QR/Analytics operation-name log tests) are retained.

**Three confirmed live test-documented leaks closed by this sub-wave:** Dashboard `LoadAsync`, Salon `CreateSalonAsync`, QR `GenerateReceptionInviteAsync` (same class as the sub-wave 2 `AcceptInviteViewModel` and sub-wave 5 `CreateBookingAsync` / `InitializeAsync` leaks).

---

## E. ARCHITECTURE

| Concern | Finding |
|---|---|
| **`[LoggerMessage]` availability** | All 7 Category-A VMs are `sealed partial` with an instance-form, operation-name-only `[LoggerMessage]` (`LogLoadFailed` on Dashboard; `LogOperationFailed` on the rest). Each has exactly one `ILogger<T>` field → no `SYSLIB1020` risk. **No logger change.** |
| **Localization usage** | `Strings.Common_ActionFailedMessage` ships fa/en/ar since Wave A `794648e`. **Already `using …Localization;`:** `DashboardPageViewModel`, `CustomerProfileViewModel`, `SettingsPageViewModel`. **`SupportPageViewModel`** uses the fully-qualified `Localization.Strings.` form (no `using`, no change). **Need `+ using Rojan.Desktop.Presentation.Localization;`:** `AnalyticsPageViewModel`, `SalonPageViewModel`, `QrCodesPageViewModel` (3 prod files) — the sub-wave 1 `PosCheckoutViewModel` precedent. **No `.resx` change.** |
| **Test impact** | ~10 existing assertions to update from the raw message to `Strings.Common_ActionFailedMessage`: `DashboardPageViewModelTests` L70 + **L85**; `AnalyticsPageViewModelTests` L79; `SalonPageViewModelTests` L60, L75, L96, **L226**; `QrCodesPageViewModelTests` L58, **L90**, **L138**; `CustomerProfileViewModelTests` L38, L93. `SupportPageViewModelTests` L72/L94/L118/L137/L184 currently `Assert.NotNull(...)` — still pass; recommend strengthening to `Assert.Equal(Localization.Strings.Common_ActionFailedMessage, …)`. `+ using …Localization;` in the Dashboard / Analytics / Salon / QR test files (CustomerProfile test already has it; Support test can use the FQ form). Recommend `DoesNotContain` sentinel additions at the 3 confirmed-leak sites + the CustomerProfile PII site. **Expect +0 to +2 net tests.** |
| **Stub impact** | **None.** Every failure path is already exercised by an existing test via an existing stub seam (`Task.FromException`, stub delegates). No new stub, no new seam. |
| **DI impact** | **None.** No constructor signature change, no registration change. |
| **Risk** | **LOW** — uniform shapes (7 plain + 2 filtered, both already established patterns), 2 `finally` blocks and 1 stale-guard to preserve verbatim, no typed-catch entanglement in the Category-A set. |

---

## F. RECOMMENDATION

**Proceed to a single implementation phase (8.125)** covering the **9 Category-A sites / 7 VMs** in one commit. **Exclude** the 2 Settings `NotSupportedException` branches.

1. **Prod (7 files):**
   - `DashboardPageViewModel.cs` — 1 catch: drop variable, `ErrorMessage = Strings.Common_ActionFailedMessage;`. (`using` already present.)
   - `AnalyticsPageViewModel.cs` — `+ using …Localization;`; 1 catch, same swap.
   - `SalonPageViewModel.cs` — `+ using …Localization;`; 2 catches, same swap; keep `finally { IsCreating = false; }` + `HasCreateError` notify + form retention.
   - `QrCodesPageViewModel.cs` — `+ using …Localization;`; 2 catches, same swap; keep `finally { IsGeneratingReceptionInvite = false; }` + `HasGenerateInviteError` notify + `if (Salon is null) return;`.
   - `SupportPageViewModel.cs` — **no `using`**; 2 **filtered** catches, keep the `when` clause + `exception` variable byte-unchanged, swap only `= exception.Message` → `= Localization.Strings.Common_ActionFailedMessage`.
   - `CustomerProfileViewModel.cs` — **no `using`** (already imports); 1 catch, drop variable, same swap; keep `State = Error` + `LogOperationFailed`.
2. **Tests (6 files):** update the ~10 raw-message assertions to the generic constant; `+ using …Localization;` in `DashboardPageViewModelTests` / `AnalyticsPageViewModelTests` / `SalonPageViewModelTests` / `QrCodesPageViewModelTests`; strengthen the 5 Support `NotNull` assertions to `Assert.Equal(Localization.Strings.Common_ActionFailedMessage, …)`; add `DoesNotContain` sentinels at Dashboard `LoadAsync`, Salon `CreateSalonAsync`, QR `GenerateReceptionInviteAsync`, and CustomerProfile `LoadAsync` (PII). No stub / DI / `.resx` change.
3. **Validation:** `dotnet build` 0/0; full suite (expect `~2,715`, ±2); Architecture 7/7; the 6 affected subsets green.
4. **Commit subject:** `fix(desktop): sanitize dashboard analytics salon qr support and profile error surfacing` (or a shorter team-preferred form).
5. **STOP** after implementation → Phase 8.126 commit scope review → Phase 8.127 commit execution.

**After sub-wave 6 the "sanitize load-error surfacing" P2 track is COMPLETE** — all 58 Category-A `= exception.Message` UI surfaces across the app are sanitized. Remaining P2/P3 items are then only: the 2 Settings `NotSupportedException` Category-D branches (optional localization polish — could reuse/add a `Strings.Settings_*_ComingSoon`), the 3 local-only infra VMs (`WorkspaceHostViewModel` / `NotificationCenterViewModel` / `CommandPaletteViewModel`, **P3**), and the Phase 8.99.1 `SettingsPage.xaml` visibility-trigger tweak.

---

## STOP

Phase 8.124 scope audit complete. **AUDIT ONLY — nothing modified.** HEAD `71fb472`, tracked tree clean.

**Sub-wave 6 (final) = 9 Category-A sites / 7 VMs:** `DashboardPageViewModel.LoadAsync`, `AnalyticsPageViewModel.LoadAsync`, `SalonPageViewModel` (`LoadAsync` / `CreateSalonAsync`), `QrCodesPageViewModel` (`LoadAsync` / `GenerateReceptionInviteAsync`), `SupportPageViewModel` (`SubmitMessageAsync` / `SubmitApplicationAsync`), `CustomerProfileViewModel.LoadAsync`. 7 plain `catch (Exception exception)` (drop the variable) + 2 filtered `when (… not OperationCanceledException)` (Support — keep the variable). Preserve: `#pragma CA1031`, `State = Error` (5 sites), the operation-name-only log calls, the 2 `finally` blocks (Salon `CreateSalonAsync`, QR `GenerateReceptionInviteAsync`), the QR `if (Salon is null)` guard, and the Support success-path form-clear / status-reset. `+ using …Localization;` in 3 prod (`Analytics`, `Salons`, `QrCodes`) + 4 test files; Support uses the FQ form; Dashboard / CustomerProfile / Settings already import. **No `.resx` / DI / service / contract / stub change.** **3 confirmed live test-documented leaks** (Dashboard backend body, Salon "Validation failed", QR "Forbidden") + `CustomerProfileViewModel` PII surface.

**The 2 `SettingsPageViewModel` `NotSupportedException` branches are Category-D (local fixed developer string, no untrusted data) — recommend EXCLUDE; optional localization-polish follow-up.**

**Sanitizing the 9 completes the P2 track (all 58 Category-A sites closed).**

**Awaiting Phase 8.125 — Sub-Wave 6 Implementation Authorization.**
