namespace Rojan.Desktop.Application.Accounting;

/// <summary>The Revenue KPI card data set - composed over payments/invoices in Application, same "read the set, compose in Application" convention every other module's derived numbers follow.</summary>
public sealed record RevenueSummaryDto(
    decimal TotalRevenue,
    decimal TodayRevenue,
    decimal OutstandingBalance,
    int PaidInvoiceCount,
    int OutstandingInvoiceCount);
