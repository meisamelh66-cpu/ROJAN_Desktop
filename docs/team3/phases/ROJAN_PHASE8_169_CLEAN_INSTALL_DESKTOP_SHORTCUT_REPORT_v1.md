# ROJAN_PHASE8_169 — CLEAN OLD INSTALLATIONS & DESKTOP SHORTCUT SETUP — REPORT v1

**Phase:** 8.169 · **Type:** Local machine cleanup · **Date:** 2026-08-29
**Machine:** DESKTOP-93E967G · Windows 10 Pro 19045 x64
**Mode:** No source change · no commits · no merges · no tags

---

## TASK A — OLD INSTALLATIONS FOUND

| Item | Path | Verdict |
|---|---|---|
| **Current install** | `%LOCALAPPDATA%\Programs\ROJAN Reception\` — v1.0.0, ARP `{D804D0AC-…}_is1`, has `unins000.exe` | **KEEP** |
| Old raw-publish copy | `%LOCALAPPDATA%\Programs\ROJAN Desktop\` — Aug 25 build, no uninstaller, no ARP entry (orphan) | REMOVE |
| Old test install | `%LOCALAPPDATA%\Programs\ROJAN Desktop (Local Test - f7d1150)\` — Aug 17, framework-dependent layout (loose DLLs), no uninstaller | REMOVE |
| Old Desktop shortcut | `%USERPROFILE%\Desktop\ROJAN Desktop.lnk` → `…\Programs\ROJAN Desktop\Rojan.Desktop.Shell.exe` | REMOVE |
| Old Start-Menu shortcut | `…\Start Menu\Programs\ROJAN Desktop.lnk` → same old folder | REMOVE |
| Current Start-Menu folder | `…\Start Menu\Programs\ROJAN Reception\` (app + uninstall lnk) | KEEP (recreated by fresh install) |
| App data | `%LOCALAPPDATA%\RojanDesktop\` — shared SQLite DB + device identity | KEEP (belongs to the current version) |
| HKLM / WOW6432 ARP | none | — |

No machine-wide (`Program Files`) installs. Only one registered uninstaller (`{D804D0AC-…}_is1` = ROJAN Reception). The two `ROJAN Desktop*` folders were manual publish copies with no ARP registration.

---

## TASK B — OLD VERSIONS REMOVED

| Action | Result |
|---|---|
| `rm -rf "…\Programs\ROJAN Desktop"` | ✅ removed (~174 MB) |
| `rm -rf "…\Programs\ROJAN Desktop (Local Test - f7d1150)"` | ✅ removed |
| `rm -f "…\Desktop\ROJAN Desktop.lnk"` | ✅ removed |
| `rm -f "…\Start Menu\Programs\ROJAN Desktop.lnk"` | ✅ removed |
| Source files / project folders | ❌ untouched — nothing under `C:\AndroidProjects\` modified |
| App data (`%LOCALAPPDATA%\RojanDesktop`) | preserved through this step (cleared later only to force a genuine first-run of the fresh install) |

Neither old folder had an `unins000.exe`, so removal was a plain directory + shortcut delete; no orphan ARP entries were left behind (there were none).

---

## TASK C — CURRENT VERSION KEPT / FRESH INSTALL

1. Stopped all running `Rojan.Desktop.Shell` processes.
2. Uninstalled the existing v1.0.0 (`unins000.exe /VERYSILENT`) → exit 0; install dir + ARP key + Start-Menu folder removed.
3. Cleared `%LOCALAPPDATA%\RojanDesktop` → true first-run state.
4. Verified fully clean: no ROJAN program folders, no ROJAN shortcuts, `_is1` ARP key absent (`Test-Path` → `False`).
5. **Fresh install:** `artifacts\ROJAN Reception Setup.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART` → **exit 0**.

| Field | Value |
|---|---|
| Installer | `ROJAN Reception Setup.exe` — 54,051,767 B — SHA-256 `4974531f52bed1a8e69903aac27e36f8b5cf6b270cac56ac9ec74d59775d9dd2` (from Phase 8.168, built from `da0c36b`) |
| Install location | `C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception\` |
| ARP entry | `ROJAN Reception` / `1.0.0` / `ROJAN` |
| Installed exe | ProductName `ROJAN Reception` · ProductVersion `1.0.0+da0c36b…` · FileVersion `1.0.0.0` · Company `ROJAN` |
| Start-Menu | `ROJAN Reception\ROJAN Reception.lnk` + `Uninstall ROJAN Reception.lnk` |

**Only ROJAN Reception v1.0.0 remains.**

---

## TASK D — DESKTOP SHORTCUT

The `/VERYSILENT` install itself created `Desktop\ROJAN Reception.lnk` (see Note). It was then **re-created explicitly** for correctness and a normalized icon:

```
Name:             ROJAN Reception
Target:           C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception\Rojan.Desktop.Shell.exe
WorkingDirectory: C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception
IconLocation:     …\Rojan.Desktop.Shell.exe,0   (the exe's embedded RojanReception icon)
Description:      ROJAN Reception
```

| Verify | Result |
|---|---|
| Icon correct | ✅ `IconLocation` = installed exe, index 0 (embedded `RojanReception.ico`) |
| Shortcut launches app | ✅ `Invoke-Item` on the `.lnk` → app starts, MainWindow "ROJAN Reception", login screen renders, no crash |
| Version = 1.0.0 | ✅ target exe ProductVersion `1.0.0` / FileVersion `1.0.0.0` |

> **Note — supersedes Phase 8.168 Observation 2.** A genuinely clean `/VERYSILENT` install (ARP `_is1` key verified absent beforehand) still created the desktop shortcut and recorded `Inno Setup: Selected Tasks: desktopicon`, despite `[Tasks] desktopicon … Flags: unchecked`. So the earlier "remembered task state" explanation was wrong: on this installer, silent install creates the desktop shortcut. Benign here (TASK D wants one anyway), but for a future release where the desktop shortcut is meant to be opt-in, the `.iss` `[Tasks]` behaviour under `/VERYSILENT` should be revisited (e.g. `checkedonce`, or documenting `/MERGETASKS=""`).

---

## TASK E — VALIDATION

| Check | Result |
|---|---|
| Application launches | ✅ launched from the Desktop shortcut; process stable; closes gracefully (no force-kill) |
| Login screen appears | ✅ "ROJAN Reception" heading, "شماره موبایل" (RTL), "ارسال کد" button; fresh SQLite `rojan.db` + `identity\device.json` created on start |
| No duplicate installations remain | ✅ old `ROJAN Desktop\` + `ROJAN Desktop (Local Test - f7d1150)\` folders and both old shortcuts deleted |
| Only one ROJAN Reception installation exists | ✅ **1** program folder (`ROJAN Reception`), **1** ARP entry (`{D804D0AC-…}_is1`, v1.0.0), **1** Start-Menu folder, **1** Desktop shortcut |

Final machine inventory:
```
Program folder:  C:\Users\ELHAEE\AppData\Local\Programs\ROJAN Reception\   (1)
ARP entry:       ROJAN Reception  1.0.0  ROJAN                             (1)
Start Menu:      …\Programs\ROJAN Reception\{ROJAN Reception, Uninstall}   (1 folder)
Desktop:         …\Desktop\ROJAN Reception.lnk  -> installed exe           (1)
App data:        %LOCALAPPDATA%\RojanDesktop\{database\rojan.db, identity\device.json}
```

---

## VERDICT

| Field | Value |
|---|---|
| **Old installations removed** | **PASS** — 2 folders + 2 shortcuts removed; 0 orphans remain |
| **New installation** | **PASS** — fresh `/VERYSILENT` install, exit 0, single registered instance |
| **Desktop shortcut** | **PASS** — created, correct icon + target, launches the app, version 1.0.0 |
| **Final installed version** | **1.0.0** |

App is installed, launchable from the Desktop and Start Menu, unsigned (external gate B1). Machine has exactly one ROJAN Reception.

---

## VERIFICATION

| Check | Result |
|---|---|
| `.cs` / `.xaml` / `.csproj` / build-logic changed | ❌ none |
| `src` / `tests` diff vs `origin/main` | empty |
| Commits / merges / tags | ❌ none |
| Branch changed | ❌ none |
| Tracked working tree | 0 dirty (HEAD `da0c36b`) |
| Machine changes | removed 2 old install folders + 2 old shortcuts; reinstalled ROJAN Reception 1.0.0 (per-user); created Desktop shortcut |
| Files created this phase | `ROJAN_PHASE8_169_CLEAN_INSTALL_DESKTOP_SHORTCUT_REPORT_v1.md` (untracked, repo root) |

---

**STOP.**
