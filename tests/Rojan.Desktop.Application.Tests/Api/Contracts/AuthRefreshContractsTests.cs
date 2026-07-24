using System.Text.Json;
using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Tests.Api.Contracts;

/// <summary>Exercises <see cref="AuthRefreshRequest"/>/<see cref="AuthRefreshResponse"/> - the wire shapes a future backend-backed <c>ISessionService.RefreshAsync</c> would send/receive. Same camelCase serialization convention as every other contract in this namespace.</summary>
public sealed class AuthRefreshContractsTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void AuthRefreshRequest_RoundTripsThroughJson()
    {
        var request = new AuthRefreshRequest("refresh-token-value");

        var json = JsonSerializer.Serialize(request, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<AuthRefreshRequest>(json, SerializerOptions);

        Assert.Equal(request, roundTripped);
        Assert.Contains("refresh-token-value", json);
    }

    [Fact]
    public void AuthRefreshResponse_RoundTripsThroughJsonWithAllFields()
    {
        var issuedAt = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
        var response = new AuthRefreshResponse(
            "new-access-token",
            issuedAt,
            issuedAt.AddHours(1),
            "new-refresh-token",
            issuedAt,
            issuedAt.AddDays(30));

        var json = JsonSerializer.Serialize(response, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<AuthRefreshResponse>(json, SerializerOptions);

        Assert.Equal(response, roundTripped);
    }

    [Fact]
    public void AuthRefreshResponse_AccessAndRefreshTokenLifetimesAreIndependent()
    {
        var issuedAt = new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero);
        var response = new AuthRefreshResponse(
            "access", issuedAt, issuedAt.AddHours(1),
            "refresh", issuedAt, issuedAt.AddDays(30));

        Assert.True(response.RefreshTokenExpiresAt > response.AccessTokenExpiresAt);
    }
}
