using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieConstructionTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();

    [Benchmark]
    public object CreateEnglishWords() => Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public object Create1000Entries() => Levenshtrie<string>.Create(_1000Entries.Select(w => new KeyValuePair<string, string>(w, w)));
}
