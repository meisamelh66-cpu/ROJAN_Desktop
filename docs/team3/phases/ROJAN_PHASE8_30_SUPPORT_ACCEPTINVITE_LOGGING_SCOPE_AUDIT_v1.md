# ROJAN AI — TEAM 3 — PHASE 8.30 WAVE 2C-1 — SUPPORT + ACCEPTINVITE LOGGING — SCOPE AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no tests added, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `cbc3a82` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md` §F/§G

Every fact verified against source this turn.

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `cbc3a820aae3daa90410eada6ff02d53c163b945` |
| Branch | `feature/team3-desktop-completion` |
| Working tree | **clean** — 0 modified/deleted tracked files |
| Existing modifications | none |
| Untracked | `.md` reports only |

`git status --porcelain` (tracked) → empty. **Tracked tree clean.** ✅

---

## B. Support Analysis (Task 2)

### B.1 `src/Rojan.Desktop.Presentation/ViewModels/Support/SupportPageViewModel.cs` (288 lines)

| Aspect | Verified |
|---|---|
| Class | `public sealed class SupportPageViewModel : ViewModelBase` (not `partial`) |
| Existing logger | **None** |
| Constructor (`:48`) | `SupportPageViewModel(IRojanBrandConfiguration brandConfiguration, ISupportMessageService messageService, IDevelopmentApplicationService applicationService)` — **3 dependencies** |
| Auto-load | **None** — this page is static content + two forms (Send Message, Development Application). No `LoadAsync`, no `_ = ...` in the ctor |
| DI registration | `services.AddTransient<SupportPageViewModel>()` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:75`) |

### B.2 Catch boundaries

| Method | Line | Catch | Current handling | In scope? |
|---|---|---|---|---|
| `SubmitMessageAsync` | `:248` | `catch (Exception exception) when (exception is not OperationCanceledException)` | `MessageError = exception.Message;` | **YES** |
| `SubmitApplicationAsync` | `:277` | `catch (Exception exception) when (exception is not OperationCanceledException)` | `ApplicationError = exception.Message;` | **YES** |
| `OpenUrl` (`Process.Start`) | `:283` | none | — | No |

Both are filtered broad catches (excluding `OperationCanceledException`, which is not logged elsewhere
either). **2 log insertion points**, each after the existing `*Error = exception.Message;` line.

### B.3 User-visible state (must stay unchanged)

`MessageError` / `MessageStatus` (Send Message form); `ApplicationError` / `ApplicationStatus`
(Development Application form). No `DashboardState`.

### B.4 Exact logging insertion points

```csharp
// SubmitMessageAsync catch — after the unchanged line:
    MessageError = exception.Message;
    LogOperationFailed(nameof(SubmitMessageAsync));   // ADD

// SubmitApplicationAsync catch — after the unchanged line:
    ApplicationError = exception.Message;
    LogOperationFailed(nameof(SubmitApplicationAsync)); // ADD

[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Support page operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);
```

Plus: `sealed`→`sealed partial`; +2 `using`s; +field; ctor +4th optional param `ILogger<SupportPageViewModel>? logger = null` + `NullLogger` fallback.
**2 log call sites.**

### B.5 Support security note (not auth-sensitive, but PII-bearing forms)

`SubmitMessageAsync` submits `MessageSenderName`, `MessageSenderEmail`, `MessageSubject`, `MessageBody`
(all user-typed). `SubmitApplicationAsync` submits applicant first/last name, **mobile**, **email**,
city, GitHub/LinkedIn/portfolio/**resume** URLs, and a free-text description. A backend validation
failure could echo any of these in its error message. With **operation-name-only, exception not passed**,
none of it is logged — the line carries only `Operation=SubmitMessageAsync` / `SubmitApplicationAsync`.
Support has **no authentication relationship** (no token, no session, no identity resolution).

---

## C. AcceptInvite Security Analysis (Task 3)

### C.1 `src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs` (214 lines)

| Aspect | Verified |
|---|---|
| Class | `public sealed class AcceptInviteViewModel : ViewModelBase` (not `partial`) |
| Existing logger | **None** |
| Constructor (`:48`) | `AcceptInviteViewModel(ISalonInviteService inviteService, ISalonContextService salonContextService, ICurrentSessionService currentSessionService)` — **3 dependencies** |
| Auto-load | **None** — the flow is user-driven (paste token → Lookup → Accept) |
| DI registration | `services.AddTransient<AcceptInviteViewModel>()` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:59`) |

### C.2 Authentication / membership relationship

`AcceptInviteViewModel` drives the confirmation screen shown **whenever the signed-in user has no real
salon membership yet** (`CurrentSessionService.InitializeAsync` decides this). It is the membership-join
flow. Two boundaries:

| Method | Line | Catch | Current handling | Auth/token exposure in the `try` |
|---|---|---|---|---|
| `LookupAsync` | `:158` | `catch (Exception exception)` (`#pragma warning disable CA1031`) | `LookupErrorMessage = exception.Message;` | `_inviteService.GetDetailsAsync(Token.Trim())` — **the invite token is sent to the backend** |
| `AcceptAsync` | `:196` | `catch (Exception exception)` (`#pragma warning disable CA1031`) | `AcceptErrorMessage = exception.Message;` | `_inviteService.AcceptAsync(Token.Trim(), Details.SalonName)` — **the invite token** + salon name; then **`_currentSessionService.InitializeAsync()`** — a real network round-trip that **re-establishes the session with the newly-granted membership** (auth-adjacent) |

### C.3 Data surface

| Data | Where | Sensitivity |
|---|---|---|
| **Invite token** (`_token` / `Token` — user-typed, settable) | passed to `GetDetailsAsync` / `AcceptAsync` | **HIGH — a bearer credential for joining a salon.** Must never be logged |
| `SalonInviteDetailsDto` | returned by `GetDetailsAsync` | record is `(string SalonName, string Role)` **only** — **no email, no inviter identity, no token echo** (verified: `src/Rojan.Desktop.Application/Membership/SalonInviteDetailsDto.cs`) |
| Salon name / role | shown to the user via `JoinPrompt` | Low (user-facing), but not needed in a log |
| User identity (email / user id) | **not held by this ViewModel**; but `_currentSessionService.InitializeAsync()` resolves it — a failure there could carry it in `Exception.Message` | Must not be logged |
| Session / auth tokens | live in `SessionService`, not this ViewModel | Must not be logged |

### C.4 What MUST NOT be logged

| Item | Guaranteed absent by the recommended design |
|---|---|
| **Invite token** | `_token` / `Token` is never referenced by any log call; the exception (which could carry it) is never passed to the logger |
| **Email** | not held by the ViewModel; and the exception is never passed |
| **User identifiers** | same — `InitializeAsync`'s failure detail is never passed |
| **Backend response** | only carried by `Exception.Message`, which is never passed |
| **`Exception.Message`** | the `LogOperationFailed(...)` call takes only `nameof(<method>)` |
| Salon name / role | not referenced by any log call (defensive — logged nowhere) |

### C.5 Recommended safe design — operation-name-only (SAFE)

Identical to `MobileOtpLoginViewModel` (Phase 8.15):

```csharp
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);

// LookupAsync catch — after the unchanged:  LookupErrorMessage = exception.Message;
    LogOperationFailed(nameof(LookupAsync));

// AcceptAsync catch — after the unchanged:   AcceptErrorMessage = exception.Message;
    LogOperationFailed(nameof(AcceptAsync));
```

Produced line, always exactly:
```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Membership.AcceptInviteViewModel: Invite operation failed. Operation=<LookupAsync|AcceptAsync>
```

**2 log call sites. Operation name only. Exception never passed. Recommended — safe.**

---

## D. Architecture Impact (Task 4)

| Check | Result (both ViewModels) |
|---|---|
| `ILogger<T>` injection possible | **Yes** — both are `AddTransient` (`ServiceCollectionExtensions.cs:75` / `:59`); `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) registers the open-generic `ILogger<T>`; DI fills the new optional last ctor param automatically |
| DI impact | **None** — the `AddTransient` lines are unchanged; an optional ctor param needs no registration edit |
| Interface impact | **None** — `ISupportMessageService`, `IDevelopmentApplicationService`, `IRojanBrandConfiguration`, `ISalonInviteService`, `ISalonContextService`, `ICurrentSessionService` all untouched |
| Domain impact | **None** — Presentation-layer only; no business rule, permission decision, backend call, or data-authority change. `Booking` / `Calendar` authority / `Shift Engine` / `RBAC` / `Authentication` / `Navigation` all untouched. `AcceptInviteViewModel` continues to depend only on `ISalonInviteService` (Application), never a Domain-typed repository — the `Rojan.Desktop.ArchitectureTests` boundary is preserved |
| Backend contract impact | **None** — no API client, DTO, or endpoint touched |
| `DependencyDirectionTests` / `ViewModelTestabilityTests` | `Microsoft.Extensions.Logging.Abstractions` not forbidden; no `System.Windows.Threading`/`Controls` added. **7/7 expected unchanged** |
| `SYSLIB1020` (multi-logger) | Not a risk — one `ILogger` field each |

---

## E. Test Strategy (Task 5)

### E.1 Existing test files

| File | Exists? | SUT construction | Throw support |
|---|---|---|---|
| `tests/Rojan.Desktop.Presentation.Tests/Support/SupportPageViewModelTests.cs` | **Yes** | `CreateSut()` → `(Sut, StubSupportMessageService, StubDevelopmentApplicationService)` tuple | **Yes** — `StubSupportMessageService.ThrowsOnSubmit` / `StubDevelopmentApplicationService.ThrowsOnSubmit` (used by existing error-path tests) |
| `tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs` | **Yes** | `CreateSut(...)` with `out` params + nested `StubSalonInviteService` / `StubSalonContextService` / `StubCurrentSessionService` | **Yes** — `StubSalonInviteService.DetailsException` / `AcceptException` settable (used by existing error-path tests at `:41`, `:104`) |

**Both `CreateSut` helpers need only a trailing optional `RecordingLogger<T>? = null` parameter.** No new
stub. No shared stub modification. `RecordingLogger<T>` reused via `using Rojan.Desktop.Presentation.Tests.Specialists;`.

### E.2 Support tests (3)

| Test | Setup | Assertion |
|---|---|---|
| `SubmitMessageAsync_ServiceThrows_LogsError` | `messages.ThrowsOnSubmit = true`; `RecordingLogger<SupportPageViewModel>` | `MessageError` set (**unchanged**) **and** an `Error` entry containing `"SubmitMessageAsync"` |
| `SubmitApplicationAsync_ServiceThrows_LogsError` | `applications.ThrowsOnSubmit = true` | `ApplicationError` set **and** an `Error` entry containing `"SubmitApplicationAsync"` |
| `NoLoggerSupplied_UsesNullLogger_SubmitFailureNeverThrows` | throw + no logger | `Record.Exception(...)` is `null` |

### E.3 AcceptInvite tests (4 — **security-focused**)

| Test | Setup | Assertion |
|---|---|---|
| `LookupAsync_Throws_LogsErrorWithoutLeakingToken` | `sut.Token = "SECRET-INVITE-TOKEN-abc123"`; `inviteService.DetailsException = new InvalidOperationException("lookup failed for token SECRET-INVITE-TOKEN-abc123")`; `RecordingLogger` | `LookupErrorMessage` set (**unchanged**); one `Error` entry; `Message` **contains** `"LookupAsync"`; `Message` **does NOT contain** `"SECRET-INVITE-TOKEN-abc123"` |
| `AcceptAsync_Throws_LogsErrorWithoutLeakingToken` | look up successfully, then `inviteService.AcceptException = new InvalidOperationException("accept failed for token SECRET-INVITE-TOKEN-abc123")` with `sut.Token = "SECRET-INVITE-TOKEN-abc123"` | one `Error` entry containing `"AcceptAsync"` and **not** containing the token |
| `AcceptAsync_SessionInitializeThrows_LogsErrorWithoutLeakingIdentity` | successful accept, then `currentSessionService` configured to throw from `InitializeAsync` with an identity-bearing message (`"session refresh failed for owner@example.com"`) | one `Error` entry containing `"AcceptAsync"` and **not** containing `"owner@example.com"` |
| `NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows` | `DetailsException` + no logger | `Record.Exception(...)` is `null` |

**The two `WithoutLeakingToken` tests + the `WithoutLeakingIdentity` test are the explicit
sensitive-log-leakage guards the authorization requires.**

Whether `StubCurrentSessionService.InitializeAsync` currently supports throwing must be confirmed at
implementation time — if not, the third test either configures it via an existing seam or is deferred
(the same `LogOperationFailed` method is already covered by the accept-token test). This is the one
implementation-time check to flag.

### E.4 Regression

Both test files gain tests only (their `CreateSut` gains a trailing optional `= null` param → existing
call sites compile unchanged). Full validation: build (0/0) + full suite (2,550 + ~7) + architecture
(7/7). Expected total: **~2,557**.

---

## F. Commit Plan (Task 6)

### F.1 Options

| | Combined commit (both VMs) | Separate commits |
|---|---|---|
| Auth sensitivity | `SupportPageViewModel` has **zero** auth relationship; `AcceptInviteViewModel` handles an **invite token** + calls `InitializeAsync()` (session re-establishment) | mixed — the sensitive change is bundled with a trivial one |
| Review anchor | a reviewer auditing the token-safety must filter it out of a 2-VM diff | AcceptInvite's diff is small, isolated, and its token-non-leak tests are visible in the same commit |
| Precedent | `MobileOtpLoginViewModel` was **split into its own isolated commit** (Wave 1) *specifically* for its auth-adjacency and to give its data-safety a focused review anchor | matches that precedent |
| Cost | 1 commit | 2 commits (the engagement's standard unit) |

### F.2 Recommendation

**SEPARATE commits.**

1. **`SupportPageViewModel`** — `fix(desktop): add ViewModel diagnostic logging (support page)`
   (1 production + 1 test file; 3 tests).
2. **`AcceptInviteViewModel`** — `fix(desktop): log invite lookup and accept failures`
   (1 production + 1 test file; 4 tests, incl. the token-non-leak + identity-non-leak guards).

Reasoning:
- `AcceptInviteViewModel` is membership/auth-adjacent and touches an invite token + session
  re-establishment. Isolating it keeps its diff small enough to verify the token-safety at a glance,
  with its no-leak tests as the review anchor — exactly the reasoning that split `MobileOtpLoginViewModel`
  out in Wave 1.
- `SupportPageViewModel` is a plain page with no auth relationship; bundling it into the sensitive commit
  would only enlarge the surface a security reviewer must scan.
- One extra commit cycle is the engagement's normal unit of work.

### F.3 Sequencing

1. **Phase 8.31 — Support implementation** (on authorization) → validate → scope review → commit.
2. **Phase 8.3x — AcceptInvite implementation** (separate authorization; resolve the E.3 `InitializeAsync`
   throw-support check first) → validate → scope review (with the token-safety pass) → commit.

Alternatively, both can be implemented together and committed as two commits back-to-back, provided each
gets its own commit scope review.

### F.4 Out of scope

- `SupportPageViewModel.OpenUrl` (`Process.Start`) — no `catch` there; not a logging gap.
- Wave 2C-2 (Automation tabs + parent plumbing), Wave 2C-3 (detail/profile VMs + `BookingWizardViewModel`).

---

## STOP

Audit complete. No implementation performed.

**Recommendation:**
- **Support:** 1 production file (`SupportPageViewModel.cs`, 2 log calls — `SubmitMessageAsync` /
  `SubmitApplicationAsync`) + `SupportPageViewModelTests.cs` (+3 tests). `Error` level, operation-only.
- **AcceptInvite:** 1 production file (`AcceptInviteViewModel.cs`, 2 log calls — `LookupAsync` /
  `AcceptAsync`) + `AcceptInviteViewModelTests.cs` (+4 tests incl. token-non-leak + identity-non-leak
  guards). `Error` level, **operation-name-only, exception never passed** — SAFE.
- **Two separate commits** (Support, then AcceptInvite), per the `MobileOtpLoginViewModel` isolation
  precedent.
