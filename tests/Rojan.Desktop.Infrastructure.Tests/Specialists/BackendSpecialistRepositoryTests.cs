using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Specialists;
using Rojan.Desktop.Infrastructure.Specialists;

namespace Rojan.Desktop.Infrastructure.Tests.Specialists;

/// <summary>
/// Exercises <see cref="BackendSpecialistRepository"/> - catalog fetch,
/// status mapping (no <see cref="SpecialistStatus.OnLeave"/> equivalent),
/// create/update wire mapping, the status-change limitation
/// <see cref="BackendSpecialistRepository.UpdateSpecialistAsync"/> enforces,
/// the always-empty skills read, and why the two skill writes always throw.
/// Only the HTTP transport (<see cref="IApiClient"/>) is faked - same
/// "exercise the real workflow" convention as
/// <c>BackendServiceRepositoryTests</c>.
/// </summary>
public sealed class BackendSpecialistRepositoryTests
{
    private const string SalonId = "salon-1";

    [Fact]
    public async Task GetSpecialistsAsync_MapsEveryField()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists"] =
            new List<SpecialistResponse> { new("specialist-1", SalonId, null, "Kiana Radmanesh", "Colour expert", null, true) };

        var repository = CreateRepository(apiClient, SalonId);

        var specialists = await repository.GetSpecialistsAsync();

        var specialist = Assert.Single(specialists);
        Assert.Equal("Kiana Radmanesh", specialist.FullName);
        Assert.Equal("Colour expert", specialist.Bio);
        Assert.Equal(SpecialistStatus.Active, specialist.Status);
        Assert.Equal(string.Empty, specialist.Title);
        Assert.Equal(string.Empty, specialist.Email);
        Assert.Equal(string.Empty, specialist.Phone);
    }

    [Fact]
    public async Task GetSpecialistsAsync_InactiveSpecialist_MapsToInactive_NoOnLeaveEquivalentExists()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists"] =
            new List<SpecialistResponse> { new("specialist-1", SalonId, null, "Jamie Stylist", null, null, false) };

        var repository = CreateRepository(apiClient, SalonId);

        var specialist = Assert.Single(await repository.GetSpecialistsAsync());

        Assert.Equal(SpecialistStatus.Inactive, specialist.Status);
    }

    [Fact]
    public async Task GetSpecialistsAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetSpecialistsAsync());
    }

    [Fact]
    public async Task GetSpecialistsAsync_FetchFails_ThrowsApiException()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists"] = (500, "Server error");

        var repository = CreateRepository(apiClient, SalonId);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetSpecialistsAsync());
    }

    [Fact]
    public async Task GetSpecialistByIdAsync_ExistingSpecialist_ReturnsIt()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "Kiana Radmanesh", null, null, true);

        var repository = CreateRepository(apiClient, SalonId);

        var specialist = await repository.GetSpecialistByIdAsync("specialist-1");

        Assert.NotNull(specialist);
        Assert.Equal("specialist-1", specialist!.Id);
    }

    [Fact]
    public async Task GetSpecialistByIdAsync_NotFound_ReturnsNull()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/missing"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);

        var specialist = await repository.GetSpecialistByIdAsync("missing");

        Assert.Null(specialist);
    }

    [Fact]
    public async Task CreateSpecialistAsync_SendsNameAndBio_NullUserIdAndPhotoUrl()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = new SpecialistResponse("specialist-server-id", SalonId, null, "Jamie Stylist", "Bio text", null, true),
        };
        var repository = CreateRepository(apiClient, SalonId);
        var specialist = new Specialist("client-temp-id", "Jamie Stylist", "Senior Stylist", "jamie@example.com", "0912-000-0000", SpecialistStatus.Active, "Bio text");

        var created = await repository.CreateSpecialistAsync(specialist);

        Assert.Equal("specialist-server-id", created.Id);
        Assert.Equal($"/api/v1/salons/{SalonId}/specialists", apiClient.LastPostCall?.Path);
        var body = (CreateSpecialistRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Null(body.UserId);
        Assert.Equal("Jamie Stylist", body.DisplayName);
        Assert.Equal("Bio text", body.Bio);
        Assert.Null(body.PhotoUrl);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_SameStatus_SendsUpdateAndReturnsMappedResponse()
    {
        var apiClient = new StubApiClient
        {
            PutResponse = new SpecialistResponse("specialist-1", SalonId, null, "Updated Name", "Updated bio", null, true),
        };
        var repository = CreateRepository(apiClient, SalonId);
        var specialist = new Specialist("specialist-1", "Updated Name", "Title", "e@x.com", "0912", SpecialistStatus.Active, "Updated bio");

        var updated = await repository.UpdateSpecialistAsync(specialist);

        Assert.Equal("Updated Name", updated.FullName);
        Assert.Equal($"/api/v1/salons/{SalonId}/specialists/specialist-1", apiClient.LastPutCall?.Path);
        var body = (UpdateSpecialistRequest)apiClient.LastPutCall!.Value.Body!;
        Assert.Equal("Updated Name", body.DisplayName);
    }

    [Fact]
    public async Task UpdateSpecialistAsync_RequestedStatusChange_ThrowsNotSupportedException()
    {
        // ROJAN_Backend's PUT /specialists/{id} has no status field at all - see
        // BackendSpecialistRepository.UpdateSpecialistAsync's own doc comment.
        var apiClient = new StubApiClient
        {
            PutResponse = new SpecialistResponse("specialist-1", SalonId, null, "Name", null, null, true), // backend still reports Active
        };
        var repository = CreateRepository(apiClient, SalonId);
        var specialist = new Specialist("specialist-1", "Name", "Title", "e@x.com", "0912", SpecialistStatus.Inactive, "Bio"); // caller wants Inactive

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.UpdateSpecialistAsync(specialist));
    }

    [Fact]
    public async Task GetSkillsAsync_AlwaysReturnsEmpty()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        var skills = await repository.GetSkillsAsync("specialist-1");

        Assert.Empty(skills);
    }

    [Fact]
    public async Task AddSkillAsync_AlwaysThrowsNotSupportedException()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);
        var skill = new SpecialistSkill("id", "specialist-1", "Balayage");

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.AddSkillAsync(skill));
    }

    [Fact]
    public async Task RemoveSkillAsync_AlwaysThrowsNotSupportedException()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.RemoveSkillAsync("specialist-1", "skill-1"));
    }

    private static BackendSpecialistRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId));

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public object? PostResponse { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public object? PutResponse { get; set; }

        public (string Path, object? Body)? LastPutCall { get; private set; }

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
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PostResponse!, 201));
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPutCall = (path, body);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PutResponse!, 200));
        }

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistRepository never patches.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistRepository never patches.");

        public Task<ApiResponse<byte[]>> GetBytesAsync(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendSpecialistRepository never fetches raw bytes.");
    }
}
