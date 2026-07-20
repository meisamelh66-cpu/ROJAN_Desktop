using Rojan.Desktop.Application.Organizations;

namespace Rojan.Desktop.Application.AI;

/// <summary>Phase 22A: Enterprise Context Migration - same "wrap the real service with permission enforcement" pattern as <c>Customers.CustomerCommandServicePermissionGate</c>. Requires <see cref="Permission.AiUse"/>.</summary>
public sealed class AIServicePermissionGate : IAIService
{
    private readonly IAIService _inner;
    private readonly IPermissionGate _permissionGate;

    public AIServicePermissionGate(IAIService inner, IPermissionGate permissionGate)
    {
        _inner = inner;
        _permissionGate = permissionGate;
    }

    public Task<SendMessageResultDto> SendMessageAsync(string sessionId, string userMessage, LanguageContextDto languageContext, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.SendMessageAsync(sessionId, userMessage, languageContext, cancellationToken);
    }

    public IAsyncEnumerable<string> StreamMessageAsync(string sessionId, string userMessage, LanguageContextDto languageContext, CancellationToken cancellationToken = default)
    {
        _permissionGate.Ensure(Permission.AiUse);
        return _inner.StreamMessageAsync(sessionId, userMessage, languageContext, cancellationToken);
    }
}
