namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application's own copy of <see cref="Rojan.Desktop.Domain.Accounting.CashSessionStatus"/> - distinct from Domain, same reasoning as <c>Customers.CustomerStatus</c>.</summary>
public enum CashSessionStatus
{
    Open,
    Closed,
}
