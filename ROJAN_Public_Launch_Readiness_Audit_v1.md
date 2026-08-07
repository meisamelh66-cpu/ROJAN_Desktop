# ROJAN AI — Public Launch Readiness Audit v1

**Priority:** P0
**Scope:** Audit only. No code, refactoring, architecture, or deployment changes made in this pass.
**Repositories:** `ROJAN_Backend` @ `6943986`, `ROJAN_Desktop` @ `8526235`.
**Method:** Direct inspection of production config (`application.yml`/`application-prod.yml`, `docker-compose.prod.yml`, `docker/nginx/`, `.env.example`, `scripts/*.sh`), security-relevant source (`SecurityConfig`, `JwtAuthenticationFilter`, `GlobalExceptionHandler`, rate-limiting/OTP use cases), Flyway migrations, and the Owner App's API/session/publish configuration — cross-checked against this repo's own `ROJAN_Security_Backlog_v1.md`, `ROJAN_Security_Audit_v1.md`, and this session's own prior verification work (Customer/Dashboard API, Calendar/Availability Phase 3).

---

## Final Summary

**Production Ready:**
**NO**

**Security:**
**FAIL**

**Backend:**
**PASS**

**Owner App:**
**FAIL**

**Database:**
**PASS**

**E2E:**
**FAIL**

### Blockers 🔴

1. **No "Create Salon" flow anywhere in the Owner App.** `POST /api/v1/salons` exists, is owner-authorized, and works — but nothing in `Rojan.Desktop.Presentation` calls it. A brand-new owner who registers and logs in has no in-app path to create the salon every other feature (Customers, Services, Specialists, Calendar, Dashboard, Bookings) silently assumes already exists (`ISalonContextService.GetSalonIdAsync` returning `null` → `"The signed-in owner does not manage any salon yet"` is the error every `Backend*Repository` throws today). This breaks the very first cycle of the required E2E flow.
2. **No distributable Owner App package.** There is no installer project (no WiX/MSIX/Squirrel) anywhere in the repo. The one committed publish profile (`Rojan.Desktop.Shell/Properties/PublishProfiles/FolderProfile.pubxml`) points its `PublishDir` at a personal local path (`D:\ایکون ورودی`) with no `SelfContained`/`RuntimeIdentifier`/code-signing configuration — this is a developer's local artifact, not a release pipeline. There is currently nothing to hand a real salon owner.
3. **No rate limiting on `/auth/login`, `/auth/register`, or `/auth/refresh`.** Confirmed by direct inspection: `AuthenticateUserUseCase` has no `RateLimiterPort` dependency at all. `RedisRateLimiter`/`RateLimiterPort` are wired **only** into `RequestOtpUseCase`/`VerifyOtpUseCase`. Nginx has no `limit_req`/`limit_conn` either. This is a real, unmitigated brute-force/credential-stuffing exposure the moment the API is reachable from the public internet — already independently identified in this repo's own `ROJAN_Security_Backlog_v1.md` §3, confirmed still true today.

### Required Actions

- Build the Owner App's "Create Salon" screen/flow against the already-existing, already-tested `POST /api/v1/salons` endpoint.
- Package the Owner App for real distribution (installer + versioning + a documented update path).
- Add rate limiting to `/auth/login`, `/auth/register`, `/auth/refresh` (Redis is already in the stack; `RateLimiterPort`/`RedisRateLimiter` already exist and just need wiring into these three use cases).
- Add a `CorsConfigurationSource` bean before any browser-origin client calls the API cross-origin — confirmed still entirely unconfigured (no `.cors()` call, no `@CrossOrigin`, no bean). Nginx already reverse-proxies the website at a *different* domain (`rojanai.ir`) than the API's own domain placeholder, so this is not hypothetical.
- Decide and implement refresh-token rotation/revocation (tracked, unresolved — `ROJAN_Security_Backlog_v1.md` §2; Redis is already provisioned and unused for this).
- Stand up external error tracking for production visibility — confirmed absent on both sides (no Sentry/App Center/Application Insights/Bugsnag reference anywhere in either repo); today, a production crash is invisible unless someone reads a log file on the VPS or a user manually sends Owner App logs.
- Confirm `scripts/backup.sh` is actually cron-scheduled on the target VPS — the script itself is correct and ready, but nothing in the repo proves it is *running* anywhere yet (it documents a cron example, it does not install one).
- Decide whether a specialist–service relationship constraint is needed before self-service/website booking ships — today `available-slots`/booking creation accept **any** specialist+service pair with no validation that the specialist actually performs that service (confirmed: `Domain.Services.Service` and `Domain.Specialists.Specialist` are fully independent on the backend; `AssignSpecialistAsync`/`UnassignSpecialistAsync` throw `NotSupportedException` on the Owner App side because there is no backend concept to call).
- Bake a default Production API URL into Owner App release builds, or explicitly document the manual first-run "set API environment in Settings" step — confirmed `ApiEnvironmentService.ProductionUrl` has no default; `ApiEnvironment.Production` resolves to `null` (and throws `ApiConnectivityException` on first call) until an operator manually sets it per install.
- Add security-event audit logging (login success/failure, ownership-denied 403s) — tracked, unresolved (`ROJAN_Security_Backlog_v1.md` §5).
- Independently confirm Website Booking's actual completion state before treating "Customer books → Backend → Owner Calendar" as launch-ready — out of this audit's two named repos, flagged as an open item below, not verified here.

---

## 1. Production Environment Readiness

**Backend deployment readiness: READY**

| Area | Finding |
|---|---|
| Configuration management | `application.yml` + `application-prod.yml` (Spring profile layering, `SPRING_PROFILES_ACTIVE=prod`), every value overridable via env var, sensible dev-safe defaults, prod-specific overrides (graceful shutdown, tightened actuator detail, Swagger disabled by default, log rotation) - a genuinely mature two-tier config, not a copy-pasted dev file. |
| Environment variables | Fully env-var driven (`DB_*`, `REDIS_*`, `JWT_*`, `SMS_*`, `SERVER_PORT`), documented in `.env.example` with generation instructions (`openssl rand -base64 48` for `JWT_SECRET`). `docker-compose.prod.yml` uses Compose's `${VAR:?message}` required-variable syntax for every secret - the stack refuses to start with a missing `DB_PASSWORD`/`JWT_SECRET`, not silently boots insecurely. |
| Database configuration | Postgres 16, HikariCP pool (`DB_POOL_SIZE`, default 10), `ddl-auto: validate` (schema drift fails startup loudly rather than silently auto-migrating), Flyway-managed migrations. Postgres is **not** port-published to the host in prod - only reachable on the internal Docker network. |
| Redis requirements | Configured (`REDIS_HOST`/`PORT`/`PASSWORD`, optional password supported), persisted via bind-mounted `appendonly yes` AOF. Currently consumed only by OTP request/verify rate limiting (`RedisRateLimiter`) - real infrastructure, under-utilized relative to what the security backlog already earmarks it for (refresh-token store, general rate limiting). |
| External services | SMS provider integration (`SMS_API_URL`/`SMS_API_KEY`/`SMS_SENDER`) has **no defaults on purpose** - same fail-loud pattern as JWT, confirmed by the config file's own comment: "startup must fail loudly... rather than silently booting with an SMS integration that can never actually deliver a code." |
| Secrets handling | No secret has a real default anywhere in version control; `.env` is gitignored; `.env.example` documents every variable without real values. TLS via Let's Encrypt/Certbot with an automated renewal loop and a self-signed bootstrap cert to solve the chicken-and-egg first-start problem. |

Also present and real (not aspirational docs): `Dockerfile`, `docker-compose.prod.yml` (Postgres + Redis + app + Nginx + Certbot, health-checked, `depends_on: condition: service_healthy` throughout), `scripts/deploy.sh`/`rollback.sh` (rollback records and restores the previous deployed commit, rebuilds, waits for health), `docker/nginx/conf.d/rojan.conf` (HTTP→HTTPS redirect, HSTS, `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, TLS 1.2/1.3 only). Kafka is deliberately **not** deployed in this milestone (producer bean exists, unused, health indicator explicitly disabled so it doesn't falsely fail `/actuator/health`) - documented as intentional, not an oversight.

**Caveat:** this is deployment *preparation*, confirmed by the compose file's own header comment - "nothing in this repo has been run against the target VPS yet." The mechanics are production-grade; whether they have actually been executed against the real server is outside what this repo can confirm.

---

## 2. Security Production Audit

**Security Status: FAIL**

### Authentication

| Area | Finding |
|---|---|
| JWT | HS256, fail-loud on missing/weak secret (no default), configurable TTLs (15 min access / 30 day refresh), issuer claim, explicit token-type check (`JwtAuthenticationFilter` rejects a refresh token used as an access token). Malformed/expired/deleted-account tokens are treated as anonymous, not thrown past the filter - correct, defensive behavior. |
| OTP flow | Real: TTL, max attempts, resend cooldown, **and** rate limiting at three levels (per-phone short window, per-phone long window, per-IP long window) plus a separate verify-attempt rate limit - genuinely thorough for the one flow it covers. |
| Token lifecycle | **Gap, tracked, unresolved:** refresh tokens are stateless JWTs with no server-side store - no revocation on logout/password-change/compromise, no reuse detection, a leaked refresh token is valid for its full 30-day life with nothing anyone can do about it short of rotating the signing secret (which invalidates every session, not just the compromised one). Documented in this repo's own `ROJAN_Security_Backlog_v1.md` §2. |

### Authorization

| Area | Finding |
|---|---|
| Owner permissions | Consistently ownership-based (`salon.ownerId == callerId`), not role-based - confirmed across Customer, Dashboard, Booking, Salon, Service, Specialist controllers in this and prior audit passes this session. |
| Salon tenant isolation | Enforced at the **use-case** layer, not just controllers, on every path checked (`GetCustomerBookingsUseCase`, `GetCustomerTimelineUseCase`, `GetDashboardInsightsUseCase`, etc.) - a valid resource id from a different salon 404s/403s rather than leaking cross-tenant, re-verified this session in `ROJAN_Team1_Integration_Verification_Report_v1.md`. |
| Resource access rules | `CurrentUserResolver` re-verifies the JWT subject against `UserRepository` on every request (a deleted account's still-valid token is rejected, not trusted from stale claims). |

### API security

| Area | Finding |
|---|---|
| CORS | **Confirmed absent.** No `CorsConfigurationSource` bean, no `.cors()` call in `SecurityConfig`, no `@CrossOrigin` anywhere in the codebase. Already flagged in `ROJAN_Security_Backlog_v1.md` §4 as "becomes a hard blocker the moment Web calls this API from a different origin" - Nginx's own config (`docker/nginx/conf.d/rojan.conf`) already reverse-proxies the website at `rojanai.ir`/`www.rojanai.ir`, a different server block/domain than the API's own `CHANGE_ME_DOMAIN` placeholder, so this is a live, not hypothetical, gap the moment a browser-based client calls the API directly. |
| Rate limiting | **Confirmed absent outside OTP.** `RateLimiterPort` has exactly two callers in the entire codebase (`RequestOtpUseCase`, `VerifyOtpUseCase`). `/auth/login`, `/auth/register`, `/auth/refresh`, and every `/api/v1/public/**` endpoint have none. Nginx has no `limit_req`/`limit_conn` either (checked directly - the backlog's own "not yet audited" note is now resolved: confirmed absent at that layer too). |
| Input validation | Real - Bean Validation (`@NotBlank`, `@Size`, `@Future`, etc.) on every request DTO checked this session, backed by `MethodArgumentNotValidException` → 400 with field-level messages. |
| Error handling | Strong. Single consistent `ApiError` shape across the whole API, a stable `errorCode` per exception type (not just HTTP status, since several types share one status), a `traceId` correlating a client-reported failure to the exact server log line, and a last-resort `Exception` handler that logs full detail server-side but returns only a generic message to the client (OWASP API9 - no stack trace/internal detail leakage). |

**Why FAIL, not PASS-with-notes:** authentication mechanics, tenant isolation, input validation, and error handling are all genuinely strong. But CORS and rate-limiting-outside-OTP are not partial gaps - they are **fully absent**, both already independently flagged in this repo's own security backlog as pre-launch-relevant, and directly exercisable the moment the API is public. A binary security gate for a public launch cannot pass with an unrated-limited login endpoint and no CORS policy.

---

## 3. Database Production Readiness

**Database Status: READY**

| Area | Finding |
|---|---|
| Migration status | Flyway-managed, 6 sequential migrations (`V1__init_schema` through `V6__customer_crm_schema`), no gaps, `baseline-on-migrate: true`. Forward-only by design (`rollback.sh`'s own comment: "Flyway migrations are forward-only; restore from a `backup.sh` archive instead" for a schema-breaking rollback) - a standard, correct convention, not an oversight. |
| Schema consistency | `ddl-auto: validate` means Hibernate refuses to start if the JPA entity model and the actual schema disagree - schema drift is a hard startup failure, not a silent runtime surprise. |
| Backup requirements | `scripts/backup.sh` exists and is correct: `pg_dump` + gzip, timestamped, 14-day retention pruning matching the log-rotation policy, documented cron example. **Not confirmed as actually scheduled** on the target VPS - the script is ready, its execution is not verifiable from this repo. |
| Data integrity risks | No schema-level red flags found in the migrations reviewed; `ddl-auto: validate` is itself an integrity guarantee against entity/schema drift. Full column-by-column constraint review was out of this audit's time budget - not a substitute for a dedicated schema review if one hasn't happened recently. |

---

## 4. Owner App Release Readiness

**Owner App: FAIL**

| Area | Finding |
|---|---|
| Production API configuration | Real mechanism exists (`ApiEnvironmentService`: Development/Production toggle, persisted to `LocalAppData`, `ROJAN_API_BASE_URL` env-var override, restart-required signaling on change). **Gap:** `ProductionUrl` has no baked-in default - a fresh install's `ApiEnvironment.Production` resolves to `null` until an operator manually configures it via Settings. `IRojanBrandConfiguration.ApiBaseUrl` documents `api.rojanai.ir` as the intended value but nothing wires it in as the actual default. |
| Release build capability | **Not ready.** `dotnet publish` produces a framework-dependent build; no installer project (WiX/MSIX/Squirrel - none found anywhere in the repo). The one committed publish profile points at a personal local path with no self-contained/runtime-identifier/signing configuration - it is not a release artifact, it is leftover local state. |
| Authentication flow | Solid - DPAPI-encrypted session storage (`DpapiSecureStorageService`, per-user-account, no plaintext token file), automatic single-retry refresh-on-401. |
| Backend connectivity | Solid - `HttpApiClient` wraps connectivity checking, retry, auth-header attachment, timeout, and exception mapping behind one interface every module already uses consistently. |
| Crash/error handling | Solid at the code level - `App.xaml.cs` installs handlers for UI-thread (`DispatcherUnhandledException`), background-thread (`AppDomain.UnhandledException`), and unobserved-task exceptions, all logged via `ILogger` and shown to the user via an error dialog rather than a silent crash. **Gap:** logging is local-only - no remote crash reporting, so the team has no visibility into a real user's crash unless that user manually sends logs. |

**Why FAIL:** the missing installer/distribution pipeline is disqualifying on its own - there is currently no artifact that could be handed to a real salon owner to install. Everything else in this section is genuinely close to ready.

---

## 5. End-to-End Business Flow

### Owner flow

```
Register  →  Login OTP  →  Create Salon  →  Create Services  →  Create Specialist  →  Create Customer  →  Create Booking  →  Dashboard Update
  (✅*)         (✅)          (❌ MISSING)         (✅)                (✅)                 (✅)               (✅)               (✅)
```

- **Register (✅, implicit):** `VerifyOtpUseCase` auto-registers a new `User` on first-ever OTP verification for a phone number - there is no separate register step for phone-only accounts by design, confirmed by that use case's own doc comment. No explicit "create account with name/email" screen exists, but this is a deliberate design choice (Mobile-First Authentication), not a gap.
- **Login OTP (✅):** `MobileOtpLoginViewModel` exists and is wired to the real backend flow.
- **Create Salon (❌ MISSING - the one required step this audit found with no implementation path):** `POST /api/v1/salons` is a real, tested, owner-authorized backend endpoint. **Nothing in the Owner App calls it.** Every subsequent feature already assumes a salon exists (`ISalonContextService.GetSalonIdAsync` returns `null` and every `Backend*Repository` throws `"The signed-in owner does not manage any salon yet"` otherwise). A brand-new owner is stuck immediately after their first successful login.
- **Create Services / Create Specialist / Create Customer (✅):** all confirmed backend-connected with real create/read flows in prior sessions' work (Customer CRM, Service Integration, Specialist Integration milestones).
- **Create Booking (✅):** confirmed this session - the Reception Booking Wizard now runs its full Customer → Service → Specialist → Availability → Time Slot → Booking flow against real backend data, including real booking creation (`POST /api/v1/salons/{salonId}/bookings`), completed as part of the Calendar/Availability Integration Phase 3 work immediately preceding this audit.
- **Dashboard Update (✅):** `GetDashboardInsightsUseCase` computes revenue/bookings/customer counts live from real booking data on every read - no caching, no staleness.

### Customer flow

```
Booking  →  Backend  →  Owner Calendar
 (⚠️)         (✅)           (✅)
```

- **Backend → Owner Calendar (✅):** confirmed this session - `BackendCalendarAvailabilityRepository` reads real backend availability, and any booking created through any path (reception or self-service) lands in the same `BookingRepository` the Owner's Calendar/Dashboard reads from. The pipe itself is proven.
- **Booking - the customer-facing entry point (⚠️, not verified in this audit):** the self-service `POST /api/v1/bookings` endpoint exists and is real, but the actual customer-facing surface that would call it at public-launch scale - the ROJAN_Web website - is explicitly named as an open item in §7 below and was not verified end-to-end as part of this audit (out of the two named repositories' scope).

**Missing steps to report:** **Create Salon** is the one architecturally-missing step in the Owner flow - everything else is either implemented or an intentional design choice. The Customer flow's backend↔Calendar pipe is sound; its entry point's completion state is unverified here.

---

## 6. Monitoring & Operations

| Area | Finding |
|---|---|
| Logging | **Present.** Backend: file + console (Logback, 50MB rolling, 14-day/1GB retention cap in prod, `INFO` for app code / `WARN` root), every error logged with a correlating `traceId`. Owner App: `ILogger`-based, global unhandled-exception handlers on every thread. |
| Health checks | **Present.** `/actuator/health`, tuned for prod (`show-details: never` - never leaks DB/disk detail regardless of auth), wired into Docker Compose's `depends_on: condition: service_healthy` and Nginx's own separate healthcheck target. Kafka's health indicator explicitly disabled so an intentionally-undeployed dependency doesn't falsely flip the whole app to DOWN. |
| Error tracking | **Absent.** No Sentry/App Center/Application Insights/Bugsnag or equivalent found anywhere in either repository - confirmed by direct search. Production errors are only visible by reading a log file on the VPS (backend) or waiting for a user to send logs (Owner App). |
| Performance monitoring | **Absent beyond bare `/actuator/health,info`.** No Micrometer/Prometheus registry, no APM agent, no dashboarding found. |
| Backup strategy | **Present, execution unconfirmed.** `scripts/backup.sh` is correct and ready (see §3); nothing in the repo proves it is actually scheduled to run on the target VPS. |

---

## 7. Known Open Items

| Item | Classification | Basis |
|---|---|---|
| **Walk-in Customer Booking limitation** | 🟡 MEDIUM | Confirmed still real and unchanged this session (`ROJAN_Team1_Integration_Verification_Report_v1.md`): a manually-created (unlinked) CRM customer correctly shows empty/zero booking history and lifetime value, by design, pending a future owner-initiated booking-for-unlinked-customer capability. Doesn't block launch - walk-ins can still be recorded and managed as CRM records today. |
| **Specialist-Service relationship** | 🟠 HIGH | Confirmed absent at the backend domain level - `Service` and `Specialist` are fully independent, no assignment concept exists at all (`AssignSpecialistAsync`/`UnassignSpecialistAsync` throw `NotSupportedException` on the Owner App side because there is nothing to call). Any specialist can currently be booked for any service with no validation. Not a crash risk today (staff can self-discipline in a reception-operated flow), but a real data-integrity gap the moment self-service/website booking removes that human check. |
| **Website Booking** | 🟠 HIGH | Not verified in this audit - `ROJAN_Web` is a separate, substantial repository outside this audit's two named repos. Infra-level readiness is real (Nginx already reverse-proxies `rojanai.ir`/`www.rojanai.ir` to a `website` service on the same Docker network as the backend), but this audit did not trace whether the website's booking flow actually calls the real backend end-to-end. Treat "Customer books via website" as unverified, not confirmed-ready, until checked directly against that repo. |
| **AI Recommendation readiness** | 🟢 POST LAUNCH | Real and already shipping - `RuleBasedRecommendationEngine` (15 passing backend tests) generates real, rule-based recommendations surfaced through `GetDashboardInsightsUseCase`/`BackendDashboardRepository` into the Owner App's Dashboard today. It is heuristic/rule-based, not ML-based, despite the "AI" branding - functionally launch-ready as-is; any move to a real ML-driven recommendation model is fairly classified as post-launch scope, not a gap blocking today's launch. |

---

## Closing Note

The backend's production deployment mechanics (config, secrets, database, TLS, health checks, rollback) are genuinely mature and among the strongest artifacts reviewed in this audit - this is not a project with weak infrastructure discipline. The gaps that make this **NOT** launch-ready are concentrated and specific: two Owner App distribution/onboarding gaps (no installer, no Create Salon flow) and one security gap that is fully absent rather than partially mitigated (rate limiting outside OTP, plus the closely-related CORS gap). None of the three blockers require architectural rework - they are additive work against interfaces and endpoints that already exist and are already tested.

**No code was written for this audit.**
