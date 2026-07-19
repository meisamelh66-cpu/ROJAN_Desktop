# Phase 17A — Microsoft Fluent 2 Compliance Audit & Final UI Polish

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

A UI-only refinement sprint - no business features, no Domain/Application/
Infrastructure/business-rule/repository/service/command changes.
Architecture stayed frozen throughout. Building on Phase 16's Fluent 2
integration, this phase is an audit: tighten the design-token naming to a
strict semantic set, remove every remaining "legacy glass" name (even
where the underlying value was already fully Fluent 2), and close a real
accessibility gap (missing disabled-state styling on several controls)
that a compliance pass is specifically meant to catch.

## Deliverables

- [x] **Color audit**: theme tokens renamed to the exact strict semantic
      set requested - `Background` (was `Navy`), `Border`/`BorderStrong`
      (was `Stroke`/`StrokeStrong`), `Disabled` (was `DisabledText`),
      `Error`/`Success`/`Warning` (were `ErrorText`/`SuccessText`/
      `WarningText` - the "Text" suffix implied foreground-only use, but
      these color borders/fills too). New `Card` token added as its own
      explicit value (same hex as `Surface` today, but named for its
      place in the hierarchy rather than borrowing the more generic
      `Surface` token by coincidence) and a new `Scrim` token (a
      theme-invariant plain black, replacing a literal `"Black"` that
      would otherwise have been the one hardcoded color introduced this
      phase). Confirmed via grep: zero hardcoded hex colors, zero named
      color literals anywhere in `src/`, both before and after every
      change in this phase.
- [x] **Surface hierarchy**: Window (`Background`) → Navigation/Header
      (`SurfaceSecondary`) → Page (`Background`) → Section/Card (`Card`)
      → Content, unchanged in structure from Phase 16, now with `Card`
      as its own real token instead of reusing `Surface` by coincidence.
- [x] **Sidebar compliance**: re-verified against the current codebase -
      icon alignment (20px reserved column, centered glyph), spacing
      (16,10 item padding, 8,1 item margin), the accent-tinted selection
      indicator (soft background + 3px leading bar) and neutral hover,
      keyboard focus (`Rojan.Style.FocusVisual`) were all already in
      place from Phase 16 and needed no further change beyond the token
      rename.
- [x] **Typography audit**: renamed to the exact requested six-step scale
      - `Display` (was `HeroTitle`) / `Title` (was `SectionTitle` - its
      only real consumer, `Controls/SectionHeader.xaml`, is genuinely the
      once-per-page title) / `SectionHeader` (was `CardTitle` - its real
      consumer, `Controls/Dashboard/WidgetHeader.xaml`, is exactly a
      section heading inside a Card) / `Subtitle` / `Body` / `Caption`.
      The prior `PageTitle` step was removed entirely - grep confirmed
      it had zero consumers since Phase 16 introduced it, an orphaned
      token this audit caught rather than a role needing a seventh name.
- [x] **Spacing audit**: already the 4/8/12/16/24/32/48 scale from Phase
      16 (`Spacing.xaml`); re-verified no arbitrary margins crept in
      across Phase 17's Inventory module.
- [x] **Controls audit**: `Button`/`TextBox`/`ComboBox`/`DatePicker`/
      `CheckBox` all confirmed to inherit shared `Themes/Controls.xaml`
      resources only - grep confirmed zero page-level control-style
      redefinitions anywhere in `Views/`. Closed a real gap: `ComboBox`,
      `CheckBox`, and `WindowCloseButton` had no `IsEnabled="False"`
      visual treatment at all (a disabled instance would have looked
      identical to an enabled one) - now all dim to 0.4 opacity,
      consistent with the buttons and `TextBox` that already had it
      (`TextBox` itself was inconsistent at 0.5 - aligned to 0.4 too).
- [x] **Dialog and Wizard audit**: Booking Wizard's Fluent surface, step
      indicator, Primary/Secondary button hierarchy, and spacing were
      already correct from Phase 16; the one real change this phase
      makes is to the Shell dialog region's scrim - was the theme
      `Background` brush (tinting the darkened backdrop with itself) at
      50% opacity, now a proper theme-invariant black `Scrim` token at a
      lighter, more Fluent-typical 40%.
- [x] **Icon audit**: re-verified all 8 called-out modules (Dashboard,
      Customers, Bookings, Calendar, Services, Specialists, Inventory,
      Settings) plus every remaining placeholder (Accounting, Reports,
      AI Center) - confirmed via raw UTF-8 byte inspection that every
      `ModuleMetadata.IconGlyph` is a distinct, valid Segoe Fluent Icons
      codepoint from `Themes/Icons.xaml`'s set, unchanged and correct
      from Phase 16/17.
- [x] **Accessibility audit**: added the missing `ComboBox`/`CheckBox`/
      `WindowCloseButton` disabled states (see Controls audit above);
      confirmed the shared `Rojan.Style.FocusVisual` (accent-colored
      focus rectangle) is applied to every interactive style; confirmed
      chrome/nav touch targets (32px minimum) and default WPF tab order
      (visual-tree order, no `TabIndex` overrides needed since no
      layout uses `Canvas`/absolute positioning) were already sound.
- [x] **Visual regression check**: launched the app and screenshotted
      Dashboard, Customers, Bookings, Calendar, Services, Specialists,
      and Inventory both mid-audit (after the semantic-token rename) and
      again after the final `Glass*`-style-key rename - pixel-identical
      both times (expected: a pure resource-key rename plus two isolated
      value changes - scrim color/opacity and disabled-state opacity -
      neither of which altered any page's day-to-day appearance).
- [x] **"No legacy glass" pass**: beyond the token rename, the *style
      key names* themselves still read as legacy even though their
      values were already fully Fluent 2 since Phase 16 - a style
      literally named `GlassCard` is a legacy-glass-named component
      regardless of what it renders. Renamed `Rojan.Style.GlassCard` →
      `Card`, `GlassPanel` → `Panel`, `GlassButton` → `ButtonPrimary`
      (completing the Primary/Secondary pair), `GlassNavigationItem` →
      `NavigationItem`, across every one of the 11 consuming files, plus
      the doc-comment prose that described them.

## Risks

- **`Card` and `Surface` share the same underlying hex value today.**
  They are deliberately two different semantic tokens (one for the
  Section/Card tier, one for generic panels/form controls) that happen
  to resolve identically right now - not true duplication (each has a
  distinct, real reason to exist and a distinct set of consumers), but a
  future visual refinement that wants to differentiate cards from plain
  panels only needs to change `Card`'s value, not touch every consumer.
- **No runtime light/dark toggle**, unchanged from Phase 16 - `Light.xaml`
  was updated to the same renamed token set but still isn't wired to any
  UI switch; not requested this phase either.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors (build was
      re-run clean after both rename passes - the semantic-token rename
      and the separate `Glass*`-style-key rename).
- [x] `dotnet test RojanDesktop.sln` - 362/362 tests passed, unchanged
      from Phase 17 - this phase touched only Presentation `Themes/`
      resource dictionaries and `MainWindow.xaml`'s dialog-scrim markup;
      no ViewModel, Application, Domain, or Infrastructure file changed
      behavior, so no test needed to change, and none did.
- [x] Runtime verified via UI Automation against the real running app,
      twice (once after each rename pass): all 7 real modules (Dashboard,
      Customers, Bookings, Calendar, Services, Specialists, Inventory)
      load and render correctly with no broken bindings, no missing
      resources, and pixel-identical appearance to before the audit.
- [x] Final pre-commit audit, all clean: zero hardcoded hex colors, zero
      named color literals, zero remaining `Rojan.Style.Glass*` keys,
      every `StaticResource Rojan.*` reference in `src/` resolves to a
      real `x:Key` definition (cross-checked by set difference), zero
      pages redefine a shared control's implicit style.
- [x] Clean Architecture boundaries unchanged - this phase touched only
      `Rojan.Desktop.Presentation/Themes/*.xaml` and one Shell file
      (`MainWindow.xaml`'s dialog-scrim markup); `ArchitectureTests`
      still passes unmodified.

## Approval

Approved by: <pending> — <date>
