namespace Rojan.Desktop.Application.Security;

/// <summary>Phase 25: Security Foundation. Symmetric authenticated encryption over raw bytes - callers supply the key (from <see cref="IKeyProvider"/>) rather than this service managing key lifecycle itself, keeping the two concerns independently testable/replaceable.</summary>
public interface IEncryptionService
{
    /// <summary>Encrypts <paramref name="plaintext"/> with <paramref name="key"/>. The returned bytes are self-contained (nonce/tag included) - <see cref="Decrypt"/> needs nothing else.</summary>
    public byte[] Encrypt(byte[] plaintext, byte[] key);

    public byte[] Decrypt(byte[] ciphertext, byte[] key);
}
