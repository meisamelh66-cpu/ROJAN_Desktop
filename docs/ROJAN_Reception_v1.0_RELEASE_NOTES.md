# ROJAN Reception v1.0 — Release Notes

**Release date:** 2026-08-21 (candidate — see the Release Candidate Report for the GO/NO-GO decision)
**Platform:** Windows 10/11, x64

---

## New Features

ROJAN Reception is a native Windows desktop application for salon reception staff and owners, connected to the real ROJAN backend (`https://api.rojanai.ir`) — no demo/mock mode.

- **Authentication** — mobile number + OTP login (primary), email/password (secondary), DPAPI-encrypted local session storage, automatic silent token refresh.
- **Salon management** — connect to an existing owned salon, or create a new one directly from the app; real-time salon profile (name, address, phone, description).
- **Dashboard** — live revenue, booking, and customer KPIs, computed fresh on every load.
- **Customers** — full CRM: search, profile, tags, notes, activity timeline, status management.
- **Services & Specialists** — real catalog browsing, backend-connected.
- **Appointments / Booking** — full reception booking wizard (customer → service → specialist → availability → time slot → confirm), real backend calendar integration.
- **Staff onboarding via Salon Invites** — accept a real invite to join a salon as Reception or Manager.
- **QR Ecosystem** — a dedicated, printable QR page (owner-only) with three real QR codes:
  - **Manager QR** — links to the ROJAN Manager app's download page.
  - **Customer QR** — links to this salon's real public booking page.
  - **Reception Invite QR** — generates a real, single-use staff invite for front-desk onboarding.
  A4 print layout with salon branding, generated via the app's native print pipeline.
- **Multi-language** — Persian (default), English, Arabic, German, with a pluggable Language Pack system.
- **Professional installer** — `ROJAN Reception Setup.exe`, self-contained (no .NET runtime install required), versioned, Start Menu + optional desktop shortcut, clean uninstall.

## Production Readiness

| Area | Status |
|---|---|
| Core business flow (Auth → Salon → Dashboard → Services → Specialists → Customers → Appointments) | ✅ Real, backend-connected |
| Installer | ✅ Built, versioned 1.0.0, verified via real install/uninstall cycles |
| Production API | ✅ Confirmed live and contract-correct (read-only/code-level verification — see the Production Checklist) |
| Branding | ✅ Real ROJAN icon on the installer, exe, and shortcuts; ⚠️ see Known Limitations for a window-title inconsistency |
| Code signing | 🟡 Hooks ready, unsigned (no certificate purchased) |
| Release pipeline | ✅ Full Build→Test→Publish→Installer→Checksum chain in CI, not yet run via a real tag |
| Clean-machine install | ⚠️ Not literally tested (no clean VM available in the environment this was built in) |

## Known Limitations

1. **Installer is unsigned.** Windows SmartScreen will show an "Unknown Publisher" warning on first run. Signing hooks are fully wired (`docs/standards/code-signing.md`) — this is a certificate-purchase decision, not an engineering gap.
2. **Window title still reads "ROJAN Desktop," not "ROJAN Reception."** Found during this release's First User Experience Audit: the app's Product metadata was rebranded, but four hardcoded XAML strings (`LoginWindow.xaml` lines 5 & 42, `MainWindow.xaml` lines 14 & 252) were not updated. Cosmetic, not functional — visible in the title bar, taskbar, login screen heading, and main shell header. Not fixed as part of this validation pass (report-only scope).
3. **No live end-user login has been exercised against production** as part of this release's own testing — every endpoint was confirmed live and contract-correct via safe, non-mutating probes (no real SMS sent, no production data written). `docs/ROJAN_Reception_v1.0_Smoke_Test_Plan.md` has the exact manual steps for whoever runs the real test with a real phone number.
4. **No literal clean-machine install test performed** — verified by proxy instead (runtime self-containment proof + real install/uninstall cycles on a machine that does have the SDK/tooling installed).
5. **POS/Checkout is not implemented** — still backed by in-memory fake data, by design. Out of scope for every sprint that built this release; a future, separately-scoped effort.
6. **No Specialist-facing app** — the original product ecosystem has Manager (mobile), Customer (mobile), and Reception (this app, desktop) only. The QR Ecosystem's "Reception Invite QR" is the resolution for what was originally going to be a "Specialist QR," since no Specialist product or backend role exists.
7. **`ROJAN_Web`'s release registry update is a manual, cross-repo step** — not automated in this release's CI pipeline (documented in `docs/standards/release-process.md`).

## Installation Requirements

- **OS:** Windows 10 (1809+) or Windows 11, x64.
- **.NET Runtime:** **none required** — the installer bundles a self-contained .NET 8 + WPF runtime (confirmed via `runtimeconfig.json`'s `includedFrameworks`, not a shared-framework reference).
- **Disk space:** ~150 MB installed.
- **Privileges:** none required — installs per-user (no admin/UAC prompt) to the current user's local application folder.
- **Network:** internet access to `api.rojanai.ir` (HTTPS/443) for all real functionality; the app will not function meaningfully offline (no offline/cached mode exists).
- **Uninstall:** standard Windows "Apps & Features" entry, or the Start Menu shortcut; removes the app and its local settings/session data completely.
