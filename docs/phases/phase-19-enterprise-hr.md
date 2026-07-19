# Phase 19 — Enterprise Staff, HR & Commission Management

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Add a ninth real business module - Staff & HR - following the exact
Domain → Application → Infrastructure → Presentation vertical-slice
pattern established by every prior module (Phases 10–18). A genuinely
new sidebar entry (no "hr"/"staff" placeholder existed to swap - same
situation Calendar was in for Phase 15), not a placeholder replacement.
Integrates with Specialists, Booking, Calendar, and Accounting entirely
through their own published Application-layer interfaces - no changes
to Architecture, Navigation mechanics, the Design System, shared
controls, or any existing module's business logic.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/HR`): the widest single
      repository interface in this app, covering nine related aggregate
      types in one vertical slice - `Employee` (core identity/employment
      fields, `SpecialistId` a free-text cross-slice reference to
      `Domain.Specialists.Specialist`, empty for non-bookable staff),
      `EmployeeProfile` (extended bio/skills/emergency-contact detail,
      same "core record plus separate extended-detail aggregate" split
      as `Customers.Customer`/`CustomerNote`), `Shift` (a reusable
      template), `ShiftAssignment`, `Attendance`, `LeaveRequest`,
      `CommissionRule`, `CommissionTransaction`, `PayrollSummary`. Seven
      enums (`EmployeeStatus`, `EmploymentType`, `Department`,
      `AttendanceStatus`, `LeaveStatus`, `CommissionType`, plus
      `EmployeeRole` - not one of the explicitly enumerated types, added
      as the natural role vocabulary a staff roster needs). New
      `EmployeeStatusRules` (activate/deactivate/suspend transition
      guards), `AttendanceRules` (derives Present/Late from a shift
      start plus a grace window; validates check-out-after-check-in
      corrections), `CommissionCalculator` (percentage or fixed-amount
      commission math), `PayrollCalculator` (Base + Commission + Bonus -
      Deduction = Net) - genuine Domain rules, same pattern as every
      other module's `*Rules`/`*Calculator` classes. `IHrRepository`
      stays "dumb" (raw reads/writes only, every "get many" method
      returns the full set) - Application composes/filters, consistent
      with the "return the read-set, compose in Application" convention
      every prior module follows. Uses `decimal` for money (Base Salary,
      Commission, Bonus, Deduction, Net Salary), same justified
      departure from the display-only string-money convention Phase 18
      established for Accounting.
- [x] **Application** (`Rojan.Desktop.Application/HR`): ten services -
      `IEmployeeQueryService` (list/search/profile aggregate/dashboard
      summary)/`IEmployeeCommandService` (create + activate/deactivate/
      suspend, each enforcing `EmployeeStatusRules` + department
      assignment)/`IAttendanceQueryService` (today's roster + leave
      requests)/`IAttendanceCommandService` (record - deriving status
      from the employee's shift when not given explicitly - correct,
      request/approve/reject leave)/`IShiftQueryService`/
      `IShiftCommandService` (create shift, assign to employee)/
      `ICommissionQueryService`/`ICommissionCommandService`/
      `IPayrollQueryService`/`IPayrollCommandService` (sums the
      employee's commission transactions for the requested month/year
      via `IHrRepository` directly - within-slice, same convention as
      `Accounting.PaymentCommandService` depending on
      `IAccountingRepository` directly rather than a sibling service).
      The headline integration - `CommissionCommandService.GenerateCommissionsFromAccountingAsync`
      - depends on `Accounting.IInvoiceQueryService` and
      `Bookings.IBookingQueryService` (cross-slice, same composition
      reasoning as `BookingWorkflow.BookingWorkflowService` and
      `Accounting.InvoiceQueryService.GetCheckoutOptionsAsync`): it
      scans every invoice that is `Paid` or `PartiallyPaid`, skips any
      invoice already processed (idempotent) or with no booking behind
      it, resolves the booking's `SpecialistId` to a matching
      `Employee`, applies that employee's `CommissionRule` (a flat 10%
      default when they have none), and writes a
      `CommissionTransaction` via `CommissionCalculator` - reading
      Accounting and Bookings only through their own already-published
      query services, never modifying either module's code.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/HR`):
      `FakeHrRepository` - 20 seed employees (5 cross-referencing the
      real specialist ids already seeded in
      `Specialists.FakeSpecialistRepository`, "specialist-1".."specialist-5";
      the other 15 are non-specialist staff - reception, management,
      nails, skincare, massage - with every `EmployeeStatus` represented
      including `Suspended` and `OnLeave`), 5 extended profiles, 6 shift
      templates, 10 shift assignments spanning yesterday/today/tomorrow,
      9 attendance records for today plus history (covering Present/
      Late/Absent/Vacation), 4 leave requests (one of each `LeaveStatus`
      outcome plus a pending one), 4 commission rules, 4 pre-seeded
      commission transactions matching four real Accounting invoices
      that have a paid/partially-paid status and a booking behind them
      - deliberately leaving a fifth real one ("invoice-8", Priya Nair's
      corporate booking) unprocessed so
      `GenerateCommissionsFromAccountingAsync` has something genuine to
      generate live, and 2 historical payroll summaries. Registered in
      `AddInfrastructure()`.
- [x] **Presentation**: one `HrPage` - HR Dashboard KPI cards
      (Employees/Present Today/Late Today/On Leave/Payroll This Month/
      Commission This Month/Average Attendance) always visible at the
      top, a local section switcher (Employees/Attendance/Shifts/Leave/
      Commission/Payroll - no new navigation surface), each section a
      master list plus a minimal quick-add form (same "list + quick-add"
      shape every other module's page uses), and the selected employee's
      `EmployeeProfileViewModel` (detail, lifecycle actions, recent
      attendance, upcoming shifts, recent commissions) always visible on
      the right regardless of which section is active, since Employee is
      this module's central entity. `HrModule` is a genuinely new module
      registration (see `CalendarModule`'s Phase 15 precedent) - no
      placeholder existed for "hr"/"staff" to swap. No new Design System
      components - every card/widget/control reuses Phase 16/17A's
      Fluent styles unchanged.
- [x] Tests added across all five projects (see Validation Checklist).

## Risks

- **Commission generation is a manual trigger, not automatic.** A
  "Generate Commissions from Accounting" button on the Commission
  section calls `GenerateCommissionsFromAccountingAsync` on demand,
  rather than firing automatically whenever a payment is recorded in
  Accounting. This keeps HR fully decoupled from Accounting (no event
  system exists in this app yet to wire automatically without one module
  reaching into another's internals) and is idempotent to call
  repeatedly, but means commissions lag behind sales until someone (or a
  future scheduled job) triggers generation.
- **Default 10% commission rate when an employee has no rule.** A
  reasonable fallback so the generator never silently skips a real sale,
  but a product decision that may need revisiting (e.g. requiring an
  explicit rule before any commission is attributed).
- **Payroll generation does not check for duplicates.** Calling
  "Generate Payroll" twice for the same employee/month/year creates two
  `PayrollSummary` records rather than replacing the first - acceptable
  for this foundation phase, matching the "foundation only" payroll
  scope explicitly requested (no government payroll, no tax engine, no
  accounting export).
- **No repository interface split.** `IHrRepository` has the widest
  method count in this app across nine aggregate types, consistent with
  this codebase's "one repository interface per vertical slice"
  convention rather than splitting per aggregate.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` - 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` - 574/574 tests passed (125 new):
      Domain.Tests 110 (+30: record equality smoke coverage for all nine
      aggregate types, `EmployeeStatusRules` transition-guard coverage,
      `AttendanceRules` grace-window and correction-validity coverage,
      `CommissionCalculator` percentage/fixed-amount and rounding
      coverage, `PayrollCalculator` coverage), Application.Tests 186
      (+64: employee query/search/profile-aggregation/dashboard-summary
      coverage, employee lifecycle-transition coverage, attendance
      record/correct/leave-request/approve/reject coverage including
      shift-derived status, shift creation/assignment coverage,
      commission-rule coverage, and thorough
      `GenerateCommissionsFromAccountingAsync` coverage - matching
      invoice+booking+employee generates a commission, no rule falls
      back to the 10% default, already-processed invoices are skipped,
      invoices with no booking/not-yet-paid/no matching employee are all
      skipped, partially-paid invoices still generate - plus payroll
      generation and monthly-sum coverage), Infrastructure.Tests 114
      (+29: seeded-data smoke tests plus create/update round-trips for
      every aggregate type), Presentation.Tests 160 (+39: page/profile
      ViewModel load-state, search, section-switching, and every
      command's CanExecute/execution coverage including the live
      commission-generation command), ArchitectureTests 4 (unchanged -
      still passing, confirming HR follows the same dependency-direction
      and ViewModel-testability rules as every other slice).
- [x] Runtime verified via UI Automation against the real running app:
      navigated to the new "Staff & HR" sidebar entry; confirmed every
      HR Dashboard KPI matched seed data exactly ("Employees 20",
      "Present Today 3", "Late Today 2", "On Leave 1", "Commission This
      Month $42.12", "Avg. Attendance 77.8%"); confirmed the Employees
      section listed all 20 employees and selecting one (Jordan Lee)
      populated the right-hand profile panel with identity, bio, and
      lifecycle buttons; confirmed the Attendance section listed all 7
      of today's records with correct statuses/notes; confirmed the
      Shifts section listed all 10 seeded assignments across
      yesterday/today/tomorrow; confirmed the Leave section listed all 4
      leave requests with Approve/Reject buttons visible only on the one
      still `Pending`; confirmed the Commission section showed the 4
      pre-seeded rules and 4 pre-seeded transactions, then clicked
      "Generate Commissions from Accounting" and confirmed it correctly
      generated exactly one new transaction live - "Priya Nair ·
      Corporate Group Styling · Invoice invoice-8 · $62.21" (12% of that
      real Accounting invoice's real $518.40 total, resolved through its
      real booking's real specialist) - proving the full Booking →
      Specialist → Accounting → Commission chain works end-to-end
      against real cross-slice data; confirmed the Payroll section
      listed both seeded summaries with correct Base + Commission +
      Bonus - Deduction = Net breakdowns. One real bug was found and
      fixed during this pass: the six section-switcher buttons' selected-state
      `DataTrigger`s each tried to set their own `Style` property to
      swap between `ButtonPrimary`/`ButtonSecondary` looks, which WPF
      explicitly disallows ("Style object is not allowed to affect the
      Style property of the object to which it applies") and threw a
      `XamlParseException` -> `StackOverflowException` the first time
      any of those buttons rendered; fixed by having each trigger set
      the individual Background/Foreground/BorderThickness properties
      instead of swapping the whole style object. Re-verified clean
      after the fix.
- [x] No changes to the Fluent 2 Design System - `Themes/` files
      untouched except `Views.xaml`'s DataTemplate registry (the same
      one-line addition every prior module made); every HR control
      reuses existing shared styles/tokens unchanged.
- [x] Clean Architecture boundaries unchanged - `Domain.HR` has no
      outward dependency, `Application.HR` depends only on `Domain.HR`
      plus Accounting's and Bookings' own Application-layer interfaces,
      `Presentation` depends only on `Application.HR` - verified by the
      unmodified, still-passing `ArchitectureTests`. No existing
      module's Domain/Application/Infrastructure code was modified.

## Approval

Approved by: <pending> — <date>
