namespace Rojan.Desktop.Infrastructure.Persistence;

/// <summary>
/// SQLite connection configuration for <see cref="RojanDbContext"/> - the
/// database file lives at
/// <c>%LocalAppData%\RojanDesktop\database\rojan.db</c> by default, same
/// "one file under RojanDesktop's LocalAppData folder" convention every
/// other persisted service in this app already uses
/// (<c>Notifications.LocalNotificationRepository</c>,
/// <c>Security.LocalSessionService</c>, etc.). The parent directory is
/// created eagerly in the constructor - SQLite will create the database
/// file itself on first connection, but not a missing directory, so
/// without this a fresh install's first connection attempt would fail
/// with "unable to open database file" purely because the folder doesn't
/// exist yet.
/// </summary>
public sealed class SqlitePersistenceOptions
{
    public SqlitePersistenceOptions(string databasePath)
    {
        DatabasePath = databasePath;

        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    /// <summary>Absolute path to the SQLite database file.</summary>
    public string DatabasePath { get; }

    /// <summary>The <c>UseSqlite</c>-ready connection string for <see cref="DatabasePath"/>.</summary>
    public string ConnectionString => $"Data Source={DatabasePath}";

    /// <summary>The real app's connection - one shared instance, same reasoning <c>RojanDbContextFactory</c> reuses it for design-time tooling so a generated migration targets the real database shape.</summary>
    public static SqlitePersistenceOptions Default { get; } = new(DefaultDatabasePath());

    private static string DefaultDatabasePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "database", "rojan.db");
}
