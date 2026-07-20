namespace Rojan.Desktop.Domain.Organizations;

/// <summary>Lifecycle state of an <see cref="Organization"/> tenant.</summary>
public enum OrganizationStatus
{
    Trial,
    Active,
    Suspended,
    Cancelled,
}
