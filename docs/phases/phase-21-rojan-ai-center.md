# Phase 21 — ROJAN AI Center

**Status:** Awaiting Approval
**Completion:** 100%

## Objectives

Build the ROJAN AI Center: a cross-cutting AI module that composes
every existing business module (CRM, Booking/Calendar, Services,
Specialists, Inventory, Accounting, HR, Reporting) through abstraction
interfaces, without creating tight coupling to any of them. Provide a
Business Assistant chat experience, live Insights/Recommendations/
Business Health Score/Smart Notifications computed fresh from real
data (never persisted), a reusable Prompt System, a full Conversation
System, and a Provider abstraction with a genuine Mock implementation —
explicitly **no API keys anywhere** in this app; OpenAI/Anthropic/
AzureOpenAI/LocalModel providers are abstraction-only this phase.

## Deliverables

- [x] **Domain** (`Rojan.Desktop.Domain/AI`): nineteen files — five
      enums (`ConversationRole`, `AIProviderType`, `InsightCategory`,
      `InsightSeverity`, `RecommendationPriority`), eleven records
      (`ConversationMessage`, `ConversationSession`, `PromptTemplate`,
      `AIProviderConfiguration` — deliberately has no credential
      field, doc-commented as to why — `TokenUsageRecord`,
      `AISettings`, `AIInsight`, `AIRecommendation`, `SuggestedTask`,
      `SmartNotification`, `BusinessHealthComponent`,
      `BusinessHealthScore`), two static Domain-logic classes
      (`BusinessHealthCalculator` — weighted average of components,
      clamped 0-100, rounded to one decimal; `ConversationRules` —
      `MaxPinnedSessions = 10`, `CanPin`, `DeriveTitle` truncating a
      first message to a 60-character title), and `IAIRepository`.
      Same "dumb repository, no aggregation logic" shape every other
      module's repository interface follows — `IAIRepository`
      deliberately does **not** own the business data Insights/
      Recommendations are computed from (that stays in each source
      module's own repository, reached only through their
      already-published Application-layer query services); it owns
      exactly what is genuinely local to AI Center: conversation
      history, prompt templates, provider/model selection, token
      usage history, and feature settings.
- [x] **Infrastructure** (`Rojan.Desktop.Infrastructure/AI`):
      `FakeAIRepository` seeds two sample conversations (one pinned,
      with real seeded messages), one system-defined prompt template
      per `InsightCategory` (nine total, including a General
      fallback), a default Mock provider configuration, default AI
      settings, and two token-usage records, so History/Prompt
      Templates/Usage Dashboard all have real content on first launch.
- [x] **Application** (`Rojan.Desktop.Application/AI`): DTOs mirroring
      every Domain type (Application's own enum copies, same
      convention as `Dashboard.TrendDirection`/`Reporting.TrendDirection`),
      an internal `AIMapper`, and seventeen named services:
      - **Prompt System** — `IIntentClassifier`/`IntentClassifier`
        (keyword-based `InsightCategory` classification, General
        fallback), `IPromptTemplateRepository`/`PromptTemplateRepository`
        (thin read-through over `IAIRepository`'s template storage),
        `IContextProvider`/`ContextProvider` (Business Context block,
        composing `Reporting.IAnalyticsQueryService`, reused
        unchanged), `IAnalyticsContextProvider`/`AnalyticsContextProvider`
        (Analytics Context block, composing `Reporting.IKpiEngineQueryService`),
        and `IPromptBuilder`/`PromptBuilder` — the composition root
        that classifies intent, picks a template, fetches both
        context blocks, and returns a `PromptContextDto` covering
        every block this phase's spec requires (System/Developer/
        User/Business Context/Analytics Context/Language Context —
        supplied by the caller, never looked up directly, so
        `Application.AI` never depends on the Localization Platform's
        concrete services — /Session Context).
      - **Conversation System** — `IConversationManager`/`ConversationManager`
        (session CRUD, message append with session `UpdatedAt`
        touch, `TogglePinAsync` enforcing the 10-pin cap, title/
        content search, plain-text export, clear-unpinned-only) and
        `IAIHistoryService`/`AIHistoryService` (Recent/Pinned/Search —
        a read-only composition over `IConversationManager`, a
        distinct concern from driving the live Chat Window).
      - **Providers** (`Application.AI.Providers`) —
        `IAIProvider` (`ProviderType`, `CompleteAsync`, and a genuine
        `IAsyncEnumerable<string>` `StreamCompleteAsync` so the
        architecture is streaming-ready) and `MockAIProvider`, the
        only concrete implementation this phase ships: deterministic,
        template-based replies derived from the request's own
        content, real word-by-word streaming with a small artificial
        per-word delay, and a ~4-characters-per-token estimate. No
        network call, no API key, anywhere.
      - **Engines** — `IInsightEngine`/`InsightEngine` (one insight
        per KPI from `Reporting.IKpiEngineQueryService` plus one
        Commission insight from `HR.ICommissionQueryService`
        current-vs-previous-month, reusing `Domain.Reporting.TrendCalculator`
        directly — a deliberate, documented exception to "Application
        composes sibling Application interfaces only," justified as
        reuse of a stateless calculator rather than a repository/
        entity dependency; Trend/Risk/Opportunity/Info severity is
        one classification function, `ClassifySeverity`, not three
        separate engines — Flat → Info, ±≥10% Up → Opportunity, ±≥10%
        Down → Risk, otherwise → Trend), `IRecommendationEngine`/
        `RecommendationEngine` (recommendations from Risk/Opportunity/
        Critical insights, priority mapped from severity; Suggested
        Tasks from the High/Urgent subset, due 1 day out for Urgent,
        3 days out for High), `ISummaryEngine`/`SummaryEngine` (Daily
        Summary — business context narrative plus Risk/Opportunity/
        Critical highlights; Executive Summary — narrative plus the
        five highest-magnitude-change insights), `IBusinessHealthService`/
        `BusinessHealthService` (five weighted components — Revenue
        trend 30%, Appointment volume 20%, Customer retention 20%,
        Staff attendance 15%, Inventory health 15% — reduced through
        `Domain.AI.BusinessHealthCalculator`), and
        `INotificationInsightService`/`NotificationInsightService`
        (a thin Risk/Critical/Opportunity filter over `IInsightEngine`,
        one sentence per notification).
      - **Facade** — `IAIService`/`AIOrchestrator`, the module's
        composition root: `SendMessageAsync` persists the user
        message, builds the prompt via `IPromptBuilder`, flattens it
        to System/Developer/User provider messages, calls the active
        `IAIProvider`, formats the reply via `IResponseFormatter`
        (trims, collapses excessive blank lines, caps at 4000 chars),
        persists the assistant message, and records token usage via
        `ITokenUsageTracker` — all async, all cancellable.
        `StreamMessageAsync` runs the identical pipeline but yields
        the reply as it streams, persisting and recording usage once
        the stream completes.
      - **Configuration/Settings/Usage** —
        `IAIConfigurationService`/`AIConfigurationService` (Model
        Selector's data source; `GetAvailableProviderTypes()` lists
        all five provider types so the UI can show unimplemented ones
        as "coming soon" rather than not existing at all),
        `IAISettingsService`/`AISettingsService` (feature toggles —
        Insights/Smart Notifications/Daily Summary/Auto-Generate
        Recommendations), `ITokenUsageTracker`/`TokenUsageTracker`
        (Usage Dashboard's write+read surface).
- [x] **Presentation**: `AiCenterModule` replaces the `"ai-center"`
      placeholder one-for-one (the exact swap `ReportingModule` made
      for `"reports"` in Phase 20 — no other Shell change needed).
      `AiCenterPageViewModel` composes all thirteen of the above
      services (plus `ILocalizationService`, for the Prompt System's
      caller-supplied Language Context) into six sections, the same
      local-section-switcher shape `ReportingPageViewModel` uses:
      **Home** (Business Health Score, Daily Summary, Smart
      Notifications), **Chat** (the Business Assistant — resumes the
      most recently active session, or creates one; New Conversation;
      Send, disabled while a reply is in flight), **Insights**
      (Insight Dashboard — every category, severity-badged),
      **Recommendations** (Recommendations Panel plus the Action
      Center's Suggested Tasks), **History** (Search, Pinned/Recent
      Conversations with Pin/Unpin/Delete/Export, a Conversation
      Viewer export preview, Clear History), and **Settings**
      (feature toggles, Model Selector, Usage Dashboard, Prompt
      Templates).

## Migration Notes / Scope Boundaries

- **Compute fresh, never persist.** Insights, Recommendations,
  Suggested Tasks, Smart Notifications, and the Business Health Score
  are computed on every request from live sibling-module data — the
  same pattern Phase 20's `Reporting.AnalyticsSummary` established.
  `IAIRepository` only owns conversations, prompt templates, provider
  configuration, settings, and token usage — genuinely local state.
- **No API keys, anywhere.** `AIProviderConfiguration` carries a
  `ProviderType` and `ModelId`, never a credential. Only `MockAIProvider`
  has a real implementation this phase; OpenAI/Anthropic/AzureOpenAI/
  LocalModel exist as `AIProviderType` values and pass through
  `IAIConfigurationService.GetAvailableProviderTypes()` so the Model
  Selector can list them as not-yet-available, but no networking code
  exists for any of them.
- **Domain isolation, Application composition, one documented
  exception.** `Domain.AI` never references another Domain module.
  `Application.AI` composes sibling Application-layer interfaces
  (`Reporting.IKpiEngineQueryService`/`IAnalyticsQueryService`,
  `HR.ICommissionQueryService`) — never their repositories or Domain
  types — with one deliberate exception: `InsightEngine` calls
  `Domain.Reporting.TrendCalculator` directly rather than
  reimplementing trend math a third time, justified as reuse of a
  pure, stateless calculator rather than a repository/entity
  dependency.
- **Language Context is caller-supplied.** `Application.AI` never
  depends on the Localization Platform's concrete `ILocalizationService`
  — `LanguageContextDto` is a plain data shape a Presentation
  ViewModel fills in from whatever localization service it already
  has, keeping the two platforms decoupled.
- **First navigation is the slow one.** `AiCenterPageViewModel`'s
  initial load issues roughly a dozen real sequential service calls
  (Business Health Score, Daily Summary, Smart Notifications,
  Insights, Recommendations, Suggested Tasks, Prompt Templates,
  session list, provider configuration, settings, usage history) —
  genuinely computed against live Reporting/HR data, not fixtures, so
  first paint takes several seconds; a future pass could parallelize
  these with `Task.WhenAll` the same way Phase 20 fixed
  `AnalyticsAggregator`'s equivalent first-paint latency, but that
  optimization was out of scope for this pass.

## Risks

- **Streaming exists in the pipeline but not yet in the Chat UI.**
  `IAIService.StreamMessageAsync` and `MockAIProvider.StreamCompleteAsync`
  are both genuinely implemented (`IAsyncEnumerable<string>`,
  real word-by-word yield), but `AiCenterPageViewModel`'s Send command
  calls the non-streaming `SendMessageAsync` — the architecture is
  streaming-ready, wiring the Chat UI to consume the stream
  incrementally is future work.
- **Mock-only provider.** Every "AI" response in this phase is a
  deterministic, keyword-derived `MockAIProvider` reply. Genuinely
  useful conversational quality requires a real provider
  implementation behind the existing `IAIProvider` abstraction — no
  architecture changes needed, just a new class plus secure
  credential storage (explicitly out of scope this phase).
- **First-load latency on AI Center Home/Chat.** See Migration Notes
  above — acceptable against the fake in-memory repositories (single
  digit seconds), worth revisiting if seeded data volume grows.

## Validation Checklist

- [x] `dotnet build RojanDesktop.sln` — 0 warnings, 0 errors.
- [x] `dotnet test RojanDesktop.sln` — 876/876 tests passing (141
      new): `Domain.Tests` (+2: `BusinessHealthCalculatorTests` —
      weighted average, clamping, zero-weight/zero-component edge
      cases; `ConversationRulesTests` — pin cap, title derivation/
      truncation), `Application.Tests` (+14 test classes:
      `AIMapperTests` — every enum round-trips Domain↔Application;
      `ConversationManagerTests` — create/append/pin-cap-enforcement/
      delete-cascades-messages/search/export/clear-unpinned-only;
      `InsightEngineTests` — one insight per KPI plus Commission,
      severity classification across Flat/Up/Down × above/below the
      10% threshold, category filtering, every `KpiType`→`InsightCategory`
      mapping; `RecommendationEngineTests` — severity→priority
      mapping, suggested-task derivation and due-date ordering;
      `NotificationInsightServiceTests`; `BusinessHealthServiceTests` —
      five components present, low-stock reduces the Inventory
      component, summary text reflects the score band;
      `SummaryEngineTests` — Daily vs Executive highlight selection
      and five-item cap; `MockAIProviderTests` — deterministic replies,
      token estimation, genuine multi-chunk streaming, cancellation
      mid-stream; `IntentClassifierTests`; `ResponseFormatterTests` —
      trimming, blank-line collapsing, 4000-char truncation;
      `TokenUsageTrackerTests`; `AIConfigurationServiceTests`;
      `AISettingsServiceTests`; `AIOrchestratorTests` — full pipeline
      against a real `ConversationManager` + `MockAIProvider`,
      covering both `SendMessageAsync` and `StreamMessageAsync`),
      `Infrastructure.Tests` (+1: `FakeAIRepositoryTests` — seeded
      session/message/template/configuration/usage shape, delete
      cascades messages), `Presentation.Tests` (+1:
      `AiCenterPageViewModelTests` — Home load reaches `Loaded`,
      active-session auto-creation, Send appends both messages and
      tracks usage, New Conversation, Pin, Delete-falls-back-to-a-new-
      session, Search, Clear History keeps pinned sessions, Export,
      Save Settings, Save Model Configuration — all driven through a
      real `ConversationManager`/`AIHistoryService`/
      `TokenUsageTracker`/`PromptTemplateRepository`/
      `AIConfigurationService`/`AISettingsService` stack over a
      test-local in-memory `IAIRepository`, with only the
      cross-module analytical engines and `IAIService` stubbed).
      `ArchitectureTests` (4, unchanged) confirm `Domain.AI`/
      `Application.AI`/the new Presentation surface respect the same
      dependency-direction rules as every other slice.
- [x] Runtime verified end-to-end via UI Automation:
      - Navigated to AI Center → Home rendered a real Business Health
        Score (**79.9 / 100**, "Business health is solid, with some
        areas worth watching.") with five live components (Revenue
        trend 100, Appointment volume 100, Customer retention 71,
        Staff attendance 44, Inventory health 60), a Daily Summary
        narrative with real seeded-data numbers ($1,283.04 revenue, 7
        appointments, 71.4% retention, 4 low-stock items, 44.4%
        attendance), and seven Smart Notifications correctly
        severity-badged (Opportunity/Risk) from live KPI trends.
      - Chat resumed the most recently active seeded conversation
        (`FakeAIRepository`'s pinned session, already containing one
        seeded Q&A round), sent a new message
        ("How is revenue trending this month?"), and received a real
        `MockAIProvider` reply referencing the supplied business
        context and explicitly disclosing it is a mock response —
        both the new user message and the new assistant reply were
        persisted and rendered alongside the two pre-existing seeded
        messages.
      - Insights section listed every KPI-derived insight
        (Revenue/Appointment/Customer/Inventory/Payroll/Attendance/
        Commission) with real values, change percentages, and
        category/severity badges.
      - Recommendations Panel showed real recommendations derived
        from the Risk/Opportunity insights above, each with a
        priority, description, and suggested action.
      - History section showed the seeded pinned session under
        Pinned Conversations and both seeded sessions under Recent
        Conversations, each with working Pin/Unpin/Delete/Export
        actions.
      - Settings section showed all four feature toggles enabled, the
        Model Selector defaulting to Provider=Mock/Model
        Id=`rojan-mock-v1`/Enabled, and Save actions for both.
- [x] **One real bug found and fixed during this pass** (build and
      the full test suite were green throughout — visible only at
      runtime, the same class of issue Phase 20 also hit twice): the
      Home section's headline `TextBlock` for the Business Health
      Score used `Style="{StaticResource Rojan.TextStyle.Heading}"` —
      a resource key that does not exist anywhere in
      `Themes/Typography.xaml` (the actual keys are `Display`,
      `Title`, `SectionHeader`, `Subtitle`, `Body`, `Caption`). WPF's
      XAML compiler did not catch this at build time — `dotnet build`
      stayed 0 warnings/0 errors throughout — and it only surfaced as
      a cascade of `System.Windows.StaticResourceExtension` "Provide
      value... threw an exception" dialogs on first navigation to AI
      Center at runtime. Fixed by switching to
      `Rojan.TextStyle.Display`, the same style
      `Controls.Dashboard.KPIValue` already uses for its own headline
      number; re-verified clean via a full fresh runtime pass (see
      above — the Business Health Score now renders correctly).
- [x] No changes to the Fluent 2 Design System — every AI Center
      control reuses existing shared styles/tokens (`DashboardCard`,
      `DashboardWidget`, `Rojan.Style.Panel`, `Rojan.Style.ButtonPrimary`/
      `ButtonSecondary`, `Rojan.TextStyle.*`) unchanged; no new visual
      primitives were introduced.
- [x] Clean Architecture boundaries unchanged — `Domain.AI` has no
      outward dependency, `Application.AI` depends only on Domain plus
      its sibling modules' own Application-layer interfaces (never
      their repositories/Domain types, with the one documented
      `TrendCalculator` exception above), Presentation depends only on
      Application. Verified by the unmodified, still-passing
      `ArchitectureTests`. No existing module's Domain/Application/
      Infrastructure code was modified — AI Center only ever reads
      through already-published query services.

## Approval

Approved by: <pending> — <date>
