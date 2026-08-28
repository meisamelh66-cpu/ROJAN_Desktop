using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Presentation.Tests.Customers;

/// <summary>
/// Records every call it receives so ViewModel tests can assert a command
/// was invoked with the right arguments, without a mocking library.
/// Returns simple, deterministic default DTOs unless a test overrides the
/// relevant *Result delegate.
/// </summary>
internal sealed class StubCustomerCommandService : ICustomerCommandService
{
    public List<CreateCustomerRequest> CreateRequests { get; } = [];

    public List<UpdateCustomerRequest> UpdateRequests { get; } = [];

    public List<(string CustomerId, string Text)> AddNoteCalls { get; } = [];

    public List<(string CustomerId, string Label)> AddTagCalls { get; } = [];

    public List<(string CustomerId, string TagId)> RemoveTagCalls { get; } = [];

    /// <summary>
    /// Optional hook run after a customer is created, before the DTO is
    /// returned - lets a test simulate a real repository's behavior (the
    /// created customer becoming visible to a subsequent reload) by adding
    /// it to whatever backing list the paired query-service stub reads
    /// from, without coupling this stub to that one directly.
    /// </summary>
    public Action<CreateCustomerRequest, CustomerDto>? OnCustomerCreated { get; set; }

    /// <summary>Production Hardening (missing-guard sweep): when set, the matching command throws this instead of succeeding - lets a test exercise the ViewModel's new try/catch without a real backend failure. Same seam pattern as StubServiceCommandService.UpdateServiceException.</summary>
    public Exception? CreateCustomerException { get; set; }

    public Exception? UpdateCustomerException { get; set; }

    public Exception? AddNoteException { get; set; }

    public Exception? AddTagException { get; set; }

    public Exception? RemoveTagException { get; set; }

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        if (CreateCustomerException is not null)
        {
            return Task.FromException<CustomerDto>(CreateCustomerException);
        }

        var dto = new CustomerDto(
            "new-customer", request.FullName, request.Company, request.Email, request.Phone,
            CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, request.Notes, "org-1", "branch-1");
        OnCustomerCreated?.Invoke(request, dto);
        return Task.FromResult(dto);
    }

    public Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        UpdateRequests.Add(request);
        if (UpdateCustomerException is not null)
        {
            return Task.FromException<CustomerDto>(UpdateCustomerException);
        }

        return Task.FromResult(new CustomerDto(
            request.Id, request.FullName, request.Company, request.Email, request.Phone,
            request.Status, request.LifetimeValue, DateTimeOffset.UnixEpoch, request.Notes, "org-1", "branch-1"));
    }

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default)
    {
        AddNoteCalls.Add((customerId, text));
        return AddNoteException is not null
            ? Task.FromException<CustomerNoteDto>(AddNoteException)
            : Task.FromResult(new CustomerNoteDto("new-note", customerId, text, DateTimeOffset.UnixEpoch));
    }

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default)
    {
        AddTagCalls.Add((customerId, label));
        return AddTagException is not null
            ? Task.FromException<CustomerTagDto>(AddTagException)
            : Task.FromResult(new CustomerTagDto("new-tag", customerId, label, DateTimeOffset.UnixEpoch));
    }

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default)
    {
        RemoveTagCalls.Add((customerId, tagId));
        return RemoveTagException is not null ? Task.FromException(RemoveTagException) : Task.CompletedTask;
    }
}
