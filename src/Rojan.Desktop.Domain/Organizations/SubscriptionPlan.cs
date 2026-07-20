namespace Rojan.Desktop.Domain.Organizations;

/// <summary>The commercial plan an <see cref="Organization"/> is subscribed to - drives no billing logic yet (out of this phase's scope), just a displayed/administered attribute.</summary>
public enum SubscriptionPlan
{
    Trial,
    Starter,
    Professional,
    Enterprise,
}
