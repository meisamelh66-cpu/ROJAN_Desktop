using System.IO;
using System.Text.Json;
using Rojan.Desktop.Domain.Automation;

namespace Rojan.Desktop.Infrastructure.Automation;

/// <summary>Default <see cref="IBusinessRuleRepository"/>. Persists to <c>%LocalAppData%\RojanDesktop\automation\rules.json</c>.</summary>
public sealed class LocalBusinessRuleRepository : IBusinessRuleRepository
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    private readonly string _filePath;

    public LocalBusinessRuleRepository()
        : this(DefaultFilePath())
    {
    }

    internal LocalBusinessRuleRepository(string filePath)
    {
        _filePath = filePath;
    }

    public Task<IReadOnlyList<BusinessRule>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<BusinessRule>>(ReadPersisted());

    public Task<BusinessRule?> GetByIdAsync(string id, CancellationToken cancellationToken = default) =>
        Task.FromResult(ReadPersisted().FirstOrDefault(rule => rule.Id == id));

    public Task SaveAsync(BusinessRule rule, CancellationToken cancellationToken = default)
    {
        var rules = ReadPersisted();
        var index = rules.FindIndex(existing => existing.Id == rule.Id);
        if (index >= 0)
        {
            rules[index] = rule;
        }
        else
        {
            rules.Add(rule);
        }

        Persist(rules);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var rules = ReadPersisted();
        if (rules.RemoveAll(rule => rule.Id == id) > 0)
        {
            Persist(rules);
        }

        return Task.CompletedTask;
    }

    private List<BusinessRule> ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<BusinessRule>>(json, SerializerOptions) ?? [];
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

    private void Persist(List<BusinessRule> rules)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_filePath, JsonSerializer.Serialize(rules, SerializerOptions));
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "automation", "rules.json");
}
