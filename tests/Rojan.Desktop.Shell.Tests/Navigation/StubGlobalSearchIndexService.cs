using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Shell.Tests.Navigation;

/// <summary>Empty <see cref="IGlobalSearchIndexService"/> test double - these navigation/branch-switcher tests never exercise Global Search behavior directly.</summary>
internal sealed class StubGlobalSearchIndexService : IGlobalSearchIndexService
{
    public Task<IReadOnlyList<SearchCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SearchCandidate>>([]);
}
