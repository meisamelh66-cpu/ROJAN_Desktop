using System.IO;
using System.Security.Cryptography;
using System.Text;
using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="ISecureStorageService"/>. Each value is encrypted
/// with Windows DPAPI (<see cref="ProtectedData"/>, <see cref="DataProtectionScope.CurrentUser"/>
/// - only the same Windows account on the same machine can decrypt it,
/// with no key of our own to manage) and written to its own file under
/// <c>%LocalAppData%\RojanDesktop\security\storage\</c>, named by a SHA-256
/// hash of the logical key (never the raw key itself, which may contain
/// characters invalid in a file name, e.g. "auth:refresh-token").
/// </summary>
public sealed class DpapiSecureStorageService : ISecureStorageService
{
    private readonly string _storageDirectory;

    public DpapiSecureStorageService()
        : this(DefaultStorageDirectory())
    {
    }

    internal DpapiSecureStorageService(string storageDirectory)
    {
        _storageDirectory = storageDirectory;
    }

    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(_storageDirectory))
        {
            Directory.CreateDirectory(_storageDirectory);
        }

        var plaintext = Encoding.UTF8.GetBytes(value);
        var protectedBytes = ProtectedData.Protect(plaintext, optionalEntropy: null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePathFor(key), protectedBytes);
        return Task.CompletedTask;
    }

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = FilePathFor(key);
        if (!File.Exists(path))
        {
            return Task.FromResult<string?>(null);
        }

        try
        {
            var protectedBytes = File.ReadAllBytes(path);
            var plaintext = ProtectedData.Unprotect(protectedBytes, optionalEntropy: null, DataProtectionScope.CurrentUser);
            return Task.FromResult<string?>(Encoding.UTF8.GetString(plaintext));
        }
        catch (CryptographicException)
        {
            // Undecryptable (e.g. the file was copied to a different
            // machine/account) is treated as "not present," not a fatal
            // error - callers already handle a null result as "not set."
            return Task.FromResult<string?>(null);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        var path = FilePathFor(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string FilePathFor(string key)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        return Path.Combine(_storageDirectory, $"{hash}.dat");
    }

    private static string DefaultStorageDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "security", "storage");
}
