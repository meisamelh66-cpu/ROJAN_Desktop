using System.Text.Json;
using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Tests.Api.Contracts;

/// <summary>Exercises <see cref="ApiErrorResponse"/> - ROJAN_Backend's real error body shape. Serialized with camelCase defaults, matching <c>Infrastructure.Api.HttpApiClient</c>'s own <c>SerializerOptions</c> convention for every other request/response body this app sends/reads.</summary>
public sealed class ApiErrorResponseTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Constructor_SetsRequiredFieldsAndDefaultsOptionalOnesToNull()
    {
        var error = new ApiErrorResponse("VALIDATION_FAILED", "The request was invalid.");

        Assert.Equal("VALIDATION_FAILED", error.ErrorCode);
        Assert.Equal("The request was invalid.", error.Message);
        Assert.Null(error.Status);
        Assert.Null(error.Error);
        Assert.Null(error.Path);
        Assert.Null(error.TraceId);
    }

    [Fact]
    public void RoundTripsThroughJsonWithAllFieldsPresent()
    {
        var error = new ApiErrorResponse("SALON_NOT_FOUND", "Salon not found: 3fa85f64", 404, "Not Found", "/api/v1/dashboard/insights", "trace-1");

        var json = JsonSerializer.Serialize(error, SerializerOptions);
        var roundTripped = JsonSerializer.Deserialize<ApiErrorResponse>(json, SerializerOptions);

        Assert.Equal(error, roundTripped);
    }

    [Fact]
    public void Deserialize_ARealBackendErrorBody_MapsEveryFieldCorrectly()
    {
        // The exact shape ROJAN_Backend's GlobalExceptionHandler returns for a 409.
        const string json = """
            {"timestamp":"2026-08-04T16:00:00Z","status":409,"error":"Conflict","errorCode":"SALON_CONTEXT_REQUIRED","message":"Owner has multiple salons","path":"/api/v1/dashboard/insights","traceId":"a1b2c3"}
            """;

        var error = JsonSerializer.Deserialize<ApiErrorResponse>(json, SerializerOptions);

        Assert.NotNull(error);
        Assert.Equal("SALON_CONTEXT_REQUIRED", error!.ErrorCode);
        Assert.Equal("Owner has multiple salons", error.Message);
        Assert.Equal(409, error.Status);
        Assert.Equal("Conflict", error.Error);
        Assert.Equal("/api/v1/dashboard/insights", error.Path);
        Assert.Equal("a1b2c3", error.TraceId);
    }

    [Fact]
    public void Deserialize_TheFixedAuthUnauthorizedBody_LeavesOptionalFieldsNull()
    {
        // The one shape the backend's security layer returns directly (bypassing GlobalExceptionHandler) - see ApiErrorResponse's own doc comment.
        const string json = """{"errorCode":"AUTH_UNAUTHORIZED","message":"Authentication required"}""";

        var error = JsonSerializer.Deserialize<ApiErrorResponse>(json, SerializerOptions);

        Assert.NotNull(error);
        Assert.Equal("AUTH_UNAUTHORIZED", error!.ErrorCode);
        Assert.Equal("Authentication required", error.Message);
        Assert.Null(error.Status);
        Assert.Null(error.Path);
        Assert.Null(error.TraceId);
    }
}
