namespace Rojan.Desktop.Application.Inventory;

/// <summary>Input to <see cref="IInventoryCommandService.CreateSupplierAsync"/> - new suppliers always start as <c>Active</c>, so Status isn't a caller-supplied field.</summary>
public sealed record CreateSupplierRequest(string Name, string ContactName, string Email, string Phone);
