using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchByPrefixTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void EmptyString(bool optimize)
    {
        string[] entries = ["", "1", "12", "123"];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        if (optimize)
        {
            levenshtrie.Optimize();
        }

        Test(levenshtrie, "", 2, ["", "1", "12", "123"]);
        Test(levenshtrie, "1", 1, ["", "1", "12", "123"]);
        Test(levenshtrie, "1", 0, ["1", "12", "123"]);
        Test(levenshtrie, "2", 0, []);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Food(bool optimize)
    {
        string[] entries = ["mood", "f", "food", "good", "dood", "flood", "fod", "fob", "foodie", "foodies", "foodier"];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        if (optimize)
        {
            levenshtrie.Optimize();
        }

        Test(levenshtrie, "food", 0, ["food", "foodie", "foodies", "foodier"]);
        Test(levenshtrie, "food", 1, ["food", "good", "dood", "mood", "flood", "fod", "foodie", "foodies", "foodier"]);
        Test(levenshtrie, "food", 2, ["food", "good", "dood", "mood", "flood", "fod", "fob", "foodie", "foodies", "foodier"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void English(bool optimize)
    {
        var factory = new LevenshtomatonFactory();

        var words = DataHelpers.EnglishWords();

        var levenshtrie = Levenshtrie<string>.Create(words.Select(word => new KeyValuePair<string, string>(word, word)));

        if (optimize)
        {
            levenshtrie.Optimize();
        }

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StackOverflow_Scenario1(bool optimize)
    {
        var baseString = new string('a', 9_999);
        string[] entries = [baseString + 'a', baseString + 'b'];
        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        if (optimize)
        {
            levenshtrie.Optimize();
        }

        Test(levenshtrie, baseString + 'a', 1, [entries[1], entries[0]]);
        Test(levenshtrie, baseString, 0, [entries[1], entries[0]]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void StackOverflow_Scenario2(bool optimize)
    {
        var entries = new List<string>();
        for (int i = 1; i < 10_000; i++)
        {
            entries.Add(new string('a', i));
        }

        var levenshtrie = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        if (optimize)
        {
            levenshtrie.Optimize();
        }

        Test(levenshtrie, entries[^1], 1, [entries[^2], entries[^1]]);
        Test(levenshtrie, "a", 0, entries);
    }

    private static void Test(Levenshtrie<string> t, string query, int distance, IEnumerable<string> expectedResults)
    {
        t.SearchByPrefix(query, distance)
            .OrderBy(x => x)
            .ShouldBe(expectedResults.OrderBy(x => x));

        /*
        t.EnumerateSearch(query, distance)
            .OrderBy(x => x)
            .ShouldBe(expectedResults.OrderBy(x => x));
        */
    }
}
