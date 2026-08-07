using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Infrastructure.Membership;

namespace Rojan.Desktop.Infrastructure.Tests.Membership;

/// <summary>
/// Exercises <see cref="BackendSalonInviteRepository"/> - the
/// <c>GET /invites/{token}</c> lookup and <c>POST /invites/{token}/accept</c>,
/// including that accepting never re-fetches details (the caller supplies
/// the salon name it already has from the preceding lookup). Only the HTTP
/// transport (<see cref="IApiClient"/>) is faked - same convention as
/// <c>BackendSalonRepositoryTests</c>.
/// </summary>
public sealed class BackendSalonInviteRepositoryTests
{
    [Fact]
    public async Task GetDetailsAsync_ReturnsSalonNameAndRole()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses["/api/v1/invites/tok-123"] = new SalonInviteDetailsResponse("Glow Salon", "RECEPTIONIST");

        var repository = new BackendSalonInviteRepository(apiClient);

        var details = await repository.GetDetailsAsync("tok-123");

        Assert.Equal("Glow Salon", details.SalonName);
        Assert.Equal("RECEPTIONIST", details.Role);
    }

    [Fact]
    public async Task GetDetailsAsync_InvalidToken_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures["/api/v1/invites/does-not-exist"] = (404, "Not found");

        var repository = new BackendSalonInviteRepository(apiClient);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetDetailsAsync("does-not-exist"));
    }

    [Fact]
    public async Task AcceptAsync_ReturnsMembershipWithCallerSuppliedSalonName_AndDoesNotRefetchDetails()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new SalonInviteAcceptedResponse("salon-1", "RECEPTIONIST"),
        };
        var repository = new BackendSalonInviteRepository(apiClient);

        var membership = await repository.AcceptAsync("tok-123", "Glow Salon");

        Assert.Equal("salon-1", membership.SalonId);
        Assert.Equal("Glow Salon", membership.SalonName);
        Assert.Equal("RECEPTIONIST", membership.Role);
        Assert.Equal("/api/v1/invites/tok-123/accept", apiClient.LastPostCall?.Path);
        Assert.Empty(apiClient.GetCallPaths);
    }

    [Fact]
    public async Task AcceptAsync_TokenAlreadyConsumed_ThrowsApiException()
    {
        var apiClient = new StubApiClient { PostFailure = (404, "Salon invite not found or no longer available") };
        var repository = new BackendSalonInviteRepository(apiClient);

        await Assert.ThrowsAsync<ApiException>(() => repository.AcceptAsync("tok-123", "Glow Salon"));
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public List<string> GetCallPaths { get; } = [];

        public object? PostResponse { get; set; }

        public (int? Status, string Message)? PostFailure { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            GetCallPaths.Add(path);

            if (GetFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            if (GetResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected GET '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPostCall = (path, body);

            if (PostFailure is { } failure)
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(failure.Status, failure.Message));
            }

            return Task.FromResult(ApiResponseFactory.Success((TResponse)PostResponse!, 200));
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonInviteRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonInviteRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonInviteRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonInviteRepository never patches.");
    }
}
