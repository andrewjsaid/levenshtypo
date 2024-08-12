using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieSearchTests
{
    private const string SearchWord = "initiate";

    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));
    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    private readonly Levenshtomaton _automaton0 = new LevenshtomatonFactory().Construct(SearchWord, 0);
    private readonly Levenshtomaton _automaton1 = new LevenshtomatonFactory().Construct(SearchWord, 1);
    private readonly Levenshtomaton _automaton2 = new LevenshtomatonFactory().Construct(SearchWord, 2);
    private readonly Levenshtomaton _automaton3 = new LevenshtomatonFactory().Construct(SearchWord, 3);

    [Benchmark]
    public object Distance0_Dictionary() => _dictionary[SearchWord];

    [Benchmark]
    public object Distance0_Levenshtypo_All() => _levenshtrie.Search(_automaton0);

    [Benchmark]
    public object Distance0_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearch(_automaton0).Count();

    [Benchmark]
    public object Distance0_Levenshtypo_Any() => _levenshtrie.EnumerateSearch(_automaton0).Any();

    [Benchmark]
    public object Distance0_Naive()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (string.Equals(SearchWord, word))
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Distance1_Levenshtypo_All() => _levenshtrie.Search(_automaton1);

    [Benchmark]
    public object Distance1_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearch(_automaton1).Count();

    [Benchmark]
    public object Distance1_Levenshtypo_Any() => _levenshtrie.EnumerateSearch(_automaton1).Any();

    [Benchmark]
    public object Distance1_Naive()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Levenshtein(SearchWord, word) <= 1)
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Distance2_Levenshtypo_All() => _levenshtrie.Search(_automaton2);

    [Benchmark]
    public object Distance2_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearch(_automaton2).Count();

    [Benchmark]
    public object Distance2_Levenshtypo_Any() => _levenshtrie.EnumerateSearch(_automaton2).Any();

    [Benchmark]
    public object Distance2_Naive()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Levenshtein(SearchWord, word) <= 2)
            {
                results.Add(word);
            }
        }
        return results;
    }

    [Benchmark]
    public object Distance3_Levenshtypo_All() => _levenshtrie.Search(_automaton3);

    [Benchmark]
    public object Distance3_Levenshtypo_Lazy() => _levenshtrie.EnumerateSearch(_automaton3).Count();

    [Benchmark]
    public object Distance3_Levenshtypo_Any() => _levenshtrie.EnumerateSearch(_automaton3).Any();

    [Benchmark]
    public object Distance3_Naive()
    {
        var results = new List<string>();
        for (int i = 0; i < _englishWords.Count; i++)
        {
            var word = _englishWords[i];
            if (LevenshteinDistance.Levenshtein(SearchWord, word) <= 3)
            {
                results.Add(word);
            }
        }
        return results;
    }
}
