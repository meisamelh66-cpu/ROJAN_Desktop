using Rojan.Desktop.Application.Search;

namespace Rojan.Desktop.Presentation.Tests.Search;

/// <summary>Fixed-list <see cref="IGlobalSearchIndexService"/> test double.</summary>
internal sealed class StubGlobalSearchIndexService : IGlobalSearchIndexService
{
    private readonly IReadOnlyList<SearchCandidate> _candidates;

    public StubGlobalSearchIndexService(IReadOnlyList<SearchCandidate>? candidates = null)
    {
        _candidates = candidates ?? [];
    }

    public Task<IReadOnlyList<SearchCandidate>> GetCandidatesAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(_candidates);
}
