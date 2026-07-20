using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.AI;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Every mutating/export use case requires <see cref="Permission.AiUse"/>; the read-only browsing methods (<see cref="GetSessionsAsync"/>/<see cref="GetMessagesAsync"/>/<see cref="SearchSessionsAsync"/>) stay open to anyone who can already reach the AI Center module.</summary>
public sealed class ConversationManagerPermissionGate : IConversationManager
{
    private readonly IConversationManager _inner;
    private readonly IPermissionGate _permissionGate;

    public ConversationManagerPermissionGate(IConversationManager inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<IReadOnlyList<ConversationSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default) =>
        _inner.GetSessionsAsync(cancellationToken);

    public Task<ConversationSessionDto> CreateSessionAsync(string initialTitle, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.CreateSessionAsync(initialTitle, cancellationToken);
    }

    public Task<IReadOnlyList<ConversationMessageDto>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _inner.GetMessagesAsync(sessionId, cancellationToken);

    public Task<ConversationMessageDto> AppendMessageAsync(string sessionId, ConversationRole role, string content, int tokenCount, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.AppendMessageAsync(sessionId, role, content, tokenCount, cancellationToken);
    }

    public Task<ConversationSessionDto> TogglePinAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.TogglePinAsync(sessionId, cancellationToken);
    }

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.DeleteSessionAsync(sessionId, cancellationToken);
    }

    public Task<IReadOnlyList<ConversationSessionDto>> SearchSessionsAsync(string searchText, CancellationToken cancellationToken = default) =>
        _inner.SearchSessionsAsync(searchText, cancellationToken);

    public Task<string> ExportSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.ExportSessionAsync(sessionId, cancellationToken);
    }

    public Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.ClearHistoryAsync(cancellationToken);
    }
}
