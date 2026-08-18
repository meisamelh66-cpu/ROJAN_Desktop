using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Customers;
using Rojan.Desktop.Infrastructure.Customers;

namespace Rojan.Desktop.Infrastructure.Tests.Customers;

/// <summary>
/// Exercises <see cref="BackendCustomerRepository"/> - pagination for
/// customers/timeline, notes/tags ordering (re-sorted client-side to match
/// this app's existing convention), status/lifetime-value/UserId mapping,
/// the empty-timeline and walk-in-with-no-linked-account cases, and why
/// <see cref="BackendCustomerRepository.AddActivityAsync"/> always throws.
/// Only the HTTP transport (<see cref="IApiClient"/>) is faked - same
/// "exercise the real workflow" convention as
/// <c>BackendBookingRepositoryTests</c>.
/// </summary>
public sealed class BackendCustomerRepositoryTests
{
    private const string SalonId = "salon-1";
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetCustomersAsync_MapsStatusLifetimeValueAndUserId()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers?page=0&size=100"] =
            new PagedResponse<CustomerResponse>([SampleCustomer("customer-1", "VIP", 1_200_000m, "user-42")], 0, 100, 1, 1);

        var repository = CreateRepository(apiClient, SalonId);

        var customers = await repository.GetCustomersAsync();

        var customer = Assert.Single(customers);
        Assert.Equal(CustomerStatus.Vip, customer.Status);
        Assert.Equal("1,200,000 تومان", customer.LifetimeValue);
        Assert.Equal("user-42", customer.UserId);
        Assert.Equal("org-1", customer.OrganizationId);
        Assert.Equal("branch-1", customer.BranchId);
    }

    [Fact]
    public async Task GetCustomersAsync_UnlinkedWalkInCustomer_HasNullUserIdAndZeroLifetimeValue()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers?page=0&size=100"] =
            new PagedResponse<CustomerResponse>([SampleCustomer("customer-1", "LEAD", 0m, userId: null)], 0, 100, 1, 1);

        var repository = CreateRepository(apiClient, SalonId);

        var customer = Assert.Single(await repository.GetCustomersAsync());

        Assert.Null(customer.UserId);
        Assert.Equal("0 تومان", customer.LifetimeValue);
    }

    [Fact]
    public async Task GetCustomersAsync_PagesThroughEveryBackendPage()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers?page=0&size=100"] =
            new PagedResponse<CustomerResponse>([SampleCustomer("customer-1", "LEAD", 0m, null)], 0, 100, 2, 2);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers?page=1&size=100"] =
            new PagedResponse<CustomerResponse>([SampleCustomer("customer-2", "LEAD", 0m, null)], 1, 100, 2, 2);

        var repository = CreateRepository(apiClient, SalonId);

        var customers = await repository.GetCustomersAsync();

        Assert.Equal(2, customers.Count);
        Assert.Contains(customers, c => c.Id == "customer-1");
        Assert.Contains(customers, c => c.Id == "customer-2");
    }

    [Fact]
    public async Task GetCustomersAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetCustomersAsync());
    }

    [Fact]
    public async Task GetCustomerByIdAsync_ExistingCustomer_ReturnsIt()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1"] = SampleCustomer("customer-1", "ACTIVE", 500_000m, null);

        var repository = CreateRepository(apiClient, SalonId);

        var customer = await repository.GetCustomerByIdAsync("customer-1");

        Assert.NotNull(customer);
        Assert.Equal("customer-1", customer!.Id);
    }

    [Fact]
    public async Task GetCustomerByIdAsync_NotFound_ReturnsNull()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/customers/missing"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);

        var customer = await repository.GetCustomerByIdAsync("missing");

        Assert.Null(customer);
    }

    [Fact]
    public async Task GetNotesAsync_ReordersNewestFirst_MatchingThisAppsExistingConvention()
    {
        // The backend endpoint itself returns oldest-first (see CustomerController.notes'
        // own doc comment) - the repository re-sorts to match EfCustomerRepository/
        // FakeCustomerRepository's own GetNotesAsync ordering (newest-first).
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1/notes"] = new List<CustomerNoteResponse>
        {
            new("note-older", "author-1", "Older note", CreatedAt),
            new("note-newer", "author-1", "Newer note", CreatedAt.AddDays(1)),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var notes = await repository.GetNotesAsync("customer-1");

        Assert.Equal(["note-newer", "note-older"], notes.Select(n => n.Id));
    }

    [Fact]
    public async Task GetTagsAsync_ReturnsRealServerIdsOldestFirst()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1/tags"] = new List<CustomerTagResponse>
        {
            new("tag-newer", "Regular", CreatedAt.AddDays(1)),
            new("tag-older", "VIP", CreatedAt),
        };

        var repository = CreateRepository(apiClient, SalonId);

        var tags = await repository.GetTagsAsync("customer-1");

        Assert.Equal(["tag-older", "tag-newer"], tags.Select(t => t.Id));
        Assert.Contains(tags, t => t.Id == "tag-older" && t.Label == "VIP");
    }

    [Fact]
    public async Task GetActivityAsync_EmptyTimeline_ReturnsEmptyListNotAnError()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1/timeline?page=0&size=100"] =
            new PagedResponse<CustomerTimelineEntryResponse>([], 0, 100, 0, 0);

        var repository = CreateRepository(apiClient, SalonId);

        var activity = await repository.GetActivityAsync("customer-1");

        Assert.Empty(activity);
    }

    [Fact]
    public async Task GetActivityAsync_MergedTimeline_MapsEveryEntryAndPagesThrough()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1/timeline?page=0&size=100"] =
            new PagedResponse<CustomerTimelineEntryResponse>(
                [new CustomerTimelineEntryResponse("TAG_ADDED", "Tag added: VIP", CreatedAt.AddDays(2))], 0, 100, 2, 2);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/customers/customer-1/timeline?page=1&size=100"] =
            new PagedResponse<CustomerTimelineEntryResponse>(
                [new CustomerTimelineEntryResponse("NOTE", "Prefers evenings", CreatedAt)], 1, 100, 2, 2);

        var repository = CreateRepository(apiClient, SalonId);

        var activity = await repository.GetActivityAsync("customer-1");

        Assert.Equal(2, activity.Count);
        Assert.Contains(activity, a => a.Description == "Tag added: VIP");
        Assert.Contains(activity, a => a.Description == "Prefers evenings");
        Assert.All(activity, a => Assert.False(string.IsNullOrEmpty(a.Id)));
    }

    [Fact]
    public async Task CreateCustomerAsync_SendsRequestAndReturnsServerAuthoritativeId()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = SampleCustomer("customer-server-id", "LEAD", 0m, null),
        };

        var repository = CreateRepository(apiClient, SalonId);
        var customer = new Customer("client-temp-id", "Jane Doe", "Acme", "jane@example.com", "0912-000-0000",
            CustomerStatus.Lead, "0", DateTimeOffset.Now, "Notes", "org-1", "branch-1");

        var created = await repository.CreateCustomerAsync(customer);

        Assert.Equal("customer-server-id", created.Id);
        Assert.Equal($"/api/v1/salons/{SalonId}/customers", apiClient.LastPostCall?.Path);
        var body = (CreateCustomerRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Equal("Jane Doe", body.FullName);
        Assert.Equal("0912-000-0000", body.PhoneNumber);
    }

    [Fact]
    public async Task CreateCustomerAsync_BlankOptionalFields_AreSentAsNull()
    {
        var apiClient = new StubApiClient { PostResponse = SampleCustomer("customer-1", "LEAD", 0m, null) };
        var repository = CreateRepository(apiClient, SalonId);
        var customer = new Customer("temp", "Jane Doe", string.Empty, string.Empty, string.Empty,
            CustomerStatus.Lead, "0", DateTimeOffset.Now, "Notes", "org-1", "branch-1");

        await repository.CreateCustomerAsync(customer);

        var body = (CreateCustomerRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Null(body.PhoneNumber);
        Assert.Null(body.Email);
        Assert.Null(body.Company);
    }

    [Fact]
    public async Task UpdateCustomerAsync_SendsPatchWithFullFieldSet_AndReturnsMappedResponse()
    {
        var apiClient = new StubApiClient
        {
            PatchResponse = SampleCustomer("customer-1", "VIP", 2_000_000m, "user-1"),
        };

        var repository = CreateRepository(apiClient, SalonId);
        var customer = new Customer("customer-1", "Jane Doe", "Acme", "jane@example.com", "0912-000-0000",
            CustomerStatus.Vip, "0", DateTimeOffset.Now, "Notes", "org-1", "branch-1");

        var updated = await repository.UpdateCustomerAsync(customer);

        Assert.Equal(CustomerStatus.Vip, updated.Status);
        Assert.Equal("2,000,000 تومان", updated.LifetimeValue);
        Assert.Equal($"/api/v1/salons/{SalonId}/customers/customer-1", apiClient.LastPatchCall?.Path);
        var body = (UpdateCustomerRequest)apiClient.LastPatchCall!.Value.Body!;
        Assert.Equal("VIP", body.Status);
    }

    [Fact]
    public async Task AddNoteAsync_ReturnsNoteWithServerGeneratedId()
    {
        var apiClient = new StubApiClient { PostResponse = new CustomerNoteResponse("note-server-id", "author-1", "Prefers evenings", CreatedAt) };
        var repository = CreateRepository(apiClient, SalonId);
        var note = new CustomerNote("client-temp-id", "customer-1", "Prefers evenings", DateTimeOffset.Now);

        var added = await repository.AddNoteAsync(note);

        Assert.Equal("note-server-id", added.Id);
        Assert.Equal($"/api/v1/salons/{SalonId}/customers/customer-1/notes", apiClient.LastPostCall?.Path);
    }

    [Fact]
    public async Task AddTagAsync_ReturnsTagWithServerGeneratedId()
    {
        var apiClient = new StubApiClient { PostResponse = new CustomerTagResponse("tag-server-id", "VIP", CreatedAt) };
        var repository = CreateRepository(apiClient, SalonId);
        var tag = new CustomerTag("client-temp-id", "customer-1", "VIP", DateTimeOffset.Now);

        var added = await repository.AddTagAsync(tag);

        Assert.Equal("tag-server-id", added.Id);
        Assert.Equal($"/api/v1/salons/{SalonId}/customers/customer-1/tags", apiClient.LastPostCall?.Path);
    }

    [Fact]
    public async Task RemoveTagAsync_SendsDeleteToTheRealTagId()
    {
        var apiClient = new StubApiClient();
        var repository = CreateRepository(apiClient, SalonId);

        await repository.RemoveTagAsync("customer-1", "tag-server-id");

        Assert.Equal($"/api/v1/salons/{SalonId}/customers/customer-1/tags/tag-server-id", apiClient.LastDeletePath);
    }

    [Fact]
    public async Task AddActivityAsync_AlwaysThrowsNotSupportedException()
    {
        // ROJAN_Backend has no generic "log an arbitrary activity" endpoint - see
        // BackendCustomerRepository.AddActivityAsync's own doc comment.
        var repository = CreateRepository(new StubApiClient(), SalonId);
        var activity = new CustomerActivity("id", "customer-1", "Custom activity", DateTimeOffset.Now);

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.AddActivityAsync(activity));
    }

    private static CustomerResponse SampleCustomer(string id, string status, decimal lifetimeValue, string? userId) => new(
        id, SalonId, userId, "Jane Doe", "0912-000-0000", "jane@example.com", "Acme", status, lifetimeValue,
        ["VIP"], true, CreatedAt, CreatedAt);

    private static BackendCustomerRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
        new(apiClient, new StubSalonContextService(salonId), new StubEnterpriseContext());

    private sealed class StubSalonContextService(string? salonId) : ISalonContextService
    {
        public Task<string?> GetSalonIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(salonId);
    }

    private sealed class StubEnterpriseContext : IEnterpriseContext
    {
        public string? CurrentOrganizationId => "org-1";

        public string? CurrentBranchId => "branch-1";

        public WorkspaceRole CurrentRole => WorkspaceRole.OrganizationOwner;

        public IReadOnlySet<string> BackendPermissions => new HashSet<string>();
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public object? PostResponse { get; set; }

        public (string Path, object? Body)? LastPostCall { get; private set; }

        public object? PatchResponse { get; set; }

        public (string Path, object? Body)? LastPatchCall { get; private set; }

        public string? LastDeletePath { get; private set; }

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

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCustomerRepository never puts.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            LastDeletePath = path;
            return Task.FromResult(ApiResponseFactory.Success(default(TResponse)!, 204));
        }

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendCustomerRepository never sends a body-less PATCH.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPatchCall = (path, body);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PatchResponse!, 200));
        }
    }
}
