using Shouldly;

namespace Levenshtypo.Tests;

public class LevenshtrieSearchTests
{
    [Fact]
    public void EmptyString()
    {
        string[] entries = ["", "1", "12", "123"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        t.Search(new LevenshtomatonFactory().Construct("", 2)).Select(r => r.Result)
            .ShouldBe(["", "1", "12"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("1", 1)).Select(r => r.Result)
            .ShouldBe(["", "1", "12"], ignoreOrder: true);
    }

    [Fact]
    public void Food()
    {
        string[] entries = ["f", "food", "good", "mood", "flood", "fod", "fob", "foodie", "\U0002f971"];
        var t = Levenshtrie<string>.Create(entries.Select(e => new KeyValuePair<string, string>(e, e)));

        t.Search(new LevenshtomatonFactory().Construct("food", 0)).Select(r => r.Result)
            .ShouldBe(["food"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 1)).Select(r => r.Result)
            .ShouldBe(["food", "good", "mood", "flood", "fod"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("food", 2)).Select(r => r.Result)
            .ShouldBe(["food", "good", "mood", "flood", "fod", "fob", "foodie"], ignoreOrder: true);

        t.Search(new LevenshtomatonFactory().Construct("\U0001f970", 1)).Select(r => r.Result)
            .ShouldBe(["f", "\U0002f971"], ignoreOrder: true);
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

            levenshtrie
                .Search(word, 0)
                .ShouldBe(search.Where(x => x.distance <= 0).Select(x => new LevenshtrieSearchResult<string>(x.distance, x.word)), ignoreOrder: true);

            levenshtrie.
                Search(word, 1)
                .ShouldBe(search.Where(x => x.distance <= 1).Select(x => new LevenshtrieSearchResult<string>(x.distance, x.word)), ignoreOrder: true);

            levenshtrie
                .Search(word, 2)
                .ShouldBe(search.Where(x => x.distance <= 2).Select(x => new LevenshtrieSearchResult<string>(x.distance, x.word)), ignoreOrder: true);

            levenshtrie
                .Search(word, 3)
                .ShouldBe(search.Where(x => x.distance <= 3).Select(x => new LevenshtrieSearchResult<string>(x.distance, x.word)), ignoreOrder: true);

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

        t.Search(entries[0], maxEditDistance: 1).Select(r => r.Result)
            .ShouldBe(entries);
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

        t.Search(entries.Last(), maxEditDistance: 1).Select(r => r.Result)
            .ShouldBe(entries[^2..]);
    }
}
