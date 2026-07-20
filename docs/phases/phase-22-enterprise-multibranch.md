# Phase 22 — Enterprise Multi-Branch & Organization Platform

**Status:** Complete
**Completion:** 100%

## Objectives

Transform ROJAN Desktop into a multi-tenant platform: independent
Organizations, unlimited Branches per Organization, a shared
Enterprise Platform, Branch-level data isolation, Organization- and
Branch-level administration, and permission-aware navigation — while
every existing module (CRM, Booking, POS, Inventory, HR, Accounting,
AI Center, Reporting) continues to work exactly as before, with zero
breaking changes.

## Architecture

Follows the same Clean Architecture layering and the established
"interface in Presentation, concrete in Shell" pattern this codebase
already uses for Localization (`ILocalizationService`) and Theming
(`IThemeService`):

- **Domain** (`Rojan.Desktop.Domain/Organizations`) — pure records,
  enums, and a static Permission Engine. No I/O, no dependency on any
  other Domain module (`Branch.Manager` is a plain `string`, not an
  `HR.Employee` reference — the same cross-module isolation every
  other Domain module already follows).
- **Application** (`Rojan.Desktop.Application/Organizations`) — DTOs
  mirroring every Domain type, an internal `OrganizationMapper`
  (explicit member-for-member enum switch mapping, the same convention
  `AIMapper`/`ReportingMapper` use), and the module's services.
- **Infrastructure** (`Rojan.Desktop.Infrastructure/Organizations`) —
  `FakeOrganizationRepository`, an in-memory `IOrganizationRepository`
  seeded with two organizations and three branches.
- **Presentation** — `ICurrentSessionService` (interface only),
  `OrganizationPageViewModel`/`OrganizationPage` (the admin surface),
  and the permission-aware navigation additions to
  `ModuleMetadata`/`NavigationItem`.
- **Shell** — `CurrentSessionService` (the concrete, file-system-backed
  `ICurrentSessionService`), `OrganizationModule` (the sidebar entry),
  and `MainWindowViewModel`'s permission-filtered navigation plus the
  header Branch Switcher.

## Entities

- **`Organization`** — `Id, Name, LegalName, Logo, BrandColor,
  TaxInformation, Subscription (SubscriptionPlan), Status
  (OrganizationStatus), CreatedDate`.
- **`Branch`** — `Id, OrganizationId, Name, Code, Address, Phone,
  Email, Manager, TimeZone, Currency, Status (BranchStatus)`.
- **`BranchSettings`** — `BranchId, BusinessHours (Open/Close time),
  WorkingDays, VatPercentage, ReceiptSettings (Header/Footer/ShowLogo),
  AppointmentRules (MinNoticeHours/MaxAdvanceBookingDays/
  AllowSameDayBooking), NotificationSettings (Email/SMS enabled,
  ReminderHoursBeforeAppointment)`.

`FakeOrganizationRepository` seeds "ROJAN Beauty Group" (Downtown and
Uptown branches) and "Luxe Salon Collective" (one branch), each with
its own distinct `BranchSettings`, so the platform has real
multi-tenant content — and a real second organization to prove
isolation against — from first launch.

## User Scoping & Session

No real multi-user authentication exists in this desktop app, so "user
scoping" is modeled as a session-level context: every running session
has a Current Organization, an optional Current Branch, and a Current
`WorkspaceRole`. `ICurrentSessionService` (Presentation interface) /
`CurrentSessionService` (Shell implementation) persists this selection
to its own `%LocalAppData%\RojanDesktop\session.json`, defaulting to
the first seeded organization's first branch as `WorkspaceRole.PlatformOwner`
when nothing is persisted yet.

Unlike Language and Theme (which require a restart — StaticResource-
based theming can't hot-swap without a full DynamicResource
rearchitecture, out of scope here), switching Branch or Role is
**live**: `SessionChanged` fires immediately and any subscribed
ViewModel refreshes without a restart, because it's a data-scope
change, not a resource-dictionary swap.

## Permission Model

`Domain.Organizations.Permission` is a 23-member enum (`CustomerRead`,
`CustomerEdit`, `BookingCreate`, `InventoryEdit`, `AccountingView`,
`ReportingExport`, `HrManage`, `OrganizationManage`, `BranchManage`,
…). `WorkspaceRole` has 11 members (`PlatformOwner`,
`OrganizationOwner`, `OrganizationManager`, `BranchManager`,
`Reception`, `Specialist`, `Inventory`, `Accounting`, `Hr`, `Ai`,
`Support`).

`Domain.Organizations.RolePermissions` is the Permission Engine's core
logic — a static `WorkspaceRole -> IReadOnlySet<Permission>` mapping.
`PlatformOwner`/`OrganizationOwner` are granted every permission that
exists (`Enum.GetValues<Permission>()`, reflective — a new permission
is automatically granted to both without an edit here); every other
role has an explicit, reviewable set (e.g. `Reception` can read/edit
Customers and create/edit Bookings but cannot touch Inventory or
Accounting; `Specialist` can read Customers and edit Bookings but not
edit Customers).

`Application.Organizations.IPermissionEngine`/`PermissionEngine` wraps
this for Presentation consumption (`HasPermission`, `GetPermissions`).

## Navigation

`ModuleMetadata` gained one additive, optional trailing parameter:
`Permission? RequiredPermission = null`. Every module registered
before this phase constructs `ModuleMetadata(id, title, icon, order)`
unchanged and stays unconditionally visible — this is the entire
non-breaking mechanism; no existing module's registration needed to
change. Only `OrganizationModule` opts in, gated by
`Permission.OrganizationManage`.

`MainWindowViewModel.BuildVisibleNavigationItems()` filters every
registered module to those whose `RequiredPermission` is either unset
or granted to `ICurrentSessionService.CurrentRole`.
`RefreshNavigationItems()` rebuilds this list live whenever
`SessionChanged` fires (a branch or role switch), re-selecting the
current item if it's still visible or falling back to the first
visible one — the sidebar always reflects the active session, no
restart required.

## Multi-Branch Filtering / Repository Filtering

`IOrganizationRepository.GetBranchesAsync(organizationId)` is the
concrete "no cross-branch data leakage" demonstration this phase's
spec asks for: it always scopes by `organizationId` and never returns
another organization's branches. `OrganizationPageViewModel`'s
Branches section is driven entirely through this call, so selecting an
organization in the UI genuinely re-scopes what's shown.

**Scope boundary, documented deliberately:** this phase builds the
Organization/Branch platform as a complete, real, working new vertical
slice with genuine org-scoped filtering proven end-to-end
(repository → Application → Presentation → UI). It does **not**
retrofit organization/branch filtering into the 10+ existing stable
modules' repositories (Customers, Bookings, Inventory, Accounting,
HR, …) — that is out of scope for this pass, consistent with this
codebase's existing "foundation now, wire up later" precedent (see
Phase 21's AI Center Migration Notes for the same pattern). No
existing module's repository or query service was modified.

## Branch Switcher

Lives in the Shell header (`MainWindow.xaml`), bound to
`MainWindowViewModel.AvailableBranches`/`CurrentBranch`. Setting
`CurrentBranch` calls `ICurrentSessionService.SwitchBranchAsync`, which
live-switches (and re-scopes the owning organization, if different)
without a restart — `SessionChanged` refreshes the header and
navigation immediately.

## UI

`OrganizationPageViewModel`/`OrganizationPage` follow every other
multi-section module page's shape (the same local-section-switcher
pattern `ReportingPageViewModel`/`AiCenterPageViewModel` use): five
sections — Organizations (list/create), Branches (list/create, scoped
to the selected organization), Branch Settings (business hours,
working days, VAT, receipt, appointment rules, notifications),
Permissions (a read-only `WorkspaceRole -> Permission` reference grid
produced by `IPermissionEngine`), and Session (current org/branch/role
plus a role switcher). Uses only the shared `Rojan.Brush.*`/
`Rojan.Style.*`/`Rojan.TextStyle.*` design-system resources — no
hardcoded brushes — and includes an explicit Refresh command,
Loading/Empty/Error states with Retry (via the existing
`DashboardWidget` control), and a Last Updated indicator, per this
phase's UI requirement. Every string resolves through
`Localization.Strings` (fa-IR/en-US/ar-SA) — no hardcoded UI text.

## Testing

- **Permission Engine** — `Domain.Tests.Organizations.RolePermissionsTests`
  (owner roles get every permission; specific role/permission grant
  and deny assertions; every role includes `DashboardView`) and
  `Application.Tests.Organizations.PermissionEngineTests` (the
  Application-layer wrapper).
- **Repository Filtering** — `Infrastructure.Tests.Organizations.FakeOrganizationRepositoryTests`
  proves `GetBranchesAsync` never crosses organization boundaries, and
  that a created branch only appears under its own organization.
- **Organization Scoping** — `Application.Tests.Organizations.OrganizationQueryServiceTests`
  proves the same guarantee at the Application/DTO layer against a
  dedicated `StubOrganizationRepository`.
- **Branch Switching** — `Shell.Tests.Organizations.CurrentSessionServiceTests`
  (against a temp settings file via the internal path-overriding
  constructor, the same pattern `ThemeServiceTests`/
  `LocalizationServiceTests` use): first-launch defaults, live branch
  switching within an organization, live branch switching that
  re-scopes the organization, persistence across restarts, live role
  switching, and the unknown-branch-id failure path.
- **Navigation Generation** — `Shell.Tests.Navigation.MainWindowViewModelNavigationTests`
  proves unpermissioned modules stay always-visible, permissioned
  modules are hidden/shown per role, navigation refreshes live on a
  `SessionChanged` role switch, and the selected item falls back
  correctly when it becomes hidden.

All new tests pass; the full solution suite (922 tests across Domain,
Application, Infrastructure, Presentation, Shell, and Architecture
Tests) passes with zero failures and the solution builds with zero
warnings and zero errors.

## Migration Notes / Scope Boundaries

- **No retrofit of existing modules.** As noted above, org/branch
  filtering is proven on the new Organization module only; existing
  modules' repositories are unchanged.
- **Session, not authentication.** There is no login system in this
  desktop app; "user scoping" is a session-level Organization/Branch/
  Role context, matching the app's existing single-user-per-install
  model.
- **Additive-only navigation change.** `ModuleMetadata.RequiredPermission`
  defaults to `null`; every module registered before this phase is
  unaffected.
- **Icon glyphs.** Consistent with every other module registration in
  this codebase, `OrganizationModule` passes `string.Empty` for
  `IconGlyph` — no existing module supplies a literal glyph either.

## Runtime Verification

Verified via UI Automation against both the Debug build and the
Release self-contained publish: the sidebar shows "Organizations"
(permission-gated, visible to the default `PlatformOwner` session),
the header Branch Switcher lists Downtown/Uptown for the seeded
organization, the Organizations/Branches/Branch Settings/Permissions/
Session sections all render and navigate correctly under the Fluent 2
Premium theme and RTL Persian layout, and the Permissions grid's
displayed grants match `RolePermissions` exactly for every role.
