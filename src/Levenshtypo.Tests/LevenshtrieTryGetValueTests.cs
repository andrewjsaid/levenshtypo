using Levenshtypo;
using Shouldly;

namespace Tests;

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
}
