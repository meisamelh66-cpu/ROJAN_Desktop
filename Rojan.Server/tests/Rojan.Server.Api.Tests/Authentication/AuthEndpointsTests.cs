using System.Net;
using System.Net.Http.Json;
using Rojan.Server.Application.Authentication;
using Rojan.Server.Domain.Authentication;

namespace Rojan.Server.Api.Tests.Authentication;

/// <summary>Exercises <c>AuthController</c>'s three endpoints end-to-end (real DI, real controller model binding/validation, EF Core InMemory persistence - see <see cref="AuthApiFactory"/>'s own doc comment) - the "API: endpoint responses" requirement this commit's own task list calls out.</summary>
public sealed class AuthEndpointsTests : IClassFixture<AuthApiFactory>
{
    private readonly AuthApiFactory _factory;

    public AuthEndpointsTests(AuthApiFactory factory)
    {
        _factory = factory;
    }

    private static RegisterOrganizationOwnerRequest NewRegisterRequest(string emailSuffix) =>
        new("Rojan Salon", $"owner-{emailSuffix}@rojan.example", "SuperSecret1", "Noah Bennett");

    [Fact]
    public async Task Register_NewOrganization_Returns200WithTenantContextAndTokens()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", NewRegisterRequest(Guid.NewGuid().ToString("N")));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEmpty(result.OrganizationId);
        Assert.Null(result.BranchId);
        Assert.NotEmpty(result.UserId);
        Assert.Equal([UserRoles.Owner], result.Roles);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409Conflict()
    {
        var client = _factory.CreateClient();
        var request = NewRegisterRequest(Guid.NewGuid().ToString("N"));
        await client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_MissingRequiredField_Returns400BadRequest()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/register", new { organizationName = "", email = "not-an-email", password = "short", fullName = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithTokens()
    {
        var client = _factory.CreateClient();
        var registerRequest = NewRegisterRequest(Guid.NewGuid().ToString("N"));
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(registerRequest.Email, registerRequest.Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
        Assert.NotNull(result);
        Assert.NotEmpty(result!.AccessToken);
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();
        var registerRequest = NewRegisterRequest(Guid.NewGuid().ToString("N"));
        await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(registerRequest.Email, "WrongPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401UnauthorizedNotDifferentFromWrongPassword()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest($"nobody-{Guid.NewGuid():N}@rojan.example", "WhateverPassword1"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns200WithNewTokens()
    {
        var client = _factory.CreateClient();
        var registerRequest = NewRegisterRequest(Guid.NewGuid().ToString("N"));
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResult>();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(registered!.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var refreshed = await response.Content.ReadFromJsonAsync<AuthenticationResult>();
        Assert.NotNull(refreshed);
        Assert.Equal(registered.UserId, refreshed!.UserId);
        Assert.NotEqual(registered.RefreshToken, refreshed.RefreshToken);
    }

    [Fact]
    public async Task Refresh_TokenAlreadyUsedOnce_Returns401OnSecondAttempt()
    {
        var client = _factory.CreateClient();
        var registerRequest = NewRegisterRequest(Guid.NewGuid().ToString("N"));
        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", registerRequest);
        var registered = await registerResponse.Content.ReadFromJsonAsync<AuthenticationResult>();
        await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(registered!.RefreshToken));

        var secondAttempt = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest(registered.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns401Unauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/auth/register")]
    [InlineData("/api/v1/auth/login")]
    [InlineData("/api/v1/auth/refresh")]
    public async Task Endpoints_NoAuthorizationHeaderPresent_AreStillReachable(string path)
    {
        // "No authorization required on these endpoints" - a malformed/empty
        // body reaching the controller (400) rather than being rejected by
        // auth middleware (401/403) proves no bearer token was required to
        // reach the action at all.
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(path, new { });

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
