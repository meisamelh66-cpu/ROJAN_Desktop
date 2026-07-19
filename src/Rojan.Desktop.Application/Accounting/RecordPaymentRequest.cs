namespace Rojan.Desktop.Application.Accounting;

/// <summary>Input to <see cref="IPaymentCommandService.RecordPaymentAsync"/> - the POS checkout's payment step. <see cref="CashSessionId"/> is empty for non-cash methods.</summary>
public sealed record RecordPaymentRequest(string InvoiceId, PaymentMethod Method, decimal Amount, string CashSessionId, string Notes);
