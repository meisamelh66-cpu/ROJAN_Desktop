using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Infrastructure.Salons;

namespace Rojan.Desktop.Infrastructure.Tests.Salons;

/// <summary>
/// Exercises <see cref="BackendSalonRepository"/> - the <c>/salons/mine</c>
/// read, the <c>POST /salons</c> create (request-field mapping, response
/// mapping, failure propagation), and empty-optional-field handling. Only
/// the HTTP transport (<see cref="IApiClient"/>) is faked - same "exercise
/// the real workflow" convention as <c>BackendSpecialistRepositoryTests</c>.
/// </summary>
public sealed class BackendSalonRepositoryTests
{
    [Fact]
    public async Task GetMineAsync_ReturnsEverySalonMapped()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses["/api/v1/salons/mine"] = new List<SalonResponse>
        {
            new("salon-1", "owner-1", "Glow Salon", "A nice salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", true),
        };

        var repository = new BackendSalonRepository(apiClient);

        var salons = await repository.GetMineAsync();

        var salon = Assert.Single(salons);
        Assert.Equal("salon-1", salon.Id);
        Assert.Equal("Glow Salon", salon.Name);
        Assert.Equal("A nice salon.", salon.Description);
        Assert.Equal("+1 555 0100", salon.Phone);
        Assert.Equal("hello@glowsalon.example", salon.Email);
        Assert.Equal("1 Main St", salon.Address);
        Assert.True(salon.Active);
    }

    [Fact]
    public async Task GetMineAsync_NoSalons_ReturnsEmptyList()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses["/api/v1/salons/mine"] = new List<SalonResponse>();

        var repository = new BackendSalonRepository(apiClient);

        var salons = await repository.GetMineAsync();

        Assert.Empty(salons);
    }

    [Fact]
    public async Task GetMineAsync_FetchFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures["/api/v1/salons/mine"] = (500, "Server error");

        var repository = new BackendSalonRepository(apiClient);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetMineAsync());
    }

    [Fact]
    public async Task CreateAsync_SendsNameDescriptionPhoneEmailAddress_ReturnsMappedResponse()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new SalonResponse("salon-server-id", "owner-1", "Glow Salon", "A nice salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", true),
        };
        var repository = new BackendSalonRepository(apiClient);
        var salon = new Rojan.Desktop.Domain.Salons.Salon("client-temp-id", "Glow Salon", "A nice salon.", "+1 555 0100", "hello@glowsalon.example", "1 Main St", true);

        var created = await repository.CreateAsync(salon);

        Assert.Equal("salon-server-id", created.Id);
        Assert.Equal("/api/v1/salons", apiClient.LastPostCall?.Path);
        var body = (CreateSalonRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Equal("Glow Salon", body.Name);
        Assert.Equal("A nice salon.", body.Description);
        Assert.Equal("+1 555 0100", body.Phone);
        Assert.Equal("hello@glowsalon.example", body.Email);
        Assert.Equal("1 Main St", body.Address);
    }

    [Fact]
    public async Task CreateAsync_BlankOptionalFields_SentAsNull()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new SalonResponse("salon-server-id", "owner-1", "Glow Salon", null, "+1 555 0100", null, "1 Main St", true),
        };
        var repository = new BackendSalonRepository(apiClient);
        var salon = new Rojan.Desktop.Domain.Salons.Salon("client-temp-id", "Glow Salon", "   ", "+1 555 0100", string.Empty, "1 Main St", true);

        await repository.CreateAsync(salon);

        var body = (CreateSalonRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Null(body.Description);
        Assert.Null(body.Email);
    }

    [Fact]
    public async Task CreateAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient { PostFailure = (400, "Validation failed") };
        var repository = new BackendSalonRepository(apiClient);
        var salon = new Rojan.Desktop.Domain.Salons.Salon("client-temp-id", string.Empty, null, string.Empty, null, string.Empty, true);

        await Assert.ThrowsAsync<ApiException>(() => repository.CreateAsync(salon));
    }

    [Fact]
    public async Task GetQrCodeAsync_ReturnsThePngBytesFromTheQrCodeEndpoint()
    {
        var apiClient = new StubApiClient();
        apiClient.GetBytesResponses["/api/v1/salons/salon-1/qr-code?size=512"] = [1, 2, 3, 4];
        var repository = new BackendSalonRepository(apiClient);

        var bytes = await repository.GetQrCodeAsync("salon-1", 512);

        Assert.Equal(new byte[] { 1, 2, 3, 4 }, bytes);
    }

    [Fact]
    public async Task GetQrCodeAsync_BackendRejects_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetBytesFailures["/api/v1/salons/salon-1/qr-code?size=512"] = (403, "Forbidden");
        var repository = new BackendSalonRepository(apiClient);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetQrCodeAsync("salon-1", 512));
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Dictionary<string, byte[]> GetBytesResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetBytesFailures { get; } = [];

        public object? PostResponse { get; set; }

        public (int? Status, string Message)? PostFailure { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
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

            return Task.FromResult(ApiResponseFactory.Success((TResponse)PostResponse!, 201));
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSalonRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            if (GetBytesFailures.TryGetValue(path, out var failure))
            {
                return Task.FromResult(ApiResponseFactory.Failure<byte[]>(failure.Status, failure.Message));
            }

            if (GetBytesResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success(response, 200));
            }

            throw new InvalidOperationException($"Unexpected GET (bytes) '{path}' - not configured by this test.");
        }
    }
}
