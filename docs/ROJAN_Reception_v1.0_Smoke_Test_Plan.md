# ROJAN Reception v1.0 — Production Smoke Test Plan (Final)

**Status:** Finalized during Release Preparation, still not executed — exact manual steps for a human tester with a real phone number to run against `https://api.rojanai.ir` before public release. No real OTP was sent as part of preparing this plan — see `docs/RojanReception_v1.0_Production_Checklist.md` §3 for the read-only/code-level verification that *was* done instead. Covers all 7 flows named for this release: OTP Login, JWT Session, Refresh Token, Salon Connection, Dashboard, Customer, Booking.

**Prerequisites:**
- The v1.0.0 installer (`artifacts/ROJAN Reception Setup.exe`), installed on a real Windows machine.
- A real phone number able to receive SMS, registered (or willing to auto-register — see Test 1) against the production backend.
- An existing salon owned by that account, or willingness to create one via the app's own "Create Salon" screen (Sidebar → سالن).
- Access to `ROJAN_Backend`'s production logs (or the `traceId` returned in any error response) if a step needs deeper investigation than the app's own UI shows.

---

## Test 1 — OTP Login

**Goal:** confirm a real phone number can request and verify an OTP end-to-end.

1. Launch ROJAN Reception. The login window opens (Mobile Number + OTP is the default panel).
2. Enter the real test phone number in the "شماره موبایل" field, in the format the field expects (E.164 or local — confirm against `Login_Mobile_PhoneLabel`'s placeholder/validation in the running app; do not guess a format here).
3. Click "ارسال کد" (Send Code).
4. **Expected:** an SMS arrives within ~30 seconds containing a numeric code. The screen transitions to the code-entry step (`MobileLogin.IsCodeSent = True`).
5. Enter the received code, submit.
6. **Expected:** login succeeds, the window closes, `MainWindow` opens.
7. **If it fails instead:** note the exact on-screen error text and, if visible, the `traceId` — cross-reference against `ROJAN_Backend`'s `RequestOtpUseCase`/`VerifyOtpUseCase` logs for that trace.

**This is also the first real registration test** if the phone number has never signed in before — `VerifyOtpUseCase` auto-registers a new `User` on first successful verification (no separate signup step, by design). Confirm this happens cleanly (no duplicate-account error, no crash).

## Test 2 — JWT Session

**Goal:** confirm the access token issued by Test 1 actually authorizes real requests, and that the app behaves correctly if it's tampered with or expires mid-session.

1. Immediately after Test 1 succeeds, navigate to Dashboard (or any module — Salon, Customers, Services).
2. **Expected:** the page loads real data (or a clean empty state if the salon genuinely has none) — not an error, not an infinite spinner.
3. **Tamper check (optional, more invasive):** with the app closed, locate the DPAPI-encrypted session file under `%LocalAppData%\RojanDesktop\` and confirm it exists and is not human-readable plaintext (confirms `DpapiSecureStorageService` is genuinely encrypting it, not just obfuscating). Do not attempt to decrypt or edit it — DPAPI is user-account-bound, editing it will just corrupt the session.
4. **Expiry check (optional, slow — access tokens are short-lived per `ROJAN_Backend`'s config, ~15 min):** leave the app open and idle past the access token's lifetime, then trigger any action that calls the backend (e.g., switch modules). **Expected:** the app transparently refreshes (see Test 3) rather than showing an auth error — the user should never notice.

## Test 3 — Refresh Token

**Goal:** confirm the 401→refresh→retry-once flow (`HttpApiClient.EnsureAuthenticatedAsync`) works against the real backend, not just against test doubles.

This is hard to trigger deliberately without waiting out the real access-token TTL (Test 2's expiry check) or without a way to force-expire a token from the client side (there isn't one — by design, this app never lets the user manipulate token lifetimes). Two practical options:

- **Passive (recommended):** during Test 2's expiry check, this *is* the refresh flow firing — no separate action needed, just confirm no visible interruption happened.
- **Active:** leave the app open across the *refresh* token's own lifetime too (30 days per `ROJAN_Backend`'s config — impractical for a pre-release smoke test). Not recommended; the passive check above is sufficient evidence for a smoke test.
- **Failure mode to also confirm:** if you have a second, already-expired session file from a much older build/session lying around, launching the app with it should land back on the login screen (refresh failed → `ApiAuthenticationException` → sign in again), not crash or hang.

## Test 4 — Salon Connection

**Goal:** confirm the owner's salon resolves correctly, and the "no salon yet" path also works for a genuinely new account.

1. **Existing-salon path:** if the test account already owns a salon, confirm the Salon page (سالن) shows its real name/address/phone immediately after login — this is `BackendSalonContextService`/`BackendSalonRepository` resolving `GET /salons/mine` for real.
2. **New-account path:** with a brand-new phone number (no salon yet), confirm every other module (Dashboard, Customers, Calendar, etc.) shows a clear "no salon yet" state rather than an error, and that the Salon page's Create form works — submit it, confirm the created salon immediately appears (no restart needed — `ISalonContextService.Invalidate()` should have picked it up).
3. **Cross-check:** the QR Ecosystem's Customer QR (Sidebar → کدهای QR, owner-only) should now render a real, scannable code — decode it with any phone camera and confirm the URL resolves to `https://rojanai.ir/s/{the-salon's-real-slug}`, landing on that salon's real public page.

## Test 5 — Dashboard

**Goal:** confirm the Dashboard's KPIs are real, live-computed values, not cached/stale/placeholder ones.

1. After login, open Dashboard (should be the default landing module).
2. **Expected:** revenue, booking count, and customer count tiles render with real numbers matching the salon's actual current state (zero/empty is fine for a fresh salon — it should read as a clean empty state, not an error or a spinner that never resolves).
3. **Live-recompute check:** note the current booking-count tile, then complete Test 7 (Booking) below, then return to Dashboard **without restarting the app**. **Expected:** the count increases by exactly one, immediately — `GetDashboardInsightsUseCase` computes these live from real booking data on every read, no caching layer to go stale.
4. **Failure mode to watch for:** a silently-zero or silently-unchanged tile after a real data change is worse than a visible error — it means the read path is stale/broken but looks fine. Treat this as a real failure, not a minor issue, if it happens.

## Test 6 — Customer

**Goal:** confirm the Customer CRM module is real, backend-connected read/write, not the in-memory fake data other unrelated modules (Inventory, HR, Reporting, AI) intentionally still use.

1. Open Customers (سالن → مشتریان or the sidebar entry).
2. **Search:** search for an existing customer by name/phone. **Expected:** real results from `GET /salons/{id}/customers`, not a hardcoded demo list.
3. **Create:** create a new test customer (name, phone — minimum required fields). **Expected:** appears in the list immediately, and if you close and reopen the app (forcing a fresh `GET`, not a cached view), it's still there — confirms the write actually landed on the backend, not just in local UI state.
4. **Profile:** open the new customer's profile. **Expected:** the Customer 360 view (stats, tags, notes, empty activity timeline for a brand-new customer) loads without error.
5. **Note:** add a note to the customer. **Expected:** appears in the timeline immediately.
6. **Clean-up:** if this was run against a real salon that will serve real customers afterward, delete or clearly mark the test customer so it doesn't pollute real CRM data — same reasoning as Test 7's clean-up step below.

## Test 7 — Booking

**Goal:** confirm a booking created through the app actually lands in the real backend and is visible from a second read path (not just optimistically shown client-side).

1. From the Bookings module, create a test booking (a real Customer — Test 6's, if you ran it — Service, Specialist, and time slot the salon actually has configured, or create minimal test versions of each first if the salon is otherwise empty).
2. **Expected:** the booking appears immediately in the Bookings list and on the Calendar.
3. **Independent verification:** reload the Dashboard — its revenue/booking counts should reflect the new booking (this is the exact cross-check Test 5 step 3 above describes — run that check here if you haven't already).
4. **Clean-up:** cancel or delete the test booking/customer afterward if this was run against a real salon that will actually be used — a smoke test should not leave test data behind in a production account that goes on to serve real customers.

---

## What this plan deliberately does not cover

- Load/performance testing — out of scope for a pre-release smoke test.
- Multi-user/concurrent-session testing (a Manager/Reception invite flow) — real and testable (Sprint 1's QR Ecosystem), but a second real phone number is needed to receive that invite's own flow; not assumed available for this plan.
- POS/Checkout — intentionally out of scope for this release per every prior sprint's constraints; nothing to smoke-test there yet.
