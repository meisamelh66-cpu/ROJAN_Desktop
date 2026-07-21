using System.Text.Json;
using Rojan.Desktop.Application.Automation;
using Rojan.Desktop.Infrastructure.Automation;

namespace Rojan.Desktop.Infrastructure.Tests.Automation;

/// <summary>Exercises <see cref="LocalEmailOutboxService"/> against a temp file - Requirement 32.6's "no live SMTP yet, persist to an outbox" contract, newest-first insertion, and the <see cref="LocalEmailOutboxService.MaxEntries"/> eviction cap.</summary>
public sealed class LocalEmailOutboxServiceTests : IDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly string _filePath;

    public LocalEmailOutboxServiceTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), "RojanDesktopTests", Guid.NewGuid().ToString("N"), "email-outbox.json");
    }

    public void Dispose()
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private List<OutboxEntryRecord> ReadRawOutbox() =>
        JsonSerializer.Deserialize<List<OutboxEntryRecord>>(File.ReadAllText(_filePath), SerializerOptions) ?? [];

    [Fact]
    public async Task SendAsync_NoPersistedFileYet_CreatesTheOutboxFile()
    {
        var service = new LocalEmailOutboxService(_filePath);

        await service.SendAsync(new EmailMessage("user@example.com", "Subject", "Body"));

        Assert.True(File.Exists(_filePath));
    }

    [Fact]
    public async Task SendAsync_MultipleMessages_InsertsAtTheFront()
    {
        var service = new LocalEmailOutboxService(_filePath);

        await service.SendAsync(new EmailMessage("first@example.com", "First", "Body"));
        await service.SendAsync(new EmailMessage("second@example.com", "Second", "Body"));

        var entries = ReadRawOutbox();
        Assert.Equal(["second@example.com", "first@example.com"], entries.Select(e => e.ToAddress));
    }

    [Fact]
    public async Task SendAsync_BeyondMaxEntries_EvictsTheOldest()
    {
        var service = new LocalEmailOutboxService(_filePath);
        for (var i = 0; i < LocalEmailOutboxService.MaxEntries + 2; i++)
        {
            await service.SendAsync(new EmailMessage($"user{i}@example.com", "Subject", "Body"));
        }

        var entries = ReadRawOutbox();

        Assert.Equal(LocalEmailOutboxService.MaxEntries, entries.Count);
        Assert.DoesNotContain(entries, e => e.ToAddress == "user0@example.com");
    }
}
