using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchTests
{
    [Fact]
    public void EmptyString()
    {
        string[] entries = ["", "1", "12", "123"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(t, "", 2, ["", "1", "12"]);
        Test(t, "1", 1, ["", "1", "12"]);
    }

    [Fact]
    public void Food()
    {
        string[] entries = ["mood", "f", "food", "good", "dood", "flood", "fod", "fob", "foodie", "\U0002f971"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(t, "food", 0, ["food"]);
        Test(t, "food", 1, ["food", "good", "dood", "mood", "flood", "fod"]);
        Test(t, "food", 2, ["food", "good", "dood", "mood", "flood", "fod", "fob", "foodie"]);
        Test(t, "\U0001f970", 1, ["f", "\U0002f971"]);
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
            var search = FindWordsWithinNDistance(word, 3);

            Test(levenshtrie, word, 0, search.Where(x => x.distance <= 0));
            Test(levenshtrie, word, 1, search.Where(x => x.distance <= 1));
            Test(levenshtrie, word, 2, search.Where(x => x.distance <= 2));
            Test(levenshtrie, word, 3, search.Where(x => x.distance <= 3));
        }

        IReadOnlyList<(string word, int distance)> FindWordsWithinNDistance(string query, int maxEditDistance)
        {
            var results = new List<(string word, int distance)>();

            foreach (var word in words)
            {
                var distance = LevenshteinDistance.Levenshtein(word, query);
                if (distance <= maxEditDistance)
                {
                    results.Add((word, distance));
                }
            }

            return results;
        }
    }

    [Fact]
    public void StackOverflow_Scenario1()
    {
        string[] entries = [new string('a', 10_000) + 'a', new string('a', 10_000) + 'b'];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(t, entries[0], 1, [(entries[1], 1), (entries[0], 0)]);
    }

    [Fact]
    public void StackOverflow_Scenario2()
    {
        var entries = new List<string>();
        for (int i = 0; i < 10_000; i++)
        {
            entries.Add(new string('a', i));
        }

        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        Test(t, entries[^1], 1, [(entries[^2], 1), (entries[^1], 0)]);
    }

    [Fact]
    public void Set()
    {
        var t = Levenshtrie.CreateEmptySet<string>(ignoreCase: false);

        t.Add("00001", "1");
        t.Add("00001", "2");
        t.Add("00002", "3");
        t.Add("00033", "4");
        t.Add("00004", "5");
        t.Add("00004", "6");
        t.Add("00004", "7");

        t.Search("00001", maxEditDistance: 1)
            .Select(r => r.Result)
            .ToArray()
            .ShouldBe(["1", "2", "3", "5", "6", "7"], ignoreOrder: true, comparer: StringComparer.OrdinalIgnoreCase);

        t.SearchByPrefix("00", maxEditDistance: 0)
            .Select(r => r.Result)
            .ToArray()
            .ShouldBe(["1", "2", "3", "4", "5", "6", "7"], ignoreOrder: true, comparer: StringComparer.OrdinalIgnoreCase);

    }

    private static void Test(Levenshtrie<string> t, string query, int distance, IEnumerable<string> expected)
    {
        Test(t, query, distance, expected.Select(e => (e, LevenshteinDistance.Calculate(query, e))));
    }

    private static void Test(Levenshtrie<string> t, string query, int distance, IEnumerable<(string word, int distance)> expected)
    {
        var expectedResults = expected.Select(e => new LevenshtrieSearchResult<string>(e.word, e.distance, LevenshtrieSearchKind.Full, metadata: 0));

        t.Search(query, distance)
            .ShouldBe(expectedResults, ignoreOrder: true, comparer: new LevenshtrieSearchResultEqualityComparer<string>());

        t.EnumerateSearch(query, distance)
            .ShouldBe(expectedResults, ignoreOrder: true, comparer: new LevenshtrieSearchResultEqualityComparer<string>());
    }
}
