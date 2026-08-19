using Rojan.Desktop.Application.Api;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Customers;
using Rojan.Desktop.Application.Salons;

namespace Rojan.Desktop.Infrastructure.Customers;

/// <summary>
/// Real, backend-connected <see cref="ICustomerIdentityService"/> - calls ROJAN_Backend's
/// <c>POST /customers/identity</c>, the structurally-restricted counterpart
/// <see cref="BackendCustomerRepository"/>'s own <c>CreateCustomerAsync</c> calls at
/// <c>/customers</c>. See <see cref="ICustomerIdentityService"/>'s own doc comment for why this
/// is a separate path rather than a variant of that method.
/// </summary>
public sealed class BackendCustomerIdentityService(IApiClient apiClient, ISalonContextService salonContextService) : ICustomerIdentityService
{
    public async Task<CustomerIdentityDto> CreateCustomerIdentityAsync(string fullName, string? phoneNumber, string? email, CancellationToken cancellationToken = default)
    {
        var salonId = await salonContextService.GetSalonIdAsync(cancellationToken).ConfigureAwait(false)
            ?? throw new ApiException("The signed-in user does not manage or work at any salon yet - there is nothing to create a customer for.");

        var request = new CreateCustomerIdentityRequest(fullName, NullIfEmpty(phoneNumber), NullIfEmpty(email));
        var response = await apiClient
            .PostAsync<CreateCustomerIdentityRequest, CustomerIdentityResponse>($"/api/v1/salons/{salonId}/customers/identity", request, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccess || response.Data is null)
        {
            throw new ApiException($"Failed to create a customer identity (status {response.StatusCode}): {response.ErrorMessage}");
        }

        return new CustomerIdentityDto(response.Data.Id, response.Data.FullName, response.Data.PhoneNumber, response.Data.Email, response.Data.Active);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
