namespace Rojan.Desktop.Domain.HR;

/// <summary>Employee lifecycle transition guards - Application enforces these before writing, same validation-enforcement pattern as <c>Domain.Bookings.BookingRules</c>/<c>Domain.Inventory.StockTransactionRules</c>.</summary>
public static class EmployeeStatusRules
{
    public static bool CanActivate(EmployeeStatus current) => current != EmployeeStatus.Active;

    public static bool CanDeactivate(EmployeeStatus current) => current != EmployeeStatus.Inactive;

    public static bool CanSuspend(EmployeeStatus current) => current is EmployeeStatus.Active or EmployeeStatus.OnLeave;
}
