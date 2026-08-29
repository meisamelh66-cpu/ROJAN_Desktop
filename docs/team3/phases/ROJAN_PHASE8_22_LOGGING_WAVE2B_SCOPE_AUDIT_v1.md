# ROJAN AI — TEAM 3 — PHASE 8.22 LOGGING WAVE 2B — SCOPE AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no behaviour change, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `75357e1` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_18_LOGGING_WAVE2_SCOPE_AUDIT_v1.md`

Every figure verified against source this turn.

---

## A. Current Coverage (Task 1)

### A.1 ViewModel population

**56 ViewModel classes** (55 Presentation + 1 Shell) — unchanged.

### A.2 Self-logging ViewModels — **13 of 56 (23.2%)**

| # | ViewModel | Landed |
|---|---|---|
| 1–4 | `BookingPageViewModel`, `PosCheckoutViewModel`, `SpecialistScheduleViewModel`, `SpecialistAvailabilityViewModel` | Phase 7.4 |
| 5–7 | `DashboardPageViewModel`, `CalendarPageViewModel`, `AccountingPageViewModel` | Phase 8.11 (`2453a7f`) |
| 8 | `MobileOtpLoginViewModel` | Phase 8.15 (`31f4b63`) |
| 9–13 | `CustomerPageViewModel`, `ServicePageViewModel`, `InventoryPageViewModel`, `HrPageViewModel`, `ReportingPageViewModel` | Phase 8.19 (`75357e1`) |

Verified by `grep -rl "NullLogger<" src/Rojan.Desktop.Presentation/ViewModels/` → exactly these 13
files.

### A.3 Remaining pool

**~18 ViewModel files** still contain an unlogged broad `catch (Exception)`. Wave 2B targets the 5 named
in the authorization; the rest are Wave 2C.

---

## B. Wave 2B Candidate Review (Task 2)

All 5 are `sealed class` (not `partial`), auto-load via `_ = LoadAsync()` in the constructor, and swallow
into `ErrorMessage`/`State` (or `CreateErrorMessage`/`GenerateInviteErrorMessage`/`StatusMessage`). All 5
are `AddTransient` in `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` (lines 58/71/72/
73 — Salon/Analytics/AiCenter/Organization; QrCodes line 60) → `ILogger<T>` is DI-injectable for free.

| # | ViewModel | Existing logger | Broad-catch boundaries (verified) | Diagnostic value | Test complexity | Architecture risk |
|---|---|---|---|---|---|---|
| 1 | `Organizations/OrganizationPageViewModel` | **None** | **1** — `LoadAsync` (`:411`). `CreateOrganizationAsync`/`CreateBranchAsync`/`SaveBranchSettingsAsync`/`SwitchRoleAsync` and the two branch-detail loaders have **no** `catch (Exception)` (out of scope) | Med — multi-branch/org admin page, opened less often than the daily-workflow pages but a load fault there is currently trace-less | **Med — no dedicated test file exists** (`OrganizationPageViewModelTests.cs` MISSING; only referenced by `NavigationServiceTests`). Needs a new test file + a throwing `IOrganizationQueryService` stub + a command stub + an `ICurrentSessionService` stub | Low (production); the ctor takes 4 deps incl. `IPermissionEngine` + `ICurrentSessionService` — neither touched by the change |
| 2 | `Analytics/AnalyticsPageViewModel` | **None** | **1** — `LoadAsync` (`:106`) | Med — analytics dashboard, KPI + chart load | **Low** — `AnalyticsPageViewModelTests.cs` exists; `CreateSut()` helper needs a throwing-stub overload (`StubKpiEngineQueryService`/`StubAnalyticsQueryService` currently take fixed data) | Low — 2-dep ctor |
| 3 | `AI/AiCenterPageViewModel` | **None** | **2** — `LoadAsync` (`:324`); **Chat Window** `SendMessageAsync` catch (`:393`) → `StatusMessage = exception.Message` | Med — AI Center + chat; the chat boundary is the one place where "don't pass the exception" matters most (see §C.2) | **Low-Med** — `AiCenterPageViewModelTests.cs` exists with a `CreateSut(...)` tuple helper + `StubAIService`; **13-parameter constructor** (adding an optional 14th) | Low — the 13 deps include `ITokenUsageTracker` (LLM billing tokens, **not** auth tokens — §C.2) |
| 4 | `Salons/SalonPageViewModel` | **None** | **2** — `LoadAsync` (`:159`); `CreateSalonAsync` (`:190`) → `CreateErrorMessage = exception.Message` | Med — salon setup/branding; infrequent after onboarding but a save failure with no trace is a real support-escalation case | **Low** — `SalonPageViewModelTests.cs` exists; `StubSalonQueryService(_ => Task.FromException(...))` already used | Low — 2-dep ctor |
| 5 | `QrCodes/QrCodesPageViewModel` | **None** | **2** — `LoadAsync` (`:156`); `GenerateInviteAsync` (`:180`) → `GenerateInviteErrorMessage = exception.Message` | Med — QR / staff-invite generation | **Low** — `QrCodesPageViewModelTests.cs` exists; `StubSalonQueryService` throw already used | Low — 3-dep ctor |

**Total production log call sites across all 5: 8** (Org 1, Analytics 1, AiCenter 2, Salon 2, QrCodes 2).

---

## C. Security Review (Task 3)

### C.1 Safe logging approach — identical to Wave 2A

**Operation name only. The `Exception` is NOT passed to the `[LoggerMessage]` method.**

```csharp
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Xxx page operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);   // NO Exception parameter

// call: LogOperationFailed(nameof(LoadAsync));
```

Produced line, always exactly:
```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.<Ns>.<Vm>: <Vm> page operation failed. Operation=<MethodName>
```

### C.2 Per-boundary sensitive-data assessment

| Boundary | What flows through the `try` | Risk if the exception were logged | Mitigation |
|---|---|---|---|
| all 5 `LoadAsync` | org/branch/settings, KPI/chart data, salon config, QR bytes, AI dashboards — **no customer PII, no credentials** | Low; org "settings" carry receipt header/footer text + VAT + hours (org config, not PII/secrets) | exception not passed → nothing logged but the method name |
| **`AiCenterPageViewModel` chat** (`:393`) | **`text` = the user's raw chat message** (a business question the user typed — could contain a customer name, a figure, free text) sent to `_aiService.SendMessageAsync` | **Medium** — a failing AI backend could echo the prompt in its error message | **exception not passed** — this is the single most important place the pattern's constraint applies; the log records only `Operation=SendMessageAsync` (or the method's `nameof`) |
| `SalonPageViewModel.CreateSalonAsync`, `QrCodesPageViewModel.GenerateInviteAsync` | salon name/branding, invite parameters | Low | exception not passed |

| Sensitive class | In any Wave 2B log line? |
|---|---|
| Customer data | **No** — not referenced by any log call; no customer-facing page is in this wave |
| Organization data (settings, receipt text, VAT) | **No** — exception not passed; templates carry only `nameof` |
| Tokens (auth) | **No** — no auth token is referenced near any of these catches. `AiCenterPageViewModel.ITokenUsageTracker` / `TokenUsage` DTOs are **LLM billing-unit counters**, not credentials, and are not referenced by any log call anyway |
| Backend responses | **No** — the only carrier is `Exception.Message`, which is never passed |

**Conclusion:** with the exception-not-passed pattern, Wave 2B has **no sensitive-data logging risk**.
The AiCenter chat boundary is the one place to call out explicitly in the scope review and implementation
report.

---

## D. Architecture Review (Task 4)

| Check | Result |
|---|---|
| `ILogger<T>` injection possible | **Yes** — all 5 are `AddTransient`; `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) registers the open-generic `ILogger<T>`; DI fills the new optional last ctor param automatically |
| No DI changes | **Confirmed** — the `AddTransient<XxxPageViewModel>()` lines are unchanged; adding an optional ctor param needs no registration edit |
| No interface changes | **Confirmed** — `IOrganizationQueryService`, `IKpiEngineQueryService`, `IAnalyticsQueryService`, `IAIService` (+ the other 12 AiCenter deps), `ISalonQueryService`, `ISalonInviteService`, `IStaticQrCodeGenerator` all untouched; the change is entirely inside the 5 concrete classes |
| No Domain impact | **Confirmed** — Presentation-layer only; no business rule, permission decision, backend call, or data-authority change. `Booking` / `Calendar` authority / `Shift Engine` / `RBAC` / `Authentication` / `Navigation` all untouched |
| Architecture tests unaffected | `DependencyDirectionTests` — `Microsoft.Extensions.Logging.Abstractions` is not forbidden (only Infrastructure/Domain/Shell/EF); already a Presentation `PackageReference`. `ViewModelTestabilityTests` — no `System.Windows.Threading`/`Controls` added. **7/7 expected unchanged** |
| `SYSLIB1020` (multi-logger) | **Not a risk** — each of the 5 would have exactly one `ILogger` field |

---

## E. Test Strategy (Task 5)

Using the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`, reused
cross-namespace via `using`).

### E.1 Wave 2B (4 files — see §F for the Organization split)

| File | Tests | Notes |
|---|---|---|
| `AnalyticsPageViewModelTests` | `LoadAsync_QueryThrows_LogsError`; `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | `CreateSut()` gains a throwing-stub overload or optional param |
| `AiCenterPageViewModelTests` | `LoadAsync_QueryThrows_LogsError`; `SendMessage_Throws_LogsErrorWithoutLeakingChatText` (asserts the chat `text` is **absent** from the log line); `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | `CreateSut(...)` tuple helper gains an optional `RecordingLogger<AiCenterPageViewModel>?`; needs `StubAIService.SendMessageAsync` to support throwing |
| `SalonPageViewModelTests` | `LoadAsync_QueryThrows_LogsError`; `CreateSalonAsync_Throws_LogsError`; `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | direct `new`; `StubSalonQueryService`/`StubSalonCommandService` throw support already present |
| `QrCodesPageViewModelTests` | `LoadAsync_QueryThrows_LogsError`; `GenerateInviteAsync_Throws_LogsError`; `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` | direct `new`; `StubSalonQueryService` throw already used; `StubSalonInviteService` throw support to confirm |

**Estimated Wave 2B tests: ~11** (2 + 3 + 3 + 3). Every ViewModel gets **failure-logs-Error +
NullLogger-safety**; the multi-boundary ones get a test per boundary where the stub supports it.
Expected suite after: **2,538 + ~11 ≈ 2,549**.

### E.2 Wave 2B-2 (Organization)

| File | Tests |
|---|---|
| **NEW** `tests/…/Organizations/OrganizationPageViewModelTests.cs` | `LoadAsync_QueryThrows_LogsError`; `NoLoggerSupplied_UsesNullLogger_LoadFailureNeverThrows` |
| **NEW** stubs | `StubOrganizationQueryService` (throwing `IOrganizationQueryService`), `StubOrganizationCommandService` (or reuse/relocate an existing one), `FakeCurrentSessionService` (mirroring the `DashboardPageViewModelTests` nested one) |

~2 tests + ~2–3 small stub classes.

### E.3 Regression

Every affected test file's helper gains only a trailing optional `= null` parameter → all existing call
sites compile and pass unchanged (`NullLogger` default). Full validation: build (0/0) + full suite +
architecture tests (7/7).

---

## F. Commit Strategy (Task 6)

### F.1 Options

| | Single Wave 2B commit (all 5) | Split: 2B (4) + 2B-2 (Organization) |
|---|---|---|
| Test infra | one commit carries a **new test file + 2–3 new stub classes** for Organization, mixed with 4 trivial changes | 2B is a clean mechanical batch; 2B-2 isolates the test-infra addition |
| Review surface | reviewer must vet new stubs alongside the pattern application | 2B reviews as "same pattern ×4"; 2B-2 reviews as "1 VM + its new test scaffolding" |
| Precedent | Wave 2A was 5 files that **all had test files** — this batch does not | matches the engagement's habit of isolating anything with extra surface (e.g. `MobileOtp` split out for its auth-adjacency) |
| AiCenter chat boundary | present in both | present in 2B — flagged in the 2B scope review |

### F.2 Recommendation

**Split: Wave 2B = 4 files (Analytics, AiCenter, Salon, QrCodes) in one commit; Wave 2B-2 = Organization
in a separate commit.**

Reasoning:
- The 4 in Wave 2B are an identical, low-risk mechanical change with existing test infrastructure — a
  clean single-concern commit, exactly the Wave 2A shape.
- Organization uniquely requires a **new test file plus new stub classes**; bundling that into Wave 2B
  would mix genuinely new test scaffolding into an otherwise-trivial batch and enlarge the review
  surface. Isolating it (2B-2) keeps each commit's diff proportionate to its risk — the same reasoning
  that split `MobileOtpLoginViewModel` out in Wave 1.
- Cost is one extra commit cycle; the engagement's rhythm already assumes per-concern commits.

Proposed messages:
```
fix(desktop): add ViewModel diagnostic logging (wave 2b)
```
```
fix(desktop): add ViewModel diagnostic logging (organization page)
```

### F.3 Sequencing

1. **Phase 8.23 — Wave 2B scope review** (readiness only): exact per-file catch sites, confirm
   `StubAIService`/`StubSalonInviteService` throw support, finalize test list.
2. Implementation authorization → implement + validate → commit scope review → commit execution
   (explicit-path staging, single commit).
3. **Phase 8.2x — Wave 2B-2** (Organization): audit the new-stub scope → scope review → implement →
   commit, separately.
4. **Then Wave 2C** (2C-1 Support/AcceptInvite, 2C-2 Automation tabs, 2C-3 detail/profile VMs +
   BookingWizard).

### F.4 Out of scope for Wave 2B

- Organization (→ 2B-2).
- Any boundary that is not a `catch (Exception)` (e.g. Organization's uncaught write methods, its
  branch-detail loaders).
- Wave 2C.
- Shared-stub throw hooks for the 3 untested Wave 2A sites (separate follow-up).

---

## STOP

Audit complete. No implementation performed.

**Recommendation: Wave 2B = `AnalyticsPageViewModel`, `AiCenterPageViewModel`, `SalonPageViewModel`,
`QrCodesPageViewModel`** (4 production + 4 test files, ~7 log sites, `Error` level, exception never
passed, one isolated commit). `OrganizationPageViewModel` → **Wave 2B-2** (separate commit — needs a new
test file + stub classes).
