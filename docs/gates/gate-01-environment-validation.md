# Gate 01 — Development Environment Validation

**Status:** 🔴 **BLOCKED — do not proceed to Phase 02 project creation**
**Validated:** 2026-07-18
**Machine:** local development machine (Windows, see §4)

## Purpose

This gate exists to confirm the machine that will build ROJAN Desktop can
actually build it — correctly, reproducibly, at the version the
architecture was designed against — before a single `.csproj` exists. A
project scaffolded against the wrong SDK version doesn't fail loudly at
creation time; it fails confusingly later, or silently produces a binary
that only happens to work on the one machine that built it. Validating
first is cheaper than debugging that later.

**Result of this validation: 2 blocking gaps found.** Per the rule
governing this gate, no `.csproj` will be created until both are resolved
and this gate is re-run and passes.

---

## 1. .NET 8 SDK

**Status:** 🔴 **MISSING — BLOCKING**

**Found:**
```
.NET SDKs installed:
  6.0.100 [C:\Program Files\dotnet\sdk]

.NET runtimes installed:
  Microsoft.AspNetCore.App 6.0.0
  Microsoft.NETCore.App 6.0.0
  Microsoft.WindowsDesktop.App 6.0.0
```
Only **.NET 6.0.100** is present. No .NET 8.x SDK, no .NET 8
`Microsoft.WindowsDesktop.App` runtime (the shared framework WPF itself
ships as).

**Why required:** the approved stack decision (Phase 01) targets
`net8.0-windows` — set in `Directory.Build.props`, inherited by every
project that will exist from Phase 02 onward. `dotnet build`/`dotnet run`
against a `net8.0-windows` TFM requires the .NET 8 SDK to compile and the
.NET 8 `Microsoft.WindowsDesktop.App` runtime to execute. Neither exists
on this machine today — any project created now would fail to restore.

**Official Microsoft installation steps:**
1. Go to **https://dotnet.microsoft.com/download/dotnet/8.0**
2. Under "Build apps – SDK", download the **.NET 8.0 SDK** (x64) Windows
   installer — not just the runtime; the SDK is required to build, the
   runtime alone only lets you run already-built apps.
3. Run the installer (standard next-next-finish; no custom options
   needed for this project).
4. Verify: open a **new** terminal (PATH is only updated for new shells)
   and run:
   ```
   dotnet --list-sdks
   ```
   Expect an `8.0.x` entry alongside the existing `6.0.100`. Installing
   .NET 8 does not remove .NET 6 — multiple SDKs coexist side by side by
   design, so this is safe to do without affecting anything else on the
   machine.

---

## 2. Visual Studio 2022 workloads

**Status:** 🟡 **PARTIAL — VS version too old for full .NET 8 support (blocking); required workload itself is present**

**Found:**
- Visual Studio **Enterprise 2022, version 17.0.0** (build 17.0.31903.59)
  — installed 2026-07-01.
- **`.NET desktop development` workload: INSTALLED** ✅ (component
  `Microsoft.VisualStudio.Workload.ManagedDesktop`) — this is the
  workload that provides WPF project templates, the XAML designer, and
  WPF-aware IntelliSense/debugging.
- Also present: `Desktop development with C++` (native toolchain, useful
  for any future native interop) and the Universal (UWP) workload.

**Why the version is a problem despite the right workload being
installed:** VS 2022 17.0.0 is the original November 2021 release —
**.NET 8 was released in November 2023**, and full first-class .NET 8
project-template/SDK-resolution support landed in **VS 17.8**. On 17.0.0,
even after the .NET 8 SDK is installed machine-wide (§1), Visual Studio's
own tooling (IntelliSense accuracy, the XAML designer, the debugger's
understanding of the runtime, new C# 12 language features used by
`net8.0`) will be working against a 2+ year old understanding of the
.NET SDK. This isn't a "might cause a subtle problem someday" risk for a
project explicitly scoped as "production-grade, enterprise, long-term
commercial maintenance" — it's asking the primary IDE to develop against
a runtime it predates.

**Official Microsoft installation steps (update, not reinstall):**
1. Open the **Visual Studio Installer** (Start menu → "Visual Studio
   Installer", or `C:\Program Files (x86)\Microsoft Visual Studio\Installer\setup.exe`).
2. Next to "Visual Studio Enterprise 2022", click **Update** — this
   updates in place to the latest 17.x release; it does not require
   uninstalling or reinstalling, and keeps existing workload selections.
3. Once updated, confirm the `.NET desktop development` workload is still
   checked (it will be, since it's already installed) — no change needed
   there, just re-verify after the update completes.
4. Verify via the terminal check in §8 (MSBuild version) after updating —
   it should report a 17.8+ version, not 17.0.0.

---

## 3. Windows App SDK / WPF requirements

**Status:** 🟢 **PASS (for WPF; Windows App SDK itself not applicable)**

**Found:** the stack decision (Phase 01) is **WPF**, not WinUI 3 — so the
Windows App SDK (which WinUI 3 requires) is not a requirement for this
project at all; including it in this checklist item is worth an explicit
non-applicable note rather than a silent skip, since "Windows App SDK"
and "WPF" are easy to conflate.

WPF's actual runtime requirement is the `Microsoft.WindowsDesktop.App`
shared framework, shipped as part of the .NET SDK/runtime itself (not a
separate download) — confirmed present at 6.0.0 today, will be present
at 8.0.0 automatically once §1 is resolved (the .NET 8 SDK installer
includes it).

**No action required beyond resolving §1.**

---

## 4. Windows SDK version

**Status:** 🟡 **PASS for current scope, recommendation for later**

**Found:**
```
OS: Windows 10 Pro, DisplayVersion 25H2, Build 10.0.26200 (UBR 8875)
Installed Windows SDK: 10.0.19041.0 (full — Include/Lib/bin/References all present)
Windows 11 SDK (22621 / 26100): not installed
```

**Note on the OS report itself:** the registry reports `ProductName:
"Windows 10 Pro"` while the build number (26200) and DisplayVersion
label (25H2) both correspond to the Windows 11 build lineage — a known
cosmetic inconsistency on some Windows installations (the `ProductName`
string doesn't always get updated on in-place upgrades), not something
this gate can or needs to resolve. Doesn't affect build capability
either way.

**Why 10.0.19041.0 is sufficient today:** a plain WPF app targeting
`net8.0-windows` resolves its WPF assemblies from the .NET SDK's own
`Microsoft.WindowsDesktop.App` reference pack, not from the Windows SDK
— the Windows SDK matters for native/COM interop and for packaging
tools (MSIX). Nothing in Phases 02–08 needs more than what's already
installed.

**Recommendation, not a blocker:** install a current Windows 11 SDK
before **Phase 09 (Release Engineering)**, since MSIX packaging tooling
and any Windows 11-specific API surface will want it. Flagged now so
it's a known future step, not a surprise mid-Phase-09.
- Install via the Visual Studio Installer → Modify → Individual
  Components tab → search "Windows 11 SDK" → check the latest version
  (currently `(10.0.26100.0)`), **or** standalone from
  **https://developer.microsoft.com/windows/downloads/windows-sdk/**.

---

## 5. Git

**Status:** 🟢 **PASS**

**Found:** `git version 2.45.1.windows.1` — current, no action needed.

---

## 6. Git LFS (if required)

**Status:** 🟢 **PASS (installed); not yet configured — not currently required**

**Found:** `git-lfs/3.5.1` is installed and available. No `.gitattributes`
exists yet in the repository (confirmed — none present), and no files
over 5MB exist in the repo today (confirmed via full-tree scan), so LFS
tracking isn't actively needed yet.

**Recommendation, not a blocker:** once the repo starts accumulating
binary assets that genuinely warrant it — app icons, installer
artifacts, any bundled media for the Design System phase (Phase 04) —
add a `.gitattributes` with LFS tracking rules for those extensions
*before* the first such file is committed, not after (retrofitting LFS
onto files already in history requires a history rewrite). Worth
revisiting explicitly at the start of Phase 04.

---

## 7. NuGet configuration

**Status:** 🟢 **PASS (global config healthy); repo-level config recommended for Phase 02**

**Found:**
```
NuGet CLI: 6.0.0.278
Registered sources:
  1. nuget.org [Enabled]  — https://api.nuget.org/v3/index.json
  2. Microsoft Visual Studio Offline Packages [Enabled]
```
Machine-global NuGet configuration is present and points at the standard,
official public feed — no untrusted or misconfigured sources.

**Recommendation, not a blocker:** no repository-level `NuGet.Config`
exists yet (none is needed until Phase 02 adds the first
`PackageReference`). When Phase 02 introduces the first package
(the DI/MVVM toolkit choices), add a repo-root `NuGet.Config` that
explicitly pins `nuget.org` as the only source — this is the enterprise
convention specifically so a build is reproducible on any machine
regardless of that machine's own global NuGet source list (which could
contain a private/internal feed misconfigured, disabled, or simply
absent on a fresh clone).

---

## 8. MSBuild

**Status:** 🟡 **Tied to §2 — will resolve automatically once VS is updated**

**Found:**
```
dotnet msbuild: 17.0.0.52104 (via .NET 6 SDK)
MSBuild.exe (VS): 17.0.0.52104 (via VS 2022 17.0.0)
```
Both report the same 17.0.0 build — consistent with each other (no
version skew between the two invocation paths, which is itself a good
sign), but both are the 2021 RTW build. `dotnet msbuild`'s version
tracks whichever .NET SDK invokes it, so it will automatically report a
current version once §1 is resolved; `MSBuild.exe`'s version tracks the
VS install directly, so it updates once §2 is resolved. **No separate
action beyond §1/§2** — this entry exists to make explicit that MSBuild
isn't an independent gap, just a symptom of the other two.

---

## 9. Build Tools

**Status:** 🟢 **PASS**

**Found:** the `.NET desktop development` workload (§2) includes the
MSBuild targets/tasks needed for WPF (XAML compilation, resource
generation, etc.) — confirmed present. `Desktop development with C++`
and its VC++ toolchain are also present, covering any future native
interop need, though nothing currently planned requires it.

---

## 10. Repository health

**Status:** 🟢 **PASS**

**Found:**
```
git status:  clean tree, only the Phase 01 untracked files (nothing committed yet — expected, per Phase 01's own report)
git fsck:    no corruption ("unborn branch" notice is expected/normal for zero commits, not an error)
branches:    main only (no commits yet, so no branch ref exists until first commit)
remotes:     none configured (already flagged as a Phase 01 risk, not new here)
large files: none >5MB
```
No anomalies. This is a clean, freshly-initialized, standalone repository
— notably **not** exhibiting the tracked/working-tree divergence found
earlier in the separate `D:\AndroidProjects` repo (that issue is
unrelated to this project and was already flagged as out of scope when
first discovered).

---

## Summary

| # | Item | Status |
|---|---|---|
| 1 | .NET 8 SDK | 🔴 Missing — **blocking** |
| 2 | VS 2022 workloads | 🟡 Workload present, **VS version blocking** |
| 3 | Windows App SDK / WPF requirements | 🟢 Pass (N/A — WPF, not WinUI3) |
| 4 | Windows SDK version | 🟡 Pass now, recommend Win11 SDK before Phase 09 |
| 5 | Git | 🟢 Pass |
| 6 | Git LFS | 🟢 Pass (installed, not yet needed) |
| 7 | NuGet configuration | 🟢 Pass (repo-level config recommended in Phase 02) |
| 8 | MSBuild | 🟡 Tied to #1/#2, no independent action |
| 9 | Build Tools | 🟢 Pass |
| 10 | Repository health | 🟢 Pass |

## Required actions before this gate can pass

1. **Install the .NET 8 SDK** (§1).
2. **Update Visual Studio 2022 to 17.8 or later** (§2) — via the Visual
   Studio Installer's "Update" button, in place.

Both are user-driven installer/updater steps — I can't run either for
you (they require interactive installer UI and, for the VS update,
likely elevation). Once both are done, tell me and I'll re-run this
validation. **Phase 02 project creation does not start until this gate
shows 🟢 on both items.**

## Approval

Approved by: *pending — blocked on required actions above*
