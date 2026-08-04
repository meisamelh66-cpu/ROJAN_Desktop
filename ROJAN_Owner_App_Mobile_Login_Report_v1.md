# ROJAN Owner App — Mobile OTP Login Implementation Report v1

**Scope:** Owner App Mobile OTP Login v1.0, per "ROJAN Owner App — Mobile OTP Login Implementation" order, building on the completed `ROJAN_Backend` Mobile Authentication Phase 1 (see `ROJAN_Backend/ROJAN_Mobile_Auth_Implementation_Report_v1.md`).
**Status:** Complete. Full solution build — **BUILD SUCCESSFUL**, 2,114/2,114 tests passing (including `Rojan.Desktop.ArchitectureTests`), 0 failures.

---

## 1. What was built

The Login window now leads with Mobile Number + OTP, not email/password:

```
Mobile Number -> Request OTP API -> OTP Input Screen -> Verify OTP API
   -> AuthResponse -> JWT Secure Storage -> Open Dashboard
```

- **Phone entry step**: enter a mobile number, "Send Code" calls `POST /api/v1/auth/otp/request`.
- **Code entry step**: enter the 6-digit code, "Verify & Sign In" calls `POST /api/v1/auth/otp/verify`; "Resend Code" re-calls `/otp/request` (disabled until the backend's own cooldown elapses); "Back" returns to the phone-entry step.
- On success, the existing `AuthResponse` (access/refresh token pair + user) flows through the **same** `ISessionService.CreateSessionFromTokensAsync` → DPAPI-encrypted secure storage → Dashboard pipeline the email flow already used - nothing about session handling, token storage, or the 401-refresh pipeline changed.
- Email/password sign-in still exists, fully functional, one tap away behind a "Sign in with email instead" link - not deleted, not degraded, just no longer the first thing shown.

**Not changed:** the `AuthResponse`/`AuthUserResponse` wire *shape* (only nullability, matching what `ROJAN_Backend` already returns - see §3), `ISessionService`'s session/token/refresh logic, `HttpApiClient`'s pipeline, `AuthBootstrapHttpClient`'s login/refresh bypass design, the Dashboard itself.

## 2. Files changed

### Domain
- `Domain/Identity/UserIdentity.cs` (modified) - added optional `PhoneNumber` (trailing, defaulted `null` - every existing 3-arg call site is untouched)

### Application
- `Application/Api/Contracts/OtpRequestRequest.cs`, `OtpVerifyRequest.cs`, `OtpIssuedResponse.cs` (new) - match `ROJAN_Backend`'s `OtpDtos.kt` field-for-field
- `Application/Api/Contracts/AuthResponse.cs` (modified) - `AuthUserResponse.Email` now nullable, added `PhoneNumber` - matches `ROJAN_Backend`'s already-shipped `UserResponse` shape (Phase 1 made these nullable on the backend; the client DTO was simply out of sync with a contract that had already changed)
- `Application/Security/OtpChallenge.cs` (new) - `(PhoneNumber, ExpiresIn, CanResendAfter)`. Deliberately placed in `Application`, not `Domain` - see the design note in §4.
- `Application/Security/IAuthenticationService.cs` (modified) - added `RequestOtpAsync`/`SignInWithOtpAsync`, same shape/reasoning as the existing `SignInWithCredentialsAsync`

### Infrastructure
- `Infrastructure/Security/BackendAuthenticationService.cs` (modified) - implements both new methods via `AuthBootstrapHttpClient` (never `IApiClient` - same reasoning as the existing login/refresh calls) and `ISessionService.CreateSessionFromTokensAsync`
- `Infrastructure/Security/LocalAuthenticationService.cs` (modified) - `NotSupportedException` stubs for both, mirroring its existing `SignInWithCredentialsAsync` stub

### Presentation
- `Presentation/Threading/IDelayScheduler.cs`, `DispatcherDelayScheduler.cs` (new) - a `DispatcherTimer`-free way for a ViewModel to schedule the resend-cooldown callback; see §4 for why this couldn't just reuse `IToastDismissScheduler`
- `Presentation/ViewModels/Security/MobileOtpLoginViewModel.cs` (new) - drives both steps of the Mobile flow
- `Presentation/ViewModels/Security/LoginWindowViewModel.cs` (new) - composes `MobileOtpLoginViewModel` + the existing `LoginViewModel` behind one `IsEmailModeActive` flag and one bubbled `SignedIn` event; `LoginViewModel` itself needed **zero** changes
- `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` (modified) - registers the two new ViewModels (transient, same lifetime reasoning as `LoginViewModel`) and `IDelayScheduler` (singleton)
- `Presentation/Localization/Strings.cs` + `Strings.resx`/`.en.resx`/`.ar.resx` (modified) - new `Login_Mobile_*`/`Login_SwitchTo*` keys in all three shipped languages (fa default, en, ar)

### Shell
- `Shell/LoginWindow.xaml` (rewritten) - Mobile step 1/step 2 panels (default visible), Email panel (behind the switch link), all via the existing Style+DataTrigger visibility technique - no new value converters
- `Shell/LoginWindow.xaml.cs` (modified) - constructor now takes `LoginWindowViewModel`; `PasswordBox_PasswordChanged` now writes to `viewModel.EmailLogin.Password`

### Tests (new)
- `tests/.../Presentation.Tests/Security/MobileOtpLoginViewModelTests.cs` - 13 tests: missing/invalid phone, success + cooldown scheduling, cooldown elapsing, network failure, rejected-by-backend (covers 429 rate-limiting), missing code, invalid/expired code, success + `SignedIn`, change-number reset, error-clears-on-edit
- `tests/.../Presentation.Tests/Security/LoginWindowViewModelTests.cs` - 5 tests: default mode, mode switching both directions, both children's `SignedIn` bubbling through
- `tests/.../Presentation.Tests/Security/StubAuthenticationService.cs`, `StubDelayScheduler.cs` (new, shared fixtures) - `internal`, reused by both files above
- `tests/.../Application.Tests/Api/Contracts/OtpContractsTests.cs` - 6 tests: JSON round-trips for the 3 new contracts, nullable-field deserialization for both a phone-only and an email-only `AuthUserResponse`
- `tests/.../Infrastructure.Tests/Security/BackendAuthenticationServiceTests.cs` (modified, 4 new tests added) - `RequestOtpAsync` success/rate-limited, `SignInWithOtpAsync` success/invalid-code, exercised against a real `AuthBootstrapHttpClient` + `BackendSessionService` (only the HTTP transport is faked), same "exercise the real workflow" convention as the existing credential tests in this file

### Tests (modified - fixed to compile against the widened `IAuthenticationService` interface)
- `tests/.../Presentation.Tests/Security/LoginViewModelTests.cs`, `tests/.../Presentation.Tests/Settings/StubAuthenticationService.cs` - `NotSupportedException` stubs added for the 2 new interface members

*(`git status` also shows unrelated pre-existing uncommitted work from earlier tickets this session - Dashboard repository, `ApiEnvironmentService`, `BackendSessionService`, Settings page changes. None of that was touched by this ticket; out of scope for this report.)*

## 3. A contract note worth flagging explicitly

`AuthUserResponse.Email` becoming nullable (and gaining `PhoneNumber`) is **not** a backend contract change made by this ticket - `ROJAN_Backend`'s `UserResponse` already shipped this shape in its own Phase 1 (`api/auth/AuthDtos.kt`). The Owner App's DTO was simply stale relative to a contract that had already changed on the server. Left un-synced, `System.Text.Json` would have silently deserialized `"email": null` into the old non-nullable `string Email` as a runtime `null` anyway (nullable reference types are compile-time-only, not enforced by the serializer) - so this wasn't a live bug, but the type now honestly reflects what the field can actually be, which matters the moment any code tries to display or reason about it.

## 4. Two design decisions worth flagging explicitly

**`OtpChallenge` lives in `Application`, not `Domain`.** The obvious first instinct was to put it next to `SessionIdentity`/`AuthenticationState` in `Domain.Security`. `Rojan.Desktop.ArchitectureTests.DependencyDirectionTests.Presentation_ShouldNotDependOnDomainInfrastructureOrShell` forbids exactly that: `MobileOtpLoginViewModel` (Presentation) is the caller of `RequestOtpAsync`, and its return type would then force a Domain-type reference into the Presentation assembly - the same reasoning `IAuthenticationService.SignInWithCredentialsAsync`'s own doc comment already gives for returning plain `Task` instead of `SessionIdentity`. Caught by design, not by the test failing - the full 2,114-test run (including this exact architecture test) confirms it.

**A new `IDelayScheduler`, not a reuse of the existing `IToastDismissScheduler`.** Both are identical in shape (`IDisposable Schedule(TimeSpan, Action)`), and `IToastDismissScheduler` already exists for exactly this "keep `DispatcherTimer` out of a ViewModel" purpose (`Rojan.Desktop.ArchitectureTests.ViewModelTestabilityTests` forbids it directly). Reusing it for the OTP resend cooldown would have worked mechanically, but its name and doc comment are toast-specific - a login screen depending on toast infrastructure would read as a real code smell to the next person touching either file. `IDelayScheduler`/`DispatcherDelayScheduler` is the same few lines, correctly named, in its own `Presentation/Threading` folder; `IToastDismissScheduler` was left untouched rather than refactored into a shared abstraction, since that refactor wasn't asked for and isn't needed for this ticket to be correct.

## 5. Tests

| Suite | New/changed tests | Result |
|---|---|---|
| `MobileOtpLoginViewModelTests` | 13 | All request/verify/expired/invalid/rate-limit/cooldown scenarios the flow can hit |
| `LoginWindowViewModelTests` | 5 | Mode switching, both children's `SignedIn` bubbling |
| `OtpContractsTests` | 6 | JSON round-trips + nullable-field deserialization |
| `BackendAuthenticationServiceTests` | +4 | Real `AuthBootstrapHttpClient`/`BackendSessionService` workflow, only HTTP faked |
| Fixed for interface widening | 2 files | `LoginViewModelTests`, Settings `StubAuthenticationService` |
| **Full solution** | **2,114** | **0 failures, 0 errors** - includes `ArchitectureTests` (6/6), proving no dependency-direction or ViewModel-testability regressions |

`dotnet build RojanDesktop.sln` - **BUILD SUCCESSFUL**, 0 warnings, 0 errors, on the first pass.

## 6. Remaining blockers / gaps (explicit, not hidden)

1. **No FullName field on the Mobile Login screen.** `ROJAN_Backend`'s `/otp/verify` accepts an optional `fullName`, used only the first time a phone number ever verifies (falls back to "ROJAN User" otherwise). This screen never collects or sends it - a deliberate minimal-scope choice matching the ticket's own flow diagram (which has no name-collection step), on the assumption that Owner accounts are pre-provisioned rather than self-registered via first OTP verification. If a genuinely-new phone number does hit `/otp/verify` through this screen, the account gets the backend's generic placeholder name. Worth explicit product sign-off if self-service Owner signup via OTP is actually intended.
2. **429/rate-limit responses show the same generic error message as any other rejected request.** `ApiErrorResponse` (which would carry the backend's `errorCode`, e.g. `OTP_REQUEST_RATE_LIMITED`) is not parsed anywhere in this codebase yet - confirmed by that contract's own doc comment before writing this ticket's code, not assumed. This is consistent with how the existing email-login flow already handles every non-401 error (generic message), not a new gap this ticket introduced - but a rate-limited OTP request is arguably a case worth a friendlier "try again in a bit" message specifically, which would require wiring up `ApiErrorResponse` parsing for the first time anywhere in this client.
3. **No live resend-cooldown countdown text.** The Resend button is simply disabled until the backend's cooldown elapses (one-shot `IDelayScheduler.Schedule`), not "Resend in 47s" ticking down live. Straightforward to add later (swap the one-shot schedule for a repeating one) but out of scope for a first pass.
4. **Manual UI verification not performed.** Per this session's own tooling limits, the new XAML was not driven through a live WPF window - correctness here rests on the build succeeding, the ViewModel/binding-path tests passing, and careful reuse of this file's own pre-existing Style+DataTrigger visibility technique (which was already proven correct for `IsBusy`/`ErrorMessage` before this change). Recommend an actual click-through before shipping, especially for the two-step panel switching and the RTL (Arabic) layout.
5. **The client-side E.164 regex (`^\+[1-9]\d{7,14}$`) is a literal copy of the backend's own validation regex**, kept in `MobileOtpLoginViewModel` rather than a shared library (none exists between the two separate repos/languages). If the backend's pattern ever changes, this one won't automatically follow - same drift risk any duplicated cross-repo validation rule has.
