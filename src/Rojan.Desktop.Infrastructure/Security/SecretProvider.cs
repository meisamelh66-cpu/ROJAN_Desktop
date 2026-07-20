using Rojan.Desktop.Application.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="ISecretProvider"/>. Checks the environment variable
/// <c>ROJAN_SECRET_{NAME}</c> first (uppercased, non-alphanumeric
/// characters replaced with <c>_</c> - lets an operator/CI override a
/// secret without touching encrypted storage), then falls back to
/// <see cref="ISecureStorageService"/>. Never returns a hardcoded value -
/// an unset secret resolves to <c>null</c>, which is the honest answer
/// until something actually provisions it.
/// </summary>
public sealed class SecretProvider : ISecretProvider
{
    private readonly ISecureStorageService _secureStorage;

    public SecretProvider(ISecureStorageService secureStorage)
    {
        _secureStorage = secureStorage;
    }

    public async Task<string?> GetSecretAsync(string name, CancellationToken cancellationToken = default)
    {
        var environmentValue = Environment.GetEnvironmentVariable(EnvironmentVariableName(name));
        if (!string.IsNullOrEmpty(environmentValue))
        {
            return environmentValue;
        }

        return await _secureStorage.GetAsync($"secret:{name}", cancellationToken).ConfigureAwait(false);
    }

    private static string EnvironmentVariableName(string name)
    {
        var sanitized = new char[name.Length];
        for (var i = 0; i < name.Length; i++)
        {
            sanitized[i] = char.IsLetterOrDigit(name[i]) ? char.ToUpperInvariant(name[i]) : '_';
        }

        return $"ROJAN_SECRET_{new string(sanitized)}";
    }
}
