namespace Rojan.Desktop.Application.HR;

/// <summary>Application-layer shape of a commission transaction, mapped from <see cref="Rojan.Desktop.Domain.HR.CommissionTransaction"/> by <see cref="HrMapper"/>.</summary>
public sealed record CommissionTransactionDto(
    string Id,
    string EmployeeId,
    string EmployeeName,
    string InvoiceId,
    string ServiceName,
    decimal GrossAmount,
    decimal CommissionAmount,
    DateTimeOffset EarnedAt);
