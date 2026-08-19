namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Reception Permission Contract Alignment: the narrow, booking-time-only counterpart to
/// <see cref="ICustomerCommandService.CreateCustomerAsync"/> - creates a customer identity
/// record (name/phone/email only, never <c>Company</c>/notes/tags) for the sole purpose of
/// attaching a walk-in customer to a booking. Backed by the backend's own
/// <c>CREATE_CUSTOMER_IDENTITY</c> permission rather than <c>MANAGE_CRM</c>, so a Reception
/// session that cannot reach <see cref="ICustomerCommandService.CreateCustomerAsync"/> can
/// still register a walk-in through this path. See
/// <c>ROJAN_Reception_Permission_Contract_Update_ADR_v1.md</c>.
/// </summary>
public interface ICustomerIdentityService
{
    public Task<CustomerIdentityDto> CreateCustomerIdentityAsync(string fullName, string? phoneNumber, string? email, CancellationToken cancellationToken = default);
}

/// <summary>The minimal customer shape <see cref="ICustomerIdentityService"/> deals in - deliberately narrower than <see cref="CustomerDto"/>, matching the backend's own <c>CustomerIdentityResponse</c> field-for-field (no <c>Company</c>, no tags, no lifetime value, no status).</summary>
public sealed record CustomerIdentityDto(string Id, string FullName, string? PhoneNumber, string? Email, bool Active);
