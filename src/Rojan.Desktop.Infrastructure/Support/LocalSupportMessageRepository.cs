using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Support;

namespace Rojan.Desktop.Infrastructure.Support;

/// <summary>Default <see cref="ISupportMessageRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\support\messages.json</c>, capped at <see cref="MaxEntries"/> like every other bounded-history store in this app - there is no real outbound delivery yet (no email server, no ticketing system), so this is the durable record of every submission.</summary>
public sealed class LocalSupportMessageRepository : ISupportMessageRepository
{
    public const int MaxEntries = 500;

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalSupportMessageRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalSupportMessageRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<SupportMessage>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<SupportMessage>>(ReadPersisted());

    public Task SaveAsync(SupportMessage message, CancellationToken cancellationToken = default)
    {
        var messages = ReadPersisted();
        messages.Insert(0, message);
        if (messages.Count > MaxEntries)
        {
            messages.RemoveRange(MaxEntries, messages.Count - MaxEntries);
        }

        Persist(messages);
        return Task.CompletedTask;
    }

    private List<SupportMessage> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<SupportMessage>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<SupportMessage> messages)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(messages, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "support", "messages.json");
}
