namespace Rojan.Desktop.Domain.HR;

/// <summary>Extended HR profile data for one <see cref="Employee"/> - kept as its own 1:1 aggregate rather than fields on Employee itself, same "core record plus separate extended-detail aggregate" split as <c>Domain.Customers.Customer</c>/<c>CustomerNote</c>.</summary>
public sealed record EmployeeProfile(
    string Id,
    string EmployeeId,
    string Bio,
    string Skills,
    string EmergencyContactName,
    string EmergencyContactPhone);
