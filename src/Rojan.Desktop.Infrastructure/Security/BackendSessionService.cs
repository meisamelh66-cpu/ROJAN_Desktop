using System.Text.Json;
using Rojan.Desktop.Application.Api.Contracts;
using Rojan.Desktop.Application.Security;
using Rojan.Desktop.Domain.Identity;
using Rojan.Desktop.Domain.Security;
using Rojan.Desktop.Infrastructure.Api;

namespace Rojan.Desktop.Infrastructure.Security;

/// <summary>
/// Owner App Backend Integration: the real, backend-connected
/// <see cref="ISessionService"/> - replaces <see cref="LocalSessionService"/>
/// in <c>ServiceCollectionExtensions.AddInfrastructure</c> (which stays in
/// the codebase, unreferenced, matching this repo's own Sprint 6 convention
/// for a superseded-but-still-useful implementation). Two differences from
/// <see cref="LocalSessionService"/>:
/// <list type="bullet">
/// <item>Tokens come from <see cref="AuthBootstrapHttpClient"/> (real
/// backend calls), never locally-generated random bytes - see
/// <see cref="CreateSessionFromTokensAsync"/>/<see cref="RefreshAsync"/>.
/// <see cref="CreateSessionAsync"/> (the local-token-generating method)
/// throws here: a "backend" session service that silently fell back to
/// fake local tokens would be actively misleading.</item>
/// <item>Persistence is <see cref="ISecureStorageService"/> (Windows
/// DPAPI, per-user-account encrypted) instead of
/// <see cref="LocalSessionService"/>'s plaintext <c>File.WriteAllText</c> -
/// this closes the plaintext-token-storage finding from the Owner App
/// Integration Audit. The DPAPI-encrypted blob is the exact same
/// <see cref="PersistedSession"/> JSON shape <see cref="LocalSessionService"/>
/// already used, just written through <see cref="ISecureStorageService.SetAsync"/>
/// instead of a raw file write.</item>
/// </list>
/// </summary>
public sealed class BackendSessionService(AuthBootstrapHttpClient authClient, ISecureStorageService secureStorage) : ISessionService
{
    private const string StorageKey = "auth:session";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private RefreshToken? _currentRefreshToken;

    public SessionIdentity? CurrentSession { get; private set; }

    public AuthToken? CurrentAccessToken { get; private set; }

    public AuthenticationState CurrentState { get; private set; } = AuthenticationState.SignedOut;

    public event EventHandler<AuthenticationState>? StateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var persisted = await ReadPersistedAsync(cancellationToken).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        if (persisted is not null && !persisted.RefreshToken.IsExpired(now))
        {
            CurrentSession = persisted.Session;
            CurrentAccessToken = persisted.AccessToken;
            _currentRefreshToken = persisted.RefreshToken;
        }
        else if (persisted is not null)
        {
            await secureStorage.RemoveAsync(StorageKey, cancellationToken).ConfigureAwait(false);
        }

        SetState(SessionRules.DetermineState(CurrentSession, now));
    }

    /// <summary>A backend-connected session service has no local token-generation fallback - see this class's own doc comment. Use <see cref="CreateSessionFromTokensAsync"/> with a real backend-issued token pair instead.</summary>
    public Task<SessionIdentity> CreateSessionAsync(UserIdentity user, DeviceIdentity device, CancellationToken cancellationToken = default) =>
        throw new NotSupportedException($"{nameof(BackendSessionService)} does not generate local tokens - sign in via the backend and use {nameof(CreateSessionFromTokensAsync)}.");

    public async Task<SessionIdentity> CreateSessionFromTokensAsync(
        UserIdentity user,
        DeviceIdentity device,
        AuthToken accessToken,
        RefreshToken refreshToken,
        CancellationToken cancellationToken = default)
    {
        var session = new SessionIdentity(Guid.NewGuid().ToString("N"), user.Id, device.Id, DateTimeOffset.UtcNow, refreshToken.ExpiresAt);

        CurrentSession = session;
        CurrentAccessToken = accessToken;
        _currentRefreshToken = refreshToken;
        await PersistAsync(session, accessToken, refreshToken, cancellationToken).ConfigureAwait(false);
        SetState(SessionRules.DetermineState(session, DateTimeOffset.UtcNow));

        return session;
    }

    /// <summary>
    /// Calls the real backend's <c>POST /api/v1/auth/refresh</c> via
    /// <see cref="AuthBootstrapHttpClient"/> - deliberately not
    /// <c>IApiClient</c>, see <see cref="AuthBootstrapHttpClient"/>'s own
    /// doc comment for why a refresh call must never go through the
    /// generic pipeline that itself calls this exact method on a 401.
    /// </summary>
    public async Task<SessionIdentity> RefreshAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentSession is null || _currentRefreshToken is null)
        {
            throw new InvalidOperationException("There is no current session to refresh.");
        }

        var response = await authClient
            .PostAsync<AuthRefreshRequest, AuthResponse>("/api/v1/auth/refresh", new AuthRefreshRequest(_currentRefreshToken.Value), cancellationToken)
            .ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;
        var accessToken = new AuthToken(response.AccessToken, now, response.AccessTokenExpiresAt);
        var refreshToken = new RefreshToken(response.RefreshToken, now, response.RefreshTokenExpiresAt);
        var renewedSession = CurrentSession with { ExpiresAt = refreshToken.ExpiresAt };

        CurrentSession = renewedSession;
        CurrentAccessToken = accessToken;
        _currentRefreshToken = refreshToken;
        await PersistAsync(renewedSession, accessToken, refreshToken, cancellationToken).ConfigureAwait(false);
        SetState(SessionRules.DetermineState(renewedSession, now));

        return renewedSession;
    }

    public async Task ExpireAsync(CancellationToken cancellationToken = default)
    {
        CurrentSession = null;
        CurrentAccessToken = null;
        _currentRefreshToken = null;
        await secureStorage.RemoveAsync(StorageKey, cancellationToken).ConfigureAwait(false);
        SetState(AuthenticationState.SignedOut);
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

    private async Task<PersistedSession?> ReadPersistedAsync(CancellationToken cancellationToken)
    {
        var json = await secureStorage.GetAsync(StorageKey, cancellationToken).ConfigureAwait(false);
        if (json is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<PersistedSession>(json, SerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private Task PersistAsync(SessionIdentity session, AuthToken accessToken, RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(new PersistedSession(session, accessToken, refreshToken), SerializerOptions);
        return secureStorage.SetAsync(StorageKey, json, cancellationToken);
    }

    private sealed record PersistedSession(SessionIdentity Session, AuthToken AccessToken, RefreshToken RefreshToken);
}
