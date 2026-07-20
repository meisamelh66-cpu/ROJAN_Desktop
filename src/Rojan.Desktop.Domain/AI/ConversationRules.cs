namespace Rojan.Desktop.Domain.AI;

/// <summary>Genuine Domain rules for the Conversation System - kept out of <c>Application.AI.ConversationManager</c> the same "rules live in Domain" reasoning every other module follows.</summary>
public static class ConversationRules
{
    /// <summary>Caps how many sessions can be pinned at once, so "Pinned Conversations" stays a short, genuinely useful shortlist rather than a second copy of the whole history.</summary>
    public const int MaxPinnedSessions = 10;

    public static bool CanPin(int currentPinnedCount) => currentPinnedCount < MaxPinnedSessions;

    /// <summary>Derives a session's display title from its first user message when none was given explicitly - truncated so it reads as a title, not a paragraph.</summary>
    public static string DeriveTitle(string firstUserMessage)
    {
        const int maxLength = 60;
        var trimmed = firstUserMessage.Trim();
        return trimmed.Length <= maxLength ? trimmed : string.Concat(trimmed.AsSpan(0, maxLength), "...");
    }
}
