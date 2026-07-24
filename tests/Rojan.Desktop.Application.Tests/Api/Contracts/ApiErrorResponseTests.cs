using System.Text.Json;
using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Tests.Api.Contracts;

/// <summary>Exercises <see cref="ApiErrorResponse"/> - the wire shape a future backend's error body would take. Serialized with camelCase defaults, matching <c>Infrastructure.Api.HttpApiClient</c>'s own <c>SerializerOptions</c> convention for every other request/response body this app sends/reads.</summary>
public sealed class ApiErrorResponseTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Constructor_SetsAllFieldsAndDefaultsDetailsToNull()
    {
        var error = new ApiErrorResponse("validation_failed", "The request was invalid.");

        Assert.Equal("validation_failed", error.Code);
        Assert.Equal("The request was invalid.", error.Message);
        Assert.Null(error.Details);
    }

    [Fact]
    public void RoundTripsThroughJsonWithAllFieldsPresent()
    {
        var error = new ApiErrorResponse("conflict", "Entity has already been modified.", "customer-1");

        var json = JsonSerializer.Serialize(error, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<ApiErrorResponse>(json, SerializerOptions);

        Assert.Equal(error, roundTripped);
    }

    [Fact]
    public void Deserialize_BodyMissingOptionalDetailsField_LeavesDetailsNull()
    {
        // A future backend that omits an empty "details" field entirely must still deserialize cleanly -
        // this is the "backward compatibility" this contract needs: a body with only Code/Message.
        const string json = """{"code":"not_found","message":"Customer not found."}""";

        var error = JsonSerializer.Deserialize<ApiErrorResponse>(json, SerializerOptions);

        Assert.NotNull(error);
        Assert.Equal("not_found", error!.Code);
        Assert.Equal("Customer not found.", error.Message);
        Assert.Null(error.Details);
    }
}
