using Rojan.Desktop.Domain.Search;

namespace Rojan.Desktop.Domain.Tests.Search;

/// <summary>Exercises <see cref="SearchRules.Match"/>'s exact/prefix/substring/fuzzy tiers and their highlight spans.</summary>
public sealed class SearchRulesTests
{
    [Fact]
    public void Match_EmptyQuery_ReturnsNoMatch()
    {
        var result = SearchRules.Match("   ", "Customers");

        Assert.False(result.IsMatch);
        Assert.Equal(0, result.Score);
    }

    [Fact]
    public void Match_EmptyText_ReturnsNoMatch()
    {
        var result = SearchRules.Match("cust", "");

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Match_ExactMatch_HighlightsTheWholeText()
    {
        var result = SearchRules.Match("Customers", "Customers");

        Assert.True(result.IsMatch);
        var span = Assert.Single(result.Spans);
        Assert.Equal(0, span.Start);
        Assert.Equal("Customers".Length, span.Length);
    }

    [Fact]
    public void Match_ExactMatch_IsCaseInsensitive()
    {
        var result = SearchRules.Match("customers", "Customers");

        Assert.True(result.IsMatch);
    }

    [Fact]
    public void Match_PrefixMatch_ScoresHigherThanMidStringSubstringMatch()
    {
        var prefixResult = SearchRules.Match("cust", "Customers");
        var substringResult = SearchRules.Match("tomer", "Customers");

        Assert.True(prefixResult.IsMatch);
        Assert.True(substringResult.IsMatch);
        Assert.True(prefixResult.Score > substringResult.Score);
    }

    [Fact]
    public void Match_SubstringMatch_HighlightsExactSpan()
    {
        var result = SearchRules.Match("tomer", "Customers");

        var span = Assert.Single(result.Spans);
        Assert.Equal("Cus".Length, span.Start);
        Assert.Equal("tomer".Length, span.Length);
    }

    [Fact]
    public void Match_ExactMatch_ScoresHigherThanPrefixMatch()
    {
        var exactResult = SearchRules.Match("Customers", "Customers");
        var prefixResult = SearchRules.Match("Custom", "Customers");

        Assert.True(exactResult.Score > prefixResult.Score);
    }

    [Fact]
    public void Match_NoSubstringButValidSubsequence_FallsBackToFuzzyMatch()
    {
        var result = SearchRules.Match("cst", "Customers");

        Assert.True(result.IsMatch);
        Assert.True(result.Score > 0);
    }

    [Fact]
    public void Match_FuzzyMatch_ScoresLowerThanAnySubstringMatch()
    {
        var fuzzyResult = SearchRules.Match("cst", "Customers");
        var substringResult = SearchRules.Match("stom", "Customers");

        Assert.True(fuzzyResult.Score < substringResult.Score);
    }

    [Fact]
    public void Match_FuzzySubsequenceOutOfOrder_DoesNotMatch()
    {
        var result = SearchRules.Match("tsc", "Customers");

        Assert.False(result.IsMatch);
    }

    [Fact]
    public void Match_FuzzyQueryWithAPartialContiguousRun_ScoresHigherThanFullyScatteredQuery()
    {
        // Neither query is a literal substring of "abcdefghij" (both skip
        // letters), so both fall to the fuzzy tier - "abg" has a genuine
        // contiguous a-b run (positions 0-1) plus a scattered "g"; "adg" is
        // fully scattered (every letter 3 apart). The contiguous run must
        // score higher.
        const string text = "abcdefghij";

        var partiallyContiguous = SearchRules.Match("abg", text);
        var fullyScattered = SearchRules.Match("adg", text);

        Assert.True(partiallyContiguous.IsMatch);
        Assert.True(fullyScattered.IsMatch);
        Assert.True(partiallyContiguous.Score > fullyScattered.Score);
    }
}
