using Rojan.Desktop.Application.BookingWorkflow;
using Rojan.Desktop.Application.Customers;

namespace Rojan.Desktop.Application.Tests.BookingWorkflow;

/// <summary>
/// Reception Permission Contract Alignment: minimal <see cref="ICustomerIdentityService"/> test
/// double, replacing the old <c>StubCustomerCommandService</c> now that
/// <see cref="BookingWorkflowService.CreateGuestCustomerAsync(string, string, CancellationToken)"/>
/// calls this narrower service instead - same "only what's exercised" scope as the sibling stubs
/// in this folder.
/// </summary>
internal sealed class StubCustomerIdentityService : ICustomerIdentityService
{
    private readonly Func<string, string?, string?, CustomerIdentityDto> _createCustomerIdentity;

    public List<(string FullName, string? PhoneNumber, string? Email)> CreateRequests { get; } = [];

    public StubCustomerIdentityService(Func<string, string?, string?, CustomerIdentityDto>? createCustomerIdentity = null)
    {
        _createCustomerIdentity = createCustomerIdentity ?? ((fullName, phoneNumber, email) =>
            new CustomerIdentityDto("guest-new", fullName, phoneNumber, email, Active: true));
    }

    public Task<CustomerIdentityDto> CreateCustomerIdentityAsync(string fullName, string? phoneNumber, string? email, CancellationToken cancellationToken = default)
    {
        CreateRequests.Add((fullName, phoneNumber, email));
        return Task.FromResult(_createCustomerIdentity(fullName, phoneNumber, email));
    }
}
