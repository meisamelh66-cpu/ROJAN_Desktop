namespace Rojan.Desktop.Domain.Accounting;

/// <summary>A POS cash drawer session - opened with a starting float, closed with a counted closing balance, as returned by <see cref="IAccountingRepository"/>. <see cref="ClosedAt"/>/<see cref="ClosingBalance"/> are null while <see cref="Status"/> is <see cref="CashSessionStatus.Open"/>.</summary>
public sealed record CashSession(
    string Id,
    string CashierName,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningFloat,
    decimal? ClosingBalance,
    CashSessionStatus Status);
