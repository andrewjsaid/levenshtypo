using Levenshtypo;
using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshteinDistanceTests
{
    [Theory]
    [InlineData("a", "a", 0)]
    [InlineData("a", "A", 1)]
    [InlineData("ab", "ab", 0)]
    [InlineData("a", "", 1)]
    [InlineData("", "ab", 2)]

    [InlineData("abc", "a", 2)]
    [InlineData("abc", "ab", 1)]
    [InlineData("abc", "abc", 0)]

    [InlineData("axx", "abc", 2)]
    [InlineData("abx", "abc", 1)]
    public void Levenshtein_CaseSensitiveTests(string a, string b, int distance)
    {
        LevenshteinDistance.Levenshtein(a, b).ShouldBe(distance);
        LevenshteinDistance.Levenshtein(b, a).ShouldBe(distance);
    }

    [Theory]
    [InlineData("a", "A", 0)]
    [InlineData("ab", "AB", 0)]
    [InlineData("a", "", 1)]
    [InlineData("", "AB", 2)]

    [InlineData("abc", "A", 2)]
    [InlineData("abc", "AB", 1)]
    [InlineData("abc", "ABC", 0)]

    [InlineData("axx", "ABC", 2)]
    [InlineData("abx", "ABC", 1)]
    public void Levenshtein_CaseInsensitiveTests(string a, string b, int distance)
    {
        LevenshteinDistance.Levenshtein(a, b, ignoreCase: true).ShouldBe(distance);
        LevenshteinDistance.Levenshtein(b, a, ignoreCase: true).ShouldBe(distance);
    }
}