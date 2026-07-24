namespace Rojan.Server.Application;

/// <summary>
/// Sprint 8 Commit 1: Backend Foundation. No business orchestration
/// exists yet in this project by design - see
/// <see cref="DependencyInjection.ServiceCollectionExtensions.AddApplication"/>'s
/// own doc comment. This marker exists purely so other layers/tooling
/// have a stable type to anchor <c>typeof(AssemblyMarker).Assembly</c>
/// reflection against (e.g. a future assembly-scanning DI registration).
/// </summary>
public static class AssemblyMarker
{
}
