using Rojan.Server.Infrastructure.Security;

namespace Rojan.Server.Infrastructure.Tests.Security;

/// <summary>Exercises <see cref="Pbkdf2PasswordHasher"/> - the "Security: password never stored plain text" requirement this commit's own task list calls out explicitly.</summary>
public sealed class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_NeverEqualsThePlainTextPassword()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple1");

        Assert.NotEqual("CorrectHorseBatteryStaple1", hash);
        Assert.DoesNotContain("CorrectHorseBatteryStaple1", hash, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashesBecauseOfRandomSalt()
    {
        var first = _sut.Hash("CorrectHorseBatteryStaple1");
        var second = _sut.Hash("CorrectHorseBatteryStaple1");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsTrue()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple1");

        Assert.True(_sut.Verify("CorrectHorseBatteryStaple1", hash));
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("CorrectHorseBatteryStaple1");

        Assert.False(_sut.Verify("SomeOtherPassword1", hash));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-valid-hash")]
    [InlineData("abc.def")]
    [InlineData("notanumber.c2FsdA==.c3ViOTk5eQ==")]
    public void Verify_MalformedStoredHash_ReturnsFalseRatherThanThrowing(string malformedHash)
    {
        Assert.False(_sut.Verify("AnyPassword1", malformedHash));
    }
}
