using System.Security.Cryptography;
using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="IEncryptionService"/>. AES-256-GCM (authenticated
/// encryption - a tampered ciphertext fails to decrypt rather than
/// silently returning garbage). Output layout is
/// <c>[12-byte nonce][16-byte tag][ciphertext]</c>, a fresh random nonce
/// per call, so <see cref="Decrypt"/> needs nothing beyond the bytes
/// <see cref="Encrypt"/> returned and the same key.
/// </summary>
public sealed class AesEncryptionService : IEncryptionService
{
    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;

    public byte[] Encrypt(byte[] plaintext, byte[] key)
    {
        var nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagSizeBytes];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Encrypt(nonce, plaintext, ciphertext, tag);

        var result = new byte[NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSizeBytes);
        Buffer.BlockCopy(tag, 0, result, NonceSizeBytes, TagSizeBytes);
        Buffer.BlockCopy(ciphertext, 0, result, NonceSizeBytes + TagSizeBytes, ciphertext.Length);
        return result;
    }

    public byte[] Decrypt(byte[] ciphertext, byte[] key)
    {
        if (ciphertext.Length < NonceSizeBytes + TagSizeBytes)
        {
            throw new CryptographicException("Ciphertext is shorter than the minimum nonce+tag length.");
        }

        var nonce = ciphertext[..NonceSizeBytes];
        var tag = ciphertext[NonceSizeBytes..(NonceSizeBytes + TagSizeBytes)];
        var cipherOnly = ciphertext[(NonceSizeBytes + TagSizeBytes)..];
        var plaintext = new byte[cipherOnly.Length];

        using var aesGcm = new AesGcm(key, TagSizeBytes);
        aesGcm.Decrypt(nonce, cipherOnly, tag, plaintext);
        return plaintext;
    }
}
