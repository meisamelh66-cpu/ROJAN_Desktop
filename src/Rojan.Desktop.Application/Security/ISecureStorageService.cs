namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Security Foundation. Key/value storage for secrets that must
/// not sit in a plaintext settings file (tokens, keys) - distinct from
/// the plaintext JSON settings files Localization/Theming/Session already
/// use for non-sensitive preferences. The concrete implementation
/// encrypts at rest (Windows DPAPI, scoped to the current user) rather
/// than merely obscuring - see <c>Infrastructure.Security.DpapiSecureStorageService</c>.
/// </summary>
public interface ISecureStorageService
{
    public Task SetAsync(string key, string value, CancellationToken cancellationToken = default);

    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
