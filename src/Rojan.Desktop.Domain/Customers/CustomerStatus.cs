namespace Rojan.Desktop.Domain.Customers;

/// <summary>Relationship stage of a customer record, as returned by <see cref="ICustomerRepository"/>.</summary>
public enum CustomerStatus
{
    Lead,
    Prospect,
    Active,
    Vip,
    Inactive,
    Churned,
}
