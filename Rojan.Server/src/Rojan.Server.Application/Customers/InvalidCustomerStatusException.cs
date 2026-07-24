namespace Rojan.Server.Application.Customers;

/// <summary>Thrown by <c>ICustomerService.UpdateCustomerAsync</c> when <c>UpdateCustomerRequest.Status</c> is not a recognized <c>Domain.Customers.CustomerStatus</c> value, or names a transition <c>Domain.Customers.CustomerRules.IsValidTransition</c> rejects - a client input error, mapped to 400.</summary>
public sealed class InvalidCustomerStatusException(string message) : Exception(message);
