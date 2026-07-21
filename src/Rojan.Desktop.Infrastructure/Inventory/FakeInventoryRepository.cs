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
            new ProductCategory("category-1", "مراقبت مو", "شامپو، نرم‌کننده و محصولات استایل‌دهی."),
            new ProductCategory("category-2", "محصولات رنگ مو", "رنگ دائم و نیمه‌دائم مو، اکسیدان و پودر دکلره."),
            new ProductCategory("category-3", "مراقبت ناخن", "لاک، ژل و لوازم مانیکور و پدیکور."),
            new ProductCategory("category-4", "مراقبت پوست", "پاک‌کننده صورت، ماسک و سرم‌های درمانی."),
            new ProductCategory("category-5", "اسپا و سلامت", "روغن ماساژ، رایحه‌درمانی و مواد مصرفی اسپا."),
            new ProductCategory("category-6", "ابزار و تجهیزات", "ابزار قابل‌استفاده مجدد، برس و تجهیزات سالن."),
        ];

        _suppliers =
        [
            new Supplier("supplier-1", "شرکت تأمین زیبایی درخشش", "مریم غفاری", "orders@glowbeautysupply.example", "021-77300201", SupplierStatus.Active),
            new Supplier("supplier-2", "محصولات حرفه‌ای درخشان", "داوود کریمی", "sales@radiantpro.example", "021-77300202", SupplierStatus.Active),
            new Supplier("supplier-3", "لوازم ضروری سالن لوکس", "سوگل ترابی", "accounts@luxesalonessentials.example", "021-77300203", SupplierStatus.Active),
            new Supplier("supplier-4", "بازرگانی شمال", "تورج باقری", "info@northlinewholesale.example", "021-77300204", SupplierStatus.Inactive),
        ];

        _products =
        [
            new Product("product-1", "HC-SHM-001", "شامپو مرطوب‌کننده ۱ لیتری", "category-1", "مراقبت مو", "supplier-1", "شرکت تأمین زیبایی درخشش",
                "180,000 تومان", ProductStatus.Active, "شامپو مرطوب‌کننده بدون سولفات، مخصوص فروش در سالن.", "org-1", "branch-1"),
            new Product("product-2", "HC-COND-002", "نرم‌کننده ترمیمی ۱ لیتری", "category-1", "مراقبت مو", "supplier-1", "شرکت تأمین زیبایی درخشش",
                "190,000 تومان", ProductStatus.Active, "نرم‌کننده ترمیم عمیق برای موهای آسیب‌دیده.", "org-1", "branch-1"),
            new Product("product-3", "CL-DEV-010", "اکسیدان رنگ ۲۰ ولوم", "category-2", "محصولات رنگ مو", "supplier-2", "محصولات حرفه‌ای درخشان",
                "120,000 تومان", ProductStatus.Active, "اکسیدان استاندارد برای رنگ دائم مو.", "org-1", "branch-1"),
            new Product("product-4", "CL-PERM-045", "رنگ دائم مو - قهوه‌ای شکلاتی", "category-2", "محصولات رنگ مو", "supplier-2", "محصولات حرفه‌ای درخشان",
                "140,000 تومان", ProductStatus.Active, "رنگ دائم مو، طیف قهوه‌ای شکلاتی.", "org-1", "branch-1"),
            new Product("product-5", "CL-BLCH-020", "پودر دکلره ۵۰۰ گرمی", "category-2", "محصولات رنگ مو", "supplier-2", "محصولات حرفه‌ای درخشان",
                "260,000 تومان", ProductStatus.Active, "پودر دکلره بدون گرد و غبار، مناسب بالیاژ.", "org-1", "branch-1"),
            new Product("product-6", "NL-GEL-030", "لاک ژل - کوارتز صورتی", "category-3", "مراقبت ناخن", "supplier-3", "لوازم ضروری سالن لوکس",
                "90,000 تومان", ProductStatus.Active, "لاک ژل ماندگار، رنگ کوارتز صورتی.", "org-1", "branch-2"),
            new Product("product-7", "NL-BASE-005", "ست بیس و تاپ کوت", "category-3", "مراقبت ناخن", "supplier-3", "لوازم ضروری سالن لوکس",
                "150,000 تومان", ProductStatus.Active, "ست بیس و تاپ کوت مخصوص مانیکور ژل.", "org-1", "branch-1"),
            new Product("product-8", "SK-MASK-012", "ماسک گلی ترمیمی ۲۵۰ میلی‌لیتری", "category-4", "مراقبت پوست", "supplier-1", "شرکت تأمین زیبایی درخشش",
                "220,000 تومان", ProductStatus.Active, "ماسک گلی پاک‌کننده عمیق، مناسب فیشیال.", "org-1", "branch-1"),
            new Product("product-9", "SP-OIL-008", "روغن ماساژ رایحه‌درمانی ۵۰۰ میلی‌لیتری", "category-5", "اسپا و سلامت", "supplier-4", "بازرگانی شمال",
                "160,000 تومان", ProductStatus.Active, "ترکیب روغن ماساژ با رایحه اسطوخودوس.", "org-1", "branch-1"),
            new Product("product-10", "TL-BRSH-090", "ست حرفه‌ای قلم‌موی رنگ", "category-6", "ابزار و تجهیزات", "supplier-3", "لوازم ضروری سالن لوکس",
                "340,000 تومان", ProductStatus.Discontinued, "مجموعه ۴ عددی قلم‌موی رنگ - با مدل جدیدتر جایگزین شده.", "org-2", "branch-3"),
        ];

        _inventoryItems =
        [
            new InventoryItem("item-1", "product-1", "شامپو مرطوب‌کننده ۱ لیتری", 42, 15),
            new InventoryItem("item-2", "product-2", "نرم‌کننده ترمیمی ۱ لیتری", 38, 15),
            new InventoryItem("item-3", "product-3", "اکسیدان رنگ ۲۰ ولوم", 8, 10),
            new InventoryItem("item-4", "product-4", "رنگ دائم مو - قهوه‌ای شکلاتی", 25, 10),
            new InventoryItem("item-5", "product-5", "پودر دکلره ۵۰۰ گرمی", 5, 8),
            new InventoryItem("item-6", "product-6", "لاک ژل - کوارتز صورتی", 60, 20),
            new InventoryItem("item-7", "product-7", "ست بیس و تاپ کوت", 18, 20),
            new InventoryItem("item-8", "product-8", "ماسک گلی ترمیمی ۲۵۰ میلی‌لیتری", 30, 12),
            new InventoryItem("item-9", "product-9", "روغن ماساژ رایحه‌درمانی ۵۰۰ میلی‌لیتری", 14, 10),
            new InventoryItem("item-10", "product-10", "ست حرفه‌ای قلم‌موی رنگ", 3, 5),
        ];

        _transactions =
        [
            new StockTransaction("txn-1", "product-1", "شامپو مرطوب‌کننده ۱ لیتری", StockTransactionType.Received, 48, now.AddDays(-30), "تحویل اولیه موجودی."),
            new StockTransaction("txn-2", "product-1", "شامپو مرطوب‌کننده ۱ لیتری", StockTransactionType.Sold, 6, now.AddDays(-5), "فروش خرده‌فروشی."),
            new StockTransaction("txn-3", "product-3", "اکسیدان رنگ ۲۰ ولوم", StockTransactionType.Received, 20, now.AddDays(-25), "تحویل اولیه موجودی."),
            new StockTransaction("txn-4", "product-3", "اکسیدان رنگ ۲۰ ولوم", StockTransactionType.Sold, 12, now.AddDays(-3), "مصرف‌شده در خدمات رنگ."),
            new StockTransaction("txn-5", "product-4", "رنگ دائم مو - قهوه‌ای شکلاتی", StockTransactionType.Received, 30, now.AddDays(-25), "تحویل اولیه موجودی."),
            new StockTransaction("txn-6", "product-4", "رنگ دائم مو - قهوه‌ای شکلاتی", StockTransactionType.Sold, 5, now.AddDays(-2), "مصرف‌شده در خدمات رنگ."),
            new StockTransaction("txn-7", "product-5", "پودر دکلره ۵۰۰ گرمی", StockTransactionType.Received, 10, now.AddDays(-20), "تحویل اولیه موجودی."),
            new StockTransaction("txn-8", "product-5", "پودر دکلره ۵۰۰ گرمی", StockTransactionType.Sold, 5, now.AddDays(-1), "مصرف‌شده برای خدمت بالیاژ."),
            new StockTransaction("txn-9", "product-10", "ست حرفه‌ای قلم‌موی رنگ", StockTransactionType.Damaged, 2, now.AddDays(-10), "موهای قلم‌مو در شست‌وشو آسیب دیده."),
        ];

        _serviceMappings =
        [
            new ServiceProductMapping("mapping-1", "service-2", "اصلاح رنگ ریشه", "product-3", "اکسیدان رنگ ۲۰ ولوم", 1),
            new ServiceProductMapping("mapping-2", "service-2", "اصلاح رنگ ریشه", "product-4", "رنگ دائم مو - قهوه‌ای شکلاتی", 1),
            new ServiceProductMapping("mapping-3", "service-3", "پکیج کامل - بالیاژ و استایل", "product-5", "پودر دکلره ۵۰۰ گرمی", 2),
            new ServiceProductMapping("mapping-4", "service-4", "مانیکور", "product-6", "لاک ژل - کوارتز صورتی", 1),
            new ServiceProductMapping("mapping-5", "service-4", "مانیکور", "product-7", "ست بیس و تاپ کوت", 1),
            new ServiceProductMapping("mapping-6", "service-5", "فیشیال ترمیمی", "product-8", "ماسک گلی ترمیمی ۲۵۰ میلی‌لیتری", 1),
            new ServiceProductMapping("mapping-7", "service-6", "ماساژ", "product-9", "روغن ماساژ رایحه‌درمانی ۵۰۰ میلی‌لیتری", 1),
            new ServiceProductMapping("mapping-8", "service-1", "کوتاهی و استایل مو", "product-1", "شامپو مرطوب‌کننده ۱ لیتری", 1),
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
