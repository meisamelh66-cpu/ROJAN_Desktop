using System.Text;
using DomainAI = Rojan.Desktop.Domain.AI;

namespace Rojan.Desktop.Application.AI;

public sealed class ConversationManager : IConversationManager
{
    private readonly DomainAI.IAIRepository _repository;

    public ConversationManager(DomainAI.IAIRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<ConversationSessionDto>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _repository.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
        return sessions.Select(AIMapper.MapSession).ToList();
    }

    public async Task<ConversationSessionDto> CreateSessionAsync(string initialTitle, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.Now;
        var title = DomainAI.ConversationRules.DeriveTitle(initialTitle);
        var session = new DomainAI.ConversationSession($"session-{Guid.NewGuid():N}", title, now, now, false);
        var created = await _repository.CreateSessionAsync(session, cancellationToken).ConfigureAwait(false);
        return AIMapper.MapSession(created);
    }

    public async Task<IReadOnlyList<ConversationMessageDto>> GetMessagesAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var messages = await _repository.GetMessagesAsync(sessionId, cancellationToken).ConfigureAwait(false);
        return messages.Select(AIMapper.MapMessage).ToList();
    }

    public async Task<ConversationMessageDto> AppendMessageAsync(string sessionId, ConversationRole role, string content, int tokenCount, CancellationToken cancellationToken = default)
    {
        var message = new DomainAI.ConversationMessage($"message-{Guid.NewGuid():N}", sessionId, AIMapper.MapRole(role), content, DateTimeOffset.Now, tokenCount);
        var added = await _repository.AddMessageAsync(message, cancellationToken).ConfigureAwait(false);

        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is not null)
        {
            await _repository.UpdateSessionAsync(session with { UpdatedAt = DateTimeOffset.Now }, cancellationToken).ConfigureAwait(false);
        }

        return AIMapper.MapMessage(added);
    }

    public async Task<ConversationSessionDto> TogglePinAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");

        if (!session.IsPinned)
        {
            var sessions = await _repository.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
            var pinnedCount = sessions.Count(s => s.IsPinned);
            if (!DomainAI.ConversationRules.CanPin(pinnedCount))
            {
                throw new InvalidOperationException($"Cannot pin more than {DomainAI.ConversationRules.MaxPinnedSessions} conversations.");
            }
        }

        var updated = await _repository.UpdateSessionAsync(session with { IsPinned = !session.IsPinned }, cancellationToken).ConfigureAwait(false);
        return AIMapper.MapSession(updated);
    }

    public Task DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default) =>
        _repository.DeleteSessionAsync(sessionId, cancellationToken);

    public async Task<IReadOnlyList<ConversationSessionDto>> SearchSessionsAsync(string searchText, CancellationToken cancellationToken = default)
    {
        var sessions = await _repository.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return sessions.Select(AIMapper.MapSession).ToList();
        }

        var matches = new List<DomainAI.ConversationSession>();
        foreach (var session in sessions)
        {
            if (session.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(session);
                continue;
            }

            var messages = await _repository.GetMessagesAsync(session.Id, cancellationToken).ConfigureAwait(false);
            if (messages.Any(m => m.Content.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
            {
                matches.Add(session);
            }
        }

        return matches.Select(AIMapper.MapSession).ToList();
    }

    public async Task<string> ExportSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetSessionByIdAsync(sessionId, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Session '{sessionId}' was not found.");
        var messages = await _repository.GetMessagesAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var builder = new StringBuilder();
        builder.Append("Conversation: ").AppendLine(session.Title);
        builder.Append("Exported: ").AppendLine(DateTimeOffset.Now.ToString("u", System.Globalization.CultureInfo.InvariantCulture));
        builder.AppendLine();
        foreach (var message in messages)
        {
            builder.Append('[').Append(message.CreatedAt.ToString("u", System.Globalization.CultureInfo.InvariantCulture)).Append("] ")
                .Append(message.Role).Append(": ").AppendLine(message.Content);
        }

        return builder.ToString();
    }

    public async Task ClearHistoryAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _repository.GetSessionsAsync(cancellationToken).ConfigureAwait(false);
        foreach (var session in sessions.Where(s => !s.IsPinned))
        {
            await _repository.DeleteSessionAsync(session.Id, cancellationToken).ConfigureAwait(false);
        }
    }
}
