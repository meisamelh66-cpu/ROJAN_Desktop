# Phase 24 — Localization Audit Completion

**Status:** Complete
**Completion:** 100%

## Objective

Close out the two scope boundaries Phase 23 explicitly deferred
(`AiCenterPage.xaml`, `HrPage.xaml`) plus every remaining Domain enum
displayed via a bare `ToString()` binding anywhere in the app — the
last category of hardcoded-English-string gap this session's
localization audit set out to eliminate.

## AiCenterPage.xaml and HrPage.xaml

Both pages (the two largest hardcoded-string surfaces in the app, ~150
combined instances across 6 sections each) are now fully localized
following the same conventions as every other module page from
Phase 23:

- `~103` new `Strings.cs` properties / resx entries (`Ai_*` for AI
  Center, `Hr_*` for Staff & HR) across `Strings.resx` (fa-IR,
  default), `Strings.en.resx`, `Strings.ar.resx`.
- Aggressive reuse of existing `Common_*`/`Reporting_*` keys wherever
  the same word already had a translation (`Common_FullName`,
  `Common_Email`, `Common_Notes`, `Common_Date`, `Common_Type`,
  `Common_Description`, `Reporting_Pin`/`Reporting_Unpin` for the
  conversation-history Pin/Unpin buttons) rather than minting
  duplicates.
- Two new shared keys added along the way: `Common_Send`,
  `Common_Delete`, `Common_Enabled` (generic enough to reuse
  elsewhere, not AI/HR-specific).
- Both pages' doc comments updated to drop the "deferred" note and
  point here instead.

## The Enum-Display Gap, Completed

Phase 23 introduced `EnumLabelConverter` (`Rojan.Converter.EnumLabel`,
merged app-wide via `Controls.xaml`) and wired it into every enum
display it could find at the time. Completing AI Center and HR surfaced
five more enum types with a read-only display binding that Phase 23's
sweep hadn't reached, because they only appear inside these two pages
or in list items whose DataTemplate wasn't yet localized:

- `ConversationRole` (Chat message author), `InsightSeverity`
  (Smart Notifications / Insights), `InsightCategory` (Insights /
  Recommendations / Prompt Templates), `RecommendationPriority`
  (Recommendations / Suggested Tasks), `AIProviderType` (Usage
  Dashboard's per-record provider).
- `EmployeeRole`, `Department`, `EmploymentType`, `EmployeeStatus`,
  `AttendanceStatus`, `CommissionType`, `LeaveStatus` (every Employee/
  Attendance/Commission/Leave list row and the employee profile
  panel).

A second, wider sweep (`grep` across every `Views/*.xaml` for any
`{Binding *.Status}`/`.Type`/`.Role`/`.Department`/`.Category` not
already routed through the converter) caught three more sites Phase 23
had missed because they live in pages that were already "fully
localized" for their literal strings but still had one bare enum
binding each:

- `OrganizationPage.xaml` — the Permissions matrix's `Role` column and
  the current-session `CurrentRole` display (`WorkspaceRole`).
- `ReportingPage.xaml` — the Report Catalog's `Category` column
  (`ReportCategory`).
- `ServicePage.xaml` — the catalog list's and profile card's
  `Category` (`ServiceCategory`).

All five newly-touched enum types plus `WorkspaceRole`/
`ReportCategory`/`ServiceCategory` added 51 new `Enum_<MemberName>`
resx entries (e.g. `Enum_Stylist`, `Enum_FullTime`, `Enum_Trend`,
`Enum_PlatformOwner`) to the same shared, per-word key convention
Phase 23 established — `Strings.GetEnumLabel` already handles any
member name generically, so no new C# properties or converter code
were needed, only data.

## One More Fixed: ServicePage's Embedded "min"

Phase 23's own report flagged one deliberately-deferred instance:
`ServicePage.xaml`'s duration KPIValue used
`StringFormat={}{0} min}`, which can't route through `Strings` because
`KPIValue.Value` is a plain `string` property, not a `Run`-composed
`TextBlock` that could split the number and the word into two
bindings. Closed with a new `MinutesSuffixConverter`
(`Rojan.Converter.MinutesSuffix`, same app-wide `Controls.xaml`
registration as `EnumLabelConverter`) that formats
`"{value} {Strings.Common_MinutesShort}"` - mirrors what every other
page already does by splitting the number and the "min" word into
separate `Run`s, just packaged as a converter since this one site has
no `Run`s to split.

## Still Out of Scope (Unchanged From Phase 23)

- Editable status/type/method `ComboBox` pickers (Employee Role/
  Department/Employment Type selectors, AI Provider selector, Customer
  status editor, etc.) still render via WPF's default `ToString()` -
  fixing these needs a per-`ComboBox` `ItemTemplate`, not a binding-
  level `Converter` swap, and remains the one systematically-deferred
  category across the whole app.
- Fake-repository seed data (employee/customer/service names,
  descriptions, bios) is still not translated, per Phase 23's
  reasoning: it is realistic content, not the kind of "demo value"
  (`Sample Company`, `John Doe`) the original spec's Remove Demo
  Content section named.
- Gregorian-calendar date formatting is unchanged - still a distinct,
  larger decision (Jalali calendar conversion) than a string-
  localization pass.

## Testing

No new tests - this pass is XAML/resx-only (localization strings and
one presentation-layer converter), no business logic touched. Full
955-test suite passes unchanged, zero warnings, zero errors, verified
after every edit batch.

## Runtime Verification

Verified via UI Automation against the Debug build with the default
fa-IR session: launched the Shell, navigated to Staff & HR, Services,
and AI Center via the nav sidebar, and screenshotted each. Confirmed:
every section switcher, card title, form label, button, and KPI label
on both previously-deferred pages now renders in Persian; status/role/
department/category badges ("فعال", "مو", "۶۰ دقیقه") render correctly
through `Rojan.Converter.EnumLabel`/`Rojan.Converter.MinutesSuffix`.
Remaining English on screen is confined to the documented residual
categories above (ComboBox pickers, seed-data names/descriptions).
