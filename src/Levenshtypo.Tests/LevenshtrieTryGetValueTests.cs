using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieTryGetValueTests
{
    [Fact]
    public void LargeDataSet()
    {
        var enFrozenTries = new List<KeyValuePair<string, int>>();
        var nonEnFrozenTries = new List<KeyValuePair<string, int>>();

        for (int i = 0; i < 10000; i++)
        {
            if (i % 17 == 0)
            {
                nonEnFrozenTries.Add(new KeyValuePair<string, int>(i.ToString(), i));
            }
            else
            {
                enFrozenTries.Add(new KeyValuePair<string, int>(i.ToString(), i));
            }
        }

        var t = Levenshtrie<int>.Create(enFrozenTries);

        foreach (var (key, value) in enFrozenTries)
        {
            var found = t.TryGetValue(key, out var actual);
            found.ShouldBeTrue();
            actual.ShouldBe(value);
        }

        foreach (var (key, value) in nonEnFrozenTries)
        {
            var found = t.TryGetValue(key, out var actual);
            found.ShouldBeFalse();
        }
    }

    [Fact]
    public void EmptyString_True()
    {
        var t = Levenshtrie<int>.Create([
            new KeyValuePair<string, int>("", -1),
            new KeyValuePair<string, int>("present", 1),
            ]);

        var foundEmptyString = t.TryGetValue(string.Empty, out var valueOfEmptyString);
        foundEmptyString.ShouldBeTrue();
        valueOfEmptyString.ShouldBe(-1);

        var foundPresent = t.TryGetValue("present", out var valueOfPresent);
        foundPresent.ShouldBeTrue();
        valueOfPresent.ShouldBe(1);

        var foundAbsent = t.TryGetValue("absent", out var _);
        foundAbsent.ShouldBeFalse();

    }

    [Fact]
    public void EmptyString_False()
    {
        var t = Levenshtrie<int>.Create([
            new KeyValuePair<string, int>("present", 1),
            ]);

        var foundEmptyString = t.TryGetValue(string.Empty, out var valueOfEmptyString);
        foundEmptyString.ShouldBeFalse();

        var foundPresent = t.TryGetValue("present", out var valueOfPresent);
        foundPresent.ShouldBeTrue();
        valueOfPresent.ShouldBe(1);

    }

    [Fact]
    public void NoEntries_False()
    {
        var t = Levenshtrie<int>.Create([]);

        var foundEmptyString = t.TryGetValue(string.Empty, out var _);
        foundEmptyString.ShouldBeFalse();

        var foundNonEmptyString = t.TryGetValue("something", out var _);
        foundNonEmptyString.ShouldBeFalse();
    }

    [Fact]
    public void TryGetValue_CaseSensitivity()
    {
        var entries = Enumerable.Range(1, 100).Select(e => new KeyValuePair<string, int>(Guid.NewGuid().ToString().ToLowerInvariant(), e)).ToArray();
        var tCaseSensitive = Levenshtrie<int>.Create(entries, ignoreCase: false);
        var tCaseInsensitive = Levenshtrie<int>.Create(entries, ignoreCase: true);

        foreach (var entry in entries)
        {
            tCaseSensitive.TryGetValue(entry.Key, out var _).ShouldBeTrue();
            tCaseInsensitive.TryGetValue(entry.Key, out var _).ShouldBeTrue();

            tCaseSensitive.TryGetValue(entry.Key.ToUpperInvariant(), out var _).ShouldBeFalse();
            tCaseInsensitive.TryGetValue(entry.Key.ToUpperInvariant(), out var _).ShouldBeTrue();

        }
    }

    [Fact]
    public void TryGetValue_CaseSensitivity_2()
    {
        string[] entries = ["abcde1", "AbCdE2"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)), ignoreCase: true);

        t.TryGetValue("abcde2", out var result).ShouldBeTrue();
        result.ShouldBe("AbCdE2");
    }

    [Fact]
    public void Search_CaseSensitivity()
    {
        var entries = Enumerable.Range(1, 100).Select(e => new KeyValuePair<string, int>(Guid.NewGuid().ToString().ToLowerInvariant(), e)).ToArray();
        var tCaseSensitive = Levenshtrie<int>.Create(entries, ignoreCase: false);
        var tCaseInsensitive = Levenshtrie<int>.Create(entries, ignoreCase: true);

        foreach (var entry in entries)
        {
            tCaseSensitive.Search(entry.Key, 0).ShouldHaveSingleItem();
            tCaseInsensitive.Search(entry.Key, 0).ShouldHaveSingleItem();

            tCaseSensitive.Search(entry.Key.ToUpperInvariant(), 0).ShouldBeEmpty();
            tCaseInsensitive.Search(entry.Key.ToUpperInvariant(), 0).ShouldHaveSingleItem();

        }
    }
}
