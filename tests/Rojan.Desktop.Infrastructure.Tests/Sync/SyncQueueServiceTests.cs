using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Sync;

namespace Rojan.Desktop.Infrastructure.Tests.Sync;

/// <summary>Exercises <see cref="SyncQueueService"/> against a temp file with a stubbed <see cref="IConnectivityService"/>/<see cref="IApiClient"/> (there is no real backend to sync to yet - see the class's own doc comment) but a real persistence round-trip.</summary>
public sealed class SyncQueueServiceTests : IDisposable
{
    private readonly string _filePath;

    public SyncQueueServiceTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "queue.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static PendingSyncOperation NewOperation(string entityId = "customer-1") =>
        new(Guid.NewGuid().ToString("N"), "Customer", entityId, "Create", "{}", DateTimeOffset.UtcNow);

    [Fact]
    public async Task InitializeAsync_NoPersistedQueue_StaysIdle()
    {
        var service = new SyncQueueService(new StubConnectivityService(), new PassThroughRetryPolicy(), new StubApiClient(), _filePath);

        await service.InitializeAsync();

        Assert.Equal(SyncState.Idle, service.CurrentState);
    }

    [Fact]
    public async Task EnqueueAsync_AddsTheOperationAndReportsPendingChanges()
    {
        var service = new SyncQueueService(new StubConnectivityService(), new PassThroughRetryPolicy(), new StubApiClient(), _filePath);

        await service.EnqueueAsync(NewOperation());

        Assert.Equal(SyncState.PendingChanges, service.CurrentState);
        Assert.Single(await service.GetPendingAsync());
    }

    [Fact]
    public async Task EnqueueAsync_PersistsAcrossInstances()
    {
        var connectivity = new StubConnectivityService();
        var apiClient = new StubApiClient();
        var first = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await first.EnqueueAsync(NewOperation());

        var second = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await second.InitializeAsync();

        Assert.Single(await second.GetPendingAsync());
    }

    [Fact]
    public async Task ProcessQueueAsync_WhileOffline_LeavesTheOperationQueuedAsPendingChanges()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Offline };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), new StubApiClient(), _filePath);
        await service.EnqueueAsync(NewOperation());

        await service.ProcessQueueAsync();

        Assert.Equal(SyncState.PendingChanges, service.CurrentState);
        Assert.Single(await service.GetPendingAsync());
    }

    [Fact]
    public async Task ProcessQueueAsync_OnlineButApiClientFails_IncrementsRetryCountAndStaysQueued()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ThrowOnPost = new ApiConnectivityException("no backend configured") };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await service.EnqueueAsync(NewOperation());

        await service.ProcessQueueAsync();

        var pending = await service.GetPendingAsync();
        Assert.Single(pending);
        Assert.Equal(1, pending[0].RetryCount);
        Assert.Equal(SyncState.PendingChanges, service.CurrentState);
    }

    [Fact]
    public async Task ProcessQueueAsync_OnlineAndApiClientSucceeds_RemovesTheOperationAndReturnsToIdle()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { SucceedPost = true };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await service.EnqueueAsync(NewOperation());

        await service.ProcessQueueAsync();

        Assert.Empty(await service.GetPendingAsync());
        Assert.Equal(SyncState.Idle, service.CurrentState);
    }

    [Fact]
    public async Task ProcessQueueAsync_ConflictResponse_RecordsAConflictWithCorrectEntityInformation()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ConflictStatusCode = 409 };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        var operation = NewOperation();
        await service.EnqueueAsync(operation);

        await service.ProcessQueueAsync();

        var conflict = Assert.Single(service.Conflicts);
        Assert.Equal(operation.Id, conflict.OperationId);
        Assert.Equal(operation.EntityType, conflict.EntityType);
        Assert.Equal(operation.EntityId, conflict.EntityId);
        Assert.Equal(operation.Payload, conflict.LocalPayload);
        Assert.Equal("conflict", conflict.RemotePayload);
        Assert.False(string.IsNullOrWhiteSpace(conflict.Reason));
        Assert.Equal(SyncConflictResolutionStatus.Unresolved, conflict.ResolutionStatus);
        Assert.Equal(SyncState.ConflictDetected, service.CurrentState);
    }

    [Fact]
    public async Task ProcessQueueAsync_ConflictResponse_KeepsTheOriginalOperationRatherThanDeletingIt()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ConflictStatusCode = 409 };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        var operation = NewOperation();
        await service.EnqueueAsync(operation);

        await service.ProcessQueueAsync();

        var pending = Assert.Single(await service.GetPendingAsync());
        Assert.Equal(operation.Id, pending.Id);
        Assert.Equal(operation.RetryCount, pending.RetryCount);
    }

    [Fact]
    public async Task ProcessQueueAsync_ConflictedOperation_IsNeverResentOnASubsequentPass()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ConflictStatusCode = 409 };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await service.EnqueueAsync(NewOperation());

        await service.ProcessQueueAsync();
        await service.ProcessQueueAsync();

        Assert.Equal(1, apiClient.PostCallCount);
        Assert.Single(service.Conflicts);
        Assert.Single(await service.GetPendingAsync());
    }

    [Fact]
    public async Task ProcessQueueAsync_MultipleOperationsAllConflict_RecordsOneConflictPerOperation()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ConflictStatusCode = 409 };
        var service = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        var first = NewOperation("customer-1");
        var second = NewOperation("customer-2");
        await service.EnqueueAsync(first);
        await service.EnqueueAsync(second);

        await service.ProcessQueueAsync();

        Assert.Equal(2, service.Conflicts.Count);
        Assert.Contains(service.Conflicts, conflict => conflict.OperationId == first.Id);
        Assert.Contains(service.Conflicts, conflict => conflict.OperationId == second.Id);
        Assert.Equal(2, (await service.GetPendingAsync()).Count);
    }

    [Fact]
    public async Task Conflicts_PersistAcrossInstances()
    {
        var connectivity = new StubConnectivityService { CurrentState = ConnectionState.Online };
        var apiClient = new StubApiClient { ConflictStatusCode = 409 };
        var first = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await first.EnqueueAsync(NewOperation());
        await first.ProcessQueueAsync();

        var second = new SyncQueueService(connectivity, new PassThroughRetryPolicy(), apiClient, _filePath);
        await second.InitializeAsync();

        Assert.Single(second.Conflicts);
        Assert.Equal(SyncState.ConflictDetected, second.CurrentState);
    }

    private sealed class StubConnectivityService : IConnectivityService
    {
        public ConnectionState CurrentState { get; set; } = ConnectionState.Online;

        public event EventHandler<ConnectionState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task<ConnectionState> CheckAsync(CancellationToken cancellationToken = default) => Task.FromResult(CurrentState);

        public void Dispose()
        {
        }
    }

    private sealed class StubApiClient : IApiClient
    {
        public bool SucceedPost { get; set; }

        public Exception? ThrowOnPost { get; set; }

        public int? ConflictStatusCode { get; set; }

        public int PostCallCount { get; private set; }

        public Task<ApiResponse<TResponse>> GetAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default)
        {
            PostCallCount++;

            if (ThrowOnPost is not null)
            {
                throw ThrowOnPost;
            }

            if (ConflictStatusCode is int code)
            {
                return Task.FromResult(ApiResponseFactory.Failure<TResponse>(code, "conflict"));
            }

            return Task.FromResult(SucceedPost
                ? ApiResponseFactory.Success(default(TResponse)!, 200)
                : ApiResponseFactory.Failure<TResponse>(500, "server error"));
        }

        public Task<ApiResponse<TResponse>> PutAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<ApiResponse<TResponse>> DeleteAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<ApiResponse<TResponse>> PatchAsync<TResponse>(string path, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");

        public Task<ApiResponse<TResponse>> PatchAsync<TRequest, TResponse>(string path, TRequest body, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException("Not used by these tests.");
    }

    /// <summary>A no-retry <see cref="IRetryPolicy"/> - the real <see cref="RetryPolicy"/> would make failure-path tests slow (5 attempts with real exponential backoff delays) for no additional coverage, since retry timing itself is already covered by <c>Application.Tests.Security.RetryPolicyTests</c>.</summary>
    private sealed class PassThroughRetryPolicy : IRetryPolicy
    {
        public Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
            operation(cancellationToken);
    }
}
