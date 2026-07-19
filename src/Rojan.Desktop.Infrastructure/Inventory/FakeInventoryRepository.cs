using Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Infrastructure.Inventory;

/// <summary>
/// In-memory <see cref="IInventoryRepository"/> providing static sample
/// data - Phase 17 explicitly has no backend integration yet, same as
/// every other vertical slice in this app. Instance (not static) mutable
/// state, same reasoning as <c>Customers.FakeCustomerRepository</c>: this
/// fake has real create/transaction/mapping commands, so it needs to
/// remember writes for the app's lifetime - registered as a DI singleton
/// (see Infrastructure's ServiceCollectionExtensions). Service-mapping
/// seed data uses the real service ids already seeded in
/// <c>Services.FakeServiceRepository</c> ("service-1".."service-9") for a
/// cohesive demo - not a real cross-slice link, just consistent naming,
/// same reasoning as every other cross-slice reference in this app. The
/// small artificial delays are deliberate, same reasoning as every other
/// fake repository: without them, Loading states would never actually be
/// observable when running the app.
/// </summary>
public sealed class FakeInventoryRepository : IInventoryRepository
{
    private readonly List<Product> _products;
    private readonly List<ProductCategory> _categories;
    private readonly List<Supplier> _suppliers;
    private readonly List<InventoryItem> _inventoryItems;
    private readonly List<StockTransaction> _transactions;
    private readonly List<ServiceProductMapping> _serviceMappings;

    public FakeInventoryRepository()
    {
        var now = DateTimeOffset.Now;

        _categories =
        [
            new ProductCategory("category-1", "Hair Care", "Shampoos, conditioners, and styling products."),
            new ProductCategory("category-2", "Colour Products", "Permanent and semi-permanent hair colour, developers, and lighteners."),
            new ProductCategory("category-3", "Nail Care", "Polishes, gels, and manicure/pedicure supplies."),
            new ProductCategory("category-4", "Skin Care", "Facial cleansers, masks, and treatment serums."),
            new ProductCategory("category-5", "Spa & Wellness", "Massage oils, aromatherapy, and spa consumables."),
            new ProductCategory("category-6", "Tools & Equipment", "Reusable tools, brushes, and salon equipment."),
        ];

        _suppliers =
        [
            new Supplier("supplier-1", "Glow Beauty Supply Co.", "Maria Gonzalez", "orders@glowbeautysupply.example", "+1 (555) 030-2001", SupplierStatus.Active),
            new Supplier("supplier-2", "Radiant Professional Products", "David Kim", "sales@radiantpro.example", "+1 (555) 030-2002", SupplierStatus.Active),
            new Supplier("supplier-3", "Luxe Salon Essentials", "Sophie Turner", "accounts@luxesalonessentials.example", "+1 (555) 030-2003", SupplierStatus.Active),
            new Supplier("supplier-4", "Northline Wholesale", "Tom Baker", "info@northlinewholesale.example", "+1 (555) 030-2004", SupplierStatus.Inactive),
        ];

        _products =
        [
            new Product("product-1", "HC-SHM-001", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
                "$18", ProductStatus.Active, "Sulfate-free hydrating shampoo for salon retail."),
            new Product("product-2", "HC-COND-002", "Repair Conditioner 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
                "$19", ProductStatus.Active, "Deep-repair conditioner for damaged hair."),
            new Product("product-3", "CL-DEV-010", "Colour Developer 20 Vol", "category-2", "Colour Products", "supplier-2", "Radiant Professional Products",
                "$12", ProductStatus.Active, "Standard-strength developer for permanent colour."),
            new Product("product-4", "CL-PERM-045", "Permanent Hair Colour - Chocolate Brown", "category-2", "Colour Products", "supplier-2", "Radiant Professional Products",
                "$14", ProductStatus.Active, "Permanent colour, chocolate brown shade."),
            new Product("product-5", "CL-BLCH-020", "Lightening Powder 500g", "category-2", "Colour Products", "supplier-2", "Radiant Professional Products",
                "$26", ProductStatus.Active, "Dust-free lightening powder for balayage work."),
            new Product("product-6", "NL-GEL-030", "Gel Polish - Rose Quartz", "category-3", "Nail Care", "supplier-3", "Luxe Salon Essentials",
                "$9", ProductStatus.Active, "Long-wear gel polish, rose quartz shade."),
            new Product("product-7", "NL-BASE-005", "Base & Top Coat Duo", "category-3", "Nail Care", "supplier-3", "Luxe Salon Essentials",
                "$15", ProductStatus.Active, "Base and top coat set for gel manicures."),
            new Product("product-8", "SK-MASK-012", "Renewal Clay Mask 250ml", "category-4", "Skin Care", "supplier-1", "Glow Beauty Supply Co.",
                "$22", ProductStatus.Active, "Deep-cleansing clay mask for facial treatments."),
            new Product("product-9", "SP-OIL-008", "Aromatherapy Massage Oil 500ml", "category-5", "Spa & Wellness", "supplier-4", "Northline Wholesale",
                "$16", ProductStatus.Active, "Lavender-scented massage oil blend."),
            new Product("product-10", "TL-BRSH-090", "Professional Colour Brush Set", "category-6", "Tools & Equipment", "supplier-3", "Luxe Salon Essentials",
                "$34", ProductStatus.Discontinued, "Set of 4 colour application brushes - replaced by a newer model."),
        ];

        _inventoryItems =
        [
            new InventoryItem("item-1", "product-1", "Hydrating Shampoo 1L", 42, 15),
            new InventoryItem("item-2", "product-2", "Repair Conditioner 1L", 38, 15),
            new InventoryItem("item-3", "product-3", "Colour Developer 20 Vol", 8, 10),
            new InventoryItem("item-4", "product-4", "Permanent Hair Colour - Chocolate Brown", 25, 10),
            new InventoryItem("item-5", "product-5", "Lightening Powder 500g", 5, 8),
            new InventoryItem("item-6", "product-6", "Gel Polish - Rose Quartz", 60, 20),
            new InventoryItem("item-7", "product-7", "Base & Top Coat Duo", 18, 20),
            new InventoryItem("item-8", "product-8", "Renewal Clay Mask 250ml", 30, 12),
            new InventoryItem("item-9", "product-9", "Aromatherapy Massage Oil 500ml", 14, 10),
            new InventoryItem("item-10", "product-10", "Professional Colour Brush Set", 3, 5),
        ];

        _transactions =
        [
            new StockTransaction("txn-1", "product-1", "Hydrating Shampoo 1L", StockTransactionType.Received, 48, now.AddDays(-30), "Initial stock delivery."),
            new StockTransaction("txn-2", "product-1", "Hydrating Shampoo 1L", StockTransactionType.Sold, 6, now.AddDays(-5), "Retail sales."),
            new StockTransaction("txn-3", "product-3", "Colour Developer 20 Vol", StockTransactionType.Received, 20, now.AddDays(-25), "Initial stock delivery."),
            new StockTransaction("txn-4", "product-3", "Colour Developer 20 Vol", StockTransactionType.Sold, 12, now.AddDays(-3), "Used across colour services."),
            new StockTransaction("txn-5", "product-4", "Permanent Hair Colour - Chocolate Brown", StockTransactionType.Received, 30, now.AddDays(-25), "Initial stock delivery."),
            new StockTransaction("txn-6", "product-4", "Permanent Hair Colour - Chocolate Brown", StockTransactionType.Sold, 5, now.AddDays(-2), "Used across colour services."),
            new StockTransaction("txn-7", "product-5", "Lightening Powder 500g", StockTransactionType.Received, 10, now.AddDays(-20), "Initial stock delivery."),
            new StockTransaction("txn-8", "product-5", "Lightening Powder 500g", StockTransactionType.Sold, 5, now.AddDays(-1), "Used for balayage service."),
            new StockTransaction("txn-9", "product-10", "Professional Colour Brush Set", StockTransactionType.Damaged, 2, now.AddDays(-10), "Bristles damaged in cleaning."),
        ];

        _serviceMappings =
        [
            new ServiceProductMapping("mapping-1", "service-2", "Colour Touch-Up", "product-3", "Colour Developer 20 Vol", 1),
            new ServiceProductMapping("mapping-2", "service-2", "Colour Touch-Up", "product-4", "Permanent Hair Colour - Chocolate Brown", 1),
            new ServiceProductMapping("mapping-3", "service-3", "Full Package - Balayage & Style", "product-5", "Lightening Powder 500g", 2),
            new ServiceProductMapping("mapping-4", "service-4", "Manicure", "product-6", "Gel Polish - Rose Quartz", 1),
            new ServiceProductMapping("mapping-5", "service-4", "Manicure", "product-7", "Base & Top Coat Duo", 1),
            new ServiceProductMapping("mapping-6", "service-5", "Facial Renewal", "product-8", "Renewal Clay Mask 250ml", 1),
            new ServiceProductMapping("mapping-7", "service-6", "Massage", "product-9", "Aromatherapy Massage Oil 500ml", 1),
            new ServiceProductMapping("mapping-8", "service-1", "Haircut & Style", "product-1", "Hydrating Shampoo 1L", 1),
        ];
    }

    public async Task<IReadOnlyList<Product>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _products.ToList();
    }

    public async Task<Product?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _products.FirstOrDefault(product => product.Id == productId);
    }

    public async Task<Product> CreateProductAsync(Product product, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _products.Add(product);
        return product;
    }

    public async Task<IReadOnlyList<ProductCategory>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _categories.ToList();
    }

    public async Task<ProductCategory> CreateCategoryAsync(ProductCategory category, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _categories.Add(category);
        return category;
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _suppliers.ToList();
    }

    public async Task<Supplier> CreateSupplierAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _suppliers.Add(supplier);
        return supplier;
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(400, cancellationToken).ConfigureAwait(true);
        return _inventoryItems.ToList();
    }

    public async Task<InventoryItem?> GetInventoryItemByProductIdAsync(string productId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _inventoryItems.FirstOrDefault(item => item.ProductId == productId);
    }

    public async Task<InventoryItem> CreateInventoryItemAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _inventoryItems.Add(item);
        return item;
    }

    public async Task<InventoryItem> UpdateInventoryQuantityAsync(string productId, int quantityOnHand, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        var index = _inventoryItems.FindIndex(item => item.ProductId == productId);
        if (index < 0)
        {
            throw new InvalidOperationException($"Product '{productId}' has no inventory record.");
        }

        var updated = _inventoryItems[index] with { QuantityOnHand = quantityOnHand };
        _inventoryItems[index] = updated;
        return updated;
    }

    public async Task<IReadOnlyList<StockTransaction>> GetTransactionsForProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _transactions.Where(transaction => transaction.ProductId == productId).ToList();
    }

    public async Task<StockTransaction> RecordTransactionAsync(StockTransaction transaction, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _transactions.Add(transaction);
        return transaction;
    }

    public async Task<IReadOnlyList<ServiceProductMapping>> GetServiceMappingsForProductAsync(string productId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        return _serviceMappings.Where(mapping => mapping.ProductId == productId).ToList();
    }

    public async Task<ServiceProductMapping> MapProductToServiceAsync(ServiceProductMapping mapping, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _serviceMappings.Add(mapping);
        return mapping;
    }

    public async Task UnmapProductFromServiceAsync(string productId, string mappingId, CancellationToken cancellationToken = default)
    {
        await Task.Delay(200, cancellationToken).ConfigureAwait(true);
        _serviceMappings.RemoveAll(mapping => mapping.ProductId == productId && mapping.Id == mappingId);
    }
}
