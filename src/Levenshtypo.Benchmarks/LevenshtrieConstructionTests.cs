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
    public object English_Levenshtypo() => Levenshtrie.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public object Numbers_Levenshtypo() => Levenshtrie.Create(_1000Entries.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public object English_Levenshtypo_Add()
    {
        var trie = Levenshtrie.CreateEmpty<string>();
        foreach (var item in _englishWords)
        {
            trie.Add(item, item);
        }
        return trie;
    }

    [Benchmark]
    public object Numbers_Levenshtypo_Add()
    {
        var trie = Levenshtrie.CreateEmpty<string>();
        foreach (var item in _1000Entries)
        {
            trie.Add(item, item);
        }
        return trie;
    }
}
