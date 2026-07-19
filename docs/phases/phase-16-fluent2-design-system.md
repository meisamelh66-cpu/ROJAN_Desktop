# Phase 16 — Microsoft Fluent 2 Design System Integration

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

A UI/UX-only sprint - no business functionality, no Domain/Application/
Infrastructure changes, no workflow changes. Replace the app's prior
"glass" visual language (translucent white fills, a purple->magenta
gradient on nearly every surface, 32px pill-shaped cards) with Microsoft
Fluent 2: neutral surfaces plus one restrained accent color, reserved for
primary actions, selection, active state, and focus. ROJAN's identity
(navy-tinted neutrals instead of Windows' own gray scale, the existing
purple accent hue, the brand wordmark) is preserved throughout - the goal
was "Fluent 2 with ROJAN identity," not a Windows clone.

## Deliverables

- [x] **Theme file structure** (`Presentation/Themes/`): `Colors.xaml`
      (theme-invariant tokens only - accent variants, status colors),
      `Dark.xaml`/`Light.xaml` (the full theme-dependent surface/text/
      stroke token set, each self-contained - the standard WPF
      swappable-theme-dictionary pattern; `Dark.xaml` is the active
      default, `Light.xaml` is built as a genuinely complete alternative
      though no runtime toggle is wired up this phase), `Typography.xaml`
      (six-step scale: PageTitle/SectionTitle/CardTitle/Subtitle/Body/
      Caption, plus HeroTitle for large KPI numbers), `Spacing.xaml`
      (4/8/12/16/24/32/48, numeric keys primary with the old semantic
      names kept as aliases), `Shapes.xaml` (4/8/12/999px corner radii,
      replacing the old 32px pill rounding), `Shadows.xaml` +
      `Elevation.xaml` (neutral black shadow tiers aliased to semantic
      surface roles - Navigation/Card/Popover/Dialog), `Icons.xaml`
      (Segoe Fluent Icons glyph tokens, falling back to Segoe MDL2 Assets
      - no new package dependency, both fonts ship with Windows),
      `RojanTheme.xaml` (renamed from `Theme.xaml`, the single entry
      point every consumer merges). `Gradients.xaml` was deleted - Fluent
      2 doesn't use heavy gradients, and its only real consumer
      (`GlassButton`'s prior gradient fill) was rewritten to a solid
      accent fill.
- [x] **Controls.xaml**: `GlassCard`/`GlassPanel`/`GlassButton`/
      `GlassNavigationItem` key names kept stable (every existing View
      already referenced these - only what they resolve to changed) but
      rebuilt on neutral surfaces/elevation instead of translucency/
      gradient; new `ButtonSecondary` (Fluent's outlined secondary
      button, completing the Primary/Secondary hierarchy). New implicit
      (TargetType, no `x:Key`) `TextBox`/`ComboBox`/`ComboBoxItem`/
      `DatePicker`/`DatePickerTextBox`/`CheckBox` styles - these had **no
      styling at all** before this phase (default Windows Aero chrome,
      a stark white box against the dark theme), the single biggest
      visual inconsistency this phase fixes; every existing form control
      in every View picks these up automatically, no View edits needed.
      A shared accent-colored `FocusVisualStyle` is applied to every
      interactive style for keyboard-focus visibility.
- [x] **ShellChrome.xaml + MainWindow.xaml**: neutral hover/pressed
      chrome-button feedback (no accent - chrome buttons aren't one of
      Fluent 2's accent-eligible cases); sidebar nav items redesigned
      with a real Fluent "selection indicator" (accent-tinted background
      + 3px accent bar on the leading edge) replacing the old solid-
      purple-fill + animated aqua-mint hover overlay; every sidebar/
      header/window-chrome glyph now comes from `Icons.xaml`'s Segoe
      Fluent Icons set instead of the prior mixed Unicode-symbol/emoji
      glyphs (☰ 🔔 › ‹ swapped for consistent glyphs from one font,
      including all 11 module sidebar icons - `ModuleMetadata.IconGlyph`
      is UI display data, not business logic, so updating these string
      literals stayed in scope).
- [x] **Booking Wizard** (`BookingWizardView.xaml`): a real Fluent Wizard
      step-progress indicator (7 segments, accent-filled up to the
      current step via a new `EnumAtLeastConverter` - a stateless,
      generic Presentation-layer display helper, not a workflow/business
      rule, so `BookingWizardViewModel.cs` itself was not touched);
      tightened dialog sizing (480px wide vs. the prior 560, less empty
      chrome); Fluent's Primary/Secondary button hierarchy (Next/Confirm
      Booking/Done are each step's one primary action; Back/Cancel are
      secondary); corrected the error-message color (`ErrorText`, not
      the decorative `RoseGoldHighlight` token the prior version used by
      mistake).
- [x] **Spot-check pass**: every page View's stray references to removed
      "glass" tokens (`VividPurple`, `AquaMint`, `SoftLavender`,
      `GlassWhite`, `GlassBorder`) fixed to the new neutral/accent
      equivalents (customer tag chips, service/specialist skill chips,
      caret colors) - verified by cross-referencing every `StaticResource
      Rojan.*` key referenced anywhere in `src/` against every key
      defined anywhere in `src/`, confirming zero dangling references.

## Risks

- **No runtime light/dark toggle.** `Light.xaml` is a complete,
  independently usable theme file, but nothing in the UI switches
  `RojanTheme.xaml`'s merge from `Dark.xaml` to it - no toggle was
  requested this phase, and the app still ships dark-only, consistent
  with the "foundation now, wire up later" pattern used elsewhere in
  this app (e.g. the Phase 07 dialog region before Phase 15 gave it a
  producer).
- **`DatePicker`'s calendar drop-down popup is only partially re-skinned.**
  The closed-state text field (`DatePickerTextBox`) is fully Fluent-
  styled; the popup `Calendar` control gets Background/Foreground/
  BorderBrush/elevation via its `CalendarStyle` property, but its
  internal day-cell hover/selected-day chrome (a deeply nested default
  template, `CalendarItem`) was not fully retemplated - a deliberate,
  bounded scope decision given the size of this phase, not an oversight.
- **A few plain, un-styled inline `Button`s remain** (the small "×"
  remove buttons on tag/skill chips) - pre-existing from before this
  phase, using default Button chrome since they were never assigned an
  explicit style; out of scope for a design-system-token phase, not a
  regression this phase introduced.
- **Trend-arrow glyphs (▲▼—) stay plain Unicode**, not Segoe Fluent
  Icons - they already render clearly and consistently; swapping them
  carried real risk (Unicode Private-Use-Area codepoints proved fragile
  to insert reliably via tooling during this phase, see the module-icon
  fix below) for marginal benefit on a glyph that isn't part of the
  sidebar/header/chrome "one icon family" surface the request called
  out.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 278/278 tests passed, unchanged
      from Phase 15 (this phase is Presentation XAML/resource-only; no
      ViewModel, Application, Domain, or Infrastructure file changed
      behavior, so no test needed to change).
- [x] Runtime verified via UI Automation against the real running app:
      captured full-window screenshots of Dashboard, Customers, Bookings,
      Calendar, Services, and Specialists. Confirmed neutral surfaces
      throughout, accent color appearing only on primary buttons and the
      selected nav item, all form controls (TextBox/ComboBox/DatePicker)
      rendering with consistent Fluent chrome instead of default Windows
      widgets, and the sidebar's new icon set rendering correctly for
      every module.
- [x] Clean Architecture boundaries unchanged - this phase touched only
      `Rojan.Desktop.Presentation` (Themes/Controls/Views/a new
      Converters folder) and `Rojan.Desktop.Shell` (MainWindow.xaml,
      module icon-glyph string literals); `ArchitectureTests` still
      passes unmodified, confirming no dependency-direction change.
- [x] No Domain, Application, Infrastructure, or workflow-rule changes -
      `BookingWizardViewModel.cs`, `BookingPageViewModel.cs`, and every
      other ViewModel/Application service file are untouched.

## Approval

Approved by: <pending> — <date>
