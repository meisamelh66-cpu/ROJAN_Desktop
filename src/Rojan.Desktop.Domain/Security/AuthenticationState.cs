namespace Rojan.Desktop.Domain.Security;

/// <summary>Phase 25: Secure Authentication Foundation. Lifecycle stage of the current <see cref="Identity.SessionIdentity"/>, as computed by <see cref="SessionRules.DetermineState"/> - never set directly by callers, the same "derived from data, not a caller-set flag" shape <c>Domain.Accounting.InvoiceStatus</c> already uses.</summary>
public enum AuthenticationState
{
    SignedOut,
    Authenticating,
    Authenticated,
    Expired,
    Locked,
    Failed,
}
