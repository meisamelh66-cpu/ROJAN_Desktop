namespace Rojan.Desktop.Domain.Inventory;

/// <summary>
/// A single catalog product, as returned by <see cref="IInventoryRepository"/>.
/// <see cref="CategoryId"/>/<see cref="SupplierId"/> are real references
/// within this same vertical slice (<see cref="ProductCategory"/> and
/// <see cref="Supplier"/> both live in <c>Domain.Inventory</c> too) - the
/// denormalized <see cref="CategoryName"/>/<see cref="SupplierName"/>
/// avoid a join for the common case of just displaying a product, same
/// reasoning as <c>Bookings.Booking</c> carrying both an id and a name for
/// each of its (cross-slice, free-form) references.
/// </summary>
public sealed record Product(
    string Id,
    string Sku,
    string Name,
    string CategoryId,
    string CategoryName,
    string SupplierId,
    string SupplierName,
    string UnitPrice,
    ProductStatus Status,
    string Description);
