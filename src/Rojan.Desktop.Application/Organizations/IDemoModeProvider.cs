namespace Rojan.Desktop.Application.Organizations;

/// <summary>
/// Phase 2B Context State Hardening: whether <see cref="DesktopContextState.DemoContext"/>
/// is explicitly enabled for this run. Development-only, off by default,
/// never triggered as a side effect of real context resolution coming back
/// empty - <c>Shell.Organizations.CurrentSessionService.InitializeAsync</c>
/// checks this before ever attempting real resolution, not after it fails.
/// A separate interface (rather than a property on <c>ICurrentSessionService</c>
/// itself) purely so tests can substitute a trivial stub instead of
/// mutating process-wide environment state.
/// </summary>
public interface IDemoModeProvider
{
    public bool IsEnabled { get; }
}
