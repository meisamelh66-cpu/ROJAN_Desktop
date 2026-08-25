namespace Rojan.Desktop.Domain.Services;

/// <summary>
/// Validation rules for the service catalog lifecycle - the same
/// deliberate deviation from this codebase's usual "Domain is just data +
/// repository contract" minimalism that <c>Bookings.BookingRules</c>/
/// <c>Customers.CustomerRules</c> already established. Laid down now, as
/// Sprint 5's foundation commit, ahead of any Application-layer command
/// that actually changes a service's status - today's <c>Application.Services.IServiceCommandService</c>
/// only assigns/unassigns specialists, so nothing calls this type yet;
/// when a status-changing command is added, it enforces these rules from
/// day one instead of writing an unvalidated status straight through, the
/// same ordering <c>CustomerRules</c> established for
/// <c>CustomerCommandService.UpdateCustomerAsync</c>.
///
/// Unlike <see cref="Bookings.BookingRules"/>, no status here is fully
/// terminal - same reasoning as <see cref="Customers.CustomerRules"/>: a
/// catalog service is an ongoing offering, not a one-shot event like a
/// booking, so a <see cref="ServiceStatus.Discontinued"/> service can
/// always be reintroduced (<see cref="ServiceStatus.Discontinued"/> -&gt;
/// <see cref="ServiceStatus.Active"/>) rather than being locked out of the
/// catalog forever.
/// </summary>
public static class ServiceRules
{
    private static readonly Dictionary<ServiceStatus, ServiceStatus[]> ValidTransitions = new()
    {
        [ServiceStatus.Active] = [ServiceStatus.Seasonal, ServiceStatus.Discontinued],
        [ServiceStatus.Seasonal] = [ServiceStatus.Active, ServiceStatus.Discontinued],
        [ServiceStatus.Discontinued] = [ServiceStatus.Active],
    };

    /// <summary>Whether moving a service directly from <paramref name="from"/> to <paramref name="to"/> is legal. Callers must only invoke this for an actual status change (<paramref name="from"/> != <paramref name="to"/>) - editing a service's other fields (name/price/duration/description/category) without changing status is not a transition at all and must never be rejected here.</summary>
    public static bool IsValidTransition(ServiceStatus from, ServiceStatus to) =>
        ValidTransitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    /// <summary>
    /// Whether a service currently in <paramref name="from"/> can be
    /// activated (moved to <see cref="ServiceStatus.Active"/>) - both
    /// non-Active statuses (Seasonal, Discontinued) allow it per
    /// <see cref="IsValidTransition"/>; an already-Active service is
    /// explicitly excluded, since "activating" something already active is
    /// not a real activation and callers should not present it as one
    /// (e.g. an enabled "Activate" button/command).
    /// </summary>
    public static bool CanActivate(ServiceStatus from) =>
        from != ServiceStatus.Active && IsValidTransition(from, ServiceStatus.Active);

    /// <summary>
    /// Whether a service currently in <paramref name="from"/> can be
    /// deactivated. Only a currently-<see cref="ServiceStatus.Active"/>
    /// service can be "deactivated" at all - a Seasonal or Discontinued
    /// service is already inactive, so moving Seasonal -&gt; Discontinued
    /// (a legal <see cref="IsValidTransition"/> pair) is not itself a
    /// deactivation and must not be reported as one here.
    /// </summary>
    public static bool CanDeactivate(ServiceStatus from) =>
        from == ServiceStatus.Active;

    // Service Catalog Authoring: field-shape validation, deliberately mirroring ROJAN_Backend's own
    // Jakarta constraints on CreateServiceRequest/UpdateServiceRequest (@NotBlank @Size(max=255) name,
    // @Positive durationMinutes, @NotNull @DecimalMin(0.0, exclusive) price) - the same "structural,
    // non-authoritative client-side mirror" reasoning already established for status transitions above.
    // ROJAN_Backend remains the final, authoritative validator regardless of what these report.

    private const int MaxNameLength = 255;

    public static bool IsValidName(string name) =>
        !string.IsNullOrWhiteSpace(name) && name.Length <= MaxNameLength;

    public static bool IsValidDuration(int durationMinutes) =>
        durationMinutes > 0;

    public static bool IsValidPrice(decimal price) =>
        price > 0m;
}
