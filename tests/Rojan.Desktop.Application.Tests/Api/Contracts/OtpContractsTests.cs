using System.Text.Json;
using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Tests.Api.Contracts;

/// <summary>Exercises <see cref="OtpRequestRequest"/>/<see cref="OtpVerifyRequest"/>/<see cref="OtpIssuedResponse"/> and the now-nullable <see cref="AuthUserResponse"/> fields - the wire shapes ROJAN_Backend's Mobile-First Authentication endpoints send/receive. Same camelCase serialization convention as every other contract in this namespace.</summary>
public sealed class OtpContractsTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void OtpRequestRequest_RoundTripsThroughJson()
    {
        var request = new OtpRequestRequest("+989123456789");

        var json = JsonSerializer.Serialize(request, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<OtpRequestRequest>(json, SerializerOptions);

        Assert.Equal(request, roundTripped);
        Assert.Contains("phoneNumber", json);
    }

    [Fact]
    public void OtpVerifyRequest_RoundTripsThroughJson_WithFullName()
    {
        var request = new OtpVerifyRequest("+989123456789", "482913", "Salon Owner");

        var json = JsonSerializer.Serialize(request, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<OtpVerifyRequest>(json, SerializerOptions);

        Assert.Equal(request, roundTripped);
    }

    [Fact]
    public void OtpVerifyRequest_FullNameDefaultsToNull_ForAReturningUser()
    {
        var request = new OtpVerifyRequest("+989123456789", "482913");

        Assert.Null(request.FullName);
    }

    [Fact]
    public void OtpIssuedResponse_RoundTripsThroughJson()
    {
        var response = new OtpIssuedResponse("+989123456789", 120, 60);

        var json = JsonSerializer.Serialize(response, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<OtpIssuedResponse>(json, SerializerOptions);

        Assert.Equal(response, roundTripped);
    }

    [Fact]
    public void AuthUserResponse_DeserializesAPhoneOnlyAccount_WithNoEmail()
    {
        const string json = """{"id":"owner-1","email":null,"phoneNumber":"+989123456789","fullName":"Salon Owner","role":"MANAGER"}""";

        var user = JsonSerializer.Deserialize<AuthUserResponse>(json, SerializerOptions);

        Assert.NotNull(user);
        Assert.Null(user!.Email);
        Assert.Equal("+989123456789", user.PhoneNumber);
    }

    [Fact]
    public void AuthUserResponse_DeserializesAnEmailOnlyAccount_WithNoPhoneNumber()
    {
        const string json = """{"id":"owner-1","email":"owner@example.com","phoneNumber":null,"fullName":"Salon Owner","role":"MANAGER"}""";

        var user = JsonSerializer.Deserialize<AuthUserResponse>(json, SerializerOptions);

        Assert.NotNull(user);
        Assert.Equal("owner@example.com", user!.Email);
        Assert.Null(user.PhoneNumber);
    }
}
