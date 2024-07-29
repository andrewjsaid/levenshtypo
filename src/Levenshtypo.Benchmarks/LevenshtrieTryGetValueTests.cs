using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieTryGetValueTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

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
