# Phase 19A — Enterprise Globalization & Localization Platform

**Status:** Awaiting Approval
**Completion:** 100% (architecture) — business-module content migration is Phase 19B

## Objectives

Build the complete enterprise localization platform and prove the
architecture works end-to-end, without rushing every screen's content
through it. Per the agreed scope refinement: implement the full
infrastructure (`LocalizationService`, `LanguageManager`-equivalent,
`CultureService`, `ResourceManager`-backed string resolution,
`LanguagePackManager`, `IDateProvider`/`PersianCalendarProvider`/
`GregorianCalendarProvider`, `CurrencyFormatter`, RTL/LTR
infrastructure, built-in Persian/English/Arabic, the Language Pack
architecture, the Settings UI, persistence, restart flow, resource
loading, pack discovery, and a versioning foundation), and fully
migrate only the Application Shell, Navigation, Dashboard, Settings,
shared controls, the dialog framework, and the menu system. The nine
remaining business modules (Customers, Bookings, Calendar, Specialists,
Services, Inventory, Accounting, HR, plus the not-yet-built Reports/AI
Center) are explicitly deferred to Phase 19B — this phase's job is to
make that migration a pure content exercise with zero architectural
changes required.

Do NOT modify: Domain, Application business logic, Navigation
mechanics, or the Fluent 2 Design System. Only the localization
platform and the strings inside the areas listed above.

## Deliverables

- [x] **Presentation.Localization** (cross-cutting, not a vertical
      slice — interfaces only, modeled on the existing
      `INavigationService`/`IDialogService` split):
      `LanguageInfo` record (code, native/English name, RTL flag, font
      family, number-digit style, default currency, date-provider id,
      pack version, compatibility version, built-in flag);
      `ILocalizationService` (current language, available languages,
      restart-required flag, initialize, set-language);
      `ILanguagePackManager` (discover languages, get a pack's string
      overrides); `ICultureService` (culture lookup with
      invariant-culture fallback, RTL→`FlowDirection` mapping);
      `ICurrencyFormatter`/`Currency` enum (Toman/Rial/Usd/Eur) with
      `NumberDigits` (Latin/Persian/Arabic) glyph substitution;
      `IDateProvider`/`PersianCalendarProvider`/
      `GregorianCalendarProvider` (future-extensibility only — nothing
      in this phase consumes a non-Gregorian calendar for real data
      yet); `LanguagePackCatalogEntry` and `ILanguagePackRepository`
      (the Language Store Foundation's abstraction). `Strings` — a
      hand-written `ResourceManager` wrapper (no VS resx designer tool
      available in this environment), backed by `Strings.resx`
      (neutral/default, containing **Persian** content — a deliberate,
      documented departure from the "neutral = English" .NET
      convention, since Persian is the true first-launch default) plus
      true satellite resources `Strings.en.resx`/`Strings.ar.resx`
      (verified to produce real `en/`/`ar/`
      `Rojan.Desktop.Presentation.resources.dll` satellite assemblies
      at build time). `Strings.SetPackOverrides` lets a non-built-in
      pack's partial string table win over the compiled resx per key,
      falling through honestly for keys it doesn't define.
- [x] **Shell.Localization** (concrete implementations — file-system
      access stays in the composition root, same reasoning as
      `Shell.Navigation.NavigationService`): `LanguagePackManager`
      scans `Languages/*.pack` (JSON via `System.Text.Json`, no new
      NuGet package) next to the executable, skipping malformed files
      rather than crashing startup; `LocalizationService` persists the
      selected language to
      `%LocalAppData%\RojanDesktop\settings.json`, defaults to Persian
      on first launch or a corrupt/missing settings file, and flags
      `IsRestartRequired` only when the selection actually changes;
      `LocalOnlyLanguagePackRepository` — the Language Store
      Foundation's only implementation this phase ships: an
      always-empty catalog, `DownloadAndInstallAsync`/`RemovePackAsync`
      both throw `NotSupportedException` rather than silently no-op, so
      a future real implementation's absence is never mistaken for
      success. Both `LanguagePackManager` and `LocalizationService`
      expose an `internal` path-overriding constructor purely for
      testability (a temp directory instead of
      `AppContext.BaseDirectory`/`LocalAppData`).
- [x] **Language Pack format**: JSON files at
      `src/Rojan.Desktop.Shell/Languages/*.pack`, copied to the build
      output via `CopyToOutputDirectory`. All three built-in languages
      (`fa-IR.pack`, `en-US.pack`, `ar-SA.pack`, each `isBuiltIn: true`)
      ship as real pack files too — not a hardcoded list — proving pack
      discovery is genuinely automatic; their strings come from the
      compiled resx, not the pack itself. A fourth, deliberately
      partial `de-DE.pack` (`isBuiltIn: false`, ~15 of ~50 keys
      overridden) is a working demo of a third-party pack, proving
      "future packs load their own resources" is a real, testable
      mechanism and not just a doc-comment promise.
- [x] **RTL/LTR infrastructure**: `MainWindow.FlowDirection` is set
      once in the code-behind constructor from
      `ILocalizationService.CurrentLanguage.IsRightToLeft`; WPF's
      `FlowDirection` inheritance cascades it through the entire visual
      tree automatically (layout mirroring, Grid column order, dialogs
      — since the dialog host is inside the same window). No per-View
      RTL code anywhere.
- [x] **Migrated content**: Shell chrome (search/back/forward/
      notifications/guest-user/minimize/maximize/close/collapse-nav,
      the "no notifications" empty state), `MainWindowViewModel`
      (status message, breadcrumbs, error-dialog text), every module's
      sidebar `Title` (all nine real modules plus the two Reports/AI
      Center placeholders — see the Fixed-bug note below), the shared
      `DashboardWidget` style in `Themes/DashboardComponents.xaml`
      (Loading/No data/Retry — used by every module's dashboard-style
      widgets), `DashboardPage.xaml` and
      `DashboardPageViewModel`'s quick-action labels, and the new
      `SettingsPage`/`SettingsPageViewModel` (Language section in
      full — built-in languages, installed packs with Remove, Apply +
      restart-required flow + Restart Now, and the Available Languages
      foundation UI where every action reports
      `Settings_Language_ComingSoon` rather than silently doing
      nothing).
- [x] **Settings module**: replaces the `"settings"` `PlaceholderModule`
      one-for-one (`SettingsModule`/`SettingsPage`/
      `SettingsPageViewModel`), registered in
      `ServiceCollectionExtensions`/`Views.xaml`'s DataTemplate
      registry the same way every other module is.

## Migration Report (required deliverable)

Estimated remaining hardcoded UI strings per business module, counted
via the same XAML-attribute survey technique used for the Phase 19A
scoping pass (`Text=`/`Content=`/`Title=`/`Header=`/`Watermark=`/
`ToolTip=` attributes starting with a letter — an undercount, since it
does not include string literals inside ViewModels, e.g. status or
validation messages, which Phase 19B will also need to migrate):

| Module | Views (.xaml files) | Estimated hardcoded strings |
| --- | --- | --- |
| HR | 1 | ~75 |
| Accounting | 2 | ~37 |
| Inventory | 1 | ~29 |
| BookingWorkflow | 1 | ~23 |
| Bookings | 1 | ~20 |
| Customers | 1 | ~18 |
| Specialists | 1 | ~14 |
| Services | 1 | ~8 |
| Calendar | 1 | ~6 |
| **Total** | **10** | **~230** |

Not included above, and each with its own nuance for Phase 19B to
resolve:

- **`Views/Modules/PlaceholderModulePage.xaml`** (shared by Reports and
  AI Center) has 1 hardcoded string ("This module hasn't been
  implemented yet.") — trivial, but shared infrastructure rather than
  per-module content, so it doesn't belong to either module's count.
- **Dashboard KPI card labels** ("Active Clients", "Total Bookings",
  "Pending Tasks", "Revenue (MTD)", etc.) are supplied by
  `Application`-layer DTOs (`KpiMetricDto.Label`), not Presentation
  XAML — migrating them needs an Application-layer decision (resource
  keys shipped from Application, or a Presentation-side lookup keyed by
  a stable metric id), out of scope for a XAML-only resx swap and worth
  scoping explicitly at the start of Phase 19B.
- **ViewModel-level strings** (validation messages, status text,
  confirmation prompts) exist in every module's `*ViewModel.cs` and are
  not captured by the XAML-attribute count above; Phase 19B's actual
  scope will be larger than the table suggests once those are counted.

## Risks

- **Restart-required, not live, language switching.** A deliberate
  simplification (no reactive `{Binding}` string plumbing needed since
  `{x:Static loc:Strings.Key}` resolves once at startup), but it means
  a user who changes language must restart to see it take effect — the
  Settings UI makes this explicit (a warning message plus a "Restart
  Now" button) rather than silent.
- **Language Store Foundation has no real backend.** Per this phase's
  explicit "do not connect to servers yet" instruction,
  `LocalOnlyLanguagePackRepository`'s catalog is always empty and
  install/remove always throw. The UI and abstraction exist; the actual
  online store does not.
- **Nine business modules still show entirely English/hardcoded UI
  text** regardless of the selected language — by design for this
  phase, but a real, visible gap until Phase 19B closes it. The
  migration report above exists specifically so that gap is measured,
  not just acknowledged.
- **`de-DE.pack` is a demo, not a real fourth language.** It exists
  purely to prove the pack-override mechanism works with a genuinely
  partial translation; it is not intended to ship as a real supported
  language and should be removed or completed before any release that
  claims German support.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` — 619/619 tests passing (45 new):
      `Rojan.Desktop.Shell.Tests` (new project, 17 tests) — the first
      test project with dedicated Shell-owned-class coverage, made
      possible by `InternalsVisibleTo` plus path-overriding internal
      constructors on `LanguagePackManager`/`LocalizationService`:
      pack discovery (well-formed packs parsed, missing directory
      returns empty, malformed pack skipped without throwing, a pack
      missing `code` is skipped, a pack's `strings` overrides are
      read correctly), `LocalizationService` (first-launch Persian
      default with no available languages at all, first-launch Persian
      default among discovered languages, persisted-language restore,
      corrupt-settings-file fallback to Persian, same-language Apply
      leaves `IsRestartRequired` false, different-language Apply
      persists and sets it true, unknown-language throws, and a
      pack-override end-to-end check against `Strings`), and
      `LocalOnlyLanguagePackRepository` (empty catalog,
      `NotSupportedException` from both install and remove).
      `Rojan.Desktop.Presentation.Tests` (+28 new, in new
      `Localization/` and `Settings/` subfolders):
      `CurrencyFormatterTests` (all four currencies, Latin/Persian/
      Arabic digit substitution, default-digits behavior),
      `CultureServiceTests` (known-culture lookup, malformed-code
      fallback to invariant culture, RTL/LTR flow-direction mapping),
      `DateProviderTests` (Gregorian ISO-order formatting; Persian
      calendar conversion verified against a real `PersianCalendar`
      computation — 2026-03-21 Gregorian is confirmed 1405/01/01
      Jalali, Nowruz), `StringsTests` (resx resolution with no
      override, pack override winning over resx, partial override
      falling through honestly, override clearing), and
      `SettingsPageViewModelTests` (built-in/installed-pack
      splitting, current-language preselection, available-pack
      loading, `ApplyLanguageCommand`'s `CanExecute`/persist/
      restart-required behavior for both a language change and a
      same-language no-op, and both `DownloadOrInstallCommand`/
      `RemovePackCommand` surfacing `NotSupportedException` as a
      status message instead of throwing through the UI thread).
      `ArchitectureTests` (4, unchanged) still pass, confirming the
      dependency-direction rules were not touched.
- [x] Runtime verified via UI Automation against the real running app,
      across three full launch cycles:
      - **First launch** (no `settings.json` present): defaults to
        Persian, `FlowDirection.RightToLeft` — sidebar mirrors to the
        right edge, breadcrumb reads right-to-left, all Shell chrome/
        Dashboard/Settings/nav content in Persian.
      - **Switch to English, Apply, Restart Now**: `settings.json`
        correctly written (`{"language":"en-US"}`) before restart;
        after relaunch, English + `FlowDirection.LeftToRight` —
        sidebar back on the left, breadcrumb "Home › Dashboard",
        every migrated string in clean English with no broken
        bindings.
      - **Switch to Arabic, Apply, Restart Now**: `settings.json`
        correctly updated (`{"language":"ar-SA"}`); after relaunch,
        Arabic + RTL again, confirming the flow round-trips through a
        second language change, not just Persian→English.
      - The navigation region's asymmetric `BorderThickness`
        (`0,0,1,0`, right-only in LTR) was confirmed to mirror
        automatically under both Arabic and Persian — WPF flips
        `Thickness` left/right the same way it flips `Grid` column
        order, so the border stays on the boundary between the nav
        region and content in both directions with no XAML change
        needed. This closes the open question left in `MainWindow.xaml`
        since Phase 05.
      - Settings cleaned back to no persisted language after
        verification, so the repo's default runtime state is
        untouched (first launch → Persian).
- [x] **One real bug found and fixed during this pass** (not a test
      failure — build and the full test suite were green throughout;
      this was only visible at runtime): `App.OnStartup` was
      `async void`. Its first `await` (`_host.StartAsync()`) returned
      control to WPF's `Application.Run()` before culture was set;
      `Application.Run()` then started the Dispatcher's main message
      loop, which captures its own `ExecutionContext` baseline at that
      moment — before `Thread.CurrentThread.CurrentUICulture` was
      changed. Every later `DispatcherOperation` replayed from that
      stale baseline: `CurrentUICulture` silently reverted to the OS
      default (`en-US`) partway through the very first session, on the
      same UI thread, despite having "already" been set — confirmed by
      instrumenting `Dispatcher.Hooks.OperationPosted`/`OperationStarted`
      (operations posted while `CurrentUICulture` was correctly
      `fa-IR` still *started* executing under `en-US`). Fixed by making
      `OnStartup` fully synchronous — blocking via
      `.GetAwaiter().GetResult()` through host-start, localization
      init, and culture-set — so nothing yields back to the framework
      until culture is durably set, before any Dispatcher loop begins
      pumping. A related bug surfaced by the same investigation: the
      Reports/AI Center placeholder modules' nav titles were
      registered via an eagerly-constructed
      `new PlaceholderModule(new ModuleMetadata(..., Strings.Nav_Reports, ...))`
      inside `ConfigureServices`, evaluating `Strings.Nav_Reports`
      before culture was set (every other module's title is a
      `private static readonly` field, lazily evaluated at first
      DI-construction time, which happens after culture is set) —
      fixed by registering them as DI factories
      (`services.AddSingleton<IModule>(_ => new PlaceholderModule(...))`)
      instead, deferring evaluation to first resolve. Both fixes
      re-verified clean across all three launch cycles above.
- [x] No changes to the Fluent 2 Design System — every migrated
      control reuses existing shared styles/tokens unchanged; only
      `Text=`/`Content=` values and `xmlns:loc` imports were added.
- [x] Clean Architecture boundaries unchanged — `Presentation.Localization`
      holds only interfaces/pure classes, `Shell.Localization` holds
      the only file-system-touching implementations, verified by the
      unmodified, still-passing `ArchitectureTests`. No Domain,
      Application, or Infrastructure code was touched.

## Approval

Approved by: <pending> — <date>
