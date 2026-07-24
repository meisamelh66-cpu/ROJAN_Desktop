namespace Rojan.Server.Application.Customers;

/// <summary>
/// Thrown by <c>ICustomerService.GetCustomerAsync</c>/<c>UpdateCustomerAsync</c>
/// when the requested customer does not exist within the caller's own
/// organization - either because it genuinely does not exist, or because
/// it belongs to a different tenant. Both cases throw this exact same
/// exception, deliberately: <c>Domain.Customers.ICustomerRepository.GetByIdAsync</c>
/// itself cannot distinguish the two (see its own doc comment), and
/// <c>Api.Controllers.CustomersController</c> maps this to a plain 404 -
/// an API response must never confirm that another tenant's customer
/// exists, the same "do not leak" reasoning
/// <c>Application.Authentication.InvalidCredentialsException</c> already
/// establishes for login.
/// </summary>
public sealed class CustomerNotFoundException(string customerId) : Exception($"Customer '{customerId}' was not found.");
