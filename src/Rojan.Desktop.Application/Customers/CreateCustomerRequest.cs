namespace Rojan.Desktop.Application.Customers;

/// <summary>Input to <see cref="ICustomerCommandService.CreateCustomerAsync"/> - new customers always start as <c>Lead</c>, so Status isn't a caller-supplied field.</summary>
public sealed record CreateCustomerRequest(string FullName, string Company, string Email, string Phone, string Notes);
