using BenchmarkDotNet.Attributes;
using Levenshtypo;

public class LevenshtrieTryGetValueTests
{
    private const string SearchWord = "initiate";

    private static readonly IReadOnlyList<string> _englishWords = DataHelpers.EnglishWords();

    private readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));
    private readonly Levenshtrie<string> _levenshtrie = Levenshtrie<string>.Create(_englishWords.Select(w => new KeyValuePair<string, string>(w, w)));

    [Benchmark]
    public bool TryGetValue_Dictionary() => _dictionary.TryGetValue(SearchWord, out var _);

    [Benchmark]
    public bool TryGetValue_Levenshtypo() => _levenshtrie.TryGetValue(SearchWord, out var _);
}
