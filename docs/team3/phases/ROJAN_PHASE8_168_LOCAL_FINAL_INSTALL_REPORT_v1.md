# ROJAN_PHASE8_168 — LOCAL FINAL INSTALLATION & LAUNCH VALIDATION — REPORT v1

**Phase:** 8.168 · **Type:** Final user-machine installation · **Date:** 2026-08-29
**Machine:** DESKTOP-93E967G · Windows 10 Pro 19045 x64
**Mode:** No source modification. Build + package + install + first-run validation only.

---

## TASK A — SOURCE VERIFIED

| Field | Value |
|---|---|
| `origin/main` SHA | `77414defe806ab705a6bbc78fb9b8cd3ad72c4f1` (`77414de`) |
| Local HEAD | `da0c36b` (docs commit; `src/` + `tests/` trees identical to `77414de`) |
| Working tree | 0 tracked files dirty — no source changes |
| Version (`Directory.Build.props` `<VersionPrefix>`) | **1.0.0** |
| Toolchain | .NET SDK 8.0.424 · Inno Setup 6.7.3 (`%LOCALAPPDATA%\Programs\Inno Setup 6\ISCC.exe`) |

---

## TASK B — FINAL RELEASE BUILD

| Step | Command | Result |
|---|---|---|
| Build | `dotnet build -c Release` | **Build succeeded — 0 Warning(s), 0 Error(s)** (`TreatWarningsAsErrors=true`) |
| Test | `dotnet test -c Release --no-build` | **2,715 / 2,715 passed · 0 failed · 0 skipped** |

| Suite | Passed |
|---|---|
| Rojan.Desktop.Domain.Tests | 456 |
| Rojan.Desktop.Application.Tests | 791 |
| Rojan.Desktop.Presentation.Tests | 772 |
| Rojan.Desktop.Infrastructure.Tests | 609 |
| Rojan.Desktop.Shell.Tests | 80 |
| Rojan.Desktop.ArchitectureTests | 7 |
| **Total** | **2,715** |

**TASK B: PASS** — 0/0, 2715/2715.

---

## TASK C — INSTALLER CREATED

`pwsh build/publish-installer.ps1` → fresh self-contained single-file `win-x64` Release publish → `ISCC.exe` → `Created … ROJAN Reception Setup.exe (unsigned)`.

| Field | Value |
|---|---|
| Path | `artifacts/ROJAN Reception Setup.exe` |
| **Size** | **54,051,767 bytes** (51.55 MB) |
| **SHA-256** | **`4974531f52bed1a8e69903aac27e36f8b5cf6b270cac56ac9ec74d59775d9dd2`** |
| Sidecar | `artifacts/ROJAN Reception Setup.exe.sha256` — regenerated this phase to match (the script does not refresh it; CI `release.yml` does) |
| Companion ZIP | `RojanDesktop-v1.0.0-win-x64.zip` — 73,887,754 bytes |
| **ProductName** | `ROJAN Reception` |
| **ProductVersion** | `1.0.0` |
| **CompanyName / Publisher** | `ROJAN` |
| **Icon** | `RojanReception.ico` — `SetupIconFile` (wizard) + `UninstallDisplayIcon={app}\Rojan.Desktop.Shell.exe` (ARP); compile succeeded with the icon embedded |
| **Signature** | `Get-AuthenticodeSignature` → **NotSigned** (no certificate — external gate B1) |
| AppId | `{D804D0AC-BF41-4A54-8904-D9EC1BB773CF}` |

> **Note — hash/size differ from Phase 8.144** (`69cb1f29…` / 54,057,848 B). Both are built from the same source tree; this run is from `da0c36b`, and Inno's compression plus the commit-id embedded in the payload exe's informational version account for the ~6 KB delta. Not a concern — version, product metadata, and behaviour are identical.

**TASK C: PASS.**

---

## TASK D — INSTALL

`ROJAN Reception Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART` → **exit 0**.

| Check | Result |
|---|---|
| Install location | `C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception\` (per-user, no admin prompt) |
| Files installed | `Rojan.Desktop.Shell.exe` (174 MB, self-contained single-file) · `Languages\{ar-SA,de-DE,en-US,fa-IR}.pack` · `unins000.exe` + `unins000.dat` |
| **Start Menu shortcut** | ✅ `…\Start Menu\Programs\ROJAN Reception\ROJAN Reception.lnk` → the app exe · plus `Uninstall ROJAN Reception.lnk` |
| **Desktop shortcut** | ✅ correctly **absent** on a clean silent install (`[Tasks] desktopicon Flags: unchecked`). See Observation 2. |
| **Uninstall entry** | ✅ ARP: `ROJAN Reception` / `1.0.0` / `ROJAN` · `UninstallString = "…\unins000.exe"` · `InstallLocation` set |
| **Application launch** | ✅ launches — MainWindow title "ROJAN Reception", ~120–140 MB working set, no crash |
| **Uninstall** (verified this phase) | `unins000.exe /VERYSILENT` → **exit 0** · install dir removed · ARP key removed · Start-Menu folder removed · desktop `.lnk` removed · `%LocalAppData%\RojanDesktop` removed — **fully clean** |

**TASK D: PASS.**

---

## TASK E — FIRST RUN

Launched `Rojan.Desktop.Shell.exe` from the installed location (app data cleared beforehand → true first run).

| Check | Result | Evidence |
|---|---|---|
| **App opens** | ✅ PASS | MainWindow renders; process stable through the full session; closes gracefully via `CloseMainWindow()` (no force-kill, no hang) |
| **fa-IR RTL UI** | ✅ PASS | Right-aligned field label "شماره موبایل"; RTL heading "ورود به روژان دسکتاپ"; RTL button "ارسال کد"; window chrome mirrored (title right, close-× left); Persian digit rendering |
| **Login screen** | ✅ PASS | "ROJAN Reception" heading, mobile-number entry, "ارسال کد" (send code) primary button — the OTP entry screen |
| **Local database creation** | ✅ PASS | `%LocalAppData%\RojanDesktop\database\rojan.db` + `rojan.db-shm` + `rojan.db-wal` (SQLite WAL, ~255 KB schema) created on startup — EF Core bootstrap ran |
| **Logger initialization** | ✅ PASS | `%LocalAppData%\RojanDesktop\logs\` directory created at startup by `LocalFileLoggerProvider`. No `rojandesktop-2026-08-29.log` file was written — by design: the provider persists only `LogLevel >= Warning` (`IsEnabled => logLevel >= LogLevel.Warning`), and the passive first-run (no user actions in this non-interactive test) produced nothing at that level. |
| **No crash without backend** | ✅ PASS | Also created: `identity\device.json` (`appVersion 1.0.0.0`, registered 2026-08-29). With no reachable backend (default env `http://localhost:8080`), the login screen surfaces a **sanitized, localized** error — "خطا در اتصال به سرور. اتصال اینترنت خود را بررسی کنید." ("Server connection error. Check your internet connection.") — **no crash, no stack trace, no raw exception text, no backend detail**. The Phase-8 P2 error-surface sanitization is confirmed working in the packaged product. |

**TASK E: PASS (all six).**

---

## OBSERVATIONS (non-blocking)

1. **Debug symbols shipped in the installer.** `build/installer/RojanReception.iss` `[Files]` uses `Source: "{#PublishDir}\*"; … recursesubdirs`, so the `.pdb` and `.xml` (XML-doc) files from `publish/` land in `{app}` on the end-user machine. Effect: larger install footprint (~2 MB of docs/symbols) and minor internal type/method-name disclosure. Not a security P0. Recommend the `[Files]` glob exclude `*.pdb` / `*.xml` (or `publish.ps1` stop emitting them) — a packaging tweak, not a code change.
2. **Desktop shortcut appeared on the first (non-clean) install, then behaved correctly.** The machine had residual Inno "Selected Tasks: desktopicon" state from an earlier install cycle (Inno's `usePreviousTasks` default). A full uninstall → clean reinstall in this phase produced **no desktop shortcut**, matching `Flags: unchecked`. Benign — Inno honouring a returning user's prior choice.
3. **Installer `.sha256` sidecar is not refreshed by `publish-installer.ps1`.** It still held the Phase-8.144 hash until regenerated here. `release.yml` regenerates it in CI; for local builds it is stale until refreshed. Recommend the script emit it.
4. **Two `Rojan.Desktop.Shell.exe` processes per launch** — expected for a self-contained single-file app (apphost + extracted runtime host). Both carry the window title; both exit on graceful close.

---

## VERDICT

| Field | Value |
|---|---|
| **Installed version** | **1.0.0** |
| **Install** | **PASS** |
| **Launch** | **PASS** |
| **Login screen** | **PASS** |
| **Backend unavailable handling** | **PASS** |

Additional: Release build **PASS** (0/0), full test suite **PASS** (2,715/2,715), installer generation **PASS**, uninstall **PASS** (clean), first-run DB + identity + logger init + fa-IR RTL **PASS**.

**ROJAN Reception v1.0.0 is currently installed on this machine** (`C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception\`, ARP entry present). The only gap remains the unsigned installer (external gate B1).

---

## VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / `.csproj` / build-logic changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits / merges / tags | ❌ none |
| Branch changed | ❌ none |
| Tracked working tree | 0 dirty (HEAD `da0c36b`) |
| Machine changes | ROJAN Reception 1.0.0 installed (per-user); `artifacts/` rebuilt (gitignored); `.sha256` sidecar refreshed (gitignored) |
| Files created this phase | `ROJAN_PHASE8_168_LOCAL_FINAL_INSTALL_REPORT_v1.md` (untracked, repo root) |

---

**STOP.**
