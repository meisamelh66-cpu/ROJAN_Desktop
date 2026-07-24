using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Domain.Tests.Security;

/// <summary>Exercises the <see cref="SyncConflict"/> data shape - field assignment and its resolution-status lifecycle (see Sprint 7 Commit 4's own doc comment on the record).</summary>
public sealed class SyncConflictTests
{
    private static SyncConflict MakeConflict() =>
        new("conflict-1", "operation-1", "Customer", "customer-1", """{"name":"local"}""", """{"name":"remote"}""",
            "HTTP 409 Conflict", DateTimeOffset.UtcNow);

    [Fact]
    public void Constructor_SetsAllFieldsAndDefaultsResolutionStatusToUnresolved()
    {
        var conflict = MakeConflict();

        Assert.Equal("conflict-1", conflict.Id);
        Assert.Equal("operation-1", conflict.OperationId);
        Assert.Equal("Customer", conflict.EntityType);
        Assert.Equal("customer-1", conflict.EntityId);
        Assert.Equal("""{"name":"local"}""", conflict.LocalPayload);
        Assert.Equal("""{"name":"remote"}""", conflict.RemotePayload);
        Assert.Equal("HTTP 409 Conflict", conflict.Reason);
        Assert.Equal(SyncConflictResolutionStatus.Unresolved, conflict.ResolutionStatus);
    }

    [Fact]
    public void With_ChangingResolutionStatus_ProducesANewRecordWithOnlyThatFieldChanged()
    {
        var conflict = MakeConflict();

        var resolved = conflict with { ResolutionStatus = SyncConflictResolutionStatus.Resolved };

        Assert.Equal(SyncConflictResolutionStatus.Resolved, resolved.ResolutionStatus);
        Assert.Equal(SyncConflictResolutionStatus.Unresolved, conflict.ResolutionStatus);
        Assert.Equal(conflict.Id, resolved.Id);
        Assert.Equal(conflict.OperationId, resolved.OperationId);
        Assert.Equal(conflict.EntityType, resolved.EntityType);
        Assert.Equal(conflict.EntityId, resolved.EntityId);
        Assert.Equal(conflict.LocalPayload, resolved.LocalPayload);
        Assert.Equal(conflict.RemotePayload, resolved.RemotePayload);
        Assert.Equal(conflict.Reason, resolved.Reason);
        Assert.Equal(conflict.CreatedAt, resolved.CreatedAt);
        Assert.NotEqual(conflict, resolved);
    }
}
