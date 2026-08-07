# ROJAN Public Launch Hardening — Implementation Plan v1

**Priority:** P0
**Scope:** Planning only. No code, refactoring, architecture, or deployment changes made in this pass.
**Input:** `ROJAN_Public_Launch_Readiness_Audit_v1.md` — this plan resolves every Blocker and Required Action from that audit, plus the two HIGH-classified Known Open Items.
**Repositories:** `ROJAN_Backend` @ `6943986`, `ROJAN_Desktop` @ `8526235`.

---

## How This Plan Is Organized

Every item below traces back to a specific audit finding — no new scope is introduced here that the audit didn't already surface. Items are grouped into three phases by blocking severity, matching the audit's own classification:

- **Phase 1 — Blockers 🔴:** must close before Production Ready can become YES. Three items, all from the audit's Final Summary.
- **Phase 2 — Required Actions 🟠/🟡:** should close before or immediately after public launch; none individually blocks launch, but leaving all of them open does.
- **Phase 3 — Post-launch 🟢:** explicitly deferred by the audit itself (AI Recommendation ML upgrade) or dependent on a team this plan doesn't control (Website Booking verification, coordinated not owned here).

Each item states: what it is, why it's needed (citing the audit), where it lands in the codebase, what "done" looks like, and what it depends on.

---

## 1. Security Fixes

### 1.1 Rate limiting on `/auth/login`, `/auth/register`, `/auth/refresh` — 🔴 BLOCKER

**Audit finding:** `RateLimiterPort`/`RedisRateLimiter` exist and are real, but have exactly two callers in the whole codebase (`RequestOtpUseCase`, `VerifyOtpUseCase`). `AuthenticateUserUseCase`, `RegisterUserUseCase`, and `RefreshTokenUseCase` have no rate-limiting dependency at all.

**Plan:**
1. Extend `AuthenticateUserUseCase`, `RegisterUserUseCase`, and `RefreshTokenUseCase` (all in `application/.../auth/`) to accept a `RateLimiterPort` dependency, mirroring exactly how `RequestOtpUseCase`/`VerifyOtpUseCase` already consume it (same `tryConsume(key, limit, window)` shape).
2. Choose per-endpoint limits and windows as explicit config, following the existing `rojan.security.otp.*` pattern in `application.yml` (e.g. `rojan.security.login.*`, `rojan.security.register.*`, `rojan.security.refresh.*` — request-limit-per-IP and per-account/phone/email, short + long window, same two-tier shape OTP already uses). Do not hardcode limits in code — every existing rate-limit knob in this codebase is env-var-overridable; match that convention.
3. Key the rate limiter by the most specific real signal available per endpoint: login by `email` (post-lookup) **and** by caller IP (pre-lookup, so an attacker can't just rotate target emails to dodge a per-account limit); register by IP; refresh by the token's `jti` claim (already present on every token) or by IP if `jti`-keying is judged too granular for a first pass.
4. Wire the new exceptions (`LoginRateLimitExceededException`-shaped, following the existing `OtpRateLimitExceededException`/`OtpVerifyRateLimitExceededException` naming and 429 status precedent) into `GlobalExceptionHandler`'s existing `handleOtpRateLimitExceeded`-style handler group — add the new exception types to that same `@ExceptionHandler` list rather than inventing a new response shape.
5. Register wiring point: `UseCaseConfig.kt` (`api/src/main/kotlin/ai/rojan/backend/api/config/`), where `RequestOtpUseCase`/`VerifyOtpUseCase` are already constructed with their `RateLimiterPort`.

**Done when:** all three endpoints return 429 with the existing `ApiError` shape (`errorCode`, `traceId`, etc.) after exceeding their configured limit, verified by the same style of unit test `RequestOtpUseCaseTest`/`OtpTestFixtures` already use for the OTP flow (fake/in-memory `RateLimiterPort`, assert the exception fires exactly at the limit boundary).

**Depends on:** nothing — Redis is already provisioned and wired; this is additive to already-tested, already-running infrastructure.

---

### 1.2 CORS policy — 🟠 HIGH (Required Action)

**Audit finding:** no `CorsConfigurationSource` bean, no `.cors()` call, no `@CrossOrigin` anywhere. Nginx already reverse-proxies the website at `rojanai.ir`/`www.rojanai.ir`, a different domain than the API's own — this is a live gap, not hypothetical, the moment a browser-origin client calls the API directly.

**Plan:**
1. **Decision first, code second:** confirm with whoever owns ROJAN_Web whether the website ever calls the API directly from browser JS (client-side fetch) or exclusively server-side (Next.js SSR/API routes proxying through the server, never exposing the API origin to the browser). This single decision determines whether CORS is even needed, and if so, which origins to allow. Do not implement a CORS bean speculatively before this is confirmed — the security backlog itself flags this as a two-sided decision, not backend-only work.
2. If browser-origin calls are confirmed needed: add a `CorsConfigurationSource` bean in `SecurityConfig.kt` (same file `PUBLIC_ENDPOINTS` already lives in), scoped to explicit allowed origins per environment (`https://rojanai.ir`, `https://www.rojanai.ir` for prod; a configurable dev origin, likely `http://localhost:3000`, via an env var following the existing `${VAR:default}` convention) — never a wildcard `*` origin on an API that issues bearer tokens.
3. Explicitly decide (and document in the same PR) whether credentials mode (cookies) is ever needed — per the security backlog, this is coupled to item 1.3 below (HttpOnly cookie session is a separate, larger, not-yet-scoped backlog item) and should stay decoupled unless that work is also being picked up now.

**Done when:** a CORS integration test (new, following this codebase's existing controller-test conventions) confirms an allowed origin's preflight succeeds and a non-allowlisted origin's preflight is rejected.

**Depends on:** a same-origin-vs-cross-origin hosting decision from whoever owns ROJAN_Web's deployment topology. This is the one item in this plan with an external dependency outside `ROJAN_Backend`/`ROJAN_Desktop`.

---

### 1.3 Refresh-token rotation/revocation — 🟠 HIGH (Required Action)

**Audit finding:** refresh tokens are stateless JWTs with no server-side store — no revocation on logout/compromise, no reuse detection. Tracked in `ROJAN_Security_Backlog_v1.md` §2.

**Plan:**
1. Add a Redis-backed token store recording issued refresh-token `jti` values per user (Redis is already provisioned; this is the same instance OTP rate limiting already uses, via a distinct key prefix, e.g. `refresh:jti:*`, mirroring `RedisRateLimiter`'s own `ratelimit:*` prefix convention).
2. `RefreshTokenUseCase`: on successful refresh, mark the presented `jti` consumed and issue a new one (rotation-on-use) — following the exact same "return the same `AuthenticationResult` shape every auth use case already returns" convention `VerifyOtpUseCase`'s own doc comment describes.
3. Reuse detection: if a refresh token whose `jti` is already marked consumed is presented again, treat it as a compromise signal — revoke the entire token family (every `jti` issued in that chain), not just the one token. This requires linking rotated tokens into a family (e.g. a `familyId` claim or a Redis-side chain), a real design decision to make explicitly before implementing, not an incidental detail.
4. Add an explicit logout endpoint (if one doesn't already exist — not confirmed in the prior audit, verify first) that revokes the current refresh token's family immediately, giving "log out everywhere" real teeth for the first time.
5. TTL every Redis entry at the refresh token's own expiry (`JWT_REFRESH_TTL_DAYS`) so the store never grows unbounded.

**Done when:** a used-once refresh token cannot be replayed (integration test: refresh twice with the same token, second call fails); a revoked family's every token is rejected even before natural expiry.

**Depends on:** none technically, but this is the largest single item in this plan — budget it as its own focused work item, not a quick add-on alongside 1.1/1.2.

---

### 1.4 Security-event audit logging — 🟡 MEDIUM (Required Action)

**Audit finding:** standard application/error logging exists, but no security-event-specific trail (login success/failure, ownership-denied 403s, password changes). Tracked in `ROJAN_Security_Backlog_v1.md` §5.

**Plan:**
1. Scope decision first (per the backlog's own note): agree the event list before building anything. Minimum recommended set given this is a multi-tenant financial dashboard: login success, login failure (with reason class, not raw credential), OTP request/verify success+failure, `SalonAccessDeniedException`/`CustomerAccessDeniedException`/`BookingAccessDeniedException` occurrences (403s), refresh-token reuse-detected events (ties directly into 1.3).
2. Storage target decision: a structured log sink (e.g. a dedicated `security-audit` logger with its own Logback appender, written to a separate rotated file alongside the existing `rojan-backend.log`) is the lower-effort option and fits the existing logging infrastructure (`application-prod.yml`'s logback rolling policy) without a new dependency; a dedicated audit table is more queryable but is new schema + a new migration. Recommend the log-sink approach for a first pass, revisit if/when compliance requirements demand a queryable audit table.
3. Emit these events from the use-case layer (where the security decision is actually made — e.g. inside `GlobalExceptionHandler`'s `handleAccessDenied`, and inside `AuthenticateUserUseCase`/`VerifyOtpUseCase` at their existing success/throw points), not scraped after the fact from generic error logs.

**Done when:** every event in the agreed scope produces one structured log line with enough context (user id or attempted identifier, IP, timestamp, outcome) to answer "who did what, when" without cross-referencing multiple log sources.

**Depends on:** an explicit scope decision (product/security, not engineering) before implementation starts, per the backlog's own instruction.

---

## 2. Owner App Production Readiness

### 2.1 "Create Salon" flow — 🔴 BLOCKER

**Audit finding:** `POST /api/v1/salons` (`CreateSalonUseCase`/`SalonController.kt`) is real, tested, and owner-authorized. Nothing in `Rojan.Desktop.Presentation` calls it. Every other feature already assumes `ISalonContextService.GetSalonIdAsync()` returns non-null.

**Plan:**
1. **Domain/Application layer:** add `ICreateSalonRequest`-shaped input (name, description, phone, email, address — matching `CreateSalonRequest` on the backend field-for-field, same convention every other `Backend*Repository` follows) and a `CreateSalonAsync` method on whatever salon-facing Application service currently exists (check for an existing `ISalonQueryService`/`ISalonCommandService` first — if none exists yet, this is the first write operation for the Salon vertical slice on the Owner App side and needs one, following the `IServiceQueryService`/`IServiceCommandService` split convention every other module already uses).
2. **Infrastructure layer:** the repository implementation calls `POST /api/v1/salons`, mapped via a new `CreateSalonRequest`/`SalonResponse` pair in `Api.Contracts` (mirroring `CreateBookingForCustomerRequest`'s recent precedent — added this session for the exact same "new backend-write, new wire contract" reason).
3. **Presentation layer:** a first-run/onboarding screen, reachable specifically from the state where `SelectedSpecialist`/salon-scoped data loads currently fail with "does not manage any salon yet" — likely gated at Shell startup (check whatever currently handles the post-login navigation decision) so a salon-less owner lands here automatically rather than hitting scattered error states across every page.
4. **DI wiring:** register the new service/repository following the exact `Backend*Repository` DI pattern every other vertical slice in `Infrastructure.DependencyInjection.ServiceCollectionExtensions` already uses.

**Done when:** a fresh Owner App install, logged in as a brand-new (salon-less) owner, can create a salon through the UI and immediately proceed to every other already-working feature (Customers, Services, Specialists, Calendar, Bookings, Dashboard) without hitting the "does not manage any salon yet" error anywhere.

**Depends on:** none — the backend side is complete and already tested; this is Owner App-only work.

---

### 2.2 Distributable Owner App package — 🔴 BLOCKER

**Audit finding:** no installer project anywhere (no WiX/MSIX/Squirrel). The one committed publish profile points at a personal local path with no self-contained/runtime-identifier/signing configuration.

**Plan:**
1. **Decision first:** pick a packaging technology. Given this is a WPF `.NET 8` desktop app targeting Windows only, MSIX (native Windows App SDK packaging, built-in auto-update support via App Installer, no third-party tooling) or a WiX-based MSI (more control, more setup) are the two realistic choices — recommend MSIX unless there's a specific reason (e.g. needing to run without Windows' app-package restrictions, or targeting a Windows edition/environment MSIX doesn't support) to prefer WiX.
2. Remove or replace the stray `FolderProfile.pubxml` files pointing at personal local paths (`D:\ایکون ورودی` and similar) — these should not exist in version control as committed publish targets; replace with a proper CI-driven publish profile (self-contained, single-file where practical, explicit `RuntimeIdentifier: win-x64`) that produces a real release artifact, not a developer's local output folder.
3. Code signing: obtain a code-signing certificate before first public distribution — an unsigned Windows installer triggers SmartScreen warnings that will materially hurt first-run trust for real salon owners. This is a procurement/ops item, not an engineering one, but it gates the packaging work above from being genuinely "done."
4. Decide an update mechanism alongside packaging (MSIX's built-in App Installer update channel is the natural fit if MSIX is chosen) — don't ship v1 without a plan for how v1.1 reaches already-installed owners; this doesn't need to be automatic on day one, but the mechanism should exist rather than being "reinstall manually."

**Done when:** a signed installer artifact exists that a non-technical salon owner can download and run to get a working Owner App pointed at production, with a documented (even if manual, for a first pass) path to receive updates.

**Depends on:** a code-signing certificate (procurement, not engineering) and the packaging-technology decision above.

---

### 2.3 Default Production API URL — 🟡 MEDIUM (Required Action)

**Audit finding:** `ApiEnvironmentService.ProductionUrl` has no default; `ApiEnvironment.Production` resolves to `null` and throws `ApiConnectivityException` until an operator manually configures it per install.

**Plan:**
1. Bake `https://api.rojanai.ir` (already documented as the intended value via `IRojanBrandConfiguration.ApiBaseUrl`, currently only consumed by the Support page) into `ApiEnvironmentService` as the actual default `ProductionUrl` when `ApiEnvironment.Production` is selected and no persisted/env-var override exists — a one-line default-value change, not a redesign of the existing Development-first-launch-default safety behavior (a fresh install should still default to Development until a release build explicitly ships defaulting to Production — see next point).
2. Decide whether release builds should default `SelectedEnvironment` itself to `Production` (not just supply a default URL for it) — today "Development is always the first-launch default... so a fresh install never accidentally points at a misconfigured or unset Production address" is a deliberate safety choice; once a real Production URL is always available, that reasoning changes for release-channel builds specifically (a real owner should never have to find a Settings screen to point the app at production). Recommend a build-time or install-channel flag distinguishing internal/dev builds (Development default, current behavior preserved) from release builds (Production default) rather than changing the shared default for everyone.

**Done when:** a release-channel install works against production immediately with zero manual configuration, while internal/dev builds retain today's safe Development-first behavior.

**Depends on:** 2.2 (this only matters once there's a real release build/channel to attach the behavior to).

---

### 2.4 Remote crash/error reporting — 🟡 MEDIUM (Required Action)

**Audit finding:** crash handling is solid at the code level (`App.xaml.cs`'s three unhandled-exception handlers, all logged via `ILogger`), but logging is local-only — no remote visibility into a real user's crash.

**Plan:**
1. Add a crash-reporting SDK to the Owner App (App Center or Sentry are the two realistic .NET/WPF-supported options; Sentry has the advantage of already needing zero backend-side changes and unifying with a future backend-side Sentry adoption if 3.3 below picks the same vendor).
2. Wire it into the three existing handlers in `App.xaml.cs` (`OnDispatcherUnhandledException`, `OnUnhandledException`, `OnUnobservedTaskException`) alongside the existing `LogException`/`ILogger` call — additive, not a replacement for local logging.
3. Explicit user-consent/privacy decision before shipping: what gets sent (stack traces, yes; any customer/booking data incidentally captured in an exception message, no) — needs a scrub/redaction pass on exception messages before they leave the machine, since this app handles real customer PII (names, phone numbers).

**Done when:** a real exception thrown in a release build appears in the chosen dashboard within minutes, with no PII in the captured payload.

**Depends on:** vendor choice, ideally shared with 3.3 (backend error tracking) for one unified dashboard rather than two.

---

## 3. Infrastructure Readiness

### 3.1 Confirm `backup.sh` is actually scheduled — 🟡 MEDIUM (Required Action)

**Audit finding:** the script is correct and ready; nothing in the repo proves it is running anywhere.

**Plan:**
1. Add the documented cron entry (`0 3 * * * /opt/rojan/backend/scripts/backup.sh >> /opt/rojan/logs/backup.log 2>&1`, already given as an example in the script's own header) to the actual target VPS's crontab — an ops action, not a code change.
2. Add a lightweight verification step to the deployment checklist (see §6) confirming the crontab entry exists and a recent backup file is present under `/opt/rojan/backups` — don't just trust that the one-time setup happened and was never accidentally lost in a VPS rebuild.
3. Consider (not required for launch) a dead-man's-switch style alert (e.g. a scheduled check that the newest backup file is <25h old, alerting if not) — flagged as a nice-to-have, not scoped further here.

**Done when:** the crontab entry is confirmed present on the real VPS and at least one automated daily backup has been observed to succeed.

**Depends on:** VPS access — an ops task, executable independently of any other item in this plan.

---

### 3.2 Nginx-layer rate limiting (defense in depth) — 🟡 MEDIUM (Required Action, complements 1.1)

**Audit finding:** confirmed absent at the Nginx layer too (no `limit_req`/`limit_conn` in `docker/nginx/conf.d/rojan.conf`).

**Plan:**
1. Add `limit_req_zone`/`limit_req` directives to `docker/nginx/nginx.conf`/`conf.d/rojan.conf` as a coarse, infrastructure-level second layer behind the application-level rate limiting in 1.1 — not a replacement for it (app-level limiting can key on account/phone identity; Nginx-level limiting only sees IP, but catches attacks before they even reach the JVM).
2. Scope initially to the auth-adjacent paths (`/api/v1/auth/*`) at a generous limit above what a real user would ever hit, purely to blunt volumetric abuse — the precise numbers should be tuned once 1.1's application-level limits are live and their thresholds are known, so the two layers agree on what "abuse" looks like rather than one triggering false positives the other wouldn't.

**Done when:** a burst of requests against `/api/v1/auth/login` from one IP is throttled at the Nginx layer even before reaching the app-level limiter in 1.1.

**Depends on:** 1.1 landing first, so the two layers' thresholds can be set consistently rather than guessed independently.

---

### 3.3 Production monitoring: error tracking + performance monitoring — 🟡 MEDIUM (Required Action)

**Audit finding:** no external error tracking (Sentry/App Center/Application Insights/Bugsnag) anywhere; no APM/Micrometer/Prometheus beyond bare `/actuator/health,info`.

**Plan:**
1. **Error tracking:** add a Sentry (or equivalent) integration to the Spring Boot app — a dependency + a DSN env var + wiring into `GlobalExceptionHandler`'s existing last-resort `handleUnexpected` handler (which already generates a `traceId` per unhandled exception — the natural place to also forward the exception to the tracking service, correlated by that same `traceId`).
2. **Performance monitoring:** add Micrometer with a Prometheus registry (`management.metrics.export.prometheus.enabled: true`, already the standard Spring Boot Actuator extension point) — exposes `/actuator/prometheus` for scraping. Whether to stand up a full Prometheus+Grafana stack immediately or just expose the endpoint for later use is a scoping decision; recommend exposing the endpoint now (cheap, additive) even if the scraping/dashboarding infrastructure follows later.
3. Coordinate vendor choice with 2.4 (Owner App crash reporting) — a single Sentry project spanning both backend and Owner App gives one unified view of a production incident that touches both sides (e.g. a booking-creation failure visible as both a backend 500 and a client-side `ApiException`).

**Done when:** an unhandled backend exception and an unhandled Owner App exception both appear in the same dashboard, correlated where possible (e.g. by `traceId` if the Owner App surfaces it from a failed `ApiException`).

**Depends on:** vendor decision (shared with 2.4).

---

### 3.4 Specialist–Service relationship — 🟠 HIGH (Known Open Item)

**Audit finding:** `Service` and `Specialist` are fully independent on the backend — no assignment concept exists at all. `available-slots`/booking creation accept any specialist+service pair with no validation.

**Plan:**
1. **Product decision first:** does launch require this constraint, or is it acceptable that reception staff self-police which specialist performs which service (today's de facto behavior, since only reception/owner create bookings)? The audit classified this HIGH specifically because the risk multiplies the moment self-service/website booking removes the human check — if Website Booking (3.5) is not part of this launch's initial scope, this item's urgency drops with it.
2. If picked up: this is backend-domain work — a new `specialist_services` join concept (new Flyway migration `V7__*`, following the existing sequential-migration convention), a new authoring endpoint (owner-only, same pattern as `weekly-availability`/`overrides`), and `GetAvailableSlotsUseCase` gaining a check that the requested specialist is actually assignable to the requested service before computing slots (404/409, matching the existing error-shape convention for "this combination is invalid").
3. Owner App side: `BackendServiceRepository.AssignSpecialistAsync`/`UnassignSpecialistAsync` currently throw `NotSupportedException` specifically because there is nothing to call — once the backend endpoint exists, these become real implementations instead of throws, plus UI to manage the relationship (likely on the Service or Specialist detail page — a design decision, not specified here).

**Done when:** attempting to book a specialist for a service they're not assigned to is rejected before a slot is ever shown as available, both via the API directly and through the Owner App's own Wizard.

**Depends on:** the product decision in step 1 — do not start backend schema work before that's settled, since it may turn out to be genuinely post-launch scope.

---

### 3.5 Website Booking verification — 🟠 HIGH (Known Open Item, external dependency)

**Audit finding:** not verified in the prior audit — `ROJAN_Web` is a separate repository outside the two named in that audit's scope. Infra-level readiness is real (Nginx already reverse-proxies the website domain), but the booking flow's actual completion state against the real backend was not traced.

**Plan:**
1. This is a coordination item, not an implementation item owned by this plan: request a status check from whoever owns `ROJAN_Web` — specifically, does the website's booking flow call the real `POST /api/v1/bookings` (self-service) endpoint end-to-end today, or is it still using placeholder/mock data?
2. Once that status is known, decide whether "Customer books via website" is in scope for this specific launch or is itself a fast-follow — the E2E validation plan in §4 below treats this as a conditional step specifically because its readiness is currently unknown, not assumed either way.
3. If confirmed incomplete, this item's own hardening plan should live in `ROJAN_Web`'s own tracking, not be absorbed into this document — this plan's job is to flag the dependency, not to re-scope another team's repository.

**Done when:** a definitive READY/NOT READY answer exists for Website Booking, sourced from `ROJAN_Web`'s own team/repository, not inferred from this repo's infra config alone.

**Depends on:** access to and cooperation from whoever owns `ROJAN_Web`.

---

## 4. E2E Validation Plan

Run only after the relevant Phase 1/2 items above are complete — this section defines what "prove it actually works" looks like, not new scope.

### 4.1 Owner flow (full cycle, fresh state)

| Step | Precondition from this plan | Validation |
|---|---|---|
| Register | none (already working) | Verify OTP against a genuinely new phone number creates a new `User` (`role = CUSTOMER`, per `VerifyOtpUseCase`'s existing documented default) |
| Login OTP | none (already working) | Re-authenticate with the same phone, confirm token issuance |
| **Create Salon** | **2.1 complete** | New Owner App onboarding screen creates a real salon via `POST /api/v1/salons`; confirm `GET /api/v1/salons/mine` now returns it |
| Create Services | none (already working) | Create at least 2 services with distinct durations, confirm they appear in the Wizard's Service step |
| Create Specialist | none (already working); **3.4 if picked up** | Create a specialist; if 3.4 lands, also assign it to a subset of services and confirm the Wizard only offers valid combinations |
| Create Customer | none (already working) | Create both a walk-in (unlinked) and confirm the CRM record behaves per the documented walk-in limitation (§7 of the audit) |
| Create Booking | none (already working, confirmed this session) | Run the full Wizard: Customer → Service → Specialist → Availability → Time Slot → Booking, confirm a real `Booking` row exists on the backend |
| Dashboard Update | none (already working) | Confirm the just-created booking is reflected in Dashboard revenue/booking counts on next load (no caching, per `GetDashboardInsightsUseCase`) |

### 4.2 Customer flow

| Step | Precondition from this plan | Validation |
|---|---|---|
| Booking (entry point) | **3.5 status known** | If Website Booking is confirmed ready: create a booking as an OTP-authenticated customer via the website. If not ready or out of scope for this launch: validate via the self-service API directly (`POST /api/v1/bookings`) as a substitute proof that the pipe works, with website-specific validation deferred to `ROJAN_Web`'s own plan |
| → Backend | none (already working) | Confirm the booking is attributed to the correct customer/salon |
| → Owner Calendar | none (already working, confirmed this session) | Confirm the booking appears in the Owner App's Calendar/Bookings view and correctly reduces that specialist's `available-slots` for the booked window |

### 4.3 Security validation (post-1.1/1.2/1.3)

- Exceed the new login rate limit; confirm 429 with correlating `traceId`, confirm the limit resets after the configured window.
- Exceed the new register/refresh rate limits similarly.
- If CORS (1.2) landed: confirm an allowed origin succeeds and a disallowed origin's preflight fails, from an actual browser (not just a curl `Origin` header spoof — browsers enforce CORS client-side, so a real browser test is the only meaningful proof).
- If refresh rotation (1.3) landed: confirm a replayed refresh token is rejected and triggers family revocation (every sibling token also becomes invalid).

### 4.4 Owner App release validation (post-2.1/2.2/2.3)

- Install the packaged artifact (2.2) on a clean Windows machine with no prior ROJAN install.
- Confirm it launches pointed at production (2.3) with zero manual Settings configuration for a release-channel build.
- Complete the full 4.1 Owner flow through that installed instance, not a dev-build/debugger-attached run.

---

## 5. Testing Strategy

### 5.1 Unit tests (backend, Kotlin/JUnit)

- Every new use-case change in §1 (rate limiting, refresh rotation, audit logging) gets unit tests following the exact existing pattern `RequestOtpUseCaseTest`/`VerifyOtpUseCaseTest`/`OtpTestFixtures` already establish — fake `RateLimiterPort`/repository implementations, boundary-condition assertions (exactly-at-limit passes, one-over fails), not integration tests for the pure-logic cases.
- `CreateSalonUseCase` already has backend-side tests (confirmed existing, unchanged by this plan) — no new backend testing needed for 2.1, only Owner App-side.

### 5.2 Unit/ViewModel tests (Owner App, C#/xUnit)

- New Salon Application-layer service (2.1): mirror the existing `ICustomerCommandService`/`ICustomerQueryService` test-double conventions (`StubCustomerRepository`-shaped fakes) already used throughout `Rojan.Desktop.Application.Tests`.
- New onboarding ViewModel (2.1): follow this session's own just-established `CalendarPageViewModelTests` pattern (stub the new query/command service, assert `State`/error transitions the same way every other page ViewModel test in this codebase already does).
- `ApiEnvironmentService` default-URL change (2.3): extend its existing test file with a case asserting the new default resolves correctly for a release-channel build and is absent/unaffected for a dev-channel build.

### 5.3 Integration tests (backend)

- Rate limiting (1.1): a real Spring Boot integration test hitting `/auth/login` repeatedly against a real (test-container or embedded) Redis, confirming the 429 boundary — the existing backend test suite already has this pattern available (`BookingConflictConcurrencyIntegrationTest` was cited in the prior audit as an example of this codebase's integration-test capability).
- CORS (1.2): an integration test issuing a preflight `OPTIONS` request with an `Origin` header, asserting the `Access-Control-Allow-Origin` response header matches only for allowlisted origins.
- Refresh rotation (1.3): integration test proving replay-after-rotation fails and family revocation actually invalidates sibling tokens, not just the replayed one.
- Specialist-Service (3.4, if picked up): extend `GetAvailableSlotsUseCaseTest` with a case asserting an unassigned specialist+service pair yields the new rejection, not a normal empty-or-populated slot list.

### 5.4 Manual/exploratory testing

- Full §4 E2E validation plan, run by a human against a real (or realistic staging) environment — not a substitute for automated coverage above, but automated tests alone won't catch UX-level onboarding friction in the new Create Salon flow, which is exactly the kind of thing that matters most for a real first-time owner.
- Installer-specific manual testing (2.2): a clean-machine install/uninstall/reinstall cycle, SmartScreen behavior check (should be clean once code-signed), update-path check if an update mechanism shipped.

### 5.5 Security testing

- Rate-limit boundary testing is covered in 5.3; additionally, a basic external-facing scan (even an informal one — e.g. `nmap`/`testssl.sh` against the deployed domain) to confirm the TLS configuration (`ssl_protocols TLSv1.2 TLSv1.3`, `ssl_ciphers HIGH:!aNULL:!MD5` from `rojan.conf`) is actually in effect as deployed, not just as configured in the repo.
- A focused review of whatever PII ends up in the new audit-logging (1.4) and crash-reporting (2.4/3.3) streams before either goes live — both are new places customer/owner data could leak if not scrubbed, and both are new in this plan, so neither has been reviewed for this yet.

### 5.6 Regression testing

- The full existing test suite (2,199 tests across the Owner App's six projects, per this session's own Calendar/Availability Phase 3 work, plus the backend's own suite) must stay green throughout — every item in this plan is additive to already-tested surfaces (new use-case dependencies, new endpoints, new DI registrations), not a rewrite of anything currently passing. Any item that requires changing an existing passing test's expected behavior (rather than adding new coverage) should be treated as a signal to re-check scope before proceeding.

---

## 6. Deployment Checklist

Sequenced to match the phases above — items within a phase can proceed in parallel unless a dependency is noted.

### 6.1 Pre-deployment (before any Phase 1 code ships)

- [ ] Confirm `.env` on the target VPS has every required variable set (`DB_*`, `JWT_SECRET`, `SMS_*`, `DOMAIN_NAME`, `LETSENCRYPT_EMAIL`) — `.env.example` is the checklist source.
- [ ] Confirm `scripts/backup.sh` is cron-scheduled (3.1) and has produced at least one successful backup **before** any schema-changing deploy (1.3's Redis token store isn't a schema change, but 3.4's specialist-service work, if picked up, is).
- [ ] Take a manual `backup.sh` snapshot immediately before the first Phase 1 deploy regardless of the cron schedule's timing, as a point-in-time safety net.

### 6.2 Phase 1 (Blockers) rollout

- [ ] 1.1 (auth rate limiting) — backend deploy, no migration, no Owner App change required.
- [ ] 2.1 (Create Salon) — Owner App change; backend already supports it, no backend deploy required for this item specifically.
- [ ] 2.2 (installer) — packaging/ops work, not a backend or running-app deploy; gates when 2.1 actually reaches real users.
- [ ] Run §4.1/§4.4 E2E validation against staging (or a production-equivalent environment) before promoting to production.
- [ ] Use `scripts/deploy.sh` for the backend portion (already handles the build/health-check/record-previous-commit sequence); `scripts/rollback.sh` is the documented fallback if `/actuator/health` doesn't recover post-deploy.

### 6.3 Phase 2 (Required Actions) rollout

- [ ] 1.2 (CORS) — only after the same-origin-vs-cross-origin decision is made; coordinate timing with `ROJAN_Web` if their calls depend on it.
- [ ] 1.3 (refresh rotation) — largest single item; deploy in a low-traffic window given it changes every subsequent login's token lifecycle behavior, and confirm 4.3's replay/revocation validation before wide rollout.
- [ ] 1.4 (audit logging), 3.3 (monitoring) — low-risk, additive; can deploy independently of everything else in this phase.
- [ ] 2.3 (default Production URL), 2.4 (crash reporting) — Owner App-side, ship alongside the next installer build (2.2) rather than as a hotfix to already-installed clients, given there's no auto-update mechanism yet.
- [ ] 3.1 (backup cron), 3.2 (Nginx rate limiting) — pure ops/infra, no app code deploy.

### 6.4 Post-deployment verification (every phase)

- [ ] `/actuator/health` reports `UP` (Postgres, Redis, disk — Kafka correctly excluded per existing config).
- [ ] `docker compose -f docker-compose.prod.yml ps` shows every service `healthy`, not just `running`.
- [ ] Tail `/opt/rojan/logs/rojan-backend.log` for unexpected errors in the first hour post-deploy.
- [ ] Re-run the specific §4 validation steps relevant to whatever just shipped (not the full E2E suite on every deploy — the full suite is for Phase-boundary promotions, targeted checks for individual item deploys).
- [ ] Confirm the new Sentry/monitoring dashboard (3.3), once live, shows no unexpected error-rate spike correlated with the deploy timestamp.

### 6.5 Rollback readiness

- [ ] `scripts/rollback.sh` is confirmed working (has a `last-deployed-commit.txt.previous` to roll back to) before each deploy that carries real risk — its own script already refuses to proceed if that file doesn't exist yet, which itself is worth a dry-run check the first time this plan's changes go out.
- [ ] For 1.3 (refresh rotation) specifically: confirm the rollback path doesn't strand already-rotated sessions in a broken state — since this changes token semantics, a code-only rollback without a corresponding Redis-state consideration could leave some in-flight sessions unable to refresh. Explicitly test this scenario in staging before the real deploy, not just assume `rollback.sh`'s generic code-revert covers it.

---

## Sequencing Summary

```
Phase 1 (Blockers, parallel-safe):
  1.1 Auth rate limiting  ──┐
  2.1 Create Salon flow   ──┼── each independently deployable
  2.2 Installer packaging ──┘   (2.2 gates real-user reach of 2.1)

Phase 2 (Required Actions):
  1.2 CORS           (needs: ROJAN_Web hosting decision)
  1.3 Refresh rotation (independent, but largest — sequence its own deploy window)
  1.4 Audit logging   (needs: scope decision)
  2.3 Default prod URL (needs: 2.2 shipped first)
  2.4 Crash reporting  (needs: vendor decision, ideally shared with 3.3)
  3.1 Backup cron      (independent, ops-only)
  3.2 Nginx rate limit (needs: 1.1 shipped first, for threshold consistency)
  3.3 Monitoring       (needs: vendor decision, ideally shared with 2.4)
  3.4 Specialist-Service (needs: product decision on launch scope)
  3.5 Website Booking  (needs: ROJAN_Web team status check — coordination, not implementation)

Phase 3 (Post-launch, per audit):
  AI Recommendation ML upgrade — explicitly out of scope for this plan
```

**No code was written for this plan.**
