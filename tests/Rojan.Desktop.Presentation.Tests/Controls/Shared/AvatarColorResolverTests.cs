using Rojan.Desktop.Presentation.Controls.Shared;

namespace Rojan.Desktop.Presentation.Tests.Controls.Shared;

public sealed class AvatarColorResolverTests
{
    [Fact]
    public void ResolveIndex_SameNameTwice_ReturnsSameIndex()
    {
        var first = AvatarColorResolver.ResolveIndex("Sara Ahmadi");
        var second = AvatarColorResolver.ResolveIndex("Sara Ahmadi");

        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveIndex_NullOrWhitespaceName_ReturnsZero(string? name)
    {
        Assert.Equal(0, AvatarColorResolver.ResolveIndex(name));
    }

    [Theory]
    [InlineData("Sara Ahmadi")]
    [InlineData("Priya Nair")]
    [InlineData("A")]
    [InlineData("A Very Long Customer Display Name With Many Words")]
    public void ResolveIndex_AnyName_ReturnsIndexWithinPaletteRange(string name)
    {
        var index = AvatarColorResolver.ResolveIndex(name);

        Assert.InRange(index, 0, 3);
    }

    [Fact]
    public void ResolveIndex_DifferentNames_CanReturnDifferentIndexes()
    {
        var indexA = AvatarColorResolver.ResolveIndex("Sara Ahmadi");
        var indexB = AvatarColorResolver.ResolveIndex("Priya Nair");

        // Not a strict guarantee for every possible pair (a 4-bucket hash
        // can collide), but these two specific names are chosen because
        // they are known to land in different buckets - proves the hash is
        // actually name-dependent, not a constant in disguise.
        Assert.NotEqual(indexA, indexB);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ResolveInitials_NullOrWhitespaceName_ReturnsQuestionMark(string? name)
    {
        Assert.Equal("?", AvatarColorResolver.ResolveInitials(name));
    }

    [Fact]
    public void ResolveInitials_SingleWordName_ReturnsFirstLetterUppercase()
    {
        Assert.Equal("S", AvatarColorResolver.ResolveInitials("sara"));
    }

    [Fact]
    public void ResolveInitials_TwoWordName_ReturnsFirstAndLastInitialsUppercase()
    {
        Assert.Equal("SA", AvatarColorResolver.ResolveInitials("sara ahmadi"));
    }

    [Fact]
    public void ResolveInitials_ThreeWordName_ReturnsFirstAndLastWordInitialsOnly()
    {
        Assert.Equal("SR", AvatarColorResolver.ResolveInitials("Sara Middle Rezaei"));
    }

    [Fact]
    public void ResolveInitials_NameWithExtraWhitespace_TrimsAndCollapsesBeforeSplitting()
    {
        Assert.Equal("SA", AvatarColorResolver.ResolveInitials("  sara    ahmadi  "));
    }

    [Fact]
    public void ResolveInitials_PersianName_HandlesNonLatinCharactersSafely()
    {
        var initials = AvatarColorResolver.ResolveInitials("سارا احمدی");

        Assert.Equal("سا", initials);
    }
}
