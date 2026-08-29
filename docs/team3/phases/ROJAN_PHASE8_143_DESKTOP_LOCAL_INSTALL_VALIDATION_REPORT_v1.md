# ROJAN AI — TEAM 3 — PHASE 8.143 — DESKTOP LOCAL INSTALLATION VALIDATION REPORT v1

**Type:** Installation + first-run check. **No source change. No commit. No branch change.** Verification only.
**Branch:** `feature/team3-desktop-completion` @ `da0c36b` (unchanged, tracked tree clean) · **Reference:** `ROJAN_PHASE8_142_AUDIT_TRAIL_COMMIT_REPORT_v1.md`

## RESULT: ✅ builds, installs, **launches**, renders a localized window, and handles the no-backend case gracefully — with two environment limits (Inno Setup not installed → no installer package; no reachable backend + non-interactive automation → cannot drive past the login/init gate to validate inner screens).

---

## A. INSTALLER ARTIFACT

### What was built — self-contained single-file `win-x64` Release publish ✅

`build/publish.ps1` (unmodified) → `dotnet publish -c Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true` — **succeeded in 1m52s**.

| Artifact | Value |
|---|---|
| `publish\Rojan.Desktop.Shell.exe` | **165.97 MB** single file (runtime + all Rojan + EF/SQLite/Hosting deps bundled) |
| `artifacts\RojanDesktop-v1.0.0-win-x64.zip` | **70.5 MB** (LZMA-compressed) |
| `publish\` payload | 17 items — the exe + `Languages\{ar-SA,de-DE,en-US,fa-IR}.pack` + `.pdb`/`.xml` symbols/docs |

### Metadata / branding verification (on the built exe, via `VersionInfo`) ✅

| Field | Value | Expected | ✓ |
|---|---|---|---|
| ProductName | `ROJAN Reception` | `ROJAN Reception` | ✅ |
| ProductVersion | `1.0.0+da0c36bccebaa741e6cd222f8c248a66fda04be2` | `1.0.0+<commit>` | ✅ |
| FileVersion | `1.0.0.0` | `1.0.0.0` | ✅ |
| CompanyName | `ROJAN` | `ROJAN` | ✅ |
| LegalCopyright | `Copyright © ROJAN 2026` | dynamic year | ✅ |
| Application icon | **extracted, 32×32, non-null** | embedded `RojanReception.ico` | ✅ |
| Configuration | **Release** (`-c Release`, deterministic, `TreatWarningsAsErrors`) | Release | ✅ |
| Language packs | 4 `.pack` files present in `Languages\` | fa/en/ar/de | ✅ |

### Installer PACKAGE (`ROJAN Reception Setup.exe`) — ⚠️ NOT BUILT — Inno Setup not installed on this machine

`iscc` / `ISCC.exe` is **not on PATH, not in `Program Files\Inno Setup 6`, `Program Files (x86)\...`, `%LOCALAPPDATA%\Programs\...`, nor in the uninstall registry.** `signtool` is also absent. `build/installer/RojanReception.iss` and `build/publish-installer.ps1` are present and unmodified. Compiling the `.iss` into the signed/unsigned `ROJAN Reception Setup.exe` requires Inno Setup on a build machine — this matches the Sprint-2 checklist's design (the installer + signing are produced in CI / on a dedicated build box, not here). **`.iss` config confirmed by inspection:** `AppId {D804D0AC-BF41-4A54-8904-D9EC1BB773CF}`, `DefaultDirName={autopf}\ROJAN Reception` (per-user, `PrivilegesRequired=lowest`), `DefaultGroupName=ROJAN Reception`, `OutputBaseFilename=ROJAN Reception Setup`, `SetupIconFile=…\RojanReception.ico`, `UninstallDisplayIcon={app}\Rojan.Desktop.Shell.exe`, `Source: {publish}\*` (recursive).

---

## B. INSTALL RESULT

**No installer `.exe` to run** (Task A). The install was performed as the `.iss` `Source:` line does it — a recursive copy of `publish\*` into the per-user install target:

| Item | Value |
|---|---|
| Target | `%LOCALAPPDATA%\Programs\ROJAN Reception\` (== `{autopf}\ROJAN Reception` for a `lowest`-privilege install) |
| Files laid down | 17 (13 top-level + `Languages\` with 4 `.pack`) |
| `Rojan.Desktop.Shell.exe` present | ✅ · ProductName `ROJAN Reception` · ProductVersion `1.0.0+da0c36b…` |
| Admin rights needed | none (per-user) |
| Source files modified | none · project config changed | none |
| **Cleanup** | the simulated install directory was **removed** after validation — the machine is left clean |

**Not exercised** (needs the real Inno Setup installer): Start-Menu shortcut, optional desktop shortcut, Add/Remove Programs entry, silent-install/uninstall cycle. The `.iss` defines all four.

---

## C. FIRST LAUNCH RESULT

The published exe **was launched** (three bounded runs, each terminated after the observation window; the machine has an interactive session available).

### Run 1 — default config (`ApiEnvironment.Development` → `localhost:8080`)

| Observation | Result |
|---|---|
| Process | PID 13128, ran **25 s+** without exiting, `Responding = True` |
| Startup crash | **none** — `stderr` empty; the app's own log has **no `[Error]` and no `Unhandled exception` entry** |
| Window | `MainWindowHandle` non-zero, **`MainWindowTitle = "ROJAN Desktop"`** (matches the RC2-branded title), `WorkingSet` 105 MB |
| Generic host | started — `stdout`: "Application started", "Hosting environment: Production", content root = install dir |
| Local persistence | **EF Core + SQLite initialized** — `__EFMigrationsHistory` checked, "No migrations were applied. The database is already up to date." App created `%LOCALAPPDATA%\RojanDesktop\` with `data/`, `identity/`, `logs/`, `notifications/`, `security/`, `workspaces/` — every local-state subsystem came up (no missing-resource failure). |
| Backend reachability | `localhost:8080` **connection refused** — logged as a single `[Warning]` from `HttpApiClient` (the one intentional full-detail diagnostics logger, per Phase 8.15); the app **did not crash** |

### Run 2 — default config, longer settle + screenshot

| Observation | Result |
|---|---|
| Process | PID 11484, ran **40 s+**, `Responding = True`, `stderr` empty |
| **Window rendered** | **426 × 159 px at (479, 315), visible** — a dialog-sized window |
| **Screenshot** (`scratchpad/rojan_firstrun.png`, 6 KB) | a standard Windows **MessageBox**: title **"ROJAN Desktop"**, ⚠ warning icon, **Yes / No** buttons, Persian body text: *"خطا در اتصال به سرور برای بارگذاری حساب شما. آیا می‌خواهید دوباره تلاش کنید؟"* — "Error connecting to the server to load your account. Do you want to try again?" |

This is the **`App.OnStartup` → `InitializeAsync` Retry / Exit prompt** (`App.xaml.cs` `confirmRetry`, documented as the deliberate "must not crash the app" boundary). The app: started → rendered a **localized** (fa-IR, RTL) window → hit the no-backend condition → **offered a graceful retry choice** instead of an unhandled-exception dialog. Exactly the designed behaviour.

### Run 3 — `ROJAN_API_BASE_URL=https://api.rojanai.ir` (to reach the login screen)

| Observation | Result |
|---|---|
| Env override | honoured — request went to `GET https://api.rojanai.ir/api/v1/users/me/salon-access` (not localhost) |
| Result | **`ApiTimeoutException` after 30 s** — this automation environment can't reach `api.rojanai.ir`. Logged as a single typed `[Warning]`; **no crash.** The window was still inside blocking `InitializeAsync` when sampled (title `''`, handle 0). |

**Confirmed:** typed-exception mapping works end to end (`HttpRequestException` → connection-refused path; `ApiTimeoutException` → timeout path), both handled gracefully.

### What could NOT be validated here — and why

| Screen / flow | Blocker |
|---|---|
| Login form (phone/OTP fields, sign-in button) | `InitializeAsync` gates on a backend round-trip; neither `localhost:8080` nor `api.rojanai.ir` is reachable from this automation environment, so the flow stops at the Retry/Exit prompt / the 30 s timeout before the login window is shown. |
| Main shell window, **navigation panel loading** | requires a completed login (a real session) → a reachable backend + real OTP. |
| **Settings page** contents; **Theme / Language** toggle interaction | reachable only after login; and validating them means *driving* the UI (clicks, reads), which a non-interactive automation session cannot do — only observe a rendered window. |

These are exactly the items the Sprint-2 `docs/team3/phases/…RojanReception_v1.0_Production_Checklist.md` §8 already flags as **"an open action item — a manual run on a machine with backend connectivity + a real phone number."** Nothing in the Phase-8 hardening work changes them; the hardening is verified structurally by the 2,715/2,715 suite and the ViewModel-level tests.

---

## D. LOCAL INSTALL CHECK

| Check | Result |
|---|---|
| Installed files exist | ✅ 17 items at `%LOCALAPPDATA%\Programs\ROJAN Reception\` — single exe + `Languages\*.pack` + symbols |
| Missing DLL / resource errors | ✅ **none** — self-contained single-file build; app launched, `stderr` empty, every local-state subsystem (SQLite, DPAPI session store, workspace/notification/search repos, file logger, localization/culture) initialized |
| App version displayed correctly | ✅ exe `VersionInfo`: ProductName **ROJAN Reception**, ProductVersion **1.0.0+da0c36b…**, FileVersion **1.0.0.0**, Company **ROJAN** |
| Window title | ✅ **"ROJAN Desktop"** (RC2-branded) |
| Application icon | ✅ embedded (extracted 32×32) |
| Start-Menu / desktop shortcut | ⚠️ **not created** — file-copy install only; the real Inno Setup installer (not available here) creates them per the `.iss` |
| Add/Remove Programs entry | ⚠️ **not created** — same reason |

---

## E. WARNINGS / ERRORS

| # | Item | Severity | Note |
|---|---|---|---|
| 1 | **Inno Setup not installed on this machine** → `ROJAN Reception Setup.exe` could not be compiled | environment limit | `.iss` + `publish-installer.ps1` verified present/unchanged; installer + signing are a CI / build-box step (matches Sprint-2 design). |
| 2 | **First-launch API environment defaults to `Development` (`localhost:8080`)** → a fresh install with no backend shows the Retry/Exit prompt immediately | **Blocker B7** (already tracked — Phase 8.133 §F / 8.134 §D) | **Confirmed live.** Not a defect in the hardening work; a Product/DevOps decision (flip default for Release / force choice in onboarding / accept). The app **handled it gracefully** — the point of this observation is that the guard works, and that B7 is real. |
| 3 | `api.rojanai.ir` unreachable from this automation environment → 30 s `ApiTimeoutException` | environment limit | typed exception mapped + logged as `warn`, no crash. |
| 4 | `HttpApiClient` logs the exception message (`… (localhost:8080)`) | expected | `HttpApiClient` is the one documented full-detail diagnostics logger (Phase 8.15), outside the operation-name-only ViewModel rule. Correct. |
| 5 | Post-login screens (nav, Settings, theme/language) not visually validated | environment limit | needs backend connectivity + a real phone + an interactive operator — the Sprint-2 §8 open action item. |

**No crash, no unhandled exception, no missing dependency, no source/config change, no commit, no branch change.**

---

## F. SCREENSHOTS

| File | Content |
|---|---|
| `…/scratchpad/rojan_firstrun.png` (6 KB) | The first-run **Retry/Exit MessageBox** — title "ROJAN Desktop", ⚠ icon, Yes/No, Persian body ("Error connecting to the server to load your account. Do you want to try again?"). Demonstrates: the app launches, renders a window, loads fa-IR localization (RTL), and the `InitializeAsync` no-backend boundary produces a graceful prompt rather than a crash. |

(Run 3 produced no screenshot — the window had not yet been shown when sampled, mid-`InitializeAsync`.)

---

## STOP

Phase 8.143 local installation validation complete. **No source change, no commit, no branch change** — `feature/team3-desktop-completion` @ `da0c36b`, tracked tree clean; the simulated install directory and all launched processes were cleaned up.

**Built:** self-contained single-file `win-x64` **Release** publish (166 MB exe, `artifacts\RojanDesktop-v1.0.0-win-x64.zip` 70.5 MB) with correct metadata (**ROJAN Reception**, **1.0.0+da0c36b**, Company **ROJAN**), **embedded icon**, and 4 language packs. **The installer `.exe` was NOT compiled — Inno Setup is not installed here** (the `.iss` + `publish-installer.ps1` are present and unchanged; installer/signing is a build-box step).

**Installed** (per-user copy per the `.iss` `Source:` line) and **launched** — the app **starts, renders a window (title "ROJAN Desktop"), loads localization/theme resources, initializes the generic host + EF Core/SQLite + DPAPI session store + file logger**, and — with **no backend reachable** — shows the **graceful `InitializeAsync` Retry/Exit prompt** (screenshot captured), **not a crash**. Typed-exception handling (`HttpRequestException`, `ApiTimeoutException`) verified. **No missing DLL / resource errors.**

**Could not validate** the post-login main shell, navigation, Settings page, or theme/language toggles — this automation environment has **no reachable backend** and **cannot drive a GUI interactively**; those remain the Sprint-2 §8 manual action item (backend connectivity + a real phone + an operator).

**Live-confirmed: Blocker B7** — first-launch API environment defaults to `Development` (`localhost:8080`) — the app handles it correctly, and the decision on whether to flip the Release default belongs to Product / DevOps.

**Awaiting next authorization.**
