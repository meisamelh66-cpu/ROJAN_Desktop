namespace Rojan.Server.Domain;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. No business entities exist yet
/// in this project by design (this commit is infrastructure foundation
/// only - see the solution's own README) - this marker exists purely so
/// other layers/tooling have a stable type to anchor
/// <c>typeof(AssemblyMarker).Assembly</c> reflection against (e.g. a
/// future assembly-scanning DI registration) - the same role
/// <c>Application.AssemblyMarker</c>/<c>Infrastructure.AssemblyMarker</c>
/// play for their own layers (not cross-referenced here via
/// <c>&lt;see cref&gt;</c> - Domain does not, and must not, reference
/// either of those assemblies).
/// </summary>
public static class AssemblyMarker
{
}
