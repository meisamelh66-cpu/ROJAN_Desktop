using Rojan.Desktop.Application.Api.Contracts;

namespace Rojan.Desktop.Application.Tests.Api.Contracts;

public sealed class ApiVersionTests
{
    [Fact]
    public void V1_IsLowercaseVTokenNotAFullPath()
    {
        Assert.Equal("v1", ApiVersion.V1);
    }

    [Fact]
    public void BasePath_NoArgument_DefaultsToV1()
    {
        Assert.Equal("/api/v1", ApiVersion.BasePath());
    }

    [Fact]
    public void BasePath_ExplicitVersion_UsesThatVersionInstead()
    {
        Assert.Equal("/api/v2", ApiVersion.BasePath("v2"));
    }
}
