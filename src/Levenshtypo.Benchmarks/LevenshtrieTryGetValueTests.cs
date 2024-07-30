using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieTryGetValueTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));
    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    // Compare against inbuilt dictionary
    [Benchmark]
    public bool Dictionary_TryGetValue_Single() => _dictionary.TryGetValue(DataHelpers.Initiate, out var _);

    // Compare against inbuilt dictionary
    [Benchmark]
    public int Dictionary_TryGetValue_Many()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_dictionary.TryGetValue(searchWord, out var _))
            {
                matches++;
            }
        }
        return matches;
    }

    [Benchmark]
    public bool TryGetValue_Single() => _levenshtrie.TryGetValue(DataHelpers.Initiate, out var _);

    [Benchmark]
    public int TryGetValue_Many()
    {
        var matches = 0;
        foreach (var searchWord in DataHelpers.InitiateDistance3)
        {
            if (_levenshtrie.TryGetValue(searchWord, out var _))
            {
                matches++;
            }
        }
        return matches;
    }
}
