namespace Rojan.Desktop.Domain.Accounting;

/// <summary>Whether a POS cash drawer session is currently active, as returned by <see cref="IAccountingRepository"/>.</summary>
public enum CashSessionStatus
{
    Open,
    Closed,
}
