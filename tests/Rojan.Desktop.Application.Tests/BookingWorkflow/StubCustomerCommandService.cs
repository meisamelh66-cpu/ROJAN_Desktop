using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>
/// Reception Stabilization Sprint: minimal <see cref="ICustomerCommandService"/> test double -
/// only <see cref="CreateCustomerAsync"/> is exercised by <see cref="BookingWorkflowServiceTests"/>'s
/// <c>CreateGuestCustomerAsync</c> coverage, same "only what's exercised" scope as the sibling
/// stubs in this folder.
/// </summary>
internal sealed class StubCustomerCommandService : ICustomerCommandService
{
    private readonly Func<CreateCustomerRequest, CustomerDto> _createCustomer;

    public List<CreateCustomerRequest> CreateRequests { get; } = [];

    public StubCustomerCommandService(Func<CreateCustomerRequest, CustomerDto>? createCustomer = null)
    {
        _createCustomer = createCustomer ?? (request => new CustomerDto(
            "guest-new", request.FullName, request.Company, request.Email, request.Phone,
            CustomerStatus.Lead, "$0", DateTimeOffset.UnixEpoch, request.Notes, "org-1", "branch-1", UserId: null));
    }

    public Task<CustomerDto> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add(request);
        return Task.FromResult(_createCustomer(request));
    }

    public Task<CustomerDto> UpdateCustomerAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not exercised by BookingWorkflowServiceTests.");

    public Task<CustomerNoteDto> AddNoteAsync(string customerId, string text, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not exercised by BookingWorkflowServiceTests.");

    public Task<CustomerTagDto> AddTagAsync(string customerId, string label, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not exercised by BookingWorkflowServiceTests.");

    public Task RemoveTagAsync(string customerId, string tagId, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("Not exercised by BookingWorkflowServiceTests.");
}
