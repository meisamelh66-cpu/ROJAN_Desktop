# ROJAN Customer — Android → iOS Blueprint v1

**Status:** Draft for review · **Date:** 2026-09-28
**Source of truth:** `meisamelh66-cpu/ROJAN_DesignLab_Main1` @ `main` `04f62cd` (Customer flavor, `ai.rojan.designlab`)
**Backend:** `ROJAN_Backend` (canonical, per `governance/02_BACKEND_AUTHORITY_RULES.md`) — production `https://api.rojanai.ir/`

This document is the plan for building a native iOS version of the ROJAN
Customer app. It is based on a read of the Android Customer code: about 290
Kotlin files and 28k lines, excluding the `manager/` and `reception/` packages.
The goal is **functional parity with the live Android Customer app**, without
copying its demo-only parts or its known workarounds. The iOS app is a client
under ROJAN Ecosystem Governance v2.0. It adds no backend behavior, changes no
API contract and does not work around RBAC.

---

## 1. Scope

**In scope:** everything the Customer flavor ships today:

- guest Explore
- OTP login (only when the user books)
- the dashboard
- salon, specialist and service detail
- the booking flow, from date and time through confirmation to success
- appointments: list, detail, cancel and reschedule
- favorites and followed salons
- profile, including avatar and cover media
- Beauty DNA
- public salon by slug
- the app-update gate
- all "coming soon" placeholders

**Out of scope:** the Manager and Reception flavors, any new backend endpoint,
payments (Android has no payment integration either; see §9), and the AI
recommendation engine (a `NoOp` provider on Android).

---

## 2. Platform stack mapping

| Concern | Android (today) | iOS (target) | Notes |
|---|---|---|---|
| Language | Kotlin 2.2 | Swift 6 (strict concurrency) | |
| UI | Jetpack Compose + Material3 | SwiftUI | No UIKit except small wrappers where needed (image picker, blur) |
| Min OS | API 24 | **iOS 17.0** | Needed for `@Observable`, `NavigationStack` path APIs, `.scrollPosition` |
| State | `ViewModel` + `StateFlow` / `mutableStateOf` | `@Observable @MainActor final class …Model` | One model per screen, same split as the Android VMs |
| Async | Coroutines, `viewModelScope` | `async/await`, `Task`, cancelled in `.task {}` | `.task` gives the same cancel-on-leave behavior as `viewModelScope` |
| Navigation | Navigation-Compose, string routes | `NavigationStack(path:)` + typed `enum Route: Hashable` | See §5 |
| DI | Manual (`BackendApiContainer`, factories) | Manual `AppEnvironment` container injected via `.environment` | No DI framework, same as Android |
| HTTP | Retrofit + OkHttp | `URLSession` + a thin `APIClient` | No Alamofire |
| JSON | kotlinx.serialization (`ignoreUnknownKeys`) | `Codable`, `JSONDecoder` (unknown keys are ignored by default) | Every optional field must be `Optional` (see §7) |
| Token storage | AES-GCM via Android Keystore → SharedPreferences | **Keychain** (`kSecClassGenericPassword`, `AfterFirstUnlockThisDeviceOnly`) | |
| Prefs | DataStore Preferences | `UserDefaults` (non-secret: `personId`, `activeSalonId`) | |
| Images | Coil | `AsyncImage` + a shared `URLCache` (≥100 MB disk) | Consider Nuke only if scroll performance needs it |
| Image upload prep | `ImageDownscale.kt` (EXIF-aware, JPEG q=80) | `PhotosPicker` → `CGImageSource` thumbnail (orientation applied) → JPEG 0.8 | Same longest-side bound as Android |
| Fonts | Vazirmatn (Regular/Medium/SemiBold/Bold) `res/font` | Same 4 TTFs, registered through `UIAppFonts` | |
| Build variants | flavors `dev` / `staging` / `production` | Xcode **configurations + `.xcconfig`**: Debug-Dev, Release-Staging, Release-Production | `API_BASE_URL` from xcconfig → Info.plist |
| Orientation | Locked portrait | `UISupportedInterfaceOrientations` = portrait only (iPhone) | No landscape layouts exist |
| Tests | JUnit, coroutines-test | XCTest / Swift Testing, `URLProtocol` stubs | |

**Why native SwiftUI rather than KMP or Flutter?** The Android code keeps
business logic in the ViewModels, and the UI is heavily custom (glass,
glow, motion). A shared-code layer would mean refactoring the Android app
first, which crosses the "architecture change → confirmation required" line
in the Android repo's `CLAUDE.md`. The domain layer that is actually portable
is small (≈1.7k lines) and simple to port by hand. If KMP is wanted later,
the §4 layering matches the Android layers one-to-one, so it can be added
without redesigning the iOS app.

---

## 3. Architecture and project layout

The iOS app keeps the Android layers so that the two codebases can be
reviewed side by side.

```
RojanCustomer/                         (Xcode project, single app target)
├── App/
│   ├── RojanCustomerApp.swift         ← MainActivity + RojanNavGraph root
│   ├── AppEnvironment.swift           ← BackendApiContainer (manual DI)
│   └── Config/ Dev.xcconfig Staging.xcconfig Production.xcconfig
├── Domain/                            ← domain/ (pure Swift, no SwiftUI/UIKit import)
│   ├── Booking/   BookingState, BookingEvent, BookingStep, BookingEngine, rules/
│   ├── Identity/  SessionState, PersonRole, CurrentUserIdentityContext
│   ├── Models/    Salon, Service, Specialist, Booking, TimeSlot, …
│   ├── Repositories/ protocols (SalonRepository, BookingRepository, …)
│   └── Util/      PhoneNumberNormalizer, PersianDigits, BookingDates
├── Data/                              ← data/
│   ├── Remote/    APIClient, Endpoints, DTOs/, APIError, TokenRefresher
│   ├── Local/     KeychainTokenStore, SessionStore (UserDefaults)
│   └── Repositories/ *RepositoryImpl
├── Presentation/                      ← presentation/ (one @Observable model per screen)
├── Screens/                           ← screens/ (SwiftUI views, grouped by feature)
│   ├── Splash/ Auth/ Dashboard/ Explore/ Search/ Salon/ Specialist/ Service/
│   ├── BookingFlow/ Appointments/ Profile/ Favorites/ BeautyDNA/ PublicSalon/
│   └── Common/ ComingSoonView, CustomerScaffold, BottomBar
├── DesignSystem/                      ← ui/ + screens/customer/hometheme/
│   ├── Tokens/ Colors, Typography, Spacing, Radius, Shadows, Motion
│   ├── Glass/ GlassSurface, PremiumMetallicBorder
│   ├── Background/ HomeBackground (navy/purple canvas + radial glows)
│   └── Components/ PremiumButton, RojanTextField, OtpField, States (Loading/Empty/Error), Shimmer, RemoteImage
└── Resources/  Fonts/, Assets.xcassets (logo, app icon), Localizable.xcstrings (fa), PrivacyInfo.xcprivacy
```

**Layering rules (carried over from Android):**

- `Domain` imports only Foundation.
- `Screens` never call `Data` directly. They go through a model.
- DTOs never leave `Data/Remote`. Repositories map them to domain types.

---

## 4. Feature and screen inventory

Each Android screen, its iOS counterpart, the backend it depends on, and its
**parity status**.

| # | Android screen (file) | iOS view | Backend calls | Status on Android → iOS action |
|---|---|---|---|---|
| 1 | `SplashScreen` | `SplashView` | runs session restore in parallel (§6.4) | Live → port (keep "hold splash until `ready`") |
| 2 | `CustomerHomeScreen` (route `explore`) | `ExploreView` | `GET /public/salons` (guest), `GET /salons` | Live → port. **Guest landing screen** |
| 3 | `CustomerDashboardScreen` (route `customer_home`) | `DashboardView` | `GET /bookings/mine` + name lookups | Live → port. **Logged-in landing screen**. The AI sections use a NoOp provider, so hide them rather than fake them |
| 4 | `SearchScreen` + `SearchModeTabs` (SERVICES / SALONS) | `SearchView` | salon browse `?name=` / `?search=` | Live → port |
| 5 | `SalonListScreen` (`salon_list/{serviceIds}`) | `SalonListView` | `GET /salons` paged | Live → port |
| 6 | `SalonDetailsScreen` | `SalonDetailsView` | salon, categories → services, specialists, working-hours; follow / favorite | Live → port (largest screen, 934 lines) |
| 7 | `PublicSalonScreen` (`public_salon/{slug}`) | `PublicSalonView` | `/public/salons/{slug}/…` | In-app only on Android → port, **and** add a Universal Link (§10.3) |
| 8 | `SpecialistProfileScreen` | `SpecialistProfileView` | `GET /salons/{sid}/specialists/{id}`, categories/services | Live. Shows a disclosed error without a `salonId` → port that behavior as-is |
| 9 | `ServiceDetailsScreen` | `ServiceDetailsView` | categories → services, searched for the id | Live → port |
| 10 | `SpecialistSelectionScreen` | `SpecialistSelectionView` | `GET /salons/{sid}/specialists` | Live → port |
| 11 | `BookingDateScreen` | `BookingDateView` | none (7-day rolling window) | Live → port (see §8 on calendars) |
| 12 | `BookingTimeScreen` | `BookingTimeView` | `GET …/available-slots` | Live → port. **Login gate is here** |
| 13 | `AuthScreen` (OTP: phone → code + optional name) | `AuthView` | `otp/request`, `otp/verify` | Live → port |
| 14 | `BookingConfirmationScreen` | `BookingConfirmationView` | summary lookups + `POST /bookings` with `Idempotency-Key` | Live → port. The payment picker is UI-only (§9) |
| 15 | `BookingSuccessScreen` | `BookingSuccessView` | — | Live → port |
| 16 | `AppointmentsScreen` | `AppointmentsView` | `GET /bookings/mine`, `PATCH …/cancel` | Live → port |
| 17 | `AppointmentDetailsScreen` | `AppointmentDetailsView` | `GET /bookings/{id}` + lookups | Live → port |
| 18 | `RescheduleAppointmentScreen` | `RescheduleView` | slots + `PUT …/reschedule` | Live → port |
| 19 | `FavoritesScreen` | `FavoritesView` | `GET /customer/favorite-salons` + `getSalon` each | Live → port |
| 20 | `FollowedSalonsScreen` | `FollowedSalonsView` | `GET /customer/followed-salons` + `getSalon` each | Live → port |
| 21 | `ProfileScreen` | `ProfileView` | `GET /users/me`, avatar/cover upload and delete, logout | Live → port |
| 22 | `BeautyDnaScreen` | `BeautyDNAView` | none. **In-memory only** (`InMemoryBeautyProfileRepository`) | Demo-grade → port the UI. Store nothing locally that would look persisted; see decision D4 |
| 23 | Waitlist, Wallet, Coupons, Membership, Loyalty, My Reviews, Beauty Timeline | `ComingSoonView(title:)` | none | Placeholders → port as placeholders with the same Persian titles |
| 24 | `AppUpdateGate` / `AppUpdateDialog` | `UpdateGate` | `GET /public/app-releases/{appId}/latest` | **Needs redesign for iOS** (§10.2) |

**Bottom tab bar** (`CustomerHomeTab`): HOME → Dashboard, SEARCH →
Explore, BOOKINGS → Appointments, FAVORITES → Favorites, PROFILE → Profile.
Use a custom glass bar, not `TabView`, so it matches the Android design (active
accent rose-gold `#E0A67A`). Each tab keeps its own state and scroll position,
which is what Android's `saveState/restoreState` does.

**Auth-guarded routes** (Android's `CustomerAccessGuard`): Appointments,
Appointment Details, Reschedule, Favorites, Followed Salons, Waitlist. On iOS,
if there is no `SessionState.loggedIn`, show `AuthView` instead and continue to
the original screen after login succeeds.

---

## 5. Navigation

```swift
enum Route: Hashable {
    case explore, dashboard, search
    case salonList(serviceIds: [String])
    case salonDetails(salonId: String, selectedServiceIds: [String] = [])
    case publicSalon(slug: String)
    case specialistProfile(specialistId: String, salonId: String?)
    case serviceDetails(serviceId: String, salonId: String?)
    case specialistSelection(salonId: String)
    case bookingDate, bookingTime, bookingConfirmation, bookingSuccess(bookingId: String)
    case auth(returnTo: AuthReturn)
    case appointments, appointmentDetails(id: String), reschedule(id: String)
    case favorites, followedSalons, profile, beautyDNA
    case comingSoon(ComingSoonFeature)
}
```

**Start destination.** Compute it **once**, after session restore finishes. It
is Dashboard if a validated user exists, otherwise Explore. Android fixed a bug
where recomputing it after a mid-booking OTP login reset the whole stack. On
iOS, never rebuild the root `NavigationStack` when the session changes; mutate
the path instead.

**Booking flow scope.** Android keeps a single `BookingViewModel` scoped to the
nested `booking_flow_graph`. It survives navigation inside the flow and is
recreated when the flow is entered again. On iOS:

- Create the `BookingFlowModel` when the flow starts and pass it down with
  `.environment`.
- Release it when the flow's root is popped.
- Also save it to `@SceneStorage` / `Codable` so it survives state
  restoration. This replaces Android's `SavedStateHandle`.

**Login during booking.** On BookingTime → Auth → success, pop Auth and go
straight to BookingConfirmation with the flow model unchanged. This is
Android's `popUpTo(AUTH) inclusive`.

---

## 6. Networking, authentication and session

### 6.1 API client

- Base URL comes from the xcconfig. `https://api.rojanai.ir/` for Production.
  A Staging URL is supplied at build time. Dev uses a per-developer
  `Dev.local.xcconfig` (gitignored), matching ADR-003.
- Fail fast on launch if the base URL is empty, as `NetworkConfig` does.
- Timeout: 30 s for request and resource (Android `NETWORK_TIMEOUT`).
- `Authorization: Bearer <access>` is attached when a token exists. This is
  Android's `AuthInterceptor`.
- Log method, path and status in DEBUG only, never bodies (Android uses
  `BASIC` in debug and `NONE` in release).

### 6.2 Error taxonomy (port `SafeApiCall.kt` exactly)

```swift
enum APIError: Error {
    case backend(status: Int, body: APIErrorBody?)   // non-2xx; body = {timestamp,status,error,errorCode?,message,path,traceId?}
    case timeout                                     // URLError.timedOut
    case offline                                     // other URLError
    case malformed(DecodingError)                    // contract drift → retryable error state, never a crash
    case unexpected(Error)
}
```

Treat `CancellationError` and `URLError.cancelled` as cancellation, not as
failure.

User-facing messages come from `ErrorMessages.kt`. Reuse these Persian strings
verbatim. Never show `body.message`, which is English and may contain ids.

| Case | Message |
|---|---|
| timeout | زمان اتصال به پایان رسید. لطفاً دوباره تلاش کنید. |
| offline | اتصال اینترنت برقرار نیست. لطفاً دوباره تلاش کنید. |
| malformed | پاسخ سرور قابل پردازش نبود. لطفاً بعداً دوباره تلاش کنید. |
| 400 | اطلاعات وارد‌شده نامعتبر است. لطفاً دوباره بررسی کنید. |
| 401 | برای این عملیات نیاز به ورود مجدد دارید. |
| 403 | اجازه دسترسی به این بخش را ندارید. |
| 404 | موردی یافت نشد. |
| 409 | این عملیات با وضعیت فعلی سازگار نیست. |
| 5xx | خطایی در سرور رخ داد. لطفاً بعداً دوباره تلاش کنید. |
| other status | خطایی در ارتباط با سرور رخ داد. |
| unexpected | خطایی غیرمنتظره رخ داد. |

### 6.3 Token refresh (port of `TokenAuthenticator`, single-flight)

```swift
actor TokenRefresher {
    private var inFlight: Task<TokenPair, Error>?
    func validToken(after failedToken: String?) async throws -> TokenPair {
        if let current = store.accessToken, current != failedToken { return store.pair! } // someone already refreshed
        if let inFlight { return try await inFlight.value }
        let task = Task { try await plainClient.refresh(store.refreshToken) } // client with NO refresher
        inFlight = task; defer { inFlight = nil }
        …
    }
}
```

Rules to keep exactly as Android has them:

1. Only **one** refresh runs at a time. Concurrent 401s wait for it, then retry
   with the new token. The backend rotates refresh tokens, so a second refresh
   would log the user out.
2. Retry the original request **once** at most.
3. Clear Keychain tokens and the persisted `personId` **only** when the refresh
   call itself returns **400/401/403**. Timeouts, offline, 5xx and malformed
   responses must leave the session intact.
4. The refresh request uses a client that has no refresher attached, so it
   cannot recurse.
5. Clearing the session publishes `loggedOut`, and the UI reacts right away.
   This is Android's `observePersonId()` → `AuthViewModel`.

### 6.4 Cold-start restore (port `SessionViewModel` + `AuthViewModel.restoreSession`)

1. Read `personId` from UserDefaults. It is only a claim.
   If `personId` exists but the Keychain has no tokens, clear `personId`
   and start as a guest. This happens after a device restore:
   UserDefaults comes back from backup, but `ThisDeviceOnly` Keychain
   items do not.
2. If it is present, `GET /users/me`, which refreshes the token if needed.
   - On success, the user is authenticated. Then call
     `GET /users/me/salon-access` to get the identity context. A failure there
     does **not** undo login.
   - On failure, clear the session **only** for 400/401/403, as above.
3. The splash stays up until step 2 finishes. Run it **during** the splash
   animation, not after, to match the Android performance fix.
4. Roles come from `/users/me/salon-access`. **Do not port**
   `DemoIdentityProvider` / `DemoSessionProvider`; see §11.

### 6.5 OTP login

- **Phone:** normalize to E.164 with `+98` (`0912…` → `+98912…`) before
  `POST /auth/otp/request`. The backend returns 400 for local formats.
  Accept Persian or Arabic-Indic digits in input and convert them to ASCII.
  Android does not do this; see §11.
- The response `{phoneNumber, expiresInSeconds, canResendAfterSeconds}` drives
  the resend countdown.
- **Verify:** `POST /auth/otp/verify {phoneNumber, code, fullName?}`. The name is
  optional and only used when the account is new. Collect it inline with the
  code, because there is no rename endpoint.
- Use `.textContentType(.oneTimeCode)` so iOS offers SMS code autofill. Android
  has nothing equivalent.
- Save the tokens to Keychain, `personId` to UserDefaults, then refresh the
  identity context.

### 6.6 Logout

Clear the Keychain tokens, `personId` and `activeSalonId`, and reset in-memory
state. There is no backend logout endpoint today.

---

## 7. API contract (Customer app surface)

All paths are relative to the base URL and prefixed `api/v1/`. Dates are
**naive ISO-8601 local date-times** (`2026-09-01T10:00:00`, no zone). Parse
and format them with a fixed `DateFormatter`
(`yyyy-MM-dd'T'HH:mm:ss`, `en_US_POSIX`, **`Asia/Tehran`**). Do not use the
device time zone, and do not use `ISO8601DateFormatter`. Token expiry
timestamps are opaque; store them but do not use them for logic.

| Area | Method + path | Auth | Notes |
|---|---|---|---|
| Auth | `POST auth/otp/request` | – | `{phoneNumber}` |
| | `POST auth/otp/verify` | – | → `AuthResponse {user, accessToken, accessTokenExpiresAt, refreshToken, refreshTokenExpiresAt}` |
| | `POST auth/refresh` | – | `{refreshToken}` → `AuthResponse` |
| | `GET users/me` | ✓ | `UserResponse {id, email?, phoneNumber?, fullName, role, avatarUrl?, coverUrl?}` |
| | `GET users/me/salon-access` | ✓ | identity context (owned salons, memberships, specialist links) |
| Media | `POST users/me/media/avatar` · `…/cover` | ✓ | multipart, one file part → `UserResponse` |
| | `DELETE users/me/media/avatar` · `…/cover` | ✓ | → `UserResponse` |
| Public | `GET public/salons?page&size&search&sortDirection` | – | Paged. **Only `id` and `name` are guaranteed**; everything else is optional |
| | `GET public/salons/{slug}` · `/categories` · `/categories/{cid}/services` · `/specialists` · `/specialists/{id}/available-slots` | – | |
| Salon | `GET salons?page&size&name&sortDirection` · `GET salons/{id}` | ✓* | |
| | `GET salons/{id}/categories` · `/categories/{cid}/services` · `/specialists` · `/specialists/{spid}` · `/working-hours` | ✓* | |
| Slots | `GET salons/{sid}/specialists/{spid}/available-slots?serviceId&date=yyyy-MM-dd&slotIntervalMinutes` | ✓* | → `[{start, end}]` |
| Bookings | `POST bookings` + header **`Idempotency-Key: <UUID>`** | ✓ | `{salonId, serviceId, specialistId, startTime, notes?}` |
| | `GET bookings/mine?page&size&status?` | ✓ | Paged `BookingResponse` |
| | `GET bookings/{id}` · `PATCH bookings/{id}/cancel` · `PUT bookings/{id}/reschedule {newStartTime}` | ✓ | |
| Relationship | `POST/DELETE customer/salons/{id}/follow` · `GET customer/followed-salons` | ✓ | |
| | `POST/DELETE customer/salons/{id}/favorite` · `GET customer/favorite-salons` | ✓ | |
| Release | `GET public/app-releases/{applicationId}/latest?versionCode` | – | see §10.2 |

✓\* = Android calls these through the authenticated client. **Before iOS
Milestone M2, confirm with System 1 which of these allow anonymous access.**
Android's guest Explore already uses the `public/` family.

**Codable rules:**

- Declare a field non-optional **only** when it is non-nullable in the backend
  contract. Android crashed on-device when `public/salons` returned a leaner
  shape than expected.
- Money (`price`) is a JSON number. Decode it as `Decimal`, not `Double`.
- `BookingResponse.service/specialist/customer` are optional enrichments that
  have not shipped yet. Never synthesize them.
- The paging envelope is `{content, page, size, totalElements, totalPages}`.
- `BookingStatus` is `PENDING | CONFIRMED | CANCELLED | COMPLETED`. Add a
  `case unknown(String)` fallback so a new status on the backend does not break
  decoding.

---

## 8. Booking flow (domain port)

Port `domain/booking/*` as pure Swift value types. Here is the whole logic:

```swift
struct BookingState: Codable, Equatable {
    var intent: BookingIntent = .unknown
    var salonId, specialistId, serviceId, packageId: String?
    var selectedDateKey: String?     // yyyy-MM-dd
    var selectedTime: String?        // HH:mm
    var promotionId, couponId: String?
    var paymentMethod: PaymentMethod = .wallet   // .wallet | .payAtSalon
}
// Next step: no service → SEARCH; no specialist → SPECIALIST; no date → DATE; no time → TIME; else CONFIRMATION.
// Completion rule: a time without a date is dropped.
// Events: SalonSelected, SpecialistSelected, ServiceSelected, PackageSelected, DateSelected,
//         TimeSelected, PromotionApplied, CouponApplied, PaymentMethodSelected, IntentDetected
```

**Flow:** Explore/Search → Salon → pick service(s) → Specialist → Date
(today plus 6 days) → Time (live slots, `slotIntervalMinutes=15`, the Android
default) → *(login gate)* → Confirmation → `POST /bookings` → Success.

**Rules to keep:**

- Build `startTime` as `"\(dateKey)T\(time):00"`.
- Generate the idempotency key **once per confirmation attempt**, and disable
  the button while the request is running.
- Show a 409 on create or reschedule as "slot no longer available". Clear the
  selected time and go back to the Time step.
- The confirmation summary finds the service by walking the salon's categories.
  There is no `GET service/{id}`. Cache categories and services for each salon
  in a small in-memory store, so the summary does not repeat the N+1 calls that
  Android makes.
- Fetch appointment-history lookups (salon and specialist names) **in parallel**
  with `withTaskGroup`. Android had to fix this after it ran them one by one.
- Cancel asks for confirmation first (`CustomerConfirmDialog` →
  `.confirmationDialog`).

**Dates.** Android labels days in the **Gregorian** calendar with Persian month
names ("۲ اکتبر"), and says openly that this is a simplification because it had
no Jalali library. iOS has `Calendar(identifier: .persian)` built in.
**Recommendation:** show Jalali labels on iOS ("امروز", "فردا", "شنبه ۶ مهر")
but always send Gregorian `yyyy-MM-dd` to the backend. This differs from
Android, so it needs sign-off (D2).

---

## 9. Payments

Android shows a payment method picker (Wallet / Pay at salon), but nothing is
wired to it. No payment API is called, and Wallet is "coming soon". On iOS:

- Port the picker **only if** product wants to keep that UI. The recommendation
  is to show "پرداخت در سالن" as the only enabled option, so the UI does not
  imply a wallet that does not exist (`governance/10_NO_FAKE_DATA_POLICY.md`).
- These are physical services, so any future payment is outside Apple IAP
  rules (guideline 3.1.3(e)), and a PSP/Shaparak web flow is allowed.

---

## 10. iOS-specific concerns

### 10.1 Distribution (**blocking decision D1**)

The product is aimed at Iran, and the Apple App Store is not available there.
Apple Developer Program enrollment from Iran is also restricted. The options
are listed in D1. This decision affects signing, the update gate, push
notifications and QA device provisioning, so it has to be made **before M0**.

### 10.2 App update gate

Android downloads and installs an APK, using `downloadUrl` and a `sha256`
check, and sends `versionCode`. iOS cannot install a binary itself.

- Call the same endpoint with iOS's own `applicationId` (for example
  `ai.rojan.customer.ios`) and `versionCode = CFBundleVersion`. **This needs a
  backend release record for the iOS app (System 1).**
- `forceUpdate` → full-screen blocking view. `updateAvailable` → a banner the
  user can dismiss.
- The CTA opens `downloadUrl` with `openURL`: the App Store / TestFlight URL or
  the distribution channel's page. The `sha256` and `fileSizeBytes` fields are
  ignored on iOS.
- If the check fails, the app continues silently. Android never blocks on an
  error here.

### 10.3 Deep links

Android has the `public_salon/{slug}` route in the app but no deep link yet. On
iOS:

- Add a Universal Link: `https://rojanai.ir/s/{slug}` (path to be confirmed).
  This needs an `apple-app-site-association` file on the web domain (System 1 /
  ROJAN_Web).
- Also add a custom scheme `rojan://salon/{slug}` as a fallback for QR codes.

### 10.4 Notifications

Android has no push notifications. Its reminders use `NoOpReminderScheduler`.
Do not add APNs in v1. Optionally, schedule **local** reminders with
`UNUserNotificationCenter` for confirmed bookings, for example 24 h and 2 h
before. This is purely client-side and within what a client may do. It still
counts as new behavior, so it is decision D5.

### 10.5 Compliance

- `PrivacyInfo.xcprivacy`: UserDefaults (reason CA92.1). No tracking.
- ATS stays on: the production API is HTTPS. Allow exceptions in the Dev config
  only, for LAN backends.
- `NSPhotoLibraryUsageDescription` is **not needed** because `PhotosPicker`
  runs out of process. Add `NSCameraUsageDescription` (Persian text) only if
  camera capture is added.
- Exclude Keychain items from backup (`ThisDeviceOnly`). This matches the
  Android backup-rules fix, where tokens do not restore to a new device.

---

## 11. Android behavior not to copy

| Android | Why | iOS |
|---|---|---|
| `DemoIdentityProvider` / `DemoSessionProvider` for roles | Hardcoded demo assignments | Take roles from `users/me/salon-access`. The Customer app sends a staff user to Explore or Dashboard as a customer; staff apps are separate |
| `InMemoryBeautyProfileRepository` | Lost on restart; no backend | UI only; mark it "پیش‌نمایش" or hide it (D4) |
| `NoOpAiRecommendationProvider` sections | Nothing to show | Do not render empty AI sections |
| Top Specialists opens a specialist without a `salonId` | The backend needs a salon scope | Only link to a specialist when the salon is known |
| Gregorian dates with Persian month names | No Jalali library on Android | Native Persian calendar (D2) |
| Phone input only accepts ASCII digits | Iranian keyboards type ۰–۹ | Convert Persian and Arabic-Indic digits before normalizing |
| `runBlocking` in the authenticator | OkHttp's API forces it | `actor` + `async` (§6.3) |
| N+1 category walk to find a service | No by-id endpoint | Cache per salon; ask System 1 for `GET salons/{sid}/services/{id}` (optional) |

---

## 12. Design system translation

The app uses a **dark navy and deep-purple canvas everywhere**
(`HomeBackgroundTheme` + `HomeColors`). The older warm-white baseline is legacy.
The glass mechanic, spacing rhythm, motion rules and RTL rules still apply.

### 12.1 Color tokens (`DesignSystem/Tokens/Colors.swift`)

| Token | Hex | Use |
|---|---|---|
| `navyBase` (RojanNavy) | `#18233A` | Canvas base |
| `deepPurple` | `#2B1F45` | Canvas gradient end |
| `glow` (RojanAIGlow) | `#7C4DFF` | Radial glow, 0.20 alpha |
| `primary` (VividPurple) | `#8E28E7` | Primary actions |
| `magenta` | `#FF4FA3` | Secondary glow, 0.10 alpha |
| `rose` | `#FF8FC8` | Accent |
| `lavender` | `#DCCBFF` | Soft accent |
| `blush` | `#FFC7DE` | Soft accent |
| `gold` (RatingGold) | `#FFB020` | Ratings |
| `roseGold` (PremiumBorderRoseGold) | `#E0A67A` | Active tab, premium border |
| `textPrimary` (WarmWhite) | `#FFFBFF` | Text on dark |
| `textSecondary` | `#CBBEE0` | |
| `textMuted` | `#9C8FB5` | |
| `error` | `#FF5C7A` | |
| `success` (StatusOnline) | `#16A34A` | |

The service-category gradients (Skin, Nails, Makeup, Hair) and the full
metallic-border set are copied as-is from `ui/theme/RojanTokens.kt`. Put every
color in an asset catalog or a `Color` extension, with **no literal hex in
views**. This is the same rule as Android's RQG #2.

### 12.2 Type, spacing, shape, motion

- **Type:** Vazirmatn at 400, 500, 600 and 700. Body text is 16 pt with a 26 pt
  line height, which is tuned for Persian. Port each `Type.kt` style to a
  `Font.custom(_:size:relativeTo:)` style so Dynamic Type works.
- **Spacing:** 4 / 8 / 16 / 24 / 32 / 48.
- **Radius:** small 16, card 32, pill 50, circle.
- **Touch target:** at least 48, and back buttons are 48.
- **Button height:** 64.
- **Shadows:** elevations 8.4, 18.9 and 25.2. Map them to `.shadow(radius:y:)`
  using the same colors.
- **Motion (ms):**
  - fast 150, quick 220, standard 280 (exit 160), reveal 420, slow 600
  - shimmer 1100, loading sweep 2500, ambient 4200
  - page slide 4 %, scale from 0.97, stagger 45, press scale 1.06
- **Reduced Motion:** honor `accessibilityReduceMotion`. Android's
  `RojanNavTransitions` does this.
- **Glass:** use `.ultraThinMaterial` over the dark canvas, add a 1 pt
  gradient stroke (the metallic border), and optionally a blurred accent glow
  under the card. Check contrast against the canvas. Material on a dark
  background can wash out, so use `.environment(\.colorScheme, .dark)` for the
  whole app.
- **Canvas:** a vertical gradient from navy to deep purple, plus two radial
  glows (the AI glow at 0.20, magenta at 0.10). Draw it once in the root
  `ZStack` and do not redraw it per screen.

### 12.3 RTL and Persian

- `CFBundleDevelopmentRegion = fa`. Put
  `.environment(\.layoutDirection, .rightToLeft)` and
  `.environment(\.locale, Locale(identifier: "fa_IR"))` at the root.
- Use `leading`/`trailing` everywhere. Directional icons (back chevrons) must
  flip. `Image(systemName:)` flips automatically for chevrons; custom assets
  need `.flipsForRightToLeftLayoutDirection(true)`.
- Numbers go through `NumberFormatter` with the `fa_IR` locale, which gives
  Persian digits. Android does this by hand with `toPersianDigits`.
- **Strings:** Android hardcodes Persian in Kotlin; its `strings.xml` holds only
  the app name. On iOS, put every string in `Localizable.xcstrings`, with `fa`
  as the only language at launch. This makes an English or Kurdish locale easy
  to add later.

---

## 13. Milestones

Each milestone ends with a build on a physical iPhone, screenshots in RTL, and
a review. This mirrors Android's RQG.

| M | Scope | Exit criteria |
|---|---|---|
| **M0** Foundation | D1 decided. Xcode project, 3 configs, fonts, color tokens, canvas, glass, buttons, text fields, state views, CI build (Xcode Cloud or GitHub Actions macOS) | Tokens screen matches Android side-by-side; CI green |
| **M1** Network + Auth | APIClient, error taxonomy, Keychain, `TokenRefresher`, session restore, Splash, OTP `AuthView` | Unit tests: concurrent 401s cause exactly 1 refresh; transient refresh failure keeps the session; 401 on refresh logs out |
| **M2** Discovery | Explore (guest), Search, SalonList, SalonDetails, SpecialistProfile, ServiceDetails, PublicSalon, follow/favorite | Guest can browse the production salon list; no crash on a lean `public/salons` row |
| **M3** Booking | Booking flow model, Specialist → Date → Time → login gate → Confirmation → Success | End-to-end booking against staging; idempotent on double-tap; 409 path handled |
| **M4** Account | Dashboard, Appointments (list, detail, cancel, reschedule), Favorites, Followed, Profile + media, Beauty DNA, Coming-soon routes, tab bar with state retention | Parity checklist (§4) is 100 % |
| **M5** Platform | Update gate (needs the backend iOS release record), Universal Links, privacy manifest, optional local reminders, accessibility (VoiceOver in Persian, Dynamic Type) | VoiceOver pass; AASA verified |
| **M6** Release | Signing through the chosen channel, TestFlight or equivalent pilot, smoke test that mirrors `CUSTOMER-REAL-DEVICE-SMOKE-TEST.md` | Pilot sign-off |

A rough estimate is 8–11 engineer-weeks for one experienced iOS engineer.
Most of the UI time goes to SalonDetails, Dashboard and the glass polish.

---

## 14. Testing strategy

- **Domain (Swift Testing):**
  - `BookingEngine` next-step table
  - completion rule (time without date is dropped)
  - phone normalizer (`0912…`, `+98…`, `۰۹۱۲…`)
  - date-key generation across the Tehran time zone and a DST-free calendar
  - Jalali label formatting
- **Data:**
  - `URLProtocol` stubs for each DTO, including a lean or unknown-field
    fixture per endpoint
  - error mapping for each status
  - `TokenRefresher` concurrency test with 10 parallel 401s → 1 refresh call
- **UI:** XCUITest smoke flows for guest browse → book → OTP (stubbed) →
  success, and appointments → cancel. Snapshot tests of key screens in RTL.
- **Contract drift:** a CI job that decodes recorded staging responses. This
  catches the same class of bug that crashed Android.

---

## 15. Open decisions

| ID | Decision | Owner | Recommendation |
|---|---|---|---|
| **D1** | iOS distribution channel for Iranian users: (a) App Store outside Iran plus TestFlight; (b) Iranian alternative stores (Sibche, Sibapp, Anardoni) with their signing model; (c) PWA via ROJAN_Web first | Owner / System 1 | Decide before M0. It sets bundle ID, signing, the update gate and support scope |
| **D2** | Jalali date labels on iOS while Android stays Gregorian | Product | Use Jalali on iOS and schedule the same change for Android |
| **D3** | Which `salons/…` read endpoints allow anonymous access (for guest SalonDetails) | System 1 | Confirm before M2 |
| **D4** | Beauty DNA: hide it, or ship it as an explicitly local preview | Product | Hide until a backend exists (no-fake-data policy) |
| **D5** | Local booking reminders (client-only) | Product | Yes, opt-in, as part of M5 |
| **D6** | iOS app-release record + `applicationId` on the backend for the update gate | System 1 | Needed for M5 |
| **D7** | Universal Link domain and path for public salons (AASA hosting) | System 1 / Web | `https://rojanai.ir/s/{slug}` |
| **D8** | Repository location for the iOS app | Owner | A new repo `ROJAN_iOS_Customer`, rather than inside the Android or Desktop repos |

---

## Appendix A — Android files to use as reference

| Area | Android file(s) |
|---|---|
| Routes and flow | `navigation/RojanDestinations.kt`, `navigation/RojanNavGraph.kt` |
| Auth / session | `presentation/auth/AuthViewModel.kt`, `presentation/session/SessionViewModel.kt`, `data/remote/TokenAuthenticator.kt`, `data/local/SecureTokenStore.kt` |
| Networking | `data/remote/SafeApiCall.kt`, `data/remote/*Api.kt`, `data/remote/dto/*`, `di/BackendApiContainer.kt` |
| Errors | `presentation/common/ErrorMessages.kt` |
| Booking | `domain/booking/**`, `presentation/booking/*`, `screens/bookingflow/*` |
| Dates | `domain/booking/RollingBookingDates.kt` |
| Design | `ui/theme/RojanTokens.kt`, `Type.kt`, `Dimensions.kt`, `RojanRadius.kt`, `Shadows.kt`, `ui/motion/RojanMotion.kt`, `screens/customer/hometheme/*` |
| Governance | `governance/06_CLIENT_RESPONSIBILITY_BOUNDARY.md`, `08_UPDATE_POLICY.md`, `10_NO_FAKE_DATA_POLICY.md` |
| QA references | `CUSTOMER-REAL-DEVICE-SMOKE-TEST.md`, `CUSTOMER-RC3-FINAL-RELEASE-AUDIT.md`, `RTL-COMPLIANCE-AUDIT-REPORT.md` |
