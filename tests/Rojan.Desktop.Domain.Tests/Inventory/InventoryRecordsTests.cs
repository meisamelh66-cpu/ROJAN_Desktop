using Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Domain.Tests.Inventory;

/// <summary>Minimal smoke coverage - see the equivalent note on Customers.CustomerTests for why Domain testing stays light.</summary>
public sealed class InventoryRecordsTests
{
    [Fact]
    public void Product_SameValues_AreEqual()
    {
        var first = new Product("product-1", "SKU-1", "Test Product", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, "Description");
        var second = new Product("product-1", "SKU-1", "Test Product", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, "Description");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Product_DifferentStatus_AreNotEqual()
    {
        var first = new Product("product-1", "SKU-1", "Test Product", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.", "$18", ProductStatus.Active, "Description");
        var second = first with { Status = ProductStatus.Discontinued };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void InventoryItem_DifferentQuantity_AreNotEqual()
    {
        var first = new InventoryItem("item-1", "product-1", "Test Product", 10, 5);
        var second = first with { QuantityOnHand = 3 };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void StockTransaction_DifferentType_AreNotEqual()
    {
        var occurredAt = DateTimeOffset.UnixEpoch;
        var first = new StockTransaction("txn-1", "product-1", "Test Product", StockTransactionType.Received, 10, occurredAt, string.Empty);
        var second = first with { Type = StockTransactionType.Sold };

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void ServiceProductMapping_SameValues_AreEqual()
    {
        var first = new ServiceProductMapping("mapping-1", "service-1", "Haircut & Style", "product-1", "Test Product", 1);
        var second = new ServiceProductMapping("mapping-1", "service-1", "Haircut & Style", "product-1", "Test Product", 1);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Supplier_DifferentStatus_AreNotEqual()
    {
        var first = new Supplier("supplier-1", "Glow Beauty Supply Co.", "Maria Gonzalez", "orders@glowbeautysupply.example", "+1 (555) 030-2001", SupplierStatus.Active);
        var second = first with { Status = SupplierStatus.Inactive };

        Assert.NotEqual(first, second);
    }
}
