using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieConstructionTests
{
    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();
    private static readonly IReadOnlyList<string> _1000Entries = Enumerable.Range(0, 1000).Select(i => i.ToString()).ToList();


    // Compare against inbuilt dictionary
    [Benchmark]
    public object English_Dictionary() => new Dictionary<string,string>(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    // Compare against inbuilt dictionary
    [Benchmark]
    public object Numbers_Dictionary() => new Dictionary<string, string>(_1000Entries.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public object English_Levenshtypo() => Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public object Numbers_Levenshtypo() => Levenshtrie<string>.Create(_1000Entries.Select(w => new KeyValuePair<string, string>(w, w)));
}
