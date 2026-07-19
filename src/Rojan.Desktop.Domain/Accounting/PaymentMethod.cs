namespace Rojan.Desktop.Domain.Accounting;

/// <summary>How a <see cref="Payment"/> was collected, as returned by <see cref="IAccountingRepository"/>.</summary>
public enum PaymentMethod
{
    Cash,
    Card,
    BankTransfer,
    GiftCard,
    StoreCredit,
}
