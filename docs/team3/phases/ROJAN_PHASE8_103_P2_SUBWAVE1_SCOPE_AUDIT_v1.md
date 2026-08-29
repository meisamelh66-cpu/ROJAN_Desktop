# ROJAN AI — TEAM 3 — PHASE 8.103 — P2 ERROR-SURFACE SANITIZATION — SUB-WAVE 1 (REPORTING + AI CENTER + ACCOUNTING/POS) — SCOPE AUDIT v1

**Type:** AUDIT ONLY. No source / test / localization / commit / push / merge / rebase / amend.
**Branch:** `feature/team3-desktop-completion` · **HEAD:** `0260bc3`
**Reference:** `ROJAN_PHASE8_102_SANITIZE_ERROR_SURFACE_SCOPE_AUDIT_v1.md` §F sub-wave 1
**Recommendation: ship all 11 sites (5 VMs) in ONE commit. LOW risk — uniform UI-string swap; no behaviour, logging, localization, DI, service or contract change.**

---

## A. GIT STATE

```
git rev-parse HEAD        → 0260bc38aabdb51af32e40bc90d22d00504e5211
git branch --show-current → feature/team3-desktop-completion
git status (tracked)      → clean
git diff --cached         → (empty)
```

Untracked: only `ROJAN_*.md`. Baseline (checkpoint §E, `0260bc3`): **2,710 / 2,710** — Domain 456, Presentation 767, Application 791, Infrastructure 609, Shell 80, Architecture 7. Build 0/0.

---

## B. TARGET INVENTORY — 11 sites / 5 ViewModels

Every site is a top-level broad catch: `catch (Exception exception) { <Surface> = exception.Message; [State = DashboardState.Error;] Log…(nameof(<Method>)); }`. The log call is already **operation-name-only** everywhere (never receives the exception). Only the `<Surface> = exception.Message` UI assignment is the target.

### B.1 `ReportingPageViewModel` — 3 sites

| Line | Method | Surface | `State = Error`? | Extra |
|---|---|---|---|---|
| 237 | `LoadAsync` | `ErrorMessage` | ✅ yes | catalog + snapshots load |
| 311 | `RunReportAsync` | `StatusMessage` | ❌ (in `finally { IsRunning = false; }`) | **preceded by** `catch (OperationCanceledException) { StatusMessage = Localization.Strings.Reporting_RunCancelled; }` — keep that branch |
| 342 | `RerunSnapshotAsync` | `StatusMessage` | ❌ (`finally { IsRunning = false; }`) | — |

- Logger: instance-form `LogOperationFailed(string)` (EventId 1, Error, `"Reporting page operation failed. Operation={Operation}"`), comment already says *"never … the exception, its message, report data/filters, or any backend response detail"*.
- `Strings` reference style in this file: **fully-qualified `Localization.Strings.X`** (no `using`).

### B.2 `AiCenterPageViewModel` — 2 sites

| Line | Method | Surface | `State = Error`? |
|---|---|---|---|
| 354 | `LoadAsync` | `ErrorMessage` | ✅ yes |
| 429 | `SendMessageAsync` | `StatusMessage` | ❌ (`finally { IsSending = false; }`) |

- Logger: instance-form `LogOperationFailed(string)` (`"AI Center page operation failed. Operation={Operation}"`), comment: *"never … its message, the user's chat text, AI responses, or any backend/token detail"*.
- `using Rojan.Desktop.Presentation.Localization;` present; file already uses `Strings.Common_ActionFailedMessage` (Wave E guards).
- **Note — a live leak the current tests miss:** `SendMessageAsync`'s `StatusMessage = exception.Message` is bound in the Chat Window. The Wave-2B test `SendMessageCommand_ServiceThrows_LogsErrorWithoutLeakingChatText` seeds `new InvalidOperationException("upstream failed for customer Sarah Johnson")` and asserts the **log** omits "Sarah Johnson" — but never checks `sut.StatusMessage`, which currently **shows** "upstream failed for customer Sarah Johnson" to the user.

### B.3 `AccountingPageViewModel` — 2 sites

| Line | Method | Surface | `State = Error`? |
|---|---|---|---|
| 176 | `LoadAsync` | `ErrorMessage` | ✅ yes | invoice list + `RevenueSummaryDto` |
| 209 | `SearchAsync` | `ErrorMessage` | ✅ yes | guarded by an out-of-order-completion check (`searchText == SearchText`) |

- Logger: **static-form** `LogOperationFailed(_logger, string)` — this class holds **two** `ILogger` fields, so the source generator (`SYSLIB1020`) needs the explicit logger arg. **Unchanged by this work.**
- `using …Localization;` present; file uses `Strings.Common_ActionFailedMessage` (Wave C `CancelInvoiceAsync` guard).
- `CancelInvoiceAsync` (Wave C) already uses `ActionErrorMessage = Strings.Common_ActionFailedMessage` — do not touch.

### B.4 `PosCheckoutViewModel` — 3 sites

| Line | Method | Surface | `State = Error`? |
|---|---|---|---|
| 277 | `LoadOptionsAsync` | `ErrorMessage` | ✅ yes | customers / bookings / products / services |
| 373 | `ProceedToPaymentAsync` | `ErrorMessage` | ✅ yes | `CreateInvoiceAsync` |
| 401 | **`ChargeAsync`** | `ErrorMessage` | ✅ yes | `RecordPaymentAsync` — payment processor / gateway |

- Logger: instance-form `LogOperationFailed(string)` (`"POS checkout operation failed. Operation={Operation}"`).
- **No `Localization` reference in this file yet** → the impl needs `using Rojan.Desktop.Presentation.Localization;` (or fully-qualify).
- `ChargeAsync`'s separate double-charge/idempotency concern (`ROJAN_PHASE8_64_*`) is **out of scope** here and untouched — this is purely the message string.

### B.5 `InvoiceProfileViewModel` — 1 site

| Line | Method | Surface | `State = Error`? |
|---|---|---|---|
| 113 | `LoadAsync` | `ErrorMessage` | ✅ yes | `InvoiceProfileDto` (invoice + items + payments + receipts) |

- Logger: instance-form `LogOperationFailed(string)` (`"Invoice profile operation failed. Operation={Operation}"`).
- **No `Localization` reference in this file yet** → same `using` / fully-qualify as Pos.

---

## C. CLASSIFICATION

| Category | Members | Action |
|---|---|---|
| **A — critical user-visible sensitive leak** | **all 11 sites** — bare `catch (Exception exception)` → `<Surface> = exception.Message` bound to a `TextBlock` | **sanitize** |
| **B — already sanitized** | `ReportingPageViewModel` `catch (OperationCanceledException) → Reporting_RunCancelled` (keep); `AccountingPageViewModel.CancelInvoiceAsync` + all Wave E `AiCenterPageViewModel` command guards → `Common_ActionFailedMessage` (do not touch) | — |
| **C — intentional technical message** | none in this cluster | — |
| **D — out of scope** | sub-waves 2–6; `SettingsPageViewModel` `NotSupportedException` sites; `PosCheckoutViewModel.ChargeAsync` idempotency concern; the static-form Accounting logger; `ExportDialogViewModel` `result.Message` (DTO field) | — |

All 11 are the same shape and the same fix.

---

## D. SECURITY

| Flow | What `exception.Message` can currently put on screen |
|---|---|
| **Reporting** — `LoadAsync` / `RunReportAsync` / `RerunSnapshotAsync` | a backend validation message can quote **report filters** (customer id, date range, employee id) or a **row value**; revenue / customer-metric / employee-performance figures ride through an `ApiException.Message`; `HttpRequestException` → the internal reporting-service host |
| **AI Center** — `LoadAsync` | a health-score / insight / recommendation backend error; token/config detail |
| **AI Center** — `SendMessageAsync` | a model-provider error that **quotes the user's prompt or a partial completion**, or the customer name inside the prompt (confirmed: current tests seed exactly this — *"…for customer Sarah Johnson"* — and it reaches `StatusMessage`) |
| **POS** — `LoadOptionsAsync` | customer list / booking references in a load error |
| **POS** — `ProceedToPaymentAsync` | invoice line items, tax, totals in a `CreateInvoiceAsync` validation message |
| **POS** — `ChargeAsync` | **payment-processor / gateway error text** — card-network decline reasons, merchant-account detail, transaction ids; `RecordPaymentRequest` amounts |
| **Invoice profile** — `LoadAsync` | full invoice + payments + receipts detail in a backend error |

**Sanitization pattern (identical to the Missing-Guard Sweep):**

```csharp
// before
catch (Exception exception)
{
    ErrorMessage = exception.Message;             // or StatusMessage
    State = DashboardState.Error;                 // where present — UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}

// after
catch (Exception)                                // drop the variable → leak structurally impossible
{
    ErrorMessage = Strings.Common_ActionFailedMessage;   // Reporting: Localization.Strings.Common_ActionFailedMessage
    State = DashboardState.Error;                 // UNCHANGED
    LogOperationFailed(nameof(LoadAsync));        // UNCHANGED
}
```

- `RunReportAsync`: keep the preceding `catch (OperationCanceledException) { StatusMessage = Localization.Strings.Reporting_RunCancelled; }` and the `finally { IsRunning = false; }` exactly.
- `SendMessageAsync`: keep `finally { IsSending = false; }`.
- No `State` is added or removed anywhere.

---

## E. ARCHITECTURE

| Question | Answer |
|---|---|
| Reuse existing `[LoggerMessage]`? | **Yes — zero logging change.** All 5 VMs already have an `ILogger` + operation-name-only generated logger invoked in the same catch (`ReportingPageViewModel` / `AiCenterPageViewModel` / `PosCheckoutViewModel` / `InvoiceProfileViewModel` instance-form; `AccountingPageViewModel` static-form because of its two `ILogger` fields). Untouched. |
| New localization? | **No.** `Strings.Common_ActionFailedMessage` ships in all 3 `.resx` (fa/en/ar, Wave A `794648e`) and reads correctly for load + action + status contexts. Optional `Common_LoadFailedMessage` deferred to the parent audit's decision — **not** needed for this sub-wave. |
| `using` additions | `ReportingPageViewModel`: none (use `Localization.Strings.` — matches the file). `AiCenterPageViewModel` / `AccountingPageViewModel`: none (already `using …Localization;`). **`PosCheckoutViewModel` + `InvoiceProfileViewModel`: `+ using Rojan.Desktop.Presentation.Localization;`** (1 line each) — or fully-qualify. |
| DI impact | **None.** |
| Service / contract impact | **None.** |
| `SYSLIB1020` / partial | Not relevant — no `[LoggerMessage]` touched; classes already `partial` where they have a generated logger. |
| `CA1031` | The `#pragma warning disable CA1031` boundary comments stay; removing the unused `exception` identifier does not affect them. |

### Test changes (concrete — from reading the 5 test files)

| Test file | Change |
|---|---|
| `ReportingPageViewModelTests` | line ~108 `Assert.Equal("boom", sut.StatusMessage)` → `Assert.Equal(Strings.Common_ActionFailedMessage, sut.StatusMessage)`; line ~122 same (`RerunSnapshotAsync`). Add a `LoadAsync`-failure test asserting `ErrorMessage == Common_ActionFailedMessage` + `State == Error` if none exists. |
| `AiCenterPageViewModelTests` | `SendMessageCommand_ServiceThrows_LogsErrorWithoutLeakingChatText` — **add** `Assert.Equal(Strings.Common_ActionFailedMessage, sut.StatusMessage)` + `Assert.DoesNotContain("Sarah Johnson", sut.StatusMessage, …)`. Add a `LoadAsync`-failure test if a seam allows it (the deep service chain may make this hard — note in the impl report). |
| `AccountingPageViewModelTests` | line ~103 `Assert.Equal("boom", sut.ErrorMessage)` → generic; **line ~120** `Assert.Equal(backendBody, sut.ErrorMessage); // user-facing behaviour unchanged` → `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage);` + `Assert.DoesNotContain(backendBody, sut.ErrorMessage, …)` + update the comment. `SearchAsync`-failure test (~138): add a surface assertion. |
| `PosCheckoutViewModelTests` | line ~61 `Assert.Equal("boom", sut.ErrorMessage)` → generic. `ProceedToPaymentCommand_BackendThrows_*` (~240) and `ChargeCommand_BackendThrows_*` (~256): add `Assert.Equal(Strings.Common_ActionFailedMessage, sut.ErrorMessage)` + `Assert.DoesNotContain("boom", sut.ErrorMessage, …)`. |
| `InvoiceProfileViewModelTests` | lines ~40 and ~78 `Assert.Equal("boom", sut.ErrorMessage)` → generic. |

**Estimate:** ~12–16 in-place assertion edits + ~1–3 new tests. **No new test files, no new stubs** (every VM's failure-injection stub already throws a seeded message — the assertion just flips from that message to the generic constant, and the existing `DoesNotContain(secret)` assertions become genuine guarantees). Suite ≈ 2,710 → **~2,713** (net, from the added tests).

---

## F. RECOMMENDATION — WAVE SPLIT

### Ship all 11 sites in ONE commit.

| Consideration | Assessment |
|---|---|
| Shape uniformity | 11 identical changes — drop `catch` variable, swap to `Common_ActionFailedMessage` |
| Domain coherence | one cluster: financial (Reporting / Accounting / POS / Invoice) + AI Center — the highest-sensitivity tranche, reviewed together |
| File count | 5 prod (`ReportingPageViewModel`, `AiCenterPageViewModel`, `AccountingPageViewModel`, `PosCheckoutViewModel`, `InvoiceProfileViewModel`) + 5 test files = **10 files** — well within a single reviewable commit |
| Behaviour risk | **none** — `State`, `finally`, cancellation branches, logging, DI, services all unchanged; the only user-visible delta is the failure message text (the intended improvement) |
| Test risk | LOW — every affected test already exists and injects a seeded message; assertions flip in place, all green after |
| Cross-VM coupling | none — each VM is independent |

**No need to split Reporting / AI Center / Accounting-POS into separate commits.** A split would triple the audit→review→commit overhead for zero risk reduction.

### Implementation plan (Phase 8.104)

- **Prod files (5):**
  - `ReportingPageViewModel.cs` — 3 catches: drop var, `= Localization.Strings.Common_ActionFailedMessage`.
  - `AiCenterPageViewModel.cs` — 2 catches: drop var, `= Strings.Common_ActionFailedMessage`.
  - `AccountingPageViewModel.cs` — 2 catches: drop var, `= Strings.Common_ActionFailedMessage`.
  - `PosCheckoutViewModel.cs` — `+ using …Localization;`; 3 catches: drop var, `= Strings.Common_ActionFailedMessage`.
  - `InvoiceProfileViewModel.cs` — `+ using …Localization;`; 1 catch: drop var, `= Strings.Common_ActionFailedMessage`.
  - Keep every `State = DashboardState.Error`, every `finally`, the Reporting `OperationCanceledException` branch, and every `Log…` call byte-for-byte.
- **Test files (5):** the edits in §E.
- **No** DI change, **no** service change, **no** contract change, **no** `.resx` change, **no** new file.
- **Risk: LOW.** **Suite:** ~2,710 → ~2,713. **Build:** 0/0 expected. **Architecture:** 7/7 (no new type dependency).
- **Commit subject:** `fix(desktop): sanitize reporting, AI center and accounting error surfacing`

### Separate from Missing-Guard work

The Missing-Guard Sweep (Waves A–F + Settings, `794648e` … `0260bc3`) added *new guards*. This changes the *message* in *existing* catches. No overlap.

---

## STOP

Phase 8.103 audit complete. HEAD `0260bc3`, tracked tree clean, baseline 2,710 / 2,710.
**11 Category-A sites across 5 ViewModels** (`ReportingPageViewModel` ×3, `AiCenterPageViewModel` ×2, `AccountingPageViewModel` ×2, `PosCheckoutViewModel` ×3, `InvoiceProfileViewModel` ×1) surface `exception.Message` to a bound `TextBlock` — exposing report filters/revenue, AI prompts/customer names (confirmed live in `SendMessageAsync`), payment-gateway errors, and invoice detail. Uniform behaviour-neutral fix: **drop the `catch` variable, swap `= exception.Message` → `= Strings.Common_ActionFailedMessage`**; keep `State = Error`, `finally`, the cancellation branch, and every log call. **No logging / localization / DI / service / contract change.** `PosCheckoutViewModel` + `InvoiceProfileViewModel` each need one `using` line.
**Recommendation: one commit, all 11 sites, LOW risk. ~12–16 test-assertion edits + ~1–3 new tests, suite ~2,713.**

**Awaiting Phase 8.104 authorization.**
