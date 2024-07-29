using System.IO.Compression;
using System.Reflection;
using Levenshtypo;
using Shouldly;

namespace Tests;

public class LevenshtrieSearchLanguageTests
{

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
                .Search(factory.Construct(word, 0))
                .ShouldBe(search.Where(x => x.distance <= 0).Select(x => x.word), ignoreOrder: true);

            levenshtrie.
                Search(factory.Construct(word, 1))
                .ShouldBe(search.Where(x => x.distance <= 1).Select(x => x.word), ignoreOrder: true);

            levenshtrie
                .Search(factory.Construct(word, 2))
                .ShouldBe(search.Where(x => x.distance <= 2).Select(x => x.word), ignoreOrder: true);

            levenshtrie
                .Search(factory.Construct(word, 3))
                .ShouldBe(search.Where(x => x.distance <= 3).Select(x => x.word), ignoreOrder: true);

        }

        IReadOnlyList<(string word, int distance)> FindWordsWithinNDistance(string query, int maxEditDistance)
        {
            var results = new List<(string word, int distance)>();

            foreach (var word in words)
            {
                var distance = LevenshteinDistance.CalculateCaseSensitive(word, query);
                if (distance <= maxEditDistance)
                {
                    results.Add((word, distance));
                }
            }

            return results;
        }
    }
}
