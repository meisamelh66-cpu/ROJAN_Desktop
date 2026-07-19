namespace Rojan.Desktop.Application.Accounting;

/// <summary>Application-layer shape of a cash drawer session, mapped from <see cref="Rojan.Desktop.Domain.Accounting.CashSession"/> by <see cref="AccountingMapper"/>.</summary>
public sealed record CashSessionDto(
    string Id,
    string CashierName,
    DateTimeOffset OpenedAt,
    DateTimeOffset? ClosedAt,
    decimal OpeningFloat,
    decimal? ClosingBalance,
    CashSessionStatus Status);
