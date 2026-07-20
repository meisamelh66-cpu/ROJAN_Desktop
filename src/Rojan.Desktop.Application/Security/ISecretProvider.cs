namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Phase 25: Security Foundation. Read-only, layered secret resolution
/// (environment variable override first, then <see cref="ISecureStorageService"/>)
/// - the single place any future code asks "what is the value of secret
/// X" without knowing or caring where it actually lives, so no secret
/// value is ever hardcoded at a call site.
/// </summary>
public interface ISecretProvider
{
    public Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default);
}
