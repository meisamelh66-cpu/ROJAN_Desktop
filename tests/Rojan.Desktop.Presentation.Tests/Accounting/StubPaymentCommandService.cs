using Rojan.Desktop.Application.Accounting;

namespace Rojan.Desktop.Presentation.Tests.Accounting;

/// <summary>Records every call it receives so ViewModel tests can assert a command was invoked with the right arguments - same reasoning as Inventory.StubInventoryCommandService.</summary>
internal sealed class StubPaymentCommandService : IPaymentCommandService
{
    private readonly Func<RecordPaymentRequest, CancellationToken, Task<PaymentDto>>? _recordPayment;

    public List<RecordPaymentRequest> RecordRequests { get; } = [];

    public StubPaymentCommandService(Func<RecordPaymentRequest, CancellationToken, Task<PaymentDto>>? recordPayment = null)
    {
        _recordPayment = recordPayment;
    }

    public Task<PaymentDto> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken cancellationToken = default)
    {
        RecordRequests.Add(request);
        return _recordPayment?.Invoke(request, cancellationToken) ?? Task.FromResult(new PaymentDto(
            "payment-new", request.InvoiceId, "customer-1", "Amelia Hart", request.Method, request.Amount, DateTimeOffset.Now, request.CashSessionId, request.Notes));
    }

    public Task<CashSessionDto> OpenCashSessionAsync(string cashierName, decimal openingFloat, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CashSessionDto("session-new", cashierName, DateTimeOffset.Now, null, openingFloat, null, CashSessionStatus.Open));

    public Task<CashSessionDto> CloseCashSessionAsync(string sessionId, decimal closingBalance, CancellationToken cancellationToken = default) =>
        Task.FromResult(new CashSessionDto(sessionId, "Jordan Lee", DateTimeOffset.UnixEpoch, DateTimeOffset.Now, 0m, closingBalance, CashSessionStatus.Closed));
}
