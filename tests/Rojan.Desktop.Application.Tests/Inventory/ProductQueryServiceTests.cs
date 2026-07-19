using Rojan.Desktop.Application.Inventory;
using DomainInventory = Rojan.Desktop.Domain.Inventory;

namespace Rojan.Desktop.Application.Tests.Inventory;

public sealed class ProductQueryServiceTests
{
    private static DomainInventory.Product MakeProduct(string id = "product-1") =>
        new(id, "SKU-1", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
            "$18", DomainInventory.ProductStatus.Active, "Sulfate-free hydrating shampoo.");

    [Fact]
    public async Task GetProductsAsync_RepositoryReturnsProducts_MapsEveryFieldToDto()
    {
        var product = MakeProduct();
        var repository = new StubInventoryRepository([product]);
        var sut = new ProductQueryService(repository);

        var result = await sut.GetProductsAsync();

        var dto = Assert.Single(result);
        Assert.Equal(product.Id, dto.Id);
        Assert.Equal(product.Sku, dto.Sku);
        Assert.Equal(product.Name, dto.Name);
        Assert.Equal(product.CategoryId, dto.CategoryId);
        Assert.Equal(product.CategoryName, dto.CategoryName);
        Assert.Equal(product.SupplierId, dto.SupplierId);
        Assert.Equal(product.SupplierName, dto.SupplierName);
        Assert.Equal(product.UnitPrice, dto.UnitPrice);
        Assert.Equal(ProductStatus.Active, dto.Status);
        Assert.Equal(product.Description, dto.Description);
    }

    [Fact]
    public async Task GetProductsAsync_RepositoryReturnsEmptyList_ReturnsEmptyList()
    {
        var repository = new StubInventoryRepository([]);
        var sut = new ProductQueryService(repository);

        var result = await sut.GetProductsAsync();

        Assert.Empty(result);
    }

    private static IReadOnlyList<DomainInventory.Product> MakeSearchFixture() =>
    [
        new("product-1", "HC-SHM-001", "Hydrating Shampoo 1L", "category-1", "Hair Care", "supplier-1", "Glow Beauty Supply Co.",
            "$18", DomainInventory.ProductStatus.Active, "Sulfate-free hydrating shampoo."),
        new("product-2", "NL-GEL-030", "Gel Polish - Rose Quartz", "category-3", "Nail Care", "supplier-3", "Luxe Salon Essentials",
            "$9", DomainInventory.ProductStatus.Active, "Long-wear gel polish."),
        new("product-3", "SK-MASK-012", "Renewal Clay Mask 250ml", "category-4", "Skin Care", "supplier-1", "Glow Beauty Supply Co.",
            "$22", DomainInventory.ProductStatus.Active, "Deep-cleansing clay mask."),
    ];

    [Fact]
    public async Task SearchProductsAsync_TextMatchesName_ReturnsOnlyThatProduct()
    {
        var repository = new StubInventoryRepository(MakeSearchFixture());
        var sut = new ProductQueryService(repository);

        var result = await sut.SearchProductsAsync("gel polish");

        Assert.Equal("product-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchProductsAsync_TextMatchesSku_ReturnsOnlyThatProduct()
    {
        var repository = new StubInventoryRepository(MakeSearchFixture());
        var sut = new ProductQueryService(repository);

        var result = await sut.SearchProductsAsync("sk-mask");

        Assert.Equal("product-3", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchProductsAsync_TextMatchesCategory_ReturnsOnlyThatProduct()
    {
        var repository = new StubInventoryRepository(MakeSearchFixture());
        var sut = new ProductQueryService(repository);

        var result = await sut.SearchProductsAsync("nail care");

        Assert.Equal("product-2", Assert.Single(result).Id);
    }

    [Fact]
    public async Task SearchProductsAsync_EmptySearchText_ReturnsEveryProduct()
    {
        var repository = new StubInventoryRepository(MakeSearchFixture());
        var sut = new ProductQueryService(repository);

        var result = await sut.SearchProductsAsync(string.Empty);

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task SearchProductsAsync_NoMatch_ReturnsEmptyList()
    {
        var repository = new StubInventoryRepository(MakeSearchFixture());
        var sut = new ProductQueryService(repository);

        var result = await sut.SearchProductsAsync("no-such-product");

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetCategoriesAsync_RepositoryReturnsCategories_MapsEveryField()
    {
        var repository = new StubInventoryRepository();
        repository.Categories.Add(new DomainInventory.ProductCategory("category-1", "Hair Care", "Shampoos and conditioners."));
        var sut = new ProductQueryService(repository);

        var result = await sut.GetCategoriesAsync();

        var dto = Assert.Single(result);
        Assert.Equal("category-1", dto.Id);
        Assert.Equal("Hair Care", dto.Name);
    }

    [Fact]
    public async Task GetSuppliersAsync_RepositoryReturnsSuppliers_MapsEveryField()
    {
        var repository = new StubInventoryRepository();
        repository.Suppliers.Add(new DomainInventory.Supplier("supplier-1", "Glow Beauty Supply Co.", "Maria Gonzalez", "orders@example.com", "+1 555 0100", DomainInventory.SupplierStatus.Active));
        var sut = new ProductQueryService(repository);

        var result = await sut.GetSuppliersAsync();

        var dto = Assert.Single(result);
        Assert.Equal("supplier-1", dto.Id);
        Assert.Equal(SupplierStatus.Active, dto.Status);
    }
}
