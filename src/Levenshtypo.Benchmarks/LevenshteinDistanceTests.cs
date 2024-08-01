using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshteinDistanceTests
{
    private const string SearchWord = "initiate";

    private static readonly string[] _englishWords = DataHelpers.EnglishWords().ToArray();
    private static readonly Levenshtomaton _automaton2 = LevenshtomatonFactory.Instance.Construct(SearchWord, maxEditDistance: 2);

    [Benchmark]
    public int Using_static_method()
    {
        var words = _englishWords;

        int count = 0;
        for (int i = 0; i < words.Length; i++)
        {
            if (LevenshteinDistance.Levenshtein(SearchWord, words[i]) <= 2)
            {
                count++;
            }
        }
        return count;
    }

    [Benchmark]
    public int Using_automaton()
    {
        var words = _englishWords;

        int count = 0;
        for (int i = 0; i < words.Length; i++)
        {
            if (_automaton2.Matches(words[i]))
            {
                count++;
            }
        }
        return count;
    }
}
