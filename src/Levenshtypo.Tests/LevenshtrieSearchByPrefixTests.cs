using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchByPrefixTests
{
    [Fact]
    public void EmptyString()
    {
        string[] entries = ["", "1", "12", "123"];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(levenshtrie, "", 2, ["", "1", "12", "123"]);
        Test(levenshtrie, "1", 1, ["", "1", "12", "123"]);
        Test(levenshtrie, "1", 0, ["1", "12", "123"]);
        Test(levenshtrie, "2", 0, []);
    }

    [Fact]
    public void Food()
    {
        string[] entries = ["mood", "f", "food", "good", "dood", "flood", "fod", "fob", "foodie", "foodies", "foodier"];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(levenshtrie, "food", 0, ["food", "foodie", "foodies", "foodier"]);
        Test(levenshtrie, "food", 1, ["food", "good", "dood", "mood", "flood", "fod", "foodie", "foodies", "foodier"]);
        Test(levenshtrie, "food", 2, ["food", "good", "dood", "mood", "flood", "fod", "fob", "foodie", "foodies", "foodier"]);
    }

    [Fact]
    public void English()
    {
        var factory = new LevenshtomatonFactory();

        var words = DataHelpers.EnglishWords();

        var levenshtrie = Levenshtrie<string>.Create(words.Select(word => new KeyValuePair<string, string>(word, word)));

        RunTest("hello");
        RunTest("world");
        RunTest("notaword");
        RunTest("messenger");
        RunTest("pool");
        RunTest("bad");

        void RunTest(string word)
        {
            var search = FindWordsWithinNDistance(LevenshtomatonFactory.Instance.Construct(word, 3));

            Test(levenshtrie, word, 0, search.Where(x => x.distance <= 0).Select(x => x.word));
            Test(levenshtrie, word, 1, search.Where(x => x.distance <= 1).Select(x => x.word));
            Test(levenshtrie, word, 2, search.Where(x => x.distance <= 2).Select(x => x.word));
            Test(levenshtrie, word, 3, search.Where(x => x.distance <= 3).Select(x => x.word));
        }

        IReadOnlyList<(string word, int distance)> FindWordsWithinNDistance(Levenshtomaton query)
        {
            var results = new List<(string word, int distance)>();

            foreach (var word in words)
            {
                int? minEditDistance = null;

                var state = query.Start();

                foreach (var rune in word.EnumerateRunes())
                {
                    if (!state.MoveNext(rune, out var nextState))
                        break;

                    state = nextState;
                    if (nextState.IsFinal)
                    {
                        if (minEditDistance is null || minEditDistance.Value > nextState.Distance)
                        {
                            minEditDistance = nextState.Distance;
                        }
                    }
                }

                if (minEditDistance is not null)
                {
                    results.Add((word, minEditDistance.Value));
                }
            }

            return results;
        }
    }

    [Fact]
    public void StackOverflow_Scenario1()
    {
        var baseString = new string('a', 9_999);
        string[] entries = [baseString + 'a', baseString + 'b'];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(levenshtrie, baseString + 'a', 1, [entries[1], entries[0]]);
        Test(levenshtrie, baseString, 0, [entries[1], entries[0]]);
    }

    [Fact]
    public void StackOverflow_Scenario2()
    {
        var entries = new List<string>();
        for (int i = 1; i < 10_000; i++)
        {
            entries.Add(new string('a', i));
        }

        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(levenshtrie, entries[^1], 1, [entries[^2], entries[^1]]);
        Test(levenshtrie, "a", 0, entries);
    }

    private static void Test(Levenshtrie<string> t, string query, int distance, IEnumerable<string> expected)
    {
        var list = expected.ToList();
        if (list.Count > 10_000)
        {
            // Test will be too slow for Shouldly to compare with ignoreCase.
            // N.B. to be clear: Levenshtypo can easily handle such numbers
            return;
        }

        var expectedResults = list.Select(e =>
        {
            var (distance, metadata) = CalculatePrefixDistance(query, e);
            return new LevenshtrieSearchResult<string>(e, distance, LevenshtrieSearchKind.Prefix, metadata);
        }).ToArray();

        t.SearchByPrefix(query, distance)
            .ShouldBe(expectedResults, ignoreOrder: true, comparer: new LevenshtrieSearchResultEqualityComparer<string>());

        t.EnumerateSearchByPrefix(query, distance)
            .ShouldBe(expectedResults, ignoreOrder: true, comparer: new LevenshtrieSearchResultEqualityComparer<string>());
    }

    private static (int distance, int metadata) CalculatePrefixDistance(string query, string result)
    {
        int minDistance = int.MaxValue;
        int totalLength = 0;
        int minDistanceSuffix = 0;

        var automaton = LevenshtomatonFactory.Instance.Construct(query, maxEditDistance: 5);

        var state = automaton.Start();

        if (state.IsFinal && state.Distance < minDistance)
        {
            minDistance = state.Distance;
            minDistanceSuffix = 0;
        }

        bool stop = false;

        foreach (var r in result.EnumerateRunes())
        {
            totalLength++;
            minDistanceSuffix++;

            if (stop)
            {
                continue;
            }

            if (!state.MoveNext(r, out state))
            {
                stop = true;
                continue;
            }

            if (!stop)
            {
                if (state.IsFinal && state.Distance < minDistance)
                {
                    minDistance = state.Distance;
                    minDistanceSuffix = 0;
                }
            }
        }

        var metadata = PrefixMetadataUtils.EncodeMetadata(totalLength - minDistanceSuffix, minDistanceSuffix);
        return (minDistance, metadata);
    }
}
