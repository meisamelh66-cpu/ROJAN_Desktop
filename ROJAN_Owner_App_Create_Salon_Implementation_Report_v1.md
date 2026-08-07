# ROJAN Public Launch Hardening — Phase 1.2: Owner App Create Salon Flow

**Scope:** Audit + implementation, per the approved `ROJAN_Public_Launch_Hardening_Plan_v1.md` §2.1 (Blocker 🔴: "no 'Create Salon' flow anywhere in the Owner App").
**Repository:** `ROJAN_Desktop` only (backend endpoint already exists and is unchanged).

---

## 1. Audit — Current State Before This Phase (verified directly, no assumptions)

Confirmed by direct inspection, not carried over from the earlier readiness audit:

- **Owner App:** exactly 4 salon-related files existed - `ISalonContextService` (read-only, resolves the *current* salon id for other modules), `BackendSalonContextService` (its implementation), `SalonResponse` (a read-only wire DTO), and `SalonHealthCard.xaml.cs` (a Dashboard widget, unrelated to creation). **No `Domain.Salons`, no `ISalonQueryService`/`ISalonCommandService`, no create-salon UI anywhere.**
- **Backend:** `POST /api/v1/salons` (`SalonController.create` → `CreateSalonUseCase`) is real, tested, and owner-authorized - confirmed by reading it fresh from `ROJAN_Backend` @ `6943986`. `CreateSalonRequest` requires `name`/`phone`/`address` (`@NotBlank`), `description`/`email` optional.
- **A caching bug that would have silently broken the fix:** `BackendSalonContextService` resolves `GET /api/v1/salons/mine` once and caches the result (including a `null` "no salon" result) for its entire singleton lifetime. Without a fix, an owner could successfully create a salon through a new UI and every other already-loaded module (Customers, Services, Specialists, Calendar, Bookings, Dashboard) would still believe they own none until an app restart. This is addressed in §2.2 below - found during audit, not assumed.
- **Registration mechanism confirmed via `CalendarModule`'s own precedent:** "adding a module anywhere in the composition root is the only step required for it to appear" - no `MainWindowViewModel`/`NavigationService`/`ModuleRegistry` architecture change needed to add a new page.
- **`DashboardWidget` behavior confirmed by reading its source:** its `Empty` state renders a *generic* placeholder message, replacing whatever content it wraps - meaning a naive "State = Empty for no salon" design would have silently discarded the create form. This shaped the ViewModel design in §2.3.

---

## 2. What Was Implemented

### 2.1 Full Clean Architecture vertical slice for Salon

Following the exact three-layer pattern every other module (Customers, Services, Specialists, Bookings) already uses:

- **Domain:** `Salons.Salon` (plain record, no invariants to enforce - matches the backend's own lack of business rules beyond required-field validation), `Salons.ISalonRepository` (`GetMineAsync`, `CreateAsync`) - deliberately *not* `salonId`-scoped like every other Domain repository, since this repository is how a caller discovers which salon(s) they own in the first place.
- **Application:** `SalonDto`, `ISalonQueryService`/`SalonQueryService` (`GetMySalonAsync` - first salon if the owner has more than one, matching `ISalonContextService`'s own documented "no salon-switcher UI yet" limitation), `ISalonCommandService`/`SalonCommandService` (`CreateSalonAsync`).
- **Infrastructure:** `BackendSalonRepository` - calls `GET /api/v1/salons/mine` and `POST /api/v1/salons` directly via `IApiClient`, no `salonId` dependency (would be circular). New `CreateSalonRequest` wire contract added to `Api.Contracts`, matching ROJAN_Backend's DTO field-for-field.

**Deliberate deviation from every other module's convention:** `SalonCommandService` is **not** permission-gated (every other `*CommandService` wraps in a `*PermissionGate` checking the local `Permission`/`WorkspaceRole` system). Reasoning, documented in the class's own doc comment: that permission system is keyed off the local, still-fake Organization/Branch/role model, which a brand-new, salon-less owner may not have a meaningful role within yet. Salon ownership on ROJAN_Backend is a property of the signed-in account itself, resolved independently of the local role system - exactly the same reasoning `ISalonContextService` already documents for salon *reads*. Gating salon *creation* behind the local role system would risk locking a real owner out of the one action that unlocks every other already-gated feature.

### 2.2 Cache-invalidation fix (the bug found during audit)

- `ISalonContextService` gained one new member: `void Invalidate()`, with a **default no-op body** (C# 8+ default interface method) - every existing test double implementing this interface across the codebase keeps compiling unchanged; only `BackendSalonContextService` overrides it with a real implementation (resets its cached id/resolved-flag).
- `SalonCommandService.CreateSalonAsync` calls `salonContextService.Invalidate()` immediately after a successful create, so the very next read from *any* module picks up the new salon without an app restart.
- Verified with a dedicated regression test proving the bug is real without the fix (`GetSalonIdAsync_WithoutInvalidate_NeverSeesASalonCreatedAfterTheFirstResolution`) alongside tests proving the fix works (`Invalidate_ThenGetSalonIdAsync_ReResolvesFromTheBackend`, `Invalidate_ThenGetSalonIdAsync_CallsTheBackendAgain`).

### 2.3 Presentation: `SalonPage` / `SalonPageViewModel`

Not a list-plus-detail page like most modules - shows exactly one of two `DashboardCard` panels (read-only salon summary, or the create form), switched by a `HasSalon`/`NeedsSalon` boolean pair, the same "boolean-driven content swap" shape `CalendarPageViewModel.IsDayView`/`IsWeekView` already established.

**Deliberately does not use `DashboardState.Empty`** for "no salon yet" - per the audit finding above, that would have been swallowed by `DashboardWidget`'s generic empty-state rendering. `State` here only ever reflects the *load* outcome (Loading/Loaded/Error); "loaded, and it turned out there's no salon yet" is still `Loaded`, with `HasSalon`/`NeedsSalon` as the separate signal the view switches on.

The create form's own submit failure (e.g. a 400 from backend validation) is tracked by a **separate** `CreateErrorMessage`/`HasCreateError`, not `ErrorMessage`/`State` - a failed create attempt leaves the form visible with the user's typed input intact for correction, rather than flipping the whole page into the generic Error state.

**A real bug caught by its own test, fixed before this report:** the first draft trimmed `Name`/`Phone`/`Address` before sending but not `Description`/`Email`, sending leading/trailing whitespace straight through for the two optional fields. Caught by `CreateSalonCommand_Success_PopulatesSalonAndClearsCreatingState` failing on a genuine assertion (not a test bug), fixed by trimming all five fields consistently.

### 2.4 Module registration

`SalonModule` (`Order: 1`, right after Dashboard's `0`, no `RequiredPermission` - unconditionally visible, same reasoning as the ungated command service) registered in `App.xaml.cs` alongside every other module, `SalonPageViewModel` registered transient in Presentation DI, `ISalonRepository`/`BackendSalonRepository` and `ISalonQueryService`/`ISalonCommandService` registered in Infrastructure/Application DI respectively, and a `SalonPageViewModel` → `SalonPage` `DataTemplate` added to `Views.xaml` - no change to `MainWindowViewModel`, `NavigationService`, or `ModuleRegistry` itself, confirmed unnecessary by `CalendarModule`'s own precedent.

### 2.5 Localization

Added `Nav_Salon`, `Salon_Title`, `Salon_Subtitle`, `Salon_YourSalon`, `Salon_CreateSalon`, `Salon_CreatePrompt`, `Common_Address`, `Common_Active`, `Common_Inactive` to all three locale files (`Strings.resx` default/Persian, `Strings.en.resx`, `Strings.ar.resx`) plus the hand-maintained `Strings.cs` accessor, following the codebase's own existing three-locale convention exactly.

---

## 3. What Was Not Implemented (explicitly out of scope, not forgotten)

- **Update/deactivate salon** - `PUT`/`DELETE /api/v1/salons/{id}` already exist on the backend but have no Owner App consumer. Only creation was in this phase's scope.
- **Multi-salon switcher UI** - the pre-existing, already-documented Phase 1 limitation (`ISalonContextService` always uses "the first salon returned"). This phase's `SalonQueryService.GetMySalonAsync` follows the same convention, not a new limitation.
- **Proactive startup redirect to the Salon page.** Considered and deliberately not implemented: `MainWindowViewModel` is a large, actively-used orchestrator (navigation, workspaces, branch switcher, notifications, command palette) with delicate constructor-time sequencing (`SelectedNavigationItem` is set synchronously before any async check could run; `WorkspaceHost.InitializeAsync` and `ApplyPrimaryModuleFromWorkspace` have documented ordering dependencies). Weaving a salon-less-owner redirect into that sequencing risked introducing a subtle race condition in code this phase's scope did not require touching. **Residual gap, confirmed by reading `DashboardPageViewModel`'s own error handling:** a brand-new, salon-less owner still lands on Dashboard by default (the first nav item), which will show its standard Error-state widget (the "does not manage any salon yet" `ApiException` message) with a Retry button that will keep failing until they notice the new "Salon" item - now second in the sidebar, immediately below Dashboard - and navigate there themselves. This is a real, honest UX gap, not hidden here: recommended as a fast-follow (a lightweight `MainWindowViewModel` startup check redirecting to Salon specifically when salon-less) rather than bundled into this phase.

---

## 4. Files Changed

**New:**
- `src/Rojan.Desktop.Domain/Salons/Salon.cs`, `ISalonRepository.cs`
- `src/Rojan.Desktop.Application/Salons/SalonDto.cs`, `SalonMapper.cs`, `ISalonQueryService.cs`, `SalonQueryService.cs`, `ISalonCommandService.cs`, `SalonCommandService.cs`
- `src/Rojan.Desktop.Infrastructure/Salons/BackendSalonRepository.cs`
- `src/Rojan.Desktop.Presentation/ViewModels/Salons/SalonPageViewModel.cs`
- `src/Rojan.Desktop.Presentation/Views/Salons/SalonPage.xaml`, `SalonPage.xaml.cs`
- `src/Rojan.Desktop.Presentation/Modules/SalonModule.cs`
- Tests: `Application.Tests/Salons/{StubSalonRepository,StubSalonContextService,SalonQueryServiceTests,SalonCommandServiceTests}.cs`, `Infrastructure.Tests/Salons/BackendSalonRepositoryTests.cs`, `Presentation.Tests/Salons/{StubSalonQueryService,StubSalonCommandService,SalonPageViewModelTests}.cs`

**Modified:**
- `src/Rojan.Desktop.Application/Salons/ISalonContextService.cs` (`Invalidate()`, default no-op)
- `src/Rojan.Desktop.Infrastructure/Salons/BackendSalonContextService.cs` (real `Invalidate()`)
- `src/Rojan.Desktop.Application/Api/Contracts/SalonResponse.cs` (added `CreateSalonRequest`)
- `src/Rojan.Desktop.Application/DependencyInjection/ServiceCollectionExtensions.cs`, `Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`, `Presentation/DependencyInjection/ServiceCollectionExtensions.cs` (new registrations)
- `src/Rojan.Desktop.Shell/App.xaml.cs` (module registration)
- `src/Rojan.Desktop.Presentation/Themes/Views.xaml` (DataTemplate)
- `src/Rojan.Desktop.Presentation/Localization/{Strings.resx,Strings.en.resx,Strings.ar.resx,Strings.cs}`
- `tests/Rojan.Desktop.Infrastructure.Tests/Salons/BackendSalonContextServiceTests.cs` (added `Invalidate()` coverage to the pre-existing test file)

**Untouched by design:** `MainWindowViewModel`, `NavigationService`, `ModuleRegistry` - confirmed unnecessary per §2.4.

---

## 5. Test Evidence

```
dotnet build RojanDesktop.sln
Build succeeded. 0 Warning(s), 0 Error(s).

dotnet test RojanDesktop.sln
Rojan.Desktop.Domain.Tests............. 454/454 passed
Rojan.Desktop.ArchitectureTests........   6/6   passed
Rojan.Desktop.Presentation.Tests....... 466/466 passed   (12 new: SalonPageViewModelTests)
Rojan.Desktop.Shell.Tests..............  45/45  passed
Rojan.Desktop.Application.Tests........ 713/713 passed   (11 new: SalonQueryService/SalonCommandServiceTests)
Rojan.Desktop.Infrastructure.Tests..... 542/542 passed   (9 new: BackendSalonRepositoryTests; 3 new: BackendSalonContextServiceTests.Invalidate coverage)
-----------------------------------------------------------------
Total: 2,226/2,226 passed, 0 failed
```

Two real bugs were caught by this test suite during implementation (not left in): the whitespace-trimming gap in §2.3, and the cache-invalidation regression proven by `GetSalonIdAsync_WithoutInvalidate_NeverSeesASalonCreatedAfterTheFirstResolution` (which documents the bug this phase fixes, run against the fixed code to confirm the *other* two Invalidate tests actually exercise something real).

---

## 6. Manual Verification

Not performed - this environment has no display/WPF runtime available (headless dev sandbox, consistent with prior phases in this session). All resource keys referenced in the new XAML (`Rojan.Brush.Error`, `Rojan.TextStyle.Display`, `StatusPill`/`EntityAvatar`/`SectionHeader`/`DashboardCard`/`DashboardWidget` control usage) were individually verified against their actual definitions before use, not assumed from memory - one incorrect assumption (a nonexistent `Rojan.Converter.BoolToSeverity` converter, and `BoolToVisibilityConverter` bound directly to string properties, which only ever evaluates `value is true`) was caught and corrected during this pass, not left for runtime discovery. **Recommend a manual WPF smoke test of the Salon page (both no-salon and has-salon states) before this ships**, since `StaticResource` lookups are not validated at compile time the way `x:Static` is - the build succeeding does not guarantee every visual renders correctly, only that every C#/`x:Static` reference resolves.

**No other hardening item from `ROJAN_Public_Launch_Hardening_Plan_v1.md` was started.**
