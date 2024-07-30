using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieSearchTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));
    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 1);
    private readonly Levenshtomaton _automaton2 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 2);
    private readonly Levenshtomaton _automaton3 = new LevenshtomatonFactory().Construct(DataHelpers.Initiate, 3);

    [Benchmark]
    public object Dictionary_Search0() => _dictionary[DataHelpers.Initiate];

    [Benchmark]
    public object Naive_Search0()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (string.Equals(DataHelpers.Initiate, word))
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Search0() => _levenshtrie.Search(_automaton0);

    [Benchmark]
    public object Naive_Search1()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Calculate(DataHelpers.Initiate, word) <= 1)
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Search1() => _levenshtrie.Search(_automaton1);

    [Benchmark]
    public object Naive_Search2()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Calculate(DataHelpers.Initiate, word) <= 2)
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Search2() => _levenshtrie.Search(_automaton2);

    [Benchmark]
    public object Naive_Search3()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Calculate(DataHelpers.Initiate, word) <= 3)
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Search3() => _levenshtrie.Search(_automaton3);
}
