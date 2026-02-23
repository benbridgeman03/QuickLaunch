using QuickLaunch.Core.Services;

namespace QuickLaunch.Tests;

public class LevenshteinDistanceTests
{
    [Fact]
    public void IdenticalStrings_ReturnsZero()
    {
        Assert.Equal(0, SearchService.LevenshteinDistance("hello", "hello"));
    }

    [Fact]
    public void EmptySource_ReturnsTargetLength()
    {
        Assert.Equal(5, SearchService.LevenshteinDistance("", "hello"));
    }

    [Fact]
    public void EmptyTarget_ReturnsSourceLength()
    {
        Assert.Equal(5, SearchService.LevenshteinDistance("hello", ""));
    }

    [Fact]
    public void BothEmpty_ReturnsZero()
    {
        Assert.Equal(0, SearchService.LevenshteinDistance("", ""));
    }

    [Fact]
    public void SingleInsertion_ReturnsOne()
    {
        Assert.Equal(1, SearchService.LevenshteinDistance("cat", "cats"));
    }

    [Fact]
    public void SingleDeletion_ReturnsOne()
    {
        Assert.Equal(1, SearchService.LevenshteinDistance("cats", "cat"));
    }

    [Fact]
    public void SingleSubstitution_ReturnsOne()
    {
        Assert.Equal(1, SearchService.LevenshteinDistance("cat", "car"));
    }

    [Fact]
    public void CompletelyDifferent_ReturnsMaxLength()
    {
        Assert.Equal(3, SearchService.LevenshteinDistance("abc", "xyz"));
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("notepad", "noetpad", 2)]
    public void KnownDistances_ReturnsExpected(string source, string target, int expected)
    {
        Assert.Equal(expected, SearchService.LevenshteinDistance(source, target));
    }
}

public class FuzzyScoreTests
{
    [Fact]
    public void ExactMatch_Returns100()
    {
        Assert.Equal(100, SearchService.GetFuzzyScore("chrome", "chrome"));
    }

    [Fact]
    public void ExactMatch_IsCaseInsensitive()
    {
        Assert.Equal(100, SearchService.GetFuzzyScore("Chrome", "chrome"));
    }

    [Fact]
    public void CompletelyDifferent_ReturnsLowScore()
    {
        int score = SearchService.GetFuzzyScore("abc", "xyz");
        Assert.True(score < 20, $"Expected low score for completely different strings, got {score}");
    }

    [Fact]
    public void CloseMatch_ReturnsHighScore()
    {
        int score = SearchService.GetFuzzyScore("chrome", "chrom");
        Assert.True(score > 70, $"Expected high score for close match, got {score}");
    }

    [Fact]
    public void NeverReturnsNegative()
    {
        int score = SearchService.GetFuzzyScore("a", "completely different long string");
        Assert.True(score >= 0, $"Score should never be negative, got {score}");
    }

    [Fact]
    public void TypoInQuery_StillScoresReasonably()
    {
        int score = SearchService.GetFuzzyScore("noetpad", "notepad");
        Assert.True(score > 50, $"Expected reasonable score for typo, got {score}");
    }

    [Fact]
    public void ShortQueryAgainstLongTarget_ScoresLow()
    {
        int score = SearchService.GetFuzzyScore("ch", "Google Chrome Browser");
        Assert.True(score < 30, $"Expected low score for short query vs long target, got {score}");
    }
}
