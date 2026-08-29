# ROJAN AI — TEAM 3 — PHASE 8.129 — SETTINGS UX VISIBILITY FIX — IMPLEMENTATION v1

**Type:** Implementation. XAML only. **No commit performed** (STOP — Phase 8.130 is the commit scope review).
**Branch:** `feature/team3-desktop-completion` · **Base HEAD:** `17306d9` (unchanged — nothing committed)
**Reference:** `ROJAN_PHASE8_128_POST_P2_CLOSURE_REVIEW_v1.md` (item E.1), the Phase 8.99 / 8.100 Settings-guard carve-out (`0260bc3`)

---

## A. PROBLEM

The Phase 8.99 Missing-Guard Settings carve-out (`0260bc3`) added failure handling to `ApplyThemeAsync`, `ApplyApiEnvironmentAsync`, `RefreshAvailablePacksAsync`, `DownloadOrInstallAsync`, `RemovePackAsync` — each assigns `Strings.Common_ActionFailedMessage` to the section's existing `*StatusMessage` property. But the 3 bound `TextBlock`s in `SettingsPage.xaml` were **visibility-gated on `Is*RestartRequired`**, which is only ever `true` after a *successful* theme/API/language change that needs a relaunch. On a **failure** the message string is set but `Is*RestartRequired` stays `false`, so the `TextBlock` stays `Collapsed` — the user sees nothing.

`AccountStatusMessage` (added in the same Phase 8.99 for `SignOutAsync`) already uses the correct pattern: `Visibility="{Binding AccountStatusMessage, Converter={StaticResource CollectionToVisibilityConverter}}"` — show whenever the string is non-empty.

---

## B. FILES CHANGED — 1

```
 src/Rojan.Desktop.Presentation/Views/Settings/SettingsPage.xaml | 52 +++++++---------------
 1 file changed, 16 insertions(+), 36 deletions(-)
```

**Not touched:** ViewModels, services, contracts, DI, `Strings.resx` / `.en` / `.ar`, Shell, navigation, tests, converters. No new files. `CollectionToVisibilityConverter` was **already** declared as a page resource (`SettingsPage.xaml:15`) and already used by `AccountStatusMessage` — no resource addition.

---

## C. CHANGE — 3 `*StatusMessage` `TextBlock` visibility gates

For each of the 3 status `TextBlock`s, the `<TextBlock.Style>` block carrying `Setter Visibility="Collapsed"` + `DataTrigger Binding="{Binding Is*RestartRequired}" Value="True" → Visible` was replaced with an inline
`Visibility="{Binding <StatusMessage>, Converter={StaticResource CollectionToVisibilityConverter}}"`
(and `Style="{StaticResource Rojan.TextStyle.Caption}"` moved from the deleted `BasedOn` onto the element — identical resolved style). A 2–3 line explanatory comment was added above each.

| # | Section | `TextBlock` `Text` binding | Old gate | New gate |
|---|---|---|---|---|
| 1 | Language / Packs | `StatusMessage` | `DataTrigger IsRestartRequired == True` | `Visibility={Binding StatusMessage, Converter=CollectionToVisibilityConverter}` |
| 2 | Theme | `ThemeStatusMessage` | `DataTrigger IsThemeRestartRequired == True` | `Visibility={Binding ThemeStatusMessage, Converter=…}` |
| 3 | API Environment | `ApiEnvironmentStatusMessage` | `DataTrigger IsApiEnvironmentRestartRequired == True` | `Visibility={Binding ApiEnvironmentStatusMessage, Converter=…}` |

`CollectionToVisibilityConverter` on a `string`: `Length == 0 → Collapsed`, non-empty → `Visible`, `null → Collapsed` (`Converters/CollectionToVisibilityConverter.cs:13,15`).

### Deliberately NOT changed

The three **"Restart Now" `Button`s** (`Settings_Language_RestartNow` / `Settings_Theme_RestartNow`, one per section) keep their `DataTrigger Binding="{Binding Is*RestartRequired}" Value="True"` gate — a relaunch button must only appear when a relaunch is genuinely pending, never on an arbitrary error message. The page-header architectural comment mentioning `IsThemeRestartRequired` is unchanged (still accurate — the service still raises the flag; the buttons still consume it).

---

## D. BEHAVIOUR — verified against the existing VM contract

The `SettingsPageViewModel` tests already pin the `*StatusMessage` semantics; the new gate is correct for every one:

| Scenario | `*StatusMessage` | `Is*RestartRequired` | Old visibility | New visibility |
|---|---|---|---|---|
| Apply succeeds, relaunch needed | `Strings.Settings_*_RestartRequired` (non-empty) — `SettingsPageViewModelTests:106/192/247` | `true` | Visible | **Visible** (unchanged) |
| Apply succeeds, no relaunch (same value re-selected) | `string.Empty` — `SettingsPageViewModelTests:121/206/263` | `false` | Collapsed | **Collapsed** (unchanged) |
| **Apply / pack op fails** | `Strings.Common_ActionFailedMessage` (non-empty) — `SettingsPageViewModelTests:276/288`, `…:133/144` | `false` | **Collapsed (the bug)** | **Visible (the fix)** |
| Pack download/remove hits `NotSupportedException` | the local "coming soon" string (non-empty) — `SettingsPageViewModelTests:133/144` | `false` | Collapsed | **Visible** |

No VM change, so every existing `SettingsPageViewModelTests` assertion is unaffected. WPF views carry no unit tests in this repo; the change is a pure binding swap onto an already-tested property.

---

## E. VALIDATION

| Gate | Expected | Actual (working tree = `17306d9` + this change) |
|---|---|---|
| `dotnet build -c Debug` (incl. XAML compile) | 0 / 0 | **Build succeeded. 0 Warning(s), 0 Error(s)** ✅ |
| Settings tests (`FullyQualifiedName~Settings`) | PASS | **34 / 34 PASS** ✅ |
| Full test suite | 2,715 / 2,715 | **2,715 / 2,715 PASS** (Failed 0, Skipped 0) ✅ |
| — Domain / Application / Presentation / Infrastructure / Shell | 456 / 791 / 772 / 609 / 80 | all ✅ |
| — ArchitectureTests | 7 / 7 | **7 / 7 PASS** ✅ |

Suite unchanged at 2,715 (XAML-only change, no test impact).

---

## STOP

Phase 8.129 implementation complete. Base HEAD `17306d9` unchanged (no commit). Build 0/0 (XAML compiled), **2,715 / 2,715** tests pass, Settings subset 34/34, Architecture 7/7.

**1 file changed — `SettingsPage.xaml` only.** The 3 section `*StatusMessage` `TextBlock` visibility gates were switched from `Is*RestartRequired` `DataTrigger` to a non-empty-string `CollectionToVisibilityConverter` binding (the pattern already used by `AccountStatusMessage`), so the failure text the Phase 8.99 Settings guard already sets (`Common_ActionFailedMessage` on Theme / API-env / pack-refresh / download / remove failure) is now actually shown. The 3 "Restart Now" buttons keep their `Is*RestartRequired` gate. No ViewModel / service / contract / DI / `.resx` / test change; `CollectionToVisibilityConverter` was already a declared page resource.

**Awaiting Phase 8.130 — Settings UX Fix Commit Scope Review.**
