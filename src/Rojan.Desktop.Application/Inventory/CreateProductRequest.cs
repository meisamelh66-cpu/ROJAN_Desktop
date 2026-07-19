namespace Rojan.Desktop.Application.Inventory;

/// <summary>
/// Input to <see cref="IInventoryCommandService.CreateProductAsync"/> -
/// new products always start as <c>Active</c>, so Status isn't a
/// caller-supplied field. Category/Supplier id and name travel together
/// (Presentation resolves both from the selected dropdown item), same
/// reasoning as <c>BookingWorkflow.CreateBookingWorkflowRequest</c>
/// carrying both a customer id and name - avoids an extra repository
/// round-trip in the command service to resolve a name from an id.
/// Creating a product also creates its initial <see cref="InventoryItemDto"/>
/// stock record in the same command, so <see cref="InitialQuantity"/>/
/// <see cref="ReorderThreshold"/> are captured here too.
/// </summary>
public sealed record CreateProductRequest(
    string Sku,
    string Name,
    string CategoryId,
    string CategoryName,
    string SupplierId,
    string SupplierName,
    string UnitPrice,
    string Description,
    int InitialQuantity,
    int ReorderThreshold);
