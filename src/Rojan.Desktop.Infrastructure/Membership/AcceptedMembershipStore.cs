using System.Text.Json;
using Rojan.Desktop.Application.Membership;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Membership;

namespace Rojan.Desktop.Infrastructure.Membership;

/// <summary>
/// Real <see cref="IAcceptedMembershipStore"/> - a thin JSON wrapper around
/// the existing <see cref="ISecureStorageService"/> (Windows DPAPI,
/// current-user-scoped), same "serialize the record, write it through
/// SecureStorage under one fixed key" pattern <c>BackendSessionService</c>
/// already uses for the auth token pair. No new encryption/file-handling
/// logic - reuses the one storage mechanism this app already has for
/// exactly this class of "small secret that must survive a restart"
/// problem.
/// </summary>
public sealed class AcceptedMembershipStore(ISecureStorageService secureStorage) : IAcceptedMembershipStore
{
    private const string StorageKey = "membership:accepted";
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<AcceptedMembership?> GetAsync(CancellationToken cancellationToken = default)
    {
        var json = await secureStorage.GetAsync(StorageKey, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AcceptedMembership>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            // Corrupt/unrecognized persisted data is treated as "not present," matching
            // DpapiSecureStorageService's own "undecryptable is not fatal" convention.
            return null;
        }
    }

    public Task SaveAsync(AcceptedMembership membership, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(membership, SerializerOptions);
        return secureStorage.SetAsync(StorageKey, json, cancellationToken);
    }

    public Task ClearAsync(CancellationToken cancellationToken = default) =>
        secureStorage.RemoveAsync(StorageKey, cancellationToken);
}
