# ROJAN AI — TEAM 3 — PHASE 8.36 ACCEPTINVITE LOGGING — COMMIT SCOPE REVIEW v1

**Type:** Readiness review only. **No commit, no push, no merge, no rebase, no amend, no source change.**
**Mode:** READINESS ONLY — confirms the exact diff, token/identity no-leak safety, and staging list
before Phase 8.37 (commit execution).

**Branch:** `feature/team3-desktop-completion`
**HEAD:** `0542041` (`git rev-parse HEAD` this turn — unchanged, no drift)
**Predecessors:** `ROJAN_PHASE8_34_ACCEPTINVITE_LOGGING_SCOPE_AUDIT_v1.md` (audit),
`ROJAN_PHASE8_35_ACCEPTINVITE_LOGGING_IMPLEMENTATION_REPORT_v1.md` (impl).

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `0542041ae6d3863d401e70c49e22b6c385233ef6` |
| Branch | `feature/team3-desktop-completion` |
| Staged files | **none** (`git diff --cached` empty) |
| Modified tracked files | **2** — 1 production + 1 test |
| Deleted / renamed | none |
| Untracked | `.md` reports only — no untracked code |

```
git status --porcelain (tracked):
 M src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs
 M tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
```

`git diff --stat`: `2 files changed, 129 insertions(+), 2 deletions(-)`

**Confirmed: no unrelated tracked changes. Only the two authorized AcceptInvite files are modified.**

---

## B. Diff Scope Review (Task 2)

### B.1 Production — `AcceptInviteViewModel.cs` (+15 / −2)

| Hunk | Change | Assessment |
|---|---|---|
| usings | +`Microsoft.Extensions.Logging`, +`Microsoft.Extensions.Logging.Abstractions` | additive; `Abstractions` already a Presentation `PackageReference` |
| class decl | `sealed`→`sealed partial` | required for the `[LoggerMessage]` source generator |
| field | +`private readonly ILogger<AcceptInviteViewModel> _logger;` | one logger field |
| ctor | +4th parameter `ILogger<AcceptInviteViewModel>? logger = null` (optional, appended last); +`_logger = logger ?? NullLogger<AcceptInviteViewModel>.Instance;` | non-breaking — the existing 3-arg positional call sites (DI + `AcceptInviteViewModelTests.CreateSut`) still compile |
| `LookupAsync` catch (`:164`) | +`LogOperationFailed(nameof(LookupAsync));` **after** the unchanged `LookupErrorMessage = exception.Message;` | additive; the `catch (Exception exception)` filter, `#pragma warning disable CA1031`, and `finally { IsLookingUp = false; }` unchanged |
| `AcceptAsync` catch (`:209`) | +`LogOperationFailed(nameof(AcceptAsync));` **after** the unchanged `AcceptErrorMessage = exception.Message;` | additive; same catch/pragma/finally unchanged |
| new method | `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")] private partial void LogOperationFailed(string operation);` + a 3-line security comment | signature takes **only `string operation`** — no `Exception` |

### B.2 Test — `AcceptInviteViewModelTests.cs` (+114 / −0)

- +2 `using`s (`Microsoft.Extensions.Logging`, `Rojan.Desktop.Presentation.Tests.Specialists` for `RecordingLogger<T>`).
- +1 `private const string SecretToken = "SECRET-INVITE-TOKEN-xyz789";`
- **+4 `[Fact]` tests** (all construct the SUT inline).
- The **private nested** `StubCurrentSessionService` gains `public Exception? InitializeException { get; set; }`
  and a throw check in `InitializeAsync` (`InitializeAsyncCallCount++;` then
  `return Task.FromException(InitializeException)` when set, else the pre-existing behaviour).
- **No existing test body was modified** — the 7 pre-existing tests are byte-for-byte unchanged; the two
  `CreateSut(...)` `out`-param helpers are unchanged.

### B.3 Confirmed NOT changed (Task 2)

| Area | Evidence in the diff |
|---|---|
| **`SupportPageViewModel`** | **not in the diff** — Wave 2C-1a (`0542041`) is untouched |
| **DI** | `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` — not in the diff. `AcceptInviteViewModel` stays `AddTransient`; `AddLogging()` fills the new optional param |
| **Interfaces** | no `I*.cs` in the diff — `ISalonInviteService`, `ISalonContextService`, `ICurrentSessionService` all untouched |
| **Domain** | no `Rojan.Desktop.Domain` file in the diff |
| **Backend contracts** | none touched — no API client, DTO, or endpoint |
| **RBAC** | no permission gate / `RolePermissions` / `IPermissionEngine` file touched |
| **Authentication services** | no auth file touched — `AcceptInviteViewModel`'s `_currentSessionService.InitializeAsync()` call is unchanged |
| **Navigation** | no `NavigationService` / `INavigationService` file touched |
| **Shared production stubs** | **none touched.** The modified `StubCurrentSessionService` is a **`private sealed` nested class inside `AcceptInviteViewModelTests.cs`**. `grep` confirms every other `StubCurrentSessionService` reference is in a **different assembly** (`tests/Rojan.Desktop.Shell.Tests/Navigation/StubCurrentSessionService.cs`, `internal sealed`) — a separate class, not affected. `RecordingLogger.cs`, `StubSalonInviteService`, `StubSalonContextService` are all unmodified |

---

## C. Security Validation (Task 3)

### C.1 Pattern

| Check | Confirmed in diff |
|---|---|
| `ILogger<AcceptInviteViewModel>` | instance field, constructor-injected via the optional 4th param |
| `NullLogger<T>.Instance` | `_logger = logger ?? NullLogger<AcceptInviteViewModel>.Instance;` — proven by `NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows` |
| `[LoggerMessage(Level = Error)]` | `[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")]`; source-generated partial (CA1848); instance form (one logger field → no `SYSLIB1020`) |

### C.2 Allowed vs forbidden — verified line-by-line

The produced lines are always exactly:
```
<timestamp> [Error] …AcceptInviteViewModel: Invite operation failed. Operation=LookupAsync
<timestamp> [Error] …AcceptInviteViewModel: Invite operation failed. Operation=AcceptAsync
```

| Forbidden item | In a log line? | Why not |
|---|---|---|
| **Invite token** (`_token` / `Token`) | **No** | never referenced by any log call; the `[LoggerMessage]` signature is `(string operation)` only; the `Exception` is **never passed** |
| **Bearer token** (session access / refresh) | **No** | not held by this ViewModel; the exception is never passed |
| **User identity** (id / name / email) | **No** | not held by this ViewModel; resolved by `_currentSessionService.InitializeAsync()` — its failure `Exception` is never passed; no identity-shaped value is referenced by any log call |
| **Email** | **No** | same |
| **Role data** (`Details.Role`) | **No** | not referenced by any log call |
| **Salon sensitive data** (`Details.SalonName`, `AcceptedMembershipDto.SalonId`) | **No** | not referenced by any log call |
| **Backend response** | **No** | only carried by `Exception.Message`, never passed |
| **Exception object** | **No** | `LogOperationFailed(string operation)` has no `Exception` parameter |
| **`Exception.Message`** | **No** | the two calls pass `nameof(LookupAsync)` / `nameof(AcceptAsync)` only |

**Only `Operation=LookupAsync` / `Operation=AcceptAsync` is logged. The exception is never passed to the
logger.** ✅

### C.3 Test-enforced

| Test | Guards against |
|---|---|
| `LookupCommand_Failure_LogsErrorWithoutLeakingToken` | `Token = SecretToken`; `GetDetailsAsync` throws a message **containing** `SecretToken`. Asserts the token **is** in the user-facing `LookupErrorMessage` (unchanged behaviour) but **`Assert.DoesNotContain(SecretToken, entry.Message)`** |
| `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` | same for the accept path — `Assert.DoesNotContain(SecretToken, entry.Message)` |
| `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` | `InitializeAsync` throws `"…for user owner@salon.example (id u-4821)"`. Asserts `entry.Message` contains `"AcceptAsync"` and **`DoesNotContain`** `"owner@salon.example"`, `"u-4821"`, **and** `SecretToken` |

---

## D. Behaviour Validation (Task 4)

| Signal | Confirmed unchanged (per diff) |
|---|---|
| Token flow | `_inviteService.GetDetailsAsync(Token.Trim())` / `_inviteService.AcceptAsync(Token.Trim(), Details.SalonName)` — not in the diff. The `Token` setter and its `Details = null` side effect — not in the diff |
| Membership flow | `Details` assignment, `HasDetails`, `JoinPrompt` — not in the diff |
| Session initialization | `_salonContextService.Invalidate()` + `await _currentSessionService.InitializeAsync()` inside `AcceptAsync`'s `try` — not in the diff |
| Permission checks | `CanLookup()` / `CanAccept()` — not in the diff |
| `ErrorMessage` behaviour | `LookupErrorMessage = exception.Message;` / `AcceptErrorMessage = exception.Message;` — untouched; log appended after |
| Existing catch behaviour | `catch (Exception exception)` filter + `#pragma warning disable CA1031` + `finally` — unchanged; no rethrow |
| `IsLookingUp` / `IsAccepting` / `IsAccepted` flags | not in the diff |

**Logging is only appended.** ✅ Verified by every pre-existing `AcceptInviteViewModelTests` test
passing unchanged, including `AcceptCommand_Failure_SetsAcceptErrorAndNeverTouchesTheSession` and
`AcceptCommand_Success_InvalidatesSalonContextAndReinitializesSession`.

---

## E. Test Validation (Task 5)

### E.1 Required coverage

| Requirement | Test | ✓ |
|---|---|---|
| LookupAsync failure — Error log created + token not leaked | `LookupCommand_Failure_LogsErrorWithoutLeakingToken` | ✅ |
| AcceptAsync failure — Error log created + token not leaked | `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` | ✅ |
| Session Initialize failure — identity data not leaked | `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` (also asserts token not leaked) | ✅ |
| NullLogger safety | `NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows` | ✅ |

Each of the first three also asserts the pre-existing user-visible outcome (`HasLookupError` /
`HasAcceptError` / `IsAccepted == false`).

### E.2 Test-infra checks

| Check | Result |
|---|---|
| `RecordingLogger<T>` reused | **Yes** — `using Rojan.Desktop.Presentation.Tests.Specialists;`, the existing double, unmodified |
| Private nested stub only | **Yes** — `StubCurrentSessionService` is `private sealed` inside `AcceptInviteViewModelTests.cs`; the same-named class in `Rojan.Desktop.Shell.Tests` is a separate `internal` class in a separate assembly, not touched |
| No shared stub modification | **Confirmed** — no shared stub file is in the diff |

### E.3 Fresh re-run this turn (HEAD `0542041` + working tree)

| Check | Result |
|---|---|
| `dotnet build` | **Build succeeded. 0 Warning(s), 0 Error(s).** |
| Full test suite | **2,557 / 2,557 passing, 0 failed, 0 skipped** (Domain 456, Presentation **614**, Application 791, Infrastructure 609, Shell 80, Architecture 7) |
| `AcceptInviteViewModelTests` (filtered) | **11 / 11 passing** (7 pre-existing + 4 new) |
| Architecture tests | **7 / 7 passing** |
| Delta vs `0542041` baseline (2,553) | **+4** — the 4 new tests; no pre-existing test changed result |

---

## F. Validation Review (Task 6) — Expected vs Actual

| Expected | Actual | Status |
|---|---|---|
| Build: 0 warnings, 0 errors | 0 / 0 | ✅ |
| Tests: 2557 / 2557 | 2,557 / 2,557 | ✅ |
| Architecture: 7 / 7 | 7 / 7 | ✅ |

---

## G. Commit Readiness (Task 7)

### G.1 Staging (explicit paths only — never `git add -A` / `git add .`)

```
git add src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs
git add tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
```

### G.2 Commit message (single isolated commit)

```
fix(desktop): log invite lookup and accept failures

Add ILogger<T> to AcceptInviteViewModel so its LookupAsync and AcceptAsync
broad-catch boundaries log the failure at Error before surfacing the
existing on-screen error. Operation name only - the exception is never
passed to the logger, so the invite token, the user identity resolved by
ICurrentSessionService.InitializeAsync, and any backend response stay out
of the log. Follows the MobileOtpLoginViewModel security precedent; no
DI, interface, or behaviour change.

Adds 4 tests (Lookup/Accept failures log Error without token leakage,
session-init failure logs Error without identity leakage, NullLogger
safety). Extends only the private nested StubCurrentSessionService with
an InitializeException seam.
```

### G.3 Post-commit follow-up (Phase 8.37)

1. Fresh validation on the new HEAD (build + full suite + architecture tests).
2. Update `ROJAN_TEAM3_PROJECT_STATE_CHECKPOINT_v1.md`: §B (new commit + Phase 8.35 detail), §E (test
   count 2,553 → 2,557; self-logging coverage 19 → 20 of 56), §F (AcceptInvite row resolved — Wave 2C-1
   complete; Wave 2C-2 next), §G.

### G.4 Deferred

- Wave 2C-2 (Automation tabs ×5 + `AutomationPageViewModel` logger plumbing).
- Wave 2C-3 (detail/profile VMs + `BookingWizardViewModel` + parent plumbing).

---

## H. Readiness Verdict

**READY TO COMMIT.**

- Diff is complete, minimal (1 production file +15/−2, 1 test file +114), single-concern, matches the
  Phase 8.35 authorization exactly (2 files).
- Build clean, 2,557/2,557 tests green (AcceptInvite's 11 verified in isolation), architecture 7/7 —
  re-verified this turn; all three match the authorization's expected values.
- No change to `SupportPageViewModel`, DI, interfaces, Domain, backend contracts, RBAC, Authentication
  services, Navigation, or shared production stubs.
- **No sensitive value can enter the log** — the exception is never passed; the template carries only a
  `nameof` operation name; token-non-leak (×2) and identity-non-leak are test-enforced with explicit
  `Assert.DoesNotContain` assertions.
- Token flow, membership flow, session initialization, permission checks, error/status behaviour, and
  existing catch behaviour verified unchanged.
- All four required tests present; `RecordingLogger` reused; only a private nested stub extended.
- Staging list and commit message specified above, ready for Phase 8.37.

---

## STOP

Commit scope review complete. No commit performed. Awaiting Phase 8.37 (commit execution) authorization.
