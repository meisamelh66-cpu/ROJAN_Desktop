# ROJAN AI — TEAM 3 — PHASE 8.35 ACCEPTINVITE SECURITY LOGGING — IMPLEMENTATION REPORT v1

**Type:** Implementation complete. Build + full test suite + architecture tests all green.
**No commit performed** — per the authorization's STOP condition ("WAIT FOR SCOPE REVIEW"). `HEAD` is
still `0542041`.

**Branch:** `feature/team3-desktop-completion`
**Authorization:** `PHASE 8.35 — ACCEPTINVITE SECURITY LOGGING — IMPLEMENTATION v1`
**Scope reference:** `ROJAN_PHASE8_34_ACCEPTINVITE_LOGGING_SCOPE_AUDIT_v1.md`

---

## A. Files Changed

Exactly 2 — 1 production + 1 test, both on the authorization's allow-list. **No DI, no interface, no
shared stub, no other production file.**

| File | +/− | Change |
|---|---|---|
| `src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs` | +15 / −2 | `sealed`→`sealed partial`; +2 `using`s; +`ILogger<AcceptInviteViewModel> _logger` field; ctor +4th optional param `ILogger<AcceptInviteViewModel>? logger = null` + `NullLogger` fallback; +1 `[LoggerMessage(Level = Error)]` partial (`LogOperationFailed(string operation)`); +2 call sites |
| `tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs` | +114 / −? | +2 `using`s; +4 tests + a `SecretToken` const; the **private nested** `StubCurrentSessionService` gains `Exception? InitializeException` + a throw check in `InitializeAsync` |

`git diff --stat`: `2 files changed, 129 insertions(+), 2 deletions(-)`

**Confirmed NOT touched:** DI registration (`ServiceCollectionExtensions.cs`), any interface, Domain,
backend contracts, RBAC, Navigation, any **shared** stub file. **The token flow, session flow, membership
logic, and permission logic are all unchanged** — the only edits inside `LookupAsync` / `AcceptAsync` are
one appended log call each. `SupportPageViewModel` (Wave 2C-1a, already committed at `0542041`) is
untouched.

---

## B. Logging Implementation

### B.1 Applied shape (the `MobileOtpLoginViewModel` security precedent)

```csharp
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public sealed partial class AcceptInviteViewModel : ViewModelBase
{
    private readonly ILogger<AcceptInviteViewModel> _logger;

    public AcceptInviteViewModel(
        ISalonInviteService inviteService,
        ISalonContextService salonContextService,
        ICurrentSessionService currentSessionService,
        ILogger<AcceptInviteViewModel>? logger = null)   // 4th, optional, appended last
    {
        // existing assignments unchanged
        _logger = logger ?? NullLogger<AcceptInviteViewModel>.Instance;
    }

    // LookupAsync catch — AFTER the unchanged  LookupErrorMessage = exception.Message;
    //     LogOperationFailed(nameof(LookupAsync));

    // AcceptAsync catch — AFTER the unchanged   AcceptErrorMessage = exception.Message;
    //     LogOperationFailed(nameof(AcceptAsync));

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")]
    private partial void LogOperationFailed(string operation);
}
```

- **2 log call sites** — the `LookupAsync` and `AcceptAsync` broad-catch boundaries.
- **Level `Error`**, `[LoggerMessage]` source-gen (CA1848), instance form (one logger field → no
  `SYSLIB1020`).
- **`LogOperationFailed(string operation)`** — signature exactly as authorized; called only with
  `nameof(LookupAsync)` / `nameof(AcceptAsync)`.
- **The `Exception` is NEVER passed to the logger.**

### B.2 Produced log lines

```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Membership.AcceptInviteViewModel: Invite operation failed. Operation=LookupAsync
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Membership.AcceptInviteViewModel: Invite operation failed. Operation=AcceptAsync
```

---

## C. Security Validation

| Prohibited item (per authorization) | In a log line? | Why not |
|---|---|---|
| **Invite token** (`_token` / `Token`) | **No** | never referenced by any log call; the `[LoggerMessage]` signature is `(string operation)` only; the `Exception` (which a backend "invite `<token>` not found" message could carry) is **never passed** |
| **Bearer token** (session access / refresh) | **No** | not held by this ViewModel; the exception is never passed |
| **User identity** (id / name / email) | **No** | not held by this ViewModel; resolved by `_currentSessionService.InitializeAsync()` inside `AcceptAsync` — its failure `Exception` is never passed; no identity-shaped value is referenced by any log call |
| **Email** | **No** | same |
| **Salon identifiers** (`Details.SalonName`, `AcceptedMembershipDto.SalonId`) | **No** | not referenced by any log call |
| **Role data** (`Details.Role`) | **No** | not referenced by any log call |
| **Backend response** | **No** | only carried by `Exception.Message`, never passed |
| **`Exception.Message`** | **No** | the two calls pass `nameof(...)` only |
| **Exception object** | **No** | `LogOperationFailed(string operation)` has no `Exception` parameter |

**Test-enforced (this is the load-bearing part):**
- `LookupCommand_Failure_LogsErrorWithoutLeakingToken` — seeds `Token = "SECRET-INVITE-TOKEN-xyz789"`,
  makes `GetDetailsAsync` throw `"invite SECRET-INVITE-TOKEN-xyz789 not found …"`, and asserts the token
  string **is** in the user-facing `LookupErrorMessage` (unchanged behaviour) but **is NOT** in
  `entry.Message`.
- `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` — same for the accept path.
- `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` — accept succeeds, then
  `InitializeAsync` throws `"session resolution failed for user owner@salon.example (id u-4821)"`;
  asserts `entry.Message` contains `"AcceptAsync"` and **does NOT contain** `"owner@salon.example"`,
  `"u-4821"`, or the token.

### C.1 Behaviour preservation

Both catches keep their exact `catch (Exception exception)` filter, `#pragma warning disable CA1031`,
their `LookupErrorMessage = exception.Message;` / `AcceptErrorMessage = exception.Message;` line, and
their `finally` block. The log call is appended **after** the error-message assignment. No catch
removed, no rethrow. Unchanged: the `Details`-invalidation-on-changed-token, `IsAccepted` on success,
`_salonContextService.Invalidate()`, the `_currentSessionService.InitializeAsync()` call,
`CanLookup`/`CanAccept`, and every `Is*`/`Has*` flag. Verified by every pre-existing
`AcceptInviteViewModelTests` test passing unchanged (incl. `AcceptCommand_Failure_SetsAcceptErrorAndNeverTouchesTheSession`,
`AcceptCommand_Success_InvalidatesSalonContextAndReinitializesSession`).

---

## D. Tests

**+4 tests** (Presentation.Tests: 610 → 614). All green.

| Test | Setup | Assertion |
|---|---|---|
| `LookupCommand_Failure_LogsErrorWithoutLeakingToken` | `Token = SecretToken`; `DetailsException` message embeds `SecretToken` | `HasLookupError`; token **in** `LookupErrorMessage`; one `Error` entry with `"LookupAsync"` and **not** `SecretToken` |
| `AcceptCommand_Failure_LogsErrorWithoutLeakingToken` | look up OK; `Token = SecretToken`; `AcceptException` message embeds `SecretToken` | `HasAcceptError`; one `Error` entry with `"AcceptAsync"` and **not** `SecretToken` |
| `AcceptCommand_SessionInitializeFailure_LogsErrorWithoutLeakingIdentity` | look up + `AcceptAsync` OK; `session.InitializeException` message embeds email + user id | `HasAcceptError`; `IsAccepted == false`; one `Error` entry with `"AcceptAsync"` and **not** `"owner@salon.example"` / `"u-4821"` / `SecretToken` |
| `NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows` | `DetailsException`; **no logger** | `Record.Exception(...)` is `null`; `HasLookupError` |

- Uses the existing `RecordingLogger<T>` (`tests/.../Specialists/RecordingLogger.cs`) via `using`.
- The 4 new tests construct the SUT **inline** (`new AcceptInviteViewModel(inviteService, new StubSalonContextService(), session, logger)`)
  rather than through the existing `CreateSut(...)` `out`-param helpers — an optional parameter cannot
  precede `out` parameters in C#, and inline construction keeps each security test's dependencies
  explicit.
- **Stub change — private nested only:** `StubCurrentSessionService` (a `private sealed` nested class in
  this test file, referenced nowhere else) gains `public Exception? InitializeException { get; set; }`;
  `InitializeAsync` now increments its call counter, then `return Task.FromException(InitializeException)`
  when set, else the pre-existing `SessionChanged`-raise + `Task.CompletedTask`. **No shared stub
  touched.** The pre-existing counter-based tests still pass (the counter still increments before the
  throw).

---

## E. Validation Results

### E.1 Build

```
dotnet build
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

### E.2 Full test suite

```
dotnet test --no-build
```

| Project | Passed | Failed | Skipped |
|---|---|---|---|
| Rojan.Desktop.Domain.Tests | 456 | 0 | 0 |
| Rojan.Desktop.Presentation.Tests | **614** | 0 | 0 |
| Rojan.Desktop.Application.Tests | 791 | 0 | 0 |
| Rojan.Desktop.Infrastructure.Tests | 609 | 0 | 0 |
| Rojan.Desktop.Shell.Tests | 80 | 0 | 0 |
| Rojan.Desktop.ArchitectureTests | 7 | 0 | 0 |
| **TOTAL** | **2,557** | **0** | **0** |

- Baseline at `0542041`: **2,553**. Now **2,557** = 2,553 + **4 new**. No pre-existing test changed
  result.

### E.3 Architecture tests

**7 / 7 passing** — unchanged. The `AcceptInviteViewModel` Application-only dependency boundary is
preserved (`Microsoft.Extensions.Logging.Abstractions` is an external abstraction, not a Domain type;
`DependencyDirectionTests` forbids only Infrastructure/Domain/Shell/EF). No
`System.Windows.Threading`/`Controls` added.

### E.4 Expected vs actual

| Expected | Actual | Status |
|---|---|---|
| `dotnet build` PASS | 0 warnings / 0 errors | ✅ |
| Full test suite PASS | 2,557 / 2,557, 0 failed | ✅ |
| Architecture tests PASS | 7 / 7 | ✅ |

---

## F. Commit Readiness

**Ready. Not committed — awaiting the Phase 8.36 commit scope review (with an explicit token/identity
no-leak pass over the final diff) + Phase 8.37 commit execution.**

- **Working tree:** the 2 authorized files modified, nothing else tracked. Untracked = `.md` reports only.
- **Proposed staging (explicit paths only — never `git add -A` / `git add .`):**
  ```
  src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs
  tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
  ```
- **Proposed commit message:**
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
- **Downstream impact:** none on Authentication, Booking, Calendar authority, Shift Engine, RBAC, or
  Navigation.
- **Checkpoint update owed after commit:** §B (new commit + detail), §E (test count 2,553 → 2,557;
  self-logging coverage 19 → 20 of 56), §F (AcceptInvite row resolved — Wave 2C-1 complete; Wave 2C-2
  next), §G.
- **Deferred:** Wave 2C-2 (Automation tabs + parent plumbing), Wave 2C-3 (detail/profile VMs +
  `BookingWizardViewModel` + parent plumbing).

---

## STOP

Implementation and validation complete. **No commit performed.** Awaiting the scope review.
