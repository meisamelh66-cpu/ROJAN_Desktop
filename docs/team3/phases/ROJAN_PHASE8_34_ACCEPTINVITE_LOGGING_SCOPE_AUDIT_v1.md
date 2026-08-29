# ROJAN AI — TEAM 3 — PHASE 8.34 ACCEPTINVITE SECURITY LOGGING — SCOPE AUDIT v1

**Type:** Audit only. **No source modified, no logger added, no tests added, no commit, no push.**
**Branch:** `feature/team3-desktop-completion`
**HEAD:** `0542041` (`git rev-parse HEAD` this turn — unchanged)
**Reference:** `ROJAN_PHASE8_30_SUPPORT_ACCEPTINVITE_LOGGING_SCOPE_AUDIT_v1.md` §C/§E

Every fact verified against source this turn. This is the **auth-adjacent second half of Wave 2C-1** —
the security review (Task 3) is the load-bearing section.

---

## A. Git State (Task 1)

| Item | Value |
|---|---|
| HEAD | `0542041ae6d3863d401e70c49e22b6c385233ef6` |
| Branch | `feature/team3-desktop-completion` |
| Working tree | **clean** — 0 modified/deleted tracked files |
| Existing modifications | none |
| Untracked | `.md` reports only |

`git status --porcelain` (tracked) → empty. **Tracked tree clean.** ✅

---

## B. ViewModel Analysis (Task 2)

### B.1 `src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs` (214 lines)

| Aspect | Verified |
|---|---|
| Class | `public sealed class AcceptInviteViewModel : ViewModelBase` (not `partial`) |
| Namespace | `Rojan.Desktop.Presentation.ViewModels.Membership` |
| Existing logger | **None** |
| Constructor (`:48`) | `AcceptInviteViewModel(ISalonInviteService inviteService, ISalonContextService salonContextService, ICurrentSessionService currentSessionService)` — **3 dependencies** (`ICurrentSessionService` = `Rojan.Desktop.Presentation.Organizations.ICurrentSessionService`, per the `using` at `:8`) |
| Auto-load | **None** — the flow is user-driven: paste token → `LookupCommand` → confirm → `AcceptCommand` |
| DI registration | `services.AddTransient<AcceptInviteViewModel>()` (`Presentation/DependencyInjection/ServiceCollectionExtensions.cs:59`) |

### B.2 Authentication / membership relationship

`AcceptInviteViewModel` drives the confirmation screen shown **whenever the signed-in user has no real
salon membership yet** (decided by `CurrentSessionService.InitializeAsync`). It is the membership-join
flow. The class doc comment records that it "Depends only on `ISalonInviteService` (Application) — never
the Domain-typed `ISalonInviteRepository`/`IAcceptedMembershipStore` directly … (enforced by
`Rojan.Desktop.ArchitectureTests`)."

### B.3 Token handling flow

| Step | Code | Token exposure |
|---|---|---|
| user types the invite token | `Token` property setter (`:65`) — plain `string _token` field | in memory |
| lookup | `_inviteService.GetDetailsAsync(Token.Trim())` (`:155`) | **the token is sent to the backend** |
| accept | `_inviteService.AcceptAsync(Token.Trim(), Details.SalonName)` (`:185`) | **the token + salon name are sent to the backend** |

The invite token is a **bearer-style credential** for joining a salon.
`GetDetailsAsync` returns `SalonInviteDetailsDto(string SalonName, string Role)` **only** — no email, no
inviter identity, no token echo (verified: `src/Rojan.Desktop.Application/Membership/SalonInviteDetailsDto.cs`).
`AcceptAsync`'s returned `AcceptedMembershipDto(SalonId, SalonName, Role)` is **discarded** by the
ViewModel (`:185` — result not assigned).

### B.4 Session initialization flow

Inside `AcceptAsync`'s `try`, after a successful `AcceptAsync`:
```csharp
_salonContextService.Invalidate();                              // :190 — sync, void
await _currentSessionService.InitializeAsync().ConfigureAwait(true);   // :191
IsAccepted = true;
```
`_currentSessionService.InitializeAsync()` is **a real network round-trip that re-establishes the session
with the newly-granted membership** (per `CurrentSessionService.InitializeAsync`'s own doc comment: it
resolves the signed-in user's real salon membership via `GET /me/salon-access`). A failure there can
carry the **user's identity (id / name / email), the resolved org/branch/role, and session/token
detail** in its `Exception.Message`.

### B.5 Catch boundaries & existing error handling

| Method | Line | Catch | Existing handling |
|---|---|---|---|
| `LookupAsync` | `:158` | `catch (Exception exception)` (`#pragma warning disable CA1031`) | `LookupErrorMessage = exception.Message;` → `finally { IsLookingUp = false; }` |
| `AcceptAsync` | `:196` | `catch (Exception exception)` (`#pragma warning disable CA1031`) | `AcceptErrorMessage = exception.Message;` → `finally { IsAccepting = false; }` |

Both are unconditional swallowing broad catches. User-visible state: `LookupErrorMessage`/`HasLookupError`,
`AcceptErrorMessage`/`HasAcceptError`, `IsLookingUp`, `IsAccepting`, `IsAccepted`, `Details`/`HasDetails`.

### B.6 Exact logging insertion points

```csharp
// LookupAsync catch — AFTER the unchanged  LookupErrorMessage = exception.Message;
    LogOperationFailed(nameof(LookupAsync));

// AcceptAsync catch — AFTER the unchanged   AcceptErrorMessage = exception.Message;
    LogOperationFailed(nameof(AcceptAsync));

[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);
```

Plus: `sealed`→`sealed partial`; +2 `using`s (`Microsoft.Extensions.Logging`, `…Logging.Abstractions`);
+`private readonly ILogger<AcceptInviteViewModel> _logger;`; ctor +4th optional param
`ILogger<AcceptInviteViewModel>? logger = null` + `_logger = logger ?? NullLogger<AcceptInviteViewModel>.Instance;`.

**Total: 2 log call sites.**

---

## C. Security Analysis (Task 3 — CRITICAL)

### C.1 Every possible log path after the change

There are **exactly two** log call sites, both reached only from a `catch`:

| Site | Call | Emitted |
|---|---|---|
| `LookupAsync` catch | `LogOperationFailed(nameof(LookupAsync))` | `… Operation=LookupAsync` |
| `AcceptAsync` catch | `LogOperationFailed(nameof(AcceptAsync))` | `… Operation=AcceptAsync` |

Produced line, always exactly:
```
<timestamp> [Error] Rojan.Desktop.Presentation.ViewModels.Membership.AcceptInviteViewModel: Invite operation failed. Operation=<LookupAsync|AcceptAsync>
```

### C.2 MUST NOT log — and why each is guaranteed absent

| Item | Present in the flow? | Guaranteed absent because |
|---|---|---|
| **Invite token** (`_token` / `Token`) | **YES** — sent to `GetDetailsAsync` / `AcceptAsync` | `_token` / `Token` is **never referenced by any log call**; the `[LoggerMessage]` signature is `(string operation)` only; the `Exception` (which could carry a backend "invite `<token>` not found" message) is **never passed** |
| **Bearer token** (session access / refresh) | not held by this ViewModel; `InitializeAsync`'s failure could reference one | the exception is never passed |
| **User identity** (id / name / email) | not held by this ViewModel; **resolved by `_currentSessionService.InitializeAsync()`** inside `AcceptAsync`'s `try` — its failure message can contain it | the exception is never passed; no identity-shaped value is referenced by any log call |
| **Email** | same as user identity | same |
| **Salon identifiers** (`Details.SalonName`, `Details.Role`, `AcceptedMembershipDto.SalonId`) | `SalonName`/`Role` shown to the user via `JoinPrompt`; `SalonId` discarded | **not referenced by any log call** — the template carries only a `nameof` operation name |
| **Backend response** | carried by `Exception.Message` from the API/HTTP layer | `Exception.Message` is never passed |
| **`Exception.Message`** | the on-screen `LookupErrorMessage` / `AcceptErrorMessage` text | the `LogOperationFailed(...)` call takes only `nameof(<method>)` |
| **Exception object** | — | `LogOperationFailed(string operation)` has no `Exception` parameter |

### C.3 Safe design — operation-name-only, exception NEVER passed (the ONLY safe design)

```csharp
[LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Invite operation failed. Operation={Operation}")]
private partial void LogOperationFailed(string operation);   // NO Exception parameter
```
- `{Operation}` = compile-time `nameof(LookupAsync)` / `nameof(AcceptAsync)` — a method name, nothing
  else.
- Identical to `MobileOtpLoginViewModel` (Phase 8.15) and every Wave-2 ViewModel: **the exception is
  never passed to the logger.**
- `Level = Error` — clears the `LocalFileLoggerProvider` `Warning` floor.

**This design cannot leak the invite token, the user's identity, a bearer token, a salon identifier, or
any backend response — none of those values is referenced by a log call, and the one object that could
carry them (the `Exception`) is never handed to the logger.** ✅

### C.4 Behaviour preservation

Both catches keep their exact `catch (Exception exception)` filter, `#pragma warning disable CA1031`,
their `LookupErrorMessage = exception.Message;` / `AcceptErrorMessage = exception.Message;` line, and
their `finally` block. The log call is appended **after** the error-message assignment. No catch
removed, no rethrow, no change to: `Details` invalidation on a changed token, `IsAccepted` on success,
`_salonContextService.Invalidate()`, the `InitializeAsync` call, or `CanLookup`/`CanAccept`.

---

## D. Architecture Impact (Task 4)

| Check | Result |
|---|---|
| `ILogger<T>` injection possible | **Yes** — `services.AddTransient<AcceptInviteViewModel>()` (`ServiceCollectionExtensions.cs:59`); `AddLogging()` (`Infrastructure/…/ServiceCollectionExtensions.cs:91`) registers the open-generic `ILogger<T>`; DI fills the new optional 4th param automatically |
| No DI registration change | **Confirmed** — the `AddTransient` line is unchanged |
| No interface change | **Confirmed** — `ISalonInviteService`, `ISalonContextService`, `ICurrentSessionService` all untouched |
| No domain change | **Confirmed** — Presentation-layer only. `AcceptInviteViewModel` continues to depend **only on `ISalonInviteService` (Application)** — `Microsoft.Extensions.Logging.Abstractions` is an external abstraction, not a Domain type. `Rojan.Desktop.ArchitectureTests.DependencyDirectionTests` (Presentation must not reference `Rojan.Desktop.Domain`/`Infrastructure`/EF) is **not** triggered by adding `ILogger<T>` (`Abstractions` is already a Presentation `PackageReference`; the test forbids only Infrastructure/Domain/Shell/EF). `ViewModelTestabilityTests` forbids only `System.Windows.Threading`/`Controls` — neither added |
| No backend contract change | **Confirmed** — no API client, DTO, or endpoint touched |
| Application-only dependency boundary | **UNCHANGED** — verified per the two architecture tests above |
| Architecture suite | **7/7 expected unchanged** |
| `SYSLIB1020` (multi-logger) | Not a risk — one `ILogger` field |

---

## E. Test Strategy (Task 5)

### E.1 Existing test file — `tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs`

| Item | Verified |
|---|---|
| File exists | **Yes** |
| SUT helpers | two `CreateSut(...)` overloads (`:118`, `:128`), both with `out` params |
| `StubSalonInviteService` (private nested, `:137`) | `DetailsException` / `AcceptException` settable — **already used** by the current error-path tests (`:41`, `:104`); also records `LastAcceptToken` / `LastAcceptSalonName` |
| `StubSalonContextService` (private nested, `:168`) | `Invalidate()` counter |
| `StubCurrentSessionService` (private nested, `:179`) | `InitializeAsync` currently does `count++; SessionChanged?.Invoke(...); return Task.CompletedTask;` — **no throw hook** |

### E.2 Required tests (4)

| # | Test | Setup | Assertion |
|---|---|---|---|
| 1 | `LookupAsync_Throws_LogsErrorWithoutLeakingToken` | `sut.Token = "SECRET-INVITE-TOKEN-xyz"`; `inviteService.DetailsException = new InvalidOperationException("invite SECRET-INVITE-TOKEN-xyz not found or no longer available")`; `RecordingLogger<AcceptInviteViewModel>` | `LookupErrorMessage` set (**unchanged**); exactly one `Error` entry; `Message` **contains** `"LookupAsync"`; `Message` **does NOT contain** `"SECRET-INVITE-TOKEN-xyz"` |
| 2 | `AcceptAsync_Throws_LogsErrorWithoutLeakingToken` | look up successfully (`DetailsResult` set); `sut.Token = "SECRET-INVITE-TOKEN-xyz"`; `inviteService.AcceptException = new InvalidOperationException("accept failed for SECRET-INVITE-TOKEN-xyz")`; execute `AcceptCommand` | `AcceptErrorMessage` set; one `Error` entry containing `"AcceptAsync"` and **not** containing the token |
| 3 | `AcceptAsync_SessionInitializeThrows_LogsErrorWithoutLeakingIdentity` | look up + `inviteService.AcceptAsync` succeed; `currentSessionService.InitializeException = new InvalidOperationException("session resolution failed for user owner@salon.example (id u-4821)")`; execute `AcceptCommand` | `AcceptErrorMessage` set; one `Error` entry containing `"AcceptAsync"` and **not** containing `"owner@salon.example"` or `"u-4821"` |
| 4 | `NoLoggerSupplied_UsesNullLogger_LookupFailureNeverThrows` | `DetailsException` + **no logger** | `Record.Exception(...)` is `null` |

### E.3 Stub / helper changes required

| Change | Scope classification |
|---|---|
| `CreateSut(...)` overloads gain a trailing optional `RecordingLogger<AcceptInviteViewModel>? logger = null`, passed to `new AcceptInviteViewModel(...)` | test file only — backward-compatible optional param |
| **`StubCurrentSessionService`** gains `public Exception? InitializeException { get; set; }` and `InitializeAsync` throws it when set (before the `count++`) | **private nested class inside `AcceptInviteViewModelTests.cs`** — **not a shared stub.** `grep` confirms it is `private sealed` and referenced nowhere else. This is the "unless unavoidable" case the authorization allows, and it is minimal and local |

`RecordingLogger<T>` — reused via `using Rojan.Desktop.Presentation.Tests.Specialists;`. **No shared
stub is modified.**

### E.4 Estimated: 4 new tests. Expected suite after implementation: **2,553 + 4 = 2,557.**

---

## F. Commit Plan (Task 6)

### F.1 Recommendation — isolated commit

**`fix(desktop): log invite lookup and accept failures`**

Files (2):
```
src/Rojan.Desktop.Presentation/ViewModels/Membership/AcceptInviteViewModel.cs
tests/Rojan.Desktop.Presentation.Tests/Membership/AcceptInviteViewModelTests.cs
```

### F.2 Why separated from Support (`0542041`, already committed)

| Reason | Detail |
|---|---|
| **Auth sensitivity** | `SupportPageViewModel` has **zero** authentication relationship — support forms, no token, no session. `AcceptInviteViewModel` is the **membership-join / session-re-establishment flow** and handles an **invite token** (a bearer credential) plus a live `InitializeAsync()` identity resolution |
| **Review anchor** | This commit's diff — and specifically its `LookupAsync_Throws_LogsErrorWithoutLeakingToken` / `AcceptAsync_Throws_LogsErrorWithoutLeakingToken` / `AcceptAsync_SessionInitializeThrows_LogsErrorWithoutLeakingIdentity` assertions — is the **security artifact a reviewer must audit in isolation**. Bundling it with the non-sensitive Support change would force that reviewer to filter it out of a 2-VM diff |
| **Precedent** | `MobileOtpLoginViewModel` was split into its own isolated commit (Wave 1) for **exactly this reason** — auth-adjacency + a focused data-safety review anchor. Wave 2C-1 was pre-planned as two commits in `ROJAN_PHASE8_30_*` §F |
| **Cost** | one commit cycle — the engagement's standard unit |

### F.3 Sequencing

1. **Phase 8.35 — Implementation** (on authorization): apply §B.6; add the 4 §E.2 tests + the two
   §E.3 helper/stub changes.
2. **Validate:** build (0/0) + full suite (2,553 + 4) + architecture (7/7).
3. **Phase 8.36 — Commit Scope Review** (readiness only — **with an explicit token/identity no-leak
   pass over the final diff and test assertions**) → **Phase 8.37 — Commit Execution**: isolated
   commit, explicit-path staging, then fresh post-commit validation + checkpoint update
   (§B new commit, §E test count 2,553 → 2,557, self-logging coverage 19 → 20 of 56, §F/§G → Wave 2C-2).

### F.4 Out of scope

- Wave 2C-2 (Automation tabs ×5 + `AutomationPageViewModel` logger plumbing).
- Wave 2C-3 (detail/profile VMs + `BookingWizardViewModel` + parent plumbing).

---

## STOP

Audit complete. No implementation performed.

**Recommendation: 1 production file (`AcceptInviteViewModel.cs`, 2 log calls — `LookupAsync` /
`AcceptAsync`, `Error` level, `LogOperationFailed(string operation)`, exception NEVER passed) + 1 test
file (`AcceptInviteViewModelTests.cs`, +4 tests incl. token-non-leak ×2 + identity-non-leak, plus an
`InitializeException` seam on the *private nested* `StubCurrentSessionService`). Isolated commit,
per the `MobileOtpLoginViewModel` precedent.**
