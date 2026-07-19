namespace Rojan.Desktop.Domain.HR;

/// <summary>
/// One commission earned by one employee from one Accounting invoice -
/// the "Booking -> Specialist performs service -> Accounting records
/// payment -> CommissionTransaction generated" integration's output.
/// <see cref="InvoiceId"/> is a free-text cross-slice reference to
/// <c>Application.Accounting.InvoiceDto.Id</c>, same reasoning as every
/// other cross-slice reference in this app - HR reads Accounting's own
/// published query services to discover invoices/bookings, never
/// modifying Accounting's code.
/// </summary>
public sealed record CommissionTransaction(
    string Id,
    string EmployeeId,
    string EmployeeName,
    string InvoiceId,
    string ServiceName,
    decimal GrossAmount,
    decimal CommissionAmount,
    DateTimeOffset EarnedAt);
