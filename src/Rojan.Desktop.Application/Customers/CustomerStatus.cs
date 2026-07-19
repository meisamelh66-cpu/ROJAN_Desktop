namespace Rojan.Desktop.Application.Customers;

/// <summary>
/// Application's own copy of <see cref="Rojan.Desktop.Domain.Customers.CustomerStatus"/> -
/// distinct from Domain, same reasoning as <c>Dashboard.TrendDirection</c>:
/// Presentation never binds to a Domain-shaped type, so anything it needs
/// gets an Application-owned equivalent, mapped explicitly by
/// <see cref="CustomerQueryService"/>.
/// </summary>
public enum CustomerStatus
{
    Lead,
    Prospect,
    Active,
    Vip,
    Inactive,
    Churned,
}
