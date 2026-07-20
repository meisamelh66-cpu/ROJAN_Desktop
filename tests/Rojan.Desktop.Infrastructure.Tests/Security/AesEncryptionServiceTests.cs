using System.Security.Cryptography;
using System.Text;
using Rojan.Desktop.Infrastructure.Security;

namespace Rojan.Desktop.Infrastructure.Tests.Security;

public sealed class AesEncryptionServiceTests
{
    [Fact]
    public void Encrypt_ThenDecrypt_RoundTripsThePlaintext()
    {
        var service = new AesEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("The quick brown fox jumps over the lazy dog.");

        var ciphertext = service.Encrypt(plaintext, key);
        var decrypted = service.Decrypt(ciphertext, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesDifferentCiphertextEachCall_BecauseTheNonceIsRandom()
    {
        var service = new AesEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var plaintext = Encoding.UTF8.GetBytes("same plaintext");

        var first = service.Encrypt(plaintext, key);
        var second = service.Encrypt(plaintext, key);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Decrypt_WrongKey_ThrowsCryptographicException()
    {
        var service = new AesEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var wrongKey = RandomNumberGenerator.GetBytes(32);
        var ciphertext = service.Encrypt(Encoding.UTF8.GetBytes("secret"), key);

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(ciphertext, wrongKey));
    }

    [Fact]
    public void Decrypt_TamperedCiphertext_ThrowsCryptographicException()
    {
        var service = new AesEncryptionService();
        var key = RandomNumberGenerator.GetBytes(32);
        var ciphertext = service.Encrypt(Encoding.UTF8.GetBytes("secret"), key);
        ciphertext[^1] ^= 0xFF;

        Assert.ThrowsAny<CryptographicException>(() => service.Decrypt(ciphertext, key));
    }
}
