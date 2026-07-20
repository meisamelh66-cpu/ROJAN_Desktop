namespace Rojan.Desktop.Domain.AI;

/// <summary>
/// One chat thread the Business Assistant holds with the user - the
/// Conversation System's "Sessions" requirement. Messages live separately
/// (<see cref="ConversationMessage"/>, keyed by <see cref="Id"/>) rather
/// than nested here, same "aggregate root plus separate detail records"
/// split every other module uses (e.g. <c>Customers.Customer</c>/
/// <c>CustomerNote</c>).
/// </summary>
public sealed record ConversationSession(
    string Id,
    string Title,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool IsPinned);
