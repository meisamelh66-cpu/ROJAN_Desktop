namespace Rojan.Desktop.Domain.Identity;

/// <summary>
/// Phase 25: Enterprise Identity Foundation. The immutable identity of the
/// person operating this installation. This app has no multi-account
/// login model yet (Phase 22A's session is scoped to
/// organization/branch/role, not a signed-in person), so
/// <see cref="LocalUser"/> is the honest bridge: it names the local OS
/// account as the current user rather than fabricating a fake signed-in
/// identity, while still giving the authentication service something
/// real to issue a <see cref="SessionIdentity"/> against. Future
/// phases wiring a real backend/IdP replace <see cref="LocalUser"/> with a
/// server-issued <see cref="UserIdentity"/> - the shape does not need to
/// change, only where <see cref="Id"/> comes from.
/// </summary>
public sealed record UserIdentity(string Id, string DisplayName, string? Email)
{
    /// <summary>Bridges the current Windows account into a <see cref="UserIdentity"/> until real accounts exist - see this type's own doc comment.</summary>
    public static UserIdentity LocalUser(string machineUserName) =>
        new($"local:{machineUserName}", machineUserName, Email: null);
}
