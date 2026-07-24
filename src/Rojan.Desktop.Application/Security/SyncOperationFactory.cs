using System.Text.Json;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Application.Security;

/// <summary>
/// Sprint 7 Commit 3: Sync Producer Foundation. Builds the
/// <see cref="PendingSyncOperation"/> a command-service producer (e.g.
/// <c>Customers.CustomerCommandServiceSyncProducer</c>) enqueues onto
/// <see cref="ISyncQueueService"/> after a write succeeds - centralizes
/// the payload serialization convention (camelCase JSON, matching
/// <c>Infrastructure.Api.HttpApiClient</c>'s own serializer options) so
/// every producer stamps an operation the same way instead of each one
/// hand-rolling its own <see cref="JsonSerializerOptions"/>.
/// </summary>
public static class SyncOperationFactory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static PendingSyncOperation Create<TPayload>(string entityType, string entityId, string operationType, TPayload payload) =>
        new(
            Guid.NewGuid().ToString("N"),
            entityType,
            entityId,
            operationType,
            JsonSerializer.Serialize(payload, SerializerOptions),
            DateTimeOffset.UtcNow);
}
