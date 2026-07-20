using System.Security.Cryptography;
using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="IKeyProvider"/>. Generates a 256-bit key via
/// <see cref="RandomNumberGenerator"/> the first time a given purpose is
/// requested, then persists it (base64-encoded) through
/// <see cref="ISecureStorageService"/> - so the key material itself is
/// DPAPI-protected at rest, not just the values it later encrypts. This
/// is the seam Phase 25.7's "future hardware-backed key support" targets:
/// a TPM-backed provider would implement <see cref="IKeyProvider"/> the
/// same way, generating/retrieving key material from hardware instead of
/// <see cref="RandomNumberGenerator"/> + secure storage, with no change
/// needed to <see cref="IEncryptionService"/> or any caller.
/// </summary>
public sealed class LocalKeyProvider : IKeyProvider
{
    private const int KeySizeBytes = 32;

    private readonly ISecureStorageService _secureStorage;

    public LocalKeyProvider(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<byte[]> GetOrCreateKeyAsync(string purpose, CancellationToken cancellationToken = default)
    {
        var storageKey = $"key-provider:{purpose}";
        var existing = await _secureStorage.GetAsync(storageKey, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return Convert.FromBase64String(existing);
        }

        var key = RandomNumberGenerator.GetBytes(KeySizeBytes);
        await _secureStorage.SetAsync(storageKey, Convert.ToBase64String(key), cancellationToken).ConfigureAwait(false);
        return key;
    }
}
