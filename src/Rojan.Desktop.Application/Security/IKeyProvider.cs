namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Security Foundation. Generates and persists symmetric keys
/// used by <see cref="IEncryptionService"/>, one per named purpose (e.g.
/// "sync-payload", "secure-storage") so a key compromise in one area does
/// not expose every encrypted value in
/// the app. The concrete implementation protects the persisted key
/// itself via <see cref="ISecureStorageService"/> - see this interface's
/// method doc for the "future hardware-backed key support" abstraction
/// point Phase 25.7 asks for.
/// </summary>
public interface IKeyProvider
{
    /// <summary>Returns the existing key for <paramref name="purpose"/>, generating and persisting a new random key on first use. The concrete implementation is the seam a future TPM/hardware-backed key store would replace - callers never see raw key material generation, only this abstraction.</summary>
    public Task<byte[]> GetOrCreateKeyAsync(string purpose, CancellationToken cancellationToken = default);
}
