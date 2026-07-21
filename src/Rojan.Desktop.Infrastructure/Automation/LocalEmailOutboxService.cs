using System.IO;
using System.Text.Json;
using Rojan.Desktop.Application.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>One persisted outbox entry - <see cref="EmailMessage"/> plus when it was "sent".</summary>
internal sealed record OutboxEntryRecord(string ToAddress, string Subject, string Body, DateTimeOffset SentAt);

/// <summary>
/// Default <see cref="IEmailNotificationService"/>. Requirement 32.6's
/// email contract has no real SMTP integration in this phase (a
/// documented future integration, not built here) - every
/// <see cref="SendAsync"/> call is persisted to a local outbox file
/// (<c>%LocalAppData%\RojanDesktop\automation\email-outbox.json</c>,
/// capped like every other bounded-history store in this app) instead of
/// actually being delivered, so the Email workflow step is fully
/// exercisable/testable end to end without a live mail server.
/// </summary>
public sealed class LocalEmailOutboxService : IEmailNotificationService
{
    public const int MaxEntries = 200;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalEmailOutboxService()
        : this(DefaultFilePath())
    {
    }

    internal LocalEmailOutboxService(string filePath)
    {
        _filePath = filePath;
    }

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var entries = ReadPersisted();
        entries.Insert(0, new OutboxEntryRecord(message.ToAddress, message.Subject, message.Body, DateTimeOffset.UtcNow));
        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }

        Persist(entries);
        return Task.CompletedTask;
    }

    private List<OutboxEntryRecord> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<OutboxEntryRecord>>(json, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
        catch (IOException)
        {
            return [];
        }
    }

    private void Persist(List<OutboxEntryRecord> entries)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(entries, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "email-outbox.json");
}
