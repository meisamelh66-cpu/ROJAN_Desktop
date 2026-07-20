using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Default <see cref="ISessionService"/>. Persists the current
/// <see cref="SessionIdentity"/> plus its <see cref="AuthToken"/>/
/// <see cref="RefreshToken"/> pair to
/// <c>%LocalAppData%\RojanDesktop\security\auth-session.json</c> so a
/// session survives an app restart. Token values are real random bytes
/// (<see cref="RandomNumberGenerator"/>, base64-encoded) - opaque local
/// bearer tokens until a real backend issues its own (see
/// <see cref="AuthToken"/>'s own doc comment). <see cref="SessionIdentity.ExpiresAt"/>
/// tracks the refresh token's expiry (the session's true outer bound);
/// the access token's shorter lifetime is tracked separately via
/// <see cref="CurrentAccessToken"/> so a caller (e.g. the API client) can
/// tell "session still valid" apart from "access token needs refreshing."
/// </summary>
public sealed class LocalSessionService : ISessionService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly TimeSpan AccessTokenLifetime = TimeSpan.FromHours(1);
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);

    private readonly string _filePath;
    private RefreshToken? _currentRefreshToken;

    public LocalSessionService()
        : this(DefaultFilePath())
    {
    }

    internal LocalSessionService(string filePath)
    {
        _filePath = filePath;
    }

    public SessionIdentity? CurrentSession { get; private set; }

    public AuthToken? CurrentAccessToken { get; private set; }

    public AuthenticationState CurrentState { get; private set; } = AuthenticationState.SignedOut;

    public event EventHandler<AuthenticationState>? StateChanged;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var persisted = ReadPersisted();
        var now = DateTimeOffset.UtcNow;

        if (persisted is not null && !persisted.RefreshToken.IsExpired(now))
        {
            CurrentSession = persisted.Session;
            CurrentAccessToken = persisted.AccessToken;
            _currentRefreshToken = persisted.RefreshToken;
        }
        else if (persisted is not null)
        {
            // Persisted but past its refresh window - clean up rather
            // than leaving a stale file behind.
            DeletePersisted();
        }

        SetState(SessionRules.DetermineState(CurrentSession, now));
        return Task.CompletedTask;
    }

    public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new SessionIdentity(Guid.NewGuid().ToString("N"), user.Id, device.Id, now, now + RefreshTokenLifetime);
        var accessToken = new AuthToken(GenerateTokenValue(), now, now + AccessTokenLifetime);
        var refreshToken = new RefreshToken(GenerateTokenValue(), now, now + RefreshTokenLifetime);

        CurrentSession = session;
        CurrentAccessToken = accessToken;
        _currentRefreshToken = refreshToken;
        Persist(session, accessToken, refreshToken);
        SetState(SessionRules.DetermineState(session, now));

        return Task.FromResult(session);
    }

    public Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentSession is null || _currentRefreshToken is null)
        {
            throw new InvalidOperationException("There is no current session to refresh.");
        }

        var now = DateTimeOffset.UtcNow;
        if (_currentRefreshToken.IsExpired(now))
        {
            SetState(AuthenticationState.Expired);
            throw new InvalidOperationException("The refresh token has expired - sign in again.");
        }

        var renewedSession = CurrentSession with { ExpiresAt = now + RefreshTokenLifetime };
        var accessToken = new AuthToken(GenerateTokenValue(), now, now + AccessTokenLifetime);
        var refreshToken = new RefreshToken(GenerateTokenValue(), now, now + RefreshTokenLifetime);

        CurrentSession = renewedSession;
        CurrentAccessToken = accessToken;
        _currentRefreshToken = refreshToken;
        Persist(renewedSession, accessToken, refreshToken);
        SetState(SessionRules.DetermineState(renewedSession, now));

        return Task.FromResult(renewedSession);
    }

    public Task ExpireAsync(CancellationToken cancellationToken = default)
    {
        CurrentSession = null;
        CurrentAccessToken = null;
        _currentRefreshToken = null;
        DeletePersisted();
        SetState(AuthenticationState.SignedOut);
        return Task.CompletedTask;
    }

    private void SetState(AuthenticationState state)
    {
        if (CurrentState == state)
        {
            return;
        }

        CurrentState = state;
        StateChanged?.Invoke(this, state);
    }

    private static string GenerateTokenValue() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private PersistedSession? ReadPersisted()
    {
        if (!File.Exists(_filePath))
        {
            return null;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<PersistedSession>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
    }

    private void Persist(SessionIdentity session, AuthToken accessToken, RefreshToken refreshToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (directory is not null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(new PersistedSession(session, accessToken, refreshToken), SerializerOptions);
        File.WriteAllText(_filePath, json);
    }

    private void DeletePersisted()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    private static string DefaultFilePath() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RojanDesktop", "security", "auth-session.json");

    private sealed record PersistedSession(SessionIdentity Session, AuthToken AccessToken, RefreshToken RefreshToken);
}
