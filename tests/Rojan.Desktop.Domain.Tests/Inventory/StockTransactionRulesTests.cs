using Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Domain.Tests.Inventory;

public sealed class StockTransactionRulesTests
{
    [Theory]
    [InlineData(StockTransactionType.Received, 5, true)]
    [InlineData(StockTransactionType.Received, 0, false)]
    [InlineData(StockTransactionType.Received, -5, false)]
    [InlineData(StockTransactionType.Sold, 5, true)]
    [InlineData(StockTransactionType.Sold, 0, false)]
    [InlineData(StockTransactionType.Adjustment, 5, true)]
    [InlineData(StockTransactionType.Adjustment, -5, true)]
    [InlineData(StockTransactionType.Adjustment, 0, false)]
    public void IsValidQuantity_VariousTypesAndQuantities_MatchesExpected(StockTransactionType type, int quantity, bool expected)
    {
        Assert.Equal(expected, StockTransactionRules.IsValidQuantity(type, quantity));
    }

    [Theory]
    [InlineData(10, StockTransactionType.Received, 5, 15)]
    [InlineData(10, StockTransactionType.Returned, 5, 15)]
    [InlineData(10, StockTransactionType.Sold, 4, 6)]
    [InlineData(10, StockTransactionType.Damaged, 4, 6)]
    [InlineData(10, StockTransactionType.Adjustment, 5, 15)]
    [InlineData(10, StockTransactionType.Adjustment, -5, 5)]
    public void Apply_VariousTransactions_ComputesExpectedQuantity(int currentQuantity, StockTransactionType type, int quantity, int expected)
    {
        Assert.Equal(expected, StockTransactionRules.Apply(currentQuantity, type, quantity));
    }

    [Fact]
    public void Apply_ResultWouldGoNegative_ClampsAtZero()
    {
        var result = StockTransactionRules.Apply(3, StockTransactionType.Sold, 10);

        Assert.Equal(0, result);
    }
}
