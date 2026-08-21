# ROJAN Reception v1.0 Production Checklist

**Scope:** `ROJAN_Desktop` + coordinated changes in `ROJAN_Web`. Desktop Productionization Sprint 2 (Production Hardening), building on Sprint 1 (installer, QR ecosystem, website coordination).
**Nothing committed, pushed, deployed, or tagged as part of this sprint** — every change is in the working tree of both repos.

---

## 1. Build Status: ✅ READY

- Full solution: **0 warnings, 0 errors, 2,280 tests passing** (Domain 454, Application 721, Infrastructure 569, Presentation 478, Shell 52, ArchitectureTests 6).
- Self-contained, single-file `win-x64` publish confirmed via `Rojan.Desktop.Shell.runtimeconfig.json`: `"includedFrameworks"` lists both `Microsoft.NETCore.App` and `Microsoft.WindowsDesktop.App` as bundled — not a `"framework"` reference to something the target machine must already have. This is the technical proof behind "no external .NET runtime requirement," not just an assertion from the publish flags used.
- Version: **1.0.0** (was `0.1.0-alpha`) — a deliberate judgment call made explicitly this sprint, per `docs/standards/versioning.md`'s own §4 rule that such a call must be explicit, not automatic. See §6 below for what this version number does and doesn't claim.

## 2. Installer Status: ✅ READY (unsigned — hooks ready)

- `artifacts/ROJAN Reception Setup.exe`, Inno Setup, versioned `1.0.0`, real ROJAN branding (icon embedded — see §4), per-user install, Start Menu + optional desktop shortcut, clean uninstall.
- **Verified with a real install → verify → uninstall → verify cycle**, twice this sprint (once before, once after the branding rebuild): silent install succeeds, `Rojan.Desktop.Shell.exe` present with the real icon, Start Menu shortcut created, Add/Remove Programs shows `DisplayVersion 1.0.0`; silent uninstall removes the app directory, Start Menu folder, and registry uninstall key completely.
- **Code signing: hooks ready, not signed.** `build/publish-installer.ps1` accepts `-CertificatePath`/`-CertificatePassword`/`-TimestampUrl` and signs both the packaged `.exe` and the installer/uninstaller via `signtool.exe`; `RojanReception.iss` has the matching `#ifdef SignInstaller` block. No certificate was purchased (explicitly out of scope) — see `docs/standards/code-signing.md` for exactly what's needed and how a future purchase plugs in with zero redesign. Until then, real users will see a SmartScreen "Unknown Publisher" warning on first run.

## 3. API Status: ✅ Reachable & contract-verified (read-only), ⚠️ not live-user-tested

Per your decision this sprint, verification is **read-only + code-level** — no real OTP SMS was sent, no production data was written. All probes below were re-run at `2026-08-21T10:16Z` against `https://api.rojanai.ir`, safely (either GET, or POST with intentionally invalid input that fails validation before touching SMS/DB):

| Flow | Live evidence | Code cross-check |
|---|---|---|
| Reachability | `GET /actuator/health` → `200 {"status":"UP"}` | — |
| TLS/hardening | HSTS, `X-Frame-Options: DENY`, `X-Content-Type-Options: nosniff` present | Matches `docker/nginx/conf.d/rojan.conf` |
| OTP login | `POST /auth/otp/request` with `{}` → `400 MALFORMED_REQUEST` (validates before any SMS send) | `BackendAuthenticationService.RequestOtpAsync` → same path |
| JWT authentication | `GET /salons/mine` (no token) → `401 AUTH_UNAUTHORIZED` | `HttpApiClient.AttachAuthenticationHeader`/`EnsureAuthenticatedAsync` targets the identical host+path |
| Refresh token | `POST /auth/refresh` with a garbage token → `401 INVALID_TOKEN` (endpoint live, correctly rejects) | `BackendSessionService.RefreshAsync` → same path |
| Salon connection | `GET /salons/mine` → `401` (not 404/500 — route exists, auth-gated) | `BackendSalonContextService`/`BackendSalonRepository` |
| Dashboard | `GET /dashboard/insights` → `401` | `BackendDashboardRepository` |
| Services | `GET /salons/{id}/categories` → `401` | `BackendServiceRepository` |
| Customers | `GET /salons/{id}/customers` → `401` | `BackendCustomerRepository` |
| Booking | `GET /salons/{id}/bookings` → `401` | `BackendBookingRepository` |
| Public salon routing | `GET /public/salons/{unknown-slug}` → `404 SALON_NOT_FOUND` with `traceId` (matches the documented `ApiError` contract exactly) | Confirms the QR Ecosystem's Customer QR link shape (Sprint 1) targets something real |
| Swagger/actuator-info | Both `401` — not publicly exposed | Matches prod-profile hardening already documented in this repo's prior audit |

`ApiEnvironmentService.ProductionUrlDefault` (`https://api.rojanai.ir`, baked in Sprint 1) is confirmed to be this exact, live host.

**Not done, and not claimed done:** a real end-to-end user session (receive a real OTP SMS, log in, load a real dashboard with real data). That requires a real phone number and would write real data to a live production system — out of scope per your decision this sprint.

## 4. Production Branding: ✅ READY

- Real ROJAN brand mark reused from `ROJAN_DesignLab`'s own Play Store master icon (`ic_launcher-playstore.png` — the rose-gold "R"/silhouette mark already used across the Manager/Customer Android apps), converted to a multi-resolution `.ico` (16/32/48/256px) via the new, reproducible `build/generate-icon.ps1`.
- `Rojan.Desktop.Shell.csproj`: `<ApplicationIcon>` wired — confirmed present on both the built `.exe` and the installer `.exe` (icon extraction succeeded on both, non-null, in this session).
- Installer wizard icon (`SetupIconFile`), Start Menu shortcut, desktop shortcut, and Add/Remove Programs entry all inherit the same real icon (shortcuts point at the exe, which now carries it).
- `Directory.Build.props`: `Product` = **"ROJAN Reception"** (internal solution/namespace names unchanged — cosmetic metadata only, confirmed via `Get-ItemProperty`/`VersionInfo` on the built exe: `ProductName: ROJAN Reception`, `ProductVersion: 1.0.0+<commit>`).

## 5. Release Pipeline: ✅ READY (not yet run for real)

`.github/workflows/release.yml` now runs the full requested chain on a version tag: Build → Test → Publish ZIP → Install Inno Setup → Publish installer (signed if `CODE_SIGNING_CERT_BASE64`/`CODE_SIGNING_CERT_PASSWORD` secrets exist, unsigned otherwise) → SHA-256 checksum → GitHub Release with all three artifacts. Updating `ROJAN_Web`'s release registry stays a deliberate manual, cross-repo step (documented in `docs/standards/release-process.md` §4) — this session has no way to test a cross-repo CI trigger end-to-end, and a fabricated one would be worse than an honest runbook.

**Not run for real**: no tag was pushed, no CI execution happened — pushing a release tag is a real, hard-to-reverse action (triggers a real GitHub Release) that wasn't requested. The workflow reuses the exact `publish.ps1`/`publish-installer.ps1` scripts already verified working locally in this session.

## 6. QR Status: ✅ Unchanged from Sprint 1, still 3/3

Manager (client-generated, static download URL), Customer (real backend `/qr-code` link), Reception staff-invite (real backend invite+QR) — all untouched this sprint. Re-verified their tests still pass as part of the full 2,280-test run above.

## 7. Website Download Verification: ✅ Verified

- `/download`, `/download/manager`, `/download/reception` all build and prerender (`● SSG`) per `next build`'s own route table.
- Customer QR landing isn't a website route — it's the backend-generated `/s/{slug}` link (Sprint 1 design, re-confirmed live in §3's public-salon-routing probe).
- `release-registry.ts`/`app-showcase.ts` updated to the real `1.0.0` build: new filename (`rojan-reception-v1.0.0-win-x64-setup.exe`), new SHA-256 (`58623bbf...b741e`, recomputed against this exact rebuilt binary — the icon/version change produced a different file than Sprint 1's), `available: true` unchanged.
- `npx tsc --noEmit`, `npx vitest run` (39/39 download-related, 127/127 full suite before this check), `npm run build` — all pass.

## 8. Clean Install Test: ⚠️ Not performed on a literal clean machine — documented, not fabricated

This environment has .NET 8/9 SDKs and Inno Setup installed; there is no clean Windows VM available here. Rather than claim an untested scenario:
- **What was verified**: the self-contained runtime-bundling proof in §1 (`"includedFrameworks"`, not `"framework"`), plus a full real install/uninstall cycle on this machine (§2).
- **What remains an open action item**: an actual install on a machine with no .NET runtime/SDK/Visual Studio. Manual steps for whoever runs this next: copy `ROJAN Reception Setup.exe` to a clean Windows 10/11 VM → run it → confirm no ".NET Desktop Runtime required" prompt appears → launch `ROJAN Reception` from the Start Menu → confirm the login screen renders → (optionally) attempt OTP login with a real phone number, since that combination (clean machine + real phone) wasn't available together in this environment.

## 9. Known Limitations (full list)

1. **Installer unsigned** — hooks ready, no certificate purchased. SmartScreen will warn on first run. See `docs/standards/code-signing.md`.
2. **No live end-user login test** — OTP/JWT/refresh/salon/dashboard/services/customers/booking are verified live-reachable and contract-correct (§3), not exercised as a real user session. Needs a real phone number.
3. **Clean-VM install not literally tested** — see §8.
4. **Release pipeline not run for real** — the GitHub Actions workflow was extended and reasoned through, not executed via an actual tag push.
5. **POS/Checkout remains on `FakeAccountingRepository`** — unchanged, explicitly out of scope per this sprint's own rules.
6. **No Specialist app** — unchanged, explicitly out of scope per this sprint's own rules; the QR Ecosystem's "Reception staff-invite QR" (Sprint 1) remains the resolution for that original ambiguity.
7. **`ROJAN_Web` release-registry update is manual** — cross-repo automation is a named future improvement, not built this sprint (see `docs/standards/release-process.md`).
8. **Version `1.0.0` is a label, not a certification** — reached deliberately per `docs/standards/versioning.md` §4, but items 1–4 above are real, unresolved gaps a genuine production launch would still need closed.
