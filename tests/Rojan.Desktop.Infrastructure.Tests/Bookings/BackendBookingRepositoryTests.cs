using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Organizations;
using Rojan.Desktop.Application.Salons;
using Rojan.Desktop.Domain.Bookings;
using Rojan.Desktop.Infrastructure.Bookings;

namespace Rojan.Desktop.Infrastructure.Tests.Bookings;

/// <summary>
/// Exercises <see cref="BackendBookingRepository"/> - pagination, the
/// best-effort service/specialist name (and price/duration) resolution
/// and its graceful fallback, status-transition endpoint mapping
/// (including the InProgress/NoShow/Pending unsupported cases), reschedule,
/// and (Calendar/Availability Integration Phase 3)
/// <see cref="BackendBookingRepository.CreateBookingAsync"/>'s real call to
/// the owner-initiated <c>POST /api/v1/salons/{salonId}/bookings</c>
/// endpoint. Only the HTTP transport (<see cref="IApiClient"/>) is faked -
/// same "exercise the real workflow" convention as <c>BackendDashboardRepositoryTests</c>.
/// </summary>
public sealed class BackendBookingRepositoryTests
{
    private const string SalonId = "salon-1";
    private static readonly DateTime StartTime = new(2026, 8, 10, 14, 0, 0);
    private static readonly DateTime EndTime = new(2026, 8, 10, 14, 45, 0);
    private static readonly DateTimeOffset CreatedAt = new(2026, 8, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task GetBookingsAsync_ResolvesServiceNamePriceDurationAndSpecialistName()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/bookings?page=0&size=100"] =
            new PagedResponse<BookingResponse>([SampleBooking("booking-1")], 0, 100, 1, 1);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse> { new("cat-1", SalonId, "Hair", null, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories/cat-1/services"] =
            new List<ServiceResponse> { new("service-1", SalonId, "cat-1", "اصلاح رنگ ریشه", null, 45, 1_200_000m, true) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] =
            new SpecialistResponse("specialist-1", SalonId, null, "کیانا رادمنش", null, null, true);

        var repository = CreateRepository(apiClient, SalonId);

        var bookings = await repository.GetBookingsAsync();

        var booking = Assert.Single(bookings);
        Assert.Equal("اصلاح رنگ ریشه", booking.ServiceName);
        Assert.Equal("کیانا رادمنش", booking.SpecialistName);
        Assert.Equal("1,200,000 تومان", booking.Price);
        Assert.Equal(45, booking.DurationMinutes);
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Equal("customer-1", booking.CustomerId);
        Assert.Equal("customer-1", booking.CustomerName); // Unresolved - see BackendBookingRepository's own doc comment.
    }

    [Fact]
    public async Task GetBookingsAsync_PagesThroughEveryBackendPage()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/bookings?page=0&size=100"] =
            new PagedResponse<BookingResponse>([SampleBooking("booking-1")], 0, 100, 2, 2);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/bookings?page=1&size=100"] =
            new PagedResponse<BookingResponse>([SampleBooking("booking-2")], 1, 100, 2, 2);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);

        var repository = CreateRepository(apiClient, SalonId);

        var bookings = await repository.GetBookingsAsync();

        Assert.Equal(2, bookings.Count);
        Assert.Contains(bookings, b => b.Id == "booking-1");
        Assert.Contains(bookings, b => b.Id == "booking-2");
    }

    [Fact]
    public async Task GetBookingsAsync_UnresolvableServiceAndSpecialist_FallsBackToTheRawId()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/bookings?page=0&size=100"] =
            new PagedResponse<BookingResponse>([SampleBooking("booking-1")], 0, 100, 1, 1);
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>(); // no categories -> service never resolves
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] = (404, "Specialist not found");

        var repository = CreateRepository(apiClient, SalonId);

        var bookings = await repository.GetBookingsAsync();

        var booking = Assert.Single(bookings);
        Assert.Equal("service-1", booking.ServiceName);
        Assert.Equal("specialist-1", booking.SpecialistName);
        Assert.Equal(string.Empty, booking.Price);
    }

    [Fact]
    public async Task GetBookingsAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);

        await Assert.ThrowsAsync<ApiException>(() => repository.GetBookingsAsync());
    }

    [Fact]
    public async Task GetBookingByIdAsync_ExistingBooking_ReturnsIt()
    {
        var apiClient = new StubApiClient();
        apiClient.GetResponses["/api/v1/bookings/booking-1"] = SampleBooking("booking-1");
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);

        var repository = CreateRepository(apiClient, SalonId);

        var booking = await repository.GetBookingByIdAsync("booking-1");

        Assert.NotNull(booking);
        Assert.Equal("booking-1", booking!.Id);
    }

    [Fact]
    public async Task GetBookingByIdAsync_NotFound_ReturnsNull()
    {
        var apiClient = new StubApiClient();
        apiClient.GetFailures["/api/v1/bookings/missing"] = (404, "Not found");

        var repository = CreateRepository(apiClient, SalonId);

        var booking = await repository.GetBookingByIdAsync("missing");

        Assert.Null(booking);
    }

    [Fact]
    public async Task CreateBookingAsync_SendsCustomerCrmIdServiceSpecialistAndStartTime()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = SampleBooking("booking-new"),
        };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);
        var repository = CreateRepository(apiClient, SalonId);
        var scheduledAt = new DateTimeOffset(2026, 8, 10, 14, 0, 0, TimeSpan.Zero);
        var booking = new Booking("client-temp-id", "customer-1", "Customer", "service-1", "Service", "specialist-1", "Specialist",
            scheduledAt, 45, "0", BookingStatus.Pending, "Walk-in", "org-1", "branch-1");

        var created = await repository.CreateBookingAsync(booking);

        Assert.Equal("booking-new", created.Id);
        Assert.Equal($"/api/v1/salons/{SalonId}/bookings", apiClient.LastPostCall?.Path);
        var body = (CreateBookingForCustomerRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Equal("customer-1", body.CustomerId);
        Assert.Equal("service-1", body.ServiceId);
        Assert.Equal("specialist-1", body.SpecialistId);
        Assert.Equal(scheduledAt.DateTime, body.StartTime);
        Assert.Equal("Walk-in", body.Notes);
    }

    [Fact]
    public async Task CreateBookingAsync_EmptyNotes_SendsNullNotes()
    {
        var apiClient = new StubApiClient
        {
            PostResponse = SampleBooking("booking-new"),
        };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);
        var repository = CreateRepository(apiClient, SalonId);
        var booking = new Booking("client-temp-id", "customer-1", "Customer", "service-1", "Service", "specialist-1", "Specialist",
            DateTimeOffset.Now, 30, "0", BookingStatus.Pending, string.Empty, "org-1", "branch-1");

        await repository.CreateBookingAsync(booking);

        var body = (CreateBookingForCustomerRequest)apiClient.LastPostCall!.Value.Body!;
        Assert.Null(body.Notes);
    }

    [Fact]
    public async Task CreateBookingAsync_NoSalon_ThrowsApiException()
    {
        var repository = CreateRepository(new StubApiClient(), salonId: null);
        var booking = new Booking("client-temp-id", "customer-1", "Customer", "service-1", "Service", "specialist-1", "Specialist",
            DateTimeOffset.Now, 30, "0", BookingStatus.Pending, string.Empty, "org-1", "branch-1");

        await Assert.ThrowsAsync<ApiException>(() => repository.CreateBookingAsync(booking));
    }

    [Fact]
    public async Task CreateBookingAsync_BackendRejects_ThrowsApiException()
    {
        // e.g. the customer has no linked account yet (409) - see SalonBookingController's own doc comment.
        var apiClient = new StubApiClient { PostFailure = (409, "Customer has no linked account") };
        var repository = CreateRepository(apiClient, SalonId);
        var booking = new Booking("client-temp-id", "customer-1", "Customer", "service-1", "Service", "specialist-1", "Specialist",
            DateTimeOffset.Now, 30, "0", BookingStatus.Pending, string.Empty, "org-1", "branch-1");

        await Assert.ThrowsAsync<ApiException>(() => repository.CreateBookingAsync(booking));
    }

    [Theory]
    [InlineData(BookingStatus.Confirmed, "confirm")]
    [InlineData(BookingStatus.Cancelled, "cancel")]
    [InlineData(BookingStatus.Completed, "complete")]
    public async Task UpdateBookingStatusAsync_SupportedStatus_CallsTheMatchingEndpoint(BookingStatus status, string expectedSegment)
    {
        var apiClient = new StubApiClient();
        apiClient.PatchResponses[$"/api/v1/bookings/booking-1/{expectedSegment}"] = SampleBooking("booking-1") with { Status = BackendStatusFor(status) };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);

        var repository = CreateRepository(apiClient, SalonId);

        var updated = await repository.UpdateBookingStatusAsync("booking-1", status);

        Assert.Equal(status, updated.Status);
        Assert.Contains($"/api/v1/bookings/booking-1/{expectedSegment}", apiClient.PatchPaths);
    }

    [Theory]
    [InlineData(BookingStatus.InProgress)]
    [InlineData(BookingStatus.NoShow)]
    [InlineData(BookingStatus.Pending)]
    public async Task UpdateBookingStatusAsync_UnsupportedStatus_ThrowsNotSupportedException(BookingStatus status)
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        await Assert.ThrowsAsync<NotSupportedException>(() => repository.UpdateBookingStatusAsync("booking-1", status));
    }

    [Fact]
    public async Task RescheduleBookingAsync_SendsTheNewStartTimeAndReturnsTheMappedBooking()
    {
        var apiClient = new StubApiClient
        {
            PutResponse = SampleBooking("booking-1") with { StartTime = new DateTime(2026, 8, 12, 10, 0, 0) },
        };
        apiClient.GetResponses[$"/api/v1/salons/{SalonId}/categories"] = new List<ServiceCategoryResponse>();
        EmptySpecialistLookupFallback(apiClient);

        var repository = CreateRepository(apiClient, SalonId);
        var newTime = new DateTimeOffset(2026, 8, 12, 10, 0, 0, TimeSpan.Zero);

        var updated = await repository.RescheduleBookingAsync("booking-1", newTime);

        Assert.Equal("/api/v1/bookings/booking-1/reschedule", apiClient.LastPutCall?.Path);
        Assert.Equal(newTime.DateTime, ((RescheduleBookingRequest)apiClient.LastPutCall!.Value.Body!).NewStartTime);
        Assert.Equal(10, updated.ScheduledAt.Hour);
    }

    [Fact]
    public void SupportsInProgressAndNoShowStatuses_IsFalse()
    {
        var repository = CreateRepository(new StubApiClient(), SalonId);

        Assert.False(repository.SupportsInProgressAndNoShowStatuses);
    }

    private static void EmptySpecialistLookupFallback(StubApiClient apiClient) =>
        apiClient.GetFailures[$"/api/v1/salons/{SalonId}/specialists/specialist-1"] = (404, "Specialist not found");

    private static BookingResponse SampleBooking(string id) => new(
        id, SalonId, "service-1", "specialist-1", "customer-1", StartTime, EndTime, "CONFIRMED", "Notes", CreatedAt, CreatedAt);

    private static string BackendStatusFor(BookingStatus status) => status switch
    {
        BookingStatus.Confirmed => "CONFIRMED",
        BookingStatus.Cancelled => "CANCELLED",
        BookingStatus.Completed => "COMPLETED",
        _ => "PENDING",
    };

    private static BackendBookingRepository CreateRepository(StubApiClient apiClient, string? salonId) =>
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
    }

    private sealed class StubApiClient : IApiClient
    {
        public Dictionary<string, object> GetResponses { get; } = [];

        public Dictionary<string, (int? Status, string Message)> GetFailures { get; } = [];

        public Dictionary<string, object> PatchResponses { get; } = [];

        public List<string> PatchPaths { get; } = [];

        public object? PutResponse { get; set; }

        public (string Path, object? Body)? LastPutCall { get; private set; }

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

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            LastPutCall = (path, body);
            return Task.FromResult(ApiResponseFactory.Success((TResponse)PutResponse!, 200));
        }

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBookingRepository never deletes.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default)
        {
            PatchPaths.Add(path);
            if (PatchResponses.TryGetValue(path, out var response))
            {
                return Task.FromResult(ApiResponseFactory.Success((TResponse)response, 200));
            }

            throw new InvalidOperationException($"Unexpected PATCH '{path}' - not configured by this test.");
        }

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("BackendBookingRepository never sends a PATCH with a body.");
    }
}
