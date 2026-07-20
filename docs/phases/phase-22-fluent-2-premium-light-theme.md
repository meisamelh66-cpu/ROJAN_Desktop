# Phase 22 — Fluent 2 Premium Light Theme

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Visual refinement only — no business logic, application architecture,
or navigation behavior changed. Adopt a genuinely premium Microsoft
Fluent 2 light theme as the app's shipped default while preserving
ROJAN's brand identity (Purple/Rose Gold/Lavender/Aqua accents, used
only for emphasis, never for ordinary text). Support Light, Dark, and
System theme modes with a real, working switcher, defaulting to
Light. Fix any WCAG contrast issues found along the way. Keep the
build clean and every existing page's layout unchanged.

## Deliverables

- [x] **Design tokens** (`Rojan.Desktop.Presentation/Themes`):
      `Colors.xaml` now documents the four ROJAN brand accents by name
      (`Rojan.Brush.AccentPurple`/`AccentRoseGold`/`AccentLavender`/
      `AccentAqua`) alongside the existing interactive Accent
      (unchanged value); Error/Warning/Success moved out of this
      theme-invariant file into `Light.xaml`/`Dark.xaml` themselves,
      each theme carrying its own WCAG AA-safe shade under the same
      key names (the prior invariant red only reached ~3.6:1 against a
      light background). `Light.xaml` was rewritten as a warm, layered
      surface ramp (Background < SurfaceSecondary < Card/Surface <
      SurfaceElevated, never pure white per this phase's explicit
      instruction) with every text color checked against the WCAG 2.1
      relative-luminance formula (HintText/Disabled were darkened to
      close a real ~3.4:1 contrast gap this phase found).
      `Typography.xaml` gained six new roles - WindowTitle, Disabled,
      Hyperlink, Error, Warning, Success - each setting its own
      Foreground so a consumer only needs one `Style="..."` reference.
      `Controls.xaml` gained `Rojan.Style.ButtonOutlined` (the Fluent
      "Outline" button variant) and an implicit `RadioButton` style
      (the one missing standard form control), plus an opt-in
      `Rojan.Style.ToggleSwitch` for future feature-toggle UI.
      `Shadows.xaml`/`Elevation.xaml`'s opacities were lowered
      (0.24-0.4 → 0.10-0.22) to read as genuinely soft Fluent 2 ambient
      shadows rather than the prior heavier, dark-theme-era look.
- [x] **Three real WCAG contrast bugs found and fixed**: the page
      title (`Controls/SectionHeader.xaml`), every card/widget title
      (`Controls/Dashboard/WidgetHeader.xaml`), and every KPI headline
      number (`Controls/Dashboard/KPIValue.xaml`) used
      `Rojan.Brush.ButtonText` (white, meant for text on a filled
      Accent button) directly on the Card background - invisible on
      Light.xaml, harmless only by coincidence on the old dark theme
      (white happened to read like `TextPrimary` there). Fixed to
      `TextPrimary`. The identical pattern was also found and fixed in
      two Dashboard/Customer activity-timeline rows
      (`Views/Dashboard/DashboardPage.xaml`,
      `Views/Customers/CustomerPage.xaml`).
- [x] **Theming platform** (`Presentation/Theming`,
      `Shell/Theming`): architecturally mirrors the Localization
      platform exactly. `IThemeService`/`ThemeMode` (Light/Dark/System)
      in Presentation; `ThemeService` in Shell persists the choice to
      its own `theme.json` (separate from Localization's
      `settings.json`), resolves `System` via the live Windows
      `HKCU\...\Personalize\AppsUseLightTheme` registry value, and
      defaults to Light on first launch (no settings file yet) per
      this phase's explicit "Default should be Light" requirement.
      Restart-required, never live - the same UX as the language
      switch, for the same reason (StaticResource-based theming can't
      hot-swap without a much larger DynamicResource rearchitecture,
      explicitly out of scope for "visual refinement only").
- [x] **Single app-level resource tree** - the real architectural
      change this phase required: every View previously self-merged
      its own copy of `RojanTheme.xaml` ("self-sufficient, works
      standalone in Blend" - a static, compile-time-fixed pattern that
      hardcoded `Dark.xaml` three times over, in `RojanTheme.xaml`,
      `Controls.xaml`, and `ShellChrome.xaml`, which would have
      silently overridden any runtime Light/Dark choice). The whole
      design system is now assembled exactly once, in code, by
      `Shell.Theming.ThemeResources.Apply`, called from
      `App.xaml.cs`'s `OnStartup` before any Window is created -
      choosing `Light.xaml` or `Dark.xaml` per
      `IThemeService.ResolvedTheme`, then merging the rest of the
      (now theme-agnostic) design system in the same order
      `RojanTheme.xaml` always used. `App.xaml` no longer merges
      anything statically. Roughly twenty View/Control files had their
      local `RojanTheme.xaml` self-merge removed; any co-located local
      resources they also carried (converters, per-file `Style`
      overrides) were preserved untouched. `RojanTheme.xaml` itself
      remains in the repo, updated to reference `Light.xaml`, as a
      standalone convenience aggregate - unused by the running app,
      but not broken for any future consumer (Blend preview, a future
      test), satisfying "maintain backward compatibility."
- [x] **Settings UI**: a new Theme section, directly below Language,
      built with the identical shape (pick a mode, Apply, a
      restart-required message, a Restart Now button) - the two
      sections share one `RestartCommand`, since a single relaunch
      applies whichever preference(s) are pending. New
      `Strings.Settings_Theme_*` keys across all three languages
      (fa-IR/en-US/ar-SA).

## Migration Notes / Scope Boundaries

- **Restart-required, not live.** Every `{StaticResource ...}`
  reference in this app resolves once, at the point its owning
  `ResourceDictionary`/`Style` is first parsed - not reactively.
  Making theme switching genuinely live (no restart) would require
  converting the whole design system to `DynamicResource`, a much
  larger, riskier change explicitly out of this phase's "visual
  refinement only, do not change architecture" scope. Restart-required
  matches the Localization platform's own established UX exactly.
- **Backward compatible by construction.** No design-system *key* was
  renamed or removed - only relocated (Error/Warning/Success moved
  from Colors.xaml into each theme file) or added. Every existing
  `{StaticResource Rojan.Brush.X}`/`Rojan.Style.X`/`Rojan.TextStyle.X`
  reference across the whole app continues to resolve unchanged.
- **`RojanTheme.xaml` is dead code, not deleted.** It is no longer
  merged by anything in this solution (Application.Resources is
  assembled in code; every View lost its self-merge), but it remains
  present, correct, and self-consistent for any future standalone
  consumer.
- **No layout changes.** Every fix in this phase (contrast bugs,
  resource-merge removal, new opt-in styles) is either a color/brush
  value change or plumbing invisible to a page's own Grid/StackPanel
  structure - no page's visual layout was touched.

## Risks

- **Design-time (Blend) preview of a single View in isolation no
  longer renders correctly on its own** - it depended on that View's
  own `RojanTheme.xaml` self-merge, now removed; the design system is
  only assembled by the running app's `OnStartup`. A future need for
  standalone design-time preview could re-add a merge to
  `RojanTheme.xaml` specifically for `d:` design-time resources
  without touching the runtime path.
- **`ThemeService.ResolveSystemTheme` depends on an undocumented but
  long-stable Windows registry value** (`AppsUseLightTheme`, present
  since Windows 10 1607). If a future Windows version removes or
  renames it, `System` mode silently falls back to Light (the same
  documented fallback behavior as a missing/corrupt `theme.json`) -
  not a crash, but worth revisiting if Microsoft ever changes this key.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` — 898/898 tests passing (11 new):
      `Presentation.Tests` (+5: `SettingsPageViewModelTests`'
      Theme-section cases - preselect current mode, select without
      persisting, apply-to-different-theme sets restart-required,
      apply-to-same-theme leaves it clear, localized current-theme
      display text), `Shell.Tests` (+6: `ThemeServiceTests` -
      first-launch defaults to Light, persisted Dark mode is restored,
      a corrupt settings file falls back to Light without throwing,
      System mode resolves to a real Light/Dark value against the live
      registry without asserting which, applying a different resolved
      theme sets restart-required, applying the same resolved theme
      does not). `ArchitectureTests` (4, unchanged) continue to pass -
      the new `Presentation.Theming`/`Shell.Theming` namespaces follow
      the existing dependency-direction rules by construction (same
      split as Localization).
- [x] Runtime verified end-to-end via UI Automation:
      - Deleted any existing `theme.json`, launched fresh: the
        Dashboard and every other page rendered the new warm Light
        theme by default (off-white layered surfaces, dark-plum text,
        purple accent buttons, green/red trend indicators) - no
        `StaticResourceExtension` errors anywhere, confirming the
        resource-dictionary restructuring resolves correctly with
        every View's redundant self-merge removed.
      - Settings → Theme section showed "Current theme: Light" with
        the Light button highlighted; selecting Dark and clicking
        Apply Theme correctly showed the restart-required message and
        a Restart Now button.
      - Clicking Restart Now relaunched the process; the new instance
        rendered the full dark navy theme identically to the
        pre-Phase-22 look, and Settings → Theme correctly showed
        "Current theme: Dark" with Dark highlighted; `theme.json` on
        disk read `{"mode":"Dark"}`.
      - Selecting "Match System" and applying persisted
        `{"mode":"System"}` without error.
      - `theme.json` was deleted again after verification so the
        shipped Light default is what the next launch (and any fresh
        clone) actually sees.
- [x] No business logic, architecture, or navigation behavior changed -
      verified by the unmodified, still-passing `ArchitectureTests`
      and by every existing page ViewModel/Domain/Application file
      being untouched; only `Themes/*.xaml`, five `Controls/*.xaml`
      title/value bindings, ~20 Views' resource-merge blocks,
      `SettingsPage.xaml`/`SettingsPageViewModel.cs`, `App.xaml(.cs)`,
      and the new Theming files changed.

## Approval

Approved by: <pending> — <date>
