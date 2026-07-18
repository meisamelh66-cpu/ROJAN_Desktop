# ROJAN Desktop — Desktop Shell Architecture

> **⚠ Superseded by the phase-gated SDLC.** Written before the formal phase
> process existed. Desktop Shell is now **Phase 05**, which doesn't start
> until Phases 01–04 are each individually approved. This draft's content
> (composition root, navigation, exception handling, logging bootstrap) is
> good raw material and will be formally reissued as the Phase 05
> deliverable when that phase begins — some of it (DI container choice,
> layer boundaries) actually belongs to **Phase 02 — Enterprise
> Architecture** instead, and will move there. Kept for reference, not an
> active or approved deliverable. See `docs/phases/phase-01-foundation.md`
> for what's actually in force right now.

**Status:** Preliminary draft — content pending reallocation to Phase 02 / Phase 05.

## 1. Purpose

The **Shell** is the outermost layer of the application: the WPF executable
that starts the process, builds the dependency-injection container, wires
every abstraction to its concrete implementation, opens the main window, and
hosts navigation between features. It owns *infrastructure-of-the-UI*
concerns — composition, lifecycle, chrome, cross-cutting error handling —
and deliberately owns **zero business logic**. If a class in the Shell
project needs to know a business rule to do its job, that's a sign that
logic belongs in `Application` or `Domain` instead.

## 2. Responsibilities (and explicit non-responsibilities)

| In scope for Shell | Explicitly NOT in scope for Shell |
|---|---|
| Composition root / DI container bootstrap | Business rules, validation logic |
| App lifecycle (`OnStartup`/`OnExit`) | Data persistence implementation |
| Main window chrome, app-level navigation host | Domain entities / value objects |
| Global exception handling | Use-case / application-service logic |
| Logging bootstrap (sinks, enrichers) | Direct database or file-format knowledge |
| Configuration loading (`appsettings.json`, user settings) | — |
| Theming / resource dictionary bootstrap | — |
| Single-instance enforcement | — |
| Splash screen / startup sequencing | — |

This table is the primary thing to review: if you disagree with what's in
or out of scope here, that's the highest-leverage feedback to give before
anything is built, since every other layer's boundary is defined in
opposition to this one.

## 3. Solution structure

```
RojanDesktop.sln
├── src/
│   ├── Rojan.Desktop.Shell/            (WPF exe — composition root, App.xaml, MainWindow)
│   ├── Rojan.Desktop.Presentation/     (class library — ViewModels, Views, Converters, Behaviors)
│   ├── Rojan.Desktop.Application/      (class library — use cases, CQRS, service interfaces, DTOs)
│   ├── Rojan.Desktop.Domain/           (class library — entities, value objects, domain services, repo interfaces)
│   ├── Rojan.Desktop.Infrastructure/   (class library — persistence, file system, logging setup, concrete services)
│   └── Rojan.Desktop.Common/           (class library — Result<T>, Guard clauses, extensions; zero business meaning)
├── tests/
│   ├── Rojan.Desktop.Domain.Tests/
│   ├── Rojan.Desktop.Application.Tests/
│   ├── Rojan.Desktop.Infrastructure.Tests/
│   ├── Rojan.Desktop.Presentation.Tests/     (ViewModel tests — no WPF Application needed)
│   └── Rojan.Desktop.ArchitectureTests/      (dependency-direction enforcement, see §9)
├── docs/
│   └── architecture/
├── Directory.Build.props        (shared MSBuild settings: Nullable=enable, LangVersion, analyzers)
├── Directory.Packages.props     (central NuGet version management)
├── .editorconfig
└── RojanDesktop.sln
```

### Why `Shell` and `Presentation` are separate projects, not one

This is the one structural decision most worth scrutinizing before
approval. Splitting them costs one extra project; the alternative (folding
ViewModels/Views into the executable project) is simpler on day one but
has real costs later:

- **ViewModel testability.** `Rojan.Desktop.Presentation` is a plain class
  library — `Rojan.Desktop.Presentation.Tests` can instantiate and test
  ViewModels with no WPF `Application`, no STA thread, no `Dispatcher`.
  If ViewModels lived in the exe project, that's still *technically*
  possible but far more fragile in practice (WPF exe projects pull in
  `Application`-level static state that leaks into test runs).
- **Enforceable boundary.** `Shell` referencing `Presentation` (rather than
  containing it) means an architecture test (§9) can assert "nothing in
  `Presentation` references `Shell`" — i.e. ViewModels can never
  accidentally reach into app-lifecycle/composition-root concerns. That
  assertion isn't expressible if they're the same project.
- **Future flexibility, not exercised now.** A second host (e.g. a
  design-time preview harness, or — hypothetically — a different shell
  entirely) could reuse `Presentation` unchanged. This isn't a planned
  requirement and shouldn't drive the decision on its own, but it's a
  natural side effect of the boundary above, not extra cost incurred to
  get it.

If you'd rather keep it as one project for simplicity and accept the
weaker test isolation, that's a reasonable call too — flag it and I'll
collapse them.

## 4. Composition root & dependency injection

**Decision: Generic Host (`Microsoft.Extensions.Hosting`) + `Microsoft.Extensions.DependencyInjection`, not Prism/MEF.**

`App.xaml.cs` builds an `IHost` in `OnStartup`, exactly the way an ASP.NET
Core `Program.cs` does:

```csharp
protected override async void OnStartup(StartupEventArgs e)
{
    _host = Host.CreateDefaultBuilder()
        .ConfigureAppConfiguration(ConfigureConfiguration)
        .ConfigureServices(ConfigureServices)
        .UseSerilog(ConfigureLogging)
        .Build();

    await _host.StartAsync();

    var mainWindow = _host.Services.GetRequiredService<MainWindow>();
    mainWindow.Show();

    base.OnStartup(e);
}
```

`ConfigureServices` is where every layer registers itself —
`Domain`/`Application` register their interfaces, `Infrastructure` registers
the concrete implementations behind those interfaces, `Presentation`
registers ViewModels (transient) and Views. Each project exposes its own
`IServiceCollection` extension method (`AddApplication()`,
`AddInfrastructure()`, `AddPresentation()`) so `Shell`'s `ConfigureServices`
reads as a short, declarative list — it does not itself know *how* each
layer wires its own internals.

**Why Generic Host over Prism or raw `ServiceCollection`:** Prism brings a
module system, region navigation, and a DI abstraction (`IContainerProvider`)
that's one more thing to learn on top of .NET's own DI — reasonable for
large plugin-style apps, but more machinery than this app needs today and a
dependency on Prism's own release cadence. Generic Host is the same pattern
every modern .NET backend already uses, gives configuration/logging/DI for
free in one coherent package, and keeps the door open to add Prism-style
modularity later *without* having built against its abstractions from day
one. Raw `new ServiceCollection()` with no host was considered and
rejected — it works, but throws away `IConfiguration`/`IHostedService`
integration for no real savings.

## 5. Application lifecycle

1. **Single-instance check** (`Mutex`-based) — before the host even starts.
   A second launch signals the existing instance to activate its window and
   exits. Production desktop apps that skip this get real bug reports
   ("why do I have five copies open").
2. **Splash screen** shown immediately (native WPF `SplashScreen` or a
   lightweight always-on-top window) while the host builds — DI
   container construction, config loading, and any startup health checks
   happen behind it, not behind a frozen blank window.
3. **Host build + start** (§4).
4. **Main window resolved from DI and shown.**
5. **Shutdown:** `OnExit` calls `_host.StopAsync()` (with a timeout) so any
   `IHostedService` background work and Serilog's sinks flush cleanly
   rather than being killed mid-write.

## 6. Navigation

**Decision: ViewModel-first navigation via an `INavigationService` abstraction, not code-behind `Frame.Navigate` calls.**

- `INavigationService` (defined in `Presentation`, since it's a
  presentation-layer concern, not a business one) exposes something like
  `NavigateTo<TViewModel>()` / `GoBack()`.
- The `Shell` project provides the concrete implementation, backed by a
  `ContentControl`'s content or a `Frame`, plus a `DataTemplate`-per-
  ViewModel convention so WPF resolves the right `View` for whatever
  `ViewModel` the navigation service activates.
- ViewModels depend on `INavigationService`, never on `Frame`, `Page`, or
  any WPF navigation type directly — keeps them testable and keeps the
  Dependency Inversion boundary real, not just nominal.

This is intentionally the minimum viable navigation design. A
multi-region/docking layout (if the app ends up needing panels, not just
one main content area) is a real possible extension but is **out of scope
for this document** — call it out now if you already know multi-region is
needed, since it changes this section's shape non-trivially.

## 7. Global exception handling

Three .NET exception surfaces, all wired in the Shell's composition root,
all funneling into the same structured-logging + user-facing-dialog path
rather than three separate ad hoc handlers:

- `Application.DispatcherUnhandledException` (UI-thread exceptions)
- `AppDomain.CurrentDomain.UnhandledException` (non-UI-thread, fatal)
- `TaskScheduler.UnobservedTaskException` (unobserved `async void`/`Task` faults)

Each logs the exception via `ILogger` (structured, with a correlation ID)
and shows a single, consistent "something went wrong" dialog rather than
WPF's default crash behavior. Whether the app attempts to continue running
after a UI-thread exception or always terminates is a real product decision
that needs an explicit answer, not a default — flagged as an open question
in §10.

## 8. Logging

**Decision: Serilog**, configured in `Infrastructure` (it owns "how do we
write logs" — file sinks, rolling policy, minimum levels per namespace) but
bootstrapped from `Shell` at startup, since `UseSerilog()` has to run before
the host builds. Everywhere else in the codebase depends only on
`Microsoft.Extensions.Logging.ILogger<T>` (the abstraction), never on
Serilog's own types directly — so `Domain`/`Application` stay logging-
framework-agnostic, consistent with the dependency rule in §3 of the
overview doc.

## 9. Enforcing the dependency rule

A dedicated `Rojan.Desktop.ArchitectureTests` project using
[`NetArchTest.Rules`](https://github.com/BenMorris/NetArchTest) asserts
things like:

```csharp
Types.InAssembly(DomainAssembly)
    .Should().NotHaveDependencyOnAny(
        "Rojan.Desktop.Application",
        "Rojan.Desktop.Infrastructure",
        "Rojan.Desktop.Presentation",
        "Rojan.Desktop.Shell")
    .GetResult().IsSuccessful.Should().BeTrue();
```

...for every layer boundary in §3's diagram. This runs in CI on every
push — a PR that violates the dependency rule fails the build, not just a
future code review. This turns "Clean Architecture" from a diagram everyone
agrees with in principle into something the build actually enforces.

## 10. Open decisions (need your answer before Shell is implemented)

These aren't blocking *this document's* approval, but they are blocking
starting the actual Shell code, so worth deciding alongside it rather than
discovering mid-implementation:

1. **On a UI-thread exception, does the app try to keep running, or always
   terminate?** (§7) Different UX, different risk profile (keep running
   risks corrupted state; always terminate risks losing unsaved user work).
2. **Theming library:** hand-rolled `ResourceDictionary`s, or a package
   (ModernWpf / MaterialDesignInXamlToolkit / Fluent WPF)? Affects the
   visual language and adds a dependency either way.
3. **Packaging/distribution target:** MSIX, traditional MSI/WiX, or
   Squirrel-style self-updating installer? Doesn't block Shell code today
   but does affect whether single-instance/update-check seams need to
   exist from day one.
4. **Is multi-window a real requirement**, or is this always a single main
   window with in-place navigation (§6)? Changes the navigation design's
   shape, not just a parameter.

## 11. Explicit non-goals for this document

To keep this review focused: this document does **not** cover the
`Domain`, `Application`, or `Infrastructure` layers' internal design (each
gets its own doc later, per the overview), does not specify any actual
business feature, and does not include a testing strategy beyond what's
needed to validate the Shell itself (§9's architecture tests, plus "the
Shell's own classes — the host builder, the exception handlers — get
conventional unit tests where they have real logic to test").

## 12. Next steps

1. You review §2 (scope table) and §10 (open decisions) — those are the
   two sections most likely to need a real answer or pushback.
2. Once approved, I scaffold the actual solution/projects from §3 (empty
   projects, `Directory.Build.props`, `Directory.Packages.props`,
   `.editorconfig`) — structure only, no logic.
3. Then implement the Shell itself per §4–§9 — still no business modules.
4. Only after the Shell runs (an empty main window, DI working, logging
   working, exceptions handled) do we write the `Domain`/`Application`
   design docs and start the first real feature.
